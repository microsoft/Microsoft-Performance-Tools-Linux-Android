// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Performance.SDK.Processing;

namespace PerfCds.MetadataTables
{
    [Table]
    [PrebuiltConfigurationsEmbeddedResource("TraceStatsPrebuiltConfiguration.json")]
    public class TraceStatsTable
    {
        public static TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{ea0ae200-964e-41ec-b550-6b76ee2b60a2}"),
            "Trace Stats",
            "Trace Stats",
            TableDescriptor.DefaultCategory,
            true);

        private static readonly ColumnConfiguration EventNameConfiguration = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{73fbbc8e-fa72-49ff-b3b2-9f4c6711b7dc}"), "Event Name", "Event Name"),
            new UIHints { Width = 200, });

        private static readonly ColumnConfiguration CountConfiguration = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{4702f3bf-5471-4d41-b70e-3ea13947236d}"), "Count"),
            new UIHints { Width = 80, TextAlignment = TextAlignment.Left, });

        private static readonly ColumnConfiguration TotalPayloadSizeConfiguration = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{5e441122-b8f0-42c4-9ca7-41c5fe29d5eb}"), "Total Payload Size"),
            new UIHints { Width = 80, TextAlignment = TextAlignment.Left, });

        internal static void BuildMetadataTable(ITableBuilder tableBuilder, PerfSourceParser sourceParser, ITableConfigurationsSerializer serializer)
        {
            ITableBuilderWithRowCount table = tableBuilder.SetRowCount(sourceParser.TraceStats.Count);

            IReadOnlyList<string> eventNames = sourceParser.TraceStats.Keys.ToList();

            var eventNameProjection = Projection.CreateUsingFuncAdaptor(x => eventNames[x]);
            var traceStatsProjection = eventNameProjection.Compose(eventName => sourceParser.TraceStats[eventName]);
            var eventCountProjection = traceStatsProjection.Compose(traceStats => traceStats.EventCount);
            var payloadBitCountProjection = traceStatsProjection.Compose(traceStats => (double)traceStats.PayloadBitCount / 8);

            table.AddColumn(
                new DataColumn<string>(
                    EventNameConfiguration,
                    eventNameProjection));

            table.AddColumn(
                new DataColumn<ulong>(
                    CountConfiguration,
                    eventCountProjection));

            table.AddColumn(
                new DataColumn<double>(
                    TotalPayloadSizeConfiguration,
                    payloadBitCountProjection));

            var configurations = TableConfigurations.GetPrebuiltTableConfigurations(
                typeof(TraceStatsTable),
                TableDescriptor.Guid,
                serializer);

            foreach (var configuration in configurations)
            {
                tableBuilder.AddTableConfiguration(configuration);
                if (StringComparer.Ordinal.Equals(configuration.Name, configurations.DefaultConfigurationName))
                {
                    tableBuilder.SetDefaultTableConfiguration(configuration);
                }
            }
        }
    }
}