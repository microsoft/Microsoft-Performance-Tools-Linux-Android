// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using System.Collections.Generic;

namespace PerfettoCds.Pipeline.DataOutput
{
    /// <summary>
    /// A logcat message
    /// info.
    /// </summary>
    public readonly struct PerfettoFtraceEvent
    {
        public Timestamp StartTimestamp { get; }
        public string ProcessName { get; }
        public string ThreadName { get; }
        public long Cpu { get; }
        public string Name { get; }

        public PerfettoFtraceEvent(Timestamp startTimestamp,
            string processName,
            string threadName,
            long cpu,
            string name)
        {
            this.StartTimestamp = startTimestamp;
            this.ProcessName = processName;
            this.ThreadName = threadName;
            this.Cpu = cpu;
            this.Name = name;
        }
    }
}
