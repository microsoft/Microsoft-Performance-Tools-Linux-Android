// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Linq;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Processing;
using PerfettoCds.Pipeline.DataOutput;
using PerfettoCds.Pipeline.SourceDataCookers;
using PerfettoProcessor;

namespace PerfettoCds.Pipeline.CompositeDataCookers
{
    /// <summary>
    /// Pulls data from multiple individual SQL tables and joins them to create events for logcat output
    /// </summary>
    public sealed class PerfettoLogcatEventCooker : CookedDataReflector, ICompositeDataCookerDescriptor
    {
        public static readonly DataCookerPath DataCookerPath = PerfettoPluginConstants.LogcatEventCookerPath;

        public string Description => "Logcat event composite cooker";

        public DataCookerPath Path => DataCookerPath;

        // Declare all of the cookers that are used by this CompositeCooker.
        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new[]
        {
            PerfettoPluginConstants.AndroidLogCookerPath,
            PerfettoPluginConstants.ThreadCookerPath,
            PerfettoPluginConstants.ProcessCookerPath
        };

        [DataOutput]
        public ProcessedEventData<PerfettoLogcatEvent> LogcatEvents { get; }

        /// <summary>
        /// The highest number of fields found in any single event.
        /// </summary>
        [DataOutput]
        public int MaximumEventFieldCount { get; private set; }

        public PerfettoLogcatEventCooker() : base(PerfettoPluginConstants.LogcatEventCookerPath)
        {
            this.LogcatEvents =
                new ProcessedEventData<PerfettoLogcatEvent>();
        }

        public void OnDataAvailable(IDataExtensionRetrieval requiredData)
        {
            // Gather the data from all the SQL tables
            var threadData = requiredData.QueryOutput<ProcessedEventData<PerfettoThreadEvent>>(new DataOutputPath(PerfettoPluginConstants.ThreadCookerPath, nameof(PerfettoThreadCooker.ThreadEvents)));
            var processData = requiredData.QueryOutput<ProcessedEventData<PerfettoProcessEvent>>(new DataOutputPath(PerfettoPluginConstants.ProcessCookerPath, nameof(PerfettoProcessCooker.ProcessEvents)));
            var androidLogData = requiredData.QueryOutput<ProcessedEventData<PerfettoAndroidLogEvent>>(new DataOutputPath(PerfettoPluginConstants.AndroidLogCookerPath, nameof(PerfettoAndroidLogCooker.AndroidLogEvents)));

            // Join them all together

            // Log contains the information for each logcat message
            // Thread and process info are gathered from their respective tables
            var joined = from log in androidLogData
                         join thread in threadData on log.Utid equals thread.Utid
                         join process in processData on thread.Upid equals process.Upid
                         select new { log, thread, process };

            // Create events out of the joined results
            foreach (var result in joined)
            {
                PerfettoLogcatEvent ev = new PerfettoLogcatEvent
                (
                    new Timestamp(result.log.RelativeTimestamp),
                    result.process.Name,
                    result.thread.Name,
                    result.log.PriorityString,
                    result.log.Tag,
                    result.log.Message
                );
                this.LogcatEvents.AddEvent(ev);
            }
            this.LogcatEvents.FinalizeData();
        }
    }
}
