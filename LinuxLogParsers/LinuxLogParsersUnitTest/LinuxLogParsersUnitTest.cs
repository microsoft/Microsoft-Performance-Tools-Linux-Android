// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AndroidLogcatMPTAddin;
using CloudInitMPTAddin;
using DmesgIsoMPTAddin;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

            using var runtime = Engine.Create(new FileDataSource(dmesgDataPath.FullName));
            var cooker = new DmesgIsoDataCooker().Path;
            runtime.EnableCooker(cooker);

            var runtimeExecutionResults = runtime.Process();

            var eventData = runtimeExecutionResults.QueryOutput<DmesgIsoLogParsedResult>(
                new DataOutputPath(
                    cooker,
                    nameof(DmesgIsoDataCooker.ParsedResult)));

            Assert.IsTrue(eventData.LogEntries.Count == 1867);
        }

        [TestMethod]
        public void AndroidLogcat()
        {
            // Some other examples focusing on RegEx
            var trickySamples = new string[] 
            {
                "12-13 10:32:21.278     0     0 I         : Linux version 5.10.43-2-windows-subsystem-for-android (oe-user@oe-host) (clang version 10.0.1 (https://github.com/llvm/llvm-project ef32c611aa214dea855364efd7ba451ec5ec3f74), GNU ld (GNU Binutils) 2.34.0.20200220) #1 SMP PREEMPT Tue Sep 14 09:09:25 UTC 2021",
               @"12-13 10:32:21.278     0     0 I Command line: initrd=\initrd.img panic=-1 nr_cpus=8 earlycon=uart8250,io,0x3f8,115200 console=hvc0 debug pty.legacy_count=0 androidboot.hardware=windows_x86_64 panic=-1 pty.legacy_count=0 androidboot.veritymode=enforcing androidboot.verifiedbootstate=green loglevel=6 transparent_hugepage=never swiotlb=noforce  androidboot.hardware.egl=emulation androidboot.hardware.gralloc=emulation",
                "12-13 10:32:21.278     0     0 I x86/fpu : Supporting XSAVE feature 0x001: 'x87 floating point registers'",
                "12-13 10:32:25.709    86    86 E APM::AudioPolicyEngine/Config: parseLegacyVolumeFile: Could not parse document /odm/etc/audio_policy_configuration.xml",
                "12-13 10:32:21.278     0     0 I BIOS-e820: [mem 0x0000000000000000-0x000000000009ffff] usable",
                "12-13 10:32:21.317     0     0 I IOAPIC[0]: apic_id 8, version 17, address 0xfec00000, GSI 0-23",
                "12-13 10:32:21.328     0     0 I         : Built 1 zonelists, mobility grouping on.  Total pages: 1547406"
            };

            foreach (var s in trickySamples)
            {
                var m = LinuxLogParser.AndroidLogcat.AndroidLogcatLogParser.AndroidLogCatRegex.Match(s);
                if (!m.Success)
                {
                    Console.WriteLine(s);
                }

                Assert.IsTrue(m.Success);
            }

            // Input data from log
            string[] androidlogcatData = { @"..\..\..\..\..\TestData\AndroidLogs\Logcat\AndroidLogcatWSA.log" };
            var androidlogcatDataPath = new FileInfo(androidlogcatData[0]);
            Assert.IsTrue(androidlogcatDataPath.Exists);

            using var runtime = Engine.Create(new FileDataSource(androidlogcatDataPath.FullName));
            var cooker = new AndroidLogcatDataCooker().Path;
            runtime.EnableCooker(cooker);

            var runtimeExecutionResults = runtime.Process();

            var eventData = runtimeExecutionResults.QueryOutput<AndroidLogcatParsedResult>(
                new DataOutputPath(
                    cooker,
                    nameof(AndroidLogcatDataCooker.ParsedResult)));

            Assert.IsTrue(eventData.LogEntries.Count > 0);
            Assert.IsTrue(eventData.LogEntries.Count == 6061);
            var noTimestampOrNotParsed = eventData.LogEntries.Where(f => f.Timestamp.ToNanoseconds < 0);
            Assert.IsTrue(noTimestampOrNotParsed.Count() == 5); // There is 5 entries with no timestamp e.g "--------- beginning of "

            //eventData.LogEntries[1].Timestamp == // Check abs time if/when SDK supports it
            Assert.IsTrue(eventData.LogEntries[1].LineNumber == 3);
            Assert.IsTrue(eventData.LogEntries[1].Timestamp == Timestamp.Zero);
            Assert.IsTrue(eventData.LogEntries[1].PID == 20);
            Assert.IsTrue(eventData.LogEntries[1].TID == 20);
            Assert.IsTrue(eventData.LogEntries[1].Priority == "I");
            Assert.IsTrue(eventData.LogEntries[1].Tag == "auditd");
            Assert.IsTrue(eventData.LogEntries[1].Message == "type=2000 audit(0.0:1): state=initialized audit_enabled=0 res=1");

            //eventData.LogEntries[6].Timestamp == // Check abs time if/when SDK supports it
            Assert.IsTrue(eventData.LogEntries[6].LineNumber == 13);
            Assert.IsTrue(eventData.LogEntries[6].Timestamp == new Timestamp(1248000000)); // 1,248,000,000 ns past 1st event
            Assert.IsTrue(eventData.LogEntries[6].PID == 0);
            Assert.IsTrue(eventData.LogEntries[6].TID == 0);
            Assert.IsTrue(eventData.LogEntries[6].Priority == "I");
            Assert.IsTrue(eventData.LogEntries[6].Tag == "Command line");
            Assert.IsTrue(!String.IsNullOrWhiteSpace(eventData.LogEntries[6].Message));

            Assert.IsTrue(eventData.DurationLogEntries.Count > 0);
            // Service 'exec 1 (/system/bin/linkerconfig --target /linkerconfig/bootstrap)' (pid 4) exited with status 0 waiting took 0.015000 seconds
            Assert.IsTrue(eventData.DurationLogEntries[0].Name == "exec 1 (/system/bin/linkerconfig --target /linkerconfig/bootstrap)");
            Assert.IsTrue(eventData.DurationLogEntries[0].Duration.ToMilliseconds == 15);

            // Service 'apexd-bootstrap' (pid 6) exited with status 0 waiting took 0.053000 seconds
            Assert.IsTrue(eventData.DurationLogEntries[1].Name == "apexd-bootstrap");
            Assert.IsTrue(eventData.DurationLogEntries[1].Duration.ToMilliseconds == 53);
            Assert.IsTrue((eventData.DurationLogEntries[1].EndTimestamp - eventData.DurationLogEntries[1].StartTimestamp).ToMilliseconds == 53);

            Assert.IsTrue(eventData.DurationLogEntries.Count == 1011);

            // MakeWindowManagerServiceReady took to complete: 3ms
            Assert.IsTrue(eventData.DurationLogEntries[443].Name == "MakeWindowManagerServiceReady");
            Assert.IsTrue(eventData.DurationLogEntries[443].Duration.ToMilliseconds == 3);
            Assert.IsTrue((eventData.DurationLogEntries[443].EndTimestamp - eventData.DurationLogEntries[443].StartTimestamp).ToMilliseconds == 3);
        }

        [TestMethod]
        public void CloudInit()
        {
            // Input data
            string[] cloudInitData = { @"..\..\..\..\..\TestData\LinuxLogs\Cloud-Init\cloud-init.log" };
            var cloutInitDataPath = new FileInfo(cloudInitData[0]);
            Assert.IsTrue(cloutInitDataPath.Exists);

            using var runtime = Engine.Create(new FileDataSource(cloutInitDataPath.FullName));
            var cooker = new CloudInitDataCooker().Path;
            runtime.EnableCooker(cooker);

            var runtimeExecutionResults = runtime.Process();

            var eventData = runtimeExecutionResults.QueryOutput<CloudInitLogParsedResult>(
                new DataOutputPath(
                    cooker,
                    nameof(CloudInitDataCooker.ParsedResult)));

            Assert.IsTrue(eventData.LogEntries.Count == 937);
        }

        [TestMethod]
        public void WaLinuxAgent()
        {
            // Input data
            string[] waLinuxAgentData = { @"..\..\..\..\..\TestData\LinuxLogs\WaLinuxAgent\waagent.log" };
            var waLinuxAgentDataPath = new FileInfo(waLinuxAgentData[0]);
            Assert.IsTrue(waLinuxAgentDataPath.Exists);

            using var runtime = Engine.Create(new FileDataSource(waLinuxAgentDataPath.FullName));
            var cooker = new WaLinuxAgentDataCooker().Path;
            runtime.EnableCooker(cooker);

            var runtimeExecutionResults = runtime.Process();

            var eventData = runtimeExecutionResults.QueryOutput<WaLinuxAgentLogParsedResult>(
                new DataOutputPath(
                    cooker,
                    nameof(WaLinuxAgentDataCooker.ParsedResult)));

            Assert.IsTrue(eventData.LogEntries.Count == 773); // Should be 795 - bug?
        }
    }
}
