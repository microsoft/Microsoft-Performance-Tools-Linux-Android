// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace LTTngDataExtensions.SourceDataCookers.Process
{
    public interface IProcess
    {
        ulong ForkTime { get; }
        ulong ExecTime { get; }
        ulong ExitTime { get; }
        ulong WaitTime { get; }
        ulong FreeTime { get; }
        int ProcessId { get; }
        int ParentProcessId { get; }
        string Name { get; }
        string Path { get; }
    }
}