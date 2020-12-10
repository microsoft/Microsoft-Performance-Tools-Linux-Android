// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace CtfPlayback.Inputs
{
    /// <summary>
    /// A set of traces be processed.
    /// </summary>
    public interface ICtfInput
        : IDisposable
    {
        /// <summary>
        /// A set of traces be processed.
        /// </summary>
        IReadOnlyList<ICtfTraceInput> Traces { get; }
    }
}