using Microsoft.Performance.SDK.Extensibility;
using PerfettoProcessor;
using System;
using System.Collections.Generic;
using System.Text;

namespace PerfettoCds.Pipeline.Events
{
    public class MyNewEvent : IKeyedDataType<string>
    {
        public readonly string Key;
        // Store this in each cookers ProcessedEventData
        public PerfettoSqlEvent SqlEvent;

        public MyNewEvent(string key, PerfettoSqlEvent sqlEvent)
        {
            this.Key = key;
            this.SqlEvent = sqlEvent;
        }

        public string GetKey()
        {
            return Key;
        }

        public int CompareTo(string other)
        {
            return this.Key.CompareTo(other);
        }
    }
}
