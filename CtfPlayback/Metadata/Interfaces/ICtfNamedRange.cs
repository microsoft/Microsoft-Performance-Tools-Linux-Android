// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfPlayback.Metadata.Interfaces
{
    /// <summary>
    /// A named range used in CTF enumerators.
    /// </summary>
    public interface ICtfNamedRange
    {
        /// <summary>
        /// Range name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The ranges associated with the name
        /// </summary>
        IReadOnlyList<ICtfIntegerRange> Ranges { get; }

        /// <summary>
        /// Determines if the named range contains the given value
        /// </summary>
        /// <param name="value">Value to search for</param>
        /// <returns>true if the value is found</returns>
        bool ContainsValue(long value);

        /// <summary>
        /// Determines if the named range contains the given value
        /// </summary>
        /// <param name="value">Value to search for</param>
        /// <returns>true if the value is found</returns>
        bool ContainsValue(ulong value);
    }
}