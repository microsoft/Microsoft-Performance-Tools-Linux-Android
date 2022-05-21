// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using DotNetEventPipe.DataOutputTypes;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Processing;
using Utilities.AccessProviders;
using Utilities.Generators;
using static Utilities.TimeHelper;

namespace DotNetEventPipe.Tables
{
    //
    // Have the MetadataTable inherit the TableBase class
    //
    [Table]              // A category is optional. It useful for grouping different types of tables
    public sealed class CpuSamplingTable
        : TraceEventTableBase
    {
        public static readonly TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{29C3ECF1-0857-4A3D-B6E3-2197CE1E9C81}"),
            "CPU Sampling",
            "Thread Profiler Samples",
            category: ".NET trace (dotnet-trace)");

        public CpuSamplingTable(IReadOnlyDictionary<string, TraceEventProcessor> traceEventProcessor)
            : base(traceEventProcessor)
        {
        }

        private static readonly ColumnConfiguration timestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{8FFAE86A-5608-42CB-80E4-01D982AFBCA2}"), "Timestamp", "The timestamp of the sample"),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration functionColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{4E6F6543-925A-479D-947C-CC3E932DA139}"), "Function", "The function of the Instruction Pointer(IP)"),
            new UIHints { Width = 130 });

        private static readonly ColumnConfiguration moduleColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{A3EE00E4-71A9-47F9-B0B8-448CAEE92BB0}"), "Module", "The module of the Instruction Pointer(IP)"),
            new UIHints { Width = 130 });

        private static readonly ColumnConfiguration countColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{711ECA36-9B10-4E88-829D-F5E6749E22A6}"), "Count", "The count of samples"),
            new UIHints { 
                Width = 130, 
                AggregationMode = AggregationMode.Sum, // Sum needed instead of Count for flame
            }); 

        private static readonly ColumnConfiguration callStackColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{5072587D-B69C-4B4B-AEB9-AD51C3FFEBFB}"), "Callstack"),
                new UIHints { Width = 800, });

        private static readonly ColumnConfiguration threadIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{11C8D2B4-3E39-46A4-B9AF-33D2F4E79148}"), "ThreadId"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration processColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{DA2F0BA6-5F54-449E-A3B8-586DB3D38DCC}"), "Process"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration processIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{357841CB-AE3B-4BFF-A778-C0B6AD3E4CD0}"), "Process Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration cpuColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{DB744571-72CE-4C56-8277-58A402682016}"), "CPU"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration weightColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{46478B6A-C6FB-4552-AF78-E129E660189B}"), "Sample Weight"),
                new UIHints { Width = 80, CellFormat = TimestampFormatter.FormatMillisecondsGrouped, });

        private static readonly ColumnConfiguration weightPctColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{EED346D1-98C4-4702-838F-FF3EFF822EC1}"), "Weight %"),
                new UIHints
                {
                    Width = 80,
                    AggregationMode = AggregationMode.Sum,
                    SortPriority = 0,
                    SortOrder = SortOrder.Descending,
                });

        private static readonly ColumnConfiguration viewportClippedStartTimeCol =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{D0F5DE08-E4FF-45D2-8644-E6D2AACFB2CD}"), "Clipped Start Time"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration viewportClippedEndTimeCol =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{8AC0B8A4-CE20-4B54-A031-1558B2A9238C}"), "Clipped End Time"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration clippedWeightCol =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{E5A38163-3FB1-47DC-9E0E-E296041A563D}"), "Clipped Weight"),
                new UIHints { Width = 80, });

        private IReadOnlyList<ThreadSamplingEvent> ThreadSamplingEvents;

        public override void Build(ITableBuilder tableBuilder)
        {
            if (TraceEventProcessor == null || TraceEventProcessor.Count == 0)
            {
                return;
            }

            var firstTraceProcessorEventsParsed = TraceEventProcessor.First().Value;  // First Log
            ThreadSamplingEvents = firstTraceProcessorEventsParsed.ThreadSamplingEvents;

            var tableGenerator = tableBuilder.SetRowCount(ThreadSamplingEvents.Count);
            var baseProjection = Projection.Index(ThreadSamplingEvents);

            
            tableGenerator.AddColumn(countColumn, baseProjection.Compose(x => 1));                  // 1 sample
            tableGenerator.AddColumn(processIdColumn, baseProjection.Compose(x => x.ProcessID));
            tableGenerator.AddColumn(processColumn, baseProjection.Compose(x => x.ProcessName));
            tableGenerator.AddColumn(cpuColumn, baseProjection.Compose(x => x.ProcessorNumber));
            tableGenerator.AddColumn(threadIdColumn, baseProjection.Compose(x => x.ThreadID));
            tableGenerator.AddColumn(moduleColumn, baseProjection.Compose(x => x.Module?.Name));
            tableGenerator.AddColumn(functionColumn, baseProjection.Compose(x => x.FullMethodName));
            tableGenerator.AddHierarchicalColumn(callStackColumn, baseProjection.Compose(x => x.CallStack), new ArrayAccessProvider<string>());
            var timeStampProjection = Projection.CreateUsingFuncAdaptor((i) => ThreadSamplingEvents[i].Timestamp);

            // Calculating sample weights
            var tenMsSample = new TimestampDelta(10000000); // 10ms - https://docs.microsoft.com/en-us/dotnet/core/diagnostics/well-known-event-providers
            var weightProj = Projection.Constant(tenMsSample);
            var oneNs = new TimestampDelta(1);

            var timeStampStartProjection = baseProjection.Compose(x => x.Timestamp - oneNs); // We will say sample lasted 1ns
            IProjection<int, Timestamp> viewportClippedStartTimeProj = Projection.ClipTimeToVisibleDomain.Create(timeStampStartProjection);
            IProjection<int, Timestamp> viewportClippedEndTimeProj = Projection.ClipTimeToVisibleDomain.Create(timeStampProjection);

            IProjection<int, TimestampDelta> clippedWeightProj = Projection.Select(
                viewportClippedEndTimeProj,
                viewportClippedStartTimeProj,
                new ReduceTimeSinceLastDiff());

            IProjection<int, double> weightPercentProj = Projection.VisibleDomainRelativePercent.Create(clippedWeightProj);

            IProjection<int, int> countProj = SequentialGenerator.Create(
                ThreadSamplingEvents.Count,
                Projection.Constant(1),
                Projection.Constant(0));

            tableGenerator.AddColumn(weightColumn, weightProj);
            tableGenerator.AddColumn(weightPctColumn, weightPercentProj);
            tableGenerator.AddColumn(timestampColumn, timeStampStartProjection);
            tableGenerator.AddColumn(viewportClippedStartTimeCol, viewportClippedStartTimeProj);
            tableGenerator.AddColumn(viewportClippedEndTimeCol, viewportClippedEndTimeProj);
            tableGenerator.AddColumn(clippedWeightCol, clippedWeightProj);

            var utilByCpuStackConfig = new TableConfiguration("Utilization by CPU, Stack")
            {
                Columns = new[]
              {
                    cpuColumn,
                    callStackColumn,
                    TableConfiguration.PivotColumn,
                    processColumn,
                    threadIdColumn,
                    functionColumn,
                    moduleColumn,
                    timestampColumn,
                    countColumn,
                    TableConfiguration.GraphColumn,
                    weightPctColumn

                },
            };
            utilByCpuStackConfig.AddColumnRole(ColumnRole.EndTime, timestampColumn);
            utilByCpuStackConfig.AddColumnRole(ColumnRole.ResourceId, cpuColumn);
            utilByCpuStackConfig.AddColumnRole(ColumnRole.Duration, weightColumn);

            var utilByCpuConfig = new TableConfiguration("Utilization by CPU")
            {
                Columns = new[]
              {
                    cpuColumn,
                    TableConfiguration.PivotColumn,
                    processColumn,
                    threadIdColumn,
                    functionColumn,
                    moduleColumn,
                    timestampColumn,
                    countColumn,
                    TableConfiguration.GraphColumn,
                    weightPctColumn

                },
            };
            utilByCpuConfig.AddColumnRole(ColumnRole.EndTime, timestampColumn);
            utilByCpuConfig.AddColumnRole(ColumnRole.ResourceId, cpuColumn);
            utilByCpuConfig.AddColumnRole(ColumnRole.Duration, weightColumn);

            var utilByProcessConfig = new TableConfiguration("Utilization by Process")
            {
                Columns = new[]
              {
                    processColumn,
                    TableConfiguration.PivotColumn,
                    cpuColumn,
                    threadIdColumn,
                    functionColumn,
                    moduleColumn,
                    timestampColumn,
                    countColumn,
                    TableConfiguration.GraphColumn,
                    weightPctColumn

                },
            };
            utilByProcessConfig.AddColumnRole(ColumnRole.EndTime, timestampColumn);
            utilByProcessConfig.AddColumnRole(ColumnRole.ResourceId, cpuColumn);
            utilByProcessConfig.AddColumnRole(ColumnRole.Duration, weightColumn);

            var utilByProcessStackConfig = new TableConfiguration("Utilization by Process, Stack")
            {
                Columns = new[]
              {
                    processColumn,
                    callStackColumn,
                    TableConfiguration.PivotColumn,
                    cpuColumn,
                    threadIdColumn,
                    functionColumn,
                    moduleColumn,
                    timestampColumn,
                    countColumn,
                    TableConfiguration.GraphColumn,
                    weightPctColumn

                },
            };
            utilByProcessStackConfig.AddColumnRole(ColumnRole.EndTime, timestampColumn);
            utilByProcessStackConfig.AddColumnRole(ColumnRole.ResourceId, cpuColumn);
            utilByProcessStackConfig.AddColumnRole(ColumnRole.Duration, weightColumn);

            var flameByProcessStackConfig = new TableConfiguration("Flame by Process, Stack")
            {
                Columns = new[]
              {
                    processColumn,
                    callStackColumn,
                    TableConfiguration.PivotColumn,
                    cpuColumn,
                    threadIdColumn,
                    functionColumn,
                    moduleColumn,
                    timestampColumn,
                    countColumn,
                    TableConfiguration.GraphColumn,
                    weightPctColumn

                },
                ChartType = ChartType.Flame,

            };
            flameByProcessStackConfig.AddColumnRole(ColumnRole.EndTime, timestampColumn);
            flameByProcessStackConfig.AddColumnRole(ColumnRole.ResourceId, cpuColumn);
            flameByProcessStackConfig.AddColumnRole(ColumnRole.Duration, weightColumn);

            var table = tableBuilder
            .AddTableConfiguration(utilByCpuStackConfig)
                .SetDefaultTableConfiguration(utilByProcessStackConfig)
                .AddTableConfiguration(utilByCpuConfig)
                .AddTableConfiguration(utilByProcessConfig)
                .AddTableConfiguration(utilByProcessStackConfig)
                .AddTableConfiguration(flameByProcessStackConfig);
        }
    }
}
