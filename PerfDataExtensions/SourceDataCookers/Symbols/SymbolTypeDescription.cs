// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace PerfDataExtensions.SourceDataCookers.Symbols
{
    public class SymbolTypeDescription
    {
        // From Linux 'man nm'
        public static List<SymbolTypeDescription> SymbolTypes = new List<SymbolTypeDescription>()
        {
            { new SymbolTypeDescription('A', SymbolScopeDef.Global_External, "Absolute",                       "The symbol's value is absolute, and will not be changed by further linking.") },
            { new SymbolTypeDescription('b', SymbolScopeDef.Both, "Uninit_Data_Section(BSS)",                  "The symbol is in the uninitialized data section (known as BSS ).") },
            { new SymbolTypeDescription('C', SymbolScopeDef.Global_External, "Common_Uninit",                  "The symbol is common. Common symbols are uninitialized data. When linking, multiple common symbols may appear with the same name. If the symbol is defined anywhere, the common symbols are treated as undefined references.") },
            { new SymbolTypeDescription('d', SymbolScopeDef.Both, "Init_Data_Section",                         "The symbol is in the initialized data section.") },
            { new SymbolTypeDescription('g', SymbolScopeDef.Both, "Init_Data_Section_Small_Objects",           "The symbol is in an initialized data section for small objects. Some object file formats permit more efficient access to small data objects, such as a global int variable as opposed to a large global array.") },
            { new SymbolTypeDescription('i', SymbolScopeDef.Local, "Runtime_ELF_IndirectFunc_PE_Dll_Specific", "For PE format files this indicates that the symbol is in a section specific to the implementation of DLLs. For ELF format files this indicates that the symbol is an indirect function. This is a GNU extension to the standard set of ELF symbol types. It indicates a symbol which if referenced by a relocation does not evaluate to its address, but instead must be invoked at runtime. The runtime execution will then return the value to be used in the relocation.") },
            { new SymbolTypeDescription('N', SymbolScopeDef.Global_External, "Debug",                          "The symbol is a debugging symbol.") },
            { new SymbolTypeDescription('p', SymbolScopeDef.Local, "Stack_Unwind_Section",                     "The symbols is in a stack unwind section.") },
            { new SymbolTypeDescription('r', SymbolScopeDef.Both, "RO_Data_Section",                           "The symbol is in a read only data section.") },
            { new SymbolTypeDescription('s', SymbolScopeDef.Both, "Uninit_Data_Section_Small_Objects",         "The symbol is in an uninitialized data section for small objects.") },
            { new SymbolTypeDescription('t', SymbolScopeDef.Both, "Text/Code_Section",                         "The symbol is in the text (code) section.") },
            { new SymbolTypeDescription('U', SymbolScopeDef.Global_External, "Undefined",                      "The symbol is undefined.") },
            { new SymbolTypeDescription('u', SymbolScopeDef.Local, "Unique_Global",                            "The symbol is a unique global symbol. This is a GNU extension to the standard set of ELF symbol bindings. For such a symbol the dynamic linker will make sure that in the entire process there is just one symbol with this name and type in use.") },
            { new SymbolTypeDescription('v', SymbolScopeDef.Both, "Weak_Object",                               "The symbol is a weak object. When a weak defined symbol is linked with a normal defined symbol, the normal defined symbol is used with no error. When a weak undefined symbol is linked and the symbol is not defined, the value of the weak symbol becomes zero with no error. On some systems, uppercase indicates that a default value has been specified.") },
            { new SymbolTypeDescription('w', SymbolScopeDef.Both, "Weak",                                      "The symbol is a weak symbol that has not been specifically tagged as a weak object symbol. When a weak defined symbol is linked with a normal defined symbol, the normal defined symbol is used with no error. When a weak undefined symbol is linked and the symbol is not defined, the value of the symbol is determined in a system-specific manner without error. On some systems, uppercase indicates that a default value has been specified.") },
            { new SymbolTypeDescription('-', SymbolScopeDef.Local, "Stabs",                                    "The symbol is a stabs symbol in an a.out object file. In this case, the next values printed are the stabs other field, the stabs desc field, and the stab type. Stabs symbols are used to hold debugging information.") },
            { new SymbolTypeDescription('?', SymbolScopeDef.Both, "Unknown",                                   "The symbol type is unknown, or object file format specific.") },
        };


        /// <summary>
        /// Use constructor when setting up static list of symbol descriptions
        /// </summary>
        /// <param name="symbolType"></param>
        /// <param name="symbolTypeShortName"></param>
        /// <param name="symbolDescription"></param>
        private SymbolTypeDescription(char symbolType, SymbolScopeDef symbolScopeDef, string symbolTypeShortName, string symbolDescription)
        {
            SymbolType = symbolType;
            SymbolScopeDef = symbolScopeDef;
            SymbolTypeShortName = symbolTypeShortName;
            SymbolDescription = symbolDescription;
        }

        public char SymbolType { get;  }
        public SymbolScopeDef SymbolScopeDef { get; }
        public string SymbolTypeShortName { get; }
        public string SymbolDescription { get; }
    }

    public enum SymbolScopeDef
    {
        Local,
        Global_External,
        Both,
    };
}
