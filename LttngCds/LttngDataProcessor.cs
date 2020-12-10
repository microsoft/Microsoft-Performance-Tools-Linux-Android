// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using LttngCds.CookerData;
using LttngCds.MetadataTables;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;

namespace LttngCds
{
    internal sealed class LttngDataProcessor
        : CustomDataProcessorBaseWithSourceParser<LttngEvent, LttngContext, string>,
          IDisposable
    {
        public LttngDataProcessor(
            ISourceParser<LttngEvent, LttngContext, string> sourceParser, 
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
                    this.SourceParser as LttngSourceParser, 
                    this.ApplicationEnvironment.Serializer);
            }
        }

        public void Dispose()
        {
            var sourceParser = this.SourceParser as LttngSourceParser;
            sourceParser?.Dispose();
        }
    }
}