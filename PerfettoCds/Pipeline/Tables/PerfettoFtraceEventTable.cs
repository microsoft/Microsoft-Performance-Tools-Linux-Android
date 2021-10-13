﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using PerfettoCds.Pipeline.DataOutput;
using Microsoft.Performance.SDK;
using PerfettoCds.Pipeline.CompositeDataCookers;
using Utilities;

namespace PerfettoCds.Pipeline.Tables
{
    [Table]
    public class PerfettoFtraceEventTable
    {
        // Set some sort of max to prevent ridiculous field counts
        public const int AbsoluteMaxFields = 20;

        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{96beb7a0-5a9e-4713-b1f7-4ee74d27851c}"),
            "Ftrace Events",
            "All Ftrace events in the Perfetto trace",
            "Perfetto - Events",
            defaultLayout: TableLayoutStyle.GraphAndTable,
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.FtraceEventCookerPath }
        );

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
            new ColumnMetadata(new Guid("{16b7cf75-de7c-4cb7-9d72-3302a1cdf54f}"), "CpuCore", "Specific CPU core"),
            new UIHints { Width = 150, SortOrder = SortOrder.Ascending });

        private static readonly ColumnConfiguration NameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{ea581f83-b632-4b5b-9a89-844994f497ca}"), "Name", "Name of the Ftrace event"),
            new UIHints { Width = 120 });


        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // We dynamically adjust the column headers
            // This is the max number of fields we can expect for this table
            int maxFieldCount = Math.Min(AbsoluteMaxFields, tableData.QueryOutput<int>(
                new DataOutputPath(PerfettoPluginConstants.FtraceEventCookerPath, nameof(PerfettoFtraceEventCooker.MaximumEventFieldCount))));

            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoFtraceEvent>>(
                new DataOutputPath(PerfettoPluginConstants.FtraceEventCookerPath, nameof(PerfettoFtraceEventCooker.FtraceEvents)));

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var eventProjection = new EventProjection<PerfettoFtraceEvent>(events);

            var processNameColumn = new BaseDataColumn<string>(
                ProcessNameColumn,
                eventProjection.Compose((ftraceEvent) => ftraceEvent.ProcessName));
            tableGenerator.AddColumn(processNameColumn);

            var threadNameColumn = new BaseDataColumn<string>(
                ThreadNameColumn,
                eventProjection.Compose((ftraceEvent) => ftraceEvent.ThreadName));
            tableGenerator.AddColumn(threadNameColumn);

            var startTimestampColumn = new BaseDataColumn<Timestamp>(
                StartTimestampColumn,
                eventProjection.Compose((ftraceEvent) => ftraceEvent.StartTimestamp));
            tableGenerator.AddColumn(startTimestampColumn);

            var cpuColumn = new BaseDataColumn<uint>(
                CpuColumn,
                eventProjection.Compose((ftraceEvent) => ftraceEvent.Cpu));
            tableGenerator.AddColumn(cpuColumn);

            var nameColumn = new BaseDataColumn<string>(
                NameColumn,
                eventProjection.Compose((ftraceEvent) => ftraceEvent.Name));
            tableGenerator.AddColumn(nameColumn);

            List<ColumnConfiguration> fieldColumns = new List<ColumnConfiguration>();

            // Add the field columns, with column names depending on the given event
            for (int index = 0; index < maxFieldCount; index++)
            {
                var colIndex = index;  // This seems unncessary but causes odd runtime behavior if not done this way. Compiler is confused perhaps because w/o this func will index=event.FieldNames.Count every time. index is passed as ref but colIndex as value into func
                string fieldName = "Field " + (colIndex + 1);

                var eventFieldNameProjection = eventProjection.Compose((ftraceEvent) => colIndex < ftraceEvent.ArgKeys.Length ? ftraceEvent.ArgKeys[colIndex] : string.Empty);

                // generate a column configuration
                var fieldColumnConfiguration = new ColumnConfiguration(
                        new ColumnMetadata(Common.GenerateGuidFromName(fieldName), fieldName, eventFieldNameProjection, fieldName),
                        new UIHints
                        {
                            IsVisible = true,
                            Width = 150,
                            TextAlignment = TextAlignment.Left,
                        });

                // Add this column to the column order
                fieldColumns.Add(fieldColumnConfiguration);

                var eventFieldAsStringProjection = eventProjection.Compose((ftraceEvent) => colIndex < ftraceEvent.Values.Length ? ftraceEvent.Values[colIndex] : string.Empty);

                tableGenerator.AddColumn(fieldColumnConfiguration, eventFieldAsStringProjection);
            }

            // Start construction of the column order. Pivot on CPU
            List<ColumnConfiguration> cpuColumns = new List<ColumnConfiguration>()
            {
                CpuColumn,
                TableConfiguration.PivotColumn, // Columns before this get pivotted
                ProcessNameColumn,
                ThreadNameColumn,
                NameColumn,
            };
            cpuColumns.AddRange(fieldColumns);
            cpuColumns.Add(TableConfiguration.GraphColumn); // Columns after this get graphed
            cpuColumns.Add(StartTimestampColumn);

            var cpuConfig = new TableConfiguration("CPU")
            {
                Columns = cpuColumns,
            };
            cpuConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);

            // Start construction of the column order. Pivot on process and thread
            List<ColumnConfiguration> processThreadColumns = new List<ColumnConfiguration>()
            {
                ProcessNameColumn,
                ThreadNameColumn,
                NameColumn,
                TableConfiguration.PivotColumn, // Columns before this get pivotted
                CpuColumn,
            };
            processThreadColumns.AddRange(fieldColumns);
            processThreadColumns.Add(TableConfiguration.GraphColumn); // Columns after this get graphed
            processThreadColumns.Add(StartTimestampColumn);

            var processThreadConfig = new TableConfiguration("Process-Thread")
            {
                Columns = processThreadColumns,
            };
            processThreadConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);

            tableBuilder
                .AddTableConfiguration(cpuConfig)
                .AddTableConfiguration(processThreadConfig)
                .SetDefaultTableConfiguration(processThreadConfig);
        }
    }
}
