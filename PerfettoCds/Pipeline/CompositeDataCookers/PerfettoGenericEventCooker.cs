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
    /// Pulls data from all the individual SQL tables and joins them to create a Generic Peretto event
    /// </summary>
    public sealed class PerfettoGenericEventCooker : CookedDataReflector, ICompositeDataCookerDescriptor
    {
        public static readonly DataCookerPath DataCookerPath = PerfettoPluginConstants.GenericEventCookerPath;

        public string Description => "Generic Event composite cooker";

        public DataCookerPath Path => DataCookerPath;

        // Declare all of the cookers that are used by this CompositeCooker.
        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new[]
        {
            PerfettoPluginConstants.SliceCookerPath,
            PerfettoPluginConstants.ArgCookerPath,
            PerfettoPluginConstants.ThreadTrackCookerPath,
            PerfettoPluginConstants.ThreadCookerPath,
            PerfettoPluginConstants.ProcessCookerPath
        };

        [DataOutput]
        public ProcessedEventData<PerfettoGenericEvent> GenericEvents { get; }

        /// <summary>
        /// The highest number of fields found in any single event.
        /// </summary>
        [DataOutput]
        public int MaximumEventFieldCount { get; private set; }

        public PerfettoGenericEventCooker() : base(PerfettoPluginConstants.GenericEventCookerPath)
        {
            this.GenericEvents =
                new ProcessedEventData<PerfettoGenericEvent>();
        }

        public void OnDataAvailable(IDataExtensionRetrieval requiredData)
        {
            // Gather the data from all the SQL tables
            var sliceData = requiredData.QueryOutput<ProcessedEventData<PerfettoSliceEvent>>(new DataOutputPath(PerfettoPluginConstants.SliceCookerPath, nameof(PerfettoSliceCooker.SliceEvents)));
            var argData = requiredData.QueryOutput<ProcessedEventData<PerfettoArgEvent>>(new DataOutputPath(PerfettoPluginConstants.ArgCookerPath, nameof(PerfettoArgCooker.ArgEvents)));
            var threadTrackData = requiredData.QueryOutput<ProcessedEventData<PerfettoThreadTrackEvent>>(new DataOutputPath(PerfettoPluginConstants.ThreadTrackCookerPath, nameof(PerfettoThreadTrackCooker.ThreadTrackEvents)));
            var threadData = requiredData.QueryOutput<ProcessedEventData<PerfettoThreadEvent>>(new DataOutputPath(PerfettoPluginConstants.ThreadCookerPath, nameof(PerfettoThreadCooker.ThreadEvents)));
            var processData = requiredData.QueryOutput<ProcessedEventData<PerfettoProcessEvent>>(new DataOutputPath(PerfettoPluginConstants.ProcessCookerPath, nameof(PerfettoProcessCooker.ProcessEvents)));

            // Join them all together

            // Slice data contains event name and a few more fields
            // Arg data contains the debug annotations
            // ThreadTrack data allows us to get to the thread
            // Thread data gives us the thread name+ID and gets us the process
            // Process data gives us the process name+ID
            var joined = from slice in sliceData
                         join arg in argData on slice.ArgSetId equals arg.ArgSetId into args
                         join threadTrack in threadTrackData on slice.TrackId equals threadTrack.Id
                         join thread in threadData on threadTrack.Utid equals thread.Utid
                         join process in processData on thread.Upid equals process.Upid
                         select new { slice, args, threadTrack, thread, process };

            // Create events out of the joined results
            foreach (var result in joined)
            {
                MaximumEventFieldCount = Math.Max(MaximumEventFieldCount, result.args.Count());

                List<string> argKeys = new List<string>();
                List<string> values = new List<string>();
                // Each event has multiple of these "debug annotations". They get stored in lists
                foreach (var arg in result.args)
                {
                    argKeys.Add(arg.ArgKey);
                    switch (arg.ValueType)
                    {
                        case "string":
                            values.Add(arg.StringValue);
                            break;
                        case "bool":
                        case "int":
                            values.Add(arg.IntValue.ToString());
                            break;
                        case "uint":
                        case "pointer":
                            values.Add(((uint)arg.IntValue).ToString());
                            break;
                        case "real":
                            values.Add(arg.RealValue.ToString());
                            break;
                        default:
                            throw new Exception("Unexpected Perfetto value type");
                    }
                }
                PerfettoGenericEvent ev = new PerfettoGenericEvent
                (
                   result.slice.Name,
                   result.slice.Type,
                   new TimestampDelta(result.slice.Duration),
                   new Timestamp(result.slice.Timestamp),
                   new Timestamp(result.slice.Timestamp + result.slice.Duration),
                   result.slice.Category,
                   result.slice.ArgSetId,
                   values,
                   argKeys,
                   string.Format($"{result.process.Name} {result.process.Pid}"),
                   string.Format($"{result.thread.Name} {result.thread.Tid}")
                );
                this.GenericEvents.AddEvent(ev);
            }
            this.GenericEvents.FinalizeData();
        }
    }
}
