// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using LinuxLogParser;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;

namespace DmesgIsoMPTAddin.Tables
{
    [Table]
    public static class ParsedTable
    {
        public static readonly TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{E3EEEA4F-6F67-4FB7-9F3B-0F62FB7400B9}"),
            "parsedDmesgLogInfo",
            "Dmesg Parsed Log",
            category: "Linux",
            requiredDataCookers: new List<DataCookerPath> {
                new DataCookerPath(SourceParserIds.DmesgIsoLog, DmesgIsoDataCooker.CookerId)
            });

        private static readonly ColumnConfiguration FileNameColumn = new ColumnConfiguration(
           new ColumnMetadata(new Guid("{5B4ADD38-41EF-4155-AD04-04D0D51DEC87}"), "FileName", "File Name"),
           new UIHints { Width = 80 });

        private static readonly ColumnConfiguration MessageNumberColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{C26B5860-2B6C-48F9-8DCE-B5EF5C3FB371}"), "Line Number", "Ordering of the lines in the file"),
            new UIHints { Width = 80, });

        private static readonly ColumnConfiguration EntityColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{9D79FFBC-01CE-4581-BC19-F7339125AD9F}"), "Entity", "The entity that produced the message."),
            new UIHints { Width = 80, });

        private static readonly ColumnConfiguration TopicColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{E40F2307-75D2-479F-9C97-787E22465E8A}"), "Topic", "The topic of the message."),
            new UIHints { Width = 80, });

        private static readonly ColumnConfiguration TimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{5481E4AC-C12E-4614-957F-9C1ED49401D3}"), "Timestamp", "Timestamp when the message was created."),
            new UIHints { Width = 80, });

        private static readonly ColumnConfiguration MetadataColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{4C0E9263-C371-4785-B689-0704389E4872}"), "Metadata Info", "Time metadata recorded in the message (often irrelevant)."),
            new UIHints { Width = 80, });

        private static readonly ColumnConfiguration MessageColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{8B925E73-06B4-47DD-94BE-CE1D2A33B5DB}"), "Message", "Logged message."),
            new UIHints { Width = 140, });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            DmesgIsoLogParsedResult parsedResult = tableData.QueryOutput<DmesgIsoLogParsedResult>(
               DataOutputPath.Create(SourceParserIds.DmesgIsoLog, DmesgIsoDataCooker.CookerId, "ParsedResult"));
            var logEntries = parsedResult.LogEntries;

            var baseProjection = Projection.Index(logEntries);

            var fileNameProjection = baseProjection.Compose(x => x.filePath);
            var lineNumberProjection = baseProjection.Compose(x => x.lineNumber);
            var entityProjection = baseProjection.Compose(x => x.entity);
            var topicProjection = baseProjection.Compose(x => x.topic);
            var timestampProjection = baseProjection.Compose(x => x.timestamp);
            var metadataProjection = baseProjection.Compose(x => x.metadata);
            var messageProjection = baseProjection.Compose(x => x.message); 

            var config = new TableConfiguration("Default")
            {
                Columns = new[]
                {
                    FileNameColumn,
                    EntityColumn,
                    TableConfiguration.PivotColumn,
                    MessageNumberColumn,
                    TopicColumn,
                    MessageColumn,
                    MetadataColumn,
                    TableConfiguration.GraphColumn,
                    TimestampColumn
                },
                Layout = TableLayoutStyle.GraphAndTable,
            };

            config.AddColumnRole(ColumnRole.StartTime, TimestampColumn);
            config.AddColumnRole(ColumnRole.EndTime, TimestampColumn);

            tableBuilder.AddTableConfiguration(config)
                .SetDefaultTableConfiguration(config)
                .SetRowCount(logEntries.Count)
                .AddColumn(FileNameColumn, fileNameProjection)
                .AddColumn(MessageNumberColumn, lineNumberProjection)
                .AddColumn(EntityColumn, entityProjection)
                .AddColumn(TopicColumn, topicProjection)
                .AddColumn(MessageColumn, messageProjection)
                .AddColumn(TimestampColumn, timestampProjection)
                .AddColumn(MetadataColumn, metadataProjection);
        }
    }
}
