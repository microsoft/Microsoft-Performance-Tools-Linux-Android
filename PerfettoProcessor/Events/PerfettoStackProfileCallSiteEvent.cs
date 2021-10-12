// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;
using Utilities;

namespace PerfettoProcessor
{
    // https://perfetto.dev/docs/analysis/sql-tables#stack_profile_callsite
    public class PerfettoStackProfileCallSiteEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoStackProfileCallSite";

        public const string SqlQuery = "select id, type, depth, parent_id, frame_id from stack_profile_callsite order by id";
        public int Id { get; set; }
        public string Type { get; set; }
        /// <summary>
        /// distance from the bottom-most frame of the callstack.
        /// </summary>
        public uint Depth { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int? ParentId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int FrameId { get; set; }


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
                        case "depth":
                            Depth = (uint)longVal;
                            break;
                        case "parent_id":
                            ParentId = (int)longVal;
                            break;
                        case "frame_id":
                            FrameId = (int)longVal;
                            break;
                    }

                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellFloat64:
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellString:
                    var strVal = Common.StringIntern(stringCells[counters.StringCounter++]);
                    switch (col)
                    {
                        case "type":
                            Type = strVal;
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
