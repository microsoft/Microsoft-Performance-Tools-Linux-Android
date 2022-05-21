using DotNetEventPipe.Tables;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using UnitTestCommon;

namespace DotnetEventpipeTest
{
    [TestClass]
    public class DotnetTraceTests
    {
        public static bool IsTraceProcessed = false;
        public static object IsTraceProcessedLock = new object();

        private static RuntimeExecutionResults RuntimeExecutionResults;


        public static void ProcessTrace()
        {
            lock (IsTraceProcessedLock)
            {
                if (!IsTraceProcessed)
                {
                    // Input data
                    string[] dotnetTraceData = { @"..\..\..\..\TestData\Dotnet-Trace\dotnet_20220517_102703.nettrace" };
                    var dotnetTraceDataPath = new FileInfo(dotnetTraceData[0]);
                    Assert.IsTrue(dotnetTraceDataPath.Exists);

                    // Approach #1 - Engine - Doesn't test tables UI but tests processing
                    var runtime = Engine.Create(new FileDataSource(dotnetTraceDataPath.FullName));

                    // Enable our various types of data
                    // Need to use cookers for this to work

                    // Enable tables used by UI
                    runtime.EnableTable(CpuSamplingTable.TableDescriptor);
                    runtime.EnableTable(GenericEventTable.TableDescriptor);
                    runtime.EnableTable(ExceptionTable.TableDescriptor);
                    runtime.EnableTable(GCTable.TableDescriptor);

                    //
                    // Process our data.
                    //

                    RuntimeExecutionResults = runtime.Process();
                    UnitTest.TestTableBuild(RuntimeExecutionResults, CpuSamplingTable.TableDescriptor, 103800);
                    UnitTest.TestTableBuild(RuntimeExecutionResults, GenericEventTable.TableDescriptor, 26104); // Wrong??
                    UnitTest.TestTableBuild(RuntimeExecutionResults, ExceptionTable.TableDescriptor, 2);
                    UnitTest.TestTableBuild(RuntimeExecutionResults, GCTable.TableDescriptor, 12);

                    IsTraceProcessed = true;
                }
            }
        }

        [TestMethod]
        public void DotnetTraceTest()
        {
            ProcessTrace();
        }
    }
}
