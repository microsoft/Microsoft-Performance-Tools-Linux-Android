// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata.Helpers;
using System.Diagnostics;

namespace CtfPlayback.Metadata.AntlrParser
{
    internal enum PostfixExpressionType
    {
        IntegerLiteral,
        StringLiteral,
        CharacterLiteral,
        Identifier,
        DynamicReference,
        Positive,
        Negative,
        IndexedExpression,
    }

    internal abstract class PostfixExpressionValue
    {
        internal PostfixExpressionValue(PostfixExpressionType valueType)
        {
            this.Type = valueType;
        }

        internal PostfixExpressionType Type { get; }

        internal abstract string ValueAsString { get; }
    }

    internal class IntegerLiteralPostfixExpressionValue
        : PostfixExpressionValue
    {
        internal IntegerLiteralPostfixExpressionValue(string value)
            : base(PostfixExpressionType.IntegerLiteral)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(value));

            this.ValueAsString = value;
        }

        internal IntegerLiteralPostfixExpressionValue(IntegerLiteral value)
            : base(PostfixExpressionType.IntegerLiteral)
        {
            Debug.Assert(value != null);

            this.Value = value;
            this.ValueAsString = value.ToString();
        }

        internal override string ValueAsString { get; }

        internal IntegerLiteral Value { get; }
    }

    internal class StringLiteralPostfixExpressionValue
        : PostfixExpressionValue
    {
        internal StringLiteralPostfixExpressionValue(string value)
            : base(PostfixExpressionType.StringLiteral)
        {
            Debug.Assert(null != value && (!" ".Equals(value)));

            this.ValueAsString = value;
        }

        internal override string ValueAsString { get; }
    }

    internal class CharacterLiteralPostfixExpressionValue
        : PostfixExpressionValue
    {
        internal CharacterLiteralPostfixExpressionValue(char value)
            : base(PostfixExpressionType.CharacterLiteral)
        {
            this.Value = value;
        }

        internal char Value { get; }

        internal override string ValueAsString => this.Value.ToString();
    }

    internal class IdentifierPostfixExpressionValue
        : PostfixExpressionValue
    {
        internal IdentifierPostfixExpressionValue(string value)
            : base(PostfixExpressionType.Identifier)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(value));

            this.ValueAsString = value;
        }

        internal override string ValueAsString { get; }
    }

    internal class DynamicReferencePostfixExpressionValue
        : PostfixExpressionValue
    {
        internal DynamicReferencePostfixExpressionValue(string value)
            : base(PostfixExpressionType.DynamicReference)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(value));

            this.ValueAsString = value;
        }

        internal override string ValueAsString { get; }
    }

    internal class PositivePostfixExpressionValue
        : PostfixExpressionValue
    {
        internal PositivePostfixExpressionValue()
            : base(PostfixExpressionType.Positive)
        {
        }

        internal override string ValueAsString => "+";
    }

    internal class NegativePostfixExpressionValue
        : PostfixExpressionValue
    {
        internal NegativePostfixExpressionValue()
            : base(PostfixExpressionType.Negative)
        {
        }

        internal override string ValueAsString => "-";
    }
}