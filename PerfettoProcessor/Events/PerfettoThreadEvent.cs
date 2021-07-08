// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;

namespace PerfettoProcessor
{
    public class PerfettoThreadEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoThreadEvent";

        public static string SqlQuery = "select utid, id, type, tid, name, start_ts, end_ts, upid, is_main_thread from thread";
        public long Utid { get; set; }
        public long Id { get; set; }
        public string Type { get; set; }
        public long Tid { get; set; }
        public string Name{ get; set; }
        public long StartTimestamp { get; set; }
        public long EndTimestamp { get; set; }
        public long Upid { get; set; }
        public long IsMainThread{ get; set; }

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
                        case "utid":
                            Utid = longVal;
                            break;
                        case "id":
                            Id = longVal;
                            break;
                        case "upid":
                            Upid = longVal;
                            break;
                        case "tid":
                            Tid = longVal;
                            break;
                        case "is_main_thread":
                            IsMainThread = longVal;
                            break;
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
                    var strVal = stringCells[counters.StringCounter++];
                    switch (col)
                    {
                        case "type":
                            Type = strVal;
                            break;
                        case "name":
                            Name = strVal;
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
