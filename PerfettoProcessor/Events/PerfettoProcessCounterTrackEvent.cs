// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;

namespace PerfettoProcessor
{
    public class PerfettoProcessCounterTrackEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoProcessCounterTrackEvent";

        public static string SqlQuery = "select name, id, upid from process_counter_track";
        public long Id { get; set; }
        public string Name { get; set; }
        public long Upid { get; set; }

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
                            Id = longVal;
                            break;
                        case "upid":
                            Upid = longVal;
                            break;
                    }
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellFloat64:
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellString:
                    var strVal = stringCells[counters.StringCounter++];
                    switch (col)
                    {
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
