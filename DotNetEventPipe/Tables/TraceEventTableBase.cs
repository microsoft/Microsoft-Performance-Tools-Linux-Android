// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing.StackSources;
using Microsoft.Performance.SDK.Processing;

namespace DotNetEventPipe.Tables
{
    public abstract class TraceEventTableBase
    {
        protected TraceEventTableBase(IReadOnlyDictionary<string, TraceEventProcessor> traceEventProcessor)
        {
            this.TraceEventProcessor = traceEventProcessor;
        }

        //
        // In this sample we are going to assume the files will fit in memory,
        // and so we will make sure all tables have access to the collection of lines in the file.
        //

        public IReadOnlyDictionary<string, TraceEventProcessor> TraceEventProcessor { get; }

        //
        // All tables will need some way to build themselves via the ITableBuilder interface.
        //

        public abstract void Build(ITableBuilder tableBuilder);
    }
}
