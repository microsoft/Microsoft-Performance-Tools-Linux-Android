// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using System.Collections.Generic;

namespace PerfettoCds.Pipeline.DataOutput
{
    /// <summary>
    /// An Ftrace event with extra args
    /// </summary>
    public readonly struct PerfettoFtraceEvent
    {
        public Timestamp StartTimestamp { get; }
        public string ProcessName { get; }
        public string ThreadName { get; }
        public long Cpu { get; }
        // Name of the ftrace event
        public string Name { get; }

        // From Args table. Variable number per event
        public List<string> Values { get; }
        public List<string> ArgKeys { get; }

        public PerfettoFtraceEvent(Timestamp startTimestamp,
            string processName,
            string threadName,
            long cpu,
            string name,
            List<string> values,
            List<string> argKeys)
        {
            this.StartTimestamp = startTimestamp;
            this.ProcessName = processName;
            this.ThreadName = threadName;
            this.Cpu = cpu;
            this.Name = name;
            this.Values = values;
            this.ArgKeys = argKeys;
        }
    }
}
