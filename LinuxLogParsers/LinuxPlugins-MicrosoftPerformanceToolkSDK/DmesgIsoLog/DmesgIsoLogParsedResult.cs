// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParser.DmesgIsoLog;
using LinuxLogParserCore;
using System.Collections.Generic;

namespace DmesgIsoMPTAddin
{
    public class DmesgIsoLogParsedResult
    {
        public List<LogEntry> LogEntries { get; }
        public IReadOnlyDictionary<string, FileMetadata> FileToMetadata { get; }

        public DmesgIsoLogParsedResult(List<LogEntry> logEntries, Dictionary<string, FileMetadata> fileToMetadata)
        {
            LogEntries = logEntries;
            FileToMetadata = fileToMetadata;
        }
    }
}
