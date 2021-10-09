// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;
using Utilities;

namespace PerfettoProcessor
{
    /// <summary>
    /// https://perfetto.dev/docs/analysis/sql-tables#args
    /// </summary>
    public class PerfettoArgEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoArgEvent";

        public const string SqlQuery = "select arg_set_id, flat_key, key, int_value, string_value, real_value, value_type from args order by arg_set_id";
        public uint ArgSetId { get; set; }
        public string Flatkey { get; set; }
        public string ArgKey { get; set; }
        public long? IntValue { get; set; }
        public string StringValue { get; set; }
        public double? RealValue { get; set; }
        public string ValueType { get; set; }

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
                            ArgSetId = (uint)longVal;
                            break;
                        case "int_value":
                            IntValue = longVal;
                            break;
                    }

                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellFloat64:
                    var floatVal = batch.Float64Cells[counters.FloatCounter++];
                    if (col == "real_value")
                    {
                        RealValue = floatVal;
                    }
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellString:
                    var strVal = Common.StringIntern(stringCells[counters.StringCounter++]);
                    switch (col)
                    {
                        case "flat_key":
                            Flatkey = strVal;
                            break;
                        case "key":
                            ArgKey = strVal;
                            break;
                        case "string_value":
                            StringValue = strVal;
                            break;
                        case "value_type":
                            ValueType = strVal;
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
