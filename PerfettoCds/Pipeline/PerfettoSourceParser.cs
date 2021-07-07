// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Performance.SDK;
using PerfettoProcessor;
using PerfettoCds.Pipeline.Events;
using System.IO;
using System.Reflection;

namespace PerfettoCds
{

    public sealed class PerfettoSourceParser : ISourceParser<PerfettoSqlEventKeyed, PerfettoSourceParser, string>
    {
        public string Id => PerfettoPluginConstants.ParserId;

        public Type DataElementType => typeof(PerfettoSqlEvent);
        public Type DataContextType => typeof(PerfettoSourceParser);
        public Type DataKeyType => typeof(string);

        public int MaxSourceParseCount => 1;

        // Information about this data source the SDK requires for building tables
        private DataSourceInfo dataSourceInfo { get; set; }

        public DataSourceInfo DataSourceInfo => this.dataSourceInfo;

        // For UI progress reporting
        private IProgress<int> Progress;
        private double CurrentProgress;

        /// <summary>
        /// Increase the progress percentage by a fixed percent
        /// </summary>
        /// <param name="percent">Percent to increase the loading progress (0-100) </param>
        private void IncreaseProgress(double percent)
        {
            if (Progress != null)
            {
                CurrentProgress += percent;
                Progress.Report((int)CurrentProgress);
            }
        }

        // Perfetto trace file (.perfetto-trace) being processed
        private readonly string filePath;
        public PerfettoSourceParser(string filePath)
        {
            this.filePath = filePath;
        }

        public void PrepareForProcessing(bool allEventsConsumed, IReadOnlyCollection<string> requestedDataKeys)
        {
            // No preperation needed
        }

        public void ProcessSource(ISourceDataProcessor<PerfettoSqlEventKeyed, PerfettoSourceParser, string> dataProcessor,
            ILogger logger,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            this.Progress = progress;
            PerfettoTraceProcessor traceProc = new PerfettoTraceProcessor();

            Timestamp? traceStartTime = null;
            Timestamp? traceEndTime = null;

            try
            {
                // Start the progress counter to indicate something is happening because
                // OpenTraceProcessor could take a few seconds
                IncreaseProgress(1);

                // Shell .exe should be located in same directory as this assembly.
                var shellDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var shellPath = Path.Combine(shellDir, PerfettoPluginConstants.TraceProcessorShellFileName);

                // Start the Perfetto trace processor shell with the trace file
                traceProc.OpenTraceProcessor(shellPath, filePath);

                double queryProgressIncrease = 99.0 / 5.0; // We're doing 5 SQL queries below

                // Use this callback to receive events parsed
                void EventCallback(PerfettoSqlEvent ev, string eventType, long cellsProcessed)
                {
                    if (ev.GetType() == typeof(PerfettoSliceEvent))
                    {
                        // We get the timestamps used for displaying these events from the slice event
                        if (traceStartTime == null)
                        {
                            traceStartTime = new Timestamp(((PerfettoSliceEvent)ev).Timestamp);
                        }
                        traceEndTime = new Timestamp(((PerfettoSliceEvent)ev).Timestamp);
                    }


                    PerfettoSqlEventKeyed newEvent = new PerfettoSqlEventKeyed(eventType, ev);

                    // Store the event
                    var result = dataProcessor.ProcessDataElement(newEvent, this, cancellationToken);
                }

                traceProc.QueryTraceForEvents(PerfettoSliceEvent.SqlQuery, PerfettoPluginConstants.SliceEvent, EventCallback);
                IncreaseProgress(queryProgressIncrease);
                traceProc.QueryTraceForEvents(PerfettoArgEvent.SqlQuery, PerfettoPluginConstants.ArgEvent, EventCallback);
                IncreaseProgress(queryProgressIncrease);
                traceProc.QueryTraceForEvents(PerfettoThreadTrackEvent.SqlQuery, PerfettoPluginConstants.ThreadTrackEvent, EventCallback);
                IncreaseProgress(queryProgressIncrease);
                traceProc.QueryTraceForEvents(PerfettoThreadEvent.SqlQuery, PerfettoPluginConstants.ThreadEvent, EventCallback);
                IncreaseProgress(queryProgressIncrease);
                traceProc.QueryTraceForEvents(PerfettoProcessEvent.SqlQuery, PerfettoPluginConstants.ProcessEvent, EventCallback);

                // Done with the SQL trace processor
                traceProc.CloseTraceConnection();

                if (traceStartTime.HasValue)
                {
                    // Use DateTime.Now as the wall clock time. This doesn't matter for displaying events on a relative timescale
                    // TODO Actual wall clock time needs to be gathered from SQL somehow
                    this.dataSourceInfo = new DataSourceInfo(traceStartTime.Value.ToNanoseconds, traceEndTime.Value.ToNanoseconds, DateTime.Now.ToUniversalTime());
                }

            }
            catch (Exception e)
            {
                logger.Error($"Error while processing Perfetto trace: {e.Message}");
                traceProc.CloseTraceConnection();
            }
        }
    }
}
