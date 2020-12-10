// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;

namespace LinuxLogParser.CloudInitLog
{
    public enum LogParsedDataKey
    {
        GeneralLog
    }

    public class CloudInitLogParsedEntry : IKeyedDataType<LogParsedDataKey>
    {
        private readonly LogParsedDataKey key;

        public CloudInitLogParsedEntry(LogParsedDataKey key)
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

    public class LogEntry: CloudInitLogParsedEntry
    {
        public string FilePath { get; private set; }
        public ulong LineNumber { get; private set; }
        public Timestamp EventTimestamp { get; private set; }

        public string PythonFile { get; private set; }

        public string LogLevel { get; private set; }
        public string Log { get; private set; }

        public LogEntry(string filePath, ulong lineNumber, Timestamp eventTimestamp, string pythonFile,
            string logLevel, string log):
            base(LogParsedDataKey.GeneralLog)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
            EventTimestamp = eventTimestamp;
            PythonFile = pythonFile;
            LogLevel = logLevel;
            Log = log;
        }
    }
}
