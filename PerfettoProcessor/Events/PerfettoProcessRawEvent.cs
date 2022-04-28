// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;

namespace PerfettoProcessor
{
    /// <summary>
    /// https://perfetto.dev/docs/analysis/sql-tables#process
    /// </summary>
    public class PerfettoProcessRawEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoProcessRawEvent";

        public const string SqlQuery = "select upid, id, type, pid, name, start_ts, end_ts, parent_upid, uid, android_appid, cmdline, arg_set_id from process";
        public long Id { get; set; }
        public string Type { get; set; }
        public uint Upid { get; set; }
        public uint Pid { get; set; }
        public string Name { get; set; }
        public long? StartTimestamp { get; set; }
        public long? RelativeStartTimestamp { get; set; }
        public long? EndTimestamp{ get; set; }
        public long? RelativeEndTimestamp { get; set; }
        public uint? ParentUpid { get; set; }
        public uint? Uid { get; set; }
        public uint? AndroidAppId { get; set; }
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
                            Upid = (uint) longVal;
                            break;
                        case "id":
                            Id = longVal;
                            break;
                        case "pid":
                            Pid = (uint) longVal;
                            break;
                        case "uid":
                            Uid = (uint) longVal;
                            break;
                        case "parent_upid":
                            ParentUpid = (uint) longVal;
                            break;
                        case "android_appid":
                            AndroidAppId = (uint) longVal;
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
