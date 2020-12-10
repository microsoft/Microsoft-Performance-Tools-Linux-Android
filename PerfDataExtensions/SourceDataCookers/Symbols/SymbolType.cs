// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PerfDataExtensions.SourceDataCookers.Symbols
{
    public class SymbolType
    {
        public static Dictionary<char, SymbolType> SymbolTypes = new Dictionary<char, SymbolType>();

        private SymbolType(char symbolTypeChar, SymbolScope symbolScope)
        {
            var symDesc = SymbolTypeDescription.SymbolTypes.SingleOrDefault(s => Char.ToLowerInvariant(s.SymbolType) == Char.ToLowerInvariant(symbolTypeChar));
            if (symDesc != null)
            {
                SymbolTypeDescription = symDesc;
            }
            else
            {
                throw new InvalidDataException($"The symbol type {symbolTypeChar} is undefined");
            }
            SymbolScope = symbolScope;
        }

        public SymbolTypeDescription SymbolTypeDescription { get; }
        public SymbolScope SymbolScope { get; }

        public static SymbolType GetSymbolType(char symbolTypeChar)
        {
            var symbolScopeType = Char.IsLower(symbolTypeChar) ? SymbolScope.Local : SymbolScope.Global_External;

            SymbolType symbolType;
            if (!SymbolTypes.TryGetValue(symbolTypeChar, out symbolType))
            {
                symbolType = new SymbolType(symbolTypeChar, symbolScopeType);
                SymbolTypes.Add(symbolTypeChar, symbolType);
            }
            return symbolType;
        }
    }

    public enum SymbolScope
    {
        Local,
        Global_External,
    };
}
