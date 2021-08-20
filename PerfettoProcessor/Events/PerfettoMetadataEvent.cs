// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;

namespace PerfettoProcessor
{
    /// <summary>
    /// metadata table contains a name/value pair style of trace metadata
    /// https://perfetto.dev/docs/analysis/sql-tables#metadata
    /// </summary>
    public class PerfettoMetadataEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoMetadataEvent";

        public static string SqlQuery = "select name, key_type, int_value, str_value from metadata";
        public string Name { get; set; }
        public string KeyType { get; set; }

        public long IntValue { get; set; }
        public string StrValue { get; set; }


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
                        case "int_value":
                            IntValue = longVal;
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
                        case "key_type":
                            KeyType = strVal;
                            break;
                        case "str_value":
                            StrValue = strVal;
                            break;
                    }

                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellBlob:
                    break;
                default:
                    throw new Exception($"Unexpected CellType {col} {cellType}");
            }
        }
    }
}
