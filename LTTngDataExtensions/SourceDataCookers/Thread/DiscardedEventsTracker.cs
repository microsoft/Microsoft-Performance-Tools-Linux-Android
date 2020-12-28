// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using LTTngCds.CookerData;

namespace LTTngDataExtensions.SourceDataCookers.Thread
{
    class DiscardedEventsTracker
    {
        private List<uint> discardedEventsPerCpu = new List<uint>();

        public uint EventsDiscardedBetweenLastTwoEvents(LTTngEvent data, LTTngContext context)
        {
            while (this.discardedEventsPerCpu.Count <= context.CurrentCpu)
            {
                this.discardedEventsPerCpu.Add(0);
            }
            uint previousDiscardedEvents = this.discardedEventsPerCpu[(int)context.CurrentCpu];
            if (previousDiscardedEvents < data.DiscardedEvents)
            {
                discardedEventsPerCpu[(int)context.CurrentCpu] = data.DiscardedEvents;
                return data.DiscardedEvents - previousDiscardedEvents;
            }
            return 0;
        }
    }
}
