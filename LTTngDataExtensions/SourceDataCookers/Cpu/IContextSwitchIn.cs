// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace LTTngDataExtensions.SourceDataCookers.Cpu
{
    public interface IContextSwitchIn
    {
        string ImageName { get; }

        int ThreadId { get; }

        int Priority { get; }
    }
}