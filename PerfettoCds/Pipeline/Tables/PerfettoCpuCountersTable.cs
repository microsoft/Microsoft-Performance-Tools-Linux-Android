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
using PerfettoCds.Pipeline.CompositeDataCookers;
using System.Linq;

namespace PerfettoCds.Pipeline.Tables
{
    [Table]
    public class PerfettoCpuCountersTable
    {
        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{cc2db5d6-5abb-4094-b8c0-475a2f4d9946}"),
            "CPU Counters (coarse)",
            "Displays coarse CPU usage based on /proc/stat counters",
            "Perfetto - System",
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.CpuCountersEventCookerPath }
        );

        private static readonly ColumnConfiguration CpuNumColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{c068970f-d5c6-4155-b265-9ffe96ab5965}"), "CpuCore", "Specific CPU core"),
            new UIHints { Width = 210, SortOrder = SortOrder.Ascending });

        private static readonly ColumnConfiguration StartTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{34ba18c1-2d44-4128-9617-43a3916415f2}"), "StartTimestamp", "Start timestamp for the CPU counter event"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration DurationColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{762bedbb-8828-41c0-9616-bc2489853909}"), "Duration", "Duration of this CPU counter event (time to the next event)"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration TotalCpuUsagePercentColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{0d177de8-3dd2-4d0e-bd51-1ab60eb713e2}"), "TotalCpuUsage%", "Total CPU usage percent (all /proc/stat/ events together except idle)"),
            new UIHints
            {
                Width = 120,
                AggregationMode = AggregationMode.Max,
            });

        private static readonly ColumnConfiguration UserPercentColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{f6832dea-9b08-4f1c-ab92-d6c7a03f2d3d}"), "UserPercentColumn", "Total % time spent normal processing in user mode"),
            new UIHints
            {
                Width = 100,
                AggregationMode = AggregationMode.Max,
            });
        private static readonly ColumnConfiguration UserNicePercentColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{e4bbf3fe-895e-4ea2-8fc3-bbdeef8b2b2a}"), "UserNicePercentColumn", "Total % time spent with niced processes in user mode"),
            new UIHints
            {
                Width = 100,
                AggregationMode = AggregationMode.Max,
            });
        private static readonly ColumnConfiguration SystemModePercentColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{914976e2-d812-4577-97ae-bf7edfc8b500}"), "SystemModePercent", "Total % time spent running in system/kernel mode"),
            new UIHints
            {
                Width = 100,
                AggregationMode = AggregationMode.Max,
            });
        private static readonly ColumnConfiguration IdlePercentColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{ba945491-c77b-4fe1-b03a-0021d508c3a1}"), "IdlePercent", "Total % time spent in idle task"),
            new UIHints
            {
                Width = 100,
                AggregationMode = AggregationMode.Max,
            });
        private static readonly ColumnConfiguration IoWaitPercentColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{2ff90dae-14c0-4fe5-b28b-727eb1325993}"), "IoWaitPercent", "Total % time spent waiting for IO to complete"),
            new UIHints
            {
                Width = 100,
                AggregationMode = AggregationMode.Max,
            });
        private static readonly ColumnConfiguration IrqPercentColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{951074d6-e32a-4144-9409-1f31aa0c3310}"), "IrqPercent", "Total % time spent servicing interrupts"),
            new UIHints
            {
                Width = 100,
                AggregationMode = AggregationMode.Max,
            });
        private static readonly ColumnConfiguration SoftIrqPercentColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{bb18f4c8-2050-4a99-a79d-770d4c0e22e5}"), "SoftIrqPercent", "Total % time spent spent servicing softirqs"),
            new UIHints
            {
                Width = 100,
                AggregationMode = AggregationMode.Max,
            });
        private static readonly ColumnConfiguration CountColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{666c7a85-19fe-4ee6-99f3-abc0004e7d57}"), "Count", "Extra column used to create new pivots"),
            new UIHints
            {
                Width = 60,
            });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoCpuCountersEvent>>(
                new DataOutputPath(PerfettoPluginConstants.CpuCountersEventCookerPath, nameof(PerfettoCpuCountersEventCooker.CpuCountersEvents)));

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
            tableGenerator.AddColumn(CountColumn, Projection.Constant<int>(1));

            // Only display the total CPU usage column
            var cpuUsageConfig = new TableConfiguration("CPU Usage %")
            {
                Columns = new[]
                {
                    CpuNumColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    CountColumn,
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
                },
                Layout = TableLayoutStyle.GraphAndTable,
                ChartType = ChartType.Line
            };
            cpuUsageConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            cpuUsageConfig.AddColumnRole(ColumnRole.ResourceId, CpuNumColumn);
            cpuUsageConfig.AddColumnRole(ColumnRole.Duration, DurationColumn);

            // Display all CPU counter columns
            var allCountersConfig = new TableConfiguration("CPU Counters - All")
            {
                Columns = new[]
                {
                    CpuNumColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    CountColumn,
                    StartTimestampColumn,
                    DurationColumn,
                    TotalCpuUsagePercentColumn,
                    IdlePercentColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    UserPercentColumn,
                    UserNicePercentColumn,
                    SystemModePercentColumn,
                    IoWaitPercentColumn,
                    IrqPercentColumn,
                    SoftIrqPercentColumn,
                },
                Layout = TableLayoutStyle.GraphAndTable,
                ChartType = ChartType.Line
            };
            allCountersConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            allCountersConfig.AddColumnRole(ColumnRole.ResourceId, CpuNumColumn);
            allCountersConfig.AddColumnRole(ColumnRole.Duration, DurationColumn);

            tableBuilder.AddTableConfiguration(cpuUsageConfig)
                .AddTableConfiguration(allCountersConfig)
                .SetDefaultTableConfiguration(cpuUsageConfig);
        }
    }
}
