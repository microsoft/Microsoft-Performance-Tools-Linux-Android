// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Performance.SDK;
using LttngCds.CookerData;
using System.Text;
using System;
using LttngDataExtensions.SourceDataCookers.Thread;

namespace LttngDataExtensions.SourceDataCookers.Module
{
    public class ModuleEvent : IModuleEvent
    {
        string eventType;
        string instructionPointer;
        int tid;
        string threadId;
        string processId;
        string processCommand;
        int refCount;
        string moduleName;
        Timestamp time;

        private static string ParseEventType(string eventName)
        {
            var splitName = eventName.Split('_');
            if (splitName.Length >= 2)
            {
                if (splitName.Length == 2)
                {
                    return splitName[1];
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
            return String.Empty;
        }

        public ModuleEvent(LttngEvent data, LttngContext context, ExecutingThreadTracker threadTracker)
        {
            this.eventType = ParseEventType(data.Name);
            
            if (data.Payload.FieldsByName.ContainsKey("_ip"))
            {
                this.instructionPointer = string.Format("0x{0:X}", data.Payload.ReadFieldAsUInt64("_ip"));
            }
            else
            {
                this.instructionPointer = String.Empty;
            }
            if (data.StreamDefinedEventContext != null && data.StreamDefinedEventContext.FieldsByName.ContainsKey("_tid"))
            {
                this.tid = data.StreamDefinedEventContext.ReadFieldAsInt32("_tid");
            }
            else
            {
                this.tid = threadTracker.CurrentTidAsInt(context.CurrentCpu);
            }
            this.threadId = this.tid.ToString();
            if (data.Payload.FieldsByName.ContainsKey("_refcnt"))
            {
                this.refCount = data.Payload.ReadFieldAsInt32("_refcnt");
            }
            else
            {
                this.refCount = 0;
            }
            
            this.moduleName = data.Payload.FieldsByName["_name"].GetValueAsString();
            this.time = data.Timestamp;
        }

        public void SetThreadInformation(ThreadBasicInfo threadInfo)
        {
            this.processId = threadInfo.Pid;
            this.processCommand = threadInfo.Command;
        }

        public string EventType => this.eventType;
        public string InstructionPointer => this.instructionPointer;
        public int Tid => this.tid;
        public string ThreadId => this.threadId;
        public string ProcessId => this.processId;
        public string ProcessCommand => this.processCommand;
        public int RefCount => this.refCount;
        public string ModuleName => this.moduleName;
        public Timestamp Time => this.time;
    }
}
