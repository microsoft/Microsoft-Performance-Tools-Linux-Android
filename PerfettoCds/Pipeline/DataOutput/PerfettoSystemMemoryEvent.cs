// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using System.Collections.Generic;

namespace PerfettoCds.Pipeline.DataOutput
{
    /// <summary>
    /// A event that represents a single system memory value at a point in time
    /// </summary>
    public readonly struct PerfettoSystemMemoryEvent
    {
        public double Value { get; }
        public string MemoryType { get; }
        public Timestamp StartTimestamp { get; }
        public TimestampDelta Duration { get; }

        public PerfettoSystemMemoryEvent(double value, string memoryType, Timestamp startTimestamp, TimestampDelta duration)
        {
            this.Value = value;
            this.MemoryType = memoryType;
            this.StartTimestamp = startTimestamp;
            this.Duration = duration;
        }
    }
}
