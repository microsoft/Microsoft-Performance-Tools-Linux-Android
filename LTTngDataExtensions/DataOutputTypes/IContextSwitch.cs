// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LTTngDataExtensions.SourceDataCookers.Cpu;
using Microsoft.Performance.SDK;

namespace LTTngDataExtensions.DataOutputTypes
{
    public interface IContextSwitch
    {
        IContextSwitchOut SwitchOut { get; }

        IContextSwitchIn SwitchIn { get; }

        Timestamp Timestamp { get; }
    }
}