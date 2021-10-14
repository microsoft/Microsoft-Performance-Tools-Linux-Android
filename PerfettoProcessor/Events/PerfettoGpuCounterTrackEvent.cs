// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;
using Utilities;

namespace PerfettoProcessor
{
    public class PerfettoGpuCounterTrackEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoGpuCounterTrackEvent";

        public const string SqlQuery = "select name, id from gpu_counter_track";
        public int Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public string Description { get; set; }
        public uint GpuId { get; set; }
        public uint? SourceArgSetId { get; set; }

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
                        case "source_arg_set_id":
                            SourceArgSetId = (uint)longVal;
                            break;
                        case "gpu_id":
                            GpuId = (uint)longVal;
                            break;
                    }
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellFloat64:
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellString:
                    var strVal = Common.StringIntern(stringCells[counters.StringCounter++]);
                    switch (col)
                    {
                        case "name":
                            Name = strVal;
                            break;
                        case "unit":
                            Unit = strVal;
                            break;
                        case "description":
                            Description = strVal;
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
