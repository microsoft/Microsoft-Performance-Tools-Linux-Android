// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using System.Collections.Generic;
using Utilities;

namespace PerfettoCds.Pipeline.DataOutput
{
    public readonly struct PerfettoGpuCountersEvent
    {
        /// <summary>
        /// Name (type) of GPU counter
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Actual value of this counter at this point in time
        /// </summary>
        public double Value { get; }

        public Timestamp StartTimestamp { get; }
        public TimestampDelta Duration { get; }

        /// <summary>
        /// For populating the current counter values
        /// </summary>
        public PerfettoGpuCountersEvent(string name, double value, Timestamp startTimestamp, TimestampDelta duration)
        {
            this.Name = Common.StringIntern(name);
            this.Value = value;
            this.StartTimestamp = startTimestamp;
            this.Duration = duration;
        }
    }
}
