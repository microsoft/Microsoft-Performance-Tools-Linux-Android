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
        public string ProcessFormattedName { get; }
        public string ThreadFormattedName { get; }
        public string ThreadName { get; }
        public uint Tid { get; }
        public uint Cpu { get; }
        // Name of the ftrace event
        public string Name { get; }

        // From Args table. Variable number per event
        public Dictionary<string, object> Args { get; }

        public PerfettoFtraceEvent(Timestamp startTimestamp,
            string processFormattedName,
            string threadFormattedName,
            string threadName,
            uint tid,
            uint cpu,
            string name,
            Dictionary<string, object> args)
        {
            this.StartTimestamp = startTimestamp;
            this.ProcessFormattedName = Common.StringIntern(processFormattedName);
            this.ThreadFormattedName = Common.StringIntern(threadFormattedName);
            this.ThreadName = Common.StringIntern(threadName);
            this.Tid = tid;
            this.Cpu = cpu;
            this.Name = Common.StringIntern(name);
            this.Args = args;
        }
    }
}
