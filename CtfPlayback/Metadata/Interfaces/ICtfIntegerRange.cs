// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata.Helpers;
using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfPlayback.Metadata.Interfaces
{
    /// <summary>
    /// Describes an integer range.
    /// </summary>
    public interface ICtfIntegerRange
    {
        /// <summary>
        /// The integer properties for the values in the range.
        /// </summary>
        ICtfIntegerDescriptor Base { get; }

        /// <summary>
        /// The starting value.
        /// </summary>
        IntegerLiteral Begin { get; }

        /// <summary>
        /// The ending value.
        /// </summary>
        IntegerLiteral End { get; }

        /// <summary>
        /// Determines if the range contains the given value.
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <returns>true if the range contains the value.</returns>
        bool ContainsValue(long value);

        /// <summary>
        /// Determines if the range contains the given value.
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <returns>true if the range contains the value.</returns>
        bool ContainsValue(ulong value);
    }
}