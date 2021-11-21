// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParser;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;

namespace WaLinuxAgentMPTAddin.Tables
{
    //
    // Add a Table attribute in order for the ProcessingSource to understand your table.
    // 

    [Table]

    //
    // Have the MetadataTable inherit the TableBase class
    //

    public sealed class WaLinuxAgentTable
    {
        public static readonly TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{31bccac5-f015-4e15-a0a8-bcf93194a850}"),
            "WaLinuxAgent",
            "waagent Log",
            category: "Linux",
            requiredDataCookers: new List<DataCookerPath> {
                DataCookerPath.ForSource(SourceParserIds.WaLinuxAgentLog, WaLinuxAgentDataCooker.CookerId)
            });

        private static readonly ColumnConfiguration FileNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{9F1FF010-868A-441B-983F-32FD7B92B6D1}"), "FileName", "File Name"),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration LineNumberColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{ae17deac-1774-41fd-b435-6aa463a44191}"), "LineNumber", "Log Line Number"),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration EventTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{ef0430ff-3ba6-42ab-ba36-6145a17e3879}"), "Event Time", "The timestamp of the log entry"),
            new UIHints { Width = 80, SortPriority = 0, SortOrder = SortOrder.Ascending });

        // todo: needs to be changed by user manually to DateTime UTC format. SDK doesn't yet support specifying this <DateTimeTimestampOptionsParameter DateTimeEnabled="true" />
        private static readonly ColumnConfiguration EventTimestampDateTimeColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{574aa62a-fc79-47ba-a43e-e65d5db7572a}"), "Event Time", "The timestamp of the log entry"),
            new UIHints { Width = 130 });

        private static readonly ColumnConfiguration LogLevelColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{c617df4d-0eaf-4b31-9af2-90b5205fde5f}"), "LogLevel", "Log level"),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration LogColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{b0bcd818-58e1-4d93-8309-a1cff3da5976}"), "Log", "Log Entry"),
            new UIHints { Width = 1200 });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            WaLinuxAgentLogParsedResult parsedResult = tableData.QueryOutput<WaLinuxAgentLogParsedResult>(
               DataOutputPath.ForSource(SourceParserIds.WaLinuxAgentLog, WaLinuxAgentDataCooker.CookerId, nameof(WaLinuxAgentDataCooker.ParsedResult)));
            var logEntries = parsedResult.LogEntries;

            var baseProjection = Projection.Index(logEntries);

            var fileNameProjection = baseProjection.Compose(x => x.FilePath);
            var lineNumberProjection = baseProjection.Compose(x => x.LineNumber);
            var eventTimeProjection = baseProjection.Compose(x => x.EventTimestamp);
            var logLevelProjection = baseProjection.Compose(x => x.LogLevel);
            var logProjection = baseProjection.Compose(x => x.Log);

            //
            // Table Configurations describe how your table should be presented to the user: 
            // the columns to show, what order to show them, which columns to aggregate, and which columns to graph. 
            // You may provide a number of columns in your table, but only want to show a subset of them by default so as not to overwhelm the user. 
            // The user can still open the table properties in UI to turn on or off columns.
            // The table configuration class also exposes four (4) columns that UI explicitly recognizes: Pivot Column, Graph Column, Left Freeze Column, Right Freeze Column
            // For more information about what these columns do, go to "Advanced Topics" -> "Table Configuration" in our Wiki. Link can be found in README.md
            //

            var config = new TableConfiguration("Default")
            {
                Columns = new[]
              {
                    LogLevelColumn,
                    TableConfiguration.PivotColumn,
                    LineNumberColumn,
                    LogColumn,
                    EventTimestampDateTimeColumn,
                    TableConfiguration.GraphColumn,
                    EventTimestampColumn,

                },
            };

            config.AddColumnRole(ColumnRole.StartTime, EventTimestampColumn);

            //
            //
            //  Use the table builder to build the table. 
            //  Add and set table configuration if applicable.
            //  Then set the row count (we have one row per file) and then add the columns using AddColumn.
            //
            tableBuilder
            .AddTableConfiguration(config)
                .SetDefaultTableConfiguration(config)
                .SetRowCount(logEntries.Count)
                .AddColumn(FileNameColumn, fileNameProjection)
                .AddColumn(LineNumberColumn, lineNumberProjection)
                .AddColumn(EventTimestampColumn, eventTimeProjection)
                .AddColumn(LogLevelColumn, logLevelProjection)
                .AddColumn(LogColumn, logProjection);
        }
    }
}
