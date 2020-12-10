// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using LttngDataExtensions.DataOutputTypes;
using Microsoft.Performance.SDK;

namespace LttngDataExtensions.SourceDataCookers.Diagnostic_Messages
{
    public class DiagnosticMessage
        : IDiagnosticMessage
    {
        readonly string message;
        readonly Timestamp timestamp;

        public DiagnosticMessage(string message, Timestamp timestamp)
        {
            this.message = message;
            this.timestamp = timestamp;
        }

        public string Message => this.message;
        public Timestamp Timestamp => this.timestamp;
    }
}
