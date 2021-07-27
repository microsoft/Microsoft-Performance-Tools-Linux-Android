// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using PerfettoProcessor.Events;
using System.Collections.Generic;

namespace PerfettoCds.Pipeline.DataOutput
{
    /// <summary>
    /// A generic app/component event that contains event name, event metadata, and thread+process
    /// info.
    /// </summary>
    public readonly struct PerfettoCpuSchedEvent
    {
        // From Slice table
        public string ProcessName { get; }
        public string ThreadName { get; }
        public TimestampDelta Duration { get; }
        public Timestamp StartTimestamp { get; }
        public Timestamp EndTimestamp { get; }
        public long Cpu { get; }
        public string EndState { get; }
        public long Priority { get; }

        public PerfettoCpuSchedEvent(string processName,
            string threadName,
            TimestampDelta duration,
            Timestamp startTimestamp,
            Timestamp endTimestamp,
            long cpu,
            string endState,
            long priority)
        {
            this.ProcessName = processName;
            this.ThreadName = threadName;
            this.Duration = duration;
            this.StartTimestamp = startTimestamp;
            this.EndTimestamp = endTimestamp;
            this.Cpu = cpu;
            this.EndState = endState;
            this.Priority = priority;
        }
    }
}
