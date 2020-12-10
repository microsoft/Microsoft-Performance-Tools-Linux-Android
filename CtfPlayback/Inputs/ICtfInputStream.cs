// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

namespace CtfPlayback.Inputs
{
    /// <summary>
    /// Data necessary to process an event stream.
    /// </summary>
    public interface ICtfInputStream
        : IDisposable
    {
        /// <summary>
        /// Identify the stream source.
        /// </summary>
        string StreamSource { get; }

        /// <summary>
        /// Stream from which to read.
        /// </summary>
        Stream Stream { get; }

        /// <summary>
        /// Size of the stream.
        /// This is necessary as not all streams support seeking, which is necessary for the Length property.
        /// </summary>
        ulong ByteCount { get; }
    }
}