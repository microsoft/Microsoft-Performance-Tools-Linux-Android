// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Performance.SDK;
using Utilities;

namespace DotNetEventPipe.DataOutputTypes
{
    /// <summary>
    /// A CPU sampling event that samples at some interval
    /// Shows process and threads, and stacks that were running on which CPUs at specific times.
    /// </summary>
    public readonly struct ThreadSamplingEvent
    {
        public int ProcessID { get; }
        public string ProcessName { get; }
        public int ProcessorNumber { get; }
        public int ThreadID { get; }
        public Timestamp Timestamp { get; }
        public string[] CallStack { get; }
        /// <summary>
        /// filename of the binary / library for the instruction pointer
        /// </summary>
        public TraceModuleFile Module { get; }
        /// <summary>
        /// Functionname of the instruction pointer
        /// </summary>
        public string FullMethodName { get; }

        public ThreadSamplingEvent(
            int processID,
            string processName,
            int processorNumber,
            int threadID,
            Timestamp timestamp,
            string[] callStack,
            TraceModuleFile module,
            string fullMethodName
            )
        {
            ProcessID = processID;
            ProcessName = Common.StringIntern(processName);
            ProcessorNumber = processorNumber;
            ThreadID = threadID;
            Timestamp = timestamp;
            CallStack = callStack; // Cache whole common stack??
            Module = module;
            FullMethodName = Common.StringIntern(fullMethodName);
        }
    }
}
