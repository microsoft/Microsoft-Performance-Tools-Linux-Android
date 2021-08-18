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

namespace PerfettoCds
{
    /// <summary>
    /// Cooks the data from the Counter table in Perfetto traces
    /// </summary>
    public sealed class PerfettoCounterCooker : BaseSourceDataCooker<PerfettoSqlEventKeyed, PerfettoSourceParser, string>
    {
        public override string Description => "Processes events from the counter Perfetto SQL table";

        //
        //  The data this cooker outputs. Tables or other cookers can query for this data
        //  via the SDK runtime
        //
        [DataOutput]
        public ProcessedEventData<PerfettoCounterEvent> CounterEvents { get; }

        // Instructs runtime to only send events with the given keys this data cooker
        public override ReadOnlyHashSet<string> DataKeys =>
            new ReadOnlyHashSet<string>(new HashSet<string> { PerfettoPluginConstants.CounterEvent });

        public PerfettoCounterCooker() : base(PerfettoPluginConstants.CounterCookerPath)
        {
            this.CounterEvents = new ProcessedEventData<PerfettoCounterEvent>();
        }

        public override DataProcessingResult CookDataElement(PerfettoSqlEventKeyed perfettoEvent, PerfettoSourceParser context, CancellationToken cancellationToken)
        {
            //this.CounterEvents.AddEvent((PerfettoCounterEvent)perfettoEvent.SqlEvent);
            var newEvent = (PerfettoCounterEvent)perfettoEvent.SqlEvent;
            newEvent.RelativeTimestamp = newEvent.Timestamp - context.FirstEventTimestamp.ToNanoseconds;
            this.CounterEvents.AddEvent(newEvent);

            return DataProcessingResult.Processed;
        }

        public override void EndDataCooking(CancellationToken cancellationToken)
        {
            base.EndDataCooking(cancellationToken);
            this.CounterEvents.FinalizeData();
        }
    }
}
