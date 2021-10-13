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
    /// Pulls data from multiple individual SQL tables and hjoins them to create a GPU counter event.
    /// </summary>
    public sealed class PerfettoGpuCountersEventCooker : CookedDataReflector, ICompositeDataCookerDescriptor
    {
        public static readonly DataCookerPath DataCookerPath = PerfettoPluginConstants.GpuCountersEventCookerPath;

        public string Description => "GPU counters composite cooker";

        public DataCookerPath Path => DataCookerPath;

        // Declare all of the cookers that are used by this CompositeCooker.
        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new[]
        {
            PerfettoPluginConstants.CounterCookerPath,
            PerfettoPluginConstants.GpuCounterTrackCookerPath
        };

        [DataOutput]
        public ProcessedEventData<PerfettoGpuCountersEvent> GpuCountersEvents { get; }

        public PerfettoGpuCountersEventCooker() : base(PerfettoPluginConstants.GpuCountersEventCookerPath)
        { 
            this.GpuCountersEvents =
                new ProcessedEventData<PerfettoGpuCountersEvent>();
        }

        public void OnDataAvailable(IDataExtensionRetrieval requiredData)
        {
            // Gather the data from all the SQL tables
            var counterData = requiredData.QueryOutput<ProcessedEventData<PerfettoCounterEvent>>(new DataOutputPath(PerfettoPluginConstants.CounterCookerPath, nameof(PerfettoCounterCooker.CounterEvents)));
            var gpuCounterTrackData = requiredData.QueryOutput<ProcessedEventData<PerfettoGpuCounterTrackEvent>>(new DataOutputPath(PerfettoPluginConstants.GpuCounterTrackCookerPath, nameof(PerfettoGpuCounterTrackCooker.GpuCounterTrackEvents)));

            // Join them all together
            // Counter table contains the timestamp and all counter values
            // GpuCounterTrack contains the counter name
            var joined = from counter in counterData
                         join gpuCounterTrack in gpuCounterTrackData on counter.TrackId equals gpuCounterTrack.Id
                         orderby counter.Timestamp ascending
                         select new { counter, gpuCounterTrack };

            // Create GPU Counter events for each type (name) at each time
            foreach (var nameGroup in joined.GroupBy(x => x.gpuCounterTrack.Name))
            {
                string name = nameGroup.Key;

                for(int i = 0; i < nameGroup.Count(); i++)
                {
                    var ele = nameGroup.ElementAt(i);
                    double val = ele.counter.FloatValue;
                    var ts = ele.counter.RelativeTimestamp;

                    long nextTs = ts;
                    if (i < nameGroup.Count() - 1)
                    {
                        // Need to look ahead in the future at the next event to get the timestamp so that we can calculate the duration
                        nextTs = nameGroup.ElementAt(i + 1).counter.RelativeTimestamp;
                    }

                    PerfettoGpuCountersEvent ev = new PerfettoGpuCountersEvent
                    (
                       name, 
                       val, 
                       new Timestamp(ts), 
                       new TimestampDelta(nextTs - ts)
                    );
                    this.GpuCountersEvents.AddEvent(ev);
                }
            }

            this.GpuCountersEvents.FinalizeData();
        }
    }
}
