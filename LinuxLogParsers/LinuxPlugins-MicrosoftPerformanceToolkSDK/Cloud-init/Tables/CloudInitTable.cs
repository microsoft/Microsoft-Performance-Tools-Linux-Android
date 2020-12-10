// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParser;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;

namespace CloudInitMPTAddin.Tables
{
    [Table]
    public static class CloudInitTable
    {
        public static readonly TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{D12FF3CB-515D-406C-A000-7CA8C8ED2DAB}"),
            "Cloud-Init",
            "Cloud-Init Log",
            category: "Linux",
            requiredDataCookers: new List<DataCookerPath> {
                new DataCookerPath(SourceParserIds.CloudInitLog, CloudInitDataCooker.CookerId)
            });

        //
        // Declare columns here. You can do this using the ColumnConfiguration class. 
        // It is possible to declaratively describe the table configuration as well. Please refer to our Advanced Topics Wiki page for more information.
        //
        // The Column metadata describes each column in the table. 
        // Each column must have a unique GUID and a unique name. The GUID must be unique globally; the name only unique within the table.
        //
        // The UIHints provides some hints to UI on how to render the column. 
        // In this sample, we are simply saying to allocate at least 80 units of width.
        //

        private static readonly ColumnConfiguration FileNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{69BC9313-CFC9-4D12-BA2D-D520126E89A8}"), "FileName", "File Name"),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration LineNumberColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{82f4cceb-5044-4f92-bc34-00ab9d0d22cb}"), "LineNumber", "Log Line Number"),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration EventTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{4ff97511-d34f-4fd9-bf3d-adba27869332}"), "Event Time", "The timestamp of the log entry"),
            new UIHints { Width = 80, SortPriority = 0, SortOrder = SortOrder.Ascending });

        // todo: needs to be changed by user manually to DateTime UTC format. SDK doesn't yet support specifying this <DateTimeTimestampOptionsParameter DateTimeEnabled="true" />
        private static readonly ColumnConfiguration EventTimestampDateTimeColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{4ff97511-d34f-4fd9-bf3d-adba27869332}"), "Event Time", "The timestamp of the log entry"),
            new UIHints { Width = 130 });

        private static readonly ColumnConfiguration PythonFileColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{70dfb005-2aeb-492b-a693-38c841437864}"), "PythonFile", "Python file logged from"),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration LogLevelColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{59941146-702b-4042-8ef9-f8ad38a7a495}"), "LogLevel", "Log level"),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration LogColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{4facff7c-2e32-44d5-83db-9d3eaed43621}"), "Log", "Log Entry"),
            new UIHints { Width = 1200 });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            CloudInitLogParsedResult parsedResult = tableData.QueryOutput<CloudInitLogParsedResult>(
               DataOutputPath.Create(SourceParserIds.CloudInitLog, CloudInitDataCooker.CookerId, "ParsedResult"));
            var logEntries = parsedResult.LogEntries;

            var baseProjection = Projection.Index(logEntries);

            var fileNameProjection = baseProjection.Compose(x => x.FilePath);
            var lineNumberProjection = baseProjection.Compose(x => x.LineNumber);
            var eventTimeProjection = baseProjection.Compose(x => x.EventTimestamp);
            var pythonFileProjection = baseProjection.Compose(x => x.PythonFile);
            var logLevelProjection = baseProjection.Compose(x => x.LogLevel);
            var logProjection = baseProjection.Compose(x => x.Log);

            var config = new TableConfiguration("Default")
            {
                Columns = new[]
              {
                    FileNameColumn,
                    LogLevelColumn,
                    TableConfiguration.PivotColumn,
                    PythonFileColumn,
                    LineNumberColumn,
                    LogColumn,
                    EventTimestampDateTimeColumn,
                    TableConfiguration.GraphColumn,
                    EventTimestampColumn,

                },
                Layout = TableLayoutStyle.GraphAndTable,
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
                .AddColumn(PythonFileColumn, pythonFileProjection)
                .AddColumn(LogLevelColumn, logLevelProjection)
                .AddColumn(LogColumn, logProjection)
                ;
        }
    }
}
