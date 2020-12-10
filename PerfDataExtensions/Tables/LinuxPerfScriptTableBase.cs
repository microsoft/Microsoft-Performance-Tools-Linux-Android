// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Diagnostics.Tracing.StackSources;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;

namespace PerfDataExtensions.Tables
{
    //
    // This is a sample base class for all regular and metadata tables in your project which helps simplify management of them.
    // 
    // A table is a logical collection of similar data points. 
    // Things like CPU Usage, Thread Lifetimes, File I/O, etc. are all tables
    //
    // There is no explicit table interface so as to give you flexibility in how you implement your tables.
    // All that matters is that you have some way of getting the data out of the data files and into the ITableBuilder in CreateTable   
    // in order for analyzer to understand your data.
    //

    public abstract class LinuxPerfScriptTableBase
    {
        protected LinuxPerfScriptTableBase(IReadOnlyDictionary<string, ParallelLinuxPerfScriptStackSource> perfDataTxtLogParsed)
        {
            this.PerfDataTxtLogParsed = perfDataTxtLogParsed;
        }

        //
        // In this sample we are going to assume the files will fit in memory,
        // and so we will make sure all tables have access to the collection of lines in the file.
        //

        public IReadOnlyDictionary<string, ParallelLinuxPerfScriptStackSource> PerfDataTxtLogParsed { get; }

        //
        // All tables will need some way to build themselves via the ITableBuilder interface.
        //

        public abstract void Build(ITableBuilder tableBuilder);
    }
}
