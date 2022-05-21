// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
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
    public sealed class GenericEventTable
        : TraceEventTableBase
    {
        public static readonly TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{50315806-167F-46FF-946A-EA8B21F33F36}"),
            "Generic Events",
            "Generic Events",
            category: ".NET trace (dotnet-trace)");

        public GenericEventTable(IReadOnlyDictionary<string, TraceEventProcessor> traceEventProcessor)
            : base(traceEventProcessor)
        {
        }

        private static readonly ColumnConfiguration eventNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{7AE1590E-20D3-4F39-90D1-4AEE5CD41394}"), "EventName", "Event Name"),
            new UIHints { Width = 305 });

        private static readonly ColumnConfiguration idColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{75FE88C4-CB8E-46E5-B8F4-CE760B8A6F51}"), "ID", "Event ID"),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration keywordsColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{54145FD1-2EF3-4A6D-9914-44ECCBBF8532}"), "Keywords", "Keywords"),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration levelColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{A2DCBDE1-4899-49E2-8096-BE34BB10674F}"), "Level", "Level"),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration opcodeColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{520E92D2-F4DE-432E-80EE-8A557D5E6BD1}"), "Opcode", "Opcode"),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration opcodeNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{912561AE-BDE2-4745-8504-D4E696C6F800}"), "OpcodeName", "Opcode Name"),
            new UIHints { Width = 170 });

        private static readonly ColumnConfiguration providerGuidColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{5C13098C-579C-443E-8476-3187FF532966}"), "ProviderGuid", "Provider Guid"),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration providerNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{93C151D8-53D0-4AAF-AFDE-BBB2D516F59A}"), "Provider Name", "Provider Name"),
            new UIHints { Width = 270 });

        private static readonly ColumnConfiguration timestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{F5AE0530-DC46-41B3-A0E9-416955A9C7CA}"), "Timestamp", "The timestamp of the event"),
            new UIHints { Width = 80 });


        private static readonly ColumnConfiguration countColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{06B669F7-A719-4B4C-9F1F-0B50B6D20EF7}"), "Count", "The count of samples"),
            new UIHints { 
                Width = 130, 
                AggregationMode = AggregationMode.Sum,
                SortOrder = SortOrder.Descending,
                SortPriority = 0,
            });

        private static readonly ColumnConfiguration hascallStackColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{2BCEC64D-A3A2-4EC5-B376-4040560066FC}"), "HasCallstack", "Has Callstack"),
                new UIHints { Width = 40, });

        private static readonly ColumnConfiguration callStackColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{37EF26BF-8FAF-4700-AD66-01D90DA743BF}"), "Callstack", "Call stack"),
                new UIHints { Width = 800, });

        private static readonly ColumnConfiguration threadIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{4EEBBEF3-C5E5-422C-AF6D-B94CE17AB3DF}"), "ThreadId"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration processColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{E3FEEE4F-CECC-4BEA-A926-253869ADD223}"), "Process"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration processIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{E40AED70-CAF0-4B24-AE44-3EE02FD2A5BD}"), "Process Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration cpuColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{5AAB0226-0599-427C-B84F-0E1A86CE6B73}"), "CPU"),
                new UIHints { Width = 80, });


        public override void Build(ITableBuilder tableBuilder)
        {
            if (TraceEventProcessor == null || TraceEventProcessor.Count == 0)
            {
                return;
            }

            var firstTraceProcessorEventsParsed = TraceEventProcessor.First().Value;  // First Log
            var genericEvents = firstTraceProcessorEventsParsed.GenericEvents;

            var tableGenerator = tableBuilder.SetRowCount(genericEvents.Count);
            var baseProjection = Projection.Index(genericEvents);

            var maximumFieldCount = 0;
            foreach (var genericEvent in genericEvents)
            {
                maximumFieldCount = Math.Max(maximumFieldCount, genericEvent.PayloadValues.Length);
            }

            tableGenerator.AddColumn(countColumn, baseProjection.Compose(x => 1));                  // 1 sample
            tableGenerator.AddColumn(eventNameColumn, baseProjection.Compose(x => x.EventName));
            tableGenerator.AddColumn(idColumn, baseProjection.Compose(x => x.ID));
            tableGenerator.AddColumn(keywordsColumn, baseProjection.Compose(x => x.Keywords));
            tableGenerator.AddColumn(levelColumn, baseProjection.Compose(x => x.Level));
            tableGenerator.AddColumn(opcodeColumn, baseProjection.Compose(x => x.Opcode));
            tableGenerator.AddColumn(opcodeNameColumn, baseProjection.Compose(x => x.OpcodeName));
            tableGenerator.AddColumn(providerGuidColumn, baseProjection.Compose(x => x.ProviderGuid));
            tableGenerator.AddColumn(providerNameColumn, baseProjection.Compose(x => x.ProviderName));
            tableGenerator.AddColumn(processIdColumn, baseProjection.Compose(x => x.ProcessID));
            tableGenerator.AddColumn(processColumn, baseProjection.Compose(x => x.ProcessName));
            tableGenerator.AddColumn(cpuColumn, baseProjection.Compose(x => x.ProcessorNumber));
            tableGenerator.AddColumn(threadIdColumn, baseProjection.Compose(x => x.ThreadID));
            tableGenerator.AddColumn(timestampColumn, baseProjection.Compose(x => x.Timestamp));
            tableGenerator.AddColumn(hascallStackColumn, baseProjection.Compose(x => x.CallStack != null));
            tableGenerator.AddHierarchicalColumn(callStackColumn, baseProjection.Compose(x => x.CallStack), new ArrayAccessProvider<string>());

            // Add the field columns, with column names depending on the given event
            List<ColumnConfiguration> fieldColumns = new List<ColumnConfiguration>();
            for (int index = 0; index < maximumFieldCount; index++)
            {
                var colIndex = index;  // This seems unncessary but causes odd runtime behavior if not done this way. Compiler is confused perhaps because w/o this func will index=genericEvent.FieldNames.Count every time. index is passed as ref but colIndex as value into func
                string fieldName = "Field " + (colIndex + 1);

                var genericEventFieldNameProjection = baseProjection.Compose((genericEvent) => colIndex < genericEvent.PayloadNames?.Length ? genericEvent.PayloadNames[colIndex] : string.Empty);

                // generate a column configuration
                var fieldColumnConfiguration = new ColumnConfiguration(
                        new ColumnMetadata(Common.GenerateGuidFromName(fieldName), fieldName, genericEventFieldNameProjection, fieldName),
                        new UIHints
                        {
                            IsVisible = true,
                            Width = 80,
                            TextAlignment = TextAlignment.Left,
                        });
                fieldColumns.Add(fieldColumnConfiguration);

                var genericEventFieldAsStringProjection = baseProjection.Compose((genericEvent) => colIndex < genericEvent.PayloadValues?.Length ? genericEvent.PayloadValues[colIndex] : string.Empty);

                tableGenerator.AddColumn(fieldColumnConfiguration, genericEventFieldAsStringProjection);
            }


            List<ColumnConfiguration> defaultColumns = new List<ColumnConfiguration>()
            {
                providerNameColumn,
                eventNameColumn,
                opcodeNameColumn,
                TableConfiguration.PivotColumn,
                cpuColumn,
                processColumn,
                threadIdColumn,
            };
            defaultColumns.AddRange(fieldColumns);
            defaultColumns.Add(countColumn);
            defaultColumns.Add(TableConfiguration.GraphColumn); // Columns after this get graphed
            defaultColumns.Add(timestampColumn);

            var actProviderNameOpcode = new TableConfiguration("Activity by Provider, EventName, Opcode")
            {
                Columns = defaultColumns
            };
            actProviderNameOpcode.AddColumnRole(ColumnRole.StartTime, timestampColumn);
            actProviderNameOpcode.AddColumnRole(ColumnRole.EndTime, timestampColumn);

            var table = tableBuilder
            .AddTableConfiguration(actProviderNameOpcode)
            .SetDefaultTableConfiguration(actProviderNameOpcode);
        }
    }
}
