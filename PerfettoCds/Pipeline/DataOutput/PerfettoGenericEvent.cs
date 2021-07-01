using Microsoft.Performance.SDK;
using System;
using System.Collections.Generic;
using System.Text;

namespace PerfettoCds.Pipeline.DataOutput
{
    /// <summary>
    /// A generic app/component event that contains event name, event metadata, and thread+process
    /// info
    /// </summary>
    public class PerfettoGenericEvent
    {
        // From Slice table
        public string EventName { get; set; }
        public string Type { get; set; }
        public long Duration { get; set; }
        public Timestamp Timestamp { get; set; }
        public string Category { get; set; }

        // Key betwee slice and args table
        public long ArgSetId { get; set; }

        // From Args table. The debug annotations for an event. Variable number per event
        public List<string> FlatKeys { get; set; }
        public List<string> Values{ get; set; }
        public List<string> ArgKeys{ get; set; }

        // From Process table
        public string Process { get; set; }

        // From Thread table
        public string Thread { get; set; }

        public PerfettoGenericEvent()
        {
            FlatKeys = new List<string>();
            ArgKeys = new List<string>();
            Values = new List<string>();
        }
    }
}
