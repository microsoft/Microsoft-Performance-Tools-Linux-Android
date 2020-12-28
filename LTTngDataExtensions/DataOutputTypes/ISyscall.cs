// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Performance.SDK;

namespace LTTngDataExtensions.SourceDataCookers.Syscall
{
    public interface ISyscall
    {
        string Name { get; }
        string ReturnValue { get; }
        string ThreadId { get; }
        string ProcessId { get; }
        string ProcessCommand { get; }
        string Arguments { get; }
        Timestamp StartTime { get; }
        Timestamp EndTime { get; }
    }
}
