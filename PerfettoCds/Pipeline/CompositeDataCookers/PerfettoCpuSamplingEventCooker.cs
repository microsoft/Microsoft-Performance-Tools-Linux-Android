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
    /// Pulls data from multiple individual SQL tables and joins them to create a CPU sampling event
    /// </summary>
    public sealed class PerfettoCpuSamplingEventCooker : CookedDataReflector, ICompositeDataCookerDescriptor
    {
        public static readonly DataCookerPath DataCookerPath = PerfettoPluginConstants.CpuSamplingEventCookerPath;

        public string Description => "CPU sampling event composite cooker";

        public DataCookerPath Path => DataCookerPath;

        // Declare all of the cookers that are used by this CompositeCooker.
        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new[]
        {
            PerfettoPluginConstants.PerfSampleCookerPath,
            PerfettoPluginConstants.StackProfileCallSiteCookerPath,
            PerfettoPluginConstants.StackProfileFrameCookerPath,
            PerfettoPluginConstants.StackProfileMappingCookerPath,
            PerfettoPluginConstants.StackProfileSymbolCookerPath,
            PerfettoPluginConstants.ThreadCookerPath,
            PerfettoPluginConstants.ProcessRawCookerPath,
        };

        [DataOutput]
        public ProcessedEventData<PerfettoCpuSamplingEvent> CpuSamplingEvents { get; }

        public PerfettoCpuSamplingEventCooker() : base(PerfettoPluginConstants.CpuSamplingEventCookerPath)
        {
            this.CpuSamplingEvents = new ProcessedEventData<PerfettoCpuSamplingEvent>();
        }

        public void OnDataAvailable(IDataExtensionRetrieval requiredData)
        {
            // Gather the data from all the SQL tables
            var threadData = requiredData.QueryOutput<ProcessedEventData<PerfettoThreadEvent>>(new DataOutputPath(PerfettoPluginConstants.ThreadCookerPath, nameof(PerfettoThreadCooker.ThreadEvents)));
            var processData = requiredData.QueryOutput<ProcessedEventData<PerfettoProcessRawEvent>>(new DataOutputPath(PerfettoPluginConstants.ProcessRawCookerPath, nameof(PerfettoProcessRawCooker.ProcessEvents)));

            var perfSampleData = requiredData.QueryOutput<ProcessedEventData<PerfettoPerfSampleEvent>>(new DataOutputPath(PerfettoPluginConstants.PerfSampleCookerPath, nameof(PerfettoPerfSampleCooker.PerfSampleEvents)));
            var stackProfileCallSiteData = requiredData.QueryOutput<ProcessedEventData<PerfettoStackProfileCallSiteEvent>>(new DataOutputPath(PerfettoPluginConstants.StackProfileCallSiteCookerPath, nameof(PerfettoStackProfileCallSiteCooker.StackProfileCallSiteEvents)));
            var stackProfileFrameData = requiredData.QueryOutput<ProcessedEventData<PerfettoStackProfileFrameEvent>>(new DataOutputPath(PerfettoPluginConstants.StackProfileFrameCookerPath, nameof(PerfettoStackProfileFrameCooker.StackProfileFrameEvents)));
            var stackProfileMappingData = requiredData.QueryOutput<ProcessedEventData<PerfettoStackProfileMappingEvent>>(new DataOutputPath(PerfettoPluginConstants.StackProfileMappingCookerPath, nameof(PerfettoStackProfileMappingCooker.StackProfileMappingEvents)));

            // stackProfileSymbolData doesn't seem to have data now in the traces we have seen
            //var stackProfileSymbolData = requiredData.QueryOutput<ProcessedEventData<PerfettoStackProfileSymbolEvent>>(new DataOutputPath(PerfettoPluginConstants.StackProfileSymbolCookerPath, nameof(PerfettoStackProfileSymbolCooker.StackProfileSymbolEvents)));

            // We need to join a bunch of tables to get the cpu samples with stack and module information
            var joined = from perfSample in perfSampleData
                         join thread in threadData on perfSample.Utid equals thread.Id
                         join threadProcess in processData on thread.Upid equals threadProcess.Upid
                           into pd
                         from threadProcess in pd.DefaultIfEmpty()  // left outer
                         select new { perfSample, thread, threadProcess }; // stackProfileCallSite

            var stackWalker = new StackWalk(stackProfileCallSiteData, stackProfileFrameData, stackProfileMappingData);

            // Create events out of the joined results
            foreach (var result in joined)
            {
                // Walk the stack
                StackWalkResult stackWalkResult = null;
                if (result.perfSample.CallsiteId.HasValue)
                {
                    stackWalkResult = stackWalker.WalkStack(result.perfSample.CallsiteId.Value);
                }

                // An event can have a thread+process or just a process
                string processName = string.Empty;
                string threadName = $"{result.thread.Name} ({result.thread.Tid})";
                if (result.threadProcess != null)
                {
                    processName = $"{result.threadProcess.Name} ({result.threadProcess.Pid})";
                }

                var ev = new PerfettoCpuSamplingEvent
                (
                    processName,
                    threadName,
                    new Timestamp(result.perfSample.RelativeTimestamp),
                    result.perfSample.Cpu,
                    result.perfSample.CpuMode,
                    result.perfSample.UnwindError,
                    stackWalkResult?.Stack,
                    stackWalkResult?.Module,
                    stackWalkResult?.Function
                );
                this.CpuSamplingEvents.AddEvent(ev);
            }
            this.CpuSamplingEvents.FinalizeData();
        }
    }
}
