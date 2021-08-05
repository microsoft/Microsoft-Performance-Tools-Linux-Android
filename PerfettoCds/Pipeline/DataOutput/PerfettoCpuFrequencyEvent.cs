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
        public TimestampDelta Duration { get; }
        public bool IsIdle { get; }

        public PerfettoCpuFrequencyEvent(double cpuFrequency, long cpuNum, Timestamp startTimestamp, TimestampDelta duration, string name, bool isIdle)
        {
            this.CpuFrequency = cpuFrequency;
            this.CpuNum = cpuNum;
            this.StartTimestamp = startTimestamp;
            this.Duration = duration;
            this.Name = name;
            this.IsIdle = isIdle;
        }
    }
}
