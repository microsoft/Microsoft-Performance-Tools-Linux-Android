// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;

namespace PerfettoProcessor
{
    public class PerfettoRawEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoRawEvent";

        public static string SqlQuery = "select type, ts, name, cpu, utid, arg_set_id from raw";
        public string Type { get; set; }
        public long Timestamp { get; set; }
        public long RelativeTimestamp { get; set; }
        public string Name { get; set; }
        public long Cpu { get; set; }
        public long Utid { get; set; }
        public long ArgSetId { get; set; }


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
                        case "ts":
                            Timestamp = longVal;
                            break;
                        case "cpu":
                            Cpu = longVal;
                            break;
                        case "arg_set_id":
                            ArgSetId = longVal;
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
