using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnitTestCommon
{
    public static class TableBuilderTests
    {
        public static bool TestRowTypesMatchColTypes(ITableBuilderWithRowCount tbr, int rowNumber)
        {
            for (var i = 0; i < tbr.Columns.Count; i++)
            {
                var col = tbr.Columns.ElementAt(i);
                try
                {
                    var projResult = col.Project(rowNumber);

                    if (projResult != null)
                    {
                        var projResultType = projResult.GetType();
                        throw new InvalidDataException($"Column DataType {col.DataType} does not match projected type {projResultType}");
                    }
                }
                catch (Exception)
                {

                }
            }

            return true;
        }
    }
}
