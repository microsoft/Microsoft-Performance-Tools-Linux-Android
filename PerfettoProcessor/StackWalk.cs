using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Utilities;

namespace PerfettoProcessor
{
    public class StackWalkResult
    {
        public string[] Stack { get; set; } 
        public string Module { get; set; }
        public string Function { get; set; }
    }

    public class StackWalk
    {
        public StackWalk(ProcessedEventData<PerfettoStackProfileCallSiteEvent> stackProfileCallSiteEvents, 
                         ProcessedEventData<PerfettoStackProfileFrameEvent> stackProfileFrameEvents, 
                         ProcessedEventData<PerfettoStackProfileMappingEvent> stackProfileMappingEvents)
        {
            StackProfileCallSiteEvents = stackProfileCallSiteEvents;
            StackProfileFrameEvents = stackProfileFrameEvents;
            StackProfileMappingEvents = stackProfileMappingEvents;
        }

        public ProcessedEventData<PerfettoStackProfileCallSiteEvent> StackProfileCallSiteEvents { get; }
        public ProcessedEventData<PerfettoStackProfileFrameEvent> StackProfileFrameEvents { get; }
        public ProcessedEventData<PerfettoStackProfileMappingEvent> StackProfileMappingEvents { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stackProfileCallsiteId">StackProfileCallsiteTable::Id in stack_profile_callsite - callsite_id</param>
        /// <returns></returns>
        public StackWalkResult WalkStack(int stackProfileCallsiteId)
        {
            if (stackProfileCallsiteId < 0)
            {
                return null;
            }

            var stackWalkResult = new StackWalkResult();
            var stackFrames = new List<string>();

            var callSite = StackProfileCallSiteEvents[stackProfileCallsiteId];
            Debug.Assert(stackProfileCallsiteId == callSite.Id);
            while (callSite != null)
            {
                var stackProfileFrame = StackProfileFrameEvents[callSite.FrameId];
                Debug.Assert(callSite.FrameId == stackProfileFrame.Id);
                var stackProfileMapping = StackProfileMappingEvents[stackProfileFrame.Mapping];
                Debug.Assert(stackProfileFrame.Mapping == stackProfileMapping.Id);
                var bestFuncName = String.IsNullOrWhiteSpace(stackProfileFrame.DeobfuscatedName) ? stackProfileFrame.Name : stackProfileFrame.DeobfuscatedName;
                if (stackWalkResult.Module == null)
                {
                    stackWalkResult.Module = Common.StringIntern(stackProfileMapping.Name);
                }
                if (stackWalkResult.Function == null)
                {
                    stackWalkResult.Function = Common.StringIntern(bestFuncName);
                }

                stackFrames.Add(Common.StringIntern($"{stackProfileMapping.Name}!{bestFuncName}"));

                if (callSite.ParentId.HasValue)
                {
                    callSite = StackProfileCallSiteEvents[callSite.ParentId.Value];
                }
                else
                {
                    callSite = null;
                }
            }

            stackFrames.Reverse();
            stackWalkResult.Stack = stackFrames.ToArray();
            return stackWalkResult;
        }
    }
}
