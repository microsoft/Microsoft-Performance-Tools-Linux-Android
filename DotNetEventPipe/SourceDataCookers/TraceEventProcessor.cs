using DotnetEventpipe.DataOutputTypes;
using DotNetEventPipe.DataOutputTypes;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.EventPipe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utilities;

namespace DotNetEventPipe
{
    public class TraceEventProcessor
    {
        const uint MSEC_TO_NS = 1000000;
        public IReadOnlyList<ThreadSamplingEvent> ThreadSamplingEvents => threadSamplingEvents.AsReadOnly();
        List<ThreadSamplingEvent> threadSamplingEvents = new List<ThreadSamplingEvent>();

        public IReadOnlyList<GenericEvent> GenericEvents => genericEvents.AsReadOnly();
        List<GenericEvent> genericEvents = new List<GenericEvent>();

        public bool HasTraceData()
        {
            return threadSamplingEvents.Any() || genericEvents.Any();
        }

        // TODO - Move this to a DataCooker
        public void ProcessTraceEvent(TraceEvent data)
        {
            string eventName = data.ProviderName + "/" + data.EventName;

            TraceCallStack stack = data.CallStack();
            var stackProcessed = ProcessCallStack(stack);

            switch (data.EventName)
            {
                case "Thread/Sample":
                    var clrTS = (ClrThreadSampleTraceData) data;


                    var threadSamplingEvent = new ThreadSamplingEvent(
                        clrTS.ProcessID,
                        clrTS.ProcessName,
                        clrTS.ProcessorNumber,
                        clrTS.ThreadID,
                        new Microsoft.Performance.SDK.Timestamp((long)(clrTS.TimeStampRelativeMSec * MSEC_TO_NS)),
                        stackProcessed?.CallStack,
                        stackProcessed?.Module,
                        stackProcessed?.FullMethodName
                        );
                    threadSamplingEvents.Add(threadSamplingEvent);
                    break;
                default:
                    var payLoadValues = new object[data.PayloadNames.Length];
                    for (int i=0; i < data.PayloadNames.Length; i++)
                    {
                        payLoadValues[i] = data.PayloadValue(i);
                    }

                    var genericEvent = new GenericEvent(
                        data.EventName,
                        data.ID,
                        data.Keywords,
                        data.Level,
                        data.Opcode,
                        data.OpcodeName,
                        data.PayloadNames,
                        payLoadValues,
                        data.ProviderGuid,
                        data.ProviderName,
                        data.ProcessID,
                        data.ProcessName,
                        data.ProcessorNumber,
                        data.ThreadID,
                        new Microsoft.Performance.SDK.Timestamp((long)(data.TimeStampRelativeMSec * MSEC_TO_NS)),
                        stackProcessed?.CallStack,
                        stackProcessed?.Module,
                        stackProcessed?.FullMethodName
                        );
                    genericEvents.Add(genericEvent);
                    break;
            }
        }

        public TraceCallStackProcessed ProcessCallStack(TraceCallStack stack)
        {
            if (stack == null)
            {
                return null;
            }

            var traceCallStackProcessed = new TraceCallStackProcessed();
            traceCallStackProcessed.Module = stack.CodeAddress.ModuleFile;
            traceCallStackProcessed.FullMethodName = stack.CodeAddress.FullMethodName;
            traceCallStackProcessed.CallStack = new string[stack.Depth];

            TraceCallStack current = stack;
            while (current != null)
            {
                traceCallStackProcessed.CallStack[current.Depth - 1] = Common.StringIntern($"{current.CodeAddress.ModuleName}!{current.CodeAddress.FullMethodName}");
                current = current.Caller;
            }

            return traceCallStackProcessed;
        }
    }
}
