// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;
using Utilities;

namespace PerfettoProcessor
{
    // https://perfetto.dev/docs/analysis/sql-tables#stack_profile_mapping
    public class PerfettoStackProfileMappingEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoStackProfileMappingEvent";

        public const string SqlQuery = "select id, type, build_id, start, end, name, exact_offset, start_offset, load_bias from stack_profile_mapping order by id";
        public int Id { get; set; }
        public string Type { get; set; }
        /// <summary>
        /// hex-encoded Build ID of the binary / library.
        /// </summary>
        public string BuildId { get; set; }
        /// <summary>
        /// start of the mapping in the process' address space.
        /// </summary>
        public long Start { get; set; }
        /// <summary>
        /// end of the mapping in the process' address space.
        /// </summary>
        public long End { get; set; }
        /// <summary>
        /// filename of the binary / library
        /// Joinable with profiler_smaps.path
        /// </summary>
        public string Name { get; set; }

        public long ExactOffset { get; set; }
        public long StartOffset { get; set; }
        public long LoadBias { get; set; }

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
                        case "start":
                            Start = longVal;
                            break;
                        case "end":
                            End = longVal;
                            break;
                        case "exact_offset":
                            ExactOffset = longVal;
                            break;
                        case "start_offset":
                            StartOffset = longVal;
                            break;
                        case "load_bias":
                            LoadBias = longVal;
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
                        case "build_id":
                            BuildId = strVal;
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
