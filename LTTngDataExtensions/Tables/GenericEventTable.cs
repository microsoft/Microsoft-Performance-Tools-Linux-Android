// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using LTTngCds.CookerData;
using LTTngDataExtensions.DataOutputTypes;
using LTTngDataExtensions.SourceDataCookers;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using Utilities;

namespace LTTngDataExtensions.Tables
{
    [Table]
    [RequiresSourceCooker(LTTngConstants.SourceId, LTTngGenericEventDataCooker.Identifier)]
    [PrebuiltConfigurationsFilePath("Resources\\GenericEventTablePrebuiltConfigurations.json")]
    public class GenericEventTable
    {
        public static TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{F5EAF336-EA19-487D-9C1F-527812B02F30}"),
            "Generic Events",
            "All events in the LTTng trace",
            "Linux LTTng");

        private static readonly ColumnConfiguration eventNameColumnConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{8132DED0-8FE7-4533-B139-4C81133A7BCD}"), "Name"),
            new UIHints
            {
                IsVisible = true,
                Width = 200,
            });

        private static readonly ColumnConfiguration eventTimestampColumnConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{F74E122E-2358-4C9C-B9F7-76668B7AD957}"), "Timestamp"),
            new UIHints
            {
                IsVisible = true,
                Width = 100,
            });

        private static readonly ColumnConfiguration eventIdColumnConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{F81236AC-45B0-4695-95DA-4739A8DD0147}"), "Id"),
            new UIHints
            {
                IsVisible = true,
                Width = 80,
            });

        private static readonly ColumnConfiguration cpuIdColumnConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{F1DBA380-E2F3-47D2-8E5E-D753B28D13DD}"), "CPU"),
            new UIHints
            {
                IsVisible = true,
                Width = 80,
            });

        private static readonly ColumnConfiguration discardedEventsColumnConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{877222E9-BF0E-49FF-9506-45091A8C518B}"), "Discarded Events Count"),
            new UIHints
            {
                IsVisible = true,
                Width = 80,
            });

        private static readonly ColumnConfiguration countColumnConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{1D8946DA-567E-4A32-BB46-EB90727DA59C}"), "Count"),
            new UIHints
            {
                IsVisible = true,
                Width = 80,
                AggregationMode = AggregationMode.Sum,
            });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            int maximumFieldCount = tableData.QueryOutput<int>(
                DataOutputPath.ForSource(LTTngConstants.SourceId, LTTngGenericEventDataCooker.Identifier, nameof(LTTngGenericEventDataCooker.MaximumEventFieldCount)));

            var events = tableData.QueryOutput<ProcessedEventData<LTTngGenericEvent>>(
                DataOutputPath.ForSource(LTTngConstants.SourceId, LTTngGenericEventDataCooker.Identifier, nameof(LTTngGenericEventDataCooker.Events)));

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);

            var genericEventProjection = new EventProjection<LTTngGenericEvent>(events);

            var eventNameColumn = new DataColumn<string>(
                eventNameColumnConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.EventName));
            tableGenerator.AddColumn(eventNameColumn);

            var eventIdColumn = new DataColumn<uint>(
                eventIdColumnConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.Id));
            tableGenerator.AddColumn(eventIdColumn);

            var cpuIdColumn = new DataColumn<uint>(
                cpuIdColumnConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.CpuId));
            tableGenerator.AddColumn(cpuIdColumn);

            var discardedEventsColumn = new DataColumn<uint>(
                discardedEventsColumnConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.DiscardedEvents));
            tableGenerator.AddColumn(discardedEventsColumn);

            var eventTimestampColumn = new DataColumn<Timestamp>(
                eventTimestampColumnConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.Timestamp));
            tableGenerator.AddColumn(eventTimestampColumn);

            tableGenerator.AddColumn(countColumnConfig, Projection.Constant(1));

            // Add the field columns, with column names depending on the given event
            for (int index = 0; index < maximumFieldCount; index++)
            {
                var colIndex = index;  // This seems unncessary but causes odd runtime behavior if not done this way. Compiler is confused perhaps because w/o this func will index=genericEvent.FieldNames.Count every time. index is passed as ref but colIndex as value into func
                string fieldName = "Field " + (colIndex + 1);

                var genericEventFieldNameProjection = genericEventProjection.Compose((genericEvent) => colIndex < genericEvent.FieldNames.Count ? genericEvent.FieldNames[colIndex] : string.Empty);

                // generate a column configuration
                var fieldColumnConfiguration = new ColumnConfiguration(
                        new ColumnMetadata(Common.GenerateGuidFromName(fieldName), fieldName, genericEventFieldNameProjection, fieldName),
                        new UIHints
                        {
                            IsVisible = true,
                            Width = 80,
                            TextAlignment = TextAlignment.Left,
                        });

                var genericEventFieldAsStringProjection = genericEventProjection.Compose((genericEvent) => colIndex < genericEvent.FieldNames.Count ? genericEvent.FieldValues[colIndex] : string.Empty);

                tableGenerator.AddColumn(fieldColumnConfiguration, genericEventFieldAsStringProjection);
            }

        }
    }
}