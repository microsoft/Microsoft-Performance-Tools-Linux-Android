// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParser.WaLinuxAgentLog;
using LinuxLogParserCore;
using System.Collections.Generic;

namespace WaLinuxAgentMPTAddin
{
    public class WaLinuxAgentLogParsedResult
    {
        public List<LogEntry> LogEntries { get; }
        public IReadOnlyDictionary<string, FileMetadata> FileToMetadata { get; }

        public WaLinuxAgentLogParsedResult(List<LogEntry> logEntries, Dictionary<string, FileMetadata> fileToMetadata)
        {
            LogEntries = logEntries;
            FileToMetadata = fileToMetadata;
        }
    }
}
