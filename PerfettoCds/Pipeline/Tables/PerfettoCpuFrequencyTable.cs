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
            "Displays CPU scheduling events for processes and threads", // TODO
            "Perfetto",
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.CpuFrequencyEventCookerPath }
        );

        private static readonly ColumnConfiguration CpuNumColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{91a51bf2-85d6-4932-9df5-dc44445e8521}"), "CPU", "CPU number"),
            new UIHints { Width = 210 });

        private static readonly ColumnConfiguration CpuFrequencyColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{62c7c290-3803-4a1a-8bcb-a4f441dc35b6}"), "CPU Frequency", "CPU frequency"),
            new UIHints 
            { 
                Width = 210,
                AggregationMode = AggregationMode.Sum,
            });

        private static readonly ColumnConfiguration StartTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{9c242b6d-bc9a-440d-8eff-82b1b6571d38}"), "StartTimestamp", "Start timestamp for the frequency sample"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration DurationColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{9601099b-21d5-4d7b-8d25-39b70ca8e6ed}"), "Duration", "Start timestamp for the frequency sample"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration CpuStateColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{5d37669b-7ae3-471b-97b2-06b593565cd6}"), "CpuState", "CPU state for the frequency sample"),
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

            var tableConfig = new TableConfiguration("Perfetto CPU Scheduling")
            {
                Columns = allColumns,
                Layout = TableLayoutStyle.GraphAndTable
            };
            tableConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            tableConfig.AddColumnRole(ColumnRole.ResourceId, CpuNumColumn);
            tableConfig.AddColumnRole(ColumnRole.Duration, DurationColumn);

            tableBuilder.AddTableConfiguration(tableConfig).SetDefaultTableConfiguration(tableConfig);
        }
    }
}
