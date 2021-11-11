// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParser;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WaLinuxAgentMPTAddin.Tables.Metadata
{
    //
    // This is a sample Metadata table for .txt files
    // Metadata tables are used to expose information about the data being processed, not the actual data being processed.
    // Metadata would be something like "number of events in the file" or "file size" or any other number of things that describes the data being processed.
    // In this sample table, we expose three columns: File Name, Line Count and Word Count.
    //

    //
    // In order for the ProcessingSource to understand your metadata table, 
    // add a MetadataTable attribute which denotes this table as metadata.
    //

    [Table]

    //
    // Have the MetadataTable inherit the TableBase class
    //
    public static class FileStatsMetadataTable
    {
        public static readonly TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{10a8a80a-27a3-42b8-8cde-3a374080e01e}"),
            "File Stats",
            "Statistics for text files",
            isMetadataTable: true,
            requiredDataCookers: new List<DataCookerPath> {
                DataCookerPath.ForSource(SourceParserIds.WaLinuxAgentLog, WaLinuxAgentDataCooker.CookerId)
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
            WaLinuxAgentLogParsedResult parsedResult = tableData.QueryOutput<WaLinuxAgentLogParsedResult>(
                DataOutputPath.ForSource(SourceParserIds.WaLinuxAgentLog, WaLinuxAgentDataCooker.CookerId, nameof(WaLinuxAgentDataCooker.ParsedResult)));
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
