// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;

namespace PerfettoProcessor
{
    /// <summary>
    /// https://perfetto.dev/docs/analysis/sql-tables#process
    /// </summary>
    public class PerfettoProcessEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoProcessEvent";

        public const string SqlQuery = "select upid, id, type, pid, name, start_ts, end_ts, parent_upid, uid, android_appid, cmdline, arg_set_id from process";
        public long Upid { get; set; }
        public long Id { get; set; }
        public string Type { get; set; }
        public long Pid { get; set; }
        public string Name { get; set; }
        public long? StartTimestamp { get; set; }
        public long? RelativeStartTimestamp { get; set; }
        public long? EndTimestamp{ get; set; }
        public long? RelativeEndTimestamp { get; set; }
        public long? ParentUpid { get; set; }
        public long? Uid { get; set; }
        public long? AndroidAppId { get; set; }
        public string CmdLine { get; set; }
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
                        case "upid":
                            Upid = longVal;
                            break;
                        case "id":
                            Id = longVal;
                            break;
                        case "pid":
                            Pid = longVal;
                            break;
                        case "uid":
                            Uid = longVal;
                            break;
                        case "parent_upid":
                            ParentUpid = longVal;
                            break;
                        case "android_appid":
                            AndroidAppId = longVal;
                            break;
                        case "arg_set_id":
                            ArgSetId = (uint)longVal;
                            break;
                        case "start_ts":
                            StartTimestamp = longVal;
                            break;
                        case "end_ts":
                            EndTimestamp = longVal;
                            break;
                    }

                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellFloat64:
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellString:
                    var strVal = stringCells[counters.StringCounter++];
                    switch (col)
                    {
                        case "type":
                            Type = strVal;
                            break;
                        case "cmdline":
                            CmdLine = strVal;
                            break;
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
