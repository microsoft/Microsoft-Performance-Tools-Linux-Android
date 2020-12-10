// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfPlayback.Metadata.Interfaces
{
    /// <summary>
    /// CTF trace descriptor
    /// </summary>
    public interface ICtfTraceDescriptor
    {
        /// <summary>
        /// CTF major version
        /// </summary>
        short Major { get; }

        /// <summary>
        /// CTF minor version
        /// </summary>
        short Minor { get; }

        /// <summary>
        /// Trace Id
        /// </summary>
        Guid Uuid { get; }

        /// <summary>
        /// Default byte order
        /// </summary>
        string ByteOrder { get; }

        /// <summary>
        /// Packet header
        /// </summary>
        ICtfStructDescriptor PacketHeader { get; }
    }
}