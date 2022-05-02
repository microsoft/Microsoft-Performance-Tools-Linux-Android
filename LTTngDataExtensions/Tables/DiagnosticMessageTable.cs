// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using LTTngCds.CookerData;
using LTTngDataExtensions.SourceDataCookers.Diagnostic_Messages;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;

namespace LTTngDataExtensions.Tables
{
    [Table]
    [RequiresSourceCooker(LTTngConstants.SourceId, LTTngDmesgDataCooker.Identifier)]
    public class DiagnosticMessageTable
    {
        public static TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{A752AFA9-B30E-44ED-B5BA-348C55A63157}"),
            "DiagnosticMessages",
            "Diagnostic Messages",
            "Linux LTTng",
            defaultLayout: TableLayoutStyle.GraphAndTable);

        private static readonly ColumnConfiguration messageColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{3A0B5030-4723-4FA4-85C5-979E5EC6C5E2}"), "Message"),
                new UIHints { Width = 80, });
        private static readonly ColumnConfiguration timestampColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{45A3AB1A-503E-46F0-B10D-20FC303DF0C7}"), "Timestamp"),
                new UIHints { Width = 80, });

        public static bool IsDataAvailable(IDataExtensionRetrieval tableData)
        {
            return tableData.QueryOutput<IReadOnlyList<IDiagnosticMessage>>(
                DataOutputPath.ForSource(LTTngConstants.SourceId, LTTngDmesgDataCooker.Identifier, nameof(LTTngDmesgDataCooker.DiagnosticMessages))).Any();
        }

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            var messages = tableData.QueryOutput<IReadOnlyList<IDiagnosticMessage>>(
                DataOutputPath.ForSource(LTTngConstants.SourceId, LTTngDmesgDataCooker.Identifier, nameof(LTTngDmesgDataCooker.DiagnosticMessages)));
            if (messages.Count == 0)
            {
                return;
            }

            var config = new TableConfiguration("MessagesByTimestamp")
            {
                Columns = new[]
                {
                    messageColumn,
                    TableConfiguration.PivotColumn,
                    TableConfiguration.GraphColumn,
                    timestampColumn
                },
            };

            config.AddColumnRole(ColumnRole.StartTime, timestampColumn);
            config.AddColumnRole(ColumnRole.EndTime, timestampColumn);

            var table = tableBuilder.AddTableConfiguration(config)
                                    .SetDefaultTableConfiguration(config)
                                    .SetRowCount(messages.Count);

            table.AddColumn(messageColumn, Projection.CreateUsingFuncAdaptor((i) => messages[i].Message));
            table.AddColumn(timestampColumn, Projection.CreateUsingFuncAdaptor((i) => messages[i].Timestamp));
        }
    }
}
