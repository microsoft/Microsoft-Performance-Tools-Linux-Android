// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using LTTngCds.CookerData;
using LTTngCds.MetadataTables;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;

namespace LTTngCds
{
    internal sealed class LTTngDataProcessor
        : CustomDataProcessorBaseWithSourceParser<LTTngEvent, LTTngContext, string>,
          IDisposable
    {
        public LTTngDataProcessor(
            ISourceParser<LTTngEvent, LTTngContext, string> sourceParser, 
            ProcessorOptions options, 
            IApplicationEnvironment applicationEnvironment, 
            IProcessorEnvironment processorEnvironment,
            IReadOnlyDictionary<TableDescriptor, Action<ITableBuilder, IDataExtensionRetrieval>> allTablesMapping, 
            IEnumerable<TableDescriptor> metadataTables) 
            : base(sourceParser, options, applicationEnvironment, processorEnvironment, allTablesMapping, metadataTables)
        {
        }

        protected override void BuildTableCore(
            TableDescriptor tableDescriptor, 
            Action<ITableBuilder, IDataExtensionRetrieval> createTable, 
            ITableBuilder tableBuilder)
        {
            if (tableDescriptor.IsMetadataTable)
            {
                BuildMetadataTable(tableDescriptor, tableBuilder);
            }
        }

        private void BuildMetadataTable(
            TableDescriptor tableDescriptor, 
            ITableBuilder tableBuilder)
        {
            if (tableDescriptor.Guid == TraceStatsTable.TableDescriptor.Guid)
            {
                TraceStatsTable.BuildMetadataTable(
                    tableBuilder, 
                    this.SourceParser as LTTngSourceParser, 
                    this.ApplicationEnvironment.Serializer);
            }
        }

        public void Dispose()
        {
            var sourceParser = this.SourceParser as LTTngSourceParser;
            sourceParser?.Dispose();
        }
    }
}