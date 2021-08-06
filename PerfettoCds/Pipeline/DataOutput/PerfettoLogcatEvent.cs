// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using System.Collections.Generic;

namespace PerfettoCds.Pipeline.DataOutput
{
    /// <summary>
    /// A logcat message
    /// </summary>
    public readonly struct PerfettoLogcatEvent
    {
        public Timestamp StartTimestamp { get; }
        public string ProcessName { get; }
        public string ThreadName { get; }
        public string Priority { get; }
        public string Tag { get; }
        public string Message { get; }

        public PerfettoLogcatEvent(Timestamp startTimestamp,
            string processName,
            string threadName,
            string priority,
            string tag,
            string message)
        {
            this.StartTimestamp = startTimestamp;
            this.ProcessName = processName;
            this.ThreadName = threadName;
            this.Priority = priority;
            this.Tag = tag;
            this.Message = message;
        }
    }
}
