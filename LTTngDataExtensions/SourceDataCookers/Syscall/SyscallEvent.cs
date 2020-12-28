// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using CtfPlayback.FieldValues;
using LTTngDataExtensions.DataOutputTypes;
using Microsoft.Performance.SDK;
using System.Collections.Generic;
using System.Text;
using System;
using LTTngCds.CookerData;
using LTTngDataExtensions.SourceDataCookers.Thread;

namespace LTTngDataExtensions.SourceDataCookers.Syscall
{
    public class SyscallEvent
        : ISyscallEvent
    {
        private string name;
        private Timestamp timestamp;
        private IReadOnlyDictionary<string, CtfFieldValue> fields;
        private int tid;
        private bool isEntry;

        struct SyscallNameInfo
        {
            public string name;
            public bool isEntry;

            public SyscallNameInfo(string name, bool isEntry)
            {
                this.name = name;
                this.isEntry = isEntry;
            }
        }

        private static readonly Dictionary<string, SyscallNameInfo> cachedNames = new Dictionary<string, SyscallNameInfo>();

        public static string EventNameToSyscallName(string eventName)
        {
            return EventNameToSyscallName(eventName, out _);
        }

        private static string EventNameToSyscallName(string eventName, out bool isEntry)
        {
            if (cachedNames.TryGetValue(eventName, out SyscallNameInfo cachedData))
            {
                isEntry = cachedData.isEntry;
                return cachedData.name;
            }
            string processedName = ProcessEventName(eventName, out isEntry);
            cachedNames.Add(eventName, new SyscallNameInfo(processedName, isEntry));
            return processedName;
        }

        private static string ProcessEventName(string eventName, out bool isEntry)
        {
            if (LTTngSyscallDataCooker.UknownSyscallExit.Equals(eventName))
            {
                isEntry = false;
                return "unknown";

            }
            var splitName = eventName.Split('_');
            if (splitName.Length > 2)
            {
                isEntry = "entry".Equals(splitName[1]);
                if (splitName.Length == 3)
                {
                    return splitName[2];
                }
                else
                {
                    StringBuilder nameBuilder = new StringBuilder(splitName[2]);
                    for (int i = 3; i < splitName.Length; ++i)
                    {
                        nameBuilder.Append('_');
                        nameBuilder.Append(splitName[i]);
                    }
                    return nameBuilder.ToString();
                }
            }
            isEntry = true;
            return String.Empty;
        }

        public SyscallEvent(LTTngEvent data, LTTngContext context, ExecutingThreadTracker threadTracker)
        {
            this.name = EventNameToSyscallName(data.Name, out this.isEntry);
            this.timestamp = data.Timestamp;
            this.fields = data.Payload.FieldsByName;
            if (data.StreamDefinedEventContext != null && data.StreamDefinedEventContext.FieldsByName.ContainsKey("_tid"))
            {
                this.tid = data.StreamDefinedEventContext.ReadFieldAsInt32("_tid");
            }
            else
            {
                this.tid = threadTracker.CurrentTidAsInt(context.CurrentCpu);
            }
        }

        public string Name => this.name;
        public int Tid => this.tid;
        public Timestamp Timestamp => this.timestamp;
        public bool IsEntry => this.isEntry;
        public IReadOnlyDictionary<string, CtfFieldValue> Fields => this.fields;
    }
}
