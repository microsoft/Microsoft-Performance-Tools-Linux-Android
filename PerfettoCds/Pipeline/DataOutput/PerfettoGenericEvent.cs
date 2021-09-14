// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using PerfettoProcessor;
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

        // From Thread table
        public string Thread { get; }

        public string Provider { get; }

        public long? ParentId{ get; }

        public int ParentTreeDepthLevel { get; }

        public PerfettoThreadTrackEvent ThreadTrack { get; }

        public string[] ParentEventNameTree { get; }

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
            string provider,
            PerfettoThreadTrackEvent threadTrack,
            long? parentId,
            int parentTreeDepthLevel,
            string[] parentEventNameTree)
        {
            EventName = eventName;
            Type = type;
            Duration = duration;
            StartTimestamp = startTimestamp;
            EndTimestamp = endTimestamp;
            Category = category;
            ArgSetId = argSetId;
            Values = values;
            ArgKeys = argKeys;
            Process = process;
            Thread = thread;
            Provider = provider;
            ThreadTrack = threadTrack;
            ParentId = parentId;
            ParentTreeDepthLevel = parentTreeDepthLevel;
            ParentEventNameTree = parentEventNameTree;
        }
    }
}
