// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using DotNetEventPipe.DataOutputTypes;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Processing;
using Utilities;
using Utilities.AccessProviders;
using static Utilities.TimeHelper;

namespace DotNetEventPipe.Tables
{
    //
    // Have the MetadataTable inherit the TableBase class
    //
    [Table]              // A category is optional. It useful for grouping different types of tables
    public sealed class GCTable
        : TraceEventTableBase
    {
        public static readonly TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{9D91E453-8ADD-4650-8C91-77E58B42BBBA}"),
            "GC",
            "Garbage Collector",
            category: ".NET trace (dotnet-trace)");

        public GCTable(IReadOnlyDictionary<string, TraceEventProcessor> traceEventProcessor)
            : base(traceEventProcessor)
        {
        }

        private static readonly ColumnConfiguration countColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{847D85A8-77CB-4E4F-B664-4FAEF6394707}"), "Count", "Count"),
            new UIHints
            {
                Width = 130,
                AggregationMode = AggregationMode.Sum,
                SortOrder = SortOrder.Descending,
                SortPriority = 0,
            });

        private static readonly ColumnConfiguration gcReasonColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{39BEA7E7-C543-46FD-9FCF-6A7C249D35EC}"), "Reason", "GC Reason"),
            new UIHints { Width = 125 });

        private static readonly ColumnConfiguration depthColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{CB84FD81-24EF-49AA-91B0-4BB2D00BBE0E}"), "Depth", "Depth"),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration gcTypeColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{096124B1-F46D-4204-8C0A-D4DA65ACEBDA}"), "Type", "GC Type"),
            new UIHints { Width = 160 });

        private static readonly ColumnConfiguration clrInstanceIDColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{FFE7E4B2-0636-403B-A2F6-0B6D696C18FC}"), "ClrInstanceID", "Clr Instance ID"),
            new UIHints { Width = 115 });

        private static readonly ColumnConfiguration clientSequenceNumberColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{105C1056-EC3B-4980-A36E-D0CBE1E4A479}"), "ClientSequenceNumber", "Client Sequence Number"),
            new UIHints { Width = 145 });

        private static readonly ColumnConfiguration timestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{00746811-C2AE-48B8-AF00-1213AFFB4D6F}"), "Timestamp", "The timestamp of the event"),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration durationColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{2EFD88CE-0559-46D7-8E1A-43F5B2EDC482}"), "Duration", "Duration of the GC"),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration threadIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{0CDD7D87-FDC3-457D-87CD-692EA4280664}"), "ThreadId"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration processColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{B3BA7352-86CC-4271-A136-04CA071D19C5}"), "Process"),
                new UIHints { Width = 100, });

        private static readonly ColumnConfiguration processIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{7F13B9C1-7FD5-417C-B768-637D7BF0CA55}"), "Process Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration cpuColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{5C9EAC74-23E2-4450-9CC0-65C516AC1422}"), "CPU"),
                new UIHints { Width = 80, });


        public override void Build(ITableBuilder tableBuilder)
        {
            if (TraceEventProcessor == null || TraceEventProcessor.Count == 0)
            {
                return;
            }

            var firstTraceProcessorEventsParsed = TraceEventProcessor.First().Value;  // First Log
            var gcStartEvents = firstTraceProcessorEventsParsed.GenericEvents.Where(f => f.ProviderName == "Microsoft-Windows-DotNETRuntime" && 
                                                                                   f.EventName == "GC/Start").ToArray();
            var gcStopEvents = firstTraceProcessorEventsParsed.GenericEvents.Where(f => f.ProviderName == "Microsoft-Windows-DotNETRuntime" &&
                                                                                   f.EventName == "GC/Stop").OrderBy(f => f.Timestamp).ToArray();

            var tableGenerator = tableBuilder.SetRowCount(gcStartEvents.Length);
            var baseProjection = Projection.Index(gcStartEvents);

            var maximumFieldCount = 0;
            foreach (var genericEvent in gcStartEvents)
            {
                maximumFieldCount = Math.Max(maximumFieldCount, genericEvent.PayloadValues.Length);
            }

            tableGenerator.AddColumn(countColumn, baseProjection.Compose(x => x.PayloadValues.Length >= 1 ? (int)x.PayloadValues[0] : 0));
            tableGenerator.AddColumn(gcReasonColumn, baseProjection.Compose(x => x.PayloadValues.Length >= 2 ? (GCReason)x.PayloadValues[1] : GCReason.AllocSmall));
            tableGenerator.AddColumn(depthColumn, baseProjection.Compose(x => x.PayloadValues.Length >= 3 ? (int)x.PayloadValues[2] : 0));
            tableGenerator.AddColumn(gcTypeColumn, baseProjection.Compose(x => x.PayloadValues.Length >= 4 ? (GCType)x.PayloadValues[3] : 0));
            tableGenerator.AddColumn(clrInstanceIDColumn, baseProjection.Compose(x => x.PayloadValues.Length >= 5 ? (int)x.PayloadValues[4] : 0));
            tableGenerator.AddColumn(clientSequenceNumberColumn, baseProjection.Compose(x => x.PayloadValues.Length >= 6 ? (long)x.PayloadValues[5] : 0));
            
            tableGenerator.AddColumn(durationColumn, baseProjection.Compose(x => FindGCDuration(x, gcStopEvents)));
            tableGenerator.AddColumn(processIdColumn, baseProjection.Compose(x => x.ProcessID));
            tableGenerator.AddColumn(processColumn, baseProjection.Compose(x => x.ProcessName));
            tableGenerator.AddColumn(cpuColumn, baseProjection.Compose(x => x.ProcessorNumber));
            tableGenerator.AddColumn(threadIdColumn, baseProjection.Compose(x => x.ThreadID));
            tableGenerator.AddColumn(timestampColumn, baseProjection.Compose(x => x.Timestamp));


            var gcConfig = new TableConfiguration("GC")
            {
                Columns = new ColumnConfiguration[]
                {
                    gcReasonColumn,
                    gcTypeColumn,
                    TableConfiguration.PivotColumn,
                    processColumn,
                    cpuColumn,
                    threadIdColumn,
                    countColumn,
                    depthColumn,
                    clientSequenceNumberColumn,
                    clrInstanceIDColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    timestampColumn,
                    durationColumn,
        }
            };
            gcConfig.AddColumnRole(ColumnRole.StartTime, timestampColumn);
            gcConfig.AddColumnRole(ColumnRole.EndTime, timestampColumn);
            gcConfig.AddColumnRole(ColumnRole.Duration, durationColumn);

            var table = tableBuilder
            .AddTableConfiguration(gcConfig)
            .SetDefaultTableConfiguration(gcConfig);
        }

        TimestampDelta FindGCDuration(GenericEvent gcStart, IEnumerable<GenericEvent> gcStops)
        {
            var stop = gcStops.FirstOrDefault(f => f.ThreadID == gcStart.ThreadID && f.Timestamp > gcStart.Timestamp);
            if (stop == null)
            {
                return TimestampDelta.Zero;
            }
            else
            {
                return stop.Timestamp - gcStart.Timestamp;
            }
        }
    }
}
