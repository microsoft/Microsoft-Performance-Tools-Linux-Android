// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using PerfettoCds.Pipeline.DataOutput;
using Microsoft.Performance.SDK;
using PerfettoCds.Pipeline.DataCookers;
using Utilities;

namespace PerfettoCds.Pipeline.Tables
{
    [Table]
    public class PerfettoGenericEventTable
    {
        // Set some sort of max to prevent ridiculous field counts
        public const int AbsoluteMaxFields = 20;

        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{506777b6-f1a3-437a-b976-bc48190450b6}"),
            "Perfetto Generic Events",
            "All app/component events in the Perfetto trace",
            "Perfetto",
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.GenericEventCookerPath }
        );

        private static readonly ColumnConfiguration ProcessNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{b690f27e-7938-4e86-94ef-d048cbc476cc}"), "Process", "Name of the process"),
            new UIHints { Width = 210 });

        private static readonly ColumnConfiguration ThreadNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{dd1cf3f6-1cab-4012-bbdf-e99e920c4112}"), "Thread", "Name of the thread"),
            new UIHints { Width = 210 });

        private static readonly ColumnConfiguration EventNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{d3bc5189-c9d1-4c14-9ce2-7bb4dc4d5ee7}"), "Name", "Name of the Perfetto event"),
            new UIHints { Width = 210 });

        private static readonly ColumnConfiguration StartTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{d458382b-1320-45c6-ba86-885da9dae71d}"), "StartTimestamp", "Start timestamp for the event"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration EndTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{4642871b-d0d8-4f74-9516-1ae1d7e9fe27}"), "EndTimestamp", "End timestamp for the event"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration DurationColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{14f4862d-5851-460d-a04b-62e4b62b6d6c}"), "Duration", "Duration of the event"),
            new UIHints { Width = 70 });

        private static readonly ColumnConfiguration CategoryColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{1aa73a71-1548-44fd-9bcd-854bca78ce2e}"), "Category", "StackID of the event"),
            new UIHints { Width = 70 });

        private static readonly ColumnConfiguration TypeColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{01d2b15f-b0fc-4444-a240-0a96f62c2c50}"), "Type", "Type of the event"),
            new UIHints { Width = 70 });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // We dynamically adjust the column headers
            // This is the max number of fields we can expect for this table
            int maxFieldCount = Math.Min(AbsoluteMaxFields, tableData.QueryOutput<int>(
                new DataOutputPath(PerfettoPluginConstants.GenericEventCookerPath, nameof(PerfettoGenericEventCooker.MaximumEventFieldCount))));

            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoGenericEvent>>(
                new DataOutputPath(PerfettoPluginConstants.GenericEventCookerPath, nameof(PerfettoGenericEventCooker.GenericEvents)));

            // Start construction of the column order. Pivot on process and thread
            List<ColumnConfiguration> allColumns = new List<ColumnConfiguration>() 
            {
                ProcessNameColumn,
                ThreadNameColumn,
                TableConfiguration.PivotColumn, // Columns before this get pivotted on
                EventNameColumn,
                CategoryColumn,
                TypeColumn,
                EndTimestampColumn,
                DurationColumn,
            };

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var genericEventProjection = new EventProjection<PerfettoGenericEvent>(events);

            // Add all the data projections
            var processNameColumn = new BaseDataColumn<string>(
                ProcessNameColumn,
                genericEventProjection.Compose((genericEvent) => genericEvent.Process));
            tableGenerator.AddColumn(processNameColumn);

            var threadNameColumn = new BaseDataColumn<string>(
                ThreadNameColumn,
                genericEventProjection.Compose((genericEvent) => genericEvent.Thread));
            tableGenerator.AddColumn(threadNameColumn);

            var eventNameColumn = new BaseDataColumn<string>(
                EventNameColumn,
                genericEventProjection.Compose((genericEvent) => genericEvent.EventName));
            tableGenerator.AddColumn(eventNameColumn);

            var startTimestampColumn = new BaseDataColumn<Timestamp>(
                StartTimestampColumn,
                genericEventProjection.Compose((genericEvent) => genericEvent.StartTimestamp));
            tableGenerator.AddColumn(startTimestampColumn);

            var endTimestampColumn = new BaseDataColumn<Timestamp>(
                EndTimestampColumn,
                genericEventProjection.Compose((genericEvent) => genericEvent.EndTimestamp));
            tableGenerator.AddColumn(endTimestampColumn);

            var durationColumn = new BaseDataColumn<TimestampDelta>(
                DurationColumn,
                genericEventProjection.Compose((genericEvent) => genericEvent.Duration));
            tableGenerator.AddColumn(durationColumn);

            var categoryColumn = new BaseDataColumn<string>(
                CategoryColumn,
                genericEventProjection.Compose((genericEvent) => genericEvent.Category));
            tableGenerator.AddColumn(categoryColumn);

            var typeColumn = new BaseDataColumn<string>(
                TypeColumn,
                genericEventProjection.Compose((genericEvent) => genericEvent.Type));
            tableGenerator.AddColumn(typeColumn);

            // Add the field columns, with column names depending on the given event
            for (int index = 0; index < maxFieldCount; index++)
            {
                var colIndex = index;  // This seems unncessary but causes odd runtime behavior if not done this way. Compiler is confused perhaps because w/o this func will index=genericEvent.FieldNames.Count every time. index is passed as ref but colIndex as value into func
                string fieldName = "Field " + (colIndex + 1);

                var genericEventFieldNameProjection = genericEventProjection.Compose((genericEvent) => colIndex < genericEvent.ArgKeys.Count ? genericEvent.ArgKeys[colIndex] : string.Empty);

                // generate a column configuration
                var fieldColumnConfiguration = new ColumnConfiguration(
                        new ColumnMetadata(Common.GenerateGuidFromName(fieldName), fieldName, genericEventFieldNameProjection, fieldName),
                        new UIHints
                        {
                            IsVisible = true,
                            Width = 150,
                            TextAlignment = TextAlignment.Left,
                        });

                // Add this column to the column order
                allColumns.Add(fieldColumnConfiguration);

                var genericEventFieldAsStringProjection = genericEventProjection.Compose((genericEvent) => colIndex < genericEvent.Values.Count ? genericEvent.Values[colIndex] : string.Empty);

                tableGenerator.AddColumn(fieldColumnConfiguration, genericEventFieldAsStringProjection);
            }

            // Finish the column order with the timestamp columned being graphed
            allColumns.Add(TableConfiguration.GraphColumn); // Columns after this get graphed
            allColumns.Add(StartTimestampColumn);

            var tableConfig = new TableConfiguration("Perfetto Trace Events")
            {
                Columns = allColumns,
                Layout = TableLayoutStyle.GraphAndTable
            };
            tableConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            tableConfig.AddColumnRole(ColumnRole.EndTime, EndTimestampColumn.Metadata.Guid);
            tableConfig.AddColumnRole(ColumnRole.Duration, DurationColumn.Metadata.Guid);

            tableBuilder.AddTableConfiguration(tableConfig).SetDefaultTableConfiguration(tableConfig);
        }
    }
}
