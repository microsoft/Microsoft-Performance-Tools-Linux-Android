// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Extensibility.DataCooking.SourceDataCooking;
using Microsoft.Performance.SDK.Processing;
using PerfettoCds.Pipeline.Events;
using PerfettoProcessor;
using System.Collections.Generic;
using System.Threading;

namespace PerfettoCds.Pipeline.SourceDataCookers
{
    /// <summary>
    /// Cooks the data from the Android_Logs table in Perfetto traces
    /// </summary>
    public sealed class PerfettoAndroidLogCooker : SourceDataCooker<PerfettoSqlEventKeyed, PerfettoSourceParser, string>
    {
        public override string Description => "Processes events from the Android_Logs Perfetto SQL table";

        //
        //  The data this cooker outputs. Tables or other cookers can query for this data
        //  via the SDK runtime
        //
        [DataOutput]
        public ProcessedEventData<PerfettoAndroidLogEvent> AndroidLogEvents { get; }

        // Instructs runtime to only send events with the given keys this data cooker
        public override ReadOnlyHashSet<string> DataKeys =>
            new ReadOnlyHashSet<string>(new HashSet<string> { PerfettoPluginConstants.AndroidLogEvent });


        public PerfettoAndroidLogCooker() : base(PerfettoPluginConstants.AndroidLogCookerPath)
        {
            this.AndroidLogEvents = new ProcessedEventData<PerfettoAndroidLogEvent>();
        }

        public override DataProcessingResult CookDataElement(PerfettoSqlEventKeyed perfettoEvent, PerfettoSourceParser context, CancellationToken cancellationToken)
        {
            var newEvent = (PerfettoAndroidLogEvent)perfettoEvent.SqlEvent;
            newEvent.RelativeTimestamp = newEvent.Timestamp - context.FirstEventTimestamp.ToNanoseconds;
            this.AndroidLogEvents.AddEvent(newEvent);

            return DataProcessingResult.Processed;
        }

        public override void EndDataCooking(CancellationToken cancellationToken)
        {
            base.EndDataCooking(cancellationToken);
            this.AndroidLogEvents.FinalizeData();
        }
    }
}
