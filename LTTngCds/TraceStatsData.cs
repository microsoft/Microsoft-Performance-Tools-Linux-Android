// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace LTTngCds
{
    /// <summary>
    /// This is a class, not a struct, because its values are updated throughout CTF playback.
    /// </summary>
    internal class TraceStatsData
    {
        /// <summary>
        /// The number of events of a given type in a given trace.
        /// </summary>
        public ulong EventCount;

        /// <summary>
        /// The total payload size, in bits, of the given event in the given trace.
        /// </summary>
        public ulong PayloadBitCount;
    }
}