// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using System.Collections.Generic;
using Utilities;

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
            this.ProcessName = Common.StringIntern(processName);
            this.ThreadName = Common.StringIntern(threadName);
            this.Priority = Common.StringIntern(priority);
            this.Tag = Common.StringIntern(tag);
            this.Message = Common.StringIntern(message);
        }
    }
}
