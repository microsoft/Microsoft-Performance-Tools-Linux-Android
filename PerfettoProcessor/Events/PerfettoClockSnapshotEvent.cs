// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;

namespace PerfettoProcessor
{
    /// <summary>
    /// The clock_snapshot table contains timings from multiple different clocks on device taken at multiple
    /// points in time across a trace. The purpose is so that events can be correctly aligned in postprocessing
    /// due to clock drift. Trace_processor_shell takes care of all the drift alignment, but we use it to get the 
    /// UTC time and start/end time of the trace
    /// </summary>
    public class PerfettoClockSnapshotEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoClockSnapshotEvent";

        // Realtime represents wall clock Unix time in nanoseconds
        public const string REALTIME = "REALTIME";
        // Boottime is counting from an arbitrary time in the past (at bootup?). This time aligns with the timestamps that most events return
        public const string BOOTTIME = "BOOTTIME";

        public const string SqlQuery = "select ts, clock_id, clock_name, clock_value, snapshot_id from clock_snapshot order by snapshot_id ASC";
        public long Timestamp { get; set; }
        public long ClockId { get; set; }
        public string ClockName { get; set; }
        public long ClockValue { get; set; }
        public long SnapshotId { get; set; }

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
                        case "ts":
                            Timestamp = longVal;
                            break;
                        case "clock_id":
                            ClockId = longVal;
                            break;
                        case "clock_value":
                            ClockValue = longVal;
                            break;
                        case "snapshot_id":
                            SnapshotId = longVal;
                            break;
                    }
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellFloat64:
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellString:
                    var strVal = stringCells[counters.StringCounter++];
                    switch (col)
                    {
                        case "clock_name":
                            ClockName = strVal;
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
