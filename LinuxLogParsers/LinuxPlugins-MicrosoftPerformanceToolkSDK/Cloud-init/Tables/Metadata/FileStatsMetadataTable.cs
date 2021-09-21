// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParser;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudInitMPTAddin.Tables.Metadata
{
    [Table]
    public sealed class FileStatsMetadataTable
    {
        public static readonly TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{806a2599-97a2-4ff7-9ed8-9ac891edeef6}"),
            "File Stats",
            "Statistics for text files",
            isMetadataTable: true,
            requiredDataCookers: new List<DataCookerPath> {
                new DataCookerPath(SourceParserIds.CloudInitLog, CloudInitDataCooker.CookerId)
            });

        private static readonly ColumnConfiguration FileNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{2604E009-F47D-4A22-AA4F-B148D1C26553}"), "File Name", "The name of the file."),
            new UIHints { Width = 80 });

        private static readonly ColumnConfiguration LineCountColumn = new ColumnConfiguration(
           new ColumnMetadata(new Guid("{C499AF57-64D1-47A9-8550-CF24D6C9615D}"), "Line Count", "The number of lines in the file."),
           new UIHints { Width = 80 });

        private static readonly ColumnConfiguration WordCountColumn = new ColumnConfiguration(
           new ColumnMetadata(new Guid("{3669E90A-DC8F-4972-A5D3-3E13AFDF5DB7}"), "Word Count", "The number of words in the file."),
           new UIHints { Width = 80 });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            CloudInitLogParsedResult parsedResult = tableData.QueryOutput<CloudInitLogParsedResult>(
                DataOutputPath.Create(SourceParserIds.CloudInitLog, CloudInitDataCooker.CookerId, "ParsedResult"));
            var fileNames = parsedResult.FileToMetadata.Keys.ToArray();
            var fileNameProjection = Projection.Index(fileNames.AsReadOnly());

            var lineCountProjection = fileNameProjection.Compose(
                fileName => parsedResult.FileToMetadata[fileName].LineCount);

            tableBuilder.SetRowCount(fileNames.Length)
                .AddColumn(FileNameColumn, fileNameProjection)
                .AddColumn(LineCountColumn, lineCountProjection);
        }
    }
}
