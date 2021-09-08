// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;

namespace PerfettoProcessor
{
    public class PerfettoProcessTrackEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoProcessTrackEvent";

        public static string SqlQuery = "select id, type, name, source_arg_set_id, upid from process_track";
        public long ArgSetId { get; set; }
        public long Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public long SourceArgSetId { get; set; }
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
                        case "arg_set_id":
                            ArgSetId = longVal;
                            break;
                        case "id":
                            Id = longVal;
                            break;
                        case "upid":
                            Upid = longVal;
                            break;
                        case "source_arg_set_id":
                            SourceArgSetId = longVal;
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
