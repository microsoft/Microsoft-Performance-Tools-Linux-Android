// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using LinuxLogParser.DmesgIsoLog;
using Microsoft.Performance.SDK.Processing;

namespace DmesgIsoMPTAddin
{
    [ProcessingSource(
    "{D1440EE2-DD94-4141-953A-82131BA3C91D}",
    "DmesgIsoLog",
    "A data source to parse dmesg.iso.log file")]
    [FileDataSource(
    "log",
    "Log files")]
    public class DmesgIsoDataSource : ProcessingSource
    {
        private IApplicationEnvironment applicationEnvironment;

        protected override ICustomDataProcessor CreateProcessorCore(IEnumerable<IDataSource> dataSources, IProcessorEnvironment processorEnvironment, ProcessorOptions options)
        {
            string[] filePaths = dataSources.Select(x => x.Uri.LocalPath).ToArray();
            var sourceParser = new DmesgIsoLogParser(filePaths);

            return new DmesgIsoCustomDataProcessor(
                sourceParser,
                options,
                this.applicationEnvironment,
                processorEnvironment,
                this.AllTables,
                this.MetadataTables);
        }

        protected override bool IsDataSourceSupportedCore(IDataSource dataSource)
        {
            return dataSource.IsFile() && dataSource.Uri.LocalPath.ToLower().EndsWith("dmesg.iso.log");
        }

        protected override void SetApplicationEnvironmentCore(IApplicationEnvironment applicationEnvironment)
        {
            this.applicationEnvironment = applicationEnvironment;
        }
    }
}
