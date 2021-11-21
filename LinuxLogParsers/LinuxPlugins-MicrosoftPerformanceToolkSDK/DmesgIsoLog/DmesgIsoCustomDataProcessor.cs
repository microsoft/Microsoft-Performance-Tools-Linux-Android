// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParser.DmesgIsoLog;
using LinuxLogParserCore;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;

namespace DmesgIsoMPTAddin
{
    public sealed class DmesgIsoCustomDataProcessor
         : CustomDataProcessorWithSourceParser<DmesgIsoLogParsedEntry, LogContext, LogParsedDataKey>
    {
        public DmesgIsoCustomDataProcessor(
            ISourceParser<DmesgIsoLogParsedEntry, LogContext, LogParsedDataKey> sourceParser,
            ProcessorOptions options,
            IApplicationEnvironment applicationEnvironment,
            IProcessorEnvironment processorEnvironment)
            : base(sourceParser, options, applicationEnvironment, processorEnvironment)
        {
        }
    }
}
