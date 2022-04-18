// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;
using Utilities;

namespace PerfettoProcessor
{
    /// <summary>
    /// https://perfetto.dev/docs/analysis/sql-tables#expected_frame_timeline_slice
    /// </summary>
    public class PerfettoExpectedFrameEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoExpectedFrameEvent";

        public const string SqlQuery = "select id, ts, dur, arg_set_id, track_id, name, type, category, parent_id, display_frame_token, surface_frame_token, layer_name, upid from expected_frame_timeline_slice order by id";
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        /// <summary>
        /// duration of the slice (in nanoseconds)
        /// </summary>
        public long Duration { get; set; }
        /// <summary>
        /// Joinable with args.arg_set_id
        /// </summary>
        public uint ArgSetId { get; set; }
        /// <summary>
        /// timestamp of the start of the slice (in nanoseconds)
        /// </summary>
        public long Timestamp { get; set; }
        public long RelativeTimestamp { get; set; }
        public string Category { get; set; }
        public int TrackId { get; set; }
        public int? ParentId { get; set; }
        public int DisplayFrameToken { get; set; }
        public int SurfaceFrameToken { get; set; }
        public string LayerName { get; set; }
        public uint Upid { get; set; }

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
                        case "ts":
                            Timestamp = longVal;
                            break;
                        case "dur":
                            Duration = longVal;
                            break;
                        case "arg_set_id":
                            ArgSetId = (uint)longVal;
                            break;
                        case "track_id":
                            TrackId = (int)longVal;
                            break;
                        case "parent_id":
                            ParentId = (int)longVal;
                            break;
                        case "display_frame_token":
                            DisplayFrameToken = (int)longVal;
                            break;
                        case "surface_frame_token":
                            SurfaceFrameToken = (int)longVal;
                            break;
                        case "upid":
                            Upid = (uint)longVal;
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
                        case "category":
                            Category = strVal;
                            break;
                        case "layer_name":
                            LayerName = strVal;
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
