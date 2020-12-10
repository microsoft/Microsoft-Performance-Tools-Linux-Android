// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace LttngCds.CookerData
{
    //todo:do we need/want this interface? LttngContext might be enough
    public interface ICursor
    {
        /// <summary>
        /// Offset from the first earliest event in all traces
        /// </summary>
        long Timestamp { get; }

        /// <summary>
        /// CPU associated with the event
        /// </summary>
        uint CurrentCpu { get; }

        /// <summary>
        /// The event number, across all streams and traces
        /// </summary>
        ulong CurrentEventNumber { get; }

        /// <summary>
        /// The event number, within the current trace
        /// </summary>
        ulong CurrentEventNumberWithinTrace { get; }

        /// <summary>
        /// The stream in which this event exists
        /// </summary>
        string StreamSource { get; }
    }
}