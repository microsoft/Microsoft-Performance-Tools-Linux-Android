// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using System.Collections.Generic;

namespace PerfettoCds.Pipeline.DataOutput
{
    public readonly struct PerfettoCpuCountersEvent
    {
        // The specific CPU core
        public int CpuNum { get; }
        public Timestamp StartTimestamp { get; }
        public TimestampDelta Duration { get; }

        // All the absolute counters from /proc/stat
        public double UserNs { get; }
        public double UserNiceNs { get; }
        public double SystemModeNs { get; }
        public double IdleNs { get; }
        public double IoWaitNs { get; }
        public double IrqNs { get; }
        public double SoftIrqNs { get; }

        // The % change in counter values
        public double UserPercent { get; }
        public double UserNicePercent { get; }
        public double SystemModePercent { get; }
        public double IdlePercent { get; }
        public double IoWaitPercent { get; }
        public double IrqPercent { get; }
        public double SoftIrqPercent { get; }

        // All counters together except idle. Or (100 - IdlePercent)
        public double CpuPercent { get; }

        /// <summary>
        /// For populating the current counter values
        /// </summary>
        public PerfettoCpuCountersEvent(int cpuNum, Timestamp startTimestamp, TimestampDelta duration,
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

        /// <summary>
        /// When we have a previous event to compare to, we can calculate the percent change
        /// </summary>
        public PerfettoCpuCountersEvent(int cpuNum, Timestamp startTimestamp, TimestampDelta duration,
            double userNs,
            double userNiceNs,
            double systemModeNs,
            double idleNs,
            double ioWaitNs,
            double irqNs,
            double softIrqNs,
            PerfettoCpuCountersEvent previousEvent)
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
            if (timeDiff != 0)
            {
                this.UserPercent = (this.UserNs - previousEvent.UserNs) / timeDiff * 100;
                this.UserNicePercent = (this.UserNiceNs - previousEvent.UserNiceNs) / timeDiff * 100;
                this.SystemModePercent = (this.SystemModeNs - previousEvent.SystemModeNs) / timeDiff * 100;
                this.IdlePercent = (this.IdleNs - previousEvent.IdleNs) / timeDiff * 100;
                this.IoWaitPercent = (this.IoWaitNs - previousEvent.IoWaitNs) / timeDiff * 100;
                this.IrqPercent = (this.IrqNs - previousEvent.IrqNs) / timeDiff * 100;
                this.SoftIrqPercent = (this.SoftIrqNs - previousEvent.SoftIrqNs) / timeDiff * 100;
                this.CpuPercent = 100 - IdlePercent;
            }
            else
            {
                this.CpuPercent = 0;
                this.UserPercent = 0;
                this.UserNicePercent = 0;
                this.SystemModePercent = 0;
                this.IdlePercent = 0;
                this.IoWaitPercent = 0;
                this.IrqPercent = 0;
                this.SoftIrqPercent = 0;
            }
        }
    }
}
