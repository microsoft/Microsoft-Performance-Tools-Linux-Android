// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;
using Utilities;

namespace PerfettoProcessor
{
    /// <summary>
    /// https://perfetto.dev/docs/analysis/sql-tables#raw
    /// </summary>
    public class PerfettoRawEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoRawEvent";

        public const string SqlQuery = "select ts, name, cpu, utid, arg_set_id from raw";
        public long Timestamp { get; set; }
        public long RelativeTimestamp { get; set; }
        public string Name { get; set; }
        public uint Cpu { get; set; }
        public uint Utid { get; set; }
        public uint ArgSetId { get; set; }

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
                            Utid = (uint)longVal;
                            break;
                        case "ts":
                            Timestamp = longVal;
                            break;
                        case "cpu":
                            Cpu = (uint)longVal;
                            break;
                        case "arg_set_id":
                            ArgSetId = (uint)longVal;
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
