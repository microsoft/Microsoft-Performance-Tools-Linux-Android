// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LttngDataExtensions.SourceDataCookers.Cpu;
using Microsoft.Performance.SDK;

namespace LttngDataExtensions.DataOutputTypes
{
    public interface IContextSwitch
    {
        IContextSwitchOut SwitchOut { get; }

        IContextSwitchIn SwitchIn { get; }

        Timestamp Timestamp { get; }
    }
}