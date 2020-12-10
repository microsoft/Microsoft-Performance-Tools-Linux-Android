// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace CtfPlayback.FieldValues
{
    /// <summary>
    /// Provides options for string formatting.
    /// </summary>
    [Flags]
    public enum GetValueAsStringOptions
    {
        /// <summary>
        /// Default value, no options specified
        /// </summary>
        NoOptions = 0,

        /// <summary>
        /// Strings should include quotes around the value.
        /// </summary>
        QuotesAroundStrings,

        /// <summary>
        /// If there is an underscore at the beginning of the string, remove the first underscore.
        /// </summary>
        TrimBeginningUnderscore,
    }
}