// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Engine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitTestCommon
{
    public class ColumnsWithData
    {
        public IEnumerable<string> ColumnNames{ get; set; }
        public object[][] Data { get; set; }
    }

    public static class ITableResultExts
    {
        public static ColumnsWithData GetDataForAllRows(this ITableResult tableResult)
        {
            var allColumns = tableResult.Columns.Select(f => f.Configuration.Metadata);
            var columnsWithData = new ColumnsWithData();
            columnsWithData.ColumnNames = allColumns.Select(f => f.Name).ToArray();
            columnsWithData.Data = GetDataForAllRows(tableResult, allColumns.Select(f => f.Guid).ToArray());
            return columnsWithData;
        }

        public static object[][] GetDataForAllRows(this ITableResult tableResult, Guid[] columns)
        {
            var allRowData = new object[tableResult.RowCount][];
            for (int rowNum = 0; rowNum < tableResult.RowCount; rowNum++)
            {
                allRowData[rowNum] = GetRowData(tableResult, columns, rowNum);
            }
            return allRowData;
        }

        public static object[] GetRowData(this ITableResult tableResult, Guid[] columns, int row)
        {   
            var rowData = new object[columns.Length];
            for(int colIdx = 0; colIdx < columns.Length; colIdx++)
            {
                var col = tableResult.Columns.Single(f => f.Configuration.Metadata.Guid == columns[colIdx]);

                if (col.ProjectorInterface.Name != "PercentGenerator`2") // % col.Projector.DependsOnVisibleDomain = true (not easily accessible) and we haven't set this
                {
                    rowData[colIdx] = col.Project(row);
                }
            }

            return rowData;
        }
    }
}
