using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Perfetto.Protos;
using Microsoft.Performance.SDK;
using PerfettoCds.Pipeline.Events;
using System.Linq;

namespace PerfettoCds
{
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

        // For WPA's loading bar
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

                // String cells get stored as a single string delimieted by null character. Split that up ourselves
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
                this.dataSourceInfo = new DataSourceInfo(traceStartTime.Value.ToNanoseconds, traceEndTime.Value.ToNanoseconds, DateTime.Now.ToUniversalTime()); // TODO update me
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
                // Start the progress counter to indicate something is happening
                // OpenTraceProcessor could take a few seconds
                IncreaseProgress(1);

                // Start the Perfetto trace processor shell with the trace file
                traceProc.OpenTraceProcessor(filePath);

                // We're saying the SQL queries take up about 50 percent of the processing
                double queryProgressIncrease = 49.0 / 5.0; // We're doing 5 SQL queries below
                
                // Run queries over all the tables we care about
                var sliceQr = traceProc.QueryTrace(filePath, PerfettoSliceEvent.SqlQuery);
                IncreaseProgress(queryProgressIncrease);
                var argQr = traceProc.QueryTrace(filePath, PerfettoArgEvent.SqlQuery);
                IncreaseProgress(queryProgressIncrease);
                var threadTrackQr = traceProc.QueryTrace(filePath, PerfettoThreadTrackEvent.SqlQuery);
                IncreaseProgress(queryProgressIncrease);
                var threadQr = traceProc.QueryTrace(filePath, PerfettoThreadEvent.SqlQuery);
                IncreaseProgress(queryProgressIncrease);
                var processQr = traceProc.QueryTrace(filePath, PerfettoProcessEvent.SqlQuery);
                IncreaseProgress(queryProgressIncrease);

                // Done with the SQL trace processor
                traceProc.CloseTraceConnection();

                // Count all the cells for our progress calculation
                var totalCells = sliceQr.Batch.Sum(x => x.Cells.Count);
                totalCells += argQr.Batch.Sum(x => x.Cells.Count);
                totalCells += threadTrackQr.Batch.Sum(x => x.Cells.Count);
                totalCells += threadQr.Batch.Sum(x => x.Cells.Count);
                totalCells += processQr.Batch.Sum(x => x.Cells.Count);

                // The cell processing takes up the other 50 percent
                double cellProgressIncrease = 50.0 / (double)totalCells;

                // Process the output of all those queries
                ProcessSqlQuery(sliceQr, dataProcessor, cancellationToken, PerfettoPluginConstants.SliceEvent, cellProgressIncrease);
                ProcessSqlQuery(argQr, dataProcessor, cancellationToken, PerfettoPluginConstants.ArgEvent, cellProgressIncrease);
                ProcessSqlQuery(threadTrackQr, dataProcessor, cancellationToken, PerfettoPluginConstants.ThreadTrackEvent, cellProgressIncrease);
                ProcessSqlQuery(threadQr, dataProcessor, cancellationToken, PerfettoPluginConstants.ThreadEvent, cellProgressIncrease);
                ProcessSqlQuery(processQr, dataProcessor, cancellationToken, PerfettoPluginConstants.ProcessEvent, cellProgressIncrease);

            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error while processing Perfetto trace: {e.Message}");
                traceProc.CloseTraceConnection();
            }
        }
    }
}
