// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using PerfettoCds.Pipeline.DataOutput;
using Microsoft.Performance.SDK;
using PerfettoCds.Pipeline.CompositeDataCookers;
using Utilities.AccessProviders;

namespace PerfettoCds.Pipeline.Tables
{
    [Table]
    public class PerfettoCpuSamplingTable
    {
        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{780AA764-4875-47AA-9DD1-12643DB12842}"),
            "CPU Sampling Events",
            "Displays CPU sampling events for processes and threads",
            "Perfetto - System",
            defaultLayout: TableLayoutStyle.GraphAndTable,
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.CpuSamplingEventCookerPath }
        );

        private static readonly ColumnConfiguration ProcessNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{CADC2BC4-4D5F-4AA3-9802-4155333418F6}"), "Process", "Name of the process"),
            new UIHints { Width = 210 });

        private static readonly ColumnConfiguration ThreadNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{CB1578E6-6FA5-4CE7-BC85-3A3EA8FCE534}"), "Thread", "Name of the thread"),
            new UIHints { Width = 210 });

        private static readonly ColumnConfiguration TimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{0DD7C359-FC5F-41F5-B73B-860484B6B343}"), "Timestamp", "Timestamp for the event"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration CpuColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{0EEC7AFF-DBB7-4456-8840-C35A65DB05E5}"), "Cpu", "the core the sampled thread was running on"),
            new UIHints { Width = 70 });

        private static readonly ColumnConfiguration UnwindErrorColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{151628C1-D40C-45A8-9311-25DA44B2E0AE}"), "UnwindError", "Indicates that the unwinding for this sampleencountered an error."),
            new UIHints { Width = 70 });

        private static readonly ColumnConfiguration ModuleColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{021608B5-FE79-4003-B639-57965D48B748}"), "Module", "The binary module for the Instruction Pointer (IP)"),
            new UIHints { Width = 70 });

        private static readonly ColumnConfiguration FunctionColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{8BC9D558-0753-446F-A39F-9BBE9BCBFF6E}"), "Function", "The function of the Instruction Pointer (IP)"),
            new UIHints { Width = 70, SortOrder = SortOrder.Descending, AggregationMode = AggregationMode.Count });

        private static readonly ColumnConfiguration CallStackColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{653E439B-2807-4E27-BD29-448964CDB85B}"), "CallStack", "Callstack of the sample"),
            new UIHints { Width = 300 });

        // In Perfetto, callstacks are often scoped/filtered.
        // TODO - We will have to determine how to detect this at a later date and only calc %  CPU Usage if it can be done accurately

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoCpuSamplingEvent>>(
                new DataOutputPath(PerfettoPluginConstants.CpuSamplingEventCookerPath, nameof(PerfettoCpuSamplingEventCooker.CpuSamplingEvents)));

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var baseProjection = Projection.Index(events);

            var startProjection = baseProjection.Compose(x => x.Timestamp);

            tableGenerator.AddColumn(CpuColumn, baseProjection.Compose(x => x.Cpu));
            tableGenerator.AddColumn(ProcessNameColumn, baseProjection.Compose(x => x.ProcessName));
            tableGenerator.AddColumn(ThreadNameColumn, baseProjection.Compose(x => x.ThreadName));
            tableGenerator.AddColumn(UnwindErrorColumn, baseProjection.Compose(x => x.UnwindError));
            tableGenerator.AddColumn(ModuleColumn, baseProjection.Compose(x => x.Module));
            tableGenerator.AddColumn(FunctionColumn, baseProjection.Compose(x => x.Function));
            tableGenerator.AddHierarchicalColumn(CallStackColumn, baseProjection.Compose(x => x.CallStack), new ArrayAccessProvider<string>());
            tableGenerator.AddColumn(TimestampColumn, startProjection);

            var processStackConfig = new TableConfiguration("By Process, Stack")
            {
                Columns = new[]
                {
                    ProcessNameColumn,
                    CallStackColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    ThreadNameColumn,
                    ModuleColumn,
                    FunctionColumn,
                    CpuColumn,
                    UnwindErrorColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    TimestampColumn,
                },
            };
            processStackConfig.AddColumnRole(ColumnRole.StartTime, TimestampColumn.Metadata.Guid);

            var processThreadStackConfig = new TableConfiguration("By Process, Thread, Stack")
            {
                Columns = new[]
                {
                    ProcessNameColumn,
                    ThreadNameColumn,
                    CallStackColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    ModuleColumn,
                    FunctionColumn,
                    CpuColumn,
                    UnwindErrorColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    TimestampColumn,
                },
            };
            processThreadStackConfig.AddColumnRole(ColumnRole.StartTime, TimestampColumn.Metadata.Guid);

            tableBuilder
                .AddTableConfiguration(processStackConfig)
                .AddTableConfiguration(processThreadStackConfig)
                .SetDefaultTableConfiguration(processStackConfig);
        }
    }
}
