// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;
using Utilities;

namespace PerfettoProcessor
{
    /// <summary>
    /// Samples from the traced_perf perf sampler.
    /// https://perfetto.dev/docs/analysis/sql-tables#perf_sample
    /// </summary>
    public class PerfettoPerfSampleEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoPerfSample";

        public const string SqlQuery = "select id, type, ts, utid, cpu, cpu_mode, callsite_id, unwind_error, perf_session_id from perf_sample order by id";
        public int Id { get; set; }
        public string Type { get; set; }
        /// <summary>
        /// timestamp of the sample.
        /// </summary>
        public long Timestamp { get; set; }
        public long RelativeTimestamp { get; set; }
        /// <summary>
        /// sampled thread.
        /// Joinable with thread.utid
        /// </summary>
        public uint Utid { get; set; }
        /// <summary>
        /// the core the sampled thread was running on.
        /// </summary>
        public uint Cpu { get; set; }
        public string CpuMode { get; set; }
        /// <summary>
        /// if set, unwound callstack of the sampled thread.
        /// </summary>
        public int? CallsiteId { get; set; }
        /// <summary>
        /// if set, indicates that the unwinding for this sampleencountered an error. Such samples still reference the best-effort result via the callsite_id (with a synthetic error frame at the point where unwinding stopped)
        /// </summary>
        public string UnwindError { get; set; }

        /// <summary>
        /// distinguishes samples from different profilingstreams (i.e. multiple data sources)
        /// Joinable with perf_counter_track.perf_session_id
        /// </summary>
        public uint PerfSessionId { get; set; }


        public override string GetSqlQuery()
        {
            return SqlQuery;
        }

        public override string GetEventKey()
        {
            return Key;
        }

        public override void ProcessCell(string colName,
            QueryResult.Types.CellsBatch.Types.CellType cellType,
            QueryResult.Types.CellsBatch batch,
            string[] stringCells,
            CellCounters counters)
        {
            var col = colName.ToLower();
            switch (cellType)
            {
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellInvalid:
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellNull:
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellVarint:
                    var longVal = batch.VarintCells[counters.IntCounter++];
                    switch (col)
                    {
                        case "id":
                            Id = (int)longVal;
                            break;
                        case "ts":
                            Timestamp = longVal;
                            break;
                        case "utid":
                            Utid = (uint)longVal;
                            break;
                        case "cpu":
                            Cpu = (uint)longVal;
                            break;
                        case "callsite_id":
                            CallsiteId = (int)longVal;
                            break;
                        case "perf_session_id":
                            PerfSessionId = (uint)longVal;
                            break;
                    }

                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellFloat64:
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellString:
                    var strVal = Common.StringIntern(stringCells[counters.StringCounter++]);
                    switch (col)
                    {
                        case "type":
                            Type = strVal;
                            break;
                        case "cpu_mode":
                            CpuMode = strVal;
                            break;
                        case "unwind_error":
                            UnwindError = strVal;
                            break;
                    }
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellBlob:
                    break;
                default:
                    throw new Exception("Unexpected CellType");
            }
        }
    }
}
