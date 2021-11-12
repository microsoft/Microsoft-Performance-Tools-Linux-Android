// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParser.CloudInitLog;
using LinuxLogParserCore;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;

namespace CloudInitMPTAddin
{
    public sealed class CloudInitCustomDataProcessor
         : CustomDataProcessorWithSourceParser<CloudInitLogParsedEntry, LogContext, LogParsedDataKey>
    {
        public CloudInitCustomDataProcessor(
            ISourceParser<CloudInitLogParsedEntry, LogContext, LogParsedDataKey> sourceParser,
            ProcessorOptions options,
            IApplicationEnvironment applicationEnvironment,
            IProcessorEnvironment processorEnvironment)
            : base(sourceParser, options, applicationEnvironment, processorEnvironment)
        {
        }
    }
}
