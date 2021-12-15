// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParser.AndroidLogcat;
using LinuxLogParserCore;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;

namespace AndroidLogcatMPTAddin
{
    public sealed class AndroidLogcatCustomDataProcessor
         : CustomDataProcessorWithSourceParser<AndroidLogcatLogParsedEntry, LogContext, LogParsedDataKey>
    {
        public AndroidLogcatCustomDataProcessor(
            ISourceParser<AndroidLogcatLogParsedEntry, LogContext, LogParsedDataKey> sourceParser,
            ProcessorOptions options,
            IApplicationEnvironment applicationEnvironment,
            IProcessorEnvironment processorEnvironment)
            : base(sourceParser, options, applicationEnvironment, processorEnvironment)
        {
        }
    }
}
