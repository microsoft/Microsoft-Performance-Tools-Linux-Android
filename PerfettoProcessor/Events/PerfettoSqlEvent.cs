// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Perfetto.Protos;

namespace PerfettoProcessor
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
    public abstract class PerfettoSqlEvent
    {
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
        /// For more information, see comments for QueryResult and CellsBatch inside TraceProcessor.cs
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
}
