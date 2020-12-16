// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using LttngCds.CookerData;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Extensibility.DataCooking.SourceDataCooking;
using CtfPlayback;
using CtfPlayback.Metadata;
using CtfPlayback.FieldValues;

namespace LttngDataExtensions.SourceDataCookers.Thread
{
    public class ThreadInfo
    {
        /// <summary>
        /// States defined exactly as in the Linux Kernel
        /// </summary>
        public enum ThreadState
        {
            TASK_RUNNING = 0x0000,
            TASK_INTERRUPTIBLE = 0x0001,
            TASK_UNINTERRUPTIBLE = 0x0002,
            __TASK_STOPPED = 0x0004,
            __TASK_TRACED = 0x0008,
            EXIT_DEAD = 0x0010,
            EXIT_ZOMBIE = 0x0020,
            EXIT_TRACE = (EXIT_ZOMBIE | EXIT_DEAD),
            TASK_PARKED = 0x0040,
            TASK_DEAD = 0x0080,
            TASK_WAKEKILL = 0x0100,
            TASK_WAKING = 0x0200,
            TASK_NOLOAD = 0x0400,
            TASK_NEW = 0x0800,
            TASK_STATE_MAX = 0x1000,
            TASK_KILLABLE = (TASK_WAKEKILL | TASK_UNINTERRUPTIBLE),
            TASK_STOPPED = (TASK_WAKEKILL | __TASK_STOPPED),
            TASK_TRACED = (TASK_WAKEKILL | __TASK_TRACED),
            TASK_IDLE = (TASK_UNINTERRUPTIBLE | TASK_NOLOAD)
        }
        private static int lastMostSignificantBitSet(uint x)
        {            
            if (x == 0)
            {
                return 0;
            }
            int r = 32;
            if ((x & 0xffff0000u) == 0)
            {
                x <<= 16;
                r -= 16;
            }
            if ((x & 0xff000000u) == 0)
            {
                x <<= 8;
                r -= 8;
            }
            if ((x & 0xf0000000u) == 0)
            {
                x <<= 4;
                r -= 4;
            }
            if ((x & 0xc0000000u) == 0)
            {
                x <<= 2;
                r -= 2;
            }
            if ((x & 0x80000000u) == 0)
            {
                ///x <<= 1;
                r -= 1;
            }
            return r;
        }

        private const uint TASK_REPORT = (uint)(
                ThreadState.TASK_RUNNING | ThreadState.TASK_INTERRUPTIBLE |
                ThreadState.TASK_UNINTERRUPTIBLE | ThreadState.__TASK_STOPPED |
                ThreadState.__TASK_TRACED | ThreadState.EXIT_DEAD |
                ThreadState.EXIT_ZOMBIE | ThreadState.TASK_PARKED);

        private const uint TASK_REPORT_IDLE = TASK_REPORT + 1;

        private static SchedulingState ThreadStateToSchedulingState(ThreadState threadState)
        {
            if (threadState == ThreadState.TASK_DEAD)
            {
                return SchedulingState.Dead;
            }
            uint state = (uint)threadState & ThreadInfo.TASK_REPORT;
            if (threadState == ThreadState.TASK_IDLE)
                state = ThreadInfo.TASK_REPORT_IDLE;
            return (SchedulingState)lastMostSignificantBitSet(state);
        }

        public enum SchedulingState
        {
            Running = 0,
            Sleeping = 1,
            DiskSleeping = 2,
            Stopped = 3,
            TracingStop = 4,
            Dead = 5,
            Zombie = 6,
            Parked = 7,
            Idle = 8,
            NewlyCreated = 9,
            Unknown = 10
        }

        public static string[] StringSchedulingState =
        {
            "R (running)",
            "S (sleeping)",
            "D (disk sleep)",
            "T (stopped)",
            "t (tracing stop)",
            "X (dead)",
            "Z (zombie)",
            "P (parked)",
            "I (idle)",
            ///The following two are not in the linux kernel, but were added for clarification
            "N (newly created)",
            ""
        };

        public static string SchedulingStateToString(SchedulingState state)
        {
            return ThreadInfo.StringSchedulingState[(uint)state];
        }

        public static string PidToString(int pid)
        {
            if (pid >= 0)
            {
                return pid.ToString();
            }
            else
            {
                return (pid * (-1)).ToString() + " [Probably]";
            }
        }

        public string PidAsString()
        {
            return PidToString(this.Pid);
        }

        public int Tid;
        public int Pid;
        public string Command;
        public Timestamp StartTime;
        public Timestamp ExitTime;
        public TimestampDelta ExecTimeNs = TimestampDelta.Zero;
        public TimestampDelta ReadyTimeNs = TimestampDelta.Zero;
        public TimestampDelta SleepTimeNs = TimestampDelta.Zero;
        public TimestampDelta DiskSleepTimeNs = TimestampDelta.Zero;
        public TimestampDelta StoppedTimeNs = TimestampDelta.Zero;
        public TimestampDelta ParkedTimeNs = TimestampDelta.Zero;
        public TimestampDelta IdleTimeNs = TimestampDelta.Zero;

        public Timestamp lastEventTimestamp;
        public Timestamp previousSwitchOutTime = Timestamp.Zero;
        public SchedulingState currentState;
        public SchedulingState previousState;

        public string readyingPid;
        public string readyingTid;
        
        public TimestampDelta previousWaitTime = TimestampDelta.Zero;

        public ThreadInfo(int tid, Timestamp firstEventTime, ThreadState startingState, ThreadInfo parentThread = null, bool inheritPid = false)
        {
            this.Tid = tid;
            if (startingState == ThreadState.TASK_NEW)
            {
                this.previousState = SchedulingState.NewlyCreated;
                if (inheritPid && parentThread != null)
                {
                    this.Pid = parentThread.Pid;
                }
                else
                {
                    this.Pid = tid;
                }
                
                if (parentThread != null)
                {
                    if (inheritPid)
                    {
                        this.Pid = parentThread.Pid;
                    }
                    else
                    {
                        this.Pid = tid;
                    }
                    this.Command = parentThread.Command;
                }
                else
                {
                    this.Command = String.Empty;
                    this.Pid = tid;
                }
                this.StartTime = firstEventTime;
            }
            else
            {
                this.Pid = tid * (-1);
                this.previousState = SchedulingState.Unknown;
                this.Command = String.Empty;
                this.StartTime = Timestamp.Zero;
            }
            
            
            this.lastEventTimestamp = firstEventTime;
            this.currentState = ThreadStateToSchedulingState(startingState);

            if (startingState == ThreadState.TASK_WAKING && parentThread != null)
            {
                this.readyingPid = parentThread.PidAsString();
                this.readyingTid = parentThread.Tid.ToString();
            }
            else
            {
                this.readyingPid = String.Empty;
                this.readyingTid = String.Empty;
            }

            
        }
        private void stateTransition(Timestamp newEventTimestamp, ThreadState newState)
        {
            TimestampDelta nanosecondsToAdd = newEventTimestamp - this.lastEventTimestamp;
            this.lastEventTimestamp = newEventTimestamp;
            var prevState = this.currentState;
            this.currentState = ThreadStateToSchedulingState(newState);
            switch (prevState)
            {
                case SchedulingState.Running:
                case SchedulingState.NewlyCreated:
                    ///Adding to ready time because exec time is added after a context switch
                    this.ReadyTimeNs += nanosecondsToAdd;
                    return;
                case SchedulingState.Sleeping:
                    this.SleepTimeNs += nanosecondsToAdd;
                    break;
                case SchedulingState.DiskSleeping:
                    this.DiskSleepTimeNs += nanosecondsToAdd;
                    break;
                case SchedulingState.Stopped:
                    this.StoppedTimeNs += nanosecondsToAdd;
                    break;
                case SchedulingState.Parked:
                    this.ParkedTimeNs += nanosecondsToAdd;
                    break;
                case SchedulingState.Idle:
                    this.IdleTimeNs += nanosecondsToAdd;
                    break;
            }
            ///Only set if current state is not running
            this.previousState = prevState;
            this.previousWaitTime = nanosecondsToAdd;
        }

        public bool isTerminated()
        {
            return this.currentState == SchedulingState.Dead || this.currentState == SchedulingState.Zombie;
        }

        public void SwitchIn(Timestamp timestamp)
        {
            this.stateTransition(timestamp, ThreadState.TASK_RUNNING);
        }

        public void SwitchOut(LttngEvent data, ThreadState newState)
        {
            this.ExecTimeNs += data.Timestamp - this.lastEventTimestamp;
            this.currentState = ThreadStateToSchedulingState(newState);
            this.previousState = SchedulingState.Running;
            this.lastEventTimestamp = data.Timestamp;
            this.previousSwitchOutTime = data.Timestamp;
            this.readyingPid = String.Empty;
            this.readyingTid = String.Empty;
            if (this.isTerminated())
            {
                this.ExitTime = data.Timestamp;
            }
        }

        public void Waking(Timestamp timestamp, ThreadInfo readyingThread)
        {
            this.stateTransition(timestamp, ThreadState.TASK_WAKING);
            if (readyingThread != null)
            {
                this.readyingPid = readyingThread.PidAsString();
                this.readyingTid = readyingThread.Tid.ToString();
            }
        }

        public void Wakeup(Timestamp timestamp)
        {
            this.stateTransition(timestamp, ThreadState.TASK_RUNNING);
        }

        public void Terminate(Timestamp timestamp)
        {
            this.stateTransition(timestamp, ThreadState.TASK_DEAD);
            this.ExitTime = timestamp;
        }
    }

    public class LttngThreadDataCooker
        : LttngBaseSourceCooker
    {
        private static class StatedumpDataKeys
        {
            public const string ProcessState = "lttng_statedump_process_state";
        }

        private static class SchedulerDataKeys
        {
            public const string ProcessExit = "sched_process_exit";
            public const string ProcessExec = "sched_process_exec";
            public const string ProcessFork = "sched_process_fork";
            public const string Switch = "sched_switch";
            public const string Wakeup = "sched_wakeup";
            public const string Waking = "sched_waking";
            public const string NewWakeup = "sched_wakeup_new";
        }

        private static class ThreadCreationDataKeys
        {
            public const string CloneExit = "syscall_exit_clone";
            public const string CloneEntry = "syscall_entry_clone";
            public const string ForkExit = "syscall_exit_fork";
            public const string ForkEntry = "syscall_entry_fork";
            public const string VForkExit = "syscall_exit_vfork";
            public const string VForkEntry = "syscall_entry_vfork";
            public const string ExecveExit = "syscall_exit_execve";
            public const string ExecveEntry = "syscall_entry_execve";
            public const string ExecveAtExit = "syscall_exit_execveat";
            public const string ExecveAtEntry = "syscall_entry_execveat";
        }

        private const string GetPidExit = "syscall_exit_getpid";

        private static readonly HashSet<string> Keys = new HashSet<string>(new[]
        {
            SchedulerDataKeys.ProcessExit,
            SchedulerDataKeys.ProcessFork,
            SchedulerDataKeys.ProcessExec,
            SchedulerDataKeys.Switch,
            SchedulerDataKeys.Wakeup,
            SchedulerDataKeys.Waking,
            SchedulerDataKeys.NewWakeup,
            ThreadCreationDataKeys.CloneExit,
            ThreadCreationDataKeys.CloneEntry,
            LttngThreadDataCooker.GetPidExit
        });

        public const string Identifier = "ThreadDataCooker";
        public const string CookerPath = LttngConstants.SourceId + "/" + Identifier;
        public static readonly DataCookerPath DataCookerPath = new DataCookerPath(LttngConstants.SourceId, LttngThreadDataCooker.Identifier);

        public LttngThreadDataCooker()
            : base(Identifier)
        {
        }

        public DataCookerPath GetCookerPath()
        {
            return Path;
        }

        private readonly Dictionary<uint, ContextSwitch> lastContextSwitch = new Dictionary<uint, ContextSwitch>();
        private readonly List<ExecutionEvent> processedExecutionEvents = new List<ExecutionEvent>();
        private readonly List<Thread> terminatedThreads = new List<Thread>();
        private readonly Dictionary<int, ThreadInfo> runningThreads = new Dictionary<int, ThreadInfo>();
        private readonly Dictionary<int, Dictionary<uint, int>> cloneSyscallFlagsPerTid = new Dictionary<int, Dictionary<uint, int>>();
        private readonly Dictionary<int, int> closingEventsToBeSkipped = new Dictionary<int, int>();
        private readonly Dictionary<int, int> recoveredPids = new Dictionary<int, int>();
        private readonly ThreadTracker threadTracker = new ThreadTracker();
        private Timestamp lastEventTimestamp;
        private readonly DiscardedEventsTracker discardedEventsTracker = new DiscardedEventsTracker(); 

        public override string Description => "Processes LTTNG events related to threads and context switches.";
        public override ReadOnlyHashSet<string> DataKeys { get; }

        public override SourceDataCookerOptions Options => SourceDataCookerOptions.ReceiveAllDataElements;

        [DataOutput]
        public IReadOnlyList<IThread> Threads => this.terminatedThreads;
        [DataOutput]
        public IReadOnlyList<IExecutionEvent> ExecutionEvents => this.processedExecutionEvents;
        [DataOutput]
        public IThreadTracker ThreadTracker => this.threadTracker;

        private ThreadInfo CurrentExecutingThread(uint cpu)
        {
            if (lastContextSwitch.TryGetValue(cpu, out ContextSwitch prevContextSwitch) && 
                runningThreads.TryGetValue(prevContextSwitch.NextTid, out ThreadInfo currentThread))
            {
                return currentThread;
            }
            return null;
        }

        private ThreadInfo EventCreatorThread(LttngEvent data, uint cpu)
        {
            if (data.StreamDefinedEventContext != null && data.StreamDefinedEventContext.FieldsByName.ContainsKey("_tid") &&
                runningThreads.TryGetValue(data.StreamDefinedEventContext.ReadFieldAsInt32("_tid"), out ThreadInfo callerThread))
            {
                return callerThread;
            }
            return this.CurrentExecutingThread(cpu);
        }

        private void AddNewThread(ThreadInfo newThread)
        {
            this.runningThreads[newThread.Tid] = newThread;
        }

        private void RecoverPid(ThreadInfo thread, int newPid)
        {
            this.recoveredPids[thread.Pid] = newPid;
            thread.Pid = newPid;
        }

        public override void EndDataCooking(CancellationToken cancellationToken)
        {
            foreach (var threadInfo in this.runningThreads.Values)
            {
                if (!threadInfo.isTerminated())
                {
                    threadInfo.Terminate(this.lastEventTimestamp);
                }
                this.terminatedThreads.Add(new Thread(threadInfo));
            }

            this.terminatedThreads.ForEach(t => t.RecoverPid(recoveredPids));
            for (int i=0; i<this.terminatedThreads.Count; ++i)
            {
                ///iterated in this fashion to guarantee order
                this.threadTracker.ProcessThread(this.terminatedThreads[i]);
            }
            this.processedExecutionEvents.ForEach(t => t.RecoverPids(recoveredPids));

            this.runningThreads.Clear();
            this.cloneSyscallFlagsPerTid.Clear();
            this.closingEventsToBeSkipped.Clear();
            this.cloneSyscallFlagsPerTid.Clear();
        }
            
        private void ProcessEventDrop(LttngEvent data, LttngContext context)
        {
            uint discardedEventsBetweenLastTwoEvents = this.discardedEventsTracker.EventsDiscardedBetweenLastTwoEvents(data, context);
            if (discardedEventsBetweenLastTwoEvents > 0)
            {
                if (this.lastContextSwitch.TryGetValue(context.CurrentCpu, out ContextSwitch lastContextSwitchOnCpu))
                {
                    processedExecutionEvents.Add(new ExecutionEvent(lastContextSwitchOnCpu, lastContextSwitchOnCpu.SwitchInTime));
                    this.lastContextSwitch.Remove(context.CurrentCpu);
                }
                this.cloneSyscallFlagsPerTid.Clear();
                this.closingEventsToBeSkipped.Clear();
            }
        }

        public override DataProcessingResult CookDataElement(LttngEvent data, LttngContext context, CancellationToken cancellationToken)
        {
            this.lastEventTimestamp = data.Timestamp;
            this.ProcessEventDrop(data, context);
            var result = DataProcessingResult.Ignored;
            
            try
            {
                if (data.Name.StartsWith("syscall") && this.EventHasMetadata(data))
                {
                    this.ProcessSyscallEventMetadata(data);
                    result = DataProcessingResult.Processed;
                }
                switch (data.Name)
                {
                    case SchedulerDataKeys.Switch:
                        this.ProcessContextSwitch(data, context);
                        return DataProcessingResult.Processed;
                    case SchedulerDataKeys.Wakeup:
                    case SchedulerDataKeys.NewWakeup:
                        this.ProcessThreadWakeUp(data, context);
                        return DataProcessingResult.Processed;
                    case SchedulerDataKeys.Waking:
                        this.ProcessThreadWaking(data, context);
                        return DataProcessingResult.Processed;
                    case SchedulerDataKeys.ProcessFork:
                        this.ProcessSchedForkEvent(data, context);
                        return DataProcessingResult.Processed;
                    case SchedulerDataKeys.ProcessExit:
                        this.ProcessThreadExit(data);
                        return DataProcessingResult.Processed;
                    case SchedulerDataKeys.ProcessExec:
                        this.ProcessThreadExec(data);
                        return DataProcessingResult.Processed;
                    case ThreadCreationDataKeys.CloneEntry:
                        this.ProcessThreadCloneSyscallEntry(data, context);
                        return DataProcessingResult.Processed;
                    case ThreadCreationDataKeys.CloneExit:
                        this.ProcessThreadCloneSyscallExit(data, context);
                        return DataProcessingResult.Processed;
                    case LttngThreadDataCooker.GetPidExit:
                        this.ProcessGetPidSyscallExit(data, context);
                        return DataProcessingResult.Processed;
                    case StatedumpDataKeys.ProcessState:
                        this.ProcessStatedump(data);
                        return DataProcessingResult.Processed;
                }
                
            }
            catch (CtfPlaybackException e)
            {
                Console.Error.WriteLine(e);
                return DataProcessingResult.CorruptData;
            }

            return result;
        }

        bool EventHasMetadata(LttngEvent data)
        {
            return data.StreamDefinedEventContext != null && data.StreamDefinedEventContext.FieldsByName.ContainsKey("_tid") &&
                  (data.StreamDefinedEventContext.FieldsByName.ContainsKey("_pid") || data.StreamDefinedEventContext.FieldsByName.ContainsKey("_procname"));
        }

        void ProcessStatedump(LttngEvent data)
        {
            int tid = data.Payload.ReadFieldAsInt32("_tid");
            if (data.StreamDefinedEventContext != null && this.runningThreads.TryGetValue(tid, out ThreadInfo thread))
            {
                this.ProcessThreadMetadata(data, thread);
            }
        }

        void ProcessSyscallEventMetadata(LttngEvent data)
        {
            int tid = data.StreamDefinedEventContext.ReadFieldAsInt32("_tid");
            ThreadInfo callerThread;
            if (!this.runningThreads.TryGetValue(tid, out callerThread))
            {
                callerThread = new ThreadInfo(tid, data.Timestamp, ThreadInfo.ThreadState.TASK_RUNNING);
                this.AddNewThread(callerThread);
            }

            this.ProcessThreadMetadata(data, callerThread);
        }

        void ProcessThreadMetadata(LttngEvent data, ThreadInfo thread)
        {
            if (data.StreamDefinedEventContext.FieldsByName.ContainsKey("_pid"))
            {
                int pid = data.StreamDefinedEventContext.ReadFieldAsInt32("_pid");
                if (thread.Pid != pid)
                {
                    this.RecoverPid(thread, pid);
                }
            }

            if (thread.Command.Equals(String.Empty))
            {
                if (data.StreamDefinedEventContext.FieldsByName.ContainsKey("_procname"))
                {
                    thread.Command = data.StreamDefinedEventContext.ReadFieldAsArray("_procname").ReadAsString();
                }
                else if (data.StreamDefinedEventContext.FieldsByName.ContainsKey("_name"))
                {
                    thread.Command = data.StreamDefinedEventContext.ReadFieldAsArray("_name").ReadAsString();
                }
            }
        }

        public void ProcessContextSwitch(LttngEvent data, LttngContext context)
        {
            int prevTid = data.Payload.ReadFieldAsInt32("_prev_tid");
            if (lastContextSwitch.TryGetValue(context.CurrentCpu, out ContextSwitch previousContextSwitch))
            {
                if (prevTid == previousContextSwitch.NextTid)
                {
                    processedExecutionEvents.Add(new ExecutionEvent(previousContextSwitch, data.Timestamp));
                }
                else
                {
                    ///If we missed context switch events, 
                    processedExecutionEvents.Add(new ExecutionEvent(previousContextSwitch, previousContextSwitch.SwitchInTime));
                }
                
            }
            int nextTid = data.Payload.ReadFieldAsInt32("_next_tid");
            ThreadInfo.ThreadState switchOutState;
            var prevStateValue = data.Payload.FieldsByName["_prev_state"];
            if (prevStateValue.FieldType == CtfTypes.Enum)
            {
               ((CtfEnumValue)prevStateValue).IntegerValue.TryGetInt32(out int enumValue);
                switchOutState = (ThreadInfo.ThreadState)enumValue;
            }
            else 
            {
                switchOutState = (ThreadInfo.ThreadState)data.Payload.ReadFieldAsInt32("_prev_state");
            }
            
            ThreadInfo prevThread;
            if (runningThreads.TryGetValue(prevTid, out prevThread))
            {
                prevThread.SwitchOut(data, switchOutState);
            }
            else
            {
                prevThread = new ThreadInfo(prevTid, data.Timestamp, switchOutState);
                this.AddNewThread(prevThread);
            }

            if (this.EventHasMetadata(data))
            {
                this.ProcessThreadMetadata(data, prevThread);
            }

            ThreadInfo nextThread;
            if (runningThreads.TryGetValue(nextTid, out nextThread))
            {
                lastContextSwitch[context.CurrentCpu] = new ContextSwitch(data, nextThread, prevThread, context.CurrentCpu);
                nextThread.SwitchIn(data.Timestamp);
            }
            else
            {
                nextThread = new ThreadInfo(nextTid, data.Timestamp, ThreadInfo.ThreadState.TASK_RUNNING);
                lastContextSwitch[context.CurrentCpu] = new ContextSwitch(data, nextThread, prevThread, context.CurrentCpu);
                this.AddNewThread(nextThread);
            }
        }

        public void ProcessThreadExit(LttngEvent data)
        {
            int tid = data.Payload.ReadFieldAsInt32("_tid");
            if (runningThreads.TryGetValue(tid, out ThreadInfo exitingThread))
            {
                exitingThread.Terminate(data.Timestamp);
            }
        }

        public void ProcessThreadWaking(LttngEvent data, LttngContext context)
        {
            int tid = data.Payload.ReadFieldAsInt32("_tid");
            ThreadInfo readyingThread = this.EventCreatorThread(data, context.CurrentCpu);
            if (runningThreads.TryGetValue(tid, out ThreadInfo nextThread))
            {
                nextThread.Waking(data.Timestamp, readyingThread);
            }
            else
            {
                this.AddNewThread(new ThreadInfo(tid, data.Timestamp, ThreadInfo.ThreadState.TASK_WAKING, readyingThread));
            }
        }

        public void ProcessThreadWakeUp(LttngEvent data, LttngContext context)
        {
            int tid = data.Payload.ReadFieldAsInt32("_tid");
            if (runningThreads.TryGetValue(tid, out ThreadInfo nextThread))
            {
                nextThread.Wakeup(data.Timestamp);
            }
            else
            {
                ThreadInfo readyingThread = this.EventCreatorThread(data, context.CurrentCpu);
                this.AddNewThread(new ThreadInfo(tid, data.Timestamp, ThreadInfo.ThreadState.TASK_RUNNING, readyingThread));
            }
        }

        public void ProcessSchedForkEvent(LttngEvent data, LttngContext context)
        {
            int newTid = data.Payload.ReadFieldAsInt32("_child_tid");
            if (runningThreads.TryGetValue(newTid, out ThreadInfo oldThreadWithSamePid))
            {
                this.terminatedThreads.Add(new Thread(oldThreadWithSamePid));
            }

            ThreadInfo parentThread = null;
            int parentTid = data.Payload.ReadFieldAsInt32("_parent_tid");
            if (runningThreads.ContainsKey(parentTid))
            {
                parentThread = runningThreads[parentTid];
                if (parentThread.Pid < 0)
                {
                    this.RecoverPid(parentThread, data.Payload.ReadFieldAsInt32("_parent_pid"));
                }
            }
            this.AddNewThread(new ThreadInfo(newTid, data.Timestamp, ThreadInfo.ThreadState.TASK_NEW, parentThread));
        }

        public void ProcessThreadCloneSyscallEntry(LttngEvent data, LttngContext context)
        {
            ThreadInfo threadInExecution = this.EventCreatorThread(data, context.CurrentCpu);
            if (threadInExecution != null)
            {
                if (this.closingEventsToBeSkipped.TryGetValue(threadInExecution.Tid, out int eventsToBeSkipped) && eventsToBeSkipped > 0)
                {
                    this.closingEventsToBeSkipped[threadInExecution.Tid] = eventsToBeSkipped + 1;
                    return;
                }
                uint flags = data.Payload.ReadFieldAsUInt32("_clone_flags");
                if (cloneSyscallFlagsPerTid.TryGetValue(threadInExecution.Tid, out Dictionary<uint, int> lastCalls))
                {
                    if (lastCalls.TryGetValue(flags, out int timesUsed))
                    {
                        lastCalls[flags] = timesUsed + 1;
                    }
                    else
                    {
                        lastCalls[flags] = 1;
                    }
                }
                else
                {
                    cloneSyscallFlagsPerTid[threadInExecution.Tid] = new Dictionary<uint, int>() { [flags] = 1 };
                }
            }
        }

        public void ProcessThreadCloneSyscallExit(LttngEvent data, LttngContext context)
        {
            int newThreadTid = data.Payload.ReadFieldAsInt32("_ret");
            if (newThreadTid == 0)
            {
                ///This is the returning syscall for the newly created child
                return;
            }
            ThreadInfo threadInExecution = this.EventCreatorThread(data, context.CurrentCpu);
            if (threadInExecution != null)
            {
                if (this.closingEventsToBeSkipped.TryGetValue(threadInExecution.Tid, out int eventsToBeSkipped) && eventsToBeSkipped > 0)
                {
                    if (eventsToBeSkipped == 1)
                    {
                        this.closingEventsToBeSkipped.Remove(threadInExecution.Tid);
                    }
                    else
                    {
                        this.closingEventsToBeSkipped[threadInExecution.Tid] = eventsToBeSkipped - 1;
                    }
                }
                else if (cloneSyscallFlagsPerTid.TryGetValue(threadInExecution.Tid, out Dictionary<uint, int> lastCalls))
                {
                    if (lastCalls.Count == 1)
                    {
                        uint flag = new List<uint>(lastCalls.Keys)[0];
                        if (newThreadTid > 0 && (flag & 0x00010000) > 0 && runningThreads.TryGetValue(threadInExecution.Tid, out ThreadInfo parentThread))
                        {
                            if (runningThreads.TryGetValue(newThreadTid, out ThreadInfo newThread) )
                            {
                                newThread.Pid = parentThread.Pid;
                            }
                            else
                            {
                                this.AddNewThread(new ThreadInfo(newThreadTid, data.Timestamp, ThreadInfo.ThreadState.TASK_NEW, threadInExecution, true));
                            }
                        }

                        int amountOfEntries = lastCalls[flag];
                        if (amountOfEntries <= 1)
                        {
                            lastCalls.Remove(flag);
                        }
                        else
                        {
                            lastCalls[flag] = amountOfEntries - 1;
                        }
                    }
                    else if (lastCalls.Count > 1)
                    {
                        int pendingEntryEvents = 0;
                        foreach (var entry in lastCalls)
                        {
                            pendingEntryEvents += entry.Value;
                        }
                        closingEventsToBeSkipped[threadInExecution.Tid] = pendingEntryEvents - 1; ///Decrease one for the current closing event
                        lastCalls.Clear();
                    }
                }
            }
        }
        private void ProcessGetPidSyscallExit(LttngEvent data, LttngContext context)
        {
            if (data.StreamDefinedEventContext == null || 
                !data.StreamDefinedEventContext.FieldsByName.ContainsKey("_pid") ||
                !data.StreamDefinedEventContext.FieldsByName.ContainsKey("_tid"))
            {
                ///This heuristic is activated when the event context does not have metadata regarding to tid and pid
                ThreadInfo executingThread = CurrentExecutingThread(context.CurrentCpu);
                int pid = data.Payload.ReadFieldAsInt32("_ret");
                if (executingThread != null && executingThread.Pid < 0)
                {
                    this.RecoverPid(executingThread, pid);
                }
            }
        }

        private void ProcessThreadExec(LttngEvent data)
        {
            var filepath = data.Payload.ReadFieldAsString("_filename").GetValueAsString().Split('/');
            if (filepath.Length > 0)
            {
                string command = filepath[filepath.Length - 1];

                int tid = data.Payload.ReadFieldAsInt32("_tid");
                if (!this.runningThreads.TryGetValue(tid, out ThreadInfo runningThread))
                {
                    this.AddNewThread(new ThreadInfo(tid, data.Timestamp, ThreadInfo.ThreadState.TASK_RUNNING));
                }
                else if (runningThread.ExecTimeNs.ToMilliseconds > 10)
                {
                    ///if it is not a fork + execv, we terminate the old thread
                    runningThread.Terminate(data.Timestamp);
                    this.terminatedThreads.Add(new Thread(runningThread));
                    this.AddNewThread(new ThreadInfo(tid, data.Timestamp, ThreadInfo.ThreadState.TASK_RUNNING, runningThread, true));
                }
                runningThreads[tid].Command = command;
            }
        }
    }
}