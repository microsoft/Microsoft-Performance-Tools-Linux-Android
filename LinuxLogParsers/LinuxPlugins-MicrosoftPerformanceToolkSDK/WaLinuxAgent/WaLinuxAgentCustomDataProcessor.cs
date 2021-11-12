// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParser.WaLinuxAgentLog;
using LinuxLogParserCore;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;

namespace WaLinuxAgentMPTAddin
{
    public sealed class WaLinuxAgentCustomDataProcessor
         : CustomDataProcessorWithSourceParser<WaLinuxAgentLogParsedEntry, LogContext, LogParsedDataKey>
    {
        public WaLinuxAgentCustomDataProcessor(
            ISourceParser<WaLinuxAgentLogParsedEntry, LogContext, LogParsedDataKey> sourceParser,
            ProcessorOptions options,
            IApplicationEnvironment applicationEnvironment,
            IProcessorEnvironment processorEnvironment)
            : base(sourceParser, options, applicationEnvironment, processorEnvironment)
        {
        }
    }
}
