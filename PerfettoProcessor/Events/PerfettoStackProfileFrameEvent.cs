// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;
using Utilities;

namespace PerfettoProcessor
{
    // https://perfetto.dev/docs/analysis/sql-tables#stack_profile_frame
    public class PerfettoStackProfileFrameEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoStackProfileFrame";

        public const string SqlQuery = "select id, type, name, mapping, rel_pc, symbol_set_id, deobfuscated_name from stack_profile_frame order by id";
        public int Id { get; set; }
        public string Type { get; set; }
        /// <summary>
        /// name of the function this location is in.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// the mapping (library / binary) this location is in.
        /// </summary>
        public int Mapping { get; set; }
        /// <summary>
        /// the program counter relative to the start of the mapping.
        /// </summary>
        public long RelPc { get; set; }
        /// <summary>
        /// if the profile was offline symbolized, the offlinesymbol information of this frame
        /// Joinable with stack_profile_symbol.symbol_set_id
        /// </summary>
        public uint? SymbolSetId { get; set; }
        public string DeobfuscatedName { get; set; }


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
                        case "mapping":
                            Mapping = (int)longVal;
                            break;
                        case "rel_pc":
                            RelPc = longVal;
                            break;
                        case "symbol_set_id":
                            SymbolSetId = (uint)longVal;
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
                        case "type":
                            Type = strVal;
                            break;
                        case "deobfuscated_name":
                            DeobfuscatedName = strVal;
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
