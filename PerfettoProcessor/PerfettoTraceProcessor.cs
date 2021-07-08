// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Google.Protobuf;
using Perfetto.Protos;
using System;
using System.Diagnostics;
using System.Net.Http;

namespace PerfettoProcessor
{
    /// <summary>
    /// Responsible for managing trace_processor_shell.exe. The shell executable loads the Perfetto trace and allows for SQL querying
    /// through HTTP on localhost. HTTP communication happens with protobuf objects/buffers.
    /// </summary>
    public class PerfettoTraceProcessor
    {
        private Process ShellProcess;

        // Default starting port
        private int HttpPort = 22222;

        // Highest ports can go
        private const int PortMax = 65535;

        // HTTP request denied takes about 3 seconds so time out after about 2 minutes
        private const int MaxRetryLimit = 40;

        /// <summary>
        /// Check if another instance of trace_processor_shell is running on this port
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        private bool IsPortAvailable(int port)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = client.GetStringAsync($"http://localhost:{HttpPort}/status").Result;
                    return !(response.Contains("Perfetto"));
                }
            }
            catch (Exception)
            {
                // Exception here is the connection refused message, meaning the RPC server has not been initialized
                // Which means this port is not being used by another trace_processor_shell
                return true;
            }
        }

        /// <summary>
        /// Initializes trace_processor_shell.exe in HTTP/RPC mode with the trace file
        /// </summary>
        /// <param name="shellPath">Full path to trace_processor_shell.exe</param>
        /// <param name="tracePath">Full path to the Perfetto trace file</param>
        public void OpenTraceProcessor(string shellPath, string tracePath)
        {
            using (var client = new HttpClient())
            {
                // Make sure another instance of trace_processor_shell isn't already running
                while (!IsPortAvailable(HttpPort) && HttpPort < PortMax)
                {
                    HttpPort++;
                }

                ShellProcess = Process.Start(shellPath, $"-D --http-port {HttpPort} -i \"{tracePath}\"");
            }
            if (ShellProcess.HasExited)
            {
                throw new Exception("Problem starting trace_processor_shell.exe");
            }
        }

        /// <summary>
        /// Initializes trace_processor_shell.exe in HTTP/RPC mode without the trace file
        /// </summary>
        /// <param name="shellPath">Full path to trace_processor_shell.exe</param>
        public void OpenTraceProcessor(string shellPath)
        {
            using (var client = new HttpClient())
            {
                // Make sure another instance of trace_processor_shell isn't already running
                while (!IsPortAvailable(HttpPort) && HttpPort < PortMax)
                {
                    HttpPort++;
                }
                ShellProcess = Process.Start(shellPath, $"-D --http-port {HttpPort}");

            }
            if (ShellProcess.HasExited)
            {
                throw new Exception("Problem starting trace_processor_shell.exe");
            }
        }

        /// <summary>
        /// Check if the trace_processor_shell has a loaded trace. Assumes the shell executable is already running. Will throw otherwise.
        /// </summary>
        /// <returns></returns>
        private bool CheckIfTraceIsLoaded()
        {
            if (ShellProcess == null || ShellProcess.HasExited)
            {
                throw new Exception("The trace_process_shell is not running");
            }

            try
            {
                using (var client = new HttpClient())
                {
                    Perfetto.Protos.StatusResult statusResult = new StatusResult();
                    var response = client.GetAsync($"http://localhost:{HttpPort}/status").GetAwaiter().GetResult();
                    var byteArray = response.Content.ReadAsByteArrayAsync().Result;
                    statusResult = StatusResult.Parser.ParseFrom(byteArray);

                    // TODO could also check that the trace name is the same
                    return statusResult.HasLoadedTraceName;
                }
            }
            catch (Exception)
            {
                // Exception here is the connection refused message, meaning the RPC server has not been initialized
                return false;
            }

        }

        /// <summary>
        /// Perform a SQL query against trace_processor_shell to gather Perfetto trace data.
        /// </summary>
        /// <param name="sqlQuery">The query to perform against the loaded trace in trace_processor_shell</param>
        /// <returns></returns>
        public QueryResult QueryTrace(string sqlQuery)
        {
            // Make sure ShellProcess is running
            if (ShellProcess == null || ShellProcess.HasExited)
            {
                throw new Exception("The trace_process_shell is not running");
            }

            int cnt = 0;
            // Check if the trace is loaded
            // We know the shell is running, so the trace could still be in the loading process. Give it a little while to finish loading
            // before we error out.
            while (!CheckIfTraceIsLoaded())
            {
                if (cnt++ > MaxRetryLimit)
                {
                    throw new Exception("Unable to query Perfetto trace because trace_processor_shell.exe does not appear to have loaded it");
                }
            }

            QueryResult qr = null;

            using (var client = new HttpClient())
            {
                // Query with protobuf over RPC using /query endpoint
                Perfetto.Protos.RawQueryArgs queryArgs = new Perfetto.Protos.RawQueryArgs();
                queryArgs.SqlQuery = sqlQuery;
                HttpContent sc = new ByteArrayContent(queryArgs.ToByteArray());
                var response = client.PostAsync($"http://localhost:{HttpPort}/query", sc).GetAwaiter().GetResult();
                var byteArray = response.Content.ReadAsByteArrayAsync().Result;
                qr = QueryResult.Parser.ParseFrom(byteArray);
            }

            return qr;
        }

        /// <summary>
        /// Perform a SQL query against trace_processor_shell to gather Perfetto trace data. Processes the QueryResult
        /// and returns PerfettoSqlObjects through a callback
        /// </summary>
        /// <param name="sqlQuery">The query to perform against the loaded trace in trace_processor_shell</param>
        /// <param name="eventKey">The event key that corresponds to the type of PerfettoSqlEvent to process for this query</param>
        /// <param name="eventCallback">Completed PerfettoSqlEvents will be sent here</param>
        public void QueryTraceForEvents(string sqlQuery, string eventKey, Action<PerfettoSqlEvent> eventCallback)
        {
            var qr = QueryTrace(sqlQuery);

            var numColumns = qr.ColumnNames.Count;
            var cols = qr.ColumnNames;
            var numBatches = qr.Batch.Count;

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
                        switch (eventKey)
                        {
                            case PerfettoSliceEvent.Key:
                                ev = new PerfettoSliceEvent();
                                break;
                            case PerfettoArgEvent.Key:
                                ev = new PerfettoArgEvent();
                                break;
                            case PerfettoThreadTrackEvent.Key:
                                ev = new PerfettoThreadTrackEvent();
                                break;
                            case PerfettoThreadEvent.Key:
                                ev = new PerfettoThreadEvent();
                                break;
                            case PerfettoProcessEvent.Key:
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

                    // If we've reached the end of a row, we've finished an event.
                    if (++cellCount % numColumns == 0)
                    {
                        // Report the event back
                        eventCallback(ev);

                        ev = null;
                    }
                }
            }
        }

        public void CloseTraceConnection()
        {
            ShellProcess?.Kill();
        }
    }
}
