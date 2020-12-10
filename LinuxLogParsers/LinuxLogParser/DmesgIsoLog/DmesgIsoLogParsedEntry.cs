// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;

namespace LinuxLogParser.DmesgIsoLog
{
    public enum LogParsedDataKey
    {
        GeneralLog
    }

    public class DmesgIsoLogParsedEntry : IKeyedDataType<LogParsedDataKey>
    {
        private readonly LogParsedDataKey key;

        public DmesgIsoLogParsedEntry(LogParsedDataKey key)
        {
            this.key = key;
        }

        public int CompareTo(LogParsedDataKey other)
        {
            return key.CompareTo(other);
        }

        public LogParsedDataKey GetKey()
        {
            return key;
        }
    }

    public class LogEntry: DmesgIsoLogParsedEntry
    {
        public Timestamp timestamp;
        public string filePath;
        public ulong lineNumber;
        public string entity;
        public string topic;
        public string message;
        public string metadata;
        public string rawLog;

        public LogEntry(): base(LogParsedDataKey.GeneralLog)
        {
        }
    }
}
