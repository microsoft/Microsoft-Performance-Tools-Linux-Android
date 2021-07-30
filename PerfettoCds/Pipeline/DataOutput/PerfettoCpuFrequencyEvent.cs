// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using System.Collections.Generic;

namespace PerfettoCds.Pipeline.DataOutput
{
    /// <summary>
    /// A event that represents the frequency and state of a CPU at a point in time
    /// info.
    /// </summary>
    public readonly struct PerfettoCpuFrequencyEvent
    {
        // From Slice table
        public double CpuFrequency { get; }
        public long CpuNum { get; }
        public Timestamp StartTimestamp { get; }
        public string Name { get; }

        public PerfettoCpuFrequencyEvent(double cpuFrequency, long cpuNum, Timestamp startTimestamp, string name)
        {
            this.CpuFrequency = cpuFrequency;
            this.CpuNum = cpuNum;
            this.StartTimestamp = startTimestamp;
            this.Name = name;
        }
    }
}
