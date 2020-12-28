// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using System.Collections.Generic;
using LTTngDataExtensions.SourceDataCookers.Thread;
using Microsoft.Performance.SDK;

namespace LTTngDataExtensions.SourceDataCookers.Syscall
{
    class Syscall
        : ISyscall
    {
        readonly string name;
        readonly string returnValue;
        readonly string tid;
        readonly string pid;
        readonly string command;
        readonly string arguments;
        readonly Timestamp startTime;
        readonly Timestamp endTime;

        public Syscall(SyscallEvent entryLogLine, SyscallEvent exitLogLine, ThreadBasicInfo threadInfo)
        {
            if ("unknown".Equals(entryLogLine.Name))
            {
                this.name = String.Empty;
            }
            else
            {
                this.name = entryLogLine.Name;
            }
            this.startTime = entryLogLine.Timestamp;

            StringBuilder argumentsText = new StringBuilder("{");
            foreach (var entry in entryLogLine.Fields)
            {
                argumentsText.Append(entry.Key.TrimStart('_'));
                argumentsText.Append(": ");
                argumentsText.Append(entry.Value.GetValueAsString());
                argumentsText.Append(", ");
            }
            if (entryLogLine.Fields.Count > 0)
            {
                argumentsText.Remove(argumentsText.Length - 2, 2);
            }
            argumentsText.Append("}");
            this.arguments = argumentsText.ToString();
            if (entryLogLine.Tid >= 0)
            {
                this.tid = entryLogLine.Tid.ToString();
            }
            else
            {
                this.tid = String.Empty;
            }
            this.pid = threadInfo.Pid;
            this.command = threadInfo.Command;
            if (exitLogLine != null)
            {
                this.endTime = exitLogLine.Timestamp;
                this.returnValue = exitLogLine.Fields["_ret"].GetValueAsString();
            }
            else
            {
                this.endTime = entryLogLine.Timestamp;
                this.returnValue = String.Empty;
            }
        }

        public string Name => this.name;
        public string ThreadId => this.tid;
        public string ProcessId => this.pid;
        public string ProcessCommand => this.command;
        public string ReturnValue => this.returnValue;
        public string Arguments => this.arguments;
        public Timestamp StartTime => this.startTime;
        public Timestamp EndTime => this.endTime;
    }
}
