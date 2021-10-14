using Perfetto.Protos;
using PerfettoProcessor;
using System;

namespace PerfettoProcessor
{
    public class PerfettoInstantEvent : PerfettoSqlEvent
    {
        public const string Key = "InstantEvent";
        public const string SqlQuery = "select id, type, ts, arg_set_id, name, ref, ref_type from instant order by id";

        public int Id { get; set; }
        public string Type { get; set; }
        public long Timestamp { get; set; }
        public int ArgSetId { get; set; }
        public string Name { get; set; }
        public long Reference { get; set; }
        public string ReferenceType { get; set; }

        public override string GetEventKey()
        {
            throw new System.NotImplementedException();
        }

        public override string GetSqlQuery()
        {
            throw new System.NotImplementedException();
        }

        public override void ProcessCell(string colName, QueryResult.Types.CellsBatch.Types.CellType cellType, QueryResult.Types.CellsBatch batch, string[] stringCells, CellCounters counters)
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
                            Timestamp = (int)longVal;
                            break;
                        case "ref":
                            Reference = longVal;
                            break;
                    }
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellFloat64:
                    var floatVal = batch.Float64Cells[counters.FloatCounter++];
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellString:
                    var strVal = stringCells[counters.StringCounter++];
                    switch (col)
                    {
                        case "type":
                            Type = strVal;
                            break;
                        case "name":
                            Type = strVal;
                            break;
                        case "ref_type":
                            ReferenceType = strVal;
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
