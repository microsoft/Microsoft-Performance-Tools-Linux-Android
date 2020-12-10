// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using LinuxLogParser;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;

namespace DmesgIsoMPTAddin.Tables
{
    [Table]
    public static class RawTable
    {
        public static readonly TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{71819F7E-9CA4-4B72-8836-3794DC62A73F}"),
            "rawDmesgLogInfo",
            "Dmesg Raw Log",
            category: "Linux",
            requiredDataCookers: new List<DataCookerPath> {
                new DataCookerPath(SourceParserIds.DmesgIsoLog, DmesgIsoDataCooker.CookerId)
            });

        private static readonly ColumnConfiguration FileNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{C7310E9A-2B1F-4C1C-B578-6BAA7FC25CED}"), "FileName", "File Name"),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration LogNumberColumn = new ColumnConfiguration(
           new ColumnMetadata(new Guid("{72B06227-4D01-4086-A746-9BB15A342465}"), "Log Number", "Number of log in the file"),
           new UIHints { Width = 80, });

        private static readonly ColumnConfiguration LogTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{67F3A9FC-41D4-4B70-9155-7FCC5F036808}"), "Log Timestamp", "Moment when the log was created."),
            new UIHints { Width = 80, });

        private static readonly ColumnConfiguration LogColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{B265567A-E0F9-4564-9853-328C3A591191}"), "Log", "Logged message."),
            new UIHints { Width = 140, });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            DmesgIsoLogParsedResult parsedResult = tableData.QueryOutput<DmesgIsoLogParsedResult>(
               DataOutputPath.Create(SourceParserIds.DmesgIsoLog, DmesgIsoDataCooker.CookerId, "ParsedResult"));
            var logEntries = parsedResult.LogEntries;

            var baseProjection = Projection.Index(logEntries);

            var fileNameProjection = baseProjection.Compose(x => x.filePath);
            var logNumberProjection = baseProjection.Compose(x => x.lineNumber);
            var timestampProjection = baseProjection.Compose(x => x.timestamp);
            var rawLogProjection = baseProjection.Compose(x => x.rawLog);

            var columnsConfig = new TableConfiguration("Default")
            {
                Columns = new[]
                {
                    FileNameColumn,
                    TableConfiguration.PivotColumn,
                    LogNumberColumn,
                    LogColumn,
                    TableConfiguration.GraphColumn,
                    LogTimestampColumn
                },
                Layout = TableLayoutStyle.Table,
            };

            columnsConfig.AddColumnRole(ColumnRole.StartTime, LogTimestampColumn);
            columnsConfig.AddColumnRole(ColumnRole.EndTime, LogTimestampColumn);

            tableBuilder.AddTableConfiguration(columnsConfig)
                .SetDefaultTableConfiguration(columnsConfig)
                .SetRowCount(logEntries.Count)
                .AddColumn(FileNameColumn, fileNameProjection)
                .AddColumn(LogNumberColumn, logNumberProjection)
                .AddColumn(LogColumn, rawLogProjection)
                .AddColumn(LogTimestampColumn, timestampProjection);
        }
    }
}
