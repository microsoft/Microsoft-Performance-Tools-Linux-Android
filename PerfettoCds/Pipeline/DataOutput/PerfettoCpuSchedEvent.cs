// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using System;
using Utilities;

namespace PerfettoCds.Pipeline.DataOutput
{
    /// <summary>
    /// A CPU scheduled event that displays which process and threads were running on which CPUs at specific times.
    /// </summary>
    public class PerfettoCpuSchedEvent
    {
        TimestampDelta? waitDuration;
        TimestampDelta? schedulingLatency;

        public string ProcessName { get; }
        public string ThreadName { get; }
        public long Tid { get; }
        public TimestampDelta Duration { get; }
        public Timestamp StartTimestamp { get; }
        public Timestamp EndTimestamp { get; }
        public uint Cpu { get; }
        public string EndState { get; }
        public int Priority { get; }
        public PerfettoCpuWakeEvent WakeEvent { get; private set; }
        public PerfettoCpuSchedEvent PreviousSchedulingEvent { get; private set; }

        public TimestampDelta WaitDuration
        {
            get
            {
                if (this.waitDuration == null)
                {
                    if (this.WakeEvent != null && this.PreviousSchedulingEvent != null)
                    {
                        this.waitDuration = this.WakeEvent.Timestamp - this.PreviousSchedulingEvent.EndTimestamp;
                    }
                    else
                    {
                        this.waitDuration = TimestampDelta.Zero;
                    }
                }

                return waitDuration.Value;
            }
        }

        public TimestampDelta SchedulingLatency
        {
            get
            {
                if (this.schedulingLatency == null)
                {
                    if (this.WakeEvent != null)
                    {
                        this.schedulingLatency = this.StartTimestamp - this.WakeEvent.Timestamp;
                    }
                    else if (this.PreviousSchedulingEvent?.EndState == "Runnable")
                    {
                        // Thread was already 'Runnable' and hence was ready to run. Scheduling latency because of non-availability of CPU.
                        this.schedulingLatency = this.StartTimestamp - this.PreviousSchedulingEvent.EndTimestamp;
                    }
                    else
                    {
                        this.schedulingLatency = TimestampDelta.Zero;
                    }
                }

                return schedulingLatency.Value;
            }
        }

        public PerfettoCpuSchedEvent(string processName,
            string threadName,
            long tid,
            TimestampDelta duration,
            Timestamp startTimestamp,
            Timestamp endTimestamp,
            uint cpu,
            string endState,
            int priority)
        {
            this.ProcessName = Common.StringIntern(processName);
            this.ThreadName = Common.StringIntern(threadName);
            this.Tid = tid;
            this.Duration = duration;
            this.StartTimestamp = startTimestamp;
            this.EndTimestamp = endTimestamp;
            this.Cpu = cpu;
            this.EndState = endState;
            this.Priority = priority;
        }

        public void AddPreviousCpuSchedulingEvent(PerfettoCpuSchedEvent previousCpuSchedEvent)
        {
            this.PreviousSchedulingEvent = previousCpuSchedEvent;
        }

        public void AddWakeEvent(PerfettoCpuWakeEvent wakeEvent)
        {
            this.WakeEvent = wakeEvent;
        }
    }
}
