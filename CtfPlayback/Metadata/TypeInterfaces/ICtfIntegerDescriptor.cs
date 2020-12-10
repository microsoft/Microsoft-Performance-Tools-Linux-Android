// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CtfPlayback.Metadata.TypeInterfaces
{
    /// <summary>
    /// Describes the properties of an integer type.
    /// </summary>
    public interface ICtfIntegerDescriptor
        : ICtfTypeDescriptor
    {
        /// <summary>
        /// The number of bits in the integer.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Whether the integer if signed.
        /// </summary>
        bool Signed { get; }

        /// <summary>
        /// When represented as a string, this is how it is encoded: UTF8 or ASCII
        /// </summary>
        string Encoding { get; }

        /// <summary>
        /// The radix. Useful when printing the value.
        /// </summary>
        short Base { get; }

        /// <summary>
        /// For integers that represent a timestamp, this identifies the clock with which the timestamp is associated.
        /// </summary>
        string Map { get; }
    }
}