// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using LttngCds;
using LttngDataExtensions.SourceDataCookers;
using LttngDataExtensions.DataOutputTypes;
using LttngDataExtensions.SourceDataCookers.Syscall;
using LttngDataExtensions.SourceDataCookers.Thread;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using UnitTestCommon;
using LttngDataExtensions.SourceDataCookers.Diagnostic_Messages;
using Microsoft.Performance.SDK;
using LttngDataExtensions.SourceDataCookers.Module;
using LttngDataExtensions.SourceDataCookers.Disk;

namespace LTTngDataExtUnitTest
{
    [TestClass]
    public class LTTngUnitTest
    {
        public static bool IsTraceProcessed = false;
        public static object IsTraceProcessedLock = new object();

        private static RuntimeExecutionResults RuntimeExecutionResults;

        private static DataCookerPath LTTngGenericEventDataCookerPath;
        private static DataCookerPath LTTngSyscallDataCookerPath;
        private static DataCookerPath LTTngThreadDataCookerPath;
        private static DataCookerPath LttngDmesgDataCookerPath;
        private static DataCookerPath LttngModuleDataCookerPath;
        private static DataCookerPath LttngDiskDataCookerPath;

        public static void ProcessTrace()
        {
            lock (IsTraceProcessedLock)
            {
                if (!IsTraceProcessed)
                {
                    // Input data
                    string[] lttngData = { @"..\..\..\..\TestData\LTTng\lttng-kernel-trace.ctf" };
                    var lttngDataPath = new FileInfo(lttngData[0]);
                    Assert.IsTrue(lttngDataPath.Exists);

                    // Approach #1 - Engine - Doesn't test tables UI but tests processing
                    var runtime = Engine.Create();

                    runtime.AddFile(lttngDataPath.FullName);

                    // Enable our various types of data
                    var lttngGenericEventDataCooker = new LttngGenericEventDataCooker();
                    LTTngGenericEventDataCookerPath = lttngGenericEventDataCooker.GetCookerPath();
                    runtime.EnableCooker(LTTngGenericEventDataCookerPath);

                    var lttngSyscallDataCooker = new LttngSyscallDataCooker();
                    LTTngSyscallDataCookerPath = lttngSyscallDataCooker.GetCookerPath();
                    runtime.EnableCooker(LTTngSyscallDataCookerPath);

                    var lttngThreadDataCooker = new LttngThreadDataCooker();
                    LTTngThreadDataCookerPath = lttngThreadDataCooker.GetCookerPath();
                    runtime.EnableCooker(LTTngThreadDataCookerPath);

                    var lttngDmesgDataCooker = new LttngDmesgDataCooker();
                    LttngDmesgDataCookerPath = lttngDmesgDataCooker.GetCookerPath();
                    runtime.EnableCooker(LttngDmesgDataCookerPath);

                    var lttngModuleDataCooker = new LttngModuleDataCooker();
                    LttngModuleDataCookerPath = lttngModuleDataCooker.GetCookerPath();
                    runtime.EnableCooker(LttngModuleDataCookerPath);

                    var lttngDiskDataCooker = new LttngDiskDataCooker();
                    LttngDiskDataCookerPath = lttngDiskDataCooker.GetCookerPath();
                    runtime.EnableCooker(LttngDiskDataCookerPath);

                    //
                    // Process our data.
                    //

                    RuntimeExecutionResults = runtime.Process();

                    // ##########################################################
                    // Approach #2 (not currently working but if it does should be able to test tables)
                    // ##########################################################
                    var lttngDataPathFullPath = lttngDataPath.FullName;
                    var datasource = new Mock<IDataSource>();
                    datasource.Setup(ds => ds.GetUri()).Returns(new Uri(lttngDataPathFullPath));

                    // Env
                    var appEnv = new Mock<IApplicationEnvironment>();
                    var serializer = new Mock<ISerializer>();
                    appEnv.Setup(ae => ae.Serializer).Returns(serializer.Object);
                    var processorEnv = new Mock<IProcessorEnvironment>();

                    var lttngDataSource = new LttngDataSource();
                    lttngDataSource.SetApplicationEnvironment(appEnv.Object);
                    //var lttngDataProcessor = lttngDataSource.CreateProcessor(datasource.Object, processorEnv.Object, ProcessorOptions.Default); // <= PROBLEM HERE

                    //var dataExtensionRetrieval = new Mock<IDataExtensionRetrieval>();
                    var tableBuilder = new TableBuilder();
                    // LttngDataExtensions.Tables.DiskTable.BuildTable(tableBuilder, 

                    IsTraceProcessed = true;
                }
            }
        }

        [TestMethod]
        public void DiagnosticMessageTable()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<IDiagnosticMessage>>(
                new DataOutputPath(
                    LttngDmesgDataCookerPath,
                    nameof(LttngDmesgDataCooker.DiagnosticMessages)));

            Assert.IsTrue(eventData.Count == 0); // TODO - UT - Trace has no DiagMessages
        }

        [TestMethod]
        public void DiskTable()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<DiskActivity>>(
                new DataOutputPath(
                    LttngDiskDataCookerPath,
                    nameof(LttngDiskDataCooker.DiskActivity)));

            Assert.IsTrue(eventData.Count > 0);
        }

        [TestMethod]
        public void ExecutionEventTable()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<IExecutionEvent>>(
                new DataOutputPath(
                    LTTngThreadDataCookerPath,
                    nameof(LttngThreadDataCooker.ExecutionEvents)));

            Assert.IsTrue(eventData.Count > 0);
        }

        [TestMethod]
        public void FileEventsTable()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<FileEvent>>(
                new DataOutputPath(
                    LttngDiskDataCookerPath,
                    nameof(LttngDiskDataCooker.FileEvents)));

            Assert.IsTrue(eventData.Count > 0);
        }

        [TestMethod]
        public void GenericEventsTable()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<LttngGenericEvent>>(
                new DataOutputPath(
                    LTTngGenericEventDataCookerPath,
                    nameof(LttngGenericEventDataCooker.Events)));

            Assert.IsTrue(eventData.Count > 0);
        }

        [TestMethod]
        public void ModuleEventsTable()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<ModuleEvent>>(
                new DataOutputPath(
                    LttngModuleDataCookerPath,
                    nameof(LttngModuleDataCooker.ModuleEvents)));

            Assert.IsTrue(eventData.Count == 0);         // TODO - UT - Trace has no ModuleEvents
        }

        [TestMethod]
        public void SyscallTable()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<List<ISyscall>>(
                new DataOutputPath(
                    LTTngSyscallDataCookerPath,
                    nameof(LttngSyscallDataCooker.Syscalls)));

            Assert.IsTrue(eventData.Count > 0);
        }

        [TestMethod]
        public void ThreadTable()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<IThread>>(
                new DataOutputPath(
                    LTTngThreadDataCookerPath,
                    nameof(LttngThreadDataCooker.Threads)));

            Assert.IsTrue(eventData.Count > 0);
        }
    }
}
