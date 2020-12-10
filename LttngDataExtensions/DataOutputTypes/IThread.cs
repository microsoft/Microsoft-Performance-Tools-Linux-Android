// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Performance.SDK;

namespace LttngDataExtensions.SourceDataCookers.Thread
{
    public interface IThread
    {
        int ThreadId { get; }
        string ProcessId { get; }
        string Command { get; }
        Timestamp StartTime { get; }
        Timestamp ExitTime { get; }
        TimestampDelta ExecTime { get; }
        TimestampDelta ReadyTime { get; }
        TimestampDelta SleepTime { get; }
        TimestampDelta DiskSleepTime { get; }
        TimestampDelta StoppedTime { get; }
        TimestampDelta ParkedTime { get; }
        TimestampDelta IdleTime { get; }
    }
}
