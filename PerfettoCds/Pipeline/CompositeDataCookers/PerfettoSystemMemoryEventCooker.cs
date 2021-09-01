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
    /// Pulls data from multiple individual SQL tables and joins them to create a system memory event. System
    /// memory events capture period system memory counts
    /// </summary>
    public sealed class PerfettoSystemMemoryEventCooker : CookedDataReflector, ICompositeDataCookerDescriptor
    {
        public static readonly DataCookerPath DataCookerPath = PerfettoPluginConstants.SystemMemoryEventCookerPath;

        public string Description => "System memory composite cooker";

        public DataCookerPath Path => DataCookerPath;

        // Declare all of the cookers that are used by this CompositeCooker.
        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new[]
        {
            PerfettoPluginConstants.CounterCookerPath,
            PerfettoPluginConstants.CounterTrackCookerPath
        };

        [DataOutput]
        public ProcessedEventData<PerfettoSystemMemoryEvent> SystemMemoryEvents { get; }

        // Perfetto captures memory counts from /proc/meminfo and outputs events with
        // the following names.
        // Set sys_stats_counters.h in Perfetto repo.
        public HashSet<string> MemoryTypes = new HashSet<string>() {
            "MemUnspecified",
            "MemTotal",
            "MemFree",
            "MemAvailable",
            "Buffers",
            "Cached",
            "SwapCached",
            "Active",
            "Inactive",
            "Active(anon)",
            "Inactive(anon)",
            "Active(file)",
            "Inactive(file)",
            "Unevictable",
            "Mlocked",
            "SwapTotal",
            "SwapFree",
            "Dirty",
            "Writeback",
            "AnonPages",
            "Mapped",
            "Shmem",
            "Slab",
            "SReclaimable",
            "SUnreclaim",
            "KernelStack",
            "PageTables",
            "CommitLimit",
            "Committed_AS",
            "VmallocTotal",
            "VmallocUsed",
            "VmallocChunk",
            "CmaTotal",
            "CmaFree"
        };

        public PerfettoSystemMemoryEventCooker() : base(PerfettoPluginConstants.SystemMemoryEventCookerPath)
        { 
            this.SystemMemoryEvents =
                new ProcessedEventData<PerfettoSystemMemoryEvent>();
        }

        public void OnDataAvailable(IDataExtensionRetrieval requiredData)
        {
            // Gather the data from all the SQL tables
            var counterData = requiredData.QueryOutput<ProcessedEventData<PerfettoCounterEvent>>(new DataOutputPath(PerfettoPluginConstants.CounterCookerPath, nameof(PerfettoCounterCooker.CounterEvents)));
            var counterTrackData = requiredData.QueryOutput<ProcessedEventData<PerfettoCounterTrackEvent>>(new DataOutputPath(PerfettoPluginConstants.CounterTrackCookerPath, nameof(PerfettoCounterTrackCooker.CounterTrackEvents)));

            // Join them all together
            // Counter table contains the memory count value, timestamp
            // counterTrackData contains the name of the memory type
            // Process contains the process name
            var joined = from counter in counterData
                         join counterTrack in counterTrackData on counter.TrackId equals counterTrack.Id
                         where MemoryTypes.Contains(counterTrack.Name)
                         orderby counter.Timestamp ascending
                         select new { counter, counterTrack };

            // Create events out of the joined results
            foreach (var memoryGroup in joined.GroupBy(x => x.counterTrack.Name))
            {
                string memoryType = memoryGroup.Key;

                for(int i = 0; i < memoryGroup.Count(); i++)
                {
                    var thing = memoryGroup.ElementAt(i);
                    double val = thing.counter.FloatValue;
                    var ts = thing.counter.RelativeTimestamp;

                    long nextTs = ts;
                    if (i < memoryGroup.Count() - 1)
                    {
                        // Need to look ahead in the future at the next event to get the timestamp so that we can calculate the duration which
                        // is needed for WPA line graphs
                        nextTs = memoryGroup.ElementAt(i + 1).counter.RelativeTimestamp;
                    }

                    PerfettoSystemMemoryEvent ev = new PerfettoSystemMemoryEvent
                    (
                        val, 
                        memoryType,
                        new Timestamp(ts),
                        new TimestampDelta(nextTs - ts)
                    );
                    this.SystemMemoryEvents.AddEvent(ev);
                }

            }
            this.SystemMemoryEvents.FinalizeData();
        }


    }
}
