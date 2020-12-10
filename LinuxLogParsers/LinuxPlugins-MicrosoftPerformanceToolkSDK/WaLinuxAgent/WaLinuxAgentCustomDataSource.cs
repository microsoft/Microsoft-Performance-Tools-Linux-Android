// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParser.WaLinuxAgentLog;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WaLinuxAgentMPTAddin
{
    //
    // This is a sample Custom Data Source (CDS) that understands files with the .txt extension
    //

    // In order for a CDS to be recognized, it MUST satisfy the following:
    //  a) Be a public type
    //  b) Have a public parameterless constructor
    //  c) Implement the ICustomDataSource interface
    //  d) Be decorated with the CustomDataSourceAttribute attribute
    //  e) Be decorated with at least one of the derivatives of the DataSourceAttribute attribute
    //

    [CustomDataSource(
        "{a9ac39bc-2d07-4a01-b9b5-13a02611f5f2}",     // The GUID must be unique for your Custom Data Source. You can use Visual Studio's Tools -> Create Guid… tool to create a new GUID
        "WaLinuxAgent",                               // The Custom Data Source MUST have a name
        @"WaLinuxAgent log parser")]                  // The Custom Data Source MUST have a description
    [FileDataSource(
        ".log",                                              // A file extension is REQUIRED
        "Linux WaLinuxAgent Cloud Provisioning Log")]  // A description is OPTIONAL. The description is what appears in the file open menu to help users understand what the file type actually is. 

    //
    // There are two methods to creating a Custom Data Source that is recognized by the SDK:
    //    1. Using the helper abstract base classes
    //    2. Implementing the raw interfaces
    // This sample demonstrates method 1 where the CustomDataSourceBase abstract class
    // helps provide a public parameterless constructor and implement the ICustomDataSource interface
    //

    public class WaLinuxAgentCustomDataSource
        : CustomDataSourceBase
    {
        private IApplicationEnvironment applicationEnvironment;

        protected override void SetApplicationEnvironmentCore(IApplicationEnvironment applicationEnvironment)
        {
            //
            // Saves the given application environment into this instance
            //

            this.applicationEnvironment = applicationEnvironment;
        }

        protected override bool IsFileSupportedCore(string path)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(
                "waagent.log",
                Path.GetFileName(path));
        }

        protected override ICustomDataProcessor CreateProcessorCore(
            IEnumerable<IDataSource> dataSources,
            IProcessorEnvironment processorEnvironment,
            ProcessorOptions options)
        {
            string[] filePaths = dataSources.Select(x => x.GetUri().LocalPath).ToArray();
            var sourceParser = new WaLinuxAgentLogParser(filePaths);

            return new WaLinuxAgentCustomDataProcessor(
                sourceParser,
                options,
                this.applicationEnvironment,
                processorEnvironment,
                this.AllTables,
                this.MetadataTables);
        }
    }
}
