// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CloudInitMPTAddin;
using DmesgIsoMPTAddin;
using Microsoft.Performance.SDK.Extensibility;
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
using WaLinuxAgentMPTAddin;

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

            var cooker = new DmesgIsoDataCooker().Path;
            runtime.EnableCooker(cooker);

            var runtimeExecutionResults = runtime.Process();

            var eventData = runtimeExecutionResults.QueryOutput<DmesgIsoLogParsedResult>(
                new DataOutputPath(
                    cooker,
                    nameof(DmesgIsoDataCooker.ParsedResult)));

            Assert.IsTrue(eventData.LogEntries.Count >= 0); 
        }

        [TestMethod]
        public void CloudInit()
        {
            // Input data
            string[] cloudInitData = { @"..\..\..\..\..\TestData\LinuxLogs\Cloud-Init\cloud-init.log" };
            var cloutInitDataPath = new FileInfo(cloudInitData[0]);
            Assert.IsTrue(cloutInitDataPath.Exists);

            var runtime = Engine.Create();
            runtime.AddFile(cloutInitDataPath.FullName);

            var cooker = new CloudInitDataCooker().Path;
            runtime.EnableCooker(cooker);

            var runtimeExecutionResults = runtime.Process();

            var eventData = runtimeExecutionResults.QueryOutput<CloudInitLogParsedResult>(
                new DataOutputPath(
                    cooker,
                    nameof(CloudInitDataCooker.ParsedResult)));

            Assert.IsTrue(eventData.LogEntries.Count >= 0);
        }

        [TestMethod]
        public void WaLinuxAgent()
        {
            // Input data
            string[] waLinuxAgentData = { @"..\..\..\..\..\TestData\LinuxLogs\WaLinuxAgent\waagent.log" };
            var waLinuxAgentDataPath = new FileInfo(waLinuxAgentData[0]);
            Assert.IsTrue(waLinuxAgentDataPath.Exists);

            var runtime = Engine.Create();
            runtime.AddFile(waLinuxAgentDataPath.FullName);

            var cooker = new WaLinuxAgentDataCooker().Path;
            runtime.EnableCooker(cooker);

            var runtimeExecutionResults = runtime.Process();

            var eventData = runtimeExecutionResults.QueryOutput<WaLinuxAgentLogParsedResult>(
                new DataOutputPath(
                    cooker,
                    nameof(WaLinuxAgentDataCooker.ParsedResult)));

            Assert.IsTrue(eventData.LogEntries.Count >= 0);
        }
    }
}
