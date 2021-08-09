// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using CtfPlayback.FieldValues;
using PerfDataExtensions.DataOutputTypes;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using Utilities;

namespace PerfDataExtensions.Tables
{
    [Table]
    [RequiresCooker("Perf/GenericEvents")]
    [PrebuiltConfigurationsFilePath("Resources\\GenericEventTablePrebuiltConfigurations.json")]
    public class GenericEventTable
    {
        public static TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{ca937336-1524-4663-9dde-4257d6287030}"),
            "Generic Events",
            "All events in the Perf trace",
            "Linux Perf");

        private static readonly ColumnConfiguration eventNameColumnConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{c3bad8d7-8693-4f85-b9ae-c7b33ab3da7d}"), "Name"),
            new UIHints
            {
                IsVisible = true,
                Width = 200,
            });

        private static readonly ColumnConfiguration eventTimestampColumnConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{5df47fff-1759-4a83-9f4d-91ae9dc3003e}"), "Timestamp"),
            new UIHints
            {
                IsVisible = true,
                Width = 100,
            });

        private static readonly ColumnConfiguration eventIdColumnConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{c0fd51b0-17e2-4d74-ac9b-90e69594bb31}"), "Id"),
            new UIHints
            {
                IsVisible = true,
                Width = 80,
            });

        private static readonly ColumnConfiguration cpuIdColumnConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{5f326fb1-c2c7-4320-ac1b-b615c0f55114}"), "CPU"),
            new UIHints
            {
                IsVisible = true,
                Width = 80,
            });

        private static readonly ColumnConfiguration countColumnConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{4aa752bf-c432-48b0-8449-6ac68a75d6f5}"), "Count"),
            new UIHints
            {
                IsVisible = true,
                Width = 80,
                AggregationMode = AggregationMode.Sum,
            });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            int maximumFieldCount = tableData.QueryOutput<int>(
                DataOutputPath.Create("Perf/GenericEvents/MaximumEventFieldCount"));

            var events = tableData.QueryOutput<ProcessedEventData<PerfGenericEvent>>(
                DataOutputPath.Create("Perf/GenericEvents/Events"));

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);

            var genericEventProjection = new EventProjection<PerfGenericEvent>(events);

            var eventNameColumn = new BaseDataColumn<string>(
                eventNameColumnConfig, 
                genericEventProjection.Compose((genericEvent) => genericEvent.EventName));
            tableGenerator.AddColumn(eventNameColumn);

            var eventIdColumn = new BaseDataColumn<uint>(
                eventIdColumnConfig, 
                genericEventProjection.Compose((genericEvent) => genericEvent.Id));
            tableGenerator.AddColumn(eventIdColumn);

            var cpuIdColumn = new BaseDataColumn<uint>(
                cpuIdColumnConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.CpuId));
            tableGenerator.AddColumn(cpuIdColumn);

            var eventTimestampColumn = new BaseDataColumn<Timestamp>(
                eventTimestampColumnConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.Timestamp));
            tableGenerator.AddColumn(eventTimestampColumn);

            tableGenerator.AddColumn(countColumnConfig, Projection.Constant(1));

            // Add the field columns, with column names depending on the given event
            for (int columnIndex = 0; columnIndex < maximumFieldCount; columnIndex++)
            {
                string fieldName = "Field " + (columnIndex + 1);

                var genericEventFieldProjection = new GenericEventFieldProjection(columnIndex, genericEventProjection);

                var genericEventFieldNameProjection =
                    genericEventFieldProjection.Compose((field) => field.HasValue ? field.Value.Name : string.Empty);

                // generate a column configuration
                var fieldColumnConfiguration = new ColumnConfiguration(
                    new ColumnMetadata(Common.GenerateGuidFromName(fieldName), fieldName, genericEventFieldNameProjection, fieldName),
                    new UIHints
                    {
                        IsVisible = true,
                        Width = 80,
                        TextAlignment = TextAlignment.Left,
                    });

                var genericEventFieldAsStringProjection =
                    genericEventFieldProjection.Compose((field) => field.HasValue ? field.Value.Value : string.Empty);

                tableGenerator.AddColumn(fieldColumnConfiguration, genericEventFieldAsStringProjection);
            }
        }
    }

    public struct GenericEventFieldProjection
        : IProjection<int, PerfGenericEventField?>
    {
        private readonly int fieldIndex;
        private readonly IProjection<int, PerfGenericEvent> genericEventProjection;

        public GenericEventFieldProjection(int fieldIndex, IProjection<int, PerfGenericEvent> genericEventProjection)
        {
            this.fieldIndex = fieldIndex;
            this.genericEventProjection = genericEventProjection;
        }

        /// <summary>
        /// Gets the type of the parameter of the function representing the selector.
        /// </summary>
        public Type SourceType => typeof(int);

        /// <summary>
        /// Gets the type of the values returned by the function representing the selector.
        /// </summary>
        public Type ResultType => typeof(PerfGenericEventField);

        public PerfGenericEventField? this[int value]
        {
            get
            {
                PerfGenericEvent genericEvent = this.genericEventProjection[value];

                if (this.fieldIndex < genericEvent.FieldCount)
                {
                    return genericEvent[this.fieldIndex];
                }

                return null;
            }
        }
    }

    public struct GenericEventFieldNameProjection
        : IProjection<int, string>
    {
        private readonly int fieldIndex;
        private readonly IProjection<int, CtfFieldValue> genericEventFieldProjection;

        public GenericEventFieldNameProjection(int fieldIndex, IProjection<int, CtfFieldValue> genericEventFieldProjection)
        {
            this.fieldIndex = fieldIndex;
            this.genericEventFieldProjection = genericEventFieldProjection;
        }

        /// <summary>
        /// Gets the type of the parameter of the function representing the selector.
        /// </summary>
        public Type SourceType => typeof(int);

        /// <summary>
        /// Gets the type of the values returned by the function representing the selector.
        /// </summary>
        public Type ResultType => typeof(string);

        public string this[int value]
        {
            get
            {

                var genericEventField = this.genericEventFieldProjection[value];
                if (genericEventField == null)
                {
                    return string.Empty;
                }

                return genericEventField.FieldName;
            }
        }
    }
}