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
        public long Upid { get; }
        public long DisplayToken { get; }
        public long SurfaceToken { get; }
        public TimestampDelta Duration { get; }
        public Timestamp StartTimestamp { get; }
        public Timestamp EndTimestamp { get; }
        public string JankType { get; }
        public string JankTag { get; }
        public string AppOnTime { get; }
        public string PresentType { get; }
        public string GpuComposition { get; }
        public string PredictionType { get; }


        public PerfettoFrameEvent(string FrameType,
            string processName,
            long upid,
            long displayToken,
            long surfaceToken,
            TimestampDelta duration,
            Timestamp startTimestamp,
            Timestamp endTimestamp,
            string JankType,
            string JankTag,
            string AppOnTime,
            string PresentType,
            string GpuComposition,
            string PredictionType)
        {
            this.FrameType = Common.StringIntern(FrameType);
            this.ProcessName = Common.StringIntern(processName);
            this.Upid = upid;
            this.DisplayToken = displayToken;
            this.SurfaceToken = surfaceToken;
            this.Duration = duration;
            this.StartTimestamp = startTimestamp;
            this.EndTimestamp = endTimestamp;
            this.JankType = Common.StringIntern(JankType);
            this.JankTag = Common.StringIntern(JankTag);
            this.AppOnTime = Common.StringIntern(AppOnTime);
            this.PresentType = Common.StringIntern(PresentType);
            this.GpuComposition = Common.StringIntern(GpuComposition);
            this.PredictionType = Common.StringIntern(PredictionType);
        }
    }
}
