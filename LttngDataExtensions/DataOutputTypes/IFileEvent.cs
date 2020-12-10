// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Performance.SDK;

namespace LttngDataExtensions.DataOutputTypes
{
    public interface IFileEvent
    {
        string Name { get; }
        string ProcessId { get; }
        string ProcessCommand { get; }
        int ThreadId { get; }
        DataSize Size { get; }
        string Filepath { get; }
        Timestamp StartTime { get; }
        Timestamp EndTime { get; }
    }
}
