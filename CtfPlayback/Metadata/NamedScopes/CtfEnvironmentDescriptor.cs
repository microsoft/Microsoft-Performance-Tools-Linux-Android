// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata.Interfaces;
using CtfPlayback.Metadata.InternalHelpers;
using System.Collections.Generic;

namespace CtfPlayback.Metadata.NamedScopes
{
    /// <summary>
    /// The environment the trace was taken in.
    /// The CTF specification doesn't specify values for this scope. It is up to the implementation.
    /// </summary>
    internal class CtfEnvironmentDescriptor
        : ICtfEnvironmentDescriptor
    {
        internal CtfEnvironmentDescriptor(CtfPropertyBag bag)
        {
            this.Properties = bag.Assignments;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Properties { get; }
    }
}