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
    /// Cooks the data from the Process table in Perfetto traces
    /// </summary>
    public sealed class PerfettoProcessRawCooker : SourceDataCooker<PerfettoSqlEventKeyed, PerfettoSourceParser, string>
    {
        public override string Description => "Processes events from the process Perfetto SQL table";

        //
        //  The data this cooker outputs. Tables or other cookers can query for this data
        //  via the SDK runtime
        //
        [DataOutput]
        public ProcessedEventData<PerfettoProcessRawEvent> ProcessEvents { get; }

        // Instructs runtime to only send events with the given keys this data cooker
        public override ReadOnlyHashSet<string> DataKeys =>
            new ReadOnlyHashSet<string>(new HashSet<string> { PerfettoPluginConstants.ProcessEventRaw });


        public PerfettoProcessRawCooker() : base(PerfettoPluginConstants.ProcessRawCookerPath)
        {
            this.ProcessEvents = new ProcessedEventData<PerfettoProcessRawEvent>();
        }

        public override DataProcessingResult CookDataElement(PerfettoSqlEventKeyed perfettoEvent, PerfettoSourceParser context, CancellationToken cancellationToken)
        {
            var newEvent = (PerfettoProcessRawEvent)perfettoEvent.SqlEvent;
            newEvent.RelativeStartTimestamp = newEvent.StartTimestamp - context.FirstEventTimestamp.ToNanoseconds;
            newEvent.RelativeEndTimestamp = newEvent.EndTimestamp.HasValue ?
                                                newEvent.EndTimestamp - context.FirstEventTimestamp.ToNanoseconds :
                                                (context.LastEventTimestamp - context.FirstEventTimestamp).ToNanoseconds;
            this.ProcessEvents.AddEvent(newEvent);

            return DataProcessingResult.Processed;
        }

        public override void EndDataCooking(CancellationToken cancellationToken)
        {
            base.EndDataCooking(cancellationToken);
            this.ProcessEvents.FinalizeData();
        }
    }
}
