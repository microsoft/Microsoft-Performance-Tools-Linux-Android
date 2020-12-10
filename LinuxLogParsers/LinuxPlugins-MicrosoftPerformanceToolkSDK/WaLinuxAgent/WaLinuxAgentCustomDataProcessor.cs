// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using Microsoft.Performance.SDK.Extensibility;
using LinuxLogParser.WaLinuxAgentLog;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using LinuxLogParserCore;

namespace WaLinuxAgentMPTAddin
{
    public sealed class WaLinuxAgentCustomDataProcessor
         : CustomDataProcessorBaseWithSourceParser<WaLinuxAgentLogParsedEntry, LogContext, LogParsedDataKey>
    {
        public WaLinuxAgentCustomDataProcessor(
            ISourceParser<WaLinuxAgentLogParsedEntry, LogContext, LogParsedDataKey> sourceParser,
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
