﻿// Copyright (c) Microsoft Corporation.
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
    /// Cooks the data from the stack_profile_frame table in Perfetto traces
    /// </summary>
    public sealed class PerfettoStackProfileFrameCooker : SourceDataCooker<PerfettoSqlEventKeyed, PerfettoSourceParser, string>
    {
        public override string Description => "Processes events from the stack_profile_frame Perfetto SQL table";

        //
        //  The data this cooker outputs. Tables or other cookers can query for this data
        //  via the SDK runtime
        //
        [DataOutput]
        public ProcessedEventData<PerfettoStackProfileFrameEvent> StackProfileFrameEvents { get; }

        // Instructs runtime to only send events with the given keys this data cooker
        public override ReadOnlyHashSet<string> DataKeys =>
            new ReadOnlyHashSet<string>(new HashSet<string> { PerfettoPluginConstants.StackProfileFrameEvent });


        public PerfettoStackProfileFrameCooker() : base(PerfettoPluginConstants.StackProfileFrameCookerPath)
        {
            this.StackProfileFrameEvents = new ProcessedEventData<PerfettoStackProfileFrameEvent>();
        }

        public override DataProcessingResult CookDataElement(PerfettoSqlEventKeyed perfettoEvent, PerfettoSourceParser context, CancellationToken cancellationToken)
        {
            var newEvent = (PerfettoStackProfileFrameEvent)perfettoEvent.SqlEvent;
            this.StackProfileFrameEvents.AddEvent(newEvent);

            return DataProcessingResult.Processed;
        }

        public override void EndDataCooking(CancellationToken cancellationToken)
        {
            base.EndDataCooking(cancellationToken);
            this.StackProfileFrameEvents.FinalizeData();
        }
    }
}
