// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Processing;
using PerfettoCds.Pipeline.DataOutput;
using PerfettoProcessor;

namespace PerfettoCds.Pipeline.DataCookers
{
    /// <summary>
    /// Pulls data from multiple individual SQL tables and joins them to create a Generic Peretto event TODO
    /// </summary>
    public sealed class PerfettoFtraceEventCooker : CookedDataReflector, ICompositeDataCookerDescriptor
    {
        public static readonly DataCookerPath DataCookerPath = PerfettoPluginConstants.FtraceEventCookerPath;

        public string Description => "Generic Event composite cooker";

        public DataCookerPath Path => DataCookerPath;

        // Declare all of the cookers that are used by this CompositeCooker.
        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new[]
        {
            PerfettoPluginConstants.RawCookerPath,
            PerfettoPluginConstants.ThreadCookerPath,
            PerfettoPluginConstants.ProcessCookerPath,
            PerfettoPluginConstants.ArgCookerPath
        };

        [DataOutput]
        public ProcessedEventData<PerfettoFtraceEvent> FtraceEvents { get; }

        /// <summary>
        /// The highest number of fields found in any single event.
        /// </summary>
        [DataOutput]
        public int MaximumEventFieldCount { get; private set; }

        public PerfettoFtraceEventCooker() : base(PerfettoPluginConstants.FtraceEventCookerPath)
        {
            this.FtraceEvents =
                new ProcessedEventData<PerfettoFtraceEvent>();
        }

        public void OnDataAvailable(IDataExtensionRetrieval requiredData)
        {
            // Gather the data from all the SQL tables
            var rawData = requiredData.QueryOutput<ProcessedEventData<PerfettoRawEvent>>(new DataOutputPath(PerfettoPluginConstants.RawCookerPath, nameof(PerfettoRawCooker.RawEvents)));
            var threadData = requiredData.QueryOutput<ProcessedEventData<PerfettoThreadEvent>>(new DataOutputPath(PerfettoPluginConstants.ThreadCookerPath, nameof(PerfettoThreadCooker.ThreadEvents)));
            var processData = requiredData.QueryOutput<ProcessedEventData<PerfettoProcessEvent>>(new DataOutputPath(PerfettoPluginConstants.ProcessCookerPath, nameof(PerfettoProcessCooker.ProcessEvents)));
            var argsData = requiredData.QueryOutput<ProcessedEventData<PerfettoArgEvent>>(new DataOutputPath(PerfettoPluginConstants.ArgCookerPath, nameof(PerfettoArgCooker.ArgEvents)));

            // Join them all together

            // TODO describe data
            var joined = from raw in rawData
                         join thread in threadData on raw.Utid equals thread.Utid
                         join process in processData on thread.Upid equals process.Upid
                         join arg in argsData on raw.ArgSetId equals arg.ArgSetId into args
                         select new { raw, args, thread, process };

            // Create events out of the joined results
            foreach (var result in joined)
            {
                PerfettoFtraceEvent ev = new PerfettoFtraceEvent
                (
                    new Timestamp(result.raw.Timestamp),
                    result.process.Name,
                    result.thread.Name,
                    result.raw.Cpu, 
                    result.raw.Name                 
                );
                this.FtraceEvents.AddEvent(ev);
            }
            this.FtraceEvents.FinalizeData();
        }
    }
}
