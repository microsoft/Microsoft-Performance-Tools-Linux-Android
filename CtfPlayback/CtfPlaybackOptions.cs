// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CtfPlayback
{
    /// <summary>
    /// Options for controlling event playback.
    /// </summary>
    public struct CtfPlaybackOptions
    {
        /// <summary>
        /// When this is set, each stream will attempt to pre-read events so they are immediately ready
        /// for processing. There is some overhead with this approach, and will not always be the optimal
        /// approach.
        /// </summary>
        public bool ReadAhead { get; set; }
    }
}