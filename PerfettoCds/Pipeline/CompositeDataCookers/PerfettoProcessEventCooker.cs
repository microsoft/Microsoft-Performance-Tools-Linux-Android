// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Processing;
using PerfettoCds.Pipeline.DataOutput;
using PerfettoCds.Pipeline.SourceDataCookers;
using PerfettoProcessor;

namespace PerfettoCds.Pipeline.CompositeDataCookers
{
    /// <summary>
    /// Pulls data from multiple individual SQL tables and joins them to create events for process output
    /// </summary>
    public sealed class PerfettoProcessEventCooker : CookedDataReflector, ICompositeDataCookerDescriptor
    {
        public static readonly DataCookerPath DataCookerPath = PerfettoPluginConstants.ProcessEventCookerPath;

        public string Description => "Process event composite cooker";

        public DataCookerPath Path => DataCookerPath;

        // Declare all of the cookers that are used by this CompositeCooker.
        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new[]
        {
            PerfettoPluginConstants.ProcessRawCookerPath,
            PerfettoPluginConstants.ArgCookerPath,
            PerfettoPluginConstants.PackageListCookerPath,
        };

        [DataOutput]
        public ProcessedEventData<PerfettoProcessEvent> ProcessEvents { get; }

        /// <summary>
        /// The highest number of arg fields found in any single event.
        /// </summary>
        [DataOutput]
        public int MaximumArgsEventFieldCount { get; private set; }

        public PerfettoProcessEventCooker() : base(PerfettoPluginConstants.ProcessEventCookerPath)
        {
            this.ProcessEvents =
                new ProcessedEventData<PerfettoProcessEvent>();
        }

        public void OnDataAvailable(IDataExtensionRetrieval requiredData)
        {
            // Gather the data from all the SQL tables
            var processData = requiredData.QueryOutput<ProcessedEventData<PerfettoProcessRawEvent>>(new DataOutputPath(PerfettoPluginConstants.ProcessRawCookerPath, nameof(PerfettoProcessRawCooker.ProcessEvents)));
            var argData = requiredData.QueryOutput<ProcessedEventData<PerfettoArgEvent>>(new DataOutputPath(PerfettoPluginConstants.ArgCookerPath, nameof(PerfettoArgCooker.ArgEvents)));
            var packageListData = requiredData.QueryOutput<ProcessedEventData<PerfettoPackageListEvent>>(new DataOutputPath(PerfettoPluginConstants.PackageListCookerPath, nameof(PerfettoPackageListCooker.PackageListEvents)));

            // Join them all together

            // Contains the information for each process entry with args
            var joined = from process in processData
                         join arg in argData on process.ArgSetId equals arg.ArgSetId into args
                         join packageList in packageListData on process.Uid equals packageList.Uid into pld
                         from packageList in pld.DefaultIfEmpty()
                         join parentProcess in processData on process.ParentUpid equals parentProcess.Upid into pp
                         from parentProcess in pp.DefaultIfEmpty()
                         select new { process, args, packageList, parentProcess };

            // Create events out of the joined results
            foreach (var result in joined)
            {
                var args = Args.ParseArgs(result.args);
                MaximumArgsEventFieldCount = Math.Max(MaximumArgsEventFieldCount, args.ArgKeys.Count());

                const string ChromeProcessLabel = "chrome.process_label[0]";
                string processLabel = null;
                if (args.ArgKeys.Contains(ChromeProcessLabel))
                {
                    processLabel = (string) args.Values[args.ArgKeys.IndexOf(ChromeProcessLabel)];
                }

                var ev = new PerfettoProcessEvent
                (
                    result.process.Id,
                    result.process.Type,
                    result.process.Upid,
                    result.process.Pid,
                    result.process.Name,
                    processLabel,
                    result.process.RelativeStartTimestamp.HasValue ? new Timestamp(result.process.RelativeStartTimestamp.Value) : Timestamp.Zero,
                    result.process.RelativeEndTimestamp.HasValue ? new Timestamp(result.process.RelativeEndTimestamp.Value) : Timestamp.MaxValue,
                    result.process.ParentUpid,
                    result.parentProcess,
                    result.process.Uid,
                    result.process.AndroidAppId,
                    result.process.CmdLine,
                    args.ArgKeys.ToArray(),
                    args.Values.ToArray(),
                    result.packageList
                );
                this.ProcessEvents.AddEvent(ev);
            }
            this.ProcessEvents.FinalizeData();
        }
    }
}
