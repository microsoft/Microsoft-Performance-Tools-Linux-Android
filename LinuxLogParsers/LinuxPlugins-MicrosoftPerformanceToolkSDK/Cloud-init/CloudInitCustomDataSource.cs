// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParser.CloudInitLog;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CloudInitMPTAddin
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
        "{a7752cda-d80f-49f6-8022-2129ab041cd2}",   // The GUID must be unique for your Custom Data Source. You can use Visual Studio's Tools -> Create Guid… tool to create a new GUID
        "Cloud-Init",                               // The Custom Data Source MUST have a name
        @"Linux Cloud-Init log parser")]            // The Custom Data Source MUST have a description
    [FileDataSource(
        ".log",                                              // A file extension is REQUIRED
        "Linux Cloud-Init Cloud Provisioning Log")]  // A description is OPTIONAL. The description is what appears in the file open menu to help users understand what the file type actually is. 

    //
    // There are two methods to creating a Custom Data Source that is recognized by the SDK:
    //    1. Using the helper abstract base classes
    //    2. Implementing the raw interfaces
    // This sample demonstrates method 1 where the CustomDataSourceBase abstract class
    // helps provide a public parameterless constructor and implement the ICustomDataSource interface
    //

    public class CloudInitCustomDataSource
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

        protected override bool IsDataSourceSupportedCore(IDataSource dataSource)
        {
            return dataSource.IsFile() && StringComparer.OrdinalIgnoreCase.Equals(
                "cloud-init.log",
                Path.GetFileName(dataSource.Uri.LocalPath));
        }

        protected override ICustomDataProcessor CreateProcessorCore(
            IEnumerable<IDataSource> dataSources,
            IProcessorEnvironment processorEnvironment,
            ProcessorOptions options)
        {
            string[] filePaths = dataSources.Select(x => x.Uri.LocalPath).ToArray();
            var sourceParser = new CloudInitLogParser(filePaths);

            return new CloudInitCustomDataProcessor(
                sourceParser,
                options,
                this.applicationEnvironment,
                processorEnvironment,
                this.AllTables,
                this.MetadataTables);
        }
    }
}
