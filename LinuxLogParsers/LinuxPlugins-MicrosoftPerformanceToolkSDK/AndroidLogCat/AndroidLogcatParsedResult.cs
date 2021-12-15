// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParser.AndroidLogcat;
using LinuxLogParserCore;
using System.Collections.Generic;

namespace AndroidLogcatMPTAddin
{
    public class AndroidLogcatParsedResult
    {
        public List<LogEntry> LogEntries { get; }
        public IReadOnlyDictionary<string, FileMetadata> FileToMetadata { get; }

        public AndroidLogcatParsedResult(List<LogEntry> logEntries, Dictionary<string, FileMetadata> fileToMetadata)
        {
            LogEntries = logEntries;
            FileToMetadata = fileToMetadata;
        }
    }
}
