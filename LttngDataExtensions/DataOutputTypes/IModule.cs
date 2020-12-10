// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Performance.SDK;

namespace LttngDataExtensions.SourceDataCookers.Module
{
    public interface IModuleEvent
    {
        string EventType { get; }
        string InstructionPointer { get; }
        string ThreadId { get; }
        string ProcessId { get; }
        string ProcessCommand { get; }
        int RefCount { get; }
        string ModuleName { get; }
        Timestamp Time { get; }
    }
}
