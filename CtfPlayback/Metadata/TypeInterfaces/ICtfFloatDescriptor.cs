// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CtfPlayback.Metadata.TypeInterfaces
{
    /// <summary>
    /// This interface is not yet implemented
    /// </summary>
    public interface ICtfFloatDescriptor
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