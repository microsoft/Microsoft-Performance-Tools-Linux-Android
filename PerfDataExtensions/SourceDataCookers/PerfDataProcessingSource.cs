// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Performance.SDK.Processing;

namespace PerfDataProcessingSource
{
    //
    // This is a sample Custom Data Source (CDS) that understands files with the .txt extension
    //

    // In order for a CDS to be recognized, it MUST satisfy the following:
    //  a) Be a public type
    //  b) Have a public parameterless constructor
    //  c) Implement the IProcessingSource interface
    //  d) Be decorated with the ProcessingSourceAttribute attribute
    //  e) Be decorated with at least one of the derivatives of the DataSourceAttribute attribute
    //

    [ProcessingSource(
        "{EA48A279-2B4E-43A0-AC86-030113A23064}",   // The GUID must be unique for your Custom Data Source. You can use Visual Studio's Tools -> Create Guid… tool to create a new GUID
        "Linux Perf Txt Data",                               // The Custom Data Source MUST have a name
        @"Linux perf.data.txt parser")]            // The Custom Data Source MUST have a description
    [FileDataSource(
        ".txt",                                              // A file extension is REQUIRED
        "Linux perf.data.txt parser")]  // A description is OPTIONAL. The description is what appears in the file open menu to help users understand what the file type actually is. 

    //
    // There are two methods to creating a Custom Data Source that is recognized by UI:
    //    1. Using the helper abstract base classes
    //    2. Implementing the raw interfaces
    // This sample demonstrates method 1 where the ProcessingSource abstract class
    // helps provide a public parameterless constructor and implement the IProcessingSource interface
    //

    public class PerfDataProcessingSource
        : ProcessingSource
    {
        private IApplicationEnvironment applicationEnvironment;

        public override ProcessingSourceInfo GetAboutInfo()
        {
            return new ProcessingSourceInfo()
            {
                ProjectInfo = new ProjectInfo() { Uri = "https://aka.ms/linuxperftools" },
            };
        }

        protected override void SetApplicationEnvironmentCore(IApplicationEnvironment applicationEnvironment)
        {
            //
            // Saves the given application environment into this instance
            //

            this.applicationEnvironment = applicationEnvironment;
        }

        protected override bool IsDataSourceSupportedCore(IDataSource dataSource)
        {
            return dataSource.IsFile() && Path.GetFileName(dataSource.Uri.LocalPath).EndsWith("perf.data.txt", StringComparison.OrdinalIgnoreCase);
        }

        protected override ICustomDataProcessor CreateProcessorCore(
            IEnumerable<IDataSource> dataSources,
            IProcessorEnvironment processorEnvironment,
            ProcessorOptions options)
        {
            //
            // Create a new instance implementing ICustomDataProcessor here to process the specified data sources.
            // Note that you can have more advanced logic here to create different processors if you would like based on the file, or any other criteria.
            // You are not restricted to always returning the same type from this method.
            //

            return new PerfDataCustomDataProcessor(
                dataSources.Select(x => x.Uri.LocalPath).ToArray(),
                options,
                this.applicationEnvironment,
                processorEnvironment);
        }
    }
}
