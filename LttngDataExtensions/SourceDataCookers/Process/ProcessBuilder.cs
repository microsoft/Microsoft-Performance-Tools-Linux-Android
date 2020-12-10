// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace LttngDataExtensions.SourceDataCookers.Process
{
    internal class ProcessBuilder : IBuilder<IProcess>
    {
        public ulong ForkTime { get; set; }

        public ulong ExecTime { get; set; }

        public ulong ExitTime { get; set; }

        public ulong WaitTime { get; set; }

        public ulong FreeTime { get; set; }

        public int ProcessId { get; set; }

        public int ParentProcessId { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public IProcess Build()
        {
            return new Process(this.ForkTime, this.ExecTime, this.ExitTime, this.WaitTime, this.FreeTime, this.ProcessId, this.ParentProcessId, this.Name,
                this.Path);
        }
    }
}