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

        public Timestamp FirstEventTimestamp { get; private set; }

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

            Timestamp firstSnapTime = Timestamp.MaxValue;
            Timestamp lastSnapTime = Timestamp.MinValue;

            Timestamp firstEventTime = Timestamp.MaxValue;
            Timestamp lastEventTime = Timestamp.MinValue;

            DateTime? traceStartDateTime = null;

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

                // Use this callback to receive events parsed and turn them in events with keys
                // that get used by data cookers
                void EventCallback(PerfettoSqlEvent ev)
                {
                    // Get all the timings we need from the snapshot events
                    if (ev.GetType() == typeof(PerfettoClockSnapshotEvent))
                    {
                        var clockSnapshot = (PerfettoClockSnapshotEvent)ev;

                        // Each "snapshot" is a collection of timings at a point of time in a trace. Each snapshot has a snapshot_ID
                        // that is incremented started from 0.

                        // SnapshotId of 0 indicates the first clock snapshot. This corresponds with the start of the trace
                        if (clockSnapshot.SnapshotId == 0 && clockSnapshot.ClockName == PerfettoClockSnapshotEvent.REALTIME)
                        {
                            // Convert from Unix time in nanoseconds to a DateTime
                            traceStartDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                            traceStartDateTime = traceStartDateTime.Value.AddSeconds(clockSnapshot.ClockValue / 1000000000);
                        }
                        else if (clockSnapshot.SnapshotId == 0 && clockSnapshot.ClockName == PerfettoClockSnapshotEvent.BOOTTIME)
                        {
                            // Capture the initial BOOTTIME because all the events use BOOTTIME
                            firstSnapTime = new Timestamp(clockSnapshot.Timestamp);
                        }
                        if (clockSnapshot.ClockName == PerfettoClockSnapshotEvent.BOOTTIME)
                        {
                            // Events are ordered ASCENDING so keep overwriting and the last event is the actual end time
                            lastSnapTime = new Timestamp(clockSnapshot.Timestamp);
                        }
                    }
                    else if (ev.GetType() == typeof(PerfettoTraceBoundsEvent))
                    {
                        // The trace_bounds table stores a single row with the timestamps of the first and last events of the trace
                        var traceBounds = (PerfettoTraceBoundsEvent)ev;
                        firstEventTime = new Timestamp(traceBounds.StartTimestamp);
                        lastEventTime = new Timestamp(traceBounds.EndTimestamp);
                    }

                    PerfettoSqlEventKeyed newEvent = new PerfettoSqlEventKeyed(ev.GetEventKey(), ev);

                    // Store the event
                    var result = dataProcessor.ProcessDataElement(newEvent, this, cancellationToken);
                }

                // Perform the base queries for all the events we need
                List<PerfettoSqlEvent> eventsToQuery = new List<PerfettoSqlEvent>
                {
                    new PerfettoTraceBoundsEvent(),
                    new PerfettoClockSnapshotEvent(),
                    new PerfettoSliceEvent(),
                    new PerfettoArgEvent(),
                    new PerfettoThreadTrackEvent(),
                    new PerfettoThreadEvent(),
                    new PerfettoProcessEvent(),
                    new PerfettoSchedSliceEvent(),
                    new PerfettoAndroidLogEvent(),
                    new PerfettoRawEvent(),
                    new PerfettoCpuCounterTrackEvent(),
                    new PerfettoCounterEvent()
                };

                // Increment progress for each table queried.
                double queryProgressIncrease = 99.0 / eventsToQuery.Count;

                // We need to run the first 2 queries (TraceBounds and ClockSnapshot) in order to have all the information we need to
                // gather the timing information. We want the timing information before we start to process the rest of the events,
                // so that the source cookers can calculate relative timestamps
                int cnt = 0;
                int minQueriesForTimings = 2; // Need TraceBounds and ClockSnapshot to have been processed

                // Run all the queries
                foreach (var query in eventsToQuery)
                {
                    logger.Verbose($"Querying for {query.GetEventKey()} using SQL query: {query.GetSqlQuery()}");

                    // Run the query and process the events.
                    var dateTimeQueryStarted = DateTime.UtcNow;
                    traceProc.QueryTraceForEvents(query.GetSqlQuery(), query.GetEventKey(), EventCallback);
                    var dateTimeQueryFinished = DateTime.UtcNow;

                    logger.Verbose($"Query for {query.GetEventKey()} completed in {(dateTimeQueryFinished - dateTimeQueryStarted).TotalSeconds}s at {dateTimeQueryFinished} UTC");

                    IncreaseProgress(queryProgressIncrease);

                    // If we have all the timing data we need, create the DataSourceInfo
                    if (++cnt == minQueriesForTimings)
                    {
                        if (firstEventTime != Timestamp.MaxValue && lastEventTime != Timestamp.MinValue && traceStartDateTime.HasValue)
                        {
                            // Get the delta between the first event time and the first snapshot time
                            var startDelta = firstEventTime - firstSnapTime;

                            // Get the delta between the first and last event
                            var eventDelta = new Timestamp(lastEventTime.ToNanoseconds - firstEventTime.ToNanoseconds);
                            this.FirstEventTimestamp = firstEventTime;

                            // The starting UTC time is from the snapshot. We need to adjust it based on when the first event happened
                            // The highest precision DateTime has is ticks (a tick is a group of 100 nanoseconds)
                            DateTime adjustedTraceStartDateTime = traceStartDateTime.Value.AddTicks(startDelta.ToNanoseconds / 100);

                            logger.Verbose($"Perfetto trace UTC start: {adjustedTraceStartDateTime.ToUniversalTime().ToString()}");
                            this.dataSourceInfo = new DataSourceInfo(0, eventDelta.ToNanoseconds, adjustedTraceStartDateTime.ToUniversalTime());
                        }
                        else
                        {
                            throw new Exception("Start and end time were not able to be determined by the Perfetto trace");
                        }
                    }
                }

                // Done with the SQL trace processor
                traceProc.CloseTraceConnection();
            }
            catch (Exception e)
            {
                logger.Error($"Error while processing Perfetto trace: {e.Message}");
                traceProc.CloseTraceConnection();
            }
        }
    }
}
