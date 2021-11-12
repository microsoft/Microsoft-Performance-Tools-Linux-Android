// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Performance.SDK.Processing;

namespace LTTngCds.MetadataTables
{
    [Table]
    [PrebuiltConfigurationsEmbeddedResource("TraceStatsPrebuiltConfiguration.json")]
    public class TraceStatsTable
    {
        public static TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{321EC524-4B0F-4324-9C6C-579BE6B5B7B2}"),
            "Trace Stats",
            "Trace Stats",
            TableDescriptor.DefaultCategory,
            true);

        private static readonly ColumnConfiguration EventNameConfiguration = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{3FF65B73-4953-4820-8884-4A6E6FC67D73}"), "Event Name", "Event Name"),
            new UIHints { Width = 200, });

        private static readonly ColumnConfiguration CountConfiguration = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{B2113983-608D-4EF3-AE0B-D56E73F2C404}"), "Count"),
            new UIHints { Width = 80, TextAlignment = TextAlignment.Left, });

        private static readonly ColumnConfiguration TotalPayloadSizeConfiguration = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{A804856F-698E-4BE6-8408-36B538D46813}"), "Total Payload Size"),
            new UIHints { Width = 80, TextAlignment = TextAlignment.Left, });

        internal static void BuildMetadataTable(ITableBuilder tableBuilder, LTTngSourceParser sourceParser, ITableConfigurationsSerializer serializer)
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