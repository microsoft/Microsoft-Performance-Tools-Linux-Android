// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Processing;

namespace LTTngCds
{
    [CustomDataSource(
        "{98608154-6231-4F25-903A-5E440574AB45}",
        "LTTng",
        "Processes LTTng CTF data")]
    [FileDataSource("ctf", "ctf")]
    [DirectoryDataSource("LTTng CTF Folder")]
    public class LTTngDataSource
        : CustomDataSourceBase
    {
        private IApplicationEnvironment applicationEnvironment;

        /// <inheritdoc />
        public override IEnumerable<Option> CommandLineOptions => Enumerable.Empty<Option>();

        protected override bool IsDataSourceSupportedCore(IDataSource dataSource)
        {
            if (dataSource.IsDirectory())
            {
                return Directory.GetFiles(dataSource.Uri.LocalPath, "metadata", SearchOption.AllDirectories).Any();
            }

            return dataSource.IsFile() && StringComparer.OrdinalIgnoreCase.Equals(".ctf", Path.GetExtension(dataSource.Uri.LocalPath));
        }

        public override CustomDataSourceInfo GetAboutInfo()
        {
            return new CustomDataSourceInfo()
            {
                ProjectInfo = new ProjectInfo() { Uri = "https://aka.ms/linuxperftools" },
                CopyrightNotice = "Copyright (C) " + DateTime.UtcNow.Year,
                AdditionalInformation = new[]
                {
                    "Built using Antlr 4\n" +
                    "Copyright (c) 2012 Terence Parr and Sam Harwell\n" +
                    "All rights reserved.\n" +
                    "Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:\n" +
                    "\n" +
                    "Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.\n" +
                    "Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.\n" +
                    "Neither the name of the author nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.\n" +
                    "THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS \"AS IS\" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.\n",
                },
            };
        }

        /// <inheritdoc />
        protected override void SetApplicationEnvironmentCore(IApplicationEnvironment applicationEnvironment)
        {
            this.applicationEnvironment = applicationEnvironment;
        }

        protected override ICustomDataProcessor CreateProcessorCore(
            IEnumerable<IDataSource> dataSources,
            IProcessorEnvironment processorEnvironment,
            ProcessorOptions options)
        {
            Guard.NotNull(dataSources, nameof(dataSources));
            Guard.NotNull(processorEnvironment, nameof(processorEnvironment));
            Guard.Any(dataSources, nameof(dataSources));

            var sourceParser = new LTTngSourceParser();

            var firstDataSource = dataSources.First();
            string sourcePath = firstDataSource.Uri.LocalPath;
            if (firstDataSource.IsDirectory() && Directory.Exists(sourcePath))
            {
                // handle open directory
                sourceParser.SetFolderInput(sourcePath);
            }
            else
            {
                // handle zip archive
                sourceParser.SetZippedInput(sourcePath);
            }

            return new LTTngDataProcessor(
                sourceParser,
                options,
                this.applicationEnvironment,
                processorEnvironment,
                this.AllTables,
                this.MetadataTables);
        }
    }
}