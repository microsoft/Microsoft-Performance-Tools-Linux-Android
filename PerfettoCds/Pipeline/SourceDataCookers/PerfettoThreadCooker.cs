﻿using Microsoft.Performance.SDK;
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
    public sealed class PerfettoThreadCooker : BaseSourceDataCooker<PerfettoSqlEvent, PerfettoSourceParser, string>
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

        public override DataProcessingResult CookDataElement(PerfettoSqlEvent perfettoEvent, PerfettoSourceParser context, CancellationToken cancellationToken)
        {
            this.ThreadEvents.AddEvent((PerfettoThreadEvent)perfettoEvent);

            return DataProcessingResult.Processed;
        }

        public override void EndDataCooking(CancellationToken cancellationToken)
        {
            base.EndDataCooking(cancellationToken);
            this.ThreadEvents.FinalizeData();
        }
    }
}
