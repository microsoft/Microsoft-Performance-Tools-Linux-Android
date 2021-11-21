// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;
using Utilities;

namespace PerfettoProcessor
{
    /// <summary>
    /// Symbolization data for a frame. Rows with the same symbol_set_id describe one callframe, with the most-inlined symbol having id == symbol_set_id.
    /// For instance, if the function foo has an inlined call to the function bar, which has an inlined call to baz, the stack_profile_symbol table would look like this.
    /// https://perfetto.dev/docs/analysis/sql-tables#stack_profile_symbol
    /// </summary>
    public class PerfettoStackProfileSymbolEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoStackProfileSymbol";

        public const string SqlQuery = "select id, type, name, source_file, line_number, symbol_set_id from stack_profile_symbol order by id";
        public int Id { get; set; }
        public string Type { get; set; }
        /// <summary>
        /// name of the function.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// name of the source file containing the function.
        /// </summary>
        public string SourceFile { get; set; }
        /// <summary>
        /// line number of the frame in the source file. This is the exact line for the corresponding program counter, not the beginning of the function.
        /// </summary>
        public uint LineNumber { get; set; }

        public uint SymbolSetId { get; set; }


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
                        case "line_number":
                            LineNumber = (uint)longVal;
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
                        case "source_file":
                            SourceFile = strVal;
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
