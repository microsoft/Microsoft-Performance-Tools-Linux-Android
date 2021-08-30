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
    /// Pulls data from multiple individual SQL tables and joins them to create a CPU counters event.
    /// CPU counters events include multiple CPU counters polled throughout the trace from  the /proc/stat file.
    /// Percent use values are calculated by comparing the difference in the counter between 2 adjacent events:
    /// time2.counterAPercent = (time2.counterA - time1.counterA) / (time2 - time1) * 100
    /// </summary>
    public sealed class PerfettoCpuCountersEventCooker : CookedDataReflector, ICompositeDataCookerDescriptor
    {
        public static readonly DataCookerPath DataCookerPath = PerfettoPluginConstants.CpuCountersEventCookerPath;

        public string Description => "CPU counters composite cooker";

        public DataCookerPath Path => DataCookerPath;

        // Declare all of the cookers that are used by this CompositeCooker.
        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new[]
        {
            PerfettoPluginConstants.CounterCookerPath,
            PerfettoPluginConstants.CpuCounterTrackCookerPath
        };

        [DataOutput]
        public ProcessedEventData<PerfettoCpuCountersEvent> CpuCountersEvents { get; }

        public PerfettoCpuCountersEventCooker() : base(PerfettoPluginConstants.CpuCountersEventCookerPath)
        { 
            this.CpuCountersEvents =
                new ProcessedEventData<PerfettoCpuCountersEvent>();
        }

        public void OnDataAvailable(IDataExtensionRetrieval requiredData)
        {
            // Gather the data from all the SQL tables
            var counterData = requiredData.QueryOutput<ProcessedEventData<PerfettoCounterEvent>>(new DataOutputPath(PerfettoPluginConstants.CounterCookerPath, nameof(PerfettoCounterCooker.CounterEvents)));
            var cpuCounterTrackData = requiredData.QueryOutput<ProcessedEventData<PerfettoCpuCounterTrackEvent>>(new DataOutputPath(PerfettoPluginConstants.CpuCounterTrackCookerPath, nameof(PerfettoCpuCounterTrackCooker.CpuCounterTrackEvents)));

            // Join them all together
            // Counter table contains the timestamp and all /proc/stat counters
            // CpuCounterTrack contains the counter name and CPU core
            var joined = from counter in counterData
                         join cpuCounterTrack in cpuCounterTrackData on counter.TrackId equals cpuCounterTrack.Id
                         where cpuCounterTrack.Name.StartsWith("cpu.times")
                         orderby counter.Timestamp ascending
                         select new { counter, cpuCounterTrack };

            // 7 different CPU counters are polled at regular intervals through the trace, for each CPU core. 
            // The names and cores are stored in the cpu_counter_track table and the actual counter values are stored
            // in the counter table
            // 
            // We will create one PerfettoCpuCountersEvent for each time grouping that contains all 7 counter values

            foreach (var cpuGroup in joined.GroupBy(x => x.cpuCounterTrack.Cpu))
            {
                var timeGroups = cpuGroup.GroupBy(z => z.counter.RelativeTimestamp);
                PerfettoCpuCountersEvent? lastEvent = null;
                for (int i = 0; i < timeGroups.Count(); i++)
                {
                    var timeGroup = timeGroups.ElementAt(i);

                    long nextTs = timeGroup.Key;
                    if (i < timeGroups.Count() - 1)
                    {
                        // Need to look ahead in the future at the next event to get the timestamp so that we can calculate the duration
                        nextTs = timeGroups.ElementAt(i + 1).Key;
                    }

                    var cpu = cpuGroup.Key;
                    var startTimestamp = new Timestamp(timeGroup.Key);
                    var duration = new TimestampDelta(nextTs - timeGroup.Key);
                    double userNs = 0.0;
                    double userNiceNs = 0.0;
                    double systemModeNs = 0.0;
                    double idleNs = 0.0;
                    double ioWaitNs = 0.0;
                    double irqNs = 0.0;
                    double softIrqNs = 0.0;

                    foreach (var nameGroup in timeGroup.GroupBy(y => y.cpuCounterTrack.Name))
                    {
                        switch (nameGroup.Key)
                        {
                            case "cpu.times.user_ns":
                                userNs = nameGroup.ElementAt(0).counter.FloatValue;
                                break;
                            case "cpu.times.user_nice_ns":
                                userNiceNs = nameGroup.ElementAt(0).counter.FloatValue;
                                break;
                            case "cpu.times.system_mode_ns":
                                systemModeNs = nameGroup.ElementAt(0).counter.FloatValue;
                                break;
                            case "cpu.times.idle_ns":
                                idleNs = nameGroup.ElementAt(0).counter.FloatValue;
                                break;
                            case "cpu.times.io_wait_ns":
                                ioWaitNs = nameGroup.ElementAt(0).counter.FloatValue;
                                break;
                            case "cpu.times.irq_ns":
                                irqNs = nameGroup.ElementAt(0).counter.FloatValue;
                                break;
                            case "cpu.times.softirq_ns":
                                softIrqNs = nameGroup.ElementAt(0).counter.FloatValue;
                                break;
                        }
                    }

                    if (lastEvent == null)
                    {
                        // Can't determine % change from the first event because we don't have the previous event to compare to.
                        // Don't graph this event
                        lastEvent = new PerfettoCpuCountersEvent
                        (
                            cpu, startTimestamp, duration, userNs, userNiceNs, systemModeNs, idleNs, ioWaitNs, irqNs, softIrqNs
                        );
                    }
                    else
                    {
                        var ev = new PerfettoCpuCountersEvent
                        (
                            cpu, startTimestamp, duration, userNs, userNiceNs, systemModeNs, idleNs, ioWaitNs, irqNs, softIrqNs, lastEvent.Value
                        );
                        lastEvent = ev;
                        this.CpuCountersEvents.AddEvent(ev);
                    }

                }
            }

            this.CpuCountersEvents.FinalizeData();
        }
    }
}
