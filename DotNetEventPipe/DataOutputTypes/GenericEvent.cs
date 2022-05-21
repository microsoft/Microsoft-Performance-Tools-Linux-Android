// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Performance.SDK;
using System;
using Utilities;

namespace DotNetEventPipe.DataOutputTypes
{
    /// <summary>
    /// A GenericEvent
    /// </summary>
    public class GenericEvent
    {
        public string EventName { get; }
        public TraceEventID ID { get; }
        public TraceEventKeyword Keywords { get; }
        public TraceEventLevel Level { get; }
        public TraceEventOpcode Opcode { get; }
        public string OpcodeName { get; }
        public string[] PayloadNames { get; }
        public object[] PayloadValues { get; }
        public Guid ProviderGuid { get; }
        public string ProviderName { get; }
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

        public GenericEvent(string eventName, TraceEventID id, TraceEventKeyword keywords, TraceEventLevel level, TraceEventOpcode opcode, string opcodeName, string[] payloadNames, object[] payloadValues, Guid providerGuid, string providerName, int processID, string processName, int processorNumber, int threadID, Timestamp timestamp, string[] callStack, TraceModuleFile module, string fullMethodName)
        {
            EventName = Common.StringIntern(eventName);
            ID = id;
            Keywords = keywords;
            Level = level;
            Opcode = opcode;
            OpcodeName = Common.StringIntern(opcodeName);
            PayloadNames = payloadNames;
            PayloadValues = payloadValues;
            ProviderGuid = providerGuid;
            ProviderName = providerName;
            ProcessID = processID;
            ProcessName = Common.StringIntern(processName);
            ProcessorNumber = processorNumber;
            ThreadID = threadID;
            Timestamp = timestamp;
            CallStack = callStack;
            Module = module;
            FullMethodName = Common.StringIntern(fullMethodName);
        }
    }
}
