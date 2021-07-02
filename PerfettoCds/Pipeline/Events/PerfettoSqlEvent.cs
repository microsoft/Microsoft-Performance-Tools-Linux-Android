using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using System;
using Perfetto.Protos;

namespace PerfettoCds.Pipeline.Events
{
    /// <summary>
    /// Keep track of where we are in the varint, varfloat, and varstring arrays in a Perfetto batch result
    /// </summary>
    public class CellCounters
    {
        public int IntCounter { get; set; }
        public int StringCounter { get; set; }
        public int FloatCounter { get; set; }
    }

    /// <summary>
    /// Base class for a row in a Perfetto SQL table
    /// </summary>
    public abstract class PerfettoSqlEvent : IKeyedDataType<String>
    {
        public readonly string Key;

        public PerfettoSqlEvent(string key)
        {
            this.Key = key;
        }

        public int CompareTo(string other)
        {
            return this.Key.CompareTo(other);
        }

        public string GetKey()
        {
            return Key;
        }

        /// <summary>
        /// This method is responsible for processing an individual cell in a SQL query, which is stored in the QueryResult
        /// protobuf object (QueryResult.Batch.Cells)
        /// 
        /// CellType defines what type this cell is (int, string, float, ...) and the batch object contains
        /// separate data arrays for each of those types (varintcells[], varfloatcells[]). The counters object
        /// maintains the indices into each of these data arrays.
        /// 
        /// String cells need to be preprocessed by us because they come as a single string delimited by null characater.
        /// 
        /// </summary>
        /// <param name="colName">String name of the column</param>
        /// <param name="cellType">Type of cell</param>
        /// <param name="batch">Batch object that contains the data arrays</param>
        /// <param name="stringCells">All the string cells already split into an array</param>
        /// <param name="counters">Indexes into the data arrays</param>
        public abstract void ProcessCell(string colName,
            QueryResult.Types.CellsBatch.Types.CellType cellType,
            QueryResult.Types.CellsBatch batch,
            string[] stringCells,
            CellCounters counters);

        /// <summary>
        /// Returns the SQL query used to query this objects information
        /// </summary>
        /// <returns></returns>
        public abstract string GetSqlQuery();
    }

    public class PerfettoSliceEvent : PerfettoSqlEvent
    {
        public static string SqlQuery = "select ts, dur, arg_set_id, track_id, name, type, category from slice order by ts";
        public string Name { get; set; }
        public string Type { get; set; }
        public long Duration { get; set; }
        public long ArgSetId { get; set; }
        public Timestamp Timestamp { get; set; }
        public string Category { get; set; }
        public long TrackId { get; set; }

        public PerfettoSliceEvent() : base(PerfettoPluginConstants.SliceEvent)
        {

        }

        public override string GetSqlQuery()
        {
            return SqlQuery;
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
                    if (col == "ts")
                    {
                        Timestamp = new Timestamp(longVal);
                    }
                    else if (col == "dur")
                    {
                        Duration = longVal;
                    }
                    else if (col == "arg_set_id")
                    {
                        ArgSetId = longVal;
                    }
                    else if (col == "track_id")
                    {
                        TrackId = longVal;
                    }

                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellFloat64:
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellString:
                    var strVal = stringCells[counters.StringCounter++];
                    if (col == "name")
                    {
                        Name = strVal;
                    }
                    else if (col == "type")
                    {
                        Type = strVal;
                    }
                    else if (col == "category")
                    {
                        Category = strVal;
                    }
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellBlob:
                    break;
                default:
                    throw new Exception("Unexpected CellType");
            }
        }
    }

    public class PerfettoArgEvent : PerfettoSqlEvent
    {
        public static string SqlQuery = "select arg_set_id, flat_key, key, int_value, string_value, real_value, value_type from args order by arg_set_id";
        public long ArgSetId { get; set; }
        public string Flatkey { get; set; }
        public string ArgKey { get; set; }
        public long IntValue { get; set; }
        public string StringValue { get; set; }
        public double RealValue { get; set; }
        public string ValueType { get; set; }


        public PerfettoArgEvent() : base(PerfettoPluginConstants.ArgEvent)
        {

        }

        public override string GetSqlQuery()
        {
            return SqlQuery;
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
                    if (col == "arg_set_id")
                    {
                        ArgSetId = longVal;
                    }
                    else if (col == "int_value")
                    {
                        IntValue = longVal;
                    }

                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellFloat64:
                    var floatVal = batch.Float64Cells[counters.FloatCounter++];
                    if (col == "real_value")
                    {
                        RealValue = floatVal;
                    }
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellString:
                    var strVal = stringCells[counters.StringCounter++];
                    if (col == "flat_key")
                    {
                        Flatkey = strVal;
                    }
                    else if (col == "key")
                    {
                        ArgKey = strVal;
                    }
                    else if (col == "string_value")
                    {
                        StringValue = strVal;
                    }
                    else if (col == "value_type")
                    {
                        ValueType = strVal;
                    }
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellBlob:
                    break;
                default:
                    throw new Exception("Unexpected CellType");
            }
        }
    }

    public class PerfettoThreadTrackEvent : PerfettoSqlEvent
    {
        public static string SqlQuery = "select id, type, name, source_arg_set_id, utid from thread_track";
        public long ArgSetId { get; set; }
        public long Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public long SourceArgSetId { get; set; }
        public long Utid { get; set; }

        public PerfettoThreadTrackEvent() : base(PerfettoPluginConstants.ThreadTrackEvent)
        {

        }

        public override string GetSqlQuery()
        {
            return SqlQuery;
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
                    if (col == "arg_set_id")
                    {
                        ArgSetId = longVal;
                    }
                    else if (col == "id")
                    {
                        Id = longVal;
                    }
                    else if (col == "utid")
                    {
                        Utid = longVal;
                    }
                    else if (col == "source_arg_set_id")
                    {
                        SourceArgSetId = longVal;
                    }

                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellFloat64:
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellString:
                    var strVal = stringCells[counters.StringCounter++];
                    if (col == "type")
                    {
                        Type = strVal;
                    }
                    else if (col == "name")
                    {
                        Name = strVal;
                    }
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellBlob:
                    break;
                default:
                    throw new Exception("Unexpected CellType");
            }
        }
    }

    public class PerfettoThreadEvent : PerfettoSqlEvent
    {
        public static string SqlQuery = "select utid, id, type, tid, name, start_ts, end_ts, upid, is_main_thread from thread";
        public long Utid { get; set; }
        public long Id { get; set; }
        public string Type { get; set; }
        public long Tid { get; set; }
        public string Name{ get; set; }
        public Timestamp StartTimestamp { get; set; }
        public Timestamp EndTimestamp{ get; set; }
        public long Upid { get; set; }
        public long IsMainThread{ get; set; }

        public PerfettoThreadEvent() : base(PerfettoPluginConstants.ThreadEvent)
        {

        }

        public override string GetSqlQuery()
        {
            return SqlQuery;
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
                    if (col == "utid")
                    {
                        Utid = longVal;
                    }
                    else if (col == "id")
                    {
                        Id = longVal;
                    }
                    else if (col == "utid")
                    {
                        Utid = longVal;
                    }
                    else if (col == "upid")
                    {
                        Upid = longVal;
                    }
                    else if (col == "tid")
                    {
                        Tid = longVal;
                    }
                    else if (col == "is_main_thread")
                    {
                        IsMainThread = longVal;
                    }
                    else if (col == "start_ts")
                    {
                        StartTimestamp = new Timestamp(longVal);
                    }
                    else if (col == "end_ts")
                    {
                        EndTimestamp = new Timestamp(longVal);
                    }

                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellFloat64:
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellString:
                    var strVal = stringCells[counters.StringCounter++];
                    if (col == "type")
                    {
                        Type = strVal;
                    }
                    else if (col == "name")
                    {
                        Name = strVal;
                    }
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellBlob:
                    break;
                default:
                    throw new Exception("Unexpected CellType");
            }
        }
    }

    public class PerfettoProcessEvent : PerfettoSqlEvent
    {
        public static string SqlQuery = "select upid, id, type, pid, name, start_ts, end_ts, parent_upid, uid, android_appid, cmdline, arg_set_id from process";
        public long Upid { get; set; }
        public long Id { get; set; }
        public string Type { get; set; }
        public long Pid { get; set; }
        public string Name { get; set; }
        public Timestamp StartTimestamp { get; set; }
        public Timestamp EndTimestamp{ get; set; }
        public long ParentUpid { get; set; }
        public long Uid { get; set; }
        public long AndroidAppId { get; set; }
        public string CmdLine { get; set; }
        public long ArgSetId { get; set; }

        public PerfettoProcessEvent() : base(PerfettoPluginConstants.ProcessEvent)
        {

        }

        public override string GetSqlQuery()
        {
            return SqlQuery;
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
                    if (col == "upid")
                    {
                        Upid = longVal;
                    }
                    else if (col == "id")
                    {
                        Id = longVal;
                    }
                    else if (col == "pid")
                    {
                        Pid = longVal;
                    }
                    else if (col == "upid")
                    {
                        Upid = longVal;
                    }
                    else if (col == "parent_upid")
                    {
                        ParentUpid = longVal;
                    }
                    else if (col == "android_appid")
                    {
                        AndroidAppId = longVal;
                    }
                    else if (col == "arg_set_id")
                    {
                        ArgSetId = longVal;
                    }
                    else if (col == "start_ts")
                    {
                        StartTimestamp = new Timestamp(longVal);
                    }
                    else if (col == "end_ts")
                    {
                        EndTimestamp = new Timestamp(longVal);
                    }

                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellFloat64:
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellString:
                    var strVal = stringCells[counters.StringCounter++];
                    if (col == "type")
                    {
                        Type = strVal;
                    }
                    else if (col == "cmdline")
                    {
                        CmdLine = strVal;
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
