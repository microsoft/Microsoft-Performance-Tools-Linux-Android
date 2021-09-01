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
    public class PerfettoSystemMemoryTable
    {
        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{edbd3ddd-5610-4929-a85f-f9ca6eceb9b2}"),
            "Perfetto System Memory",
            "Displays system memory counts gathered from /proc/meminfo",
            "Perfetto - System",
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.SystemMemoryEventCookerPath }
        );

        private static readonly ColumnConfiguration MemoryTypeColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{ac84aa1d-ea66-46d2-8fc3-8ead853f81b4}"), "MemoryType", "Current frequency for this CPU. When idle, displays 0"),
            new UIHints { Width = 210, });
        private static readonly ColumnConfiguration MemoryValueColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{3ada3dda-2893-4366-b1a7-a5fe8344e17b}"), "MemoryValue", "Current frequency for this CPU. When idle, displays 0"),
            new UIHints { Width = 210,  AggregationMode = AggregationMode.Max});


        private static readonly ColumnConfiguration StartTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{6a9f870f-103c-461c-b909-9b098fe3695f}"), "StartTimestamp", "Start timestamp for the frequency event"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration DurationColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{bade2ff2-0a7c-4358-a736-058163739ae4}"), "Duration", "Start timestamp for the frequency sample"),
            new UIHints { Width = 120 });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoSystemMemoryEvent>>(
                new DataOutputPath(PerfettoPluginConstants.SystemMemoryEventCookerPath, nameof(PerfettoSystemMemoryEventCooker.SystemMemoryEvents)));

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var baseProjection = Projection.Index(events);

            tableGenerator.AddColumn(StartTimestampColumn, baseProjection.Compose(x => x.StartTimestamp));
            tableGenerator.AddColumn(MemoryTypeColumn, baseProjection.Compose(x => x.MemoryType));
            tableGenerator.AddColumn(MemoryValueColumn, baseProjection.Compose(x => x.Value));
            tableGenerator.AddColumn(DurationColumn, baseProjection.Compose(x => x.Duration));

            // Virtual
            var tableConfig = new TableConfiguration("Virtual")
            {
                Columns = new[]
                {
                    MemoryTypeColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    StartTimestampColumn,
                    DurationColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    MemoryValueColumn
                },
                Layout = TableLayoutStyle.GraphAndTable,
                ChartType = ChartType.Line
            };
            tableConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            //tableConfig.AddColumnRole(ColumnRole.ResourceId, MemoryTypeColumn);
            tableConfig.AddColumnRole(ColumnRole.Duration, DurationColumn);

            tableBuilder.AddTableConfiguration(tableConfig)
                .SetDefaultTableConfiguration(tableConfig);
        }
    }
}
