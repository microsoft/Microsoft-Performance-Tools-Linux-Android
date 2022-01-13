// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using LinuxLogParser;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;

namespace AndroidLogcatMPTAddin.Tables
{
    [Table]
    public static class DurationTable
    {
        public static readonly TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{9C29BFFE-2678-4C84-A9B5-19313C5EB4F9}"),
            "Android Logcat Durations",
            "Android Logcat Durations",
            category: "Android",
            requiredDataCookers: new List<DataCookerPath> {
                DataCookerPath.ForSource(SourceParserIds.AndroidLogcatLog, AndroidLogcatDataCooker.CookerId)
            });

        private static readonly ColumnConfiguration FileNameColumn = new ColumnConfiguration(
           new ColumnMetadata(new Guid("{E550F06E-523C-47FA-BB19-1929D9F42754}"), "FileName", "File Name"),
           new UIHints { Width = 80 });

        private static readonly ColumnConfiguration NameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{DF14D97F-866E-4DEC-8ECA-88DEB60CFFDF}"), "Name", "Name of the duration"),
            new UIHints { Width = 80, });

        private static readonly ColumnConfiguration StartTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{83DDA2B5-FB97-4BD1-B139-B42321B47AA1}"), "Start Timestamp", "Timestamp when the duration started."),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration StartTimestampColumnSorted = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{83DDA2B5-FB97-4BD1-B139-B42321B47AA1}"), "Start Timestamp", "Timestamp when the duration started."),
            new UIHints { Width = 80, SortOrder = SortOrder.Ascending, SortPriority = 0 });

        private static readonly ColumnConfiguration EndTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{FDEAAF97-F0C0-4567-AA45-6574D10D3523}"), "End Timestamp", "Timestamp when the duration ended"),
            new UIHints { Width = 80, });

        private static readonly ColumnConfiguration DurationColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{3894B186-D168-4E46-B0B5-0091BD5807A1}"), "Duration", "Duration of the event"),
            new UIHints { Width = 80, CellFormat = TimestampFormatter.FormatMillisecondsGrouped });

        private static readonly ColumnConfiguration DurationColumnOrderedMax = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{3894B186-D168-4E46-B0B5-0091BD5807A1}"), "Duration", "Duration of the event"),
            new UIHints 
            {
                SortOrder = SortOrder.Descending,
                SortPriority = 0,
                Width = 80, 
                CellFormat = TimestampFormatter.FormatMillisecondsGrouped, 
                AggregationMode = AggregationMode.Max 
            });

        private static readonly ColumnConfiguration LineNumberColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{EDBD9BAF-0BD8-43D7-9752-BFCEF3FCBC13}"), "Line Number", "Ordering of the lines in the file"),
            new UIHints
            {
                SortOrder = SortOrder.Ascending,
                SortPriority = 1,
                Width = 80,
            });

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

            var durationEntries = parsedResult.DurationLogEntries;

            var baseProjection = Projection.Index(durationEntries);

            var nameProjection = baseProjection.Compose(x => x.Name);
            var startTimestampProjection = baseProjection.Compose(x => x.StartTimestamp);
            var endTimestampProjection = baseProjection.Compose(x => x.EndTimestamp);
            var durationProjection = baseProjection.Compose(x => x.Duration);
            var fileNameProjection = baseProjection.Compose(x => x.FilePath);
            var lineNumberProjection = baseProjection.Compose(x => x.LineNumber);
            var pidProjection = baseProjection.Compose(x => x.PID);
            var tidProjection = baseProjection.Compose(x => x.TID);
            var priorityProjection = baseProjection.Compose(x => x.Priority);
            var tagProjection = baseProjection.Compose(x => x.Tag);
            var messageProjection = baseProjection.Compose(x => x.Message);

            var timeOrderConfig = new TableConfiguration("Time Order")
            {
                Columns = new[]
                {
                    FileNameColumn,
                    LineNumberColumn,
                    NameColumn,
                    TableConfiguration.PivotColumn,
                    PIDColumn,
                    TIDColumn,
                    PriorityColumn,
                    TagColumn,
                    MessageColumn,
                    DurationColumn,
                    TableConfiguration.GraphColumn,
                    StartTimestampColumnSorted,
                    EndTimestampColumn
                },
            };

            timeOrderConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumnSorted);
            timeOrderConfig.AddColumnRole(ColumnRole.EndTime, EndTimestampColumn);
            timeOrderConfig.AddColumnRole(ColumnRole.Duration, DurationColumn);

            var longestDurationConfig = new TableConfiguration("Longest Duration")
            {
                Columns = new[]
                {
                    FileNameColumn,
                    TagColumn,
                    NameColumn,
                    TableConfiguration.PivotColumn,
                    LineNumberColumn,
                    PIDColumn,
                    TIDColumn,
                    PriorityColumn,
                    MessageColumn,
                    DurationColumnOrderedMax,
                    TableConfiguration.GraphColumn,
                    StartTimestampColumn,
                    EndTimestampColumn
                },
            };

            longestDurationConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumnSorted);
            longestDurationConfig.AddColumnRole(ColumnRole.EndTime, EndTimestampColumn);
            longestDurationConfig.AddColumnRole(ColumnRole.Duration, DurationColumnOrderedMax);

            tableBuilder
                .AddTableConfiguration(timeOrderConfig)
                .AddTableConfiguration(longestDurationConfig)
                .SetDefaultTableConfiguration(longestDurationConfig)
                .SetRowCount(durationEntries.Count)
                .AddColumn(NameColumn, nameProjection)
                .AddColumn(FileNameColumn, fileNameProjection)
                .AddColumn(LineNumberColumn, lineNumberProjection)
                .AddColumn(PIDColumn, pidProjection)
                .AddColumn(TIDColumn, pidProjection)
                .AddColumn(PriorityColumn, priorityProjection)
                .AddColumn(MessageColumn, messageProjection)
                .AddColumn(DurationColumn, durationProjection)
                .AddColumn(StartTimestampColumnSorted, startTimestampProjection)
                .AddColumn(EndTimestampColumn, endTimestampProjection)
                .AddColumn(TagColumn, tagProjection);
        }
    }
}
