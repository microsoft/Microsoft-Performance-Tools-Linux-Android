// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using LinuxLogParser;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;

namespace AndroidLogcatMPTAddin.Tables
{
    [Table]
    public static class LogTable
    {
        public static readonly TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{C2035282-4E3F-4436-821B-46C98D383A56}"),
            "Android Logcat",
            "Android Logcat Parsed Log",
            category: "Android",
            requiredDataCookers: new List<DataCookerPath> {
                DataCookerPath.ForSource(SourceParserIds.AndroidLogcatLog, AndroidLogcatDataCooker.CookerId)
            });

        private static readonly ColumnConfiguration FileNameColumn = new ColumnConfiguration(
           new ColumnMetadata(new Guid("{E550F06E-523C-47FA-BB19-1929D9F42754}"), "FileName", "File Name"),
           new UIHints { Width = 80 });

        private static readonly ColumnConfiguration TimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{B5896C69-020C-40A0-9BDC-C34A19011C28}"), "Timestamp", "Timestamp when the message was created."),
            new UIHints { Width = 80, });

        // todo: needs to be changed by user manually to DateTime UTC format. SDK doesn't yet support specifying this <DateTimeTimestampOptionsParameter DateTimeEnabled="true" />
        private static readonly ColumnConfiguration TimestampAbsColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{B5896C69-020C-40A0-9BDC-C34A19011C28}"), "Timestamp", "Timestamp when the message was created."),
            new UIHints { Width = 80, });

        private static readonly ColumnConfiguration LineNumberColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{EDBD9BAF-0BD8-43D7-9752-BFCEF3FCBC13}"), "Line Number", "Ordering of the lines in the file"),
            new UIHints { Width = 80, SortOrder = SortOrder.Ascending, SortPriority = 0 });

        private static readonly ColumnConfiguration PIDColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{F75CD977-60C7-4CC3-9AB4-94ED44FFDC21}"), "PID", "The process ID that produced the message."),
            new UIHints { Width = 40, });

        private static readonly ColumnConfiguration TIDColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{8BBD6094-9772-4790-B4B8-C5EAAA6D67E1}"), "TID", "The thread ID that produced the message."),
            new UIHints { Width = 40, });

        private static readonly ColumnConfiguration PriorityColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{3FD930DE-AFC1-4C62-832F-C7AEE8081162}"), "Priority", "The priority of the message (V)erbose (D)ebug (I)nfo (W)arning (E)rror (A)ssert"),
            new UIHints { Width = 20, });

        private static readonly ColumnConfiguration TagColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{F541B9A7-EEDD-42AC-B3AB-F353E93169EB}"), "Tag", "Indicates which system component logged the message"),
            new UIHints { Width = 120, });

        private static readonly ColumnConfiguration MessageColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{43D28B83-AE40-4989-8E32-8837B0BB10D7}"), "Message", "Logged message."),
            new UIHints { Width = 600, });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            var parsedResult = tableData.QueryOutput<AndroidLogcatParsedResult>(
               DataOutputPath.ForSource(SourceParserIds.AndroidLogcatLog, AndroidLogcatDataCooker.CookerId, nameof(AndroidLogcatDataCooker.ParsedResult)));
            var logEntries = parsedResult.LogEntries;

            var baseProjection = Projection.Index(logEntries);

            var timestampProjection = baseProjection.Compose(x => x.Timestamp);
            var fileNameProjection = baseProjection.Compose(x => x.FilePath);
            var lineNumberProjection = baseProjection.Compose(x => x.LineNumber);
            var pidProjection = baseProjection.Compose(x => x.PID);
            var tidProjection = baseProjection.Compose(x => x.TID);
            var priorityProjection = baseProjection.Compose(x => x.Priority);
            var tagProjection = baseProjection.Compose(x => x.Tag);
            var messageProjection = baseProjection.Compose(x => x.Message);

            var config = new TableConfiguration("Default")
            {
                Columns = new[]
                {
                    FileNameColumn,
                    TableConfiguration.PivotColumn,
                    LineNumberColumn,
                    TimestampAbsColumn,
                    PIDColumn,
                    TIDColumn,
                    PriorityColumn,
                    TagColumn,
                    MessageColumn,
                    TableConfiguration.GraphColumn,
                    TimestampColumn
                },
            };

            config.AddColumnRole(ColumnRole.StartTime, TimestampColumn);

            tableBuilder.AddTableConfiguration(config)
                .SetDefaultTableConfiguration(config)
                .SetRowCount(logEntries.Count)
                .AddColumn(FileNameColumn, fileNameProjection)
                .AddColumn(LineNumberColumn, lineNumberProjection)
                .AddColumn(PIDColumn, pidProjection)
                .AddColumn(TIDColumn, pidProjection)
                .AddColumn(PriorityColumn, priorityProjection)
                .AddColumn(MessageColumn, messageProjection)
                .AddColumn(TimestampColumn, timestampProjection)
                .AddColumn(TagColumn, tagProjection);
        }
    }
}
