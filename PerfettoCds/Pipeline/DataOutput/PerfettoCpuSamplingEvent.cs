// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using Utilities;

namespace PerfettoCds.Pipeline.DataOutput
{
    /// <summary>
    /// A CPU sampling event that samples at some interval
    /// Shows process and threads, and stacks that were running on which CPUs at specific times.
    /// </summary>
    public readonly struct PerfettoCpuSamplingEvent
    {
        public string ProcessName { get; }
        public string ThreadName { get; }
        public Timestamp Timestamp { get; }
        public uint Cpu { get; }
        public string CpuMode { get; }
        public string UnwindError { get; }
        public string[] CallStack { get; }
        /// <summary>
        /// filename of the binary / library for the instruction pointer
        /// </summary>
        public string Module { get; }
        /// <summary>
        /// Functionname of the instruction pointer
        /// </summary>
        public string Function { get; }

        public PerfettoCpuSamplingEvent(
            string processName,
            string threadName,
            Timestamp timestamp,
            uint cpu,
            string cpuMode,
            string unwindError,
            string[] callStack,
            string module,
            string function
            )
        {
            ProcessName = Common.StringIntern(processName);
            ThreadName = Common.StringIntern(threadName);
            Timestamp = timestamp;
            Cpu = cpu;
            CpuMode = cpuMode;
            UnwindError = unwindError;
            CallStack = callStack;
            Module = module;
            Function = function;
        }
    }
}
