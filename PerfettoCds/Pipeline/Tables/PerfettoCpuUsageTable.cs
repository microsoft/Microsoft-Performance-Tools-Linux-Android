// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;
using PerfettoCds.Pipeline.DataOutput;
using Microsoft.Performance.SDK;
using PerfettoCds.Pipeline.DataCookers;
using System.Linq;

namespace PerfettoCds.Pipeline.Tables
{
    [Table]
    public class PerfettoCpuUsageTable
    {
        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{cc2db5d6-5abb-4094-b8c0-475a2f4d9946}"),
            "Perfetto CPU Usage Events",
            "Displays CPU Usage scaling events and idle states for CPUs. Idle CPUs show a frequency of 0.",
            "Perfetto",
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.CpuUsageEventCookerPath }
        );

        private static readonly ColumnConfiguration CpuNumColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{c068970f-d5c6-4155-b265-9ffe96ab5965}"), "CpuCore", "Specific CPU core"),
            new UIHints { Width = 210, SortOrder = SortOrder.Ascending });

        private static readonly ColumnConfiguration StartTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{34ba18c1-2d44-4128-9617-43a3916415f2}"), "StartTimestamp", "Start timestamp for the frequency event"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration DurationColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{762bedbb-8828-41c0-9616-bc2489853909}"), "Duration", "Start timestamp for the frequency sample"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration TotalCpuUsagePercentColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{0d177de8-3dd2-4d0e-bd51-1ab60eb713e2}"), "CpuFrequency(Hz)", "Current frequency for this CPU. When idle, displays 0"),
            new UIHints
            {
                Width = 120,
                AggregationMode = AggregationMode.Max,
            });

        private static readonly ColumnConfiguration UserPercentColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{f6832dea-9b08-4f1c-ab92-d6c7a03f2d3d}"), "UserPercentColumn", "Current frequency for this CPU. When idle, displays 0"),
            new UIHints
            {
                Width = 120,
                AggregationMode = AggregationMode.Max,
            });
        private static readonly ColumnConfiguration UserNicePercentColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{e4bbf3fe-895e-4ea2-8fc3-bbdeef8b2b2a}"), "UserNicePercentColumn", "Current frequency for this CPU. When idle, displays 0"),
            new UIHints
            {
                Width = 120,
                AggregationMode = AggregationMode.Max,
            });
        private static readonly ColumnConfiguration SystemModePercentColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{914976e2-d812-4577-97ae-bf7edfc8b500}"), "SystemModePercentColumn", "Current frequency for this CPU. When idle, displays 0"),
            new UIHints
            {
                Width = 120,
                AggregationMode = AggregationMode.Max,
            });
        private static readonly ColumnConfiguration IdlePercentColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{ba945491-c77b-4fe1-b03a-0021d508c3a1}"), "IdlePercentColumn", "Current frequency for this CPU. When idle, displays 0"),
            new UIHints
            {
                Width = 120,
                AggregationMode = AggregationMode.Max,
            });
        private static readonly ColumnConfiguration IoWaitPercentColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{2ff90dae-14c0-4fe5-b28b-727eb1325993}"), "IoWaitPercentColumn", "Current frequency for this CPU. When idle, displays 0"),
            new UIHints
            {
                Width = 120,
                AggregationMode = AggregationMode.Max,
            });
        private static readonly ColumnConfiguration IrqPercentColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{951074d6-e32a-4144-9409-1f31aa0c3310}"), "IrqPercentColumn", "Current frequency for this CPU. When idle, displays 0"),
            new UIHints
            {
                Width = 120,
                AggregationMode = AggregationMode.Max,
            });
        private static readonly ColumnConfiguration SoftIrqPercentColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{bb18f4c8-2050-4a99-a79d-770d4c0e22e5}"), "SoftIrqPercentColumn", "Current frequency for this CPU. When idle, displays 0"),
            new UIHints
            {
                Width = 120,
                AggregationMode = AggregationMode.Max,
            });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoCpuUsageEvent>>(
                new DataOutputPath(PerfettoPluginConstants.CpuUsageEventCookerPath, nameof(PerfettoCpuUsageEventCooker.CpuUsageEvents)));

            // Start construction of the column order. Pivot on process and thread
            List<ColumnConfiguration> allColumns = new List<ColumnConfiguration>()
            {
                CpuNumColumn,
                TableConfiguration.PivotColumn, // Columns before this get pivotted on
                StartTimestampColumn,
                DurationColumn,
                UserPercentColumn,
                UserNicePercentColumn,
                SystemModePercentColumn,
                IdlePercentColumn,
                IoWaitPercentColumn,
                IrqPercentColumn,
                SoftIrqPercentColumn,
                TableConfiguration.GraphColumn, // Columns after this get graphed
                TotalCpuUsagePercentColumn
            };

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var baseProjection = Projection.Index(events);

            tableGenerator.AddColumn(CpuNumColumn, baseProjection.Compose(x => x.CpuNum));
            tableGenerator.AddColumn(StartTimestampColumn, baseProjection.Compose(x => x.StartTimestamp));
            tableGenerator.AddColumn(DurationColumn, baseProjection.Compose(x => x.Duration));
            tableGenerator.AddColumn(UserPercentColumn, baseProjection.Compose(x => x.UserPercent));
            tableGenerator.AddColumn(UserNicePercentColumn, baseProjection.Compose(x => x.UserNicePercent));
            tableGenerator.AddColumn(SystemModePercentColumn, baseProjection.Compose(x => x.SystemModePercent));
            tableGenerator.AddColumn(IdlePercentColumn, baseProjection.Compose(x => x.IdlePercent));
            tableGenerator.AddColumn(IoWaitPercentColumn, baseProjection.Compose(x => x.IoWaitPercent));
            tableGenerator.AddColumn(IrqPercentColumn, baseProjection.Compose(x => x.IrqPercent));
            tableGenerator.AddColumn(SoftIrqPercentColumn, baseProjection.Compose(x => x.SoftIrqPercent));
            tableGenerator.AddColumn(TotalCpuUsagePercentColumn, baseProjection.Compose(x => x.CpuPercent));

            // We are graphing CPU Usage + duration with MAX accumulation, which gives a steady line graph of the current CPU Usage
            var tableConfig = new TableConfiguration("Perfetto CPU Scheduling")
            {
                Columns = allColumns,
                Layout = TableLayoutStyle.GraphAndTable,
                ChartType = ChartType.Line
            };
            tableConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            tableConfig.AddColumnRole(ColumnRole.ResourceId, CpuNumColumn);
            tableConfig.AddColumnRole(ColumnRole.Duration, DurationColumn);

            tableBuilder.AddTableConfiguration(tableConfig).SetDefaultTableConfiguration(tableConfig);
        }
    }
}
