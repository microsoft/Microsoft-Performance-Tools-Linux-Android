// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Performance.SDK;

namespace LTTngDataExtensions.SourceDataCookers.Thread
{

    public class ExecutionEvent
        : IExecutionEvent
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
        private Timestamp switchOutTime;
        private Timestamp nextThreadPreviousSwitchOutTime;

        public ExecutionEvent(ContextSwitch contextSwitch, Timestamp switchOutTime)
        {
            this.cpu = contextSwitch.Cpu;
            this.nextPid = contextSwitch.NextPid;
            this.nextTid = contextSwitch.NextTid;
            this.previousPid = contextSwitch.PreviousPid;
            this.previousTid = contextSwitch.PreviousTid;
            this.priority = contextSwitch.Priority;
            this.readyingPid = contextSwitch.ReadyingPid;
            this.readyingTid = contextSwitch.ReadyingTid;
            this.previousState = contextSwitch.PreviousState;
            this.nextCommand = contextSwitch.NextImage;
            this.previousCommand = contextSwitch.PreviousImage;
            this.readyTime = contextSwitch.ReadyTime;
            this.waitTime = contextSwitch.WaitTime;
            this.switchInTime = contextSwitch.SwitchInTime;
            this.switchOutTime = switchOutTime;
            this.nextThreadPreviousSwitchOutTime = contextSwitch.NextThreadPreviousSwitchOutTime;
        }

        public void RecoverPids(Dictionary<int, int> recoveredPids)
        {
            if (this.nextPid.Contains(" "))
            {
                int reconstructedPid = this.reconstructPid(this.nextPid);
                if (recoveredPids.TryGetValue(reconstructedPid, out int recoveredNextPid))
                {
                    this.nextPid = recoveredNextPid.ToString();
                }
            }

            if (this.previousPid.Contains(" "))
            {
                int reconstructedPid = this.reconstructPid(this.nextPid);
                if (recoveredPids.TryGetValue(reconstructedPid, out int recoveredPrevPid))
                {
                    this.previousPid = recoveredPrevPid.ToString();
                }
            }   
        }

        private int reconstructPid(string pid)
        {
            return Int32.Parse(pid.Split(' ')[0]) * (-1);
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
        public Timestamp SwitchOutTime => this.switchOutTime;
        public Timestamp NextThreadPreviousSwitchOutTime => this.nextThreadPreviousSwitchOutTime;
    }
}
