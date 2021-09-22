// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using PerfettoCds.Pipeline.DataOutput;
using Microsoft.Performance.SDK;
using PerfettoCds.Pipeline.CompositeDataCookers;

namespace PerfettoCds.Pipeline.Tables
{
    [Table]
    public class PerfettoCpuSchedTable
    {
        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{db17169e-afe5-41f6-ba24-511af1d869f9}"),
            " CPU Scheduler Events", // Space at the start so it shows up alphabetically first in the table list
            "Displays CPU scheduling events for processes and threads",
            "Perfetto - System",
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.CpuSchedEventCookerPath }
        );

        private static readonly ColumnConfiguration ProcessNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{91a51bf2-85d6-4932-9df5-dc44445e8521}"), "Process", "Name of the process"),
            new UIHints { Width = 210 });

        private static readonly ColumnConfiguration ThreadNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{62c7c290-3803-4a1a-8bcb-a4f441dc35b6}"), "Thread", "Name of the thread"),
            new UIHints { Width = 210 });

        private static readonly ColumnConfiguration StartTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{9c242b6d-bc9a-440d-8eff-82b1b6571d38}"), "StartTimestamp", "Start timestamp for the event"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration EndTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{5d37669b-7ae3-471b-97b2-06b593565cd6}"), "EndTimestamp", "End timestamp for the event"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration DurationColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{1e1d2517-9bf9-4533-b00f-9744021dcf05}"), "Duration", "Duration of the event"),
            new UIHints { Width = 70 });

        private static readonly ColumnConfiguration CpuColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{704e6901-bc63-46b4-b426-c0642342c991}"), "Cpu", "The CPU this event happened on"),
            new UIHints { Width = 70, SortOrder = SortOrder.Ascending });

        private static readonly ColumnConfiguration EndStateColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{9f33703d-d2d6-49b1-8d0c-758f4a875d2b}"), "EndState", "Ending state of the event"),
            new UIHints { Width = 70 });

        private static readonly ColumnConfiguration PriorityColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{73984a25-99b1-43a9-8412-c57b55de5518}"), "Priority", "Priority of the event"),
            new UIHints { Width = 70 });

        private static readonly ColumnConfiguration PercentCpuUsageColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{4dda5bb8-3921-4122-9dec-3b3c5c2d95b0}"), "% CPU Usage") { IsPercent = true },
            new UIHints
            {
                IsVisible = true,
                Width = 100,
                TextAlignment = TextAlignment.Right,
                CellFormat = ColumnFormats.PercentFormat,
                AggregationMode = AggregationMode.Sum,
                SortOrder = SortOrder.Descending,
                SortPriority = 0,
            });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoCpuSchedEvent>>(
                new DataOutputPath(PerfettoPluginConstants.CpuSchedEventCookerPath, nameof(PerfettoCpuSchedEventCooker.CpuSchedEvents)));

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var baseProjection = Projection.Index(events);

            var startProjection = baseProjection.Compose(x => x.StartTimestamp);
            var endProjection = baseProjection.Compose(x => x.EndTimestamp);

            tableGenerator.AddColumn(CpuColumn, baseProjection.Compose(x => x.Cpu));
            tableGenerator.AddColumn(ProcessNameColumn, baseProjection.Compose(x => x.ProcessName));
            tableGenerator.AddColumn(ThreadNameColumn, baseProjection.Compose(x => x.ThreadName));
            tableGenerator.AddColumn(DurationColumn, baseProjection.Compose(x => x.Duration));
            tableGenerator.AddColumn(EndStateColumn, baseProjection.Compose(x => x.EndState));
            tableGenerator.AddColumn(PriorityColumn, baseProjection.Compose(x => x.Priority));
            tableGenerator.AddColumn(StartTimestampColumn, startProjection);
            tableGenerator.AddColumn(EndTimestampColumn, endProjection);

            // Create projections that are used for calculating CPU usage%
            var startProjectionClippedToViewport = Projection.ClipTimeToViewport.Create(startProjection);
            var endProjectionClippedToViewport = Projection.ClipTimeToViewport.Create(endProjection);

            IProjection<int, TimestampDelta> cpuUsageInViewportColumn = Projection.Select(
                    endProjectionClippedToViewport,
                    startProjectionClippedToViewport,
                    new ReduceTimeSinceLastDiff());

            var percentCpuUsageColumn = Projection.ViewportRelativePercent.Create(cpuUsageInViewportColumn);
            tableGenerator.AddColumn(PercentCpuUsageColumn, percentCpuUsageColumn);

            // We want to exclude the idle thread ('swapper' on Android/Linux) from the display because it messes up CPU usage and clutters
            // the scheduler view
            const string swapperIdleFilter = "[Thread]:=\"swapper (0)\"";

            var cpuSchedConfig = new TableConfiguration("CPU Scheduling")
            {
                Columns = new[]
                {
                    CpuColumn,
                    ProcessNameColumn,
                    ThreadNameColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    DurationColumn,
                    EndStateColumn,
                    PriorityColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    StartTimestampColumn,
                    EndTimestampColumn
                },
                Layout = TableLayoutStyle.GraphAndTable,
                InitialFilterShouldKeep = false, // This means we're not keeping what the filter matches
                InitialFilterQuery = swapperIdleFilter
            };
            cpuSchedConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            cpuSchedConfig.AddColumnRole(ColumnRole.EndTime, EndTimestampColumn.Metadata.Guid);
            cpuSchedConfig.AddColumnRole(ColumnRole.Duration, DurationColumn.Metadata.Guid);

            var perCpuUsageConfig = new TableConfiguration("Utilization by CPU")
            {
                Columns = new[]
                {
                    CpuColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    ProcessNameColumn,
                    ThreadNameColumn,
                    DurationColumn,
                    StartTimestampColumn,
                    EndTimestampColumn,
                    EndStateColumn,
                    PriorityColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    PercentCpuUsageColumn
                },
                Layout = TableLayoutStyle.GraphAndTable,
                InitialFilterShouldKeep = false, // This means we're not keeping what the filter matches
                InitialFilterQuery = swapperIdleFilter
            };
            perCpuUsageConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            perCpuUsageConfig.AddColumnRole(ColumnRole.EndTime, EndTimestampColumn.Metadata.Guid);
            perCpuUsageConfig.AddColumnRole(ColumnRole.Duration, DurationColumn.Metadata.Guid);

            var perProcessUsageConfig = new TableConfiguration("Utilization by Process, Thread")
            {
                Columns = new[]
                {
                    ProcessNameColumn,
                    ThreadNameColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    CpuColumn,
                    DurationColumn,
                    StartTimestampColumn,
                    EndTimestampColumn,
                    EndStateColumn,
                    PriorityColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    PercentCpuUsageColumn
                },
                Layout = TableLayoutStyle.GraphAndTable,
                InitialFilterShouldKeep = false, // This means we're not keeping what the filter matches
                InitialFilterQuery = swapperIdleFilter
            };
            perProcessUsageConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            perProcessUsageConfig.AddColumnRole(ColumnRole.EndTime, EndTimestampColumn.Metadata.Guid);
            perProcessUsageConfig.AddColumnRole(ColumnRole.Duration, DurationColumn.Metadata.Guid);

            tableBuilder
                .AddTableConfiguration(cpuSchedConfig)
                .AddTableConfiguration(perCpuUsageConfig)
                .AddTableConfiguration(perProcessUsageConfig)
                .SetDefaultTableConfiguration(perCpuUsageConfig);
        }

        struct ReduceTimeSinceLastDiff
            : IFunc<int, Timestamp, Timestamp, TimestampDelta>
        {
            public TimestampDelta Invoke(int value, Timestamp timeSinceLast1, Timestamp timeSinceLast2)
            {
                return timeSinceLast1 - timeSinceLast2;
            }
        }

    }
}
