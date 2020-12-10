// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using CtfPlayback.FieldValues;
using Microsoft.Performance.SDK;
using PerfDataExtensions.SourceDataCookers.Symbols;

namespace PerfDataExtensions.DataOutputTypes
{
    public interface ICpuClockEvent
    {
        ulong Cpu { get; }
        ulong Ip { get; }
        KernelSymbol Ip_Symbol { get; }
        ulong Tid { get; }
        ulong Pid { get; }
        ulong Id { get; }
        ulong Perf_Period { get; }
        ulong Perf_Callchain_Size { get; }
        long Perf_Callchain { get; }
        string[] CallStack { get; }
        Timestamp Timestamp { get; }
    }
}
