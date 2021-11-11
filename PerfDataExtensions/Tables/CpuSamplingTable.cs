// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using PerfCds.CookerData;
using PerfDataExtensions.DataOutputTypes;
using PerfDataExtensions.SourceDataCookers.Cpu;
using PerfDataExtensions.Tables.Generators;
using System;
using System.Collections.Generic;
using Utilities.AccessProviders;
using static PerfDataExtensions.Tables.TimeHelper;

namespace PerfDataExtensions.Tables
{
    [Table]
    [RequiresSourceCooker(PerfConstants.SourceId, PerfCpuClockDataCooker.Identifier)]
    public class CpuSamplingTable
    {
        public static TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{887ac8ea-2023-4818-85dc-320908775cfa}"),
            "cpu-clock",
            "Cpu Sampling",
            "Linux Perf");

        // TODO: Add config option on table to default filter out idle - PID == 0

        private static readonly ColumnConfiguration cpuColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{882508D5-606D-4796-96A7-CF254ABD1385}"), "CPU"),
                new UIHints { Width = 80 });

        private static readonly ColumnConfiguration ipColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{ccd4e9e2-2892-4893-a5ef-c32709ff5b68}"), "Ip"),
                new UIHints { Width = 80, CellFormat = ColumnFormats.HexFormat });

        private static readonly ColumnConfiguration ipSymbolColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{a8a9376b-e24a-4b4b-a51d-20bf120a405c}"), "Instruction Pointer (Ip)"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration symbolTypeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{d734bdba-0bfa-4047-830b-992d63f269af}"), "Symbol Type"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration threadIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{24bdcc03-755d-4a44-a910-6ee14c73b0a6}"), "Thread Id (Tid)"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration pidColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{d6ae2bfd-259a-492b-b38b-3ca4667c2782}"), "Process Id (Pid)"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration idColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{abec90ca-08da-41cc-8f29-7affc4e98ab7}"), "Perf Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration perfPeriodColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{34603bd3-3d15-4c8f-8966-8c5ddf3ee80f}"), "Perf Period"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration perfCallchainSizeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{3ed00836-7859-469e-8957-ddb683a49788}"), "Perf Callchain Size"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration callStackColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{FAD10C0F-F382-4224-89AB-66F7E8357C4E}"), "Callstack"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration timeStampColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{fbefb6d1-6c57-400a-aac5-5031d7ef84f3}"), "Timestamp"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration weightColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{DBB28E38-EFF8-4630-B966-2A1D70AF4697}"), "Sample Weight"),
                new UIHints { Width = 80, CellFormat = TimestampFormatter.FormatMillisecondsGrouped, });

        private static readonly ColumnConfiguration weightPctColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{653AB235-4D63-4CC8-906B-238284ED9390}"), "Weight %"),
                new UIHints
                {
                    Width = 80,
                    AggregationMode = AggregationMode.Sum,
                    SortPriority = 0,
                    SortOrder = SortOrder.Descending,
                });

        private static readonly ColumnConfiguration startTimeCol =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{281DC102-0F73-487E-ADE7-AC5FD7961A29}"), "Start Time"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration viewportClippedStartTimeCol =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{9257965D-2DCA-4EFB-8275-8213B0A75D9D}"), "Clipped Start Time"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration viewportClippedEndTimeCol =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{079C5D05-237D-4DA8-9516-5CF28E11414B}"), "Clipped End Time"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration clippedWeightCol =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{6618AE2E-9A6D-403B-91E1-A6E78A3FC8CC}"), "Clipped Weight"),
                new UIHints { Width = 80, });


        private static readonly ColumnConfiguration countColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{7618F209-B4B5-47E2-A572-19CE89668CE8}"), "Count"),
                new UIHints
                {
                    Width = 80,
                    AggregationMode = AggregationMode.Sum,
                    SortPriority = 1,
                    SortOrder = SortOrder.Descending,
                });


        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            var pathIdentifier = DataOutputPath.ForSource(PerfConstants.SourceId, PerfCpuClockDataCooker.Identifier, nameof(PerfCpuClockDataCooker.CpuClockEvents));

            var cpuClocks = tableData.QueryOutput<IReadOnlyList<ICpuClockEvent>>(pathIdentifier);
            if (cpuClocks.Count == 0)
            {
                return;
            }

            var config = new TableConfiguration("CPU Utilization")
            {
                Columns = new[]
                {
                    cpuColumn,
                    callStackColumn,
                    TableConfiguration.PivotColumn,
                    pidColumn,
                    threadIdColumn,
                    ipColumn,
                    symbolTypeColumn,
                    ipSymbolColumn,
                    idColumn,
                    weightColumn,
                    countColumn,
                    timeStampColumn,
                    TableConfiguration.GraphColumn,
                    weightPctColumn
                },
            };

            config.AddColumnRole(ColumnRole.EndTime, timeStampColumn);
            config.AddColumnRole(ColumnRole.Duration, weightColumn);
            config.AddColumnRole(ColumnRole.ResourceId, cpuColumn);

            var table = tableBuilder.AddTableConfiguration(config)
                                    .SetDefaultTableConfiguration(config)
                                    .SetRowCount(cpuClocks.Count);

            var timeStampProjection = Projection.CreateUsingFuncAdaptor((i) => cpuClocks[i].Timestamp);
            table.AddColumn(cpuColumn, Projection.CreateUsingFuncAdaptor((i) => cpuClocks[i].Cpu));
            table.AddColumn(ipColumn, Projection.CreateUsingFuncAdaptor((i) => cpuClocks[i].Ip));
            table.AddColumn(symbolTypeColumn, Projection.CreateUsingFuncAdaptor((i) => cpuClocks[i].Ip_Symbol?.SymbolType?.SymbolTypeDescription.SymbolTypeShortName));
            table.AddColumn(ipSymbolColumn, Projection.CreateUsingFuncAdaptor((i) => cpuClocks[i].Ip_Symbol?.Name));
            table.AddColumn(threadIdColumn, Projection.CreateUsingFuncAdaptor((i) => cpuClocks[i].Tid));
            table.AddColumn(pidColumn, Projection.CreateUsingFuncAdaptor((i) => cpuClocks[i].Pid));
            table.AddColumn(idColumn, Projection.CreateUsingFuncAdaptor((i) => cpuClocks[i].Id));
            table.AddColumn(perfPeriodColumn, Projection.CreateUsingFuncAdaptor((i) => cpuClocks[i].Perf_Period));
            table.AddColumn(perfCallchainSizeColumn, Projection.CreateUsingFuncAdaptor((i) => cpuClocks[i].Perf_Callchain_Size));
            table.AddHierarchicalColumn(callStackColumn, Projection.CreateUsingFuncAdaptor((i) => cpuClocks[i].CallStack), new ArrayAccessProvider<string>());
            table.AddColumn(timeStampColumn, timeStampProjection);

            var oneMsSample = new TimestampDelta(1000000); // 1ms for now until we can figure out weight better
            var oneNs = new TimestampDelta(1);
            var weightProj = Projection.Constant(oneMsSample);

            var timeStampStartProjection = Projection.CreateUsingFuncAdaptor((i) => cpuClocks[i].Timestamp - oneNs); // We will say sample lasted 1ns
            IProjection<int, Timestamp> viewportClippedStartTimeProj = Projection.ClipTimeToVisibleDomain.Create(timeStampStartProjection);
            IProjection<int, Timestamp> viewportClippedEndTimeProj = Projection.ClipTimeToVisibleDomain.Create(timeStampProjection);

            IProjection<int, TimestampDelta> clippedWeightProj = Projection.Select(
                viewportClippedEndTimeProj,
                viewportClippedStartTimeProj,
                new ReduceTimeSinceLastDiff());

            IProjection<int, double> weightPercentProj = Projection.VisibleDomainRelativePercent.Create(clippedWeightProj);

            IProjection<int, int> countProj = SequentialGenerator.Create(
                cpuClocks.Count,
                Projection.Constant(1),
                Projection.Constant(0));

            table.AddColumn(weightColumn, weightProj);
            table.AddColumn(countColumn, countProj);
            table.AddColumn(weightPctColumn, weightPercentProj);
            table.AddColumn(startTimeCol, timeStampStartProjection);
            table.AddColumn(viewportClippedStartTimeCol, viewportClippedStartTimeProj);
            table.AddColumn(viewportClippedEndTimeCol, viewportClippedEndTimeProj);
            table.AddColumn(clippedWeightCol, clippedWeightProj);
        }

        public static TimestampDelta ProjectSampleToWeight(ICpuClockEvent sample)
        {
            return new TimestampDelta(1000000); // 1ms for now until we can figure out weight better
        }
    }
}
