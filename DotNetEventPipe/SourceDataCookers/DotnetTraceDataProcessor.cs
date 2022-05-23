// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DotNetEventPipe.Tables;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Diagnostics.Tracing.EventPipe;
using Microsoft.Diagnostics.Tracing.Etlx;

namespace DotNetEventPipe
{
    public sealed class DotnetTraceDataProcessor
        : CustomDataProcessor
    {
        private readonly string[] filePaths;
        private IReadOnlyDictionary<string, TraceEventProcessor> fileContent;
        private DataSourceInfo dataSourceInfo;

        public DotnetTraceDataProcessor(
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
            var contentDictionary = new Dictionary<string, TraceEventProcessor>();

            foreach (var path in this.filePaths)
            {
                var traceStartTime = DateTime.UtcNow.Date;

                // EventPipeEventSource doesn't expose the callstacks - https://github.com/Microsoft/perfview/blob/main/src/TraceEvent/EventPipe/EventPipeFormat.md
                // But currently it's SessionDuration, SessionStartTime are correct
                // Can remove when when this is released - https://github.com/microsoft/perfview/pull/1635
                var dotnetFileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                using (var traceSource = new EventPipeEventSource(dotnetFileStream))
                {
                    traceSource.Process();
                    this.dataSourceInfo = new DataSourceInfo(0, traceSource.SessionDuration.Ticks * 100, traceSource.SessionStartTime.ToUniversalTime());
                }

                var tmpEtlx = Path.Combine(Path.GetTempPath(), Path.GetFileName(path) + ".etlx");
                
                string traceLogPath = TraceLog.CreateFromEventPipeDataFile(path, tmpEtlx);
                using (TraceLog traceLog = new TraceLog(traceLogPath))
                {
                    TraceLogEventSource source = traceLog.Events.GetSource();

                    var traceEventProcessor = new TraceEventProcessor();
                    contentDictionary[path] = traceEventProcessor;
                    source.AllEvents += traceEventProcessor.ProcessTraceEvent;
                    source.Process();
                    // Below will work when this is released - https://github.com/microsoft/perfview/pull/1635
                    //this.dataSourceInfo = new DataSourceInfo(0, source.SessionDuration.Ticks * 100, source.SessionStartTime.ToUniversalTime());
                }
                File.Delete(tmpEtlx);
            }

            this.fileContent = new ReadOnlyDictionary<string, TraceEventProcessor>(contentDictionary);

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

        private TraceEventTableBase InstantiateTable(Type tableType)
        {
            //
            // This private method is added to activate the given table type and pass in the file content.
            //

            var instance = Activator.CreateInstance(tableType, new[] { this.fileContent, });
            return (TraceEventTableBase)instance;
        }
    }
}
