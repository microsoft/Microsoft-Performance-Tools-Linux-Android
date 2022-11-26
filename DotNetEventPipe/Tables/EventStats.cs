using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetEventPipe.Tables
{
    [Table]
    public sealed class EventStats : TraceEventTableBase
    {
        public static TableDescriptor TableDescriptor = new TableDescriptor(
           Guid.Parse("{3B038894-8539-47CE-8FB7-8E01226E9DDD}"),
           "Event Stats",
           "Event Stats",
           category: ".NET trace (dotnet-trace)",
           defaultLayout: TableLayoutStyle.GraphAndTable);

        private static readonly ColumnConfiguration providerNameColumnConfig =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{380F9262-5F13-486D-96B5-3ADB9779E54D}"), "Provider Name"),
                new UIHints { Width = 300 });

        private static readonly ColumnConfiguration eventNameColumnConfig =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{67BB853A-94FC-49EA-ACB1-E6236D3125F2}"), "Event Name"),
                new UIHints { Width = 600 });

        private static readonly ColumnConfiguration eventCountColumnConfig =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{C657FBD3-C424-4896-A8DF-B84199499A02}"), "Event Count"),
                new UIHints { Width = 150, SortOrder = SortOrder.Descending, SortPriority = 0, AggregationMode = AggregationMode.Sum });

        private static readonly ColumnConfiguration stackCountColumnConfig =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{1F98EDA4-A80A-49E6-AB0A-D3323A6E7DD0}"), "Stack Count"),
                new UIHints { Width = 150, SortOrder = SortOrder.Descending, SortPriority = 1 });

        private static readonly ColumnConfiguration startTimeConfig =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{F4E2F334-AE8E-4B15-88DC-3304D0E3BA91}"), "StartTime"),
                new UIHints { Width = 150, IsVisible = false });

        private static readonly ColumnConfiguration endTimeConfig =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{191CCB3E-F0A1-40C7-96BA-015E9F99BA17}"), "EndTime"),
                new UIHints { Width = 150, IsVisible = false });

        public EventStats(IReadOnlyDictionary<string, TraceEventProcessor> traceEventProcessor)
            : base(traceEventProcessor)
        {
        }

        public override void Build(ITableBuilder tableBuilder)
        {
            if (TraceEventProcessor == null || TraceEventProcessor.Count == 0)
            {
                return;
            }

            var firstTraceProcessorEventsParsed = TraceEventProcessor.First().Value;  // First Log
            var threadSamplingEvents = firstTraceProcessorEventsParsed.ThreadSamplingEvents;
            EventStat[] threadSamplingStats = { new EventStat
            {
                ProviderName = "Microsoft-DotNETCore-SampleProfiler",
                EventName = "Thread/Sample",
                Count = threadSamplingEvents.Count(),
                StackCount = threadSamplingEvents.Count(f => f.CallStack != null),
                StartTime = threadSamplingEvents.Min(f => f.Timestamp),
                EndTime = threadSamplingEvents.Max(f => f.Timestamp),
            } };

            var genericEvents = firstTraceProcessorEventsParsed.GenericEvents;
            var genericEventStats = genericEvents.GroupBy(
                                                    ge => new { ge.ProviderName, ge.EventName },
                                                    ge => ge,
                                                    (geGroup, geElems) => new EventStat
                                                    { 
                                                        ProviderName = geGroup.ProviderName,
                                                        EventName = geGroup.EventName,
                                                        Count = geElems.Count(),
                                                        StackCount = geElems.Count(f => f.CallStack != null),
                                                        StartTime = geElems.Min(f => f.Timestamp),
                                                        EndTime = geElems.Max(f => f.Timestamp),
                                                    });

            var allEventStats = threadSamplingStats.Union(genericEventStats).ToArray();

            var table = tableBuilder.SetRowCount(allEventStats.Length);
            var baseProj = Projection.Index(allEventStats);

            var providerNameProj = baseProj.Compose(x => x.ProviderName);
            var eventNameProj = baseProj.Compose(x => x.EventName);
            var countProj = baseProj.Compose(x => x.Count);
            var stackCountProj = baseProj.Compose(x => x.StackCount);
            var startTimeProj = baseProj.Compose(x => x.StartTime);
            var endTimeProj = baseProj.Compose(x => x.EndTime);

            table.AddColumn(providerNameColumnConfig, providerNameProj);
            table.AddColumn(eventNameColumnConfig, eventNameProj);
            table.AddColumn(eventCountColumnConfig, countProj);
            table.AddColumn(stackCountColumnConfig, stackCountProj);
            table.AddColumn(startTimeConfig, startTimeProj);
            table.AddColumn(endTimeConfig, endTimeProj);

            var tableConfig = new TableConfiguration("Stats")
            {
                Columns = new[]
                {
                    providerNameColumnConfig,
                    eventNameColumnConfig,
                    TableConfiguration.PivotColumn,
                    stackCountColumnConfig,
                    TableConfiguration.GraphColumn,
                    eventCountColumnConfig,
                }
            };
            tableConfig.AddColumnRole(ColumnRole.StartTime, startTimeConfig);
            tableConfig.AddColumnRole(ColumnRole.EndTime, endTimeConfig);
            tableConfig.ChartType = ChartType.StackedBars;

            tableBuilder.SetDefaultTableConfiguration(tableConfig);
        }

    }
}
