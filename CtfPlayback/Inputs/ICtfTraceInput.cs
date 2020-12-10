// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace CtfPlayback.Inputs
{
    /// <summary>
    /// Data about a trace necessary to process the trace.
    /// </summary>
    public interface ICtfTraceInput
        : IDisposable
    {
        /// <summary>
        /// Trace metadata stream.
        /// </summary>
        ICtfInputStream MetadataStream { get; }

        /// <summary>
        /// Trace event streams.
        /// </summary>
        IList<ICtfInputStream> EventStreams { get; }
    }
}