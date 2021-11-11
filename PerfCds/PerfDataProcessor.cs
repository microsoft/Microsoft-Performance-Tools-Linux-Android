// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;
using PerfCds.CookerData;
using PerfCds.MetadataTables;
using System;

namespace PerfCds
{
    internal sealed class PerfDataProcessor
        : CustomDataProcessorWithSourceParser<PerfEvent, PerfContext, string>,
          IDisposable
    {
        public PerfDataProcessor(
            ISourceParser<PerfEvent, PerfContext, string> sourceParser,
            ProcessorOptions options,
            IApplicationEnvironment applicationEnvironment,
            IProcessorEnvironment processorEnvironment)
            : base(sourceParser, options, applicationEnvironment, processorEnvironment)
        {
        }

        protected override void BuildTableCore(
            TableDescriptor tableDescriptor,
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