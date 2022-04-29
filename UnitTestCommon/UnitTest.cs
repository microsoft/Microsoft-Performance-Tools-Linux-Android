// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTestCommon
{
    public static class UnitTest
    {
        public static void TestTableBuild(RuntimeExecutionResults runtimeExecutionResults, TableDescriptor tableDescriptor, int expectedCount, bool skipDataAvailableCheck = false)
        {
            var isTableDataAvailable = runtimeExecutionResults.IsTableDataAvailable(tableDescriptor) == true;

            if (skipDataAvailableCheck || isTableDataAvailable)
            {
                var tableResult = runtimeExecutionResults.BuildTable(tableDescriptor);
                Assert.IsTrue(tableResult.RowCount == expectedCount);
                var tableData = tableResult.GetDataForAllRows();
            }
            else if (!isTableDataAvailable && expectedCount > 0)
            {
                throw new Exception($"No data available for {tableDescriptor.Name} but we expected {expectedCount} rows");
            }
        }
    }
}
