// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;

namespace LinuxLogParser.AndroidLogcat
{
    public enum LogParsedDataKey
    {
        GeneralLog
    }

    public class AndroidLogcatLogParsedEntry : IKeyedDataType<LogParsedDataKey>
    {
        private readonly LogParsedDataKey key;

        public AndroidLogcatLogParsedEntry(LogParsedDataKey key)
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

    /// <summary>
    ///  Per https://developer.android.com/studio/debug/am-logcat
    ///  date time PID    TID    priority tag: message
    ///  Example: "12-13 10:32:24.869    26    26 I Checkpoint: cp_prepareCheckpoint called"
    /// </summary>
    public class LogEntry: AndroidLogcatLogParsedEntry
    {
        public Timestamp Timestamp;
        public string FilePath;
        public ulong LineNumber;
        public uint PID;
        public uint TID;
        public string Priority;
        public string Tag;
        public string Message;

        public LogEntry(): base(LogParsedDataKey.GeneralLog)
        {
        }
    }
}
