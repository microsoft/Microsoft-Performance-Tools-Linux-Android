// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using CtfPlayback.Metadata.AntlrParser;
using CtfPlayback.Metadata.Helpers;
using CtfPlayback.Metadata.Types;

/// <summary>
/// This partial class incorporates additional state data in many of the rule context classes.
/// The per-rule data allows us to process and aggregate the data necessary at lower level rules to
/// then process data at higher levels.
/// <remarks>
/// This class is only public because the Antlr generated code makes it public, and I prefer not to modify
/// generated code unless absolutely necessary - as it would make regenerating the code a more involved process.
/// </remarks>
/// </summary>
public partial class CtfParser
{
    /// <summary>
    /// Adds context
    /// </summary>
    public partial class Declaration_specifiersContext
    {
        internal List<string> PrefixTokens { get; } = new List<string>();

        internal CtfMetadataTypeDescriptor MetadataType { get; set; }

        internal List<string> SuffixTokens { get; } = new List<string>();
    }

    /// <summary>
    /// Adds context
    /// </summary>
    public partial class Struct_type_specifierContext
    {
        internal CtfStructDescriptor TypeSpecifier { get; set; }
    }

    /// <summary>
    /// Adds context
    /// </summary>
    public partial class Variant_type_specifierContext
    {
        internal CtfVariantDescriptor TypeSpecifier { get; set; }
    }

    /// <summary>
    /// Adds context
    /// </summary>
    public partial class Type_specifierContext
    {
        internal CtfMetadataTypeDescriptor TypeSpecifier { get; set; }
    }

    /// <summary>
    /// Adds context
    /// </summary>
    public partial class Unary_expressionContext
    {
        internal List<PostfixExpressionValue> PostfixValues { get; } = new List<PostfixExpressionValue>();
    }

    /// <summary>
    /// Adds context
    /// </summary>
    public partial class Postfix_expression_complexContext
    {
        internal List<PostfixExpressionValue> PostfixValues { get; } = new List<PostfixExpressionValue>();
    }

    /// <summary>
    /// Adds context
    /// </summary>
    public partial class Postfix_expressionContext
    {
        internal List<PostfixExpressionValue> PostfixValues { get; } = new List<PostfixExpressionValue>();
    }

    /// <summary>
    /// Adds context
    /// </summary>
    public partial class Enumerator_mappingContext
    {
        /// <summary>
        /// This is used whether there is a range or not
        /// </summary>
        internal IntegerLiteral MappingStart { get; set; }

        /// <summary>
        /// This is only used if a range is specified
        /// </summary>
        internal IntegerLiteral MappingStop { get; set; }
    }

    /// <summary>
    /// Adds context
    /// </summary>
    public partial class EnumeratorContext
    {
        internal EnumeratorMapping Mapping { get; set; }
    }

    /// <summary>
    /// Adds context
    /// </summary>
    public partial class Enumerator_listContext
    {
        internal List<EnumeratorMapping> Mappings { get; } = new List<EnumeratorMapping>();
    }

    /// <summary>
    /// Adds context
    /// </summary>
    public partial class Enum_type_specifierContext
    {
        internal CtfEnumDescriptor Enum { get; set; }
    }

    /// <summary>
    /// Adds context
    /// </summary>
    public partial class Enum_integer_type_specifierContext
    {
        internal CtfIntegerDescriptor IntegerType { get; set; }
    }

    /// <summary>
    /// Adds context
    /// </summary>
    public partial class Enum_integer_declaration_specifiersContext
    {
        internal CtfIntegerDescriptor IntegerType { get; set; }
    }

    /// <summary>
    /// Adds context
    /// </summary>
    public partial class DeclaratorContext
    {
        internal string Identifier { get; set; }

        // IDENTIFIER [ indexedValue ]
        internal List<PostfixExpressionValue> UnaryExpressionValues { get; } = new List<PostfixExpressionValue>();
    }

    /// <summary>
    /// Adds context
    /// </summary>
    public partial class IntegerLiteralContext
    {
        internal IntegerLiteral Value { get; set; }
    }

    /// <summary>
    /// Adds context
    /// </summary>
    public partial class Dynamic_referenceContext
    {
        internal string DynamicScopePath { get; set; }
    }
}
