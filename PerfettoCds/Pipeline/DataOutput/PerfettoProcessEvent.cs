// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using System.Collections.Generic;
using Utilities;

namespace PerfettoCds.Pipeline.DataOutput
{
    /// <summary>
    /// Contains information of processes seen during the trace (composite)
    /// </summary>
    public readonly struct PerfettoProcessEvent
    {
        public long Id { get; }
        public string Type { get; }
        public long Upid { get; }
        public long Pid { get; }
        public string Name { get; }

        public Timestamp StartTimestamp { get; }
        public Timestamp EndTimestamp { get; }

        public long? ParentUpid { get; }
        public long? Uid { get; }
        public long? AndroidAppId { get; }
        public string CmdLine { get; }

        // From Args table. The args for a process. Variable number per event
        public string[] ArgKeys { get; }
        public object[] Values { get; }

        public PerfettoProcessEvent(long id, string type, long upid, long pid, string name, Timestamp startTimestamp, Timestamp endTimestamp, long? parentUpid, long? uid, long? androidAppId, string cmdLine, string[] argKeys, object[] values)
        {
            Id = id;
            Type = type;
            Upid = upid;
            Pid = pid;
            Name = name;
            StartTimestamp = startTimestamp;
            EndTimestamp = endTimestamp;
            ParentUpid = parentUpid;
            Uid = uid;
            AndroidAppId = androidAppId;
            CmdLine = cmdLine;
            ArgKeys = argKeys;
            Values = values;
        }
    }
}
