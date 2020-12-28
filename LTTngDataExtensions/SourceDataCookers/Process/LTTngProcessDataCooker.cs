// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using CtfPlayback;
using CtfPlayback.FieldValues;
using LTTngCds.CookerData;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;

namespace LTTngDataExtensions.SourceDataCookers.Process
{
   /* public class LTTngProcessDataCooker
        : LTTngBaseSourceCooker
    {
        public const string Identifier = "ProcessDataCooker";

        private static class EventName
        {
            public const string Exec = "sched_process_exec";
            public const string Exit = "sched_process_exit";
            public const string Fork = "sched_process_fork";
            public const string Free = "sched_process_free";
            public const string StateDump = "lttng_statedump_process_state";
            public const string Wait = "sched_process_Wait";
        }

        private static readonly HashSet<string> Keys = new HashSet<string>(
            new[]
            {
                EventName.StateDump,
                EventName.Exec,
                EventName.Exit,
                EventName.Fork,
                EventName.Free,
                EventName.Wait,
            });

        private readonly Dictionary<int, ProcessBuilder> processesInProgress = new Dictionary<int, ProcessBuilder>();
        private readonly List<ProcessBuilder> processesBuilders = new List<ProcessBuilder>();

        private List<IProcess> processes = new List<IProcess>();

        public LTTngProcessDataCooker()
            : base(Identifier)
        {
            this.DataKeys = new ReadOnlyHashSet<string>(Keys);
        }

        public override string Description => "Processes LTTNG events related to processes.";

        public override ReadOnlyHashSet<string> DataKeys { get; }

        public Action<LTTngEvent, LTTngContext> OnEvent { get; set; }

        public override DataProcessingResult CookDataElement(
            LTTngEvent data, 
            LTTngContext context, 
            CancellationToken cancellationToken)
        {
            try
            {
                this.OnEvent?.Invoke(data, context);

                switch (data.Name)
                {
                    case EventName.Exec:
                        this.ProcessExec(data);
                        return DataProcessingResult.Processed;
                    case EventName.Exit:
                        this.ProcessExit(data);
                        return DataProcessingResult.Processed;
                    case EventName.Fork:
                        this.ProcessFork(data);
                        return DataProcessingResult.Processed;
                    case EventName.Free:
                        this.ProcessFree(data);
                        return DataProcessingResult.Processed;
                    case EventName.StateDump:
                        this.ProcessStateDump(data);
                        return DataProcessingResult.Processed;
                    case EventName.Wait:
                        this.ProcessWait(data);
                        return DataProcessingResult.Processed;
                    default:
                        return DataProcessingResult.Ignored;
                }
            }
            catch (CtfPlaybackException e)
            {
                Console.Error.WriteLine(e);
                return DataProcessingResult.CorruptData;
            }
        }

        public override void EndDataCooking(CancellationToken cancellationToken)
        {
            this.processes = new List<IProcess>(this.processesBuilders.Count);

            for (int index = 0; index < this.processesBuilders.Count; ++index)
            {
                this.processes.Add(this.processesBuilders[index].Build());
            }
        }

        [DataOutput]
        public IReadOnlyList<IProcess> Processes => this.processes;

        ProcessBuilder GetOrAddProcess(int threadId)
        {
            if (!this.processesInProgress.TryGetValue(threadId, out ProcessBuilder process))
            {
                process = new ProcessBuilder();
                this.processesInProgress.Add(threadId, process);
                this.processesBuilders.Add(process);
            }

            return process;
        }

        void ProcessExec(LTTngEvent data)
        {
            ExecUserData parsed = ExecUserData.Read(data.Payload);

            ProcessBuilder process = this.GetOrAddProcess(parsed.ThreadId);
            process.ExecTime = (ulong)data.Timestamp.ToNanoseconds;
            process.Path = parsed.FileName;
        }

        void ProcessExit(LTTngEvent data)
        {
            ExitUserData parsed = ExitUserData.Read(data.Payload);

            ProcessBuilder process = this.GetOrAddProcess(parsed.ThreadId);
            process.ExitTime = (ulong)data.Timestamp.ToNanoseconds;
        }

        void ProcessFork(LTTngEvent data)
        {
            ForkUserData parsed = ForkUserData.Read(data.Payload);

            ProcessBuilder process = this.GetOrAddProcess(parsed.ChildThreadId);
            process.ForkTime = (ulong)data.Timestamp.ToNanoseconds;
            process.ProcessId = parsed.ChildProcessId;
            process.ParentProcessId = parsed.ParentProcessId;
            if (string.IsNullOrWhiteSpace(process.Name))
            {
                process.Name = parsed.ChildImageName;
            }
        }

        void ProcessFree(LTTngEvent data)
        {
            FreeUserData parsed = FreeUserData.Read(data.Payload);

            ProcessBuilder process = this.GetOrAddProcess(parsed.ThreadId);
            process.FreeTime = (ulong)data.Timestamp.ToNanoseconds;
        }

        void ProcessStateDump(LTTngEvent data)
        {
            StateDumpUserData parsed = StateDumpUserData.Read(data.Payload);

            ProcessBuilder process = this.GetOrAddProcess(parsed.ThreadId);
            process.ProcessId = parsed.ProcessId;
            process.ParentProcessId = parsed.ParentProcessId;
            process.Name = parsed.Name;
        }

        void ProcessWait(LTTngEvent data)
        {
            WaitUserData parsed = WaitUserData.Read(data.Payload);

            ProcessBuilder process = this.GetOrAddProcess(parsed.ThreadId);
            process.WaitTime = (ulong)data.Timestamp.ToNanoseconds;
        }

        class ExecUserData
        {
            public string FileName;
            public int ThreadId;
            public int OldThreadId;

            public static ExecUserData Read(CtfStructValue data)
            {
                return new ExecUserData
                {
                    FileName = data.ReadFieldAsString("_filename").Value,
                    ThreadId = data.ReadFieldAsInt32("_tid"),
                    OldThreadId = data.ReadFieldAsInt32("_old_tid")
                };
            }
        }

        class ExitUserData
        {
            public string ImageName;
            public int ThreadId;
            public int Priority;

            public static ExitUserData Read(CtfStructValue data)
            {
                return new ExitUserData
                {
                    ImageName = data.ReadFieldAsArray("_comm").ReadAsString(),
                    ThreadId = data.ReadFieldAsInt32("_tid"),
                    Priority = data.ReadFieldAsInt32("_prio")
                };
            }
        }

        class ForkUserData
        {
            public string ParentImageName;
            public int ParentThreadId;
            public int ParentProcessId;
            public uint ParentNamespaceInodeNumber;
            public string ChildImageName;
            public int ChildThreadId;
            public byte VirtualThreadCount;
            public int[] VirtualThreads;
            public int ChildProcessId;
            public uint ChildNamespaceInodeNumber;

            public static ForkUserData Read(CtfStructValue data)
            {
                return new ForkUserData
                {
                    ParentImageName = data.ReadFieldAsArray("_parent_comm").ReadAsString(),
                    ParentThreadId = data.ReadFieldAsInt32("_parent_tid"),
                    ParentProcessId = data.ReadFieldAsInt32("_parent_pid"),
                    ParentNamespaceInodeNumber = data.ReadFieldAsUInt32("_parent_ns_inum"),
                    ChildImageName = data.ReadFieldAsArray("_child_comm").ReadAsString(),
                    ChildThreadId = data.ReadFieldAsInt32("_child_tid"),
                    VirtualThreadCount = data.ReadFieldAsUInt8("__vtids_length"),
                    VirtualThreads = data.ReadFieldAsArray("_vtids").ReadAsInt32Array(),
                    ChildProcessId = data.ReadFieldAsInt32("_child_pid"),
                    ChildNamespaceInodeNumber = data.ReadFieldAsUInt32("_child_ns_inum")
                };
            }
        }

        class FreeUserData
        {
            public string ImageName;
            public int ThreadId;
            public int Priority;

            public static FreeUserData Read(CtfStructValue data)
            {
                return new FreeUserData
                {
                    ImageName = data.ReadFieldAsArray("_comm").ReadAsString(),
                    ThreadId = data.ReadFieldAsInt32("_tid"),
                    Priority = data.ReadFieldAsInt32("_prio")
                };
            }
        }

        class StateDumpUserData
        {
            public int ThreadId;
            public int VirtualThreadId;
            public int ProcessId;
            public int VirtualProcessId;
            public int ParentProcessId;
            public int VirtualParentProcessId;
            public string Name;
            public int Type;
            public int Mode;
            public int Submode;
            public int Status;
            public int NamespaceLevel;
            public uint NamespaceInodeNumber;
            public uint Cpu;

            public static StateDumpUserData Read(CtfStructValue data)
            {
                return new StateDumpUserData
                {
                    ThreadId = data.ReadFieldAsInt32("_tid"),
                    VirtualThreadId = data.ReadFieldAsInt32("_vtid"),
                    ProcessId = data.ReadFieldAsInt32("_pid"),
                    VirtualProcessId = data.ReadFieldAsInt32("_vpid"),
                    ParentProcessId = data.ReadFieldAsInt32("_ppid"),
                    VirtualParentProcessId = data.ReadFieldAsInt32("_vppid"),
                    Name = data.ReadFieldAsArray("_name").ReadAsString(),
                    Type = data.ReadFieldAsInt32("_type"),
                    Mode = data.ReadFieldAsInt32("_mode"),
                    Submode = data.ReadFieldAsInt32("_submode"),
                    Status = data.ReadFieldAsInt32("_status"),
                    NamespaceLevel = data.ReadFieldAsInt32("_ns_level"),
                    NamespaceInodeNumber = data.ReadFieldAsUInt32("_ns_inum"),
                    Cpu = data.ReadFieldAsUInt32("_cpu")
                };
            }
        }

        class WaitUserData
        {
            public string ImageName;
            public int ThreadId;
            public int Priority;

            public static WaitUserData Read(CtfStructValue data)
            {
                return new WaitUserData
                {
                    ImageName = data.ReadFieldAsArray("_comm").ReadAsString(),
                    ThreadId = data.ReadFieldAsInt32("_tid"),
                    Priority = data.ReadFieldAsInt32("_prio")
                };
            }
        }
    }*/
}