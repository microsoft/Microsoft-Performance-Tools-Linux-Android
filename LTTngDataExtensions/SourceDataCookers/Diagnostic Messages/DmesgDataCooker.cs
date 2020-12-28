// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using LTTngCds.CookerData;
using CtfPlayback;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;

namespace LTTngDataExtensions.SourceDataCookers.Diagnostic_Messages
{
    public class LTTngDmesgDataCooker
        : LTTngBaseSourceCooker
    {
        public const string Identifier = "DmesgDataCooker";
        public const string CookerPath = LTTngConstants.SourceId + "/" + Identifier;

        public LTTngDmesgDataCooker()
            : base(Identifier)
        {
        }

        public override string Description => "Processes LTTng events that are diagnostic messages.";


        private static string DmesgDataKey = "printk_console";
        private static HashSet<string> DataKeySet = new HashSet<string>() { DmesgDataKey };
        public override ReadOnlyHashSet<string> DataKeys =>  new ReadOnlyHashSet<string>(DataKeySet);

        private readonly List<DiagnosticMessage> diagnosticMessages = new List<DiagnosticMessage>();

        [DataOutput]
        public IReadOnlyList<IDiagnosticMessage> DiagnosticMessages => this.diagnosticMessages;

        public override DataProcessingResult CookDataElement(LTTngEvent data, LTTngContext context, CancellationToken cancellationToken)
        {
            try
            {
                if (DmesgDataKey.Equals(data.Name))
                {
                    ProcessDiagnosticMessage(data);
                    return DataProcessingResult.Processed;
                }
                else
                {
                    return DataProcessingResult.Ignored;
                }
            }
            catch (CtfPlaybackException e)
            {
                Console.Error.WriteLine(e);
                return DataProcessingResult.CorruptData;
            }
        }

        private void ProcessDiagnosticMessage(LTTngEvent data)
        {
            string logLine = data.Payload.FieldsByName["_msg"].GetValueAsString();
            Timestamp timestamp = data.Timestamp;
            string message = logLine;
            int messageStartIndex = 1;
            while (messageStartIndex < logLine.Length && logLine[messageStartIndex] != ']')
            {
                ++messageStartIndex;
            }
            if (messageStartIndex > 1 && messageStartIndex < logLine.Length)
            {
                if (Double.TryParse(logLine.Substring(1, messageStartIndex-1), out double timestampDouble))
                {
                    timestamp = new Timestamp((long)(timestampDouble * 1000000000.0));
                }
                if (messageStartIndex < logLine.Length-1 && logLine[messageStartIndex+1]==' ')
                {
                    ++messageStartIndex;
                }
                message = logLine.Substring(messageStartIndex);
            }
            this.diagnosticMessages.Add(new DiagnosticMessage(message, timestamp));
        }
    }
}
