// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Performance.SDK.Processing;

namespace PerfDataProcessingSource
{
    [ProcessingSource(
        "{EA48A279-2B4E-43A0-AC86-030113A23064}",   // The GUID must be unique for your Custom Data Source. You can use Visual Studio's Tools -> Create Guid… tool to create a new GUID
        "Linux Perf Txt Data",                               // The Custom Data Source MUST have a name
        @"Linux perf.data.txt parser")]            // The Custom Data Source MUST have a description
    [FileDataSource(
        ".txt",                                              // A file extension is REQUIRED
        "Linux perf.data.txt parser")]  // A description is OPTIONAL. The description is what appears in the file open menu to help users understand what the file type actually is. 


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
            return new PerfDataCustomDataProcessor(
                dataSources.Select(x => x.Uri.LocalPath).ToArray(),
                options,
                this.applicationEnvironment,
                processorEnvironment);
        }
    }
}
