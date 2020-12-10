// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParser;
using LinuxLogParser.CloudInitLog;
using LinuxLogParserCore;
using System.Collections.Generic;

namespace CloudInitMPTAddin
{
    public class CloudInitLogParsedResult
    {
        public List<LogEntry> LogEntries { get; }
        public IReadOnlyDictionary<string, FileMetadata> FileToMetadata { get; }

        public CloudInitLogParsedResult(List<LogEntry> logEntries, Dictionary<string, FileMetadata> fileToMetadata)
        {
            LogEntries = logEntries;
            FileToMetadata = fileToMetadata;
        }
    }
}
