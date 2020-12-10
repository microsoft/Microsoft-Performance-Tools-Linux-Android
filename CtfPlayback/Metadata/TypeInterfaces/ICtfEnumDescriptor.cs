// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata.Interfaces;
using System.Collections.Generic;

namespace CtfPlayback.Metadata.TypeInterfaces
{
    /// <summary>
    /// Describe a CTF enum. A CTF enum consists of a set of values mapped to an integer value or range (or ranges).
    /// The exact integer properties are not defined by CTF, but are specified in the metadata.
    /// </summary>
    public interface ICtfEnumDescriptor
        : ICtfTypeDescriptor
    {
        /// <summary>
        /// The integer type of the mapped value or range.
        /// </summary>
        ICtfIntegerDescriptor BaseType { get; }
        
        /// <summary>
        /// The mapped values/ranges in the enum.
        /// </summary>
        IEnumerable<ICtfNamedRange> EnumeratorValues { get; }

        /// <summary>
        /// Returns an enum value that is mapped to the given integer value.
        /// </summary>
        /// <param name="value">A mapped value</param>
        /// <returns>The enum value represented by the given integer value</returns>
        string GetName(ulong value);

        /// <summary>
        /// Returns an enum value that is mapped to the given integer value.
        /// </summary>
        /// <param name="value">A mapped value</param>
        /// <returns>The enum value represented by the given integer value</returns>
        string GetName(long value);

        /// <summary>
        /// Retrieve the named range for the given enum value.
        /// </summary>
        /// <param name="name">Enum value</param>
        /// <returns>Named range</returns>
        ICtfNamedRange GetValue(string name);
    }
}