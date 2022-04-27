// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using PerfettoCds.Pipeline.CompositeDataCookers;
using PerfettoCds.Pipeline.DataOutput;
using Utilities;

namespace PerfettoCds.Pipeline.Tables
{
    [Table]
    public class PerfettoProcessTable
    {
        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{B1CB0340-91E6-4BCF-B42D-DD303446CDC8}"),
            "Process",
            "Contains information of processes seen during the trace",
            "Perfetto - System",
            defaultLayout: TableLayoutStyle.GraphAndTable,
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.ProcessEventCookerPath }
        );

        // Set some sort of max to prevent ridiculous field counts
        public const int AbsoluteMaxFields = 20;

        private static readonly ColumnConfiguration ProcessNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{96C00E4C-9544-442D-BA36-8BBE980BF1D6}"), "ProcessName", "The name of the process. Can be populated from manysources (e.g. ftrace, /proc scraping, track event etc)"),
            new UIHints { Width = 210 });
        private static readonly ColumnConfiguration ProcessLabelColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{57CE4F9E-A687-45C8-9A7B-CA7824773AD0}"), "ProcessLabel", "The process label"),
            new UIHints { Width = 210, IsVisible = false });
        private static readonly ColumnConfiguration UpidColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{F23C0CBC-5823-4889-9582-31C8C2B724CA}"), "Upid", "Unique process id. This is != the OS pid.This is a monotonic number associated to each process. The OS process id(pid) cannot be used as primary key because tids and pids are recycled by most kernels."),
            new UIHints { Width = 210 });
        private static readonly ColumnConfiguration PidColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{D359564F-587F-4B5E-8213-1DD96A64772D}"), "Pid", "The OS id for this process. Note: this is not unique over the lifetime of the trace so cannot be used as a primary key."),
            new UIHints { Width = 210 });
        private static readonly ColumnConfiguration StartTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{FC213F4F-BCF1-40A2-97D5-983576672EF9}"), "StartTimestamp", "The start timestamp of this process (if known). Isnull in most cases unless a process creation event is enabled (e.g. task_newtask ftrace event on Linux/Android)."),
            new UIHints { Width = 120 });
        private static readonly ColumnConfiguration EndTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{FA7FB0B1-8DAF-4155-848E-AEE56F13AF60}"), "EndTimestamp", "The end timestamp of this process (if known). Isnull in most cases unless a process destruction event is enabled (e.g. sched_process_free ftrace event on Linux/Android)."),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration ParentUpidColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{F0BFB2C2-7A25-464E-BF6C-BEA7B65D3817}"), "ParentUpid", "The upid of the process which caused this process to be spawned"),
            new UIHints { Width = 120 });
        private static readonly ColumnConfiguration ParentProcessNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{8278B256-8A15-4BD2-99EB-3CACBEB7CA75}"), "ParentProcessName", "The name of the process which caused this process to be spawned"),
            new UIHints { Width = 120 });
        private static readonly ColumnConfiguration UidColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{873E82C4-4B79-480F-A5EF-A9364DBB8E59}"), "Uid", "The Unix user id of the process"),
            new UIHints { Width = 120 });
        private static readonly ColumnConfiguration AndroidAppIdColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{81963502-B1AA-4F98-9B75-784C57ADE40A}"), "AndroidAppId", "Android appid of this process."),
            new UIHints { Width = 120 });
        private static readonly ColumnConfiguration CmdLineColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{06771957-9545-4989-BB9C-7EE8A00D9078}"), "CmdLine", "/proc/cmdline for this process."),
            new UIHints { Width = 120 });


        public static bool IsDataAvailable(IDataExtensionRetrieval tableData)
        {
            return tableData.QueryOutput<ProcessedEventData<PerfettoProcessEvent>>(
                new DataOutputPath(PerfettoPluginConstants.ProcessEventCookerPath, nameof(PerfettoProcessEventCooker.ProcessEvents))).Any();
        }

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoProcessEvent>>(
                new DataOutputPath(PerfettoPluginConstants.ProcessEventCookerPath, nameof(PerfettoProcessEventCooker.ProcessEvents)));

            int maxArgsFieldCount = Math.Min(AbsoluteMaxFields, tableData.QueryOutput<int>(
                new DataOutputPath(PerfettoPluginConstants.ProcessEventCookerPath, nameof(PerfettoProcessEventCooker.MaximumArgsEventFieldCount))));

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var baseProjection = Projection.Index(events);

            tableGenerator.AddColumn(ProcessNameColumn, baseProjection.Compose(x => x.Name));
            tableGenerator.AddColumn(ProcessLabelColumn, baseProjection.Compose(x => x.Label));
            tableGenerator.AddColumn(StartTimestampColumn, baseProjection.Compose(x => x.StartTimestamp));
            tableGenerator.AddColumn(EndTimestampColumn, baseProjection.Compose(x => x.EndTimestamp));
            tableGenerator.AddColumn(UpidColumn, baseProjection.Compose(x => x.Upid));
            tableGenerator.AddColumn(PidColumn, baseProjection.Compose(x => x.Pid));
            tableGenerator.AddColumn(ParentUpidColumn, baseProjection.Compose(x => x.ParentUpid));
            tableGenerator.AddColumn(ParentProcessNameColumn, baseProjection.Compose(x => x.ParentProcess != null ? x.ParentProcess.Name : String.Empty));
            
            tableGenerator.AddColumn(UidColumn, baseProjection.Compose(x => x.Uid));
            tableGenerator.AddColumn(AndroidAppIdColumn, baseProjection.Compose(x => x.AndroidAppId));
            tableGenerator.AddColumn(CmdLineColumn, baseProjection.Compose(x => x.CmdLine));

            if (events.Any(f => !String.IsNullOrWhiteSpace(f.Label)))
            {
                ProcessLabelColumn.DisplayHints.IsVisible = true;
            }

            List<ColumnConfiguration> extraProcessArgColumns = new List<ColumnConfiguration>();
            // Add the field columns, with column names depending on the given event
            for (int index = 0; index < maxArgsFieldCount; index++)
            {
                var colIndex = index;  // This seems unncessary but causes odd runtime behavior if not done this way. Compiler is confused perhaps because w/o this func will index=genericEvent.FieldNames.Count every time. index is passed as ref but colIndex as value into func
                string fieldName = "Field " + (colIndex + 1);

                var processArgKeysFieldNameProjection = baseProjection.Compose((pe) => colIndex < pe.ArgKeys.Length ? pe.ArgKeys[colIndex] : string.Empty);

                // generate a column configuration
                var fieldColumnConfiguration = new ColumnConfiguration(
                    new ColumnMetadata(Common.GenerateGuidFromName(fieldName), fieldName, processArgKeysFieldNameProjection, fieldName)
                    {
                        IsDynamic = true
                    },
                    new UIHints
                    {
                        IsVisible = true,
                        Width = 150,
                        TextAlignment = TextAlignment.Left,
                    });

                // Add this column to the column order
                extraProcessArgColumns.Add(fieldColumnConfiguration);

                var argsAsStringProjection = baseProjection.Compose((pe) => colIndex < pe.Values.Length ? pe.Values[colIndex].ToString() : string.Empty);

                tableGenerator.AddColumn(fieldColumnConfiguration, argsAsStringProjection);
            }

            // Default
            List<ColumnConfiguration> defaultColumns = new List<ColumnConfiguration>()
            {
                    ProcessNameColumn,
                    ProcessLabelColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    CmdLineColumn,
                    PidColumn,
                    UpidColumn,
                    ParentUpidColumn,
                    ParentProcessNameColumn,
                    UidColumn,
                    AndroidAppIdColumn,
            };
            defaultColumns.AddRange(extraProcessArgColumns);
            defaultColumns.Add(TableConfiguration.GraphColumn); // Columns after this get graphed
            defaultColumns.Add(StartTimestampColumn);
            defaultColumns.Add(EndTimestampColumn);

            var processDefaultConfig = new TableConfiguration("Default")
            {
                Columns = defaultColumns,
                ChartType = ChartType.Line
            };
            processDefaultConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn);
            processDefaultConfig.AddColumnRole(ColumnRole.EndTime, EndTimestampColumn);;

            tableBuilder.AddTableConfiguration(processDefaultConfig)
                        .SetDefaultTableConfiguration(processDefaultConfig);
        }
    }
}
