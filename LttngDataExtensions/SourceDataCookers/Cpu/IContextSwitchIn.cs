// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace LttngDataExtensions.SourceDataCookers.Cpu
{
    public interface IContextSwitchIn
    {
        string ImageName { get; }

        int ThreadId { get; }

        int Priority { get; }
    }
}