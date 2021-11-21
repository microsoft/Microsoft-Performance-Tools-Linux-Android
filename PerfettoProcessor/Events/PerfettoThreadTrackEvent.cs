// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;

namespace PerfettoProcessor
{
    /// <summary>
    /// https://perfetto.dev/docs/analysis/sql-tables#thread_track
    /// </summary>
    public class PerfettoThreadTrackEvent : PerfettoTrackEvent
    {
        public new const string Key = "PerfettoThreadTrackEvent";

        public new const string SqlQuery = "select id, type, name, source_arg_set_id, utid from thread_track";

        public long Utid { get; set; }

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
                        case "utid":
                            Utid = longVal;
                            break;
                        case "source_arg_set_id":
                            SourceArgSetId = (uint) longVal;
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
