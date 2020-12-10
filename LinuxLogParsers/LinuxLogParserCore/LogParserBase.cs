// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;

namespace LinuxLogParserCore
{
    public abstract class LogParserBase<TLogEntry, TKey>: SourceParserBase<TLogEntry, LogContext, TKey>
        where TLogEntry: IKeyedDataType<TKey>
    {
        public LogParserBase(string[] filePaths)
        {
            FilePaths = filePaths;
        }

        protected LogContext Context { get; private set; } = new LogContext();

        protected string[] FilePaths { get; private set; }
    }
}
