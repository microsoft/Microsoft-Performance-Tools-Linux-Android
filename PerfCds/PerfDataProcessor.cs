// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using PerfCds.CookerData;
using PerfCds.MetadataTables;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;

namespace PerfCds
{
    internal sealed class PerfDataProcessor
        : CustomDataProcessorBaseWithSourceParser<PerfEvent, PerfContext, string>,
          IDisposable
    {
        public PerfDataProcessor(
            ISourceParser<PerfEvent, PerfContext, string> sourceParser, 
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
                    this.SourceParser as PerfSourceParser, 
                    this.ApplicationEnvironment.Serializer);
            }
        }

        public void Dispose()
        {
            var sourceParser = this.SourceParser as PerfSourceParser;
            sourceParser?.Dispose();
        }
    }
}