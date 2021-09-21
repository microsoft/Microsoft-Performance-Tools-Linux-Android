// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;

namespace PerfettoProcessor
{
    /// <summary>
    /// Trace_bounds table contains a single event that lists the timestamps for the overall first and last 
    /// events in the trace
    /// </summary>
    public class PerfettoTraceBoundsEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoTraceBoundsEvent";

        public const string SqlQuery = "select start_ts, end_ts from trace_bounds";
        public long StartTimestamp { get; set; }
        public long RelativeStartTimestamp { get; set; }

        public long EndTimestamp { get; set; }
        public long RelativeEndTimestamp { get; set; }


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
                        case "start_ts":
                            StartTimestamp = longVal;
                            break;
                        case "end_ts":
                            EndTimestamp = longVal;
                            break;
                    }
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellFloat64:
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellString:
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellBlob:
                    break;
                default:
                    throw new Exception("Unexpected CellType");
            }
        }
    }
}
