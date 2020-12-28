// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace LTTngDataExtensions.SourceDataCookers.Cpu
{
    public interface IContextSwitchOut
    {
        string ImageName { get; }

        int ThreadId { get; }

        int Priority { get; }
    }
}