// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PerfDataExtensions.Tables;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using UnitTestCommon;

namespace PerfUnitTest
{
    [TestClass]
    public class PerfUnitTest
    {
        [TestMethod]
        public void ProcessPerfCpuClock()
        {
            // Input data
            string[] perfData = { @"..\..\..\..\TestData\Perf\perf.data.txt" };
            var perfDataPath = new FileInfo(perfData[0]);
            Assert.IsTrue(perfDataPath.Exists);

            var perfDataPathFullPath = perfDataPath.FullName;
            var datasource = new Mock<IDataSource>();
            datasource.Setup(ds => ds.Uri).Returns(new Uri(perfDataPathFullPath));

            // Env
            var appEnv = new Mock<IApplicationEnvironment>();
            var serializer = new Mock<ISerializer>();
            appEnv.Setup(ae => ae.Serializer).Returns(serializer.Object);
            var processorEnv = new Mock<IProcessorEnvironment>();

            // DataSource and Processor
            var perfDataCustomDataSource = new PerfDataCustomDataSource.PerfDataCustomDataSource();
            perfDataCustomDataSource.SetApplicationEnvironment(appEnv.Object);

            var perfDataCustomDataSourceProcessor = perfDataCustomDataSource.CreateProcessor(datasource.Object, processorEnv.Object, ProcessorOptions.Default);
            var perfDataCustomDataSourceProcessorTask = perfDataCustomDataSourceProcessor.ProcessAsync(new Progress(), new CancellationToken());
            perfDataCustomDataSourceProcessorTask.GetAwaiter().GetResult();
            Assert.IsTrue(perfDataCustomDataSourceProcessorTask.IsCompleted && !perfDataCustomDataSourceProcessorTask.IsFaulted);

            var dataSourceInfo = perfDataCustomDataSourceProcessor.GetDataSourceInfo();
            Assert.IsTrue(dataSourceInfo.FirstEventTimestampNanoseconds >= 0 && dataSourceInfo.EndTimestampNanoseconds >= 0);

            // Build table
            var tableBuilder = new TableBuilder();
            perfDataCustomDataSourceProcessor.BuildTable(PerfTxtCpuSamplingTable.TableDescriptor, tableBuilder);
            var tbr = tableBuilder.TableBuilderWithRowCount;

            TableBuilderTests.TestRowTypesMatchColTypes(tbr, 0);

            var rowNumber = 2;
            // Sample #
            var sampleNumber = (long)tbr.Columns.ElementAt(0).Project(rowNumber);
            Assert.IsTrue(sampleNumber == 2);

            // Timestamp
            var ts = (Timestamp) tbr.Columns.ElementAt(1).Project(rowNumber);
            Assert.IsTrue(ts == new Timestamp(27000));

            // IP
            var ip = (string)tbr.Columns.ElementAt(2).Project(rowNumber);
            Assert.IsTrue(ip == "is_prime");

            // IPModule
            var ipModule = (string) tbr.Columns.ElementAt(3).Project(rowNumber);
            Assert.IsTrue(ipModule == "stress-ng");

            // Process
            var process = (string)tbr.Columns.ElementAt(7).Project(rowNumber);
            // Assert.IsTrue(process == "Process stress-ng-cpu (7499)");  // TODO - Figure this out - some sort of race condition not present in UI. Sometimes this populates, sometimes not

            // CPU
            var cpu = (int)tbr.Columns.ElementAt(13).Project(rowNumber);
            Assert.IsTrue(cpu == 4);

            // Callstack
            var callstack = (string[]) tbr.Columns.ElementAt(14).Project(rowNumber);
            Assert.IsTrue(callstack[0] == "stress-ng!is_prime");
        }
    }
}
