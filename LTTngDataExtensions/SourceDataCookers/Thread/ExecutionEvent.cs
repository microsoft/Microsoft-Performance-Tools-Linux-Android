// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private Dictionary<string, long> performanceCountersDiffByName;

        public ExecutionEvent(ContextSwitch switchInContextSwitch, ContextSwitch switchOutContextSwitch, Timestamp switchOutTime)
        {
            this.cpu = switchInContextSwitch.Cpu;
            this.nextPid = switchInContextSwitch.NextPid;
            this.nextTid = switchInContextSwitch.NextTid;
            this.previousPid = switchInContextSwitch.PreviousPid;
            this.previousTid = switchInContextSwitch.PreviousTid;
            this.priority = switchInContextSwitch.Priority;
            this.readyingPid = switchInContextSwitch.ReadyingPid;
            this.readyingTid = switchInContextSwitch.ReadyingTid;
            this.previousState = switchInContextSwitch.PreviousState;
            this.nextCommand = switchInContextSwitch.NextImage;
            this.previousCommand = switchInContextSwitch.PreviousImage;
            this.readyTime = switchInContextSwitch.ReadyTime;
            this.waitTime = switchInContextSwitch.WaitTime;
            this.switchInTime = switchInContextSwitch.SwitchInTime;
            this.switchOutTime = switchOutTime;
            this.nextThreadPreviousSwitchOutTime = switchInContextSwitch.NextThreadPreviousSwitchOutTime;
            this.performanceCountersDiffByName = new Dictionary<string, long>();

            foreach(string performanceCounterName in switchInContextSwitch.PerformanceCountersByName.Keys)
            {
                if (switchOutContextSwitch!= default && switchOutContextSwitch.PerformanceCountersByName.ContainsKey(performanceCounterName))
                {
                    performanceCountersDiffByName[performanceCounterName] =
                        switchOutContextSwitch.PerformanceCountersByName[performanceCounterName] -
                        switchInContextSwitch.PerformanceCountersByName[performanceCounterName];
                }
            }
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
        public IReadOnlyDictionary<string, long> PerformanceCountersDiffByName => this.performanceCountersDiffByName;
    }
}
