using Microsoft.Performance.SDK;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetEventPipe.Tables
{
    internal class EventStat
    {
        public string ProviderName { get; set; }
        public string EventName { get; set; }
        public int Count { get; set; }
        public int StackCount { get; set; }
        public Timestamp StartTime { get; set; }
        public Timestamp EndTime { get; set; }
    }
}
