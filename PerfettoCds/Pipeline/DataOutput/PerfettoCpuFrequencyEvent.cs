// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using System.Collections.Generic;

namespace PerfettoCds.Pipeline.DataOutput
{
    /// <summary>
    /// A event that represents the frequency and state of a CPU at a point in time
    /// </summary>
    public readonly struct PerfettoCpuFrequencyEvent
    {
        // The current frequency of this CPU
        public double CpuFrequency { get; }
        // The specific CPU core
        public long CpuNum { get; }
        public Timestamp StartTimestamp { get; }
        // Type of CPU frequency event. Whether it's an idle change or frequency change event
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

    public readonly struct PerfettoCpuUsageEvent
    {
        // The specific CPU core
        public long CpuNum { get; }
        public Timestamp StartTimestamp { get; }
        public TimestampDelta Duration { get; }

        public double UserNs { get; }
        public double UserNiceNs { get; }
        public double SystemModeNs { get; }
        public double IdleNs { get; }
        public double IoWaitNs { get; }
        public double IrqNs { get; }
        public double SoftIrqNs { get; }

        public double UserPercent { get; }
        public double UserNicePercent { get; }
        public double SystemModePercent { get; }
        public double IdlePercent { get; }
        public double IoWaitPercent { get; }
        public double IrqPercent { get; }
        public double SoftIrqPercent { get; }

        public double CpuPercent { get; }

        public PerfettoCpuUsageEvent(long cpuNum, Timestamp startTimestamp, TimestampDelta duration,
            double userNs,
            double userNiceNs,
            double systemModeNs,
            double idleNs,
            double ioWaitNs,
            double irqNs,
            double softIrqNs)
        {
            this.CpuNum = cpuNum;
            this.StartTimestamp = startTimestamp;
            this.Duration = duration;

            this.UserNs = userNs;
            this.UserNiceNs = userNiceNs;
            this.SystemModeNs = systemModeNs;
            this.IdleNs = idleNs;
            this.IoWaitNs = ioWaitNs;
            this.IrqNs = irqNs;
            this.SoftIrqNs = softIrqNs;

            this.CpuPercent = 0;
            this.UserPercent = 0;
            this.UserNicePercent = 0;
            this.SystemModePercent = 0;
            this.IdlePercent = 0;
            this.IoWaitPercent = 0;
            this.IrqPercent = 0;
            this.SoftIrqPercent = 0;
        }

        public PerfettoCpuUsageEvent(long cpuNum, Timestamp startTimestamp, TimestampDelta duration,
            double userNs,
            double userNiceNs,
            double systemModeNs,
            double idleNs,
            double ioWaitNs,
            double irqNs,
            double softIrqNs,
            PerfettoCpuUsageEvent previousEvent)
        {
            this.CpuNum = cpuNum;
            this.StartTimestamp = startTimestamp;
            this.Duration = duration;

            this.UserNs = userNs;
            this.UserNiceNs = userNiceNs;
            this.SystemModeNs = systemModeNs;
            this.IdleNs = idleNs;
            this.IoWaitNs = ioWaitNs;
            this.IrqNs = irqNs;
            this.SoftIrqNs = softIrqNs;

            var timeDiff = (this.StartTimestamp.ToNanoseconds - previousEvent.StartTimestamp.ToNanoseconds);

            this.UserPercent = (this.UserNs - previousEvent.UserNs) / timeDiff * 100;
            this.UserNicePercent = (this.UserNiceNs - previousEvent.UserNiceNs) / timeDiff * 100;
            this.SystemModePercent = (this.SystemModeNs - previousEvent.SystemModeNs) / timeDiff * 100;
            this.IdlePercent = (this.IdleNs - previousEvent.IdleNs) / timeDiff * 100;
            this.IoWaitPercent = (this.IoWaitNs - previousEvent.IoWaitNs) / timeDiff * 100;
            this.IrqPercent = (this.IrqNs - previousEvent.IrqNs) / timeDiff * 100;
            this.SoftIrqPercent = (this.SoftIrqNs - previousEvent.SoftIrqNs) / timeDiff * 100;
            this.CpuPercent = 100 - IdlePercent; // TODO don't know that idlepercent is below 100. Clamp?
        }
    }
}
