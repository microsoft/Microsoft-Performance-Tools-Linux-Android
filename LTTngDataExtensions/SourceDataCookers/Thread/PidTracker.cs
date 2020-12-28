// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.Performance.SDK;

namespace LTTngDataExtensions.SourceDataCookers.Thread
{
    public interface IThreadTracker
    {
        ThreadBasicInfo QueryInfo(int tid, Timestamp timestamp);
    }

    public class ThreadBasicInfo
    {
        public readonly string Pid;
        public readonly string Command;

        public ThreadBasicInfo(string command, string pid)
        {
            this.Command = command;
            this.Pid = pid;
        }
    }

    public class ThreadTracker
        : IThreadTracker
    {
        private class InfoUsage
        {
            public readonly ThreadBasicInfo Info;
            public readonly Timestamp StartTime;

            public InfoUsage(Timestamp startTime, string command, string pid)
            {
                this.StartTime = startTime;
                this.Info = new ThreadBasicInfo(command, pid);
            }
        }
        public ThreadTracker() { }

        private readonly Dictionary<int, List<InfoUsage>> timeline = new Dictionary<int, List<InfoUsage>>();

        /// <summary>
        /// Pid guessing only happens for threads executing before the tracing starts, 
        /// and the we use the thread's tid as guess.
        /// Therefore only one translation per wrong pid may exist
        /// </summary>

        public void ProcessThread(IThread Thread)
        {
            List<InfoUsage> assignedPidsList;
            if (!timeline.TryGetValue(Thread.ThreadId, out assignedPidsList))
            {
                assignedPidsList = new List<InfoUsage>(1);
                timeline[Thread.ThreadId] = assignedPidsList;
            }
            assignedPidsList.Add(new InfoUsage(Thread.StartTime, Thread.Command, Thread.ProcessId));
        }

        public ThreadBasicInfo QueryInfo(int tid, Timestamp timestamp)
        {
            if (this.timeline.TryGetValue(tid, out List<InfoUsage> infoUsageList))
            {
                int floor = 0;
                int ceiling = infoUsageList.Count;
                int middle;

                while (floor + 1 < ceiling)
                {
                    middle = (ceiling + floor) / 2;
                    if (infoUsageList[middle].StartTime <= timestamp)
                    {
                        floor = middle;
                    }
                    else
                    {
                        ceiling = middle;
                    }
                }
                return infoUsageList[floor].Info;
            }
            return new ThreadBasicInfo("", "");
        }
    }
}
