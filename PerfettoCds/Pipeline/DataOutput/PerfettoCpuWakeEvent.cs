// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using Utilities;

namespace PerfettoCds.Pipeline.DataOutput
{
    /// <summary>
    /// A CPU wake event that displays which process and threads were woken on which CPUs at specific times.
    /// </summary>
    public readonly struct PerfettoCpuWakeEvent
    {
        public string WokenProcessName { get; }
        public long? WokenPid { get; }
        public string WokenThreadName { get; }
        public long WokenTid { get; }
        
        public string WakerProcessName { get; }
        public long? WakerPid { get; }
        public string WakerThreadName { get; }
        public long WakerTid { get; }
       
        public Timestamp Timestamp { get; }
        public int Success { get; }
        public int Cpu { get; }
        public int TargetCpu { get; }
        public int Priority { get; }

        public PerfettoCpuWakeEvent(string wokenProcessName,
            long? wokenPid,
            string wokenThreadName,
            long wokenTid,
            string wakerProcessName,
            long? wakerPid,
            string wakerThreadName,
            long wakerTid,
            Timestamp timestamp,
            int success,
            int cpu,
            int targetCpu,
            int priority)
        {
            this.WokenProcessName = Common.StringIntern(wokenProcessName);
            this.WokenPid = wokenPid;
            this.WokenThreadName = Common.StringIntern(wokenThreadName);
            this.WokenTid = wokenTid;

            this.WakerProcessName = Common.StringIntern(wakerProcessName);
            this.WakerPid = wakerPid;
            this.WakerThreadName = Common.StringIntern(wakerThreadName);
            this.WakerTid = wakerTid;

            this.Timestamp = timestamp;
            this.Success = success;
            this.Cpu = cpu;
            this.TargetCpu = targetCpu;
            this.Priority = priority;
        }
    }
}
