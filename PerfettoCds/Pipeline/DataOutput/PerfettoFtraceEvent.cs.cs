// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using System.Collections.Generic;
using Utilities;

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
        public int Cpu { get; }
        // Name of the ftrace event
        public string Name { get; }

        // From Args table. Variable number per event
        public string[] Values { get; }
        public string[] ArgKeys { get; }

        public PerfettoFtraceEvent(Timestamp startTimestamp,
            string processName,
            string threadName,
            int cpu,
            string name,
            List<string> values,
            List<string> argKeys)
        {
            this.StartTimestamp = startTimestamp;
            this.ProcessName = Common.StringIntern(processName);
            this.ThreadName = Common.StringIntern(threadName);
            this.Cpu = cpu;
            this.Name = Common.StringIntern(name);
            this.Values = values.ToArray();
            this.ArgKeys = argKeys.ToArray();
        }
    }
}
