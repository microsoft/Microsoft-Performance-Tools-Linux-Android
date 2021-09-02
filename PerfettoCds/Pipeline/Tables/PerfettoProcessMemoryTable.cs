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
    public class PerfettoProcessMemoryTable
    {
        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{80d5ef1d-a24f-472c-83be-707b03239d35}"),
            "Perfetto Process Memory",
            "Displays per process memory counts gathered from /proc/<pid>/status",
            "Perfetto - System",
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.ProcessMemoryEventCookerPath }
        );

        private static readonly ColumnConfiguration ProcessNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{5f47812f-85ab-42e8-bae3-1e7bf377a689}"), "Process", "Process name"),
            new UIHints { Width = 210 });
        private static readonly ColumnConfiguration StartTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{4b2c0e42-04ee-4e4f-916f-bf7065f34018}"), "StartTimestamp", "Start timestamp for the memory event"),
            new UIHints { Width = 120 });
        private static readonly ColumnConfiguration DurationColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{1db8a444-3bcc-4787-bc4c-f8ffd25ccf98}"), "Duration", "Start timestamp for the memory sample"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration RssAnonColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{59db9b2a-09aa-42c5-9da7-631a507f0dbc}"), "RssAnonymous(kb)", "Resident set size - anonymous memory"),
            new UIHints { Width = 120, AggregationMode = AggregationMode.Max });
        private static readonly ColumnConfiguration RssShMemColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{2969f7a3-54b3-492c-a393-1bd937389bd2}"), "RssSharedMem(kb)", "Resident set size - shared memory"),
            new UIHints { Width = 120, AggregationMode = AggregationMode.Max });
        private static readonly ColumnConfiguration RssFileColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{96a12428-279e-4ef7-830b-89268c1d90cf}"), "RssFile(kb)", "Resident set size - file mappings"),
            new UIHints { Width = 120, AggregationMode = AggregationMode.Max });
        private static readonly ColumnConfiguration RssHwmColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{88f8d32e-c884-4263-9712-44166aee1f95}"), "RssHighWatermark(kb)", "Resident set size - peak (high water mark)"),
            new UIHints { Width = 120, AggregationMode = AggregationMode.Max });
        private static readonly ColumnConfiguration RssColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{1ea71b65-4a32-4fc0-8a87-273073a51aa9}"), "Rss(kb)", "Resident set size - sum of anon, file, shared mem"),
            new UIHints { Width = 120, AggregationMode = AggregationMode.Max });
        private static readonly ColumnConfiguration LockedColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{b9a65a94-f421-40cf-840e-74dffb84857f}"), "Locked(kb)", "Locked memory size"),
            new UIHints { Width = 120, AggregationMode = AggregationMode.Max });
        private static readonly ColumnConfiguration SwapColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{f0ebf9e8-39a1-44b7-8bf0-5f11ff6a8089}"), "Swap(kb)", "Swapped out VM size by anonymous private pages"),
            new UIHints { Width = 120, AggregationMode = AggregationMode.Max });
        private static readonly ColumnConfiguration VirtColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{83d575ba-2c24-46f7-901e-57241f72b918}"), "Virtual(kb)", "Peak virtual memory size"),
            new UIHints { Width = 120, AggregationMode = AggregationMode.Max });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoProcessMemoryEvent>>(
                new DataOutputPath(PerfettoPluginConstants.ProcessMemoryEventCookerPath, nameof(PerfettoProcessMemoryEventCooker.ProcessMemoryEvents)));

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var baseProjection = Projection.Index(events);

            tableGenerator.AddColumn(StartTimestampColumn, baseProjection.Compose(x => x.StartTimestamp));
            tableGenerator.AddColumn(ProcessNameColumn, baseProjection.Compose(x => x.ProcessName));
            tableGenerator.AddColumn(DurationColumn, baseProjection.Compose(x => x.Duration));

            tableGenerator.AddColumn(RssAnonColumn, baseProjection.Compose(x => x.RssAnon));
            tableGenerator.AddColumn(LockedColumn, baseProjection.Compose(x => x.Locked));
            tableGenerator.AddColumn(RssShMemColumn, baseProjection.Compose(x => x.RssShMem));
            tableGenerator.AddColumn(RssFileColumn, baseProjection.Compose(x => x.RssFile));
            tableGenerator.AddColumn(RssHwmColumn, baseProjection.Compose(x => x.RssHwm));
            tableGenerator.AddColumn(RssColumn, baseProjection.Compose(x => x.Rss));
            tableGenerator.AddColumn(SwapColumn, baseProjection.Compose(x => x.Swap));
            tableGenerator.AddColumn(VirtColumn, baseProjection.Compose(x => x.Virt));

            // Virtual
            var virtConfig = new TableConfiguration("Virtual")
            {
                Columns = new[]
                {
                    ProcessNameColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    StartTimestampColumn,
                    DurationColumn,
                    RssAnonColumn,
                    RssShMemColumn,
                    RssFileColumn,
                    RssHwmColumn,
                    RssColumn,
                    LockedColumn,
                    SwapColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    VirtColumn
                },
                Layout = TableLayoutStyle.GraphAndTable,
                ChartType = ChartType.Line
            };
            virtConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            virtConfig.AddColumnRole(ColumnRole.ResourceId, ProcessNameColumn);
            virtConfig.AddColumnRole(ColumnRole.Duration, DurationColumn);

            // Swap
            var swapConfig = new TableConfiguration("Swap")
            {
                Columns = new[]
                {
                    ProcessNameColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    StartTimestampColumn,
                    DurationColumn,
                    RssAnonColumn,
                    RssShMemColumn,
                    RssFileColumn,
                    RssHwmColumn,
                    RssColumn,
                    LockedColumn,
                    VirtColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    SwapColumn
                },
                Layout = TableLayoutStyle.GraphAndTable,
                ChartType = ChartType.Line
            };
            swapConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            swapConfig.AddColumnRole(ColumnRole.ResourceId, ProcessNameColumn);
            swapConfig.AddColumnRole(ColumnRole.Duration, DurationColumn);

            // Locked
            var lockedConfig = new TableConfiguration("Locked")
            {
                Columns = new[]
                {
                    ProcessNameColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    StartTimestampColumn,
                    DurationColumn,
                    RssAnonColumn,
                    RssShMemColumn,
                    RssFileColumn,
                    RssHwmColumn,
                    RssColumn,
                    SwapColumn,
                    VirtColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    LockedColumn
                },
                Layout = TableLayoutStyle.GraphAndTable,
                ChartType = ChartType.Line
            };
            lockedConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            lockedConfig.AddColumnRole(ColumnRole.ResourceId, ProcessNameColumn);
            lockedConfig.AddColumnRole(ColumnRole.Duration, DurationColumn);

            // Rss
            var rssConfig = new TableConfiguration("RSS (sum of anon, file, shared mem)")
            {
                Columns = new[]
                {
                    ProcessNameColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    StartTimestampColumn,
                    DurationColumn,
                    RssAnonColumn,
                    RssShMemColumn,
                    RssFileColumn,
                    RssHwmColumn,
                    LockedColumn,
                    SwapColumn,
                    VirtColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    RssColumn
                },
                Layout = TableLayoutStyle.GraphAndTable,
                ChartType = ChartType.Line
            };
            rssConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            rssConfig.AddColumnRole(ColumnRole.ResourceId, ProcessNameColumn);
            rssConfig.AddColumnRole(ColumnRole.Duration, DurationColumn);

            // rssHwm
            var rssHwmConfig = new TableConfiguration("RSS Peak (high water mark)")
            {
                Columns = new[]
                {
                    ProcessNameColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    StartTimestampColumn,
                    DurationColumn,
                    RssAnonColumn,
                    RssShMemColumn,
                    RssFileColumn,
                    RssColumn,
                    LockedColumn,
                    SwapColumn,
                    VirtColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    RssHwmColumn
                },
                Layout = TableLayoutStyle.GraphAndTable,
                ChartType = ChartType.Line
            };
            rssHwmConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            rssHwmConfig.AddColumnRole(ColumnRole.ResourceId, ProcessNameColumn);
            rssHwmConfig.AddColumnRole(ColumnRole.Duration, DurationColumn);

            // rssFile
            var rssFileConfig = new TableConfiguration("RSS File")
            {
                Columns = new[]
                {
                    ProcessNameColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    StartTimestampColumn,
                    DurationColumn,
                    RssAnonColumn,
                    RssShMemColumn,
                    RssHwmColumn,
                    RssColumn,
                    LockedColumn,
                    SwapColumn,
                    VirtColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    RssFileColumn
                },
                Layout = TableLayoutStyle.GraphAndTable,
                ChartType = ChartType.Line
            };
            rssFileConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            rssFileConfig.AddColumnRole(ColumnRole.ResourceId, ProcessNameColumn);
            rssFileConfig.AddColumnRole(ColumnRole.Duration, DurationColumn);

            // rssShMem
            var rssShMemConfig = new TableConfiguration("RSS Shared Memory")
            {
                Columns = new[]
                {
                    ProcessNameColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    StartTimestampColumn,
                    DurationColumn,
                    RssAnonColumn,
                    RssFileColumn,
                    RssHwmColumn,
                    RssColumn,
                    LockedColumn,
                    SwapColumn,
                    VirtColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    RssShMemColumn
                },
                Layout = TableLayoutStyle.GraphAndTable,
                ChartType = ChartType.Line
            };
            rssShMemConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            rssShMemConfig.AddColumnRole(ColumnRole.ResourceId, ProcessNameColumn);
            rssShMemConfig.AddColumnRole(ColumnRole.Duration, DurationColumn);

            // rssAnon
            var rssAnonConfig = new TableConfiguration("RSS Anonymous")
            {
                Columns = new[]
                {
                    ProcessNameColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    StartTimestampColumn,
                    DurationColumn,
                    RssShMemColumn,
                    RssFileColumn,
                    RssHwmColumn,
                    RssColumn,
                    LockedColumn,
                    SwapColumn,
                    VirtColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    RssAnonColumn
                },
                Layout = TableLayoutStyle.GraphAndTable,
                ChartType = ChartType.Line
            };
            rssAnonConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            rssAnonConfig.AddColumnRole(ColumnRole.ResourceId, ProcessNameColumn);
            rssAnonConfig.AddColumnRole(ColumnRole.Duration, DurationColumn);

            tableBuilder.AddTableConfiguration(virtConfig)
                .AddTableConfiguration(virtConfig)
                .AddTableConfiguration(swapConfig)
                .AddTableConfiguration(lockedConfig)
                .AddTableConfiguration(rssConfig)
                .AddTableConfiguration(rssHwmConfig)
                .AddTableConfiguration(rssFileConfig)
                .AddTableConfiguration(rssShMemConfig)
                .AddTableConfiguration(rssAnonConfig)
                .SetDefaultTableConfiguration(virtConfig);
        }
    }
}
