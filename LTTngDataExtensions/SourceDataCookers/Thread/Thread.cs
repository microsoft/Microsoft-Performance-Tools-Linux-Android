// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Performance.SDK;
using System.Collections.Generic;

namespace LTTngDataExtensions.SourceDataCookers.Thread
{
    class Thread
        : IThread
    {
        private int tid;
        private int pidAsInt;
        private string pidAsString;
        private string command;
        private Timestamp startTime;
        private Timestamp exitTime;
        private TimestampDelta execTime;
        private TimestampDelta readyTime;
        private TimestampDelta sleepTime;
        private TimestampDelta diskSleepTime;
        private TimestampDelta stoppedTime;
        private TimestampDelta parkedTime;
        private TimestampDelta idleTime;

        public Thread(ThreadInfo threadInfo)
        {
            this.tid = threadInfo.Tid;
            this.pidAsInt = threadInfo.Pid;
            this.pidAsString = threadInfo.PidAsString();
            this.command = threadInfo.Command;
            this.startTime = threadInfo.StartTime;
            this.exitTime = threadInfo.ExitTime;
            this.execTime = threadInfo.ExecTimeNs;
            this.readyTime = threadInfo.ReadyTimeNs;
            this.sleepTime = threadInfo.SleepTimeNs;
            this.diskSleepTime = threadInfo.DiskSleepTimeNs;
            this.stoppedTime = threadInfo.StoppedTimeNs;
            this.parkedTime = threadInfo.ParkedTimeNs;
            this.idleTime = threadInfo.IdleTimeNs;
        }

        public void RecoverPid(Dictionary<int, int> recoveredPids)
        {
            if (this.pidAsInt < 0 && recoveredPids.TryGetValue(this.pidAsInt, out int recoveredPid))
            {
                this.pidAsInt = recoveredPid;
                this.pidAsString = recoveredPid.ToString();
            }
        }

        public int ThreadId => this.tid;
        public string ProcessId => this.pidAsString;
        public string Command => this.command;
        public Timestamp StartTime => this.startTime;
        public Timestamp ExitTime => this.exitTime;
        public TimestampDelta ExecTime => this.execTime;
        public TimestampDelta ReadyTime => this.readyTime;
        public TimestampDelta SleepTime => this.sleepTime;
        public TimestampDelta DiskSleepTime => this.diskSleepTime;
        public TimestampDelta StoppedTime => this.stoppedTime;
        public TimestampDelta ParkedTime => this.parkedTime;
        public TimestampDelta IdleTime => this.idleTime;
    }
}
