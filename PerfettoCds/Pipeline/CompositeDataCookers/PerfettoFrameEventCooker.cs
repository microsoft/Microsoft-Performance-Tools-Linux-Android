// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Processing;
using PerfettoCds.Pipeline.DataOutput;
using PerfettoCds.Pipeline.SourceDataCookers;
using PerfettoProcessor;
using Utilities;

namespace PerfettoCds.Pipeline.CompositeDataCookers
{
    /// <summary>
    /// Pulls data from multiple individual SQL tables and joins them to create a frame event.
    /// These frame events represent frames that were scheduled and then rendered by apps.
    /// </summary>
    public sealed class PerfettoFrameEventCooker : CookedDataReflector, ICompositeDataCookerDescriptor
    {
        public static readonly DataCookerPath DataCookerPath = PerfettoPluginConstants.FrameEventCookerPath;

        public string Description => "Frame event composite cooker";

        public DataCookerPath Path => DataCookerPath;

        // Declare all of the cookers that are used by this CompositeCooker.
        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new[]
        {
            PerfettoPluginConstants.ThreadCookerPath,
            PerfettoPluginConstants.ProcessCookerPath,
            PerfettoPluginConstants.ActualFrameCookerPath,
            PerfettoPluginConstants.ExpectedFrameCookerPath
        };

        [DataOutput]
        public ProcessedEventData<PerfettoFrameEvent> FrameEvents { get; }


        public PerfettoFrameEventCooker() : base(PerfettoPluginConstants.FrameEventCookerPath)
        {
            this.FrameEvents = new ProcessedEventData<PerfettoFrameEvent>();
        }

        public void OnDataAvailable(IDataExtensionRetrieval requiredData)
        {
            // Gather the data from all the SQL tables
            var threadData = requiredData.QueryOutput<ProcessedEventData<PerfettoThreadEvent>>(new DataOutputPath(PerfettoPluginConstants.ThreadCookerPath, nameof(PerfettoThreadCooker.ThreadEvents)));
            var processData = requiredData.QueryOutput<ProcessedEventData<PerfettoProcessEvent>>(new DataOutputPath(PerfettoPluginConstants.ProcessCookerPath, nameof(PerfettoProcessCooker.ProcessEvents)));
            PopulateFrameEvents(requiredData, threadData, processData);
        }

        void PopulateFrameEvents(IDataExtensionRetrieval requiredData, ProcessedEventData<PerfettoThreadEvent> threadData, ProcessedEventData<PerfettoProcessEvent> processData)
        {
            var actualFrameData = requiredData.QueryOutput<ProcessedEventData<PerfettoActualFrameEvent>>(new DataOutputPath(PerfettoPluginConstants.ActualFrameCookerPath, nameof(PerfettoActualFrameCooker.ActualFrameEvents)));
            var expectedFrameData = requiredData.QueryOutput<ProcessedEventData<PerfettoExpectedFrameEvent>>(new DataOutputPath(PerfettoPluginConstants.ExpectedFrameCookerPath, nameof(PerfettoExpectedFrameCooker.ExpectedFrameEvents)));

            // The sched slice data contains the timings, CPU, priority, and end state info. We get the process and thread from
            // those respective tables
            var joinedActual = from frame in actualFrameData
                               join process in processData on frame.Upid equals process.Upid into pd orderby frame.Id
                               from process in pd.DefaultIfEmpty()
                               select new { frame, process };
            var joinedExpected = from frame in expectedFrameData
                               join process in processData on frame.Upid equals process.Upid into pd orderby frame.Id
                               from process in pd.DefaultIfEmpty()
                               select new { frame, process };

            // Create events out of the joined results
            foreach (var result in joinedExpected)
            {
                Timestamp startTimestamp = new Timestamp(result.frame.RelativeTimestamp);
                Timestamp endTimestamp = new Timestamp(result.frame.RelativeTimestamp + result.frame.Duration);

                PerfettoFrameEvent ev = new PerfettoFrameEvent
                (
                    "Expected",
                    result.process.Name,
                    result.frame.Upid,
                    result.frame.DisplayFrameToken,
                    result.frame.SurfaceFrameToken,
                    new TimestampDelta(result.frame.Duration),
                    startTimestamp,
                    endTimestamp,
                    String.Empty,
                    String.Empty,
                    String.Empty,
                    String.Empty,
                    String.Empty,
                    String.Empty
                );

                this.FrameEvents.AddEvent(ev);
            }

            foreach (var result in joinedActual)
            {
                Timestamp startTimestamp = new Timestamp(result.frame.RelativeTimestamp);
                Timestamp endTimestamp = new Timestamp(result.frame.RelativeTimestamp + result.frame.Duration);

                PerfettoFrameEvent ev = new PerfettoFrameEvent
                (
                    "Actual",
                    result.process.Name,
                    result.frame.Upid,
                    result.frame.DisplayFrameToken,
                    result.frame.SurfaceFrameToken,
                    new TimestampDelta(result.frame.Duration),
                    startTimestamp,
                    endTimestamp,
                    result.frame.JankType,
                    result.frame.JankTag,
                    result.frame.PresentType,
                    result.frame.PredictionType,
                    result.frame.OnTimeFinish.ToString(),
                    result.frame.GpuComposition.ToString()
                );

                this.FrameEvents.AddEvent(ev);
            }

            this.FrameEvents.FinalizeData();

        }
    }
}
