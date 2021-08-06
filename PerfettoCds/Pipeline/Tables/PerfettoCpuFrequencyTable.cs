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
    public class PerfettoCpuFrequencyTable
    {
        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{5b9689d4-617c-484c-9b0a-c7242565ec13}"),
            "Perfetto CPU Frequency Events",
            "Displays CPU frequency scaling events and idle states for CPUs",
            "Perfetto",
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.CpuFrequencyEventCookerPath }
        );

        private static readonly ColumnConfiguration CpuNumColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{cae82fa3-65c6-43b3-8fc8-2e94c17840bd}"), "CpuCore", "Specific CPU core"),
            new UIHints { Width = 210, SortOrder = SortOrder.Ascending });

        private static readonly ColumnConfiguration CpuFrequencyColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{cb753d1a-2c97-414a-9985-06509b6f8ba3}"), "CpuFrequency(Hz)", "Current frequency for this CPU. When idle, displays 0"),
            new UIHints 
            { 
                Width = 210,
                AggregationMode = AggregationMode.Max,
            });

        private static readonly ColumnConfiguration StartTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{196f823d-646c-4bd2-a263-3fb2c5110f74}"), "StartTimestamp", "Start timestamp for the frequency event"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration DurationColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{d42adc27-b3d4-41af-8917-baee8d9f0f21}"), "Duration", "Start timestamp for the frequency sample"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration CpuStateColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{009c2448-eec0-47b4-a6fe-6ef19ab136f7}"), "CpuState", "Indicates CPU change events. 'cpuidle' indicates a change in idle state. 'cpufreq' indicates a change of frequency"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration IsIdleColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{1b06c328-0d33-49bb-a1fc-381a4b447493}"), "IsIdle", "Whether or not this CPU is idle"),
            new UIHints { Width = 120 });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoCpuFrequencyEvent>>(
                new DataOutputPath(PerfettoPluginConstants.CpuFrequencyEventCookerPath, nameof(PerfettoCpuFrequencyEventCooker.CpuFrequencyEvents)));

            // Start construction of the column order. Pivot on process and thread
            List<ColumnConfiguration> allColumns = new List<ColumnConfiguration>()
            {
                CpuNumColumn,
                TableConfiguration.PivotColumn, // Columns before this get pivotted on
                StartTimestampColumn,
                CpuStateColumn,
                DurationColumn,
                IsIdleColumn,
                TableConfiguration.GraphColumn, // Columns after this get graphed
                CpuFrequencyColumn
            };

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var baseProjection = Projection.Index(events);

            tableGenerator.AddColumn(CpuNumColumn, baseProjection.Compose(x => x.CpuNum));
            tableGenerator.AddColumn(CpuFrequencyColumn, baseProjection.Compose(x => x.CpuFrequency));
            tableGenerator.AddColumn(StartTimestampColumn, baseProjection.Compose(x => x.StartTimestamp));
            tableGenerator.AddColumn(CpuStateColumn, baseProjection.Compose(x => x.Name));
            tableGenerator.AddColumn(DurationColumn, baseProjection.Compose(x => x.Duration));
            tableGenerator.AddColumn(IsIdleColumn, baseProjection.Compose(x => x.IsIdle));

            // We are graphing CPU frequency + duration with MAX accumulation, which gives a steady line graph of the current CPU frequency
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
