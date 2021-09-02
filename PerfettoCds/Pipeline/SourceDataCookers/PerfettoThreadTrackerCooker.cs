// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Extensibility.DataCooking.SourceDataCooking;
using Microsoft.Performance.SDK.Processing;
using System.Collections.Generic;
using System.Threading;
using PerfettoCds.Pipeline.Events;
using PerfettoProcessor;

namespace PerfettoCds.Pipeline.SourceDataCookers
{
    /// <summary>
    /// Cooks the data from the ThreadTrack table in Perfetto traces
    /// </summary>
    public sealed class PerfettoThreadTrackCooker : BaseSourceDataCooker<PerfettoSqlEventKeyed, PerfettoSourceParser, string>
    {
        public override string Description => "Processes events from the thread_tracks Perfetto SQL table";

        //
        //  The data this cooker outputs. Tables or other cookers can query for this data
        //  via the SDK runtime
        //
        [DataOutput]
        public ProcessedEventData<PerfettoThreadTrackEvent> ThreadTrackEvents { get; }

        // Instructs runtime to only send events with the given keys this data cooker
        public override ReadOnlyHashSet<string> DataKeys =>
            new ReadOnlyHashSet<string>(new HashSet<string> { PerfettoPluginConstants.ThreadTrackEvent });


        public PerfettoThreadTrackCooker() : base(PerfettoPluginConstants.ThreadTrackCookerPath)
        {
            this.ThreadTrackEvents =
                new ProcessedEventData<PerfettoThreadTrackEvent>();
        }

        public override DataProcessingResult CookDataElement(PerfettoSqlEventKeyed perfettoEvent, PerfettoSourceParser context, CancellationToken cancellationToken)
        {
            this.ThreadTrackEvents.AddEvent((PerfettoThreadTrackEvent)perfettoEvent.SqlEvent);

            return DataProcessingResult.Processed;
        }

        public override void EndDataCooking(CancellationToken cancellationToken)
        {
            base.EndDataCooking(cancellationToken);
            this.ThreadTrackEvents.FinalizeData();
        }
    }
}
