// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfPlayback.Metadata.Interfaces
{
    /// <summary>
    /// Represents a CTF event
    /// </summary>
    public interface ICtfEventDescriptor
    {
        /// <summary>
        /// Event context type
        /// </summary>
        ICtfTypeDescriptor Context { get; }

        /// <summary>
        /// Event payload type
        /// </summary>
        ICtfTypeDescriptor Payload { get; }
    }
}