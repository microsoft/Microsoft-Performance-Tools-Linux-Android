// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace LttngDataExtensions.SourceDataCookers.Cpu
{
    public interface IContextSwitchOut
    {
        string ImageName { get; }

        int ThreadId { get; }

        int Priority { get; }
    }
}