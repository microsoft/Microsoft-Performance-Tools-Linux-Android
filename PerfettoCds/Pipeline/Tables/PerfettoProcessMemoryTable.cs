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
    public class PerfettoProcessMemoryTable
    {
        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{80d5ef1d-a24f-472c-83be-707b03239d35}"),
            "Perfetto Process Memory",
            "Displays CPU frequency scaling events and idle states for CPUs. Idle CPUs show a frequency of 0.",
            "Perfetto",
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.ProcessMemoryEventCookerPath }
        );

        private static readonly ColumnConfiguration ValueColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{989b9a02-63ab-4ab4-b466-06671db4c61f}"), "Value", "Specific CPU core"),
            new UIHints { Width = 210, SortOrder = SortOrder.Ascending });

        private static readonly ColumnConfiguration ProcessNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{5f47812f-85ab-42e8-bae3-1e7bf377a689}"), "Process", "Current frequency for this CPU. When idle, displays 0"),
            new UIHints 
            { 
                Width = 210,
                AggregationMode = AggregationMode.Max,
            });

        private static readonly ColumnConfiguration StartTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{4b2c0e42-04ee-4e4f-916f-bf7065f34018}"), "StartTimestamp", "Start timestamp for the frequency event"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration DurationColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{1db8a444-3bcc-4787-bc4c-f8ffd25ccf98}"), "Duration", "Start timestamp for the frequency sample"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration MemoryTypeColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{e54510cb-b2e7-4f78-be95-e220e59eb3af}"), "MemoryType", "Indicates CPU change events. 'cpuidle' indicates a change in idle state. 'cpufreq' indicates a change of frequency"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration RssAnonColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{59db9b2a-09aa-42c5-9da7-631a507f0dbc}"), "RssAnon", "Indicates CPU change events. 'cpuidle' indicates a change in idle state. 'cpufreq' indicates a change of frequency"),
            new UIHints { Width = 120 });
        private static readonly ColumnConfiguration LockedColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{b9a65a94-f421-40cf-840e-74dffb84857f}"), "Locked", "Indicates CPU change events. 'cpuidle' indicates a change in idle state. 'cpufreq' indicates a change of frequency"),
            new UIHints { Width = 120 });
        private static readonly ColumnConfiguration RssShMemColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{2969f7a3-54b3-492c-a393-1bd937389bd2}"), "RssShMem", "Indicates CPU change events. 'cpuidle' indicates a change in idle state. 'cpufreq' indicates a change of frequency"),
            new UIHints { Width = 120 });
        private static readonly ColumnConfiguration RssFileColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{96a12428-279e-4ef7-830b-89268c1d90cf}"), "RssFile", "Indicates CPU change events. 'cpuidle' indicates a change in idle state. 'cpufreq' indicates a change of frequency"),
            new UIHints { Width = 120 });
        private static readonly ColumnConfiguration RssHwmColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{88f8d32e-c884-4263-9712-44166aee1f95}"), "RssHwm", "Indicates CPU change events. 'cpuidle' indicates a change in idle state. 'cpufreq' indicates a change of frequency"),
            new UIHints { Width = 120 });
        private static readonly ColumnConfiguration RssColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{1ea71b65-4a32-4fc0-8a87-273073a51aa9}"), "Rss", "Indicates CPU change events. 'cpuidle' indicates a change in idle state. 'cpufreq' indicates a change of frequency"),
            new UIHints { Width = 120 });
        private static readonly ColumnConfiguration SwapColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{f0ebf9e8-39a1-44b7-8bf0-5f11ff6a8089}"), "Swap", "Indicates CPU change events. 'cpuidle' indicates a change in idle state. 'cpufreq' indicates a change of frequency"),
            new UIHints { Width = 120 });
        private static readonly ColumnConfiguration VirtColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{83d575ba-2c24-46f7-901e-57241f72b918}"), "Virt", "Indicates CPU change events. 'cpuidle' indicates a change in idle state. 'cpufreq' indicates a change of frequency"),
            new UIHints { Width = 120 });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoProcessMemoryEvent>>(
                new DataOutputPath(PerfettoPluginConstants.ProcessMemoryEventCookerPath, nameof(PerfettoProcessMemoryEventCooker.ProcessMemoryEvents)));

            // Start construction of the column order. Pivot on process and thread


            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var baseProjection = Projection.Index(events);

            tableGenerator.AddColumn(ValueColumn, baseProjection.Compose(x => x.Value));
            tableGenerator.AddColumn(MemoryTypeColumn, baseProjection.Compose(x => x.MemoryType));
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

            // We are graphing CPU frequency + duration with MAX accumulation, which gives a steady line graph of the current CPU frequency
            var tableConfig = new TableConfiguration("Table 1")
            {
                Columns = new[]
                {
                    ProcessNameColumn,
                    MemoryTypeColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    StartTimestampColumn,
                    DurationColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    ValueColumn
                },
                Layout = TableLayoutStyle.GraphAndTable,
                ChartType = ChartType.Line
            };
            tableConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            tableConfig.AddColumnRole(ColumnRole.ResourceId, ProcessNameColumn); // TODO need this?
            tableConfig.AddColumnRole(ColumnRole.Duration, DurationColumn);

            var tableConfig2 = new TableConfiguration("Table 2")
            {
                Columns = new[]
                {
                    ProcessNameColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    StartTimestampColumn,
                    DurationColumn,
                    RssAnonColumn,
                    LockedColumn,
                    RssShMemColumn,
                    RssFileColumn,
                    RssHwmColumn,
                    RssColumn,
                    SwapColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    RssAnonColumn
                },
                Layout = TableLayoutStyle.GraphAndTable,
                ChartType = ChartType.Line
            };
            tableConfig2.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            tableConfig2.AddColumnRole(ColumnRole.ResourceId, ProcessNameColumn); // TODO need this?
            tableConfig2.AddColumnRole(ColumnRole.Duration, DurationColumn);

            tableBuilder.AddTableConfiguration(tableConfig).AddTableConfiguration(tableConfig2).SetDefaultTableConfiguration(tableConfig);
        }
    }
}
