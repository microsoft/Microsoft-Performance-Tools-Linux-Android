// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using CtfPlayback;
using PerfCds.CookerData;
using PerfDataExtensions.DataOutputTypes;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Extensibility.DataCooking.SourceDataCooking;
using Microsoft.Performance.SDK.Processing;

namespace PerfDataExtensions.SourceDataCookers
{
    public class PerfGenericEventDataCooker
        : PerfBaseSourceCooker
    {
        public const string Identifier = "GenericEvents";

        public override string Description => "All events reported in the source.";

        public PerfGenericEventDataCooker()
            : base(Identifier)
        {
            this.Events = new ProcessedEventData<PerfGenericEvent>();
        }

        /// <summary>
        /// No specific data keys for generic events, rather, the ReceiveAllEvents option is set.
        /// </summary>
        public override ReadOnlyHashSet<string> DataKeys => EmptyDataKeys;

        /// <summary>
        /// This data cooker receives all data elements.
        /// </summary>
        public override SourceDataCookerOptions Options => SourceDataCookerOptions.ReceiveAllDataElements;

        public override DataProcessingResult CookDataElement(
            PerfEvent data, 
            PerfContext context, 
            CancellationToken cancellationToken)
        {
            try
            {
                Events.AddEvent(new PerfGenericEvent(data, context));

                this.MaximumEventFieldCount =
                    Math.Max(data.Payload.Fields.Count, this.MaximumEventFieldCount);
            }
            catch (CtfPlaybackException e)
            {
                Console.Error.WriteLine($"Error consuming event: {e.Message}");
                return DataProcessingResult.CorruptData;
            }

            return DataProcessingResult.Processed;
        }

        public override void EndDataCooking(CancellationToken cancellationToken)
        {
            this.Events.FinalizeData();
        }

        [DataOutput]
        public ProcessedEventData<PerfGenericEvent> Events { get; }

        /// <summary>
        /// The highest number of fields found in any single event.
        /// </summary>
        [DataOutput]
        public int MaximumEventFieldCount { get; private set; }
    }
}
