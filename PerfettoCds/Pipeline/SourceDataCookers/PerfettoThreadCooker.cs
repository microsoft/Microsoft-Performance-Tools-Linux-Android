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
    /// Cooks the data from the Thread table in Perfetto traces
    /// </summary>
    public sealed class PerfettoThreadCooker : SourceDataCooker<PerfettoSqlEventKeyed, PerfettoSourceParser, string>
    {
        public override string Description => "Processes events from the thread Perfetto SQL table";

        //
        //  The data this cooker outputs. Tables or other cookers can query for this data
        //  via the SDK runtime
        //
        [DataOutput]
        public ProcessedEventData<PerfettoThreadEvent> ThreadEvents { get; }

        // Instructs runtime to only send events with the given keys this data cooker
        public override ReadOnlyHashSet<string> DataKeys =>
            new ReadOnlyHashSet<string>(new HashSet<string> { PerfettoPluginConstants.ThreadEvent });


        public PerfettoThreadCooker() : base(PerfettoPluginConstants.ThreadCookerPath)
        {
            this.ThreadEvents = new ProcessedEventData<PerfettoThreadEvent>();
        }

        public override DataProcessingResult CookDataElement(PerfettoSqlEventKeyed perfettoEvent, PerfettoSourceParser context, CancellationToken cancellationToken)
        {
            var newEvent = (PerfettoThreadEvent)perfettoEvent.SqlEvent;
            newEvent.RelativeStartTimestamp = newEvent.StartTimestamp - context.FirstEventTimestamp.ToNanoseconds;
            newEvent.RelativeEndTimestamp = newEvent.EndTimestamp - context.FirstEventTimestamp.ToNanoseconds;
            this.ThreadEvents.AddEvent(newEvent);

            return DataProcessingResult.Processed;
        }

        public override void EndDataCooking(CancellationToken cancellationToken)
        {
            base.EndDataCooking(cancellationToken);
            this.ThreadEvents.FinalizeData();
        }
    }
}
