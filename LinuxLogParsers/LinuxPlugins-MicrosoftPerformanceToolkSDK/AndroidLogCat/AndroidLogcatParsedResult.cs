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
        public List<DurationLogEntry> DurationLogEntries;
        public IReadOnlyDictionary<string, FileMetadata> FileToMetadata { get; }

        public AndroidLogcatParsedResult(List<LogEntry> logEntries, List<DurationLogEntry> durationLogEntries, Dictionary<string, FileMetadata> fileToMetadata)
        {
            LogEntries = logEntries;
            DurationLogEntries = durationLogEntries;
            FileToMetadata = fileToMetadata;
        }
    }
}
