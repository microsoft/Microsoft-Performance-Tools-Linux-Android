// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using CtfPlayback.FieldValues;
using Microsoft.Performance.SDK;

namespace LTTngDataExtensions.DataOutputTypes
{
    public interface ISyscallEvent
    {
        string Name { get; }
        int Tid { get; }
        Timestamp Timestamp { get; }
        bool IsEntry { get; }
        IReadOnlyDictionary<string, CtfFieldValue> Fields { get; }
    }
}
