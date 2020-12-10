// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using LttngDataExtensions.DataOutputTypes;
using LttngDataExtensions.SourceDataCookers.Thread;
using Microsoft.Performance.SDK;
using System;

namespace LttngDataExtensions.SourceDataCookers.Disk
{
    public class DiskActivityBuilder
    {
        public DiskActivityBuilder(uint deviceId, ulong sectorNumber, string filepath, int tid)
        {
            this.DeviceId = deviceId;
            this.SectorNumber = sectorNumber;
            this.Filepath = filepath;
            this.ThreadId = tid;
            this.Error = 0;
            this.DeviceName = String.Empty;
        }

        public Timestamp? InsertTime { get; set; }

        public Timestamp? IssueTime { get; set; }

        public Timestamp? CompleteTime { get; set; }

        public uint DeviceId { get; set; }

        public string DeviceName { get; set; }

        public ulong SectorNumber { get; set; }

        public DataSize? Size { get; set; }

        public string Filepath { get; set; }

        public int ThreadId { get; set; }

        public String ProcessId { get; set; }

        public String ProcessCommand { get; set; }

        public int Error { get; set; }

        public DiskActivity Build()
        {
            return new DiskActivity(this);
        }
        public void SetThreadInfo(ThreadBasicInfo info)
        {
            this.ProcessId = info.Pid;
            this.ProcessCommand = info.Command;
        }
    }
}
