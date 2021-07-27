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
using PerfettoProcessor.Events;

namespace PerfettoCds.Pipeline.DataCookers
{
    /// <summary>
    /// Pulls data from all the individual SQL tables and joins them to create a Generic Peretto event
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

        /// <summary>
        /// The highest number of fields found in any single event.
        /// </summary>
        [DataOutput]
        public int MaximumEventFieldCount { get; private set; }

        public PerfettoCpuSchedEventCooker() : base(PerfettoPluginConstants.CpuSchedEventCookerPath)
        {
            this.CpuSchedEvents =
                new ProcessedEventData<PerfettoCpuSchedEvent>();
        }

        public void OnDataAvailable(IDataExtensionRetrieval requiredData)
        {
            var dateTimeQueryStarted = DateTime.UtcNow;
            // Gather the data from all the SQL tables
            var threadData = requiredData.QueryOutput<ProcessedEventData<PerfettoThreadEvent>>(new DataOutputPath(PerfettoPluginConstants.ThreadCookerPath, nameof(PerfettoThreadCooker.ThreadEvents)));
            var processData = requiredData.QueryOutput<ProcessedEventData<PerfettoProcessEvent>>(new DataOutputPath(PerfettoPluginConstants.ProcessCookerPath, nameof(PerfettoProcessCooker.ProcessEvents)));
            var schedSliceData = requiredData.QueryOutput<ProcessedEventData<PerfettoSchedSliceEvent>>(new DataOutputPath(PerfettoPluginConstants.SchedSliceCookerPath, nameof(PerfettoSchedSliceCooker.SchedSliceEvents)));

            var dateTimeQueryFinished = DateTime.UtcNow;
            Console.Error.WriteLine($"*** GenericEventCooker queries completed in {(dateTimeQueryFinished - dateTimeQueryStarted).TotalSeconds}s at {dateTimeQueryFinished} UTC");

            // Join them all together
            dateTimeQueryStarted = DateTime.UtcNow;

            // Slice data contains event name and a few more fields
            // Arg data contains the debug annotations
            // ThreadTrack data allows us to get to the thread
            // Thread data gives us the thread name+ID and gets us the process
            // Process data gives us the process name+ID
            //var joined = from slice in sliceData
            //             join arg in argData on slice.ArgSetId equals arg.ArgSetId into args
            //             join threadTrack in threadTrackData on slice.TrackId equals threadTrack.Id
            //             join thread in threadData on threadTrack.Utid equals thread.Utid
            //             join process in processData on thread.Upid equals process.Upid
            //             select new { slice, args, threadTrack, thread, process };

            var joined = from schedSlice in schedSliceData
                         join thread in threadData on schedSlice.Utid equals thread.Utid
                         join process in processData on thread.Upid equals process.Upid
                         select new { schedSlice, thread, process };

            dateTimeQueryFinished = DateTime.UtcNow;
            Console.Error.WriteLine($"*** Giant join completed in {(dateTimeQueryFinished - dateTimeQueryStarted).TotalSeconds}s at {dateTimeQueryFinished} UTC");

            dateTimeQueryStarted = DateTime.UtcNow;

            // Create events out of the joined results
            foreach (var result in joined)
            {
                PerfettoCpuSchedEvent ev = new PerfettoCpuSchedEvent
                (
                    result.process.Name,
                    result.thread.Name,
                    new TimestampDelta(result.schedSlice.Duration),
                    new Timestamp(result.schedSlice.Timestamp),
                    new Timestamp(result.schedSlice.Timestamp + result.schedSlice.Duration),
                    result.schedSlice.Cpu,
                    result.schedSlice.EndStateStr,
                    result.schedSlice.Priority
                );
                this.CpuSchedEvents.AddEvent(ev);
            }
            this.CpuSchedEvents.FinalizeData();
            dateTimeQueryFinished = DateTime.UtcNow;
            Console.Error.WriteLine($"*** Giant event processing completed in {(dateTimeQueryFinished - dateTimeQueryStarted).TotalSeconds}s at {dateTimeQueryFinished} UTC");
        }
    }
}
