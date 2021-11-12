// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using PerfettoCds.Pipeline.CompositeDataCookers;
using PerfettoCds.Pipeline.DataOutput;

namespace PerfettoCds.Pipeline.Tables
{
    [Table]
    public class PerfettoGpuCountersTable
    {
        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{82a4f9c4-b0c6-4583-9610-5b95aaad6346}"),
            "GPU Counters",
            "Displays various GPU counters for this trace",
            "Perfetto - System",
            defaultLayout: TableLayoutStyle.GraphAndTable,
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.GpuCountersEventCookerPath }
        );

        private static readonly ColumnConfiguration NameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{baf56e72-21e0-4e51-80d0-a24bc44dc818}"), "GpuCounter", "Name/type of the GPU counter"),
            new UIHints { Width = 210, SortOrder = SortOrder.Ascending });

        private static readonly ColumnConfiguration ValueColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{e4621c17-5ba9-44ce-b2d5-72f7adf546e1}"), "Value", "Value for this counter at this point in time"),
            new UIHints { Width = 210, AggregationMode = AggregationMode.Max });

        private static readonly ColumnConfiguration StartTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{5881324c-ce05-4d7f-8c8c-473fa436f99d}"), "StartTimestamp", "Start timestamp for the GPU event"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration DurationColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{8f132c9d-af37-47d7-851f-97d2e8a6934d}"), "Duration", "Start timestamp for the GPU event"),
            new UIHints { Width = 120 });

        public static bool IsDataAvailable(IDataExtensionRetrieval tableData)
        {
            return tableData.QueryOutput<ProcessedEventData<PerfettoGpuCountersEvent>>(
                new DataOutputPath(PerfettoPluginConstants.GpuCountersEventCookerPath, nameof(PerfettoGpuCountersEventCooker.GpuCountersEvents))).Any();
        }

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoGpuCountersEvent>>(
                new DataOutputPath(PerfettoPluginConstants.GpuCountersEventCookerPath, nameof(PerfettoGpuCountersEventCooker.GpuCountersEvents)));

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var baseProjection = Projection.Index(events);

            tableGenerator.AddColumn(NameColumn, baseProjection.Compose(x => x.Name));
            tableGenerator.AddColumn(ValueColumn, baseProjection.Compose(x => x.Value));
            tableGenerator.AddColumn(StartTimestampColumn, baseProjection.Compose(x => x.StartTimestamp));
            tableGenerator.AddColumn(DurationColumn, baseProjection.Compose(x => x.Duration));

            var tableConfig = new TableConfiguration("GPU Counters")
            {
                Columns = new[]
                {
                    NameColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    StartTimestampColumn,
                    DurationColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    ValueColumn
                },
                ChartType = ChartType.Line
            };

            tableConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            tableConfig.AddColumnRole(ColumnRole.Duration, DurationColumn);

            tableBuilder
                .AddTableConfiguration(tableConfig)
                .SetDefaultTableConfiguration(tableConfig);
        }
    }
}
