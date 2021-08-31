// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using System.Collections.Generic;

namespace PerfettoCds.Pipeline.DataOutput
{
    /// <summary>
    /// A event that represents the frequency and state of a CPU at a point in time
    /// </summary>
    public readonly struct PerfettoProcessMemoryEvent
    {
        public double Value { get; }
        public string ProcessName { get; }
        public Timestamp StartTimestamp { get; }
        public string MemoryType { get; }
        public TimestampDelta Duration { get; }

        public double RssAnon { get; }
        public double Locked { get; }
        public double RssShMem { get; }
        public double RssFile { get; }
        public double RssHwm { get; }
        public double Rss { get; }
        public double Swap { get; }
        public double Virt { get; }

        public PerfettoProcessMemoryEvent(double value, string processName, Timestamp startTimestamp, string memoryType, TimestampDelta duration,
            double rssAnon,
            double locked,
            double rssShMem,
            double rssFile,
            double rssHwm,
            double rss,
            double swap,
            double virt)
        {
            this.Value = value;
            this.ProcessName = processName;
            this.StartTimestamp = startTimestamp;
            this.Duration = duration;
            this.MemoryType = memoryType;

            this.RssAnon = rssAnon;
            this.Locked = locked;
            this.RssShMem = rssShMem;
            this.RssFile = rssFile;
            this.RssHwm = rssHwm;
            this.Rss = rss;
            this.Swap = swap;
            this.Virt = virt;
        }
    }
}
