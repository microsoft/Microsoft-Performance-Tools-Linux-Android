// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Performance.SDK.Processing;

namespace DotNetEventPipe
{
    [ProcessingSource(
        "{890C0A11-011E-43E1-AE28-7E1A903A6633}",   // The GUID must be unique for your Custom Data Source. You can use Visual Studio's Tools -> Create Guid… tool to create a new GUID
        ".NET (dotnet-trace)",                      // The Custom Data Source MUST have a name
        @".net trace EventPipe")]                   // The Custom Data Source MUST have a description
    [FileDataSource(
        ".nettrace",                                // A file extension is REQUIRED
        "dotnet-trace")]                            // A description is OPTIONAL. The description is what appears in the file open menu to help users understand what the file type actually is. 

    //
    // There are two methods to creating a Custom Data Source that is recognized by UI:
    //    1. Using the helper abstract base classes
    //    2. Implementing the raw interfaces
    // This sample demonstrates method 1 where the ProcessingSource abstract class
    // helps provide a public parameterless constructor and implement the IProcessingSource interface
    //

    public class DotnetTraceProcessingSource
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
            return dataSource.IsFile() && Path.GetExtension(dataSource.Uri.LocalPath) == ".nettrace";
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

            return new DotnetTraceDataProcessor(
                dataSources.Select(x => x.Uri.LocalPath).ToArray(),
                options,
                this.applicationEnvironment,
                processorEnvironment);
        }
    }
}
