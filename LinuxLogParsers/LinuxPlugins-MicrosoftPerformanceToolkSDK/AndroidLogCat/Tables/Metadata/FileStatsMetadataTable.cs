// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using LinuxLogParser;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;

namespace AndroidLogcatMPTAddin.Tables.Metadata
{
    [Table]
    public sealed class FileStatsMetadataTable
    {
        public static readonly TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{8D9DA96E-07D9-4C13-9F38-C1A2F4E70578}"),
            "File Stats",
            "Statistics for Android logcat .log files",
            isMetadataTable: true,
            requiredDataCookers: new List<DataCookerPath> {
                DataCookerPath.ForSource(SourceParserIds.AndroidLogcatLog, AndroidLogcatDataCooker.CookerId)
            });

        private static readonly ColumnConfiguration FileNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{3DF95CE9-1BB4-428E-AF75-E8D53526ABA7}"), "File Name", "The name of the file."),
            new UIHints { Width = 80, });

        private static readonly ColumnConfiguration LineCountColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{5D78C14B-2CEF-4C1F-9D3B-FC713992D3C6}"), "Line Count", "The number of lines in the file."),
            new UIHints { Width = 80, });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            var parsedResult = tableData.QueryOutput<AndroidLogcatParsedResult>(
                DataOutputPath.ForSource(SourceParserIds.AndroidLogcatLog, AndroidLogcatDataCooker.CookerId, nameof(AndroidLogcatDataCooker.ParsedResult)));
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
