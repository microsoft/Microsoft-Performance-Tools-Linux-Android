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
            const string ReadPastEndOfStreamExceptionMessage = "Read past end of stream."; // Trace can be partially written but still have data - https://github.com/microsoft/perfview/issues/1637
            var contentDictionary = new Dictionary<string, TraceEventProcessor>();

            foreach (var path in this.filePaths)
            {
                var traceStartTime = DateTime.UtcNow.Date;

                var tmpEtlx = Path.Combine(Path.GetTempPath(), Path.GetFileName(path) + ".etlx");
                var traceEventProcessor = new TraceEventProcessor();
                try
                {
                    string traceLogPath = TraceLog.CreateFromEventPipeDataFile(path, tmpEtlx);
                    using (TraceLog traceLog = new TraceLog(traceLogPath))
                    {
                        TraceLogEventSource source = traceLog.Events.GetSource();

                        contentDictionary[path] = traceEventProcessor;
                        source.AllEvents += traceEventProcessor.ProcessTraceEvent;
                        source.Process();
                        this.dataSourceInfo = new DataSourceInfo(0, source.SessionDuration.Ticks * 100, source.SessionStartTime.ToUniversalTime());
                    }
                }
                catch (Exception e)
                {
                    if (e.Message != ReadPastEndOfStreamExceptionMessage || !traceEventProcessor.HasTraceData())
                    {
                        throw;
                    }
                }
                finally
                {
                    try
                    {
                        File.Delete(tmpEtlx);
                    }
                    catch (Exception)
                    {
                    }
                }
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
