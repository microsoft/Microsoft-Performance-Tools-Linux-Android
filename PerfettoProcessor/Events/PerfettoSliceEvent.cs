// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;

namespace PerfettoProcessor
{
    public class PerfettoSliceEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoSliceEvent";

        public static string SqlQuery = "select id, ts, dur, arg_set_id, track_id, name, type, category, parent_id from slice order by ts";
        public long Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public long Duration { get; set; }
        public long ArgSetId { get; set; }
        public long Timestamp { get; set; }
        public long RelativeTimestamp { get; set; }
        public string Category { get; set; }
        public long TrackId { get; set; }
        public long? ParentId { get; set; }

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
                        case "ts":
                            Timestamp = longVal;
                            break;
                        case "dur":
                            Duration = longVal;
                            break;
                        case "arg_set_id":
                            ArgSetId = longVal;
                            break;
                        case "track_id":
                            TrackId = longVal;
                            break;
                        case "parent_id":
                            ParentId = longVal;
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
                        case "type":
                            Type = strVal;
                            break;
                        case "category":
                            Category = strVal;
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
