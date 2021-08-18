// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;

namespace PerfettoProcessor
{
    public class PerfettoAndroidLogEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoAndroidLogEvent";

        public static string SqlQuery = "select ts, prio, tag, msg, utid from android_logs";
        public long Timestamp { get; set; }
        public long RelativeTimestamp { get; set; }
        public long Priority { get; set; }
        public string PriorityString { get; set; }
        public string Tag { get; set; }
        public string  Message { get; set; }
        public long Utid { get; set; }

        public override string GetSqlQuery()
        {
            return SqlQuery;
        }

        public override string GetEventKey()
        {
            return Key;
        }

        // Priority codes gathered from AndroidLogPriority in Perfetto repo
        private static readonly string[] PriorityToString = new string[8] { "Unspecified", "Unusued", "Verbose", "Debug", "Info", "Warn", "Error", "Fatal" };

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
                            Utid = longVal;
                            break;
                        case "ts":
                            Timestamp = longVal;
                            break;
                        case "prio":
                            Priority = longVal;
                            if (Priority >= 0 && Priority < PriorityToString.Length)
                            {
                                PriorityString = PriorityToString[longVal];
                            }
                            else
                            {
                                PriorityString = longVal.ToString();
                            }
                            break;
                    }

                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellFloat64:
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellString:
                    var strVal = stringCells[counters.StringCounter++];
                    switch (col)
                    {
                        case "tag":
                            Tag = strVal;
                            break;
                        case "msg":
                            Message = strVal;
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
