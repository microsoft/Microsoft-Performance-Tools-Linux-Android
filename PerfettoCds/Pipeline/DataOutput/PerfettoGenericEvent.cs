// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using System.Collections.Generic;

namespace PerfettoCds.Pipeline.DataOutput
{
    /// <summary>
    /// A generic app/component event that contains event name, event metadata, and thread+process
    /// info.
    /// </summary>
    public readonly struct PerfettoGenericEvent
    {
        // From Slice table
        public string EventName { get; }
        public string Type { get; }
        public TimestampDelta Duration { get; }
        public Timestamp StartTimestamp { get; }
        public Timestamp EndTimestamp { get; }
        public string Category { get; }

        // Key between slice and args table
        public long ArgSetId { get; }

        // From Args table. The debug annotations for an event. Variable number per event
        public List<string> Values { get; }
        public List<string> ArgKeys { get; }

        // From Process table
        public string Process { get; }
        public string Process2 { get; }

        // From Thread table
        public string Thread { get; }

        public string Provider { get; }

        public PerfettoGenericEvent(string eventName, 
            string type, 
            TimestampDelta duration, 
            Timestamp startTimestamp, 
            Timestamp endTimestamp, 
            string category, 
            long argSetId, 
            List<string> values,
            List<string> argKeys,
            string process,
            string thread,
            string provider, string process2)
        {
            this.EventName = eventName;
            this.Type = type;
            this.Duration = duration;
            this.StartTimestamp = startTimestamp;
            this.EndTimestamp = endTimestamp;
            this.Category = category;
            this.ArgSetId = argSetId;
            this.Values = values;
            this.ArgKeys = argKeys;
            this.Process = process;
            this.Thread = thread;
            this.Provider = provider;
            this.Process2 = process2;
        }
    }
}
