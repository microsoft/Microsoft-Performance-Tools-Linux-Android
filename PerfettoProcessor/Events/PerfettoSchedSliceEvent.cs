using Perfetto.Protos;
using System;
using System.Text;
using Utilities;

namespace PerfettoProcessor
{
    public class PerfettoSchedSliceEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoSchedSliceEvent";

        public const string SqlQuery = "select utid, ts, dur, cpu, end_state, priority from sched_slice";

        public int Utid { get; set; }
        public long Timestamp { get; set; }
        public long RelativeTimestamp { get; set; }
        public long Duration { get; set; }
        public uint Cpu { get; set; }
        public string EndStateCode { get; set; }
        public string EndStateStr { get; set; }
        public int Priority { get; set; }

        public override string GetEventKey()
        {
            return Key;
        }

        public override string GetSqlQuery()
        {
            return SqlQuery;
        }

        /// <summary>
        /// End state is returned as a code. Use the table found in perfetto-repo/docs/data-sources/cpu-scheduling
        /// to make it human readable. There can be multiple characters in a row.
        /// </summary>
        /// <param name="code"></param>
        private void SetEndState(string code)
        {
            EndStateCode = code;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < code.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(" ");
                }
                var x = code[i];
                switch (x)
                {
                    case 'R':
                        sb.Append("Runnable");
                        break;
                    case 'S':
                        sb.Append("Sleeping");
                        break;
                    case 'D':
                        sb.Append("Uninterruptible Sleep");
                        break;
                    case 'T':
                        sb.Append("Stopped");
                        break;
                    case 't':
                        sb.Append("Traced");
                        break;
                    case 'X':
                        sb.Append("Exit (Dead)");
                        break;
                    case 'Z':
                        sb.Append("Exit (Zombie)");
                        break;
                    case 'x':
                        sb.Append("Task Dead");
                        break;
                    case 'I':
                        sb.Append("Task Dead");
                        break;
                    case 'K':
                        sb.Append("Wake Kill");
                        break;
                    case 'W':
                        sb.Append("Waking");
                        break;
                    case 'P':
                        sb.Append("Parked");
                        break;
                    case 'N':
                        sb.Append("No Load");
                        break;
                    case '+':
                        sb.Append("(Preempted)");
                        break;
                    default:
                        sb.Append(x);
                        break;
                }
            }
            EndStateStr = Common.StringIntern(sb.ToString());
        }

        public override void ProcessCell(string colName, QueryResult.Types.CellsBatch.Types.CellType cellType, QueryResult.Types.CellsBatch batch, string[] stringCells, CellCounters counters)
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
                            Utid = (int)longVal;
                            break;
                        case "ts":
                            Timestamp = longVal;
                            break;
                        case "dur":
                            Duration = longVal;
                            break;
                        case "cpu":
                            Cpu = (uint)longVal;
                            break;
                        case "priority":
                            Priority = (int)longVal;
                            break;
                    }

                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellFloat64:
                    var floatVal = batch.Float64Cells[counters.FloatCounter++];
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellString:
                    var strVal = stringCells[counters.StringCounter++];
                    switch (col)
                    {
                        case "end_state":
                            SetEndState(strVal);
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
