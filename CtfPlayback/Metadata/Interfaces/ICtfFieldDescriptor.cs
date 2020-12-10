// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.FieldValues;
using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfPlayback.Metadata.Interfaces
{
    /// <summary>
    /// Represents a field from CTF metadata
    /// </summary>
    public interface ICtfFieldDescriptor
    {
        /// <summary>
        /// The type of the field
        /// </summary>
        ICtfTypeDescriptor TypeDescriptor { get; }

        /// <summary>
        /// The name of the field
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Reads the field from a packet of an event stream
        /// </summary>
        /// <param name="reader">Packet reader</param>
        /// <param name="parent">Container of this field</param>
        /// <returns>The field value</returns>
        CtfFieldValue Read(IPacketReader reader, CtfFieldValue parent = null);
    }
}