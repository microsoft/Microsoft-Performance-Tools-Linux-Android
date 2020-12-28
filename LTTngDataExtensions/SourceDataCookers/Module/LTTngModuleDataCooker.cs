// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using LTTngDataExtensions.SourceDataCookers.Thread;
using System.Threading;
using CtfPlayback;
using LTTngCds.CookerData;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Extensibility.DataCooking.SourceDataCooking;

namespace LTTngDataExtensions.SourceDataCookers.Module
{
    public class LTTngModuleDataCooker
        : LTTngBaseSourceCooker
    {
        public const string Identifier = "ModuleDataCooker";
        public const string CookerPath = LTTngConstants.SourceId + "/" + Identifier;

        private DiscardedEventsTracker discardedEventsTracker = new DiscardedEventsTracker();
        private ExecutingThreadTracker threadTracker = new ExecutingThreadTracker();

        private ICookedDataRetrieval dataRetrieval;

        public LTTngModuleDataCooker()
            : base(Identifier)
        {
        }

        private static readonly HashSet<DataCookerPath> RequiredPaths = new HashSet<DataCookerPath>
        {
            LTTngThreadDataCooker.DataCookerPath
        };

        public override IReadOnlyCollection<DataCookerPath> RequiredDataCookers => RequiredPaths;

        public override IReadOnlyDictionary<DataCookerPath, DataCookerDependencyType> DependencyTypes => dependencyTypes;

        private static readonly Dictionary<DataCookerPath, DataCookerDependencyType> dependencyTypes = new Dictionary<DataCookerPath, DataCookerDependencyType>
        {
            { LTTngThreadDataCooker.DataCookerPath, DataCookerDependencyType.AsConsumed }
        };


        public override string Description => "Processes LTTng events related to modules.";

        public override ReadOnlyHashSet<string> DataKeys => EmptyDataKeys;

        private readonly List<ModuleEvent> moduleEvents = new List<ModuleEvent>();

        [DataOutput]
        public IReadOnlyList<ModuleEvent> ModuleEvents => this.moduleEvents;

        /// <summary>
        /// This data cooker receives all data elements.
        /// </summary>
        public override SourceDataCookerOptions Options => SourceDataCookerOptions.ReceiveAllDataElements;

        public override DataProcessingResult CookDataElement(LTTngEvent data, LTTngContext context, CancellationToken cancellationToken)
        {
            try
            {
                if (this.discardedEventsTracker.EventsDiscardedBetweenLastTwoEvents(data, context) > 0)
                {
                    this.threadTracker.ReportEventsDiscarded(context.CurrentCpu);
                }
                this.threadTracker.ProcessEvent(data, context);
                if (data.Name.StartsWith("module"))
                {
                    this.moduleEvents.Add(new ModuleEvent(data, context, this.threadTracker));
                    return DataProcessingResult.Processed;
                }
                else
                {
                    return DataProcessingResult.Ignored;
                }
            }
            catch (CtfPlaybackException e)
            {
                Console.Error.WriteLine(e);
                return DataProcessingResult.CorruptData;
            }
        }

        public override void BeginDataCooking(ICookedDataRetrieval dependencyRetrieval, CancellationToken cancellationToken)
        {
            this.dataRetrieval = dependencyRetrieval;
        }
        public override void EndDataCooking(CancellationToken cancellationToken)
        {
            IThreadTracker pidTracker = this.dataRetrieval.QueryOutput<IThreadTracker>(
                new DataOutputPath(LTTngThreadDataCooker.DataCookerPath, "ThreadTracker"));

            this.moduleEvents.ForEach(x => x.SetThreadInformation(pidTracker.QueryInfo(x.Tid, x.Time)));
        }

    }
}
