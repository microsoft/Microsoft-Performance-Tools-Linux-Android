// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Performance.SDK;

namespace LttngDataExtensions.DataOutputTypes
{
    public interface IDiskActivity
    {
        Timestamp? InsertTime { get; }

        Timestamp? IssueTime { get; }

        Timestamp? CompleteTime { get; }

        uint DeviceId { get; }

        string DeviceName { get; }

        ulong SectorNumber { get; }

        DataSize? Size { get; }

        string Filepath { get; }

        int ThreadId { get; }

        string ProcessId { get; }

        string ProcessCommand { get; }

        int Error { get; }
    }
}