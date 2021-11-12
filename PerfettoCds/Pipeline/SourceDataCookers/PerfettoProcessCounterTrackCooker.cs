// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Threading;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Extensibility.DataCooking.SourceDataCooking;
using Microsoft.Performance.SDK.Processing;
using PerfettoCds.Pipeline.Events;
using PerfettoProcessor;

namespace PerfettoCds.Pipeline.SourceDataCookers
{
    /// <summary>
    /// Cooks the data from the ProcessCounterTrack table in Perfetto traces
    /// </summary>
    public sealed class PerfettoProcessCounterTrackCooker : SourceDataCooker<PerfettoSqlEventKeyed, PerfettoSourceParser, string>
    {
        public override string Description => "Processes events from the process_counter_track Perfetto SQL table";

        //
        //  The data this cooker outputs. Tables or other cookers can query for this data
        //  via the SDK runtime
        //
        [DataOutput]
        public ProcessedEventData<PerfettoProcessCounterTrackEvent> ProcessCounterTrackEvents { get; }

        // Instructs runtime to only send events with the given keys this data cooker
        public override ReadOnlyHashSet<string> DataKeys =>
            new ReadOnlyHashSet<string>(new HashSet<string> { PerfettoPluginConstants.ProcessCounterTrackEvent });


        public PerfettoProcessCounterTrackCooker() : base(PerfettoPluginConstants.ProcessCounterTrackCookerPath)
        {
            this.ProcessCounterTrackEvents =
                new ProcessedEventData<PerfettoProcessCounterTrackEvent>();
        }

        public override DataProcessingResult CookDataElement(PerfettoSqlEventKeyed perfettoEvent, PerfettoSourceParser context, CancellationToken cancellationToken)
        {
            this.ProcessCounterTrackEvents.AddEvent((PerfettoProcessCounterTrackEvent)perfettoEvent.SqlEvent);

            return DataProcessingResult.Processed;
        }

        public override void EndDataCooking(CancellationToken cancellationToken)
        {
            base.EndDataCooking(cancellationToken);
            this.ProcessCounterTrackEvents.FinalizeData();
        }
    }
}
