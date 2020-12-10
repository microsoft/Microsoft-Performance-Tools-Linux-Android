// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using LinuxLogParser.DmesgIsoLog;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using LinuxLogParserCore;

namespace DmesgIsoMPTAddin
{
    public sealed class DmesgIsoCustomDataProcessor
         : CustomDataProcessorBaseWithSourceParser<DmesgIsoLogParsedEntry, LogContext, LogParsedDataKey>
    {
        public DmesgIsoCustomDataProcessor(
            ISourceParser<DmesgIsoLogParsedEntry, LogContext, LogParsedDataKey> sourceParser,
            ProcessorOptions options,
            IApplicationEnvironment applicationEnvironment,
            IProcessorEnvironment processorEnvironment,
            IReadOnlyDictionary<TableDescriptor, Action<ITableBuilder, IDataExtensionRetrieval>> allTablesMapping,
            IEnumerable<TableDescriptor> metadataTables)
            : base(sourceParser, options, applicationEnvironment, processorEnvironment, allTablesMapping, metadataTables)
        {
        }
    }
}
