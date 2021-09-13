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
using PerfettoCds.Pipeline.SourceDataCookers;
using PerfettoProcessor;

namespace PerfettoCds.Pipeline.CompositeDataCookers
{
    /// <summary>
    /// Pulls data from multiple individual SQL tables and joins them to create a CPU scheduling event
    /// </summary>
    public sealed class PerfettoCpuSchedEventCooker : CookedDataReflector, ICompositeDataCookerDescriptor
    {
        public static readonly DataCookerPath DataCookerPath = PerfettoPluginConstants.CpuSchedEventCookerPath;

        public string Description => "CPU scheduling event composite cooker";

        public DataCookerPath Path => DataCookerPath;

        // Declare all of the cookers that are used by this CompositeCooker.
        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new[]
        {
            PerfettoPluginConstants.ThreadCookerPath,
            PerfettoPluginConstants.ProcessCookerPath,
            PerfettoPluginConstants.SchedSliceCookerPath
        };

        [DataOutput]
        public ProcessedEventData<PerfettoCpuSchedEvent> CpuSchedEvents { get; }

        public PerfettoCpuSchedEventCooker() : base(PerfettoPluginConstants.CpuSchedEventCookerPath)
        {
            this.CpuSchedEvents =
                new ProcessedEventData<PerfettoCpuSchedEvent>();
        }

        public void OnDataAvailable(IDataExtensionRetrieval requiredData)
        {
            // Gather the data from all the SQL tables
            var threadData = requiredData.QueryOutput<ProcessedEventData<PerfettoThreadEvent>>(new DataOutputPath(PerfettoPluginConstants.ThreadCookerPath, nameof(PerfettoThreadCooker.ThreadEvents)));
            var processData = requiredData.QueryOutput<ProcessedEventData<PerfettoProcessEvent>>(new DataOutputPath(PerfettoPluginConstants.ProcessCookerPath, nameof(PerfettoProcessCooker.ProcessEvents)));
            var schedSliceData = requiredData.QueryOutput<ProcessedEventData<PerfettoSchedSliceEvent>>(new DataOutputPath(PerfettoPluginConstants.SchedSliceCookerPath, nameof(PerfettoSchedSliceCooker.SchedSliceEvents)));

            // The sched slice data contains the timings, CPU, priority, and end state info. We get the process and thread from
            // those respective tables
            var joined = from schedSlice in schedSliceData
                         join thread in threadData on schedSlice.Utid equals thread.Utid
                         join process in processData on thread.Upid equals process.Upid into pd from process in pd.DefaultIfEmpty()
                         select new { schedSlice, thread, process };

            // Create events out of the joined results
            foreach (var result in joined)
            {
                PerfettoCpuSchedEvent ev = new PerfettoCpuSchedEvent
                (
                    result.process?.Name,
                    result.thread.Name,
                    new TimestampDelta(result.schedSlice.Duration),
                    new Timestamp(result.schedSlice.RelativeTimestamp),
                    new Timestamp(result.schedSlice.RelativeTimestamp + result.schedSlice.Duration),
                    result.schedSlice.Cpu,
                    result.schedSlice.EndStateStr,
                    result.schedSlice.Priority
                );
                this.CpuSchedEvents.AddEvent(ev);
            }
            this.CpuSchedEvents.FinalizeData();
        }
    }
}
