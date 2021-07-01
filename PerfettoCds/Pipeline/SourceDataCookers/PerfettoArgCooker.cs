using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Extensibility.DataCooking.SourceDataCooking;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using PerfettoCds.Pipeline.Events;

namespace PerfettoCds
{
    public sealed class PerfettoArgCooker : BaseSourceDataCooker<PerfettoSqlEvent, PerfettoSourceParser, string>
    {
        public override string Description => "Processes events from the args Perfetto SQL table";

        //
        //  The data this cooker outputs. Tables or other cookers can query for this data
        //  via the SDK runtime
        //
        [DataOutput]
        public ProcessedEventData<PerfettoArgEvent> ArgEvents { get; }

        // Instructs runtime to only send events with the given keys this data cooker
        public override ReadOnlyHashSet<string> DataKeys =>
            new ReadOnlyHashSet<string>(new HashSet<string> { PerfettoPluginConstants.ArgEvent });

        public PerfettoArgCooker() : base(PerfettoPluginConstants.ArgCookerPath)
        {
            this.ArgEvents = new ProcessedEventData<PerfettoArgEvent>();
        }

        public override DataProcessingResult CookDataElement(PerfettoSqlEvent perfettoEvent, PerfettoSourceParser context, CancellationToken cancellationToken)
        {
            this.ArgEvents.AddEvent((PerfettoArgEvent)perfettoEvent);

            return DataProcessingResult.Processed;
        }

        public override void EndDataCooking(CancellationToken cancellationToken)
        {
            base.EndDataCooking(cancellationToken);
            this.ArgEvents.FinalizeData();
        }
    }
}
