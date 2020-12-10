// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.FieldValues;
using PerfDataExtensions.DataOutputTypes;
using Microsoft.Performance.SDK;
using System.Collections.Generic;
using System.Text;
using System;
using System.Linq;
using PerfCds.CookerData;
using PerfDataExtensions.SourceDataCookers.Symbols;
using System.Collections.Concurrent;

namespace PerfDataExtensions.SourceDataCookers.Cpu
{
    public class CpuClockEvent
        : ICpuClockEvent
    {
        public ulong Cpu { get; }
        public ulong Ip { get; }
        public KernelSymbol Ip_Symbol { get; }
        public ulong Tid { get; }
        public ulong Pid { get; }
        public ulong Id { get; }
        public ulong Perf_Period { get; }
        public ulong Perf_Callchain_Size { get; }
        public long Perf_Callchain { get; }
        public string[] CallStack { get; }
        public Timestamp Timestamp { get; }

        private static ConcurrentDictionary<ulong, KernelSymbol> cachedIpSymList = new ConcurrentDictionary<ulong, KernelSymbol>();
        private static KernelSymbol maxKernelSymbol;

        public CpuClockEvent(PerfEvent data, PerfContext context, SortedList<ulong, KernelSymbol> kernelSymbols)
        {
            this.Timestamp = data.Timestamp;
            this.Cpu = context.CurrentCpu;
            if (data.Payload != null)
            {
                if (data.Payload.FieldsByName.ContainsKey("perf_ip"))
                {
                    Ip = data.Payload.ReadFieldAsUInt64("perf_ip");

                    if (kernelSymbols.Count > 0)
                    {
                        Ip_Symbol = FindSymbolForIp(Ip, kernelSymbols);
                    }
                }
                if (data.Payload.FieldsByName.ContainsKey("perf_tid"))
                {
                    Tid = data.Payload.ReadFieldAsUInt64("perf_tid");
                }
                if (data.Payload.FieldsByName.ContainsKey("perf_pid"))
                {
                    Pid = data.Payload.ReadFieldAsUInt64("perf_pid");
                }
                if (data.Payload.FieldsByName.ContainsKey("perf_id"))
                {
                    Id = data.Payload.ReadFieldAsUInt64("perf_id");
                }
                if (data.Payload.FieldsByName.ContainsKey("perf_period"))
                {
                    Perf_Period = data.Payload.ReadFieldAsUInt64("perf_period");
                }
                if (data.Payload.FieldsByName.ContainsKey("perf_callchain_size"))
                {
                    Perf_Callchain_Size = data.Payload.ReadFieldAsUInt64("perf_callchain_size");
                }
                if (Perf_Callchain_Size > 0 && data.Payload.FieldsByName.ContainsKey("perf_callchain") && Perf_Callchain_Size < Int32.MaxValue)
                {
                    CallStack = new string[Perf_Callchain_Size + 1];
                    var perf_Callchain = data.Payload.ReadFieldAsArray("perf_callchain").ReadAsUInt64Array();

                    CallStack[0] = "[Root]";
                    for (ulong stackIdx = 1; stackIdx <= Perf_Callchain_Size; stackIdx++)
                    {
                        var sym = FindSymbolForIp(perf_Callchain[Perf_Callchain_Size - stackIdx], kernelSymbols);
                        CallStack[stackIdx] = sym != null ? sym.Name : null;
                    }
                }
            }
        }

        private KernelSymbol FindSymbolForIp(ulong ip, SortedList<ulong, KernelSymbol> kernelSymbols)
        {
            KernelSymbol kernelSymbol;
            if (cachedIpSymList.TryGetValue(ip, out kernelSymbol))
            {
                return kernelSymbol;
            }
            else
            {
                if (maxKernelSymbol == null)
                {
                    maxKernelSymbol = kernelSymbols.Values.LastOrDefault();
                }

                var sym = kernelSymbols.Where(f => (long)f.Key <= (long)ip).LastOrDefault(); // Redo implementation later for perf
                if (sym.Value != null)
                {
                    if (sym.Value != maxKernelSymbol)
                    {
                        cachedIpSymList.TryAdd(ip, sym.Value);
                    }
                    else
                    {
                        cachedIpSymList.TryAdd(ip, null);
                    }
                    return sym.Value;
                }
                return null;
            }
        }
    }
}
