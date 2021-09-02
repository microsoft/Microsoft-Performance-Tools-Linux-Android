// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using System.Collections.Generic;

namespace PerfettoCds.Pipeline.DataOutput
{
    /// <summary>
    /// A event that represents several memory values for a process at a point in time
    /// </summary>
    public readonly struct PerfettoProcessMemoryEvent
    {
        public string ProcessName { get; }
        public Timestamp StartTimestamp { get; }
        public TimestampDelta Duration { get; }

        /// Resident set size - anonymous memory
        public double RssAnon { get; }
        /// Resident set size - shared memory
        public double RssShMem { get; }
        /// Resident set size - file mappings
        public double RssFile { get; }
        /// Resident set size - Peak (high water mark)
        public double RssHwm { get; }
        /// Resident set size - Sum of anon, file, ShMem
        public double Rss { get; }
        /// Locked memory size
        public double Locked { get; }
        /// Swapped out VM size by anonymous private pages
        public double Swap { get; }
        /// Peak virtual memory size
        public double Virt { get; }

        public PerfettoProcessMemoryEvent(string processName, Timestamp startTimestamp, TimestampDelta duration,
            double rssAnon,
            double rssShMem,
            double rssFile,
            double rssHwm,
            double rss,
            double locked,
            double swap,
            double virt)
        {
            this.ProcessName = processName;
            this.StartTimestamp = startTimestamp;
            this.Duration = duration;

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
