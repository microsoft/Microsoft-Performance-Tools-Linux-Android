// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Stacks;
using Microsoft.Diagnostics.Tracing.StackSources;
using Microsoft.Performance.SDK.Processing;
using PerfDataExtensions.Tables;

namespace PerfDataProcessingSource
{
    public sealed class PerfDataCustomDataProcessor
        : CustomDataProcessor
    {
        private readonly string[] filePaths;
        private IReadOnlyDictionary<string, ParallelLinuxPerfScriptStackSource> fileContent;
        private DataSourceInfo dataSourceInfo;

        public PerfDataCustomDataProcessor(
           string[] filePaths,
           ProcessorOptions options,
           IApplicationEnvironment applicationEnvironment,
           IProcessorEnvironment processorEnvironment)
            : base(options, applicationEnvironment, processorEnvironment)
        {
            //
            // Assign the files array to a readonly backing field.
            //

            this.filePaths = filePaths;
        }

        public override DataSourceInfo GetDataSourceInfo()
        {
            // The DataSourceInfo is used to tell analzyer the time range of the data(if applicable) and any other relevant data for rendering / synchronizing.

            return this.dataSourceInfo;

        }

        protected override Task ProcessAsyncCore(
           IProgress<int> progress,
           CancellationToken cancellationToken)
        {
            var contentDictionary = new Dictionary<string, ParallelLinuxPerfScriptStackSource>();

            foreach (var path in this.filePaths)
            {
                // Hack because perf.data.txt and parsing only includes relative offsets, not absolute time
                // Look for timestamp.txt in the path of the trace and use that as trace start UTC time
                // If it doesn't exist, just use today
                var traceStartTime = DateTime.UtcNow.Date;
                var traceTimeStampStartFile = Path.Combine(Path.GetDirectoryName(path), "timestamp.txt");
                if (File.Exists(traceTimeStampStartFile))
                {
                    string time = File.ReadAllText(traceTimeStampStartFile).Trim();

                    if (!DateTime.TryParse(time, out traceStartTime))
                    {
                        traceStartTime = DateTime.UtcNow.Date; // traceStartTime got overwritten

                        try
                        {
                            traceStartTime = DateTime.ParseExact(time, "ddd MMM d HH:mm:ss yyyy", CultureInfo.InvariantCulture); // "Thu Oct 17 15:37:51 2019" See if this "captured on" date format from "sudo perf report --header-only -i perf.data.merged"
                        }
                        catch (FormatException)
                        {
                            Logger.Error("Could not parse time {0} in file {1}. Format expected is: ddd MMM d HH:mm:ss yyyy", time, traceTimeStampStartFile);
                        }
                    }
                    traceStartTime = DateTime.FromFileTimeUtc(traceStartTime.ToFileTimeUtc());
                }

                var stackSource = new ParallelLinuxPerfScriptStackSource(path);

                var lastSample = stackSource.GetLinuxPerfScriptSampleByIndex((StackSourceSampleIndex)stackSource.SampleIndexLimit - 1);

                contentDictionary[path] = stackSource;
                this.dataSourceInfo = new DataSourceInfo(0, (long)lastSample.TimeRelativeMSec * 1000000, traceStartTime);

            }

            this.fileContent = new ReadOnlyDictionary<string, ParallelLinuxPerfScriptStackSource>(contentDictionary);

            return Task.CompletedTask;
        }

        protected override void BuildTableCore(
            TableDescriptor tableDescriptor,
            ITableBuilder tableBuilder)
        {
            //
            // Instantiate the table, and pass the tableBuilder to it.
            //

            var table = this.InstantiateTable(tableDescriptor.Type);
            table.Build(tableBuilder);
        }

        private LinuxPerfScriptTableBase InstantiateTable(Type tableType)
        {
            //
            // This private method is added to activate the given table type and pass in the file content.
            //

            var instance = Activator.CreateInstance(tableType, new[] { this.fileContent, });
            return (LinuxPerfScriptTableBase)instance;
        }
    }
}
