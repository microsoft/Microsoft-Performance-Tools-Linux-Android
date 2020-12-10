// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using LttngCds.CookerData;

namespace LttngDataExtensions.SourceDataCookers.Thread
{
    public class ExecutingThreadTracker
    {
        static readonly string ContextSwitchEventName = "sched_switch";
        private List<int> executingThreadPerCpu = new List<int>();
        public static readonly HashSet<string> UsedDataKeys = new HashSet<string>() { ContextSwitchEventName };

        public void ProcessEvent(LttngEvent data, LttngContext context)
        {
            if (ContextSwitchEventName.Equals(data.Name))
            {
                while (this.executingThreadPerCpu.Count <= context.CurrentCpu)
                {
                    this.executingThreadPerCpu.Add(-1);
                }
                this.executingThreadPerCpu[(int)context.CurrentCpu] = data.Payload.ReadFieldAsInt32("_next_tid");
            }
        }
        public void ReportEventsDiscarded(uint cpu)
        {
            if (executingThreadPerCpu.Count > cpu)
            {
                executingThreadPerCpu[(int)cpu] = -1;
            }
        }

        public string CurrentTidAsString(uint cpu)
        {
            int executingTid = this.CurrentTidAsInt(cpu);
            if (executingTid >= 0)
            {
                return executingTid.ToString();
            }
            return "";
        }

        public int CurrentTidAsInt(uint cpu)
        {
            if (this.executingThreadPerCpu.Count > cpu)
            {
                return this.executingThreadPerCpu[(int)cpu];
            }
            return -1;
        }
    }
}
