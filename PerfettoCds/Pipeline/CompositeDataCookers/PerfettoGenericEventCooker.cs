using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Processing;
using PerfettoCds.Pipeline.DataOutput;
using PerfettoCds.Pipeline.Events;

namespace PerfettoCds.Pipeline.DataCookers
{
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
            var sliceData = requiredData.QueryOutput<ProcessedEventData<PerfettoSliceEvent>>(new DataOutputPath(PerfettoPluginConstants.SliceCookerPath, nameof(PerfettoSliceCooker.SliceEvents)));
            var argData = requiredData.QueryOutput<ProcessedEventData<PerfettoArgEvent>>(new DataOutputPath(PerfettoPluginConstants.ArgCookerPath, nameof(PerfettoArgCooker.ArgEvents)));
            var threadTrackData = requiredData.QueryOutput<ProcessedEventData<PerfettoThreadTrackEvent>>(new DataOutputPath(PerfettoPluginConstants.ThreadTrackCookerPath, nameof(PerfettoThreadTrackCooker.ThreadTrackEvents)));
            var threadData = requiredData.QueryOutput<ProcessedEventData<PerfettoThreadEvent>>(new DataOutputPath(PerfettoPluginConstants.ThreadCookerPath, nameof(PerfettoThreadCooker.ThreadEvents)));
            var processData = requiredData.QueryOutput<ProcessedEventData<PerfettoProcessEvent>>(new DataOutputPath(PerfettoPluginConstants.ProcessCookerPath, nameof(PerfettoProcessCooker.ProcessEvents)));

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

            foreach (var result in joined)
            {
                PerfettoGenericEvent ev = new PerfettoGenericEvent();
                ev.EventName = result.slice.Name;
                ev.Type = result.slice.Type;
                ev.Duration = result.slice.Duration;
                ev.Timestamp = result.slice.Timestamp;
                ev.Category = result.slice.Category;
                ev.ArgSetId = result.slice.ArgSetId;

                MaximumEventFieldCount = Math.Max(MaximumEventFieldCount, result.args.Count());

                ev.Process = string.Format($"{result.process.Name} {result.process.Pid}");
                ev.Thread = string.Format($"{result.thread.Name} {result.thread.Tid}");

                foreach (var arg in result.args)
                {
                    ev.FlatKeys.Add(arg.Flatkey);
                    ev.ArgKeys.Add(arg.ArgKey);
                    switch (arg.ValueType)
                    {
                        case "string":
                            ev.Values.Add(arg.StringValue);
                            break;
                        case "bool":
                        case "int":
                            ev.Values.Add(arg.IntValue.ToString());
                            break;
                        case "uint":
                        case "pointer":
                            ev.Values.Add(((uint)arg.IntValue).ToString());
                            break;
                        case "real":
                            ev.Values.Add(arg.RealValue.ToString());
                            break;
                        default:
                            throw new Exception("Unexpected value type");
                    }
                }
                this.GenericEvents.AddEvent(ev);
            }
            this.GenericEvents.FinalizeData();
        }
    }
}
