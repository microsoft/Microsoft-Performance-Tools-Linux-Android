// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using PerfettoCds.Pipeline.DataOutput;
using Microsoft.Performance.SDK;
using PerfettoCds.Pipeline.CompositeDataCookers;
using Utilities;
using Utilities.AccessProviders;

namespace PerfettoCds.Pipeline.Tables
{
    [Table]
    public class PerfettoGenericEventTable
    {
        // Set some sort of max to prevent ridiculous field counts
        public const int AbsoluteMaxFields = 20;

        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{506777b6-f1a3-437a-b976-bc48190450b6}"),
            " Perfetto Generic Events",  // Space at the start so it shows up alphabetically first in the table list
            "All app/component events in the Perfetto trace",
            "Perfetto - Events",
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.GenericEventCookerPath }
        );

        private static readonly ColumnConfiguration ProcessNameColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{b690f27e-7938-4e86-94ef-d048cbc476cc}"), "Process", "Name of the process"),
            new UIHints { Width = 210 });

        private static readonly ColumnConfiguration ThreadNameColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{dd1cf3f6-1cab-4012-bbdf-e99e920c4112}"), "Thread", "Name of the thread"),
            new UIHints { Width = 210 });

        private static readonly ColumnConfiguration EventNameColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{d3bc5189-c9d1-4c14-9ce2-7bb4dc4d5ee7}"), "Name", "Name of the Perfetto event"),
            new UIHints { Width = 210 });

        private static readonly ColumnConfiguration StartTimestampColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{d458382b-1320-45c6-ba86-885da9dae71d}"), "StartTimestamp", "Start timestamp for the event"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration EndTimestampColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{4642871b-d0d8-4f74-9516-1ae1d7e9fe27}"), "EndTimestamp", "End timestamp for the event"),
            new UIHints { Width = 120 });

        // Need 2 of these with different sorting
        const string DurationColumnGuid = "{14f4862d-5851-460d-a04b-62e4b62b6d6c}";
        private static readonly ColumnConfiguration DurationColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid(DurationColumnGuid), "Duration", "Duration of the event"),
            new UIHints { 
                Width = 70,
                AggregationMode = AggregationMode.Max,
                SortPriority = 1,
                SortOrder = SortOrder.Descending,
                CellFormat = TimestampFormatter.FormatMillisecondsGrouped,
            });

        private static readonly ColumnConfiguration DurationNotSortedColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid(DurationColumnGuid), "Duration", "Duration of the event"),
            new UIHints
            {
                Width = 70,
                AggregationMode = AggregationMode.Max,
                CellFormat = TimestampFormatter.FormatMillisecondsGrouped,
            });

        private static readonly ColumnConfiguration CategoryColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{1aa73a71-1548-44fd-9bcd-854bca78ce2e}"), "Category", "StackID of the event"),
            new UIHints { Width = 70 });

        private static readonly ColumnConfiguration TypeColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{01d2b15f-b0fc-4444-a240-0a96f62c2c50}"), "Type", "Type of the event"),
            new UIHints { Width = 70 });

        private static readonly ColumnConfiguration ProviderColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{e7d08f97-f52c-4686-bc49-737f7a6a8bbb}"), "Provider", "Provider name of the event"),
            new UIHints { Width = 240 });

        private static readonly ColumnConfiguration TrackNameIdColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{111094F9-BEB4-486F-AD60-3F53CFF702EA}"), "TrackNameId", "Track Name (Id)"),
            new UIHints { Width = 240 });

        private static readonly ColumnConfiguration ParentIdColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{A77736C3-AC5C-4100-B246-3821A2E73B15}"), "Parent Id", "Parent Id"),
            new UIHints { Width = 240 });

        private static readonly ColumnConfiguration ParentDepthLevelColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{89572E6E-86D5-4CBA-B0D4-4F9D2147BF50}"), "Parent Depth Level", "Parent Depth Level (0 is at Top of Tree)"),
            new UIHints
            {
                Width = 70,
                SortPriority = 0,
                SortOrder = SortOrder.Ascending,
            });

        private static readonly ColumnConfiguration ParentEventNameTreeBranchColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{A9496811-0330-4DEC-9C47-890C6D2F7BE1}"), "Parent Event Name Path", "Parent Event Name Tree Path"),
            new UIHints
            {
                Width = 210,
            });

        // Need 2 of these with different sorting
        const string CountColumnGuid = "{99192cbf-5888-4873-a3b3-4faf5beaea15}";
        private static readonly ColumnConfiguration CountColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid(CountColumnGuid), "Count", "Constant column of 1 for summing"),
            new UIHints
            {
                Width = 80,
                AggregationMode = AggregationMode.Sum
            });

        private static readonly ColumnConfiguration CountSortedColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid(CountColumnGuid), "Count", "Constant column of 1 for summing"),
            new UIHints 
            { 
                Width = 80, 
                AggregationMode = AggregationMode.Sum,
                SortPriority = 0,
                SortOrder = SortOrder.Descending,
            });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // We dynamically adjust the column headers
            // This is the max number of fields we can expect for this table
            int maxFieldCount = Math.Min(AbsoluteMaxFields, tableData.QueryOutput<int>(
                new DataOutputPath(PerfettoPluginConstants.GenericEventCookerPath, nameof(PerfettoGenericEventCooker.MaximumEventFieldCount))));

            bool hasProviders = tableData.QueryOutput<bool>(
                new DataOutputPath(PerfettoPluginConstants.GenericEventCookerPath, nameof(PerfettoGenericEventCooker.HasProviders)));

            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoGenericEvent>>(
                new DataOutputPath(PerfettoPluginConstants.GenericEventCookerPath, nameof(PerfettoGenericEventCooker.GenericEvents)));

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var genericEventProjection = new EventProjection<PerfettoGenericEvent>(events);

            // Add all the data projections
            var processNameColumn = new BaseDataColumn<string>(
                ProcessNameColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.Process));
            tableGenerator.AddColumn(processNameColumn);

            var threadNameColumn = new BaseDataColumn<string>(
                ThreadNameColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.Thread));
            tableGenerator.AddColumn(threadNameColumn);

            var eventNameColumn = new BaseDataColumn<string>(
                EventNameColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.EventName));
            tableGenerator.AddColumn(eventNameColumn);

            var startTimestampColumn = new BaseDataColumn<Timestamp>(
                StartTimestampColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.StartTimestamp));
            tableGenerator.AddColumn(startTimestampColumn);

            var endTimestampColumn = new BaseDataColumn<Timestamp>(
                EndTimestampColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.EndTimestamp));
            tableGenerator.AddColumn(endTimestampColumn);

            var durationColumn = new BaseDataColumn<TimestampDelta>(
                DurationColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.Duration));
            tableGenerator.AddColumn(durationColumn);

            var categoryColumn = new BaseDataColumn<string>(
                CategoryColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.Category));
            tableGenerator.AddColumn(categoryColumn);

            var typeColumn = new BaseDataColumn<string>(
                TypeColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.Type));
            tableGenerator.AddColumn(typeColumn);

            var providerColumn = new BaseDataColumn<string>(
                ProviderColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.Provider));
            tableGenerator.AddColumn(providerColumn);

            var trackNameIdColumn = new BaseDataColumn<string>(
                TrackNameIdColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.ThreadTrack != null ? 
                                                                    (!String.IsNullOrWhiteSpace(genericEvent.ThreadTrack.Name) ? $"{genericEvent.ThreadTrack.Name} ({genericEvent.ThreadTrack.Id})" : genericEvent.ThreadTrack.Id.ToString()) 
                                                                    : String.Empty));
            tableGenerator.AddColumn(trackNameIdColumn);

            var parentIdColumn = new BaseDataColumn<int>(
                ParentIdColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.ParentId.HasValue? genericEvent.ParentId.Value : -1));
            tableGenerator.AddColumn(parentIdColumn);

            var parentDepthLevelColumn = new BaseDataColumn<int>(
                ParentDepthLevelColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.ParentTreeDepthLevel));
            tableGenerator.AddColumn(parentDepthLevelColumn);

            tableGenerator.AddHierarchicalColumn(ParentEventNameTreeBranchColConfig, genericEventProjection.Compose((genericEvent) => genericEvent.ParentEventNameTree), new ArrayAccessProvider<string>());

            tableGenerator.AddColumn(CountColConfig, Projection.Constant<int>(1));

            // The provider column is optionally populated depending on whether or not the user specified a ProviderGUID mapping file
            ProviderColConfig.DisplayHints.IsVisible = hasProviders;

            List<ColumnConfiguration> fieldColumns = new List<ColumnConfiguration>();
            // Add the field columns, with column names depending on the given event
            for (int index = 0; index < maxFieldCount; index++)
            {
                var colIndex = index;  // This seems unncessary but causes odd runtime behavior if not done this way. Compiler is confused perhaps because w/o this func will index=genericEvent.FieldNames.Count every time. index is passed as ref but colIndex as value into func
                string fieldName = "Field " + (colIndex + 1);

                var genericEventFieldNameProjection = genericEventProjection.Compose((genericEvent) => colIndex < genericEvent.ArgKeys.Length ? genericEvent.ArgKeys[colIndex] : string.Empty);

                // generate a column configuration
                var fieldColumnConfiguration = new ColumnConfiguration(
                    new ColumnMetadata(Common.GenerateGuidFromName(fieldName), fieldName, genericEventFieldNameProjection, fieldName)
                    {
                        IsDynamic = true
                    }, 
                    new UIHints
                    {
                        IsVisible = true,
                        Width = 150,
                        TextAlignment = TextAlignment.Left,
                    });

                // Add this column to the column order
                fieldColumns.Add(fieldColumnConfiguration);

                var genericEventFieldAsStringProjection = genericEventProjection.Compose((genericEvent) => colIndex < genericEvent.Values.Length ? genericEvent.Values[colIndex] : string.Empty);

                tableGenerator.AddColumn(fieldColumnConfiguration, genericEventFieldAsStringProjection);
            }

            List<ColumnConfiguration> defaultColumns = new List<ColumnConfiguration>()
            {
                ProviderColConfig,
                ProcessNameColConfig,
                ThreadNameColConfig,
                TableConfiguration.PivotColumn, // Columns before this get pivotted on
                EventNameColConfig,
                CategoryColConfig,
                TypeColConfig,
                DurationColConfig,
            };
            defaultColumns.AddRange(fieldColumns);
            defaultColumns.Add(TableConfiguration.GraphColumn); // Columns after this get graphed
            defaultColumns.Add(StartTimestampColConfig);
            defaultColumns.Add(EndTimestampColConfig);

            var processThreadConfig = new TableConfiguration("Perfetto Trace Events - Process-Thread")
            {
                Columns = defaultColumns,
                Layout = TableLayoutStyle.GraphAndTable
            };
            SetGraphTableConfig(processThreadConfig);

            var processThreadActivityColumns = new List<ColumnConfiguration>(defaultColumns);
            processThreadActivityColumns.Remove(StartTimestampColConfig);
            processThreadActivityColumns.Insert(8, StartTimestampColConfig);
            processThreadActivityColumns.Remove(EndTimestampColConfig);
            processThreadActivityColumns.Insert(9, EndTimestampColConfig);
            processThreadActivityColumns.Add(CountSortedColConfig);

            // Different sorting than default
            processThreadActivityColumns.Remove(DurationColConfig);
            processThreadActivityColumns.Insert(7, DurationNotSortedColConfig);
            DurationNotSortedColConfig.DisplayHints.SortPriority = 1;
            DurationNotSortedColConfig.DisplayHints.SortOrder = SortOrder.Descending;

            var processThreadActivityConfig = new TableConfiguration("Perfetto Trace Events - Process-Thread Activity")
            {
                Columns = processThreadActivityColumns,
                Layout = TableLayoutStyle.GraphAndTable
            };
            SetGraphTableConfig(processThreadActivityConfig);

            var processThreadNameColumns = new List<ColumnConfiguration>(defaultColumns);
            processThreadNameColumns.Insert(3, ParentDepthLevelColConfig);
            processThreadNameColumns.Remove(EventNameColConfig);
            processThreadNameColumns.Insert(4, EventNameColConfig);
            processThreadNameColumns.Insert(9, ParentEventNameTreeBranchColConfig);
            var processThreadNameConfig = new TableConfiguration("Perfetto Trace Events - Process-Thread-Name")
            {
                Columns = processThreadNameColumns,
                Layout = TableLayoutStyle.GraphAndTable
            };
            SetGraphTableConfig(processThreadNameConfig);

            var processThreadNameTreeColumns = new List<ColumnConfiguration>(defaultColumns);
            processThreadNameTreeColumns.Insert(3, ParentEventNameTreeBranchColConfig);
            processThreadNameTreeColumns.Insert(9, ParentDepthLevelColConfig);
            processThreadNameTreeColumns.Insert(10, ParentEventNameTreeBranchColConfig);
            var processThreadParentNameTreeConfig = new TableConfiguration("Perfetto Trace Events - Process-Thread-ParentNameTree")
            {
                Columns = processThreadNameTreeColumns,
                Layout = TableLayoutStyle.GraphAndTable
            };
            SetGraphTableConfig(processThreadParentNameTreeConfig);

            tableBuilder
                .AddTableConfiguration(processThreadConfig)
                .AddTableConfiguration(processThreadActivityConfig)
                .AddTableConfiguration(processThreadNameConfig)
                .AddTableConfiguration(processThreadParentNameTreeConfig)
                .SetDefaultTableConfiguration(processThreadNameConfig);
        }

        private static void SetGraphTableConfig(TableConfiguration tableConfig)
        {
            tableConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColConfig.Metadata.Guid);
            tableConfig.AddColumnRole(ColumnRole.EndTime, EndTimestampColConfig.Metadata.Guid);
            tableConfig.AddColumnRole(ColumnRole.Duration, DurationColConfig.Metadata.Guid);
        }
    }
}