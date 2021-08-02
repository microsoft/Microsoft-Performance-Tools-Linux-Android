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

namespace PerfettoCds.Pipeline.Tables
{
    [Table]
    public class PerfettoFtraceEventTable
    {
        // Set some sort of max to prevent ridiculous field counts
        public const int AbsoluteMaxFields = 20;

        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{96beb7a0-5a9e-4713-b1f7-4ee74d27851c}"),
            "Perfetto Ftrace Events",
            "All Ftrace events in the Perfetto trace",
            "Perfetto",
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.FtraceEventCookerPath }
        );

        // TODO update descriptions
        private static readonly ColumnConfiguration StartTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{e9675de9-4a76-4bba-a387-169c7ee38425}"), "StartTimestamp", "Start timestamp of the event"),
            new UIHints { Width = 180 });

        private static readonly ColumnConfiguration ProcessNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{8027964f-4c41-4309-ada1-b9a40d685b24}"), "ProcessName", "Name of the process that logged the event"),
            new UIHints { Width = 210 });
        
        private static readonly ColumnConfiguration ThreadNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{276ab2ad-722c-4a1b-8d9f-dc7b562d3a5c}"), "ThreadName", "Name of the thread that logged the event"),
            new UIHints { Width = 210 });

        private static readonly ColumnConfiguration CpuColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{16b7cf75-de7c-4cb7-9d72-3302a1cdf54f}"), "Cpu", "CPU"),
            new UIHints { Width = 150 });

        private static readonly ColumnConfiguration NameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{ea581f83-b632-4b5b-9a89-844994f497ca}"), "Name", "Name of the Ftrace event"),
            new UIHints { Width = 120 });


        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoFtraceEvent>>(
                new DataOutputPath(PerfettoPluginConstants.FtraceEventCookerPath, nameof(PerfettoFtraceEventCooker.FtraceEvents)));

            // Start construction of the column order. Pivot on process and thread
            List<ColumnConfiguration> allColumns = new List<ColumnConfiguration>() 
            {
                CpuColumn,
                TableConfiguration.PivotColumn, // Columns before this get pivotted
                ProcessNameColumn,
                ThreadNameColumn,
                NameColumn,
                TableConfiguration.GraphColumn, // Columns after this get graphed
                StartTimestampColumn
            };

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var baseProjection = Projection.Index(events);

            tableGenerator.AddColumn(StartTimestampColumn, baseProjection.Compose(x => x.StartTimestamp));
            tableGenerator.AddColumn(ProcessNameColumn, baseProjection.Compose(x => x.ProcessName));
            tableGenerator.AddColumn(ThreadNameColumn, baseProjection.Compose(x => x.ThreadName));
            tableGenerator.AddColumn(CpuColumn, baseProjection.Compose(x => x.Cpu));
            tableGenerator.AddColumn(NameColumn, baseProjection.Compose(x => x.Name));

            var tableConfig = new TableConfiguration("Perfetto Logcat Events")
            {
                Columns = allColumns,
                Layout = TableLayoutStyle.GraphAndTable
            };
            tableConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);

            tableBuilder.AddTableConfiguration(tableConfig).SetDefaultTableConfiguration(tableConfig);
        }
    }
}
