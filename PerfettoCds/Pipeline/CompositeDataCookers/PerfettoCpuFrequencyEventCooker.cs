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
    /// Pulls data from multiple individual SQL tables and joins them to create a Generic Peretto event
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
            // TODO describe tables
            var joined = from counter in counterData
                         join cpuCounterTrack in cpuCounterTrackData on counter.TrackId equals cpuCounterTrack.Id
                         where cpuCounterTrack.Name == "cpuidle" || cpuCounterTrack.Name == "cpufreq"
                         orderby counter.Timestamp ascending
                         //group new { counter, cpuCounterTrack } by cpuCounterTrack.Cpu into test
                            select new { counter, cpuCounterTrack };

            int cnt = 0;
            // Create events out of the joined results
            foreach (var result in joined.GroupBy(x=>x.cpuCounterTrack.Cpu))
            {
                double lastFreq = 0;

                //foreach (var thing in result)
                for(int i = 0; i < result.Count(); i++)
                {
                    var thing = result.ElementAt(i);
                    var frequency = thing.counter.FloatValue;
                    var name = thing.cpuCounterTrack.Name;
                    var ts = thing.counter.Timestamp;
                    bool isIdle = true;

                    // TODO explain
                    if (thing.counter.FloatValue == 4294967295)
                    {
                        frequency = lastFreq;
                        isIdle = false;
                    }
                    else if (frequency != 0)
                    {
                        lastFreq = frequency;
                        isIdle = false;
                    }

                    long nextTs = ts;
                    if (i < result.Count() - 1)
                    {
                        nextTs = result.ElementAt(i + 1).counter.Timestamp;
                    }
                    
                    PerfettoCpuFrequencyEvent ev = new PerfettoCpuFrequencyEvent
                    (
                        frequency,
                        thing.cpuCounterTrack.Cpu,
                        new Timestamp(thing.counter.Timestamp),
                        new TimestampDelta(nextTs - ts),
                        name,
                        isIdle
                    );
                    this.CpuFrequencyEvents.AddEvent(ev);
                }
                //var frequency = result.counter.FloatValue;
                //var name = result.cpuCounterTrack.Name;
                //// TODO explain
                //if (result.counter.FloatValue == 4294967295)
                //{
                //    frequency = 0;
                //    name = name + "back to not-idle";
                //}
                //PerfettoCpuFrequencyEvent ev = new PerfettoCpuFrequencyEvent
                //(
                //    frequency,
                //    result.cpuCounterTrack.Cpu,
                //    new Timestamp(result.counter.Timestamp),
                //    name
                //);
                //this.CpuFrequencyEvents.AddEvent(ev);
            }
            this.CpuFrequencyEvents.FinalizeData();
        }
    }
}
