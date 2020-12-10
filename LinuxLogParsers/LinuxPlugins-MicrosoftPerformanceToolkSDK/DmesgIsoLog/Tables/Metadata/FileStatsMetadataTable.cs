// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using LinuxLogParser;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;

namespace DmesgIsoMPTAddin.Tables.Metadata
{
    [Table]
    public sealed class FileStatsMetadataTable
    {
        public static readonly TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{F720776E-329B-4C2E-927F-88BD5720D0E9}"),
            "File Stats",
            "Statistics for dmesg.iso.log files",
            isMetadataTable: true,
            requiredDataCookers: new List<DataCookerPath> {
                new DataCookerPath(SourceParserIds.DmesgIsoLog, DmesgIsoDataCooker.CookerId)
            });

        private static readonly ColumnConfiguration FileNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{64697A4A-68D9-4114-95E0-374E46AF0C87}"), "File Name", "The name of the file."),
            new UIHints { Width = 80, });

        private static readonly ColumnConfiguration LineCountColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{4FE35802-2D09-4EB1-ACE9-4D72F5A1E274}"), "Line Count", "The number of lines in the file."),
            new UIHints { Width = 80, });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            DmesgIsoLogParsedResult parsedResult = tableData.QueryOutput<DmesgIsoLogParsedResult>(
                DataOutputPath.Create(SourceParserIds.DmesgIsoLog, DmesgIsoDataCooker.CookerId, "ParsedResult"));
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
