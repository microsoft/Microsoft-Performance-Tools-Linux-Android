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
    /// Pulls data from multiple individual SQL tables and joins them to create a process memory event. Process
    /// memory events list different memory counts per process
    /// </summary>
    public sealed class PerfettoProcessMemoryEventCooker : CookedDataReflector, ICompositeDataCookerDescriptor
    {
        public static readonly DataCookerPath DataCookerPath = PerfettoPluginConstants.ProcessMemoryEventCookerPath;

        public string Description => "Process memory composite cooker";

        public DataCookerPath Path => DataCookerPath;

        // Declare all of the cookers that are used by this CompositeCooker.
        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new[]
        {
            PerfettoPluginConstants.CounterCookerPath,
            PerfettoPluginConstants.ProcessCounterTrackCookerPath,
            PerfettoPluginConstants.ProcessCookerPath
        };

        [DataOutput]
        public ProcessedEventData<PerfettoProcessMemoryEvent> ProcessMemoryEvents { get; }

        public PerfettoProcessMemoryEventCooker() : base(PerfettoPluginConstants.ProcessMemoryEventCookerPath)
        { 
            this.ProcessMemoryEvents =
                new ProcessedEventData<PerfettoProcessMemoryEvent>();
        }

        public void OnDataAvailable(IDataExtensionRetrieval requiredData)
        {
            // Gather the data from all the SQL tables
            var counterData = requiredData.QueryOutput<ProcessedEventData<PerfettoCounterEvent>>(new DataOutputPath(PerfettoPluginConstants.CounterCookerPath, nameof(PerfettoCounterCooker.CounterEvents)));
            var processCounterTrackData = requiredData.QueryOutput<ProcessedEventData<PerfettoProcessCounterTrackEvent>>(new DataOutputPath(PerfettoPluginConstants.ProcessCounterTrackCookerPath, nameof(PerfettoProcessCounterTrackCooker.ProcessCounterTrackEvents)));
            var processData = requiredData.QueryOutput<ProcessedEventData<PerfettoProcessEvent>>(new DataOutputPath(PerfettoPluginConstants.ProcessCookerPath, nameof(PerfettoProcessCooker.ProcessEvents)));

            // Join them all together
            // Counter table contains the memory count value, timestamp
            // ProcessCounterTrack contains the UPID and memory type name. All the memory types we care about start with "mem."
            // Process contains the process name
            var joined = from counter in counterData
                         join processCounterTrack in processCounterTrackData on counter.TrackId equals processCounterTrack.Id
                         join process in processData on processCounterTrack.Upid equals process.Upid
                         where processCounterTrack.Name.StartsWith("mem.")
                         orderby counter.Timestamp ascending
                         select new { counter, processCounterTrack, process };

            // Create events out of the joined results
            foreach (var processGroup in joined.GroupBy(x => x.processCounterTrack.Upid))
            {
                var timeGroups = processGroup.GroupBy(z => z.counter.RelativeTimestamp);

                for (int i = 0; i < timeGroups.Count(); i++)
                {
                    var timeGroup = timeGroups.ElementAt(i);

                    var ts = timeGroup.Key;
                    var processName = $"{timeGroup.ElementAt(0).process.Name} {processGroup.Key}";
                    long nextTs = ts;
                    if (i < timeGroups.Count() - 1)
                    {
                        // Need to look ahead in the future at the next event to get the timestamp so that we can calculate the duration
                        nextTs = timeGroups.ElementAt(i + 1).Key;
                    }

                    double virt = 0.0, rss = 0.0, rssAnon = 0.0, rssFile = 0.0, rssShMem = 0.0, rssHwm = 0.0, swap = 0.0, locked = 0.0;
                    // Gather each type of memory
                    foreach (var thing in timeGroup)
                    {
                        switch (thing.processCounterTrack.Name)
                        {
                            case "mem.virt":
                                virt = thing.counter.FloatValue;
                                break;
                            case "mem.rss":
                                rss = thing.counter.FloatValue;
                                break;
                            case "mem.rss.anon":
                                rssAnon = thing.counter.FloatValue;
                                break;
                            case "mem.rss.file":
                                rssFile = thing.counter.FloatValue;
                                break;
                            case "mem.rss.shmem":
                                rssShMem = thing.counter.FloatValue;
                                break;
                            case "mem.rss.watermark":
                                rssHwm = thing.counter.FloatValue;
                                break;
                            case "mem.locked":
                                locked = thing.counter.FloatValue;
                                break;
                            case "mem.swap":
                                swap = thing.counter.FloatValue;
                                break;
                        }
                    }
                    PerfettoProcessMemoryEvent ev = new PerfettoProcessMemoryEvent
                    (
                        processName,
                        new Timestamp(ts),
                        new TimestampDelta(nextTs - ts),
                        rssAnon,
                        rssShMem,
                        rssFile,
                        rssHwm,
                        rss,
                        locked,
                        swap,
                        virt
                    );
                    this.ProcessMemoryEvents.AddEvent(ev);
                }
            }
            this.ProcessMemoryEvents.FinalizeData();
        }
    }
}
