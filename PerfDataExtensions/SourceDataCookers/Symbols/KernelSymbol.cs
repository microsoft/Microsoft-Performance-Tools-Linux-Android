// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PerfDataExtensions.DataOutputTypes;
using System;
using System.Diagnostics;

namespace PerfDataExtensions.SourceDataCookers.Symbols
{
    public class KernelSymbol
        : IKernelSymbol
    {

        public ulong Address { get; }
        public SymbolType SymbolType { get; }
        public string Name { get; }

        public KernelSymbol(string symbolLine)
        {
            var splitSymbol = symbolLine.Split(' ');

            if (splitSymbol.Length == 3)
            {
                Address = Convert.ToUInt64(splitSymbol[0], 16);
                SymbolType = SymbolType.GetSymbolType(splitSymbol[1][0]);
                Name = splitSymbol[2];
            }
            else
            {
                Debug.Assert(false, $"Symbol parser - didn't expect more tokens: {symbolLine}");
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as KernelSymbol);
        }

        public bool Equals(KernelSymbol other)
        {
            return other != null && Address == other.Address;
        }

        public override int GetHashCode()
        {
            return Address.GetHashCode();
        }
    }
}
