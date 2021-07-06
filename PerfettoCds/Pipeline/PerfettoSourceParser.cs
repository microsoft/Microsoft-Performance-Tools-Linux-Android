// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.Threading;
using Perfetto.Protos;
using Microsoft.Performance.SDK;
using PerfettoCds.Pipeline.Events;
using System.Linq;

namespace PerfettoCds
{

    public class TableQueryResult
    {
        public PerfettoSqlEvent EventType;
        public QueryResult QueryResult;

        public TableQueryResult(PerfettoSqlEvent eventType)
        {
            this.EventType = eventType;
        }
    }

    public sealed class PerfettoSourceParser : ISourceParser<PerfettoSqlEvent, PerfettoSourceParser, string>
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

        /// <summary>
        /// Process a SQL query result (a QueryResult protobuf object). The result is split into batches
        /// and within each batch are the cells. Cells are processed by the type of event that they are.
        /// </summary>
        /// <param name="qr">QueryResult</param>
        /// <param name="dataProcessor"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="eventType">Type of event that is being processed.</param>
        /// <param name="progressIncrease">Amount of progress to increase for processing a single cell.</param>
        public void ProcessSqlQuery(QueryResult qr, 
            ISourceDataProcessor<PerfettoSqlEvent, PerfettoSourceParser, string> dataProcessor,
            CancellationToken cancellationToken,
            string eventType,
            double progressIncrease)
        {
            var numColumns = qr.ColumnNames.Count;
            var cols = qr.ColumnNames;
            var numBatches = qr.Batch.Count;
            Timestamp? traceStartTime = null;
            Timestamp? traceEndTime = null;

            foreach (var batch in qr.Batch)
            {
                CellCounters cellCounters = new CellCounters();

                // String cells get stored as a single string delimited by null character. Split that up ourselves
                var stringCells = batch.StringCells.Split('\0');

                int cellCount = 0;
                PerfettoSqlEvent ev = null;
                foreach (var cell in batch.Cells)
                {
                    if (ev == null)
                    {
                        switch (eventType)
                        {
                            case PerfettoPluginConstants.SliceEvent:
                                ev = new PerfettoSliceEvent();
                                break;
                            case PerfettoPluginConstants.ArgEvent:
                                ev = new PerfettoArgEvent();
                                break;
                            case PerfettoPluginConstants.ThreadTrackEvent:
                                ev = new PerfettoThreadTrackEvent();
                                break;
                            case PerfettoPluginConstants.ThreadEvent:
                                ev = new PerfettoThreadEvent();
                                break;
                            case PerfettoPluginConstants.ProcessEvent:
                                ev = new PerfettoProcessEvent();
                                break;
                            default:
                                throw new Exception("Invalid event type");
                        }
                    }

                    var colIndex = cellCount % numColumns;
                    var colName = cols[colIndex].ToLower();

                    // The event itself is responsible for figuring out how to process and store cell contents
                    ev.ProcessCell(colName, cell, batch, stringCells, cellCounters);

                    // If we've reached the end of a row, we've finished an event. Store it.
                    if (++cellCount % numColumns == 0)
                    {
                        if (ev.GetType() == typeof(PerfettoSliceEvent))
                        {
                            // We get the timestamps used for displaying these events from the slice event
                            if (traceStartTime == null)
                            {
                                traceStartTime = ((PerfettoSliceEvent)ev).Timestamp;
                            }
                            traceEndTime = ((PerfettoSliceEvent)ev).Timestamp;
                        }

                        // Store the event
                        var result = dataProcessor.ProcessDataElement(ev, this, cancellationToken);

                        ev = null;
                    }
                    IncreaseProgress(progressIncrease);
                }
            }

            if (traceStartTime.HasValue)
            {
                // Use DateTime.Now as the wall clock time. This doesn't matter for displaying events on a relative timescale
                // TODO Actual wall clock time needs to be gathered from SQL somehow
                this.dataSourceInfo = new DataSourceInfo(traceStartTime.Value.ToNanoseconds, traceEndTime.Value.ToNanoseconds, DateTime.Now.ToUniversalTime());
            }
        }


        public void ProcessSource(ISourceDataProcessor<PerfettoSqlEvent, PerfettoSourceParser, string> dataProcessor,
            ILogger logger,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            this.Progress = progress;

            PerfettoTraceProcessor traceProc = new PerfettoTraceProcessor();

            try
            {
                // Start the progress counter to indicate something is happening because
                // OpenTraceProcessor could take a few seconds
                IncreaseProgress(1);

                // Start the Perfetto trace processor shell with the trace file
                traceProc.OpenTraceProcessor(filePath);

                // We're saying the SQL queries take up about 50 percent of the processing
                double queryProgressIncrease = 49.0 / 5.0; // We're doing 5 SQL queries below

                List<TableQueryResult> tableQueries = new List<TableQueryResult>()
                {
                    new TableQueryResult(new PerfettoSliceEvent()),
                    new TableQueryResult(new PerfettoArgEvent()),
                    new TableQueryResult(new PerfettoThreadTrackEvent()),
                    new TableQueryResult(new PerfettoThreadEvent()),
                    new TableQueryResult(new PerfettoProcessEvent())
                };

                int totalCells = 0;
                foreach (var query in tableQueries)
                {
                    logger.Info($"Executing Perfetto SQL query: {query.EventType.GetSqlQuery()}");
                    query.QueryResult = traceProc.QueryTrace(query.EventType.GetSqlQuery());
                    IncreaseProgress(queryProgressIncrease);
                    totalCells += query.QueryResult.Batch.Sum(x => x.Cells.Count);
                }
                logger.Info($"Processing {totalCells} total Perfetto cells");

                // Done with the SQL trace processor
                traceProc.CloseTraceConnection();

                // The cell processing takes up the other 50 percent
                double cellProgressIncrease = 50.0 / (double)totalCells;

                foreach (var query in tableQueries)
                {
                    ProcessSqlQuery(query.QueryResult, dataProcessor, cancellationToken, query.EventType.Key, cellProgressIncrease);
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
