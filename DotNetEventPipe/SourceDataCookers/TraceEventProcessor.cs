using DotNetEventPipe.DataOutputTypes;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.EventPipe;
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace DotNetEventPipe
{
    public class TraceEventProcessor
    {
        const uint MSEC_TO_NS = 1000000;
        public List<ThreadSamplingEvent> ThreadSamplingEvents = new List<ThreadSamplingEvent>();

        // TODO - Move this to a DataCooker

        public void ProcessTraceEvent(TraceEvent data)
        {
            string eventName = data.ProviderName + "/" + data.EventName;

            TraceCallStack stack = data.CallStack();

            switch (data.EventName)
            {
                case "Thread/Sample":
                    var clrTS = (ClrThreadSampleTraceData) data;

                    string[] callStack = null;
                    TraceModuleFile module = null;
                    string fullMethodName = null;
                    if (stack != null)
                    {
                        module = stack.CodeAddress.ModuleFile;
                        fullMethodName = stack.CodeAddress.FullMethodName;
                        callStack = new string[stack.Depth];

                        TraceCallStack current = stack;
                        while (current != null)
                        {
                            callStack[current.Depth-1] = Common.StringIntern($"{current.CodeAddress.ModuleName}!{current.CodeAddress.FullMethodName}");
                            current = current.Caller;
                        }
                    }

                    var threadSamplingEvent = new ThreadSamplingEvent(
                        clrTS.ProcessID,
                        clrTS.ProcessName,
                        clrTS.ProcessorNumber,
                        clrTS.ThreadID,
                        new Microsoft.Performance.SDK.Timestamp((long)(clrTS.TimeStampRelativeMSec * MSEC_TO_NS)),
                        callStack,
                        module,
                        fullMethodName
                        );
                    ThreadSamplingEvents.Add(threadSamplingEvent);
                    break;
                default:
                    // TODO all other events go to GenericEvents
                    break;
            }
        }
    }
}
