// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using LTTngDataExtensions.DataOutputTypes;
using Microsoft.Performance.SDK;
using System;

namespace LTTngDataExtensions.SourceDataCookers.Disk
{
    public class DiskActivity 
        : IDiskActivity
    {

        private Timestamp? insertTime;
        private Timestamp? issueTime;
        private Timestamp? completeTime;
        private string deviceName;
        private uint deviceId;
        private ulong sectorNumber;
        private DataSize? size;
        private string filepath;
        private int error;
        private int threadId;
        private string processId;
        private string processCommand;


        public DiskActivity(DiskActivityBuilder builder)
        {
            this.insertTime = builder.InsertTime;
            this.issueTime = builder.IssueTime;
            this.completeTime = builder.CompleteTime;
            this.deviceId = builder.DeviceId;
            this.deviceName = builder.DeviceName;
            this.sectorNumber = builder.SectorNumber;
            this.size = builder.Size;
            this.filepath = builder.Filepath;
            this.threadId = builder.ThreadId;
            this.processId = builder.ProcessId;
            this.processCommand = builder.ProcessCommand;
            this.error = builder.Error;
        }

        public Timestamp? InsertTime => this.insertTime;

        public Timestamp? IssueTime => this.issueTime;

        public Timestamp? CompleteTime => this.completeTime;

        public string DeviceName=> this.deviceName;

        public uint DeviceId => this.deviceId;

        public ulong SectorNumber => this.sectorNumber;

        public DataSize? Size => this.size;

        public string Filepath => this.filepath;

        public int ThreadId => this.threadId;

        public string ProcessId => this.processId;

        public string ProcessCommand => this.processCommand;

        public int Error => this.error;
    }

}