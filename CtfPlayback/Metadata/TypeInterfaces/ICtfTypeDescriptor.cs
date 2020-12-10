// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.FieldValues;

namespace CtfPlayback.Metadata.TypeInterfaces
{
    /// <summary>
    /// Describes a type in a CTF metadata file
    /// </summary>
    public interface ICtfTypeDescriptor
    {
        /// <summary>
        /// Type of the type
        /// </summary>
        CtfTypes CtfType { get; }

        /// <summary>
        /// Bit alignment for the type
        /// </summary>
        int Align { get; }

        /// <summary>
        /// Read an instance of the type from an event stream
        /// </summary>
        /// <param name="reader">Source to read from</param>
        /// <param name="parent">Container of the instance of this type</param>
        /// <returns>An instance of this type read from the source</returns>
        CtfFieldValue Read(IPacketReader reader, CtfFieldValue parent = null);
    }
}