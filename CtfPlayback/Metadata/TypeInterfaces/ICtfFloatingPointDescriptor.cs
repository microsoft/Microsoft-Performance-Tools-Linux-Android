// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CtfPlayback.Metadata.TypeInterfaces
{
    /// <summary>
    /// Describes the properties of a floating point type.
    /// </summary>
    public interface ICtfFloatingPointDescriptor
        : ICtfTypeDescriptor
    {
        /// <summary>
        /// Byte order layout
        /// </summary>
        string ByteOrder { get; }

        /// <summary>
        /// Exponent
        /// </summary>
        int Exponent { get; }

        /// <summary>
        /// Mantissa
        /// </summary>
        int Mantissa { get; }
    }
}