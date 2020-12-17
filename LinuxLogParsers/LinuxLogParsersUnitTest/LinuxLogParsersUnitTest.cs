using DmesgIsoMPTAddin;
using LinuxLogParser.CloudInitLog;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnitTestCommon;

namespace LinuxLogParsersUnitTest
{
    [TestClass]
    public class LinuxLogParsersUnitTest
    {
        [TestMethod]
        public void Dmesg()
        {
            // Input data
            string[] dmesgData = { @"..\..\..\..\..\TestData\LinuxLogs\Dmesg\dmesg.iso.log" };
            var dmesgDataPath = new FileInfo(dmesgData[0]);
            Assert.IsTrue(dmesgDataPath.Exists);

            var runtime = Engine.Create();
            runtime.AddFile(dmesgDataPath.FullName);

            runtime.EnableCooker(new DmesgIsoDataCooker().Path);

            var runtimeExecutionResults = runtime.Process();
            var cooker = runtime.SourceDataCookers.Where(c => c.DataCookerId == DmesgIsoDataCooker.CookerId).First();

            var eventData = runtimeExecutionResults.QueryOutput<DmesgIsoLogParsedResult>(
                new DataOutputPath(
                    cooker,
                    nameof(DmesgIsoDataCooker.ParsedResult)));

            //var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<IDiagnosticMessage>>(
            //    new DataOutputPath(
            //        LttngDmesgDataCookerPath,
            //        nameof(LttngDmesgDataCooker.DiagnosticMessages)));

            Assert.IsTrue(eventData.LogEntries.Count == 0); // TODO - UT - Trace has no DiagMessages

            // Option 2 - UI - not working
            //var dmesgDataPathFullPath = dmesgDataPath.FullName;
            //var datasource = new Mock<IDataSource>();
            //datasource.Setup(ds => ds.GetUri()).Returns(new Uri(dmesgDataPathFullPath));

            // var appEnv = new Mock<IApplicationEnvironment>();
            // var serializer = new Mock<ISerializer>();
            // appEnv.Setup(ae => ae.Serializer).Returns(serializer.Object);
            // appEnv.Setup(ae => ae.SourceSessionFactory.CreateSourceSession(It.IsAny<IKeyedDataType<TKey>, TConte>)
            // var processorEnv = new Mock<IProcessorEnvironment>();

            //// this.SourceProcessingSession = this.ApplicationEnvironment.SourceSessionFactory.CreateSourceSession(this);
            // // this.extensibilitySupport = this.ProcessorEnvironment.CreateDataProcessorExtensibilitySupport(this);

            // // DataSource and Processor
            // var dmesgIsoDataSource = new DmesgIsoDataSource();
            // dmesgIsoDataSource.SetApplicationEnvironment(appEnv.Object);

            // var dmesgIsoDataSourceProcessor = dmesgIsoDataSource.CreateProcessor(datasource.Object, processorEnv.Object, ProcessorOptions.Default);
            // var dmesgIsoDataSourceProcessorTask = dmesgIsoDataSourceProcessor.ProcessAsync(new Progress(), new CancellationToken());
            // dmesgIsoDataSourceProcessorTask.GetAwaiter().GetResult();
            // Assert.IsTrue(dmesgIsoDataSourceProcessorTask.IsCompleted && !dmesgIsoDataSourceProcessorTask.IsFaulted);

            // var dataSourceInfo = dmesgIsoDataSourceProcessor.GetDataSourceInfo();
            // Assert.IsTrue(dataSourceInfo.FirstEventTimestampNanoseconds >= 0 && dataSourceInfo.EndTimestampNanoseconds >= 0);

            // // Build table
            // var tableBuilder = new TableBuilder();
            // dmesgIsoDataSourceProcessor.BuildTable(DmesgIsoMPTAddin.Tables.ParsedTable.TableDescriptor, tableBuilder);
            // var tbr = tableBuilder.TableBuilderWithRowCount;

            // TableBuilderTests.TestRowTypesMatchColTypes(tbr, 0);
        }

        [TestMethod]
        public void CloudInit()
        {
        }

        [TestMethod]
        public void WaLinuxAgent()
        {
        }
    }
}
