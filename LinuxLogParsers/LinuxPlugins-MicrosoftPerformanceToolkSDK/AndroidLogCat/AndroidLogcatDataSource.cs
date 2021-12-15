// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinuxLogParser.AndroidLogcat;
using Microsoft.Performance.SDK.Processing;

namespace AndroidLogcatMPTAddin
{
    [ProcessingSource(
    "{DC6FAB5D-D839-4745-8F6C-5BF6941E0DB4}",
    "Android Logcat (Txt)",
    "A data source to parse logcat events in a text file")]
    [FileDataSource(
    "log",
    "Log files")]
    public class AndroidLogcatDataSource : ProcessingSource
    {
        private IApplicationEnvironment applicationEnvironment;

        protected override ICustomDataProcessor CreateProcessorCore(IEnumerable<IDataSource> dataSources, IProcessorEnvironment processorEnvironment, ProcessorOptions options)
        {
            string[] filePaths = dataSources.Select(x => x.Uri.LocalPath).ToArray();
            var sourceParser = new AndroidLogcatLogParser(filePaths);

            return new AndroidLogcatCustomDataProcessor(
                sourceParser,
                options,
                this.applicationEnvironment,
                processorEnvironment);
        }

        protected override bool IsDataSourceSupportedCore(IDataSource dataSource)
        {
            return dataSource.IsFile() && 
                   (Path.GetExtension(dataSource.Uri.LocalPath) == ".log");
        }

        protected override void SetApplicationEnvironmentCore(IApplicationEnvironment applicationEnvironment)
        {
            this.applicationEnvironment = applicationEnvironment;
        }
    }
}
