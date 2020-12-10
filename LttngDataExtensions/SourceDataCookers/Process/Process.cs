// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace LttngDataExtensions.SourceDataCookers.Process
{
    public class Process 
        : IProcess
    {
        readonly ulong forkTime;
        readonly ulong execTime;
        readonly ulong exitTime;
        readonly ulong waitTime;
        readonly ulong freeTime;
        readonly int processId;
        readonly int parentProcessId;
        readonly string name;
        readonly string path;

        public Process(ulong forkTime, ulong execTime, ulong exitTime, ulong waitTime, ulong freeTime, int processId,
            int parentProcessId, string name, string path)
        {
            this.forkTime = forkTime;
            this.execTime = execTime;
            this.exitTime = exitTime;
            this.waitTime = waitTime;
            this.freeTime = freeTime;
            this.processId = processId;
            this.parentProcessId = parentProcessId;
            this.name = name;
            this.path = path;
        }

        public ulong ForkTime => this.forkTime;

        public ulong ExecTime => this.execTime;

        public ulong ExitTime => this.exitTime;

        public ulong WaitTime => this.waitTime;

        public ulong FreeTime => this.freeTime;

        public int ProcessId => this.processId;

        public int ParentProcessId => this.parentProcessId;

        public string Name => this.name;

        public string Path => this.path;
    }
}