using Microsoft.Diagnostics.Tracing.Etlx;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetEventpipe.DataOutputTypes
{
    public class TraceCallStackProcessed
    {
        public string[] CallStack { get; set; }
        public TraceModuleFile Module { get; set; }
        public string FullMethodName { get; set; }
    }
}
