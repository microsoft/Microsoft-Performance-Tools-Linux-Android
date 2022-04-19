// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK;
using System;
using Utilities;

namespace PerfettoCds.Pipeline.DataOutput
{
    /// <summary>
    /// An event that represents a frame that was scheduled or actual displayed by a process. 
    /// </summary>
    public class PerfettoFrameEvent
    {

        public string FrameType { get; }
        public string ProcessName { get; }
        public uint Upid { get; }
        public long DisplayFrameToken { get; }
        public long SurfaceFrameToken { get; }
        public TimestampDelta Duration { get; }
        public Timestamp StartTimestamp { get; }
        public Timestamp EndTimestamp { get; }
        public string JankType { get; }
        public string JankTag { get; }
        public string PresentType { get; }
        public string PredictionType { get; }
        public string GpuComposition { get; }

        public string OnTimeFinish { get; }


        public PerfettoFrameEvent(string FrameType,
            string processName,
            uint upid,
            long displayToken,
            long surfaceToken,
            TimestampDelta duration,
            Timestamp startTimestamp,
            Timestamp endTimestamp,
            string JankType,
            string JankTag,
            string PresentType,
            string PredictionType,
            string OnTimeFinish,
            string GpuComposition)
        {
            this.FrameType = Common.StringIntern(FrameType);
            this.ProcessName = Common.StringIntern(processName);
            this.Upid = upid;
            this.DisplayFrameToken = displayToken;
            this.SurfaceFrameToken = surfaceToken;
            this.Duration = duration;
            this.StartTimestamp = startTimestamp;
            this.EndTimestamp = endTimestamp;
            this.JankType = Common.StringIntern(JankType);
            this.JankTag = Common.StringIntern(JankTag);
            this.PresentType = Common.StringIntern(PresentType);
            this.PredictionType = Common.StringIntern(PredictionType);
            this.OnTimeFinish = OnTimeFinish;
            this.GpuComposition = GpuComposition;
        }
    }
}
