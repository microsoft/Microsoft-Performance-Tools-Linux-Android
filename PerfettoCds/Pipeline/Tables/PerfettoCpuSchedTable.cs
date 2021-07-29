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
    public class PerfettoCpuSchedTable
    {
        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{db17169e-afe5-41f6-ba24-511af1d869f9}"),
            "Perfetto CPU Scheduler Events",
            "Displays CPU scheduling events for processes and threads",
            "Perfetto",
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

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoCpuSchedEvent>>(
                new DataOutputPath(PerfettoPluginConstants.CpuSchedEventCookerPath, nameof(PerfettoCpuSchedEventCooker.CpuSchedEvents)));

            var test = events.OrderBy(x => x.Cpu);

            // Start construction of the column order. Pivot on process and thread
            List<ColumnConfiguration> allColumns = new List<ColumnConfiguration>()
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
            };

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var baseProjection = Projection.Index(events);

            tableGenerator.AddColumn(CpuColumn, baseProjection.Compose(x => x.Cpu));
            tableGenerator.AddColumn(ProcessNameColumn, baseProjection.Compose(x => x.ProcessName));
            tableGenerator.AddColumn(ThreadNameColumn, baseProjection.Compose(x => x.ThreadName));
            tableGenerator.AddColumn(DurationColumn, baseProjection.Compose(x => x.Duration));
            tableGenerator.AddColumn(EndStateColumn, baseProjection.Compose(x => x.EndState));
            tableGenerator.AddColumn(PriorityColumn, baseProjection.Compose(x => x.Priority));
            tableGenerator.AddColumn(StartTimestampColumn, baseProjection.Compose(x => x.StartTimestamp));
            tableGenerator.AddColumn(EndTimestampColumn, baseProjection.Compose(x => x.EndTimestamp));

            var tableConfig = new TableConfiguration("Perfetto CPU Scheduling")
            {
                Columns = allColumns,
                Layout = TableLayoutStyle.GraphAndTable
            };
            tableConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            tableConfig.AddColumnRole(ColumnRole.EndTime, EndTimestampColumn.Metadata.Guid);
            tableConfig.AddColumnRole(ColumnRole.Duration, DurationColumn.Metadata.Guid);

            tableBuilder.AddTableConfiguration(tableConfig).SetDefaultTableConfiguration(tableConfig);
        }
    }
}
