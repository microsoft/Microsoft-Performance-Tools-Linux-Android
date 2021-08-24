// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PerfettoCds
{
    [CustomDataSource("9fc8515e-9206-4690-b14a-3e7b54745c5f", "PerfettoTraceDataSource", "Processes Perfetto trace files")]
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

        public override CustomDataSourceInfo GetAboutInfo()
        {
            return new CustomDataSourceInfo()
            {
                ProjectInfo = new ProjectInfo() { Uri = "https://aka.ms/linuxperftools" },
                CopyrightNotice = "Copyright (C) " + DateTime.UtcNow.Year,
                AdditionalInformation = new[]
                {
                    "Built using Google Perfetto 4\n" +
                    "Copyright (C) 2020 The Android Open Source Project\n" +
                    "\n" +
                    "Licensed under the Apache License, Version 2.0 (the \"License\");\n" +
                    "you may not use this file except in compliance with the License.\n" +
                    "You may obtain a copy of the License at\n" +
                    "\n" +
                    "http://www.apache.org/licenses/LICENSE-2.0\n" +
                    "\n" +
                    "Unless required by applicable law or agreed to in writing, software\n" +
                    "distributed under the License is distributed on an \"AS IS\" BASIS,\n" +
                    "WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.\n" +
                    "See the License for the specific language governing permissions and\n" +
                    "limitations under the License.\n"
                },
            };
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

    [CustomDataSource("99e4223a-6211-4ce7-a0da-917a893797f2", "PftraceDataSource", "Processes .pftrace Perfetto trace files")]
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

        public override CustomDataSourceInfo GetAboutInfo()
        {
            return new CustomDataSourceInfo()
            {
                ProjectInfo = new ProjectInfo() { Uri = "https://aka.ms/linuxperftools" },
                CopyrightNotice = "Copyright (C) " + DateTime.UtcNow.Year,
                AdditionalInformation = new[]
                {
                    "Built using Google Perfetto 4\n" +
                    "Copyright (C) 2020 The Android Open Source Project\n" +
                    "\n" +
                    "Licensed under the Apache License, Version 2.0 (the \"License\");\n" +
                    "you may not use this file except in compliance with the License.\n" +
                    "You may obtain a copy of the License at\n" +
                    "\n" +
                    "http://www.apache.org/licenses/LICENSE-2.0\n" +
                    "\n" +
                    "Unless required by applicable law or agreed to in writing, software\n" +
                    "distributed under the License is distributed on an \"AS IS\" BASIS,\n" +
                    "WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.\n" +
                    "See the License for the specific language governing permissions and\n" +
                    "limitations under the License.\n"
                },
            };
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
