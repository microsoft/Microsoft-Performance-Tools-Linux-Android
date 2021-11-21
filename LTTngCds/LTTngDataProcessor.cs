// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using LTTngCds.CookerData;
using LTTngCds.MetadataTables;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;

namespace LTTngCds
{
    internal sealed class LTTngDataProcessor
        : CustomDataProcessorWithSourceParser<LTTngEvent, LTTngContext, string>,
          IDisposable
    {
        public LTTngDataProcessor(
            ISourceParser<LTTngEvent, LTTngContext, string> sourceParser,
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