// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using CtfPlayback;
using LTTngDataExtensions.SourceDataCookers.Thread;
using System.Threading;
using LTTngCds.CookerData;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Extensibility.DataCooking.SourceDataCooking;

namespace LTTngDataExtensions.SourceDataCookers.Syscall
{
    public class LTTngSyscallDataCooker
        : LTTngBaseSourceCooker
    {
        public const string Identifier = "SyscallDataCooker";

        public static string UknownSyscallExit = "compat_syscall_exit_unknown";

        private readonly Dictionary<string, List<SyscallEvent>> syscallEvents = new Dictionary<string, List<SyscallEvent>>();

        private readonly List<Timestamp> discardedEventsTimestamps = new List<Timestamp>();
        private DiscardedEventsTracker discardedEventsTracker = new DiscardedEventsTracker();
        private ExecutingThreadTracker threadTracker = new ExecutingThreadTracker();

        private ICookedDataRetrieval dataRetrieval;

        public LTTngSyscallDataCooker()
            : base(Identifier)
        {
        }

        private static readonly HashSet<DataCookerPath> RequiredPaths = new HashSet<DataCookerPath>
        {
            LTTngThreadDataCooker.DataCookerPath
        };

        public override IReadOnlyCollection<DataCookerPath> RequiredDataCookers => RequiredPaths;

        public override IReadOnlyDictionary<DataCookerPath, DataCookerDependencyType> DependencyTypes => dependencyTypes;

        private static readonly Dictionary<DataCookerPath, DataCookerDependencyType> dependencyTypes = new Dictionary<DataCookerPath, DataCookerDependencyType>
        {
            { LTTngThreadDataCooker.DataCookerPath, DataCookerDependencyType.AsConsumed }
        };

        public override string Description => "Processes LTTng events related to syscalls.";

        public override ReadOnlyHashSet<string> DataKeys => EmptyDataKeys;

        private readonly List<ISyscall> syscallEntries = new List<ISyscall>();

        [DataOutput]
        public IReadOnlyList<ISyscall> Syscalls => this.syscallEntries;

        /// <summary>
        /// This data cooker receives all data elements.
        /// </summary>
        public override SourceDataCookerOptions Options => SourceDataCookerOptions.ReceiveAllDataElements;

        public override DataProcessingResult CookDataElement(LTTngEvent data, LTTngContext context, CancellationToken cancellationToken)
        {
            try
            {
                if (this.discardedEventsTracker.EventsDiscardedBetweenLastTwoEvents(data, context) > 0)
                {
                    this.discardedEventsTimestamps.Add(data.Timestamp);
                    this.threadTracker.ReportEventsDiscarded(context.CurrentCpu);
                }
                this.threadTracker.ProcessEvent(data, context);
                if (data.Name.StartsWith("syscall") || LTTngSyscallDataCooker.UknownSyscallExit.Equals(data.Name))
                {
                    this.ProcessSyscall(new SyscallEvent(data, context, this.threadTracker));
                    return DataProcessingResult.Processed;
                }
                else
                {
                    return DataProcessingResult.Ignored;
                }
            }
            catch (CtfPlaybackException e)
            {
                Console.Error.WriteLine(e);
                return DataProcessingResult.CorruptData;
            }
        }

        public override void BeginDataCooking(ICookedDataRetrieval dependencyRetrieval, CancellationToken cancellationToken)
        {
            this.dataRetrieval = dependencyRetrieval;
        }

        public override void EndDataCooking(CancellationToken cancellationToken)
        {
            IThreadTracker pidTracker = this.dataRetrieval.QueryOutput<IThreadTracker>(
                new DataOutputPath(LTTngThreadDataCooker.DataCookerPath, "ThreadTracker"));

            Dictionary<int, List<SyscallEvent>> syscallsPerThread = new Dictionary<int, List<SyscallEvent>>();
            Dictionary<int, int> threadClosingEventsToSkip = new Dictionary<int, int>();

            foreach (var syscallType in syscallEvents)
            {
                threadClosingEventsToSkip.Clear();
                new List<List<SyscallEvent>>(syscallsPerThread.Values).ForEach(l => l.Clear());
                int t = 1;
                Timestamp nextDroppedEventTimestamp = Timestamp.MaxValue;
                if (discardedEventsTimestamps.Count > 0)
                {
                    nextDroppedEventTimestamp = discardedEventsTimestamps[0];
                }

                var receivedEntries = syscallType.Value;
                for (int i = 0; i < receivedEntries.Count; ++i)
                {
                    if (receivedEntries[i].Timestamp >= nextDroppedEventTimestamp)
                    {
                        new List<List<SyscallEvent>>(syscallsPerThread.Values).ForEach(l => l.Clear());
                        threadClosingEventsToSkip.Clear();
                        if (t < discardedEventsTimestamps.Count)
                        {
                            nextDroppedEventTimestamp = discardedEventsTimestamps[t++];
                        }
                        else
                        {
                            nextDroppedEventTimestamp = Timestamp.MaxValue;
                        }
                    }
                    if (!syscallsPerThread.TryGetValue(receivedEntries[i].Tid, out List<SyscallEvent> ongoingSyscalls))
                    {
                        ongoingSyscalls = new List<SyscallEvent>();
                        syscallsPerThread[receivedEntries[i].Tid] = ongoingSyscalls;
                    }
                    if (threadClosingEventsToSkip.TryGetValue(receivedEntries[i].Tid, out int amountOfEventsToSkip) && amountOfEventsToSkip > 0)
                    {
                        if (receivedEntries[i].IsEntry)
                        {
                            this.syscallEntries.Add(new Syscall(receivedEntries[i], null, pidTracker.QueryInfo(receivedEntries[i].Tid, receivedEntries[i].Timestamp)));
                            threadClosingEventsToSkip[receivedEntries[i].Tid] = amountOfEventsToSkip + 1;
                        }
                        else
                        {
                            threadClosingEventsToSkip[receivedEntries[i].Tid] = amountOfEventsToSkip - 1;
                        }
                    }
                    else if (receivedEntries[i].IsEntry)
                    {
                        if (receivedEntries[i].Name.StartsWith("exit"))
                        {
                            this.syscallEntries.Add(new Syscall(receivedEntries[i], null, pidTracker.QueryInfo(receivedEntries[i].Tid, receivedEntries[i].Timestamp)));
                        }
                        else
                        {
                            ongoingSyscalls.Add(receivedEntries[i]);
                        }
                    }
                    else if (ongoingSyscalls.Count == 1)
                    {
                        this.syscallEntries.Add(new Syscall(ongoingSyscalls[0], receivedEntries[i], pidTracker.QueryInfo(receivedEntries[i].Tid, receivedEntries[i].Timestamp)));
                        ongoingSyscalls.Clear();
                    }
                    else if (ongoingSyscalls.Count > 1)
                    {
                        threadClosingEventsToSkip[receivedEntries[i].Tid] = ongoingSyscalls.Count - 1;
                        ongoingSyscalls.ForEach(syscall => this.syscallEntries.Add(new Syscall(syscall, null, pidTracker.QueryInfo(syscall.Tid, syscall.Timestamp))));
                        ongoingSyscalls.Clear();
                    }
                }
                receivedEntries.Clear();
            }
            syscallEvents.Clear();
            discardedEventsTimestamps.Clear();
            this.syscallEntries.Sort(delegate (ISyscall a, ISyscall b)
            {
                return a.StartTime.ToNanoseconds.CompareTo(b.StartTime.ToNanoseconds);
            });
        }

        private void ProcessSyscall(SyscallEvent newEntry)
        {
            if (!syscallEvents.TryGetValue(newEntry.Name, out List<SyscallEvent> syscallList))
            {
                syscallList = new List<SyscallEvent>();
                syscallEvents[newEntry.Name] = syscallList;
            }
            syscallList.Add(newEntry);
        }
    }
}
