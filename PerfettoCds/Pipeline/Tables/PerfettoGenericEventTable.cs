// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using PerfettoCds.Pipeline.CompositeDataCookers;
using PerfettoCds.Pipeline.DataOutput;
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
            " Generic Events",  // Space at the start so it shows up alphabetically first in the table list
            "All app/component events in the Perfetto trace",
            "Perfetto - Events",
            defaultLayout: TableLayoutStyle.GraphAndTable,
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.GenericEventCookerPath }
        );

        private static readonly ColumnConfiguration SliceIdColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{ED471485-6246-4041-8C85-4153C0C33EF9}"), "SliceId", "Slice Id"),
            new UIHints { Width = 210, IsVisible = false });

        private static readonly ColumnConfiguration ProcessNameColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{b690f27e-7938-4e86-94ef-d048cbc476cc}"), "Process", "Name of the process"),
            new UIHints { Width = 210 });

        private static readonly ColumnConfiguration ProcessLabelColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{043E8352-0543-4593-9F7A-04DBD0103A80}"), "ProcessLabel", "Label of the process"),
            new UIHints { Width = 210, IsVisible = false });

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

        private static readonly ColumnConfiguration DurationNotSortedColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{14f4862d-5851-460d-a04b-62e4b62b6d6c}"), "Duration", "Duration of the event"),
            new UIHints
            {
                Width = 70,
                AggregationMode = AggregationMode.Max,
                CellFormat = TimestampFormatter.FormatMillisecondsGrouped,
            });

        // Need 3 of these with different sorting
        const string DurationExColumnGuid = "{F607C7BF-EFC2-4151-BFD9-216CE0D7A891}";
        private static readonly ColumnConfiguration DurationExSortedColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid(DurationExColumnGuid), "Duration (Exclusive)", "Duration of the event (exclusive of children durations)"),
            new UIHints
            {
                Width = 70,
                AggregationMode = AggregationMode.Max,
                SortPriority = 1,
                SortOrder = SortOrder.Descending,
                CellFormat = TimestampFormatter.FormatMillisecondsGrouped,
            });

        private static readonly ColumnConfiguration DurationExSortedSumColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid(DurationExColumnGuid), "Duration (Exclusive)", "Duration of the event (exclusive of children durations)"),
            new UIHints
            {
                Width = 70,
                AggregationMode = AggregationMode.Sum,
                SortPriority = 1,
                SortOrder = SortOrder.Descending,
                CellFormat = TimestampFormatter.FormatMillisecondsGrouped,
            });

        private static readonly ColumnConfiguration DurationExNotSortedMaxColConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid(DurationExColumnGuid), "Duration (Exclusive)", "Duration of the event (exclusive of children durations)"),
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
            new UIHints { Width = 70, IsVisible = false, });

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
                IsVisible = false,
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


        public static bool IsDataAvailable(IDataExtensionRetrieval tableData)
        {
            return tableData.QueryOutput<ProcessedEventData<PerfettoGenericEvent>>(
                new DataOutputPath(PerfettoPluginConstants.GenericEventCookerPath, nameof(PerfettoGenericEventCooker.GenericEvents))).Any();
        }

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
            var sliceIdColumn = new DataColumn<int>(
                SliceIdColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.SliceId));
            tableGenerator.AddColumn(sliceIdColumn);
        
            var processNameColumn = new DataColumn<string>(
                ProcessNameColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.Process));
            tableGenerator.AddColumn(processNameColumn);

            var processLabelColumn = new DataColumn<string>(
                ProcessLabelColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.ProcessLabel));
            tableGenerator.AddColumn(processLabelColumn);
            if (events.Any(f => !String.IsNullOrWhiteSpace(f.ProcessLabel)))
            {
                ProcessLabelColConfig.DisplayHints.IsVisible = true;
            }

            var threadNameColumn = new DataColumn<string>(
                ThreadNameColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.Thread));
            tableGenerator.AddColumn(threadNameColumn);

            var eventNameColumn = new DataColumn<string>(
                EventNameColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.EventName));
            tableGenerator.AddColumn(eventNameColumn);

            var startTimestampColumn = new DataColumn<Timestamp>(
                StartTimestampColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.StartTimestamp));
            tableGenerator.AddColumn(startTimestampColumn);

            var endTimestampColumn = new DataColumn<Timestamp>(
                EndTimestampColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.EndTimestamp));
            tableGenerator.AddColumn(endTimestampColumn);

            var durationColumn = new DataColumn<TimestampDelta>(
                DurationNotSortedColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.Duration));
            tableGenerator.AddColumn(durationColumn);

            var durationExColumn = new DataColumn<TimestampDelta>(
                DurationExSortedColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.DurationExclusive));
            tableGenerator.AddColumn(durationExColumn);

            var categoryColumn = new DataColumn<string>(
                CategoryColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.Category));
            tableGenerator.AddColumn(categoryColumn);

            var typeColumn = new DataColumn<string>(
                TypeColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.Type));
            tableGenerator.AddColumn(typeColumn);

            var providerColumn = new DataColumn<string>(
                ProviderColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.Provider));
            tableGenerator.AddColumn(providerColumn);

            var trackNameIdColumn = new DataColumn<string>(
                TrackNameIdColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.ThreadTrack != null ?
                                                                    (!String.IsNullOrWhiteSpace(genericEvent.ThreadTrack.Name) ? $"{genericEvent.ThreadTrack.Name} ({genericEvent.ThreadTrack.Id})" : genericEvent.ThreadTrack.Id.ToString())
                                                                    : String.Empty));
            tableGenerator.AddColumn(trackNameIdColumn);

            var parentIdColumn = new DataColumn<int>(
                ParentIdColConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.ParentId.HasValue ? genericEvent.ParentId.Value : -1));
            tableGenerator.AddColumn(parentIdColumn);

            var parentDepthLevelColumn = new DataColumn<int>(
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

                var genericEventFieldNameProjection = genericEventProjection.Compose((genericEvent) => colIndex < genericEvent.Args?.Count ? genericEvent.Args.ElementAt(colIndex).Key : string.Empty);

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

                var genericEventFieldAsStringProjection = genericEventProjection.Compose((genericEvent) => colIndex < genericEvent.Args?.Count ? genericEvent.Args.ElementAt(colIndex).Value : string.Empty);

                tableGenerator.AddColumn(fieldColumnConfiguration, genericEventFieldAsStringProjection);
            }

            List<ColumnConfiguration> defaultColumns = new List<ColumnConfiguration>()
            {
                ProviderColConfig,
                ProcessNameColConfig,
                ProcessLabelColConfig,
                ThreadNameColConfig,
                TableConfiguration.PivotColumn, // Columns before this get pivotted on
                EventNameColConfig,
                CategoryColConfig,
                TypeColConfig,
                DurationNotSortedColConfig,
                DurationExSortedColConfig,
            };
            defaultColumns.AddRange(fieldColumns);
            defaultColumns.Add(TableConfiguration.GraphColumn); // Columns after this get graphed
            defaultColumns.Add(StartTimestampColConfig);
            defaultColumns.Add(EndTimestampColConfig);

            // Proces-Thread config
            var processThreadConfig = new TableConfiguration("Process-Thread")
            {
                Columns = defaultColumns,
            };
            SetGraphTableConfig(processThreadConfig);
            tableBuilder.AddTableConfiguration(processThreadConfig);

            // Process-Thread by StartTime config
            var processThreadByStartTimeColumns = new List<ColumnConfiguration>(defaultColumns);
            processThreadByStartTimeColumns.Remove(EndTimestampColConfig);
            processThreadByStartTimeColumns.Insert(processThreadByStartTimeColumns.Count - 2, EndTimestampColConfig);

            var processThreadByStartTimeConfig = new TableConfiguration("Process-Thread by Start Time")
            {
                Columns = processThreadByStartTimeColumns,
            };
            SetGraphTableConfig(processThreadByStartTimeConfig);
            tableBuilder.AddTableConfiguration(processThreadByStartTimeConfig);

            // Process-Thread Activity config
            var processThreadActivityColumns = new List<ColumnConfiguration>(defaultColumns);
            processThreadActivityColumns.Remove(StartTimestampColConfig);
            processThreadActivityColumns.Insert(10, StartTimestampColConfig);
            processThreadActivityColumns.Remove(EndTimestampColConfig);
            processThreadActivityColumns.Insert(11, EndTimestampColConfig);
            processThreadActivityColumns.Add(CountSortedColConfig);

            // Different sorting than default
            processThreadActivityColumns.Remove(DurationExSortedColConfig);
            processThreadActivityColumns.Insert(9, DurationExNotSortedMaxColConfig);
            DurationExNotSortedMaxColConfig.DisplayHints.SortPriority = 1;
            DurationExNotSortedMaxColConfig.DisplayHints.SortOrder = SortOrder.Descending;

            var processThreadActivityConfig = new TableConfiguration("Process-Thread Activity")
            {
                Columns = processThreadActivityColumns,
            };
            SetGraphTableConfig(processThreadActivityConfig);
            tableBuilder.AddTableConfiguration(processThreadActivityConfig);

            // Default - Process-Thread-NestedSlices-Name config
            var processThreadNameColumns = new List<ColumnConfiguration>(defaultColumns);
            processThreadNameColumns.Insert(4, ParentDepthLevelColConfig);
            processThreadNameColumns.Remove(EventNameColConfig);
            processThreadNameColumns.Insert(5, EventNameColConfig);
            var processThreadNestedNameConfig = new TableConfiguration("Process-Thread-NestedSlices-Name")
            {
                Columns = processThreadNameColumns,
            };
            SetGraphTableConfig(processThreadNestedNameConfig);
            tableBuilder.AddTableConfiguration(processThreadNestedNameConfig)
                        .SetDefaultTableConfiguration(processThreadNestedNameConfig);

            // Process-Thread-EventName config
            var processThreadEventNameColumns = new List<ColumnConfiguration>(defaultColumns);
            processThreadEventNameColumns.Remove(EventNameColConfig);
            processThreadEventNameColumns.Insert(4, EventNameColConfig);
            var processThreadEventNameConfig = new TableConfiguration("Process-Thread-Name")
            {
                Columns = processThreadEventNameColumns,
            };
            SetGraphTableConfig(processThreadEventNameConfig);
            tableBuilder.AddTableConfiguration(processThreadEventNameConfig);

            // Process-Thread-Name by StartTime config
            var processThreadNameByStartTimeColumns = new List<ColumnConfiguration>(defaultColumns);
            processThreadNameByStartTimeColumns.Insert(4, ParentDepthLevelColConfig);
            processThreadNameByStartTimeColumns.Remove(EventNameColConfig);
            processThreadNameByStartTimeColumns.Insert(5, EventNameColConfig);
            processThreadNameByStartTimeColumns.Remove(EndTimestampColConfig);
            processThreadNameByStartTimeColumns.Insert(processThreadNameByStartTimeColumns.Count - 2, EndTimestampColConfig);

            var processThreadNameByStartTimeConfig = new TableConfiguration("Process-Thread-Name by Start Time")
            {
                Columns = processThreadNameByStartTimeColumns,
            };
            SetGraphTableConfig(processThreadNameByStartTimeConfig);
            tableBuilder.AddTableConfiguration(processThreadNameByStartTimeConfig);

            // Process-Thread-ParentTree config
            var processThreadNameTreeColumns = new List<ColumnConfiguration>(defaultColumns);
            processThreadNameTreeColumns.Insert(4, ParentEventNameTreeBranchColConfig);
            processThreadNameTreeColumns.Remove(DurationExSortedColConfig);
            processThreadNameTreeColumns.Insert(10, DurationExSortedSumColConfig);
            var processThreadParentNameTreeConfig = new TableConfiguration("Process-Thread-ParentTree")
            {
                Columns = processThreadNameTreeColumns,
            };
            ParentEventNameTreeBranchColConfig.DisplayHints.IsVisible = true;
            SetGraphTableConfig(processThreadParentNameTreeConfig);
            tableBuilder.AddTableConfiguration(processThreadParentNameTreeConfig);
        }

        private static void SetGraphTableConfig(TableConfiguration tableConfig)
        {
            tableConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColConfig.Metadata.Guid);
            tableConfig.AddColumnRole(ColumnRole.EndTime, EndTimestampColConfig.Metadata.Guid);
            tableConfig.AddColumnRole(ColumnRole.Duration, DurationNotSortedColConfig.Metadata.Guid);
        }
    }
}