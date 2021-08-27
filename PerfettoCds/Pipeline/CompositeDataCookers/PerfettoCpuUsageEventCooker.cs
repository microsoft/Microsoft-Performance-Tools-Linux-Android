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
    /// Pulls data from multiple individual SQL tables and joins them to create a a CPU frequency event. CPU frequency events
    /// include the current CPU frequency each CPU is running at and whether or not the CPU is idle
    /// </summary>
    public sealed class PerfettoCpuUsageEventCooker : CookedDataReflector, ICompositeDataCookerDescriptor
    {
        public static readonly DataCookerPath DataCookerPath = PerfettoPluginConstants.CpuUsageEventCookerPath;

        public string Description => "CPU usage composite cooker";

        public DataCookerPath Path => DataCookerPath;

        // Declare all of the cookers that are used by this CompositeCooker.
        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new[]
        {
            PerfettoPluginConstants.CounterCookerPath,
            PerfettoPluginConstants.CpuCounterTrackCookerPath
        };

        [DataOutput]
        public ProcessedEventData<PerfettoCpuUsageEvent> CpuUsageEvents { get; }

        /// Frequency scaling related constants. See docs/data-sources/cpu-freq in the Perfetto repo
        // String constants that define the types of CpuCounterTrack events
        private const string CpuIdleString = "cpuidle"; // Cpu is transitioning in/out of idle
        private const string CpuFreqString = "cpufreq"; // Cpu is changing frequency
        // This value for a 'cpuidle' event indicates the CPU is going back to not-idle.
        private const long BackToNotIdleCode = 4294967295;

        public PerfettoCpuUsageEventCooker() : base(PerfettoPluginConstants.CpuUsageEventCookerPath)
        { 
            this.CpuUsageEvents =
                new ProcessedEventData<PerfettoCpuUsageEvent>();
        }

        public void OnDataAvailable(IDataExtensionRetrieval requiredData)
        {
            // Gather the data from all the SQL tables
            var counterData = requiredData.QueryOutput<ProcessedEventData<PerfettoCounterEvent>>(new DataOutputPath(PerfettoPluginConstants.CounterCookerPath, nameof(PerfettoCounterCooker.CounterEvents)));
            var cpuCounterTrackData = requiredData.QueryOutput<ProcessedEventData<PerfettoCpuCounterTrackEvent>>(new DataOutputPath(PerfettoPluginConstants.CpuCounterTrackCookerPath, nameof(PerfettoCpuCounterTrackCooker.CpuCounterTrackEvents)));

            // Join them all together
            // Counter table contains the frequency, timestamp
            // CpuCounterTrack contains the event type and CPU number
            // Event type is either cpuidle or cpufreq. See below for further explanation
            var joined = from counter in counterData
                         join cpuCounterTrack in cpuCounterTrackData on counter.TrackId equals cpuCounterTrack.Id
                         where cpuCounterTrack.Name.StartsWith("cpu.times")
                         orderby counter.Timestamp ascending
                         select new { counter, cpuCounterTrack };

            // See the following Perfetto docs for their documentation: repo_root/docs/data-sources/cpu-freq

            // CPU frequency can change and the idle state can change independently of the CPU frequency.
            // Events are emitted every time a CPU state changes, whether it's an idle change or CPU frequency change
            // 'cpufreq' events indicate that the CPU is active and the frequency has changed from the previous frequency
            // 'cpuidle' events with a value of 0 indicate the CPU moving to idle
            // 'cpuidle' events with a value of 4294967295 indicate the CPU moving back to non-idle at the last specified frequency


            foreach (var cpuGroup in joined.GroupBy(x => x.cpuCounterTrack.Cpu))
            {
                var timeGroups = cpuGroup.GroupBy(z => z.counter.RelativeTimestamp);
                PerfettoCpuUsageEvent? lastEvent = null;
                for (int i = 0; i < timeGroups.Count(); i++)
                {
                    var timeGroup = timeGroups.ElementAt(i);

                    long nextTs = timeGroup.Key;
                    if (i < timeGroups.Count() - 1)
                    {
                        // Need to look ahead in the future at the next event to get the timestamp so that we can calculate the duration which
                        // is needed for WPA line graphs
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
                        lastEvent = new PerfettoCpuUsageEvent
                        (
                            cpu, startTimestamp, duration, userNs, userNiceNs, systemModeNs, idleNs, ioWaitNs, irqNs, softIrqNs
                        );
                    }
                    else
                    {
                        var ev = new PerfettoCpuUsageEvent
                        (
                            cpu, startTimestamp, duration, userNs, userNiceNs, systemModeNs, idleNs, ioWaitNs, irqNs, softIrqNs, lastEvent.Value
                        );
                        this.CpuUsageEvents.AddEvent(ev);
                    }

                }
            }

            // Create events out of the joined results
            //foreach (var cpuGroup in joined.GroupBy(x=>x.cpuCounterTrack.Cpu))
            //{
            //    foreach (var nameGroup in cpuGroup.GroupBy(y => y.cpuCounterTrack.Name))
            //    {
            //        PerfettoCpuUsageEvent? lastEvent = null;

            //        for (int i = 0; i < nameGroup.Count(); i++)
            //        {
            //            var result = nameGroup.ElementAt(i);

            //            var value = result.counter.FloatValue;
            //            var name = result.cpuCounterTrack.Name;
            //            var ts = result.counter.Timestamp;
            //            double percentage = 0;

            //            long nextTs = ts;
            //            if (i < nameGroup.Count() - 1)
            //            {
            //                // Need to look ahead in the future at the next event to get the timestamp so that we can calculate the duration which
            //                // is needed for WPA line graphs
            //                nextTs = nameGroup.ElementAt(i + 1).counter.Timestamp;
            //            }

            //            if (lastEvent != null)
            //            {
            //                //percentage = (value - lastEvent.Value.Value) / (result.counter.RelativeTimestamp - lastEvent.Value.StartTimestamp.ToNanoseconds);
            //                percentage *= 100;
            //            }

            //            //PerfettoCpuUsageEvent ev = new PerfettoCpuUsageEvent
            //            //(
            //            //    value,
            //            //    percentage,
            //            //    result.cpuCounterTrack.Cpu,
            //            //    new Timestamp(result.counter.RelativeTimestamp),
            //            //    new TimestampDelta(nextTs - ts),
            //            //    name
            //            //);
            //            //lastEvent = ev;
            //            //this.CpuUsageEvents.AddEvent(ev);
            //        }
            //    }
            //}
            this.CpuUsageEvents.FinalizeData();
        }
    }
}
