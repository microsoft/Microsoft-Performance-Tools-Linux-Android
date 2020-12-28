// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Performance.SDK;

namespace LTTngDataExtensions.SourceDataCookers.Thread
{
    public interface IExecutionEvent
    {
        uint Cpu { get; }
        string NextPid { get; }
        int NextTid { get; }
        string PreviousPid { get; }
        int PreviousTid { get; }
        int Priority { get; }
        string ReadyingPid { get; }
        string ReadyingTid { get; }
        string PreviousState { get; }
        string NextImage { get; }
        string PreviousImage { get; }
        TimestampDelta ReadyTime { get; }
        TimestampDelta WaitTime { get; }
        Timestamp SwitchInTime { get; }
        Timestamp SwitchOutTime { get; }
        Timestamp NextThreadPreviousSwitchOutTime { get; }
    }
}
