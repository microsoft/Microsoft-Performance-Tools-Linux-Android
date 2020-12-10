// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace CtfPlayback.Metadata.Interfaces
{
    /// <summary>
    /// Describes the environment from CTF metadata.
    /// </summary>
    public interface ICtfEnvironmentDescriptor
    {
        // The name/value pairs specified in the descriptor.
        IReadOnlyDictionary<string, string> Properties { get; }
    }
}