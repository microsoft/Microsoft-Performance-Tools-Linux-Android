// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using PerfettoProcessor;
using System.Collections.Generic;
using Utilities;

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

        // From Args table. The debug annotations for an event. Variable number per event
        public string[] Values { get; }
        public string[] ArgKeys { get; }

        // From Process table
        public string Process { get; }
        public string ProcessLabel { get; }

        // From Thread table
        public string Thread { get; }

        public string Provider { get; }

        public int? ParentId{ get; }

        public int ParentTreeDepthLevel { get; }

        public PerfettoThreadTrackEvent ThreadTrack { get; }

        public string[] ParentEventNameTree { get; }

        public PerfettoGenericEvent(string eventName, 
            string type, 
            TimestampDelta duration, 
            Timestamp startTimestamp, 
            Timestamp endTimestamp, 
            string category, 
            List<string> values,
            List<string> argKeys,
            string process,
            string processLabel,
            string thread,
            string provider,
            PerfettoThreadTrackEvent threadTrack,
            int? parentId,
            int parentTreeDepthLevel,
            string[] parentEventNameTree)
        {
            EventName = Common.StringIntern(eventName);
            Type = Common.StringIntern(type);
            Duration = duration;
            StartTimestamp = startTimestamp;
            EndTimestamp = endTimestamp;
            Category =  Common.StringIntern(category);
            Values = values.ToArray();
            ArgKeys = argKeys.ToArray();
            Process = Common.StringIntern(process);
            ProcessLabel = Common.StringIntern(processLabel);
            Thread = Common.StringIntern(thread);
            Provider = Common.StringIntern(provider);
            ThreadTrack = threadTrack;
            ParentId = parentId;
            ParentTreeDepthLevel = parentTreeDepthLevel;
            ParentEventNameTree = parentEventNameTree;
        }
    }
}
