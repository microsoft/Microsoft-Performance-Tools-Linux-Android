// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using Utilities;

namespace PerfettoCds.Pipeline.DataOutput
{
    /// <summary>
    /// A CPU scheduled event that displays which process and threads were running on which CPUs at specific times.
    /// </summary>
    public readonly struct PerfettoCpuSchedEvent
    {
        public string ProcessName { get; }
        public string ThreadName { get; }
        public TimestampDelta Duration { get; }
        public Timestamp StartTimestamp { get; }
        public Timestamp EndTimestamp { get; }
        public int Cpu { get; }
        public string EndState { get; }
        public int Priority { get; }

        public PerfettoCpuSchedEvent(string processName,
            string threadName,
            TimestampDelta duration,
            Timestamp startTimestamp,
            Timestamp endTimestamp,
            int cpu,
            string endState,
            int priority)
        {
            this.ProcessName = Common.StringIntern(processName);
            this.ThreadName = Common.StringIntern(threadName);
            this.Duration = duration;
            this.StartTimestamp = startTimestamp;
            this.EndTimestamp = endTimestamp;
            this.Cpu = cpu;
            this.EndState = endState;
            this.Priority = priority;
        }
    }
}
