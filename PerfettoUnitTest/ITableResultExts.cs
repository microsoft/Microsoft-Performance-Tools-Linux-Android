using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PerfettoUnitTest
{
    public static class ITableResultExts
    {
        public static object[][] GetDataForAllRows(this ITableResult tableResult)
        {
            var allColumns = tableResult.Columns.Select(f => f.Configuration.Metadata.Guid).ToArray();
            return GetDataForAllRows(tableResult, allColumns);
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
