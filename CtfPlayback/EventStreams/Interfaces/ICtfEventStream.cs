// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Inputs;
using CtfPlayback.Metadata.Interfaces;

namespace CtfPlayback.EventStreams.Interfaces
{
    /// <summary>
    /// Data necessary to read and process a CTF event stream.
    /// </summary>
    public interface ICtfEventStream
        : ICtfInputStream
    {
        /// <summary>
        /// Metadata associated with the event stream.
        /// </summary>
        ICtfMetadata Metadata { get; }

        /// <summary>
        /// Extensibility points for processing the event stream.
        /// </summary>
        ICtfPlaybackCustomization PlaybackCustomization { get; }
    }
}