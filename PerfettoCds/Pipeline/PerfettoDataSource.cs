using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

            return dataSource.IsFile() && StringComparer.OrdinalIgnoreCase.Equals(".perfetto-trace", Path.GetExtension(dataSource.Uri.LocalPath));
        }

        protected override void SetApplicationEnvironmentCore(IApplicationEnvironment applicationEnvironment)
        {
            this.applicationEnvironment = applicationEnvironment;
        }
    }
}
