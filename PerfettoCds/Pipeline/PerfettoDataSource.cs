// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PerfettoCds
{
    [CustomDataSource("9fc8515e-9206-4690-b14a-3e7b54745c5f", "Perfetto DataSource", "Processes Perfetto trace files")]
    [FileDataSource(".perfetto-trace", "Perfetto trace files")]
    public sealed class PerfettoDataSource : CustomDataSourceBase
    {
        private IApplicationEnvironment applicationEnvironment;

        protected override ICustomDataProcessor CreateProcessorCore(IEnumerable<IDataSource> dataSources, IProcessorEnvironment processorEnvironment, ProcessorOptions options)
        {
            var filePath = dataSources.First().Uri.LocalPath;
            var parser = new PerfettoSourceParser(filePath);
            return new PerfettoDataProcessor(parser,
                                            options,
                                            this.applicationEnvironment,
                                            processorEnvironment,
                                            this.AllTables,
                                            this.MetadataTables);
        }

        protected override bool IsDataSourceSupportedCore(IDataSource dataSource)
        {
            if (dataSource.IsDirectory())
            {
                return false;
            }

            var ext = Path.GetExtension(dataSource.Uri.LocalPath);

            return dataSource.IsFile() && StringComparer.OrdinalIgnoreCase.Equals(".perfetto-trace", ext);
        }

        protected override void SetApplicationEnvironmentCore(IApplicationEnvironment applicationEnvironment)
        {
            this.applicationEnvironment = applicationEnvironment;
        }
    }

    [CustomDataSource("99e4223a-6211-4ce7-a0da-917a893797f2", "Perfetto DataSource", "Processes Perfetto trace files")]
    [FileDataSource(".pftrace", "Perfetto trace files")]
    public sealed class PfDataSource : CustomDataSourceBase
    {
        private IApplicationEnvironment applicationEnvironment;

        protected override ICustomDataProcessor CreateProcessorCore(IEnumerable<IDataSource> dataSources, IProcessorEnvironment processorEnvironment, ProcessorOptions options)
        {
            var filePath = dataSources.First().Uri.LocalPath;
            var parser = new PerfettoSourceParser(filePath);
            return new PerfettoDataProcessor(parser,
                                            options,
                                            this.applicationEnvironment,
                                            processorEnvironment,
                                            this.AllTables,
                                            this.MetadataTables);
        }

        protected override bool IsDataSourceSupportedCore(IDataSource dataSource)
        {
            if (dataSource.IsDirectory())
            {
                return false;
            }

            var ext = Path.GetExtension(dataSource.Uri.LocalPath);

            return dataSource.IsFile() && StringComparer.OrdinalIgnoreCase.Equals(".pftrace", ext);
        }

        protected override void SetApplicationEnvironmentCore(IApplicationEnvironment applicationEnvironment)
        {
            this.applicationEnvironment = applicationEnvironment;
        }
    }
}
