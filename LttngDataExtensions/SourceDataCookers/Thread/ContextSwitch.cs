// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Performance.SDK;
using LttngCds.CookerData;

namespace LttngDataExtensions.SourceDataCookers.Thread
{
    public class ContextSwitch
    {
        private uint cpu;
        private int nextTid;
        private string nextPid;
        private string previousPid;
        private int previousTid;
        private int priority;
        private string readyingPid;
        private string readyingTid;
        private string previousState;
        private string nextCommand;
        private string previousCommand;
        private TimestampDelta readyTime;
        private TimestampDelta waitTime;
        private Timestamp switchInTime;
        private Timestamp nextThreadPreviousSwitchOutTime;

        public ContextSwitch(LttngEvent data, ThreadInfo nextThread, ThreadInfo previousThread, uint cpu)
        {
            this.cpu = cpu;
            this.nextPid = nextThread.PidAsString();
            this.nextTid = nextThread.Tid;
            this.previousPid = previousThread.PidAsString();
            this.previousTid = previousThread.Tid;
            this.priority = data.Payload.ReadFieldAsInt32("_next_prio");
            if (nextThread.currentState == ThreadInfo.SchedulingState.Running)
            {
                this.readyingPid = nextThread.readyingPid;
                this.readyingTid = nextThread.readyingTid;
                this.readyTime = data.Timestamp - nextThread.lastEventTimestamp;
                this.previousState = ThreadInfo.SchedulingStateToString(nextThread.previousState);
                if (nextThread.previousState == ThreadInfo.SchedulingState.Running ||
                    nextThread.previousState == ThreadInfo.SchedulingState.NewlyCreated ||
                    nextThread.previousState == ThreadInfo.SchedulingState.Unknown)
                {
                    this.waitTime = new TimestampDelta(0);
                }
                else
                {
                    this.waitTime = nextThread.previousWaitTime;
                }
            }
            else
            {
                this.readyingPid = string.Empty;
                this.readyingTid = string.Empty;
                this.readyTime = new TimestampDelta(0);
                this.waitTime = data.Timestamp - nextThread.lastEventTimestamp;
                this.previousState = ThreadInfo.SchedulingStateToString(nextThread.currentState);
            }
            this.nextCommand = data.Payload.ReadFieldAsArray("_next_comm").GetValueAsString();
            this.previousCommand = data.Payload.ReadFieldAsArray("_prev_comm").GetValueAsString();
            this.switchInTime = data.Timestamp;
            this.nextThreadPreviousSwitchOutTime = nextThread.previousSwitchOutTime;
        }

        public uint Cpu => this.cpu;
        public string NextPid => this.nextPid;
        public int NextTid => this.nextTid;
        public string PreviousPid => this.previousPid;
        public int PreviousTid => this.previousTid;
        public int Priority => this.priority;
        public string ReadyingPid => this.readyingPid;
        public string ReadyingTid => this.readyingTid;
        public string PreviousState => this.previousState;
        public string NextImage => this.nextCommand;
        public string PreviousImage => this.previousCommand;
        public TimestampDelta ReadyTime => this.readyTime;
        public TimestampDelta WaitTime => this.waitTime;
        public Timestamp SwitchInTime => this.switchInTime;
        public Timestamp NextThreadPreviousSwitchOutTime => this.nextThreadPreviousSwitchOutTime;
    }
}
