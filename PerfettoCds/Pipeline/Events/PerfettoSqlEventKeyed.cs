using Microsoft.Performance.SDK.Extensibility;
using PerfettoProcessor;
using System;
using System.Collections.Generic;
using System.Text;

namespace PerfettoCds.Pipeline.Events
{
    /// <summary>
    /// This class serves as a holder for PerfettoSqlEvents as they pass through cookers. It stores
    /// the PerfettoSqlEvent and the key that identifies its type and which cookers process it
    /// </summary>
    public class PerfettoSqlEventKeyed : IKeyedDataType<string>
    {
        public readonly string Key;

        // The SQL event being passed on
        public PerfettoSqlEvent SqlEvent;

        public PerfettoSqlEventKeyed(string key, PerfettoSqlEvent sqlEvent)
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
