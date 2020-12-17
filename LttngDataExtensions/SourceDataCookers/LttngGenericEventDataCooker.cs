// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Collections.Generic;
using CtfPlayback;
using LttngCds.CookerData;
using LttngDataExtensions.DataOutputTypes;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Extensibility.DataCooking.SourceDataCooking;
using Microsoft.Performance.SDK.Processing;

namespace LttngDataExtensions.SourceDataCookers
{
    public class LttngGenericEventDataCooker
        : LttngBaseSourceCooker
    {
        public const string Identifier = "GenericEvents";

        public override string Description => "All events reported in the source.";

        public LttngGenericEventDataCooker()
            : base(Identifier)
        {
            this.Events = new ProcessedEventData<LttngGenericEvent>();
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
            LttngEvent data, 
            LttngContext context, 
            CancellationToken cancellationToken)
        {
            try
            {
                Events.AddEvent(new LttngGenericEvent(data, context));

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
        public ProcessedEventData<LttngGenericEvent> Events { get; }


        /// <summary>
        /// The highest number of fields found in any single event.
        /// </summary>
        [DataOutput]
        public int MaximumEventFieldCount { get; private set; }
    }
}
