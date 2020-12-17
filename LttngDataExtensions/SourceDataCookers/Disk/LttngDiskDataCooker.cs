// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using CtfPlayback.FieldValues;
using LttngCds.CookerData;
using LttngDataExtensions.DataOutputTypes;
using LttngDataExtensions.SourceDataCookers.Thread;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using LttngDataExtensions.SourceDataCookers.Syscall;

namespace LttngDataExtensions.SourceDataCookers.Disk
{
    public class LttngDiskDataCooker
        : LttngBaseSourceCooker
    {
        struct FileDescriptorInfo
        {
            public string Filepath;
            public uint? DeviceId;

            public FileDescriptorInfo(string filepath, uint? deviceId)
            {
                this.Filepath = filepath;
                this.DeviceId = deviceId;
            }
        }

        public const string Identifier = "DiskDataCooker";
        public const string CookerPath = LttngConstants.SourceId + "/" + Identifier;

        private static class DiskDataKeys
        {
            public const string Complete = "block_rq_complete";
            public const string Insert = "block_rq_insert";
            public const string Issue = "block_rq_issue";
        }

        private static class StatedumpDataKeys
        {
            public const string FileDescriptor = "lttng_statedump_file_descriptor";
            public const string BlockDevice = "lttng_statedump_block_device";
        }

        private static class FileIOEntryDataKeys
        {
            public const string ReadEntry = "syscall_entry_read";
            public const string ReadVEntry = "syscall_entry_readv";
            public const string PReadEntry = "syscall_entry_pread";
            public const string PReadVEntry = "syscall_entry_preadv";
            public const string WriteEntry = "syscall_entry_write";
            public const string WriteVEntry = "syscall_entry_writev";
            public const string PWriteEntry = "syscall_entry_pwrite";
            public const string PWriteVEntry = "syscall_entry_pwritev";
            public const string LSeekEntry = "syscall_entry_lseek";
            public const string IoctlEntry = "syscall_entry_ioctl";
            public const string NewfstatEntry = "syscall_entry_newfstat";
            public const string SendFileEntry = "syscall_entry_sendfile";
            public const string FTruncateEntry = "syscall_entry_ftruncate";
            public const string FAllocateEntry = "syscall_entry_fallocate";
            public const string FAdvise64Entry = "syscall_entry_fadvise64";
            public const string FChdirEntry = "syscall_entry_fchdir";
            public const string FChmodEntry = "syscall_entry_fchmod";
            public const string SyncFileRangeEntry = "syscall_entry_sync_file_range";
        }

        private static class FileIOExitDataKeys
        {
            public const string ReadExit = "syscall_exit_read";
            public const string ReadVExit = "syscall_exit_readv";
            public const string PReadExit = "syscall_exit_pread";
            public const string PReadVExit = "syscall_exit_preadv";
            public const string WriteExit = "syscall_exit_write";
            public const string WriteVExit = "syscall_exit_writev";
            public const string PWriteExit = "syscall_exit_pwrite";
            public const string PWriteVExit = "syscall_exit_pwritev";
            public const string LSeekExit = "syscall_exit_lseek";
            public const string IoctlExit = "syscall_exit_ioctl";
            public const string NewfstatExit = "syscall_exit_newfstat";
            public const string SendFileExit = "syscall_exit_sendfile";
            public const string FTruncateExit = "syscall_exit_ftruncate";
            public const string FAllocateExit = "syscall_exit_fallocate";
            public const string FAdvise64Exit = "syscall_exit_fadvise64";
            public const string FChdirExit = "syscall_exit_fchdir";
            public const string FChmodExit = "syscall_exit_fchmod";
            public const string SyncFileRangeExit = "syscall_exit_sync_file_range";
        }

        private static class FilepathDataKeys
        {
            public const string CreateEntry = "syscall_entry_creat";
            public const string CreateExit = "syscall_exit_creat";
            public const string OpenEntry = "syscall_entry_open";
            public const string OpenExit = "syscall_exit_open";
            public const string OpenAtEntry = "syscall_entry_openat";
            public const string OpenAtExit = "syscall_exit_openat";
            public const string MemfdCreateEntry = "syscall_entry_memfd_create";
            public const string MemfdCreateExit = "syscall_exit_memfd_create";
            public const string MknodAtEntry = "syscall_entry_mknodat";
            public const string MknodAtExit = "syscall_exit_mknodat";
            public const string MknodEntry = "syscall_entry_mknod";
            public const string MknodExit = "syscall_exit_mknod";
            public const string NewstatEntry = "syscall_entry_newstat";
            public const string NewstatExit = "syscall_exit_newstat";
            public const string ChdirEntry = "syscall_entry_chdir";
            public const string ChdirExit = "syscall_exit_chdir";
            public const string ChmodEntry = "syscall_entry_chmod";
            public const string ChmodExit = "syscall_exit_chmod";
            public const string ChmodAtEntry = "syscall_entry_chmodat";
            public const string ChmodAtExit = "syscall_exit_chmodat";
            public const string ChrootEntry = "syscall_entry_chroot";
            public const string ChrootExit = "syscall_exit_chroot";
            public const string MkdirEntry = "syscall_entry_mkdir";
            public const string MkdirExit = "syscall_exit_mkdir";
            public const string MkdirAtEntry = "syscall_entry_mkdirat";
            public const string MkdirAtExit = "syscall_exit_mkdirat";
            public const string RmdirEntry = "syscall_entry_rmdir";
            public const string RmdirExit = "syscall_exit_rmdir";
            public const string TruncateEntry = "syscall_entry_truncate";
            public const string TruncateExit = "syscall_exit_truncate";
            public const string OpenByHandleAtEntry = "syscall_entry_open_by_handle_at";
            public const string OpenByHandleAtExit = "syscall_exit_open_by_handle_at";
        }

        private static class RenameFileDataKeys
        {
            public const string RenameEntry = "syscall_entry_rename";
            public const string RenameExit = "syscall_exit_rename";
            public const string RenameatEntry = "syscall_entry_renameat";
            public const string RenameatExit = "syscall_exit_renameat";
            public const string Renameat2Entry = "syscall_entry_renameat2";
            public const string Renameat2Exit = "syscall_exit_renameat2";
        }

        private static class FileHandleDataKeys
        {
            public const string NameToHandleAtEntry = "syscall_entry_name_to_handle_at";
            public const string NameToHandleAtExit = "syscall_exit_name_to_handle_at";
        }

        private static readonly HashSet<string> DiskKeys = new HashSet<string>(new[]
        {
            DiskDataKeys.Complete,
            DiskDataKeys.Insert,
            DiskDataKeys.Issue,
        });

        private bool IsDiskEvent(string eventName)
        {
            return DiskKeys.Contains(eventName);
        }

        private static readonly HashSet<string> FileIOEntryKeys = new HashSet<string>(new[]
        {
            FileIOEntryDataKeys.ReadEntry,
            FileIOEntryDataKeys.ReadVEntry,
            FileIOEntryDataKeys.PReadEntry,
            FileIOEntryDataKeys.PReadVEntry,
            FileIOEntryDataKeys.WriteEntry,
            FileIOEntryDataKeys.WriteVEntry,
            FileIOEntryDataKeys.PWriteEntry,
            FileIOEntryDataKeys.PWriteVEntry,
            FileIOEntryDataKeys.LSeekEntry,
            FileIOEntryDataKeys.IoctlEntry,
            FileIOEntryDataKeys.NewfstatEntry,
            FileIOEntryDataKeys.SendFileEntry,
            FileIOEntryDataKeys.FTruncateEntry,
            FileIOEntryDataKeys.FAllocateEntry,
            FileIOEntryDataKeys.FAdvise64Entry,
            FileIOEntryDataKeys.FChdirEntry,
            FileIOEntryDataKeys.FChmodEntry,
            FileIOEntryDataKeys.SyncFileRangeEntry
        });

        private bool IsFileIOEntryEvent(string eventName)
        {
            return FileIOEntryKeys.Contains(eventName);
        }

        private static readonly HashSet<string> FileIOExitKeys = new HashSet<string>(new[]
        {
            FileIOExitDataKeys.ReadExit,
            FileIOExitDataKeys.ReadVExit,
            FileIOExitDataKeys.PReadExit,
            FileIOExitDataKeys.PReadVExit,
            FileIOExitDataKeys.WriteExit,
            FileIOExitDataKeys.WriteVExit,
            FileIOExitDataKeys.PWriteExit,
            FileIOExitDataKeys.PWriteVExit,
            FileIOExitDataKeys.LSeekExit,
            FileIOExitDataKeys.IoctlExit,
            FileIOExitDataKeys.NewfstatExit,
            FileIOExitDataKeys.SendFileExit,
            FileIOExitDataKeys.FTruncateExit,
            FileIOExitDataKeys.FAllocateExit,
            FileIOExitDataKeys.FAdvise64Exit,
            FileIOExitDataKeys.FChdirExit,
            FileIOExitDataKeys.FChmodExit,
            FileIOExitDataKeys.SyncFileRangeExit,
        });

        private bool IsFileIOExitEvent(string eventName)
        {
            return FileIOExitKeys.Contains(eventName);
        }

        private static readonly HashSet<string> OpenFileEntryKeys = new HashSet<string>(new[]
        {
            FilepathDataKeys.CreateEntry,
            FilepathDataKeys.OpenEntry,
            FilepathDataKeys.OpenAtEntry,
            FilepathDataKeys.MemfdCreateEntry,
            FilepathDataKeys.OpenByHandleAtEntry,
        });

        private static readonly HashSet<string> OpenFileExitKeys = new HashSet<string>(new[]
        {
            FilepathDataKeys.CreateExit,
            FilepathDataKeys.OpenExit,
            FilepathDataKeys.OpenAtExit,
            FilepathDataKeys.MemfdCreateExit,
            FilepathDataKeys.OpenByHandleAtExit,
        });

        private bool IsOpenFileSyscallExitEvent(string eventName)
        {
            return OpenFileExitKeys.Contains(eventName);
        }

        private static readonly HashSet<string> FilepathEntryKeys = new HashSet<string>(new[]
        {
            FilepathDataKeys.MknodAtEntry,
            FilepathDataKeys.MknodEntry,
            FilepathDataKeys.NewstatEntry,
            FilepathDataKeys.TruncateEntry,
            FilepathDataKeys.ChdirEntry,
            FilepathDataKeys.ChrootEntry,
            FilepathDataKeys.MkdirEntry,
            FilepathDataKeys.MkdirAtEntry,
            FilepathDataKeys.RmdirEntry,
            FilepathDataKeys.ChmodEntry,
            FilepathDataKeys.ChmodAtEntry,
        });

        private bool IsFilepathSyscallEntryEvent(string eventName)
        {
            return FilepathEntryKeys.Contains(eventName);
        }

        private static readonly HashSet<string> FilepathExitKeys = new HashSet<string>(new[]
        {
            FilepathDataKeys.MknodAtExit,
            FilepathDataKeys.MknodExit,
            FilepathDataKeys.NewstatExit,
            FilepathDataKeys.TruncateExit,
            FilepathDataKeys.ChdirExit,
            FilepathDataKeys.ChrootExit,
            FilepathDataKeys.MkdirExit,
            FilepathDataKeys.MkdirAtExit,
            FilepathDataKeys.RmdirExit,
            FilepathDataKeys.ChmodExit,
            FilepathDataKeys.ChmodAtExit,
        });

        private bool IsFilepathSyscallExitEvent(string eventName)
        {
            return FilepathExitKeys.Contains(eventName);
        }

        private static readonly HashSet<string> RenameFileEntryKeys = new HashSet<string>(new[]
        {
            RenameFileDataKeys.RenameEntry,
            RenameFileDataKeys.RenameatEntry,
            RenameFileDataKeys.Renameat2Entry,
        });

        private bool IsRenameFileSyscallEntryEvent(string eventName)
        {
            return RenameFileEntryKeys.Contains(eventName);
        }

        private static readonly HashSet<string> RenameFileExitKeys = new HashSet<string>(new[]
        {
            RenameFileDataKeys.RenameExit,
            RenameFileDataKeys.RenameatExit,
            RenameFileDataKeys.Renameat2Exit,
        });

        private bool IsRenameFileSyscallExitEvent(string eventName)
        {
            return RenameFileExitKeys.Contains(eventName);
        }

        public LttngDiskDataCooker()
            : base(Identifier)
        {
            FilepathEntryKeys.UnionWith(OpenFileEntryKeys);
            FilepathExitKeys.UnionWith(OpenFileExitKeys);
            HashSet<string> allKeys = new HashSet<string>(DiskKeys);
            allKeys.UnionWith(FilepathEntryKeys);
            allKeys.UnionWith(FilepathExitKeys);
            allKeys.UnionWith(FileIOEntryKeys);
            allKeys.UnionWith(FileIOExitKeys);
            allKeys.UnionWith(RenameFileEntryKeys);
            allKeys.UnionWith(RenameFileExitKeys);
            allKeys.UnionWith(ExecutingThreadTracker.UsedDataKeys);
            allKeys.Add(FileHandleDataKeys.NameToHandleAtEntry);
            allKeys.Add(FileHandleDataKeys.NameToHandleAtExit);
            allKeys.Add(StatedumpDataKeys.FileDescriptor);
            allKeys.Add(StatedumpDataKeys.BlockDevice);
            this.DataKeys = new ReadOnlyHashSet<string>(allKeys);
        }

        readonly Dictionary<uint, Dictionary<ulong, Dictionary<int, DiskActivityBuilder>>> diskActivitiesInProgress =
            new Dictionary<uint, Dictionary<ulong, Dictionary<int, DiskActivityBuilder>>>();

        readonly Dictionary<uint, Dictionary<ulong, int>> rqCompleteEventsToSkip =
            new Dictionary<uint, Dictionary<ulong, int>>();

        readonly Dictionary<int, Dictionary<string, List<OngoingFilepathOperation>>> filepathSyscallsInProgress =
            new Dictionary<int, Dictionary<string, List<OngoingFilepathOperation>>>();

        Dictionary<int, Dictionary<string, int>> threadClosingEventsToSkip = new Dictionary<int, Dictionary<string, int>>();

        readonly Dictionary<long, FileDescriptorInfo> fileDescriptorInfo =
            new Dictionary<long, FileDescriptorInfo>();

        readonly Dictionary<int, Dictionary<string, List<OngoingFileDescriptorOperation>>> fileIOSyscallsInProgress =
            new Dictionary<int, Dictionary<string, List<OngoingFileDescriptorOperation>>>();

        readonly Dictionary<int, List<HandleNaming>> nameHandleSyscallsInProgress =
            new Dictionary<int, List<HandleNaming>>();

        readonly Dictionary<int, Dictionary<int, int>> nameHandleSyscallsReturnValue = new Dictionary<int, Dictionary<int, int>>();

        readonly Dictionary<int, Dictionary<long, int>> fileDescriptorsBeingUsedByThread =
            new Dictionary<int, Dictionary<long, int>>();

        readonly Dictionary<int, Dictionary<string, int>> filepathsBeingUsedByThread =
            new Dictionary<int, Dictionary<string, int>>();

        readonly Dictionary<int, Dictionary<long, List<FileRenaming>>> fileDescriptorsBeingRenamed = new Dictionary<int, Dictionary<long, List<FileRenaming>>>();

        readonly Dictionary<int, Dictionary<int, int>> renameSyscallsReturnValue = new Dictionary<int, Dictionary<int, int>>();

        readonly Dictionary<int, Dictionary<long, string>> fileHandleToPathname = new Dictionary<int, Dictionary<long, string>>();

        readonly Dictionary<uint, string> deviceIdToName = new Dictionary<uint, string>();

        private ExecutingThreadTracker threadTracker = new ExecutingThreadTracker();
        private DiscardedEventsTracker discardedEventsTracker = new DiscardedEventsTracker();

        private ICookedDataRetrieval dataRetrieval;

        private string FileDescriptorCurrentlyInUse(int tid, uint? deviceId = null)
        {
            if (this.fileDescriptorsBeingUsedByThread.TryGetValue(tid, out Dictionary<long, int> fileDescriptorsInUse))
            {
                if (fileDescriptorsInUse.Count == 1)
                {
                    long fdInUse = new List<long>(fileDescriptorsInUse.Keys)[0];
                    if (this.fileDescriptorInfo.TryGetValue(fdInUse, out FileDescriptorInfo fdInfo) &&
                       (!deviceId.HasValue || !fdInfo.DeviceId.HasValue || fdInfo.DeviceId.Value == deviceId.Value))
                    {
                        if (deviceId.HasValue && !fdInfo.DeviceId.HasValue)
                        {
                            fdInfo.DeviceId = deviceId;
                            this.fileDescriptorInfo[fdInUse] = fdInfo;
                        }
                        return fdInfo.Filepath;
                    }
                }
                else if (fileDescriptorsInUse.Count > 1 && deviceId.HasValue)
                {
                    FileDescriptorInfo candidate = new FileDescriptorInfo("", null);
                    long usedFd = 0;
                    bool candidateSet = false; //Need to use a boolean varaible because the candidate may be the empty string
                    foreach (var fileDescriptorInfo in fileDescriptorsInUse)
                    {
                        if (this.fileDescriptorInfo.TryGetValue(fileDescriptorInfo.Key, out FileDescriptorInfo fdInfo) &&
                            (!fdInfo.DeviceId.HasValue || fdInfo.DeviceId.Value == deviceId.Value) &&
                            (fdInfo.Filepath != candidate.Filepath || !candidateSet))
                        {
                            if (candidateSet)
                            {
                                return String.Empty;
                            }
                            else
                            {
                                usedFd = fileDescriptorInfo.Key;
                                candidate = fdInfo;
                                candidateSet = true;
                            }
                        }
                    }
                    if (candidateSet && !candidate.DeviceId.HasValue)
                    {
                        candidate.DeviceId = deviceId;
                        this.fileDescriptorInfo[usedFd] = candidate;
                    }
                    return candidate.Filepath;
                }
            }
            return String.Empty;
        }

        private string FilepathCurrentlyInUse(int tid)
        {
            if (this.filepathsBeingUsedByThread.TryGetValue(tid, out Dictionary<string, int> filepathInUse) && filepathInUse.Count == 1)
            {
                return new List<string>(filepathInUse.Keys)[0];
            }
            return String.Empty;
        }

        private string FileCurrentlyInUse(int tid, uint? deviceId = null)
        {
            string fdInUse = this.FileDescriptorCurrentlyInUse(tid, deviceId);
            string pathInUse = this.FilepathCurrentlyInUse(tid);
            if (fdInUse.Equals(pathInUse) || pathInUse.Length == 0)
            {
                return fdInUse;
            }
            else if (fdInUse.Length == 0)
            {
                return pathInUse;
            }
            return String.Empty;
        }

        private void AddFileDescriptorUsage(int tid, long fd)
        {
            Dictionary<long, int> lastUsedFileDescriptorsByCurrentThread;
            if (!this.fileDescriptorsBeingUsedByThread.TryGetValue(tid, out lastUsedFileDescriptorsByCurrentThread))
            {
                lastUsedFileDescriptorsByCurrentThread = new Dictionary<long, int>();
                this.fileDescriptorsBeingUsedByThread[tid] = lastUsedFileDescriptorsByCurrentThread;
            }

            if (lastUsedFileDescriptorsByCurrentThread.TryGetValue(fd, out int amountOfUses))
            {
                lastUsedFileDescriptorsByCurrentThread[fd] = amountOfUses + 1;
            }
            else
            {
                lastUsedFileDescriptorsByCurrentThread[fd] = 1;
            }
        }

        private void AddFilepathUsage(int tid, string filepath)
        {
            Dictionary<string, int> lastUsedFilepathsByCurrentThread;
            if (!this.filepathsBeingUsedByThread.TryGetValue(tid, out lastUsedFilepathsByCurrentThread))
            {
                lastUsedFilepathsByCurrentThread = new Dictionary<string, int>();
                this.filepathsBeingUsedByThread[tid] = lastUsedFilepathsByCurrentThread;
            }

            if (lastUsedFilepathsByCurrentThread.TryGetValue(filepath, out int amountOfUses))
            {
                lastUsedFilepathsByCurrentThread[filepath] = amountOfUses + 1;
            }
            else
            {
                lastUsedFilepathsByCurrentThread[filepath] = 1;
            }
        }

        private void RemoveFilepathUsage(int tid, string filepath)
        {
            if (this.filepathsBeingUsedByThread.TryGetValue(tid, out Dictionary<string, int> lastUsedFilepathByCurrentThread) &&
                lastUsedFilepathByCurrentThread.TryGetValue(filepath, out int amountOfUses))
            {
                if (amountOfUses > 1)
                {
                    lastUsedFilepathByCurrentThread[filepath] = amountOfUses - 1;
                }
                else
                {
                    lastUsedFilepathByCurrentThread.Remove(filepath);
                }
            }
        }

        private int SyscallEventsToSkip(string syscallName, int tid)
        {
            if (this.threadClosingEventsToSkip.TryGetValue(tid, out Dictionary<string, int> eventsToSkipPerEvent) &&
                eventsToSkipPerEvent.TryGetValue(syscallName, out int amountToSkip))
            {
                return amountToSkip;
            }
            return 0;
        }

        private void SetSyscallEventsToSkip(string syscallName, int tid, int amount)
        {
            Dictionary<string, int> amountToSkipPerEvent;
            if (!this.threadClosingEventsToSkip.TryGetValue(tid, out amountToSkipPerEvent))
            {
                amountToSkipPerEvent = new Dictionary<string, int>();
                this.threadClosingEventsToSkip[tid] = amountToSkipPerEvent;
            }
            amountToSkipPerEvent[syscallName] = amount;
        }

        private void SetDiskEventsToSkip(uint deviceId, ulong sectorNumber, int amount)
        {
            Dictionary<ulong, int> amountsToSkipPerSector;
            if (!this.rqCompleteEventsToSkip.TryGetValue(deviceId, out amountsToSkipPerSector))
            {
                amountsToSkipPerSector = new Dictionary<ulong, int>();
                this.rqCompleteEventsToSkip[deviceId] = amountsToSkipPerSector;
            }

            amountsToSkipPerSector[sectorNumber] = amount;
        }

        private int EventThreadId(LttngEvent data, LttngContext context)
        {
            if (data.StreamDefinedEventContext != null && data.StreamDefinedEventContext.FieldsByName.ContainsKey("_tid"))
            {
                return data.StreamDefinedEventContext.ReadFieldAsInt32("_tid");
            }
            else
            {
                return this.threadTracker.CurrentTidAsInt(context.CurrentCpu);
            }
        }

        public override string Description => "Disk activity.";

        public override ReadOnlyHashSet<string> DataKeys { get; }

        private void ProcessEventDrop(LttngEvent data, LttngContext context)
        {
            uint discardedEventsBetweenLastTwoEvents = this.discardedEventsTracker.EventsDiscardedBetweenLastTwoEvents(data, context);
            if (discardedEventsBetweenLastTwoEvents > 0)
            {
                this.threadTracker.ReportEventsDiscarded(context.CurrentCpu);
                foreach (var deviceActivities in this.diskActivitiesInProgress)
                {
                    foreach (var sectorIncompleteActivities in deviceActivities.Value)
                    {
                        this.completedActivities.AddRange(sectorIncompleteActivities.Value.Values);
                    }
                }
                this.diskActivitiesInProgress.Clear();

                foreach (var filepathSyscalls in this.filepathSyscallsInProgress)
                {
                    foreach (var incompleteSyscalls in filepathSyscalls.Value)
                    {
                        incompleteSyscalls.Value.ForEach(openSyscall => this.fileEvents.Add(new FileEvent(openSyscall.Name + " (start of syscall)", openSyscall.ThreadId, openSyscall.Filepath, getOperationSize(data), openSyscall.StartTime, openSyscall.StartTime)));
                    }
                }
                this.filepathSyscallsInProgress.Clear();

                foreach (var filepathSyscalls in this.fileIOSyscallsInProgress)
                {
                    foreach (var incompleteSyscalls in filepathSyscalls.Value)
                    {
                        incompleteSyscalls.Value.ForEach(openSyscall =>
                            {
                                string filePath = String.Empty;
                                if (this.fileDescriptorInfo.ContainsKey(openSyscall.FileDescriptor))
                                {
                                    filePath = this.fileDescriptorInfo[openSyscall.FileDescriptor].Filepath;
                                }
                                this.fileEvents.Add(new FileEvent(openSyscall.Name + " (start of syscall)", openSyscall.ThreadId, filePath, openSyscall.Size, openSyscall.StartTime, openSyscall.StartTime));
                            }
                        );
                    }
                }
                this.fileIOSyscallsInProgress.Clear();
                this.nameHandleSyscallsInProgress.Clear();
                this.rqCompleteEventsToSkip.Clear();
                this.threadClosingEventsToSkip.Clear();
                this.fileDescriptorsBeingUsedByThread.Clear();
                this.filepathsBeingUsedByThread.Clear();
                this.fileDescriptorsBeingRenamed.Clear();
                this.renameSyscallsReturnValue.Clear();
            }
        }

        public override DataProcessingResult CookDataElement(
            LttngEvent data,
            LttngContext context,
            CancellationToken cancellationToken)
        {
            this.ProcessEventDrop(data, context);
            try
            {
                this.threadTracker.ProcessEvent(data, context);    
                if (IsFileIOEntryEvent(data.Name))
                {
                    ProcessFileIOEntryEvent(data, context);
                    return DataProcessingResult.Processed;
                }
                else if (IsFileIOExitEvent(data.Name))
                {
                    ProcessFileIOExitEvent(data, context);
                    return DataProcessingResult.Processed;
                }
                else if (IsFilepathSyscallEntryEvent(data.Name))
                {
                    ProcessFilepathSyscallEntryEvent(data, context);
                    return DataProcessingResult.Processed;
                }
                else if (IsFilepathSyscallExitEvent(data.Name))
                {
                    ProcessFilepathSyscallExitEvent(data, context);
                    return DataProcessingResult.Processed;
                }
                else if (IsDiskEvent(data.Name))
                {
                    ProcessDiskEvent(data);
                    return DataProcessingResult.Processed;
                }
                else if (StatedumpDataKeys.FileDescriptor.Equals(data.Name))
                {
                    ProcessFDStatedump(data);
                    return DataProcessingResult.Processed;
                }
                else if (StatedumpDataKeys.BlockDevice.Equals(data.Name))
                {
                    ProcessDeviceStatedump(data);
                    return DataProcessingResult.Processed;
                }
                else if (IsRenameFileSyscallEntryEvent(data.Name))
                {
                    ProcessRenameFileEntryEvent(data, context);
                    return DataProcessingResult.Processed;
                }
                else if (IsRenameFileSyscallExitEvent(data.Name))
                {
                    ProcessRenameFileExitEvent(data, context);
                    return DataProcessingResult.Processed;
                }
                else if (FileHandleDataKeys.NameToHandleAtEntry.Equals(data.Name))
                {
                    ProcessNameToHandleAtEntryEvent(data, context);
                    return DataProcessingResult.Processed;
                }
                else if (FileHandleDataKeys.NameToHandleAtExit.Equals(data.Name))
                {
                    ProcessNameToHandleAtExitEvent(data, context);
                    return DataProcessingResult.Processed;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return DataProcessingResult.CorruptData;
            }
            return DataProcessingResult.Ignored;
        }

        public override void BeginDataCooking(ICookedDataRetrieval dependencyRetrieval, CancellationToken cancellationToken)
        {
            this.dataRetrieval = dependencyRetrieval;
        }

        public override void EndDataCooking(CancellationToken cancellationToken)
        {
            foreach (var deviceActivities in this.diskActivitiesInProgress)
            {
                foreach (var sectorIncompleteActivities in deviceActivities.Value)
                {
                    this.completedActivities.AddRange(sectorIncompleteActivities.Value.Values);
                }
            }

            this.completedActivities.ForEach(a => this.SetDeviceName(a));

            this.deviceIdToName.Clear();
            this.diskActivitiesInProgress.Clear();
            this.rqCompleteEventsToSkip.Clear();
            this.filepathSyscallsInProgress.Clear();
            this.threadClosingEventsToSkip.Clear();
            this.fileIOSyscallsInProgress.Clear();
            this.fileDescriptorInfo.Clear();
            this.nameHandleSyscallsInProgress.Clear();
            this.nameHandleSyscallsReturnValue.Clear();
            this.fileDescriptorsBeingUsedByThread.Clear();
            this.filepathsBeingUsedByThread.Clear();
            this.fileDescriptorsBeingRenamed.Clear();
            this.renameSyscallsReturnValue.Clear();
            this.fileHandleToPathname.Clear();

            IThreadTracker threadTracker = this.dataRetrieval.QueryOutput<IThreadTracker>(
                new DataOutputPath(LttngThreadDataCooker.DataCookerPath, "ThreadTracker"));

            completedActivities.ForEach(activity =>
            {
                if (activity.InsertTime.HasValue)
                {
                    activity.SetThreadInfo(threadTracker.QueryInfo(activity.ThreadId, activity.InsertTime.Value));
                }
                else if (activity.IssueTime.HasValue)
                {
                    activity.SetThreadInfo(threadTracker.QueryInfo(activity.ThreadId, activity.IssueTime.Value));
                }
                else if (activity.CompleteTime.HasValue)
                {
                    activity.SetThreadInfo(threadTracker.QueryInfo(activity.ThreadId, activity.CompleteTime.Value));
                }
            }
            );

            completedActivities.ForEach(activityBuilder => this.builtDiskActivities.Add(activityBuilder.Build()));

            completedActivities.Clear();

            this.fileEvents.ForEach(x => x.SetThreadInfo(threadTracker.QueryInfo(x.ThreadId, x.StartTime)));
        }

        private static readonly HashSet<DataCookerPath> RequiredPaths = new HashSet<DataCookerPath>
        {
            LttngThreadDataCooker.DataCookerPath
        };

        public override IReadOnlyCollection<DataCookerPath> RequiredDataCookers => RequiredPaths;

        public override IReadOnlyDictionary<DataCookerPath, DataCookerDependencyType> DependencyTypes => dependencyTypes;

        private static readonly Dictionary<DataCookerPath, DataCookerDependencyType> dependencyTypes = new Dictionary<DataCookerPath, DataCookerDependencyType>
        {
            { LttngThreadDataCooker.DataCookerPath, DataCookerDependencyType.AsConsumed }
        };


        [DataOutput]
        public IReadOnlyList<DiskActivity> DiskActivity => this.builtDiskActivities;

        [DataOutput]
        public IReadOnlyList<FileEvent> FileEvents => this.fileEvents;

        private readonly List<FileEvent> fileEvents = new List<FileEvent>();

        private readonly List<DiskActivityBuilder> completedActivities = new List<DiskActivityBuilder>();

        private readonly List<DiskActivity> builtDiskActivities = new List<DiskActivity>();

        void ProcessDiskEvent(LttngEvent data)
        {
            switch (data.Name)
            {
                case DiskDataKeys.Complete:
                    ProcessComplete(data);
                    break;
                case DiskDataKeys.Insert:
                    ProcessInsert(data);
                    break;
                case DiskDataKeys.Issue:
                    ProcessIssue(data);
                    break;
            }
        }

        private void SetDeviceName(DiskActivityBuilder activity)
        {
            if (this.deviceIdToName.ContainsKey(activity.DeviceId))
            {
                activity.DeviceName = this.deviceIdToName[activity.DeviceId];
            }
        }

        void ProcessFilepathSyscallEntryEvent(LttngEvent data, LttngContext context)
        {
            int eventTid = this.EventThreadId(data, context);
            string syscallName = SyscallEvent.EventNameToSyscallName(data.Name);
            int amountToSkip = this.SyscallEventsToSkip(syscallName, eventTid);

            if (amountToSkip > 0)
            {
                this.threadClosingEventsToSkip[eventTid][syscallName] = amountToSkip + 1;
                return;
            }

            string newEntryFilepath = String.Empty;
            if (data.Payload.FieldsByName.ContainsKey("_pathname"))
            {
                newEntryFilepath = data.Payload.FieldsByName["_pathname"].GetValueAsString();
            }
            else if (data.Payload.FieldsByName.ContainsKey("_filename"))
            {
                newEntryFilepath = data.Payload.FieldsByName["_filename"].GetValueAsString();
            }
            else if (data.Payload.FieldsByName.ContainsKey("_name"))
            {
                newEntryFilepath = data.Payload.FieldsByName["_name"].GetValueAsString();
            }
            else if (data.Payload.FieldsByName.ContainsKey("_path"))
            {
                newEntryFilepath = data.Payload.FieldsByName["_path"].GetValueAsString();
            }
            else if (data.Payload.FieldsByName.ContainsKey("_handle"))
            {
                long fileHandle = data.Payload.ReadFieldAsInt64("_handle");
                if (this.fileHandleToPathname.TryGetValue(eventTid, out Dictionary<long, string> handles) &&
                    handles.TryGetValue(fileHandle, out string pathname))
                {
                    newEntryFilepath = pathname;
                }
            }

            Dictionary<string, List<OngoingFilepathOperation>> ongoingSyscallsOfCurrentThread;
            if (!this.filepathSyscallsInProgress.TryGetValue(eventTid, out ongoingSyscallsOfCurrentThread))
            {
                ongoingSyscallsOfCurrentThread = new Dictionary<string, List<OngoingFilepathOperation>>();
                this.filepathSyscallsInProgress[eventTid] = ongoingSyscallsOfCurrentThread;
            }

            List<OngoingFilepathOperation> openSyscallsWithSpecifiedName;
            if (!ongoingSyscallsOfCurrentThread.TryGetValue(syscallName, out openSyscallsWithSpecifiedName))
            {
                openSyscallsWithSpecifiedName = new List<OngoingFilepathOperation>();
                ongoingSyscallsOfCurrentThread[syscallName] = openSyscallsWithSpecifiedName;
            }

            openSyscallsWithSpecifiedName.Add(new OngoingFilepathOperation(data, newEntryFilepath, eventTid));
            this.AddFilepathUsage(eventTid, newEntryFilepath);
        }

        void ProcessFilepathSyscallExitEvent(LttngEvent data, LttngContext context)
        {
            int eventTid = this.EventThreadId(data, context);
            string syscallName = SyscallEvent.EventNameToSyscallName(data.Name);
            int amountToSkip = this.SyscallEventsToSkip(syscallName, eventTid);
            long returnedValue = data.Payload.ReadFieldAsInt64("_ret");

            if (amountToSkip > 0)
            {
                if (returnedValue != -1 && this.IsOpenFileSyscallExitEvent(data.Name) && this.fileDescriptorInfo.ContainsKey(returnedValue))
                {
                    this.fileDescriptorInfo.Remove(returnedValue);
                }

                if (amountToSkip > 1)
                {
                    this.threadClosingEventsToSkip[eventTid][syscallName] = amountToSkip - 1;
                    this.fileEvents.Add(new FileEvent(syscallName + " (end of syscall)", eventTid, this.FileCurrentlyInUse(eventTid), getOperationSize(data), data.Timestamp, data.Timestamp));
                }
                else if (amountToSkip == 1)
                {
                    this.fileEvents.Add(new FileEvent(syscallName + " (end of syscall)", eventTid, this.FileCurrentlyInUse(eventTid), getOperationSize(data), data.Timestamp, data.Timestamp));
                    if (this.filepathsBeingUsedByThread.TryGetValue(eventTid, out Dictionary<string, int> usedFilepaths))
                    {
                        usedFilepaths.Clear();
                    }
                    this.threadClosingEventsToSkip[eventTid].Remove(syscallName);
                }

                return;
            }

            if (this.filepathSyscallsInProgress.TryGetValue(eventTid, out Dictionary<string, List<OngoingFilepathOperation>> ongoingSyscallsOfCurrentThread) &&
                ongoingSyscallsOfCurrentThread.TryGetValue(syscallName, out List<OngoingFilepathOperation> openSyscallsWithSpecifiedName))
            {
                if (openSyscallsWithSpecifiedName.Count == 1)
                {
                    OngoingFilepathOperation exitingOperation = openSyscallsWithSpecifiedName[0];
                    if (returnedValue != -1 && this.IsOpenFileSyscallExitEvent(data.Name))
                    {
                        this.fileDescriptorInfo[returnedValue] = new FileDescriptorInfo(exitingOperation.Filepath, null);
                    }
                    this.fileEvents.Add(new FileEvent(syscallName, exitingOperation.ThreadId, exitingOperation.Filepath, this.getOperationSize(data), exitingOperation.StartTime, data.Timestamp));
                    this.RemoveFilepathUsage(eventTid, exitingOperation.Filepath);
                }
                else if (openSyscallsWithSpecifiedName.Count > 1)
                {
                    if (returnedValue != -1 && this.IsOpenFileSyscallExitEvent(data.Name) && this.fileDescriptorInfo.ContainsKey(returnedValue))
                    {
                        this.fileDescriptorInfo.Remove(returnedValue);
                    }
                    SetSyscallEventsToSkip(syscallName, eventTid, openSyscallsWithSpecifiedName.Count - 1);
                    this.fileEvents.Add(new FileEvent(syscallName + " (end of syscall)", eventTid, this.FileCurrentlyInUse(eventTid), this.getOperationSize(data), data.Timestamp, data.Timestamp));
                    openSyscallsWithSpecifiedName.ForEach(openSyscall => this.fileEvents.Add(new FileEvent(syscallName + " (start of syscall)", openSyscall.ThreadId, openSyscall.Filepath, this.getOperationSize(data), openSyscall.StartTime, openSyscall.StartTime)));
                }

                openSyscallsWithSpecifiedName.Clear();
            }
        }

        long getOperationSize(LttngEvent data)
        {
            switch (data.Name)
            {
                case FileIOEntryDataKeys.FTruncateEntry:
                    return data.Payload.ReadFieldAsInt64("_length");
                case FileIOEntryDataKeys.FAllocateEntry:
                case FileIOEntryDataKeys.FAdvise64Entry:
                    return data.Payload.ReadFieldAsInt64("_len");
                case FileIOEntryDataKeys.ReadEntry:
                case FileIOEntryDataKeys.PReadEntry:
                case FileIOEntryDataKeys.WriteEntry:
                case FileIOEntryDataKeys.PWriteEntry:
                case FileIOEntryDataKeys.SendFileEntry:
                    return data.Payload.ReadFieldAsInt64("_count");
                case FileIOEntryDataKeys.SyncFileRangeEntry:
                    return data.Payload.ReadFieldAsInt64("_nbytes");
                case FileIOEntryDataKeys.FChdirEntry:
                case FilepathDataKeys.ChmodExit:
                case FilepathDataKeys.ChmodAtExit:
                    return 4;
                default:
                    return 0;
            }
        }

        void ProcessFileIOEntryEvent(LttngEvent data, LttngContext context)
        {
            if (data.Payload.FieldsByName.ContainsKey("_fd"))
            {
                long usedFileDescriptor = data.Payload.ReadFieldAsInt64("_fd");
                string syscallName = SyscallEvent.EventNameToSyscallName(data.Name);
                int eventTid = this.EventThreadId(data, context);
                
                this.AddFileDescriptorUsage(eventTid, usedFileDescriptor);

                int amountToSkip = this.SyscallEventsToSkip(syscallName, eventTid);

                if (amountToSkip > 0)
                {
                    this.fileEvents.Add(new FileEvent(syscallName + " (start of syscall)", eventTid, this.FileCurrentlyInUse(eventTid), this.getOperationSize(data), data.Timestamp, data.Timestamp));
                    this.threadClosingEventsToSkip[eventTid][syscallName] = amountToSkip + 1;
                    return;
                }

                Dictionary<string, List<OngoingFileDescriptorOperation>> ongoingSyscallsOfCurrentThread;
                if (!this.fileIOSyscallsInProgress.TryGetValue(eventTid, out ongoingSyscallsOfCurrentThread))
                {
                    ongoingSyscallsOfCurrentThread = new Dictionary<string, List<OngoingFileDescriptorOperation>>();
                    this.fileIOSyscallsInProgress[eventTid] = ongoingSyscallsOfCurrentThread;
                }

                List<OngoingFileDescriptorOperation> openSyscallsWithSpecifiedName;
                if (!ongoingSyscallsOfCurrentThread.TryGetValue(syscallName, out openSyscallsWithSpecifiedName))
                {
                    openSyscallsWithSpecifiedName = new List<OngoingFileDescriptorOperation>();
                    ongoingSyscallsOfCurrentThread[syscallName] = openSyscallsWithSpecifiedName;
                }

                openSyscallsWithSpecifiedName.Add(new OngoingFileDescriptorOperation(data, usedFileDescriptor, eventTid, this.getOperationSize(data)));
            }
        }

        void ProcessFileIOExitEvent(LttngEvent data, LttngContext context)
        {
            string syscallName = SyscallEvent.EventNameToSyscallName(data.Name);
            int eventTid = this.EventThreadId(data, context);

            int amountToSkip = this.SyscallEventsToSkip(syscallName, eventTid);

            if (amountToSkip > 1)
            {
                this.threadClosingEventsToSkip[eventTid][syscallName] = amountToSkip - 1;
                this.fileEvents.Add(new FileEvent(syscallName + " (end of syscall)", eventTid, this.FileCurrentlyInUse(eventTid), 0, data.Timestamp, data.Timestamp));
                return;
            }
            else if (amountToSkip == 1)
            {
                this.fileEvents.Add(new FileEvent(syscallName + " (end of syscall)", eventTid, this.FileCurrentlyInUse(eventTid), 0, data.Timestamp, data.Timestamp));
                if (this.fileDescriptorsBeingUsedByThread.TryGetValue(eventTid, out Dictionary<long, int> usedFileDescriptors))
                {
                    usedFileDescriptors.Clear();
                    ///this.fileDescriptorsBeingUsedByThread[eventTid] = usedFileDescriptors;
                }
                this.threadClosingEventsToSkip[eventTid].Remove(syscallName);
                return;
            }

            if (this.fileIOSyscallsInProgress.TryGetValue(eventTid, out Dictionary<string, List<OngoingFileDescriptorOperation>> ongoingSyscallsOfCurrentThread) &&
                ongoingSyscallsOfCurrentThread.TryGetValue(syscallName, out List<OngoingFileDescriptorOperation> openSyscallsWithSpecifiedName))
            {
                if (openSyscallsWithSpecifiedName.Count > 1)
                {
                    SetSyscallEventsToSkip(syscallName, eventTid, openSyscallsWithSpecifiedName.Count - 1);
                    this.fileEvents.Add(new FileEvent(syscallName + " (end of syscall)", eventTid, this.FileCurrentlyInUse(eventTid), 0, data.Timestamp, data.Timestamp));
                    openSyscallsWithSpecifiedName.ForEach(openSyscall =>
                        {
                            string filePath = String.Empty;
                            if (this.fileDescriptorInfo.ContainsKey(openSyscall.FileDescriptor))
                            {
                                filePath = this.fileDescriptorInfo[openSyscall.FileDescriptor].Filepath;
                            }
                            this.fileEvents.Add(new FileEvent(syscallName + " (start of syscall)", openSyscall.ThreadId, filePath, openSyscall.Size, openSyscall.StartTime, openSyscall.StartTime));
                        }
                    );
                }
                if (openSyscallsWithSpecifiedName.Count == 1)
                {
                    OngoingFileDescriptorOperation exitingOperation = openSyscallsWithSpecifiedName[0];

                    if (this.fileDescriptorsBeingUsedByThread.TryGetValue(eventTid, out Dictionary<long, int> lastUsedFileDescriptorsByCurrentThread) &&
                        lastUsedFileDescriptorsByCurrentThread.TryGetValue(exitingOperation.FileDescriptor, out int amountOfUses)
                        )
                    {
                        if (amountOfUses > 1)
                        {
                            lastUsedFileDescriptorsByCurrentThread[exitingOperation.FileDescriptor] = amountOfUses - 1;
                        }
                        else
                        {
                            lastUsedFileDescriptorsByCurrentThread.Remove(exitingOperation.FileDescriptor);
                        }
                    }

                    long operationSize = exitingOperation.Size;
                    long returnValue = data.Payload.ReadFieldAsInt64("_ret");
                    if (returnValue < 0)
                    {
                        operationSize = 0;
                    }
                    else if (data.Name != FileIOExitDataKeys.LSeekExit &&
                             data.Name != FileIOExitDataKeys.FTruncateExit &&
                             data.Name != FileIOExitDataKeys.FAllocateExit)
                    {
                        operationSize = returnValue;
                    }

                    string filePath = String.Empty;
                    if (this.fileDescriptorInfo.ContainsKey(exitingOperation.FileDescriptor))
                    {
                        filePath = this.fileDescriptorInfo[exitingOperation.FileDescriptor].Filepath;
                    }
                    this.fileEvents.Add(new FileEvent(syscallName, exitingOperation.ThreadId, filePath, operationSize, exitingOperation.StartTime, data.Timestamp));
                }

                openSyscallsWithSpecifiedName.Clear();
            }
        }

        void ProcessComplete(LttngEvent data)
        {
            CompletedRequestUserData parsed = CompletedRequestUserData.Read(data.Payload);

            if (this.rqCompleteEventsToSkip.TryGetValue(parsed.Dev, out Dictionary<ulong, int> sectorAmountsToSkip) &&
                sectorAmountsToSkip.TryGetValue(parsed.Sector, out int amountToSkip) && amountToSkip > 0)
            {
                sectorAmountsToSkip[parsed.Sector] = amountToSkip - 1;
                return;
            }

            if (this.diskActivitiesInProgress.TryGetValue(parsed.Dev, out Dictionary<ulong, Dictionary<int, DiskActivityBuilder>> deviceActivities) &&
                deviceActivities.TryGetValue(parsed.Sector, out Dictionary<int, DiskActivityBuilder> sectorActivities) && sectorActivities.Count > 0)
            {
                if (sectorActivities.Count == 1)
                {
                    DiskActivityBuilder diskActivity = new List<DiskActivityBuilder>(sectorActivities.Values)[0];
                    diskActivity.CompleteTime = data.Timestamp;
                    diskActivity.Error = parsed.Error;
                    if (diskActivity.Filepath.Length == 0)
                    {
                        diskActivity.Filepath = this.FileCurrentlyInUse(diskActivity.ThreadId, diskActivity.DeviceId);
                    }
                    this.completedActivities.Add(diskActivity);
                }
                else
                {
                    foreach (var activity in sectorActivities)
                    {
                        this.completedActivities.Add(activity.Value);
                    }
                    this.SetDiskEventsToSkip(parsed.Dev, parsed.Sector, sectorActivities.Count - 1);
                }

                sectorActivities.Clear();
            }
            else
            {
                this.completedActivities.Add(new DiskActivityBuilder(parsed.Dev, parsed.Sector, "", -1));
            }
        }

        void ProcessInsert(LttngEvent data)
        {
            DiskActivityBuilder diskActivity = GetOrAddDiskActivity(UnsentRequestData.Read(data.Payload));
            diskActivity.InsertTime = data.Timestamp;
        }

        void ProcessIssue(LttngEvent data)
        {
            UnsentRequestData parsed = UnsentRequestData.Read(data.Payload);
            DiskActivityBuilder diskActivity = GetOrAddDiskActivity(parsed, false);
            diskActivity.IssueTime = data.Timestamp;

            if (this.rqCompleteEventsToSkip.TryGetValue(parsed.Dev, out Dictionary<ulong, int> sectorAmountsToSkip) &&
                sectorAmountsToSkip.TryGetValue(parsed.Sector, out int amountToSkip) && amountToSkip > 0)
            {
                this.completedActivities.Add(diskActivity);
                this.diskActivitiesInProgress[parsed.Dev][parsed.Sector].Remove(parsed.Tid);
                sectorAmountsToSkip[parsed.Sector] = amountToSkip + 1;
            }
            else if (diskActivity.Size != DataSize.FromBytes(parsed.Bytes))
            {
                throw new InvalidOperationException("Disk activity sizes do not match.");
            }
        }

        void ProcessFDStatedump(LttngEvent data)
        {
            int fd = data.Payload.ReadFieldAsInt32("_fd");
            string filename = data.Payload.FieldsByName["_filename"].GetValueAsString();
            FileDescriptorInfo fdInfo;
            if (!this.fileDescriptorInfo.TryGetValue(fd, out fdInfo))
            {
                fdInfo = new FileDescriptorInfo(filename, null);
            }
            fdInfo.Filepath = filename;
            this.fileDescriptorInfo[fd] = fdInfo;
        }

        void ProcessDeviceStatedump(LttngEvent data)
        {
            uint diskId = data.Payload.ReadFieldAsUInt32("_dev");
            if (!this.deviceIdToName.ContainsKey(diskId))
            {
                this.deviceIdToName[diskId] = data.Payload.FieldsByName["_diskname"].GetValueAsString();
            }
        }

        DiskActivityBuilder GetOrAddDiskActivity(UnsentRequestData requestData, bool isInsert = true)
        {
            Dictionary<ulong, Dictionary<int, DiskActivityBuilder>> deviceActivities;
            if (!this.diskActivitiesInProgress.TryGetValue(requestData.Dev, out deviceActivities))
            {
                deviceActivities = new Dictionary<ulong, Dictionary<int, DiskActivityBuilder>>();
                this.diskActivitiesInProgress[requestData.Dev] = deviceActivities;
            }

            Dictionary<int, DiskActivityBuilder> sectorActivities;
            if (!deviceActivities.TryGetValue(requestData.Sector, out sectorActivities))
            {
                sectorActivities = new Dictionary<int, DiskActivityBuilder>();
                deviceActivities[requestData.Sector] = sectorActivities;
            }

            string filepath = this.FileCurrentlyInUse(requestData.Tid, requestData.Dev);

            DiskActivityBuilder diskActivity;
            if (sectorActivities.TryGetValue(requestData.Tid, out diskActivity))
            {
                if (isInsert || diskActivity.IssueTime.HasValue || diskActivity.CompleteTime.HasValue)
                {
                    ///If it is from a Previous activity, we complete the old one and set up a new one
                    this.completedActivities.Add(diskActivity);
                    sectorActivities.Remove(requestData.Tid);
                }
                else
                {
                    ///If we continue the same activity, we return it
                    if (diskActivity.Filepath.Length == 0 && filepath.Length > 0)
                    {
                        diskActivity.Filepath = filepath;
                    }
                    return diskActivity;
                }
            }

            diskActivity = new DiskActivityBuilder(requestData.Dev, requestData.Sector, filepath, requestData.Tid);
            diskActivity.Size = DataSize.FromBytes(requestData.Bytes);
            sectorActivities[requestData.Tid] = diskActivity;

            return diskActivity;
        }

        void ProcessRenameFileEntryEvent(LttngEvent data, LttngContext context)
        {
            string oldName = data.Payload.FieldsByName["_oldname"].GetValueAsString();
            string newName = data.Payload.FieldsByName["_newname"].GetValueAsString();
            int eventTid = this.EventThreadId(data, context);

            this.AddFilepathUsage(eventTid, oldName);

            var keys = new List<long>(this.fileDescriptorInfo.Keys);
            keys.ForEach(key =>
                {
                    var fd = this.fileDescriptorInfo[key];
                    string path = fd.Filepath;
                    if (path.Equals(oldName) ||  ///same name
                        ((path.EndsWith(")") && path.Length > oldName.Length) && /// or is a pending rename and
                        (path.Substring(path.Length - oldName.Length - 1, oldName.Length).Equals(oldName) || /// failed to confirm that it was renamed or
                        ((path[oldName.Length] == ' ' && path.StartsWith(oldName)))))) ///failed to confirm that it was not renamed
                    {
                        StringBuilder nameBuilder = new StringBuilder(oldName);
                        nameBuilder.Append(" (maybe renamed to ");
                        nameBuilder.Append(newName);
                        nameBuilder.Append(')');

                        FileRenaming rename = new FileRenaming(oldName, newName, data.Timestamp);

                        if (this.fileDescriptorsBeingRenamed.TryGetValue(eventTid, out Dictionary<long, List<FileRenaming>> fileDescriptorsCount))
                        {
                            if (fileDescriptorsCount.TryGetValue(key, out List<FileRenaming> renamesList))
                            {
                                renamesList.Add(rename);
                                this.fileDescriptorsBeingRenamed[eventTid][key] = renamesList;
                            }
                            else
                            {
                                this.fileDescriptorsBeingRenamed[eventTid][key] = new List<FileRenaming>() { rename };
                            }
                        }
                        else
                        {
                            this.fileDescriptorsBeingRenamed[eventTid] = new Dictionary<long, List<FileRenaming>>() { { key, new List<FileRenaming>() { rename } } };
                        }

                        this.fileDescriptorInfo[key] = new FileDescriptorInfo(nameBuilder.ToString(), fd.DeviceId);
                    }
                }
            );
        }

        Dictionary<int, int> AddExitCodeAndReturnAllExitCodes(Dictionary<int, Dictionary<int, int>> returnValuesPerThread, int threadId, int exitCode)
        {
            if (!returnValuesPerThread.TryGetValue(threadId, out Dictionary<int, int> returnTimesPerExitCode))
            {
                returnTimesPerExitCode = new Dictionary<int, int>() { { exitCode, 1 } };
                returnValuesPerThread[threadId] = returnTimesPerExitCode;
            }
            else if (returnTimesPerExitCode.TryGetValue(exitCode, out int timesReturned))
            {
                returnTimesPerExitCode[exitCode] = timesReturned + 1;
            }
            else
            {
                returnTimesPerExitCode[exitCode] = 1;
            }

            return returnTimesPerExitCode;
        }

        void ProcessRenameFileExitEvent(LttngEvent data, LttngContext context)
        {
            int eventTid = this.EventThreadId(data, context);

            if (this.fileDescriptorsBeingRenamed.TryGetValue(eventTid, out Dictionary<long, List<FileRenaming>> fileDescriptorsBeingRenamed) &&
                fileDescriptorsBeingRenamed.Count > 0)
            {
                var returnTimesPerExitCode = AddExitCodeAndReturnAllExitCodes(this.renameSyscallsReturnValue, eventTid, data.Payload.ReadFieldAsInt32("_ret"));

                int pendingRenameOperations = 0;
                foreach (var fd in fileDescriptorsBeingRenamed)
                {
                    pendingRenameOperations += fd.Value.Count;
                }

                int finishedOperations = 0;
                foreach (var syscallReturn in returnTimesPerExitCode)
                {
                    finishedOperations += syscallReturn.Value;
                }

                if (finishedOperations == pendingRenameOperations)
                {
                    string syscallName = SyscallEvent.EventNameToSyscallName(data.Name);
                    if (!returnTimesPerExitCode.ContainsKey(0))
                    {
                        /// all operations were unsuccessful
                        foreach (var fd in fileDescriptorsBeingRenamed)
                        {
                            if (this.fileDescriptorInfo.TryGetValue(fd.Key, out FileDescriptorInfo fdinfo))
                            {
                                string newFilename = fd.Value[fd.Value.Count - 1].newName;
                                string oldFilename = fd.Value[fd.Value.Count - 1].oldName;
                                this.fileDescriptorInfo[fd.Key] = new FileDescriptorInfo(oldFilename, fdinfo.DeviceId);
                                this.fileEvents.Add(new FileEvent(syscallName, eventTid, $"{oldFilename} failed to be renamed to {newFilename}", 0, fd.Value[fd.Value.Count - 1].startTimestamp, data.Timestamp));
                                this.RemoveFilepathUsage(eventTid, oldFilename);
                            }
                        }
                    }
                    else if (returnTimesPerExitCode.Count == 1)
                    {
                        /// all operations were successful
                        foreach (var fd in fileDescriptorsBeingRenamed)
                        {
                            if (!this.filepathsBeingUsedByThread.TryGetValue(eventTid, out Dictionary<string, int> lastUsedFilepathByCurrentThread))
                            {
                                lastUsedFilepathByCurrentThread = new Dictionary<string, int>();
                            }
                            if (this.fileDescriptorInfo.TryGetValue(fd.Key, out FileDescriptorInfo fdinfo))
                            {
                                string oldFilename = fd.Value[fd.Value.Count - 1].oldName;
                                string newFilename = fd.Value[fd.Value.Count - 1].newName;
                                this.fileDescriptorInfo[fd.Key] = new FileDescriptorInfo(newFilename, fdinfo.DeviceId);
                                this.fileEvents.Add(new FileEvent(syscallName, eventTid, $"{oldFilename} renamed to {newFilename}", Math.Max(oldFilename.Length, newFilename.Length) + 1, fd.Value[fd.Value.Count - 1].startTimestamp, data.Timestamp));
                                if (lastUsedFilepathByCurrentThread.ContainsKey(oldFilename))
                                {
                                    ///If the rename succeded, the file was not being used
                                    lastUsedFilepathByCurrentThread.Remove(oldFilename);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var fd in fileDescriptorsBeingRenamed)
                        {
                            if (this.fileDescriptorInfo.TryGetValue(fd.Key, out FileDescriptorInfo fdinfo))
                            {
                                string newFilename = fd.Value[fd.Value.Count - 1].newName;
                                string oldFilename = fd.Value[fd.Value.Count - 1].oldName;
                                this.fileEvents.Add(new FileEvent(syscallName, eventTid, $"{oldFilename} may have been renamed to {newFilename}", 0, fd.Value[fd.Value.Count - 1].startTimestamp, data.Timestamp));
                                this.RemoveFilepathUsage(eventTid, oldFilename);
                            }
                        }
                    }

                    fileDescriptorsBeingRenamed.Clear();
                    returnTimesPerExitCode.Clear();
                }
            }
        }

        void ProcessNameToHandleAtEntryEvent(LttngEvent data, LttngContext context)
        {
            int eventTid = this.EventThreadId(data, context);

            List<HandleNaming> namingsInProgress;
            if (!this.nameHandleSyscallsInProgress.TryGetValue(eventTid, out namingsInProgress))
            {
                namingsInProgress = new List<HandleNaming>();
                this.nameHandleSyscallsInProgress[eventTid] = namingsInProgress;
            }

            long handle = data.Payload.ReadFieldAsInt64("_handle");
            string pathname = data.Payload.FieldsByName["_name"].GetValueAsString();
            namingsInProgress.Add(new HandleNaming(handle, pathname, data.Timestamp));
        }

        void ProcessNameToHandleAtExitEvent(LttngEvent data, LttngContext context)
        {
            int eventTid = this.EventThreadId(data, context);
            int exitCode = data.Payload.ReadFieldAsInt32("_ret");

            if (this.nameHandleSyscallsInProgress.TryGetValue(eventTid, out List<HandleNaming> ongoingNamings) &&
                ongoingNamings.Count > 0)
            {
                var returnTimesPerExitCode = AddExitCodeAndReturnAllExitCodes(this.nameHandleSyscallsReturnValue, eventTid, exitCode);

                int finishedOperations = 0;
                foreach (var syscallReturn in returnTimesPerExitCode)
                {
                    finishedOperations += syscallReturn.Value;
                }

                if (finishedOperations == ongoingNamings.Count)
                {
                    string syscallName = SyscallEvent.EventNameToSyscallName(data.Name);

                    if (returnTimesPerExitCode.Count == 1 && returnTimesPerExitCode.ContainsKey(0))
                    {
                        Dictionary<long, string> handleTranslations;
                        if (!this.fileHandleToPathname.TryGetValue(eventTid, out handleTranslations))
                        {
                            handleTranslations = new Dictionary<long, string>();
                        }

                        for (int i = 0; i < ongoingNamings.Count; ++i)
                        {
                            handleTranslations[ongoingNamings[i].handle] = ongoingNamings[i].pathname;
                        }
                    }

                    for (int i = 0; i < ongoingNamings.Count; ++i)
                    {
                        this.fileEvents.Add(new FileEvent(syscallName, eventTid, ongoingNamings[i].pathname, 0, ongoingNamings[i].startTimestamp, data.Timestamp));
                    }

                    ongoingNamings.Clear();
                    returnTimesPerExitCode.Clear();
                }
            }
        }

        abstract class OngoingFileOperation
        {
            public string Name;
            public Timestamp StartTime;
            public int ThreadId;

            public OngoingFileOperation(LttngEvent data, int threadId)
            {
                this.Name = data.Name;
                this.StartTime = data.Timestamp;
                this.ThreadId = threadId;
            }
        }

        class OngoingFilepathOperation
            : OngoingFileOperation
        {
            public string Filepath;

            public OngoingFilepathOperation(LttngEvent data, string filepath, int threadId)
                : base(data, threadId)
            {
                this.Filepath = filepath;
            }
        };

        class OngoingFileDescriptorOperation
            : OngoingFileOperation
        {
            public long FileDescriptor;
            public long Size;

            public OngoingFileDescriptorOperation(LttngEvent data, long fileDescriptor, int threadId, long size)
                : base(data, threadId)
            {
                this.FileDescriptor = fileDescriptor;
                this.Size = size;
            }
        };

        public class CompletedFileOperation
        {
            public string Name;
            public Timestamp StartTime;
            public Timestamp EndTime;
            public string Filepath;
            public long Size;
        };

        class CompletedRequestUserData
        {
            public ulong Sector;
            public uint Dev;
            public int Error;
            ///public uint NRSector;
            ///public uint RWBS;

            public static CompletedRequestUserData Read(CtfStructValue data)
            {
                int error = 0;
                if (data.FieldsByName.ContainsKey("_error"))
                {
                    error = data.ReadFieldAsInt32("_error");
                }
                else if (data.FieldsByName.ContainsKey("_errors"))
                {
                    error = data.ReadFieldAsInt32("_errors");
                }
                return new CompletedRequestUserData
                {
                    Dev = data.ReadFieldAsUInt32("_dev"),
                    Sector = data.ReadFieldAsUInt64("_sector"),
                    Error = error
                    ///NRSector = data.ReadFieldAsUInt32("_nr_sector"),
                    ///RWBS = data.ReadFieldAsUInt32("_rwbs")
                };
            }
        }

        class UnsentRequestData
        {
            public ulong Sector;
            public uint Dev;
            public uint Bytes;
            public int Tid;

            public static UnsentRequestData Read(CtfStructValue data)
            {
                return new UnsentRequestData
                {
                    Dev = data.ReadFieldAsUInt32("_dev"),
                    Sector = data.ReadFieldAsUInt64("_sector"),
                    Bytes = data.ReadFieldAsUInt32("_bytes"),
                    Tid = data.ReadFieldAsInt32("_tid")
                };
            }
        }

        struct FileRenaming
        {
            public string oldName;
            public string newName;
            public Timestamp startTimestamp;

            public FileRenaming(string oldName, string newName, Timestamp startTimestamp)
            {
                this.oldName = oldName;
                this.newName = newName;
                this.startTimestamp = startTimestamp;
            }
        }
        struct HandleNaming
        {
            public long handle;
            public string pathname;
            public Timestamp startTimestamp;

            public HandleNaming(long handle, string pathname, Timestamp startTimestamp)
            {
                this.handle = handle;
                this.pathname = pathname;
                this.startTimestamp = startTimestamp;
            }
        }
    }
}