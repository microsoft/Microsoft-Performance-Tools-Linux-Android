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
    public sealed class PerfettoCpuFrequencyEventCooker : CookedDataReflector, ICompositeDataCookerDescriptor
    {
        public static readonly DataCookerPath DataCookerPath = PerfettoPluginConstants.CpuFrequencyEventCookerPath;

        public string Description => "CPU Frequency composite cooker";

        public DataCookerPath Path => DataCookerPath;

        // Declare all of the cookers that are used by this CompositeCooker.
        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new[]
        {
            PerfettoPluginConstants.CounterCookerPath,
            PerfettoPluginConstants.CpuCounterTrackCookerPath
        };

        [DataOutput]
        public ProcessedEventData<PerfettoCpuFrequencyEvent> CpuFrequencyEvents { get; }

        /// Frequency scaling related constants. See docs/data-sources/cpu-freq in the Perfetto repo
        // String constants that define the types of CpuCounterTrack events
        private const string CpuIdleString = "cpuidle"; // Cpu is transitioning in/out of idle
        private const string CpuFreqString = "cpufreq"; // Cpu is changing frequency
        // This value for a 'cpuidle' event indicates the CPU is going back to not-idle.
        private const long BackToNotIdleCode = 4294967295;

        public PerfettoCpuFrequencyEventCooker() : base(PerfettoPluginConstants.CpuFrequencyEventCookerPath)
        { 
            this.CpuFrequencyEvents =
                new ProcessedEventData<PerfettoCpuFrequencyEvent>();
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
                         where cpuCounterTrack.Name == CpuIdleString || cpuCounterTrack.Name == CpuFreqString
                         orderby counter.Timestamp ascending
                         select new { counter, cpuCounterTrack };

            // See the following Perfetto docs for their documentation: repo_root/docs/data-sources/cpu-freq

            // CPU frequency can change and the idle state can change independently of the CPU frequency.
            // Events are emitted every time a CPU state changes, whether it's an idle change or CPU frequency change
            // 'cpufreq' events indicate that the CPU is active and the frequency has changed from the previous frequency
            // 'cpuidle' events with a value of 0 indicate the CPU moving to idle
            // 'cpuidle' events with a value of 4294967295 indicate the CPU moving back to non-idle at the last specified frequency

            // Create events out of the joined results
            foreach (var cpuGroup in joined.GroupBy(x=>x.cpuCounterTrack.Cpu))
            {
                double lastFreq = 0;

                for(int i = 0; i < cpuGroup.Count(); i++)
                {
                    var result = cpuGroup.ElementAt(i);

                    var frequency = result.counter.FloatValue;
                    var name = result.cpuCounterTrack.Name;
                    var ts = result.counter.Timestamp;
                    bool isIdle = true;

                    // This means the CPU is going back to non-idle at the last frequency
                    if (result.counter.FloatValue == BackToNotIdleCode && name == CpuIdleString)
                    {
                        frequency = lastFreq;
                        isIdle = false;
                    }
                    // This means the CPU is non-idle at a new frequency
                    else if (frequency != 0 && name == CpuFreqString)
                    {
                        lastFreq = frequency;
                        isIdle = false;
                    }

                    long nextTs = ts;
                    if (i < cpuGroup.Count() - 1)
                    {
                        // Need to look ahead in the future at the next event to get the timestamp so that we can calculate the duration which
                        // is needed for WPA line graphs
                        nextTs = cpuGroup.ElementAt(i + 1).counter.Timestamp;
                    }
                    
                    PerfettoCpuFrequencyEvent ev = new PerfettoCpuFrequencyEvent
                    (
                        frequency,
                        result.cpuCounterTrack.Cpu,
                        //new Timestamp(result.counter.Timestamp),
                        new Timestamp(result.counter.RelativeTimestamp),
                        new TimestampDelta(nextTs - ts),
                        name,
                        isIdle
                    );
                    this.CpuFrequencyEvents.AddEvent(ev);
                }
            }
            this.CpuFrequencyEvents.FinalizeData();
        }
    }
}
