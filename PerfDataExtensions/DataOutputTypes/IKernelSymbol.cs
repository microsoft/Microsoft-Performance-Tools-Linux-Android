// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PerfDataExtensions.SourceDataCookers.Symbols;
using System;
using System.Collections.Generic;
using System.Text;

namespace PerfDataExtensions.DataOutputTypes
{
    public interface IKernelSymbol
    {
        ulong Address { get;  }
        SymbolType SymbolType { get; } // Types can be decoded from 'man nm' but not used right now
        string Name { get; }
    }
}
