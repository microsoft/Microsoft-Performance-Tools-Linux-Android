// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using LttngDataExtensions.DataOutputTypes;
using LttngDataExtensions.SourceDataCookers.Thread;
using Microsoft.Performance.SDK;

namespace LttngDataExtensions.SourceDataCookers.Disk
{
    public class FileEvent
        : IFileEvent
    {
        readonly string name;
        readonly string filepath;
        readonly Timestamp startTime;
        readonly Timestamp endTime;
        private string processId;
        private string processCommand;
        readonly int threadId;
        readonly DataSize size;

        public FileEvent(string name, int threadId, string filepath, long sizeInBytes, Timestamp startTime, Timestamp endTime)
        {
            this.name = name;
            this.threadId = threadId;
            this.filepath = filepath;
            this.size = new DataSize(sizeInBytes);
            this.startTime = startTime;
            this.endTime = endTime;
        }

        public void SetThreadInfo(ThreadBasicInfo info)
        {
            this.processId = info.Pid;
            this.processCommand = info.Command;
        }

        public string Name => this.name;
        public string ProcessId => this.processId;
        public string ProcessCommand => this.processCommand;
        public int ThreadId => this.threadId;
        public DataSize Size => this.size;
        public string Filepath => this.filepath;
        public Timestamp StartTime => this.startTime;
        public Timestamp EndTime => this.endTime;
    }
}
