﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Performance.SDK;

namespace LTTngDataExtensions.SourceDataCookers.Diagnostic_Messages
{
    public interface IDiagnosticMessage
    {
        string Message { get; }
        Timestamp Timestamp { get; }
    }
}
