// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CtfPlayback.Metadata.AntlrParser.Scopes;
using CtfPlayback.Metadata.Helpers;
using CtfPlayback.Metadata.Interfaces;
using CtfPlayback.Metadata.InternalHelpers;
using CtfPlayback.Metadata.NamedScopes;
using CtfPlayback.Metadata.TypeInterfaces;
using CtfPlayback.Metadata.Types;

namespace CtfPlayback.Metadata.AntlrParser
{
    /// <summary>
    /// This class receives callbacks from the Antlr parser when rules are hit. It is responsible for building
    /// context around individual rules into higher level constructs, specific to CTF, that may be used to parse
    /// the CTF event streams.
    /// </summary>
    internal class CtfListener
        : CtfBaseListener
    {
        internal static readonly string DefaultEnumBaseType = "int";

        private readonly CtfParser parser;
        private readonly ICtfMetadataCustomization metadataCustomization;

        private int anonymousStructCount;
        private int anonymousVariantCount;
        private int streamCount;
        private int eventCount;
        private int integerCount;
        private int floatCount;
        private int enumCount;
        private int stringCount;

        internal CtfListener(
            CtfParser parser, 
            ICtfMetadataCustomization metadataCustomization, 
            ICtfMetadataBuilder metadataBuilder)
        {
            Debug.Assert(parser != null);
            Debug.Assert(metadataCustomization != null);
            Debug.Assert(metadataBuilder != null);

            this.parser = parser;
            this.metadataCustomization = metadataCustomization;
            this.GlobalScope = new CtfGlobalScope(metadataBuilder);
            this.CurrentScope = this.GlobalScope;
        }

        internal CtfGlobalScope GlobalScope { get; }

        internal CtfScope CurrentScope { get; set; }

        public override void EnterTrace_declaration(CtfParser.Trace_declarationContext context)
        {
            this.PushTraceScope();
        }

        public override void ExitTrace_declaration(CtfParser.Trace_declarationContext context)
        {
            var traceScope = this.CurrentScope;
            Debug.Assert(StringComparer.Ordinal.Equals(traceScope.Name, "trace"));

            this.PopScope();

            if (!(this.CurrentScope is CtfGlobalScope globalScope))
            {
                throw new CtfMetadataException(
                    $"Trace scope declared outside global scope: line={context.Start.Line}");
            }

            if (!(globalScope.TraceDescriptor is null))
            {
                throw new CtfMetadataException(
                    $"Extraneous trace scope declared: line={context.Start.Line}");
            }

            // note:according to CTF specification 1.8.2 section C.Examples."Minimal Examples", packet.header is not
            // required for traces with a single packet. this isn't currently a concern, but might need to be addressed
            // at some point in the future.
            if (!traceScope.Types.TryGetValue("packet.header", out var packetHeaderDeclaration))
            {
                throw new CtfMetadataException(
                    $"No 'packet.header' declaration found for trace: line={context.Start.Line}");
            }

            if (!(packetHeaderDeclaration.Type is ICtfStructDescriptor packetHeaderStruct))
            {
                throw new CtfMetadataException(
                    $"Trace.packet.header declaration is not a struct: line={context.Start.Line}");
            }

            try
            {
                var traceDescriptor = new CtfTraceDescriptor(traceScope.PropertyBag, packetHeaderStruct);
                globalScope.SetTraceDescriptor(traceDescriptor);
            }
            catch (ArgumentException e)
            {
                throw new CtfMetadataException($"{e.Message}, line={context.Start.Line}");
            }
        }

        public override void EnterEnv_declaration(CtfParser.Env_declarationContext context)
        {
            this.PushEnvScope();
        }

        public override void ExitEnv_declaration(CtfParser.Env_declarationContext context)
        {
            Debug.Assert(StringComparer.Ordinal.Equals(this.CurrentScope.Name, "env"));

            var envPropertyBag = this.CurrentScope.PropertyBag;

            this.PopScope();

            if (!(this.CurrentScope is CtfGlobalScope globalScope))
            {
                throw new CtfMetadataException(
                    $"'env' scope declared outside global scope: line={context.Start.Line}");
            }

            if (globalScope.EnvironmentDescriptor != null)
            {
                throw new CtfMetadataException(
                    $"Extraneous 'env' scope declared: line={context.Start.Line}");
            }

            var environment = new CtfEnvironmentDescriptor(envPropertyBag);
            globalScope.SetEnvironmentDescriptor(environment);
        }

        public override void EnterClock_declaration(CtfParser.Clock_declarationContext context)
        {
            this.PushClockScope();
        }

        public override void ExitClock_declaration(CtfParser.Clock_declarationContext context)
        {
            Debug.Assert(StringComparer.Ordinal.Equals(this.CurrentScope.Name, "clock"));

            var clockPropertyBag = this.CurrentScope.PropertyBag;

            this.PopScope();

            if (!(this.CurrentScope is CtfGlobalScope globalScope))
            {
                throw new CtfMetadataException(
                    $"'clock' declared outside global scope: line={context.Start.Line}");
            }

            if (!clockPropertyBag.ContainsKey("name"))
            {
                // According to Ctf specification 1.8.2 section 8, this property is mandatory.
                throw new CtfMetadataException(
                    "'clock' declared without a name: line={context.Start.Line}");
            }

            try
            {
                globalScope.AddClock(new CtfClockDescriptor(clockPropertyBag));
            }
            catch (Exception e)
            {
                throw new CtfMetadataException($"{e.Message}, line={context.Start.Line}");
            }
        }

        public override void EnterStream_declaration(CtfParser.Stream_declarationContext context)
        {
            this.PushStreamScope();
        }

        public override void ExitStream_declaration(CtfParser.Stream_declarationContext context)
        {
            var streamScope = this.CurrentScope;
            
            Debug.Assert(streamScope.Name.StartsWith("[stream]", StringComparison.Ordinal));

            this.PopStreamScope();

            if (!(this.CurrentScope is CtfGlobalScope globalScope))
            {
                throw new CtfMetadataException(
                    $"'stream' declared outside global scope: line={context.Start.Line}");
            }

            if (!streamScope.Types.TryGetValue("event.header", out var eventHeaderDeclaration))
            {
                throw new CtfMetadataException(
                    $"No 'event.header' declaration found for stream: line={context.Start.Line}");
            }

            if (!(eventHeaderDeclaration.Type is CtfStructDescriptor eventHeader))
            {
                throw new CtfMetadataException(
                    $"'stream.event.header' declaration is not a struct: line={context.Start.Line}");
            }

            if (!streamScope.Types.TryGetValue("packet.context", out var packetContextDeclaration))
            {
                throw new CtfMetadataException(
                    $"No 'packet.context' declaration found for stream: line={context.Start.Line}");
            }

            if (!(packetContextDeclaration.Type is CtfStructDescriptor packetContext))
            {
                throw new CtfMetadataException(
                    $"'stream.packet.context' declaration is not a struct: line={context.Start.Line}");
            }

            CtfStructDescriptor eventContext = null;
            if (streamScope.Types.TryGetValue("event.context", out var eventContextDeclaration))
            {
                if (!(eventContextDeclaration.Type is CtfStructDescriptor))
                {
                    throw new CtfMetadataException(
                            $"'stream.event.context' declaration is not a struct: line={context.Start.Line}");
                }

                eventContext = (CtfStructDescriptor) eventContextDeclaration.Type;
            }

            try
            {
                var stream = new CtfStreamDescriptor(streamScope.PropertyBag, eventHeader, eventContext, packetContext);
                globalScope.AddStream(stream);
            }
            catch (Exception e)
            {
                throw new CtfMetadataException($"{e.Message}, line={context.Start.Line}");
            }
        }

        public override void EnterEvent_declaration(CtfParser.Event_declarationContext context)
        {
            this.PushEventScope();
        }

        public override void ExitEvent_declaration(CtfParser.Event_declarationContext context)
        {
            var eventScope = this.CurrentScope;

            Debug.Assert(eventScope.Name.StartsWith("[event]", StringComparison.Ordinal));

            this.PopEventScope();

            if (!eventScope.Types.TryGetValue("fields", out var fieldsDeclaration))
            {
                throw new CtfMetadataException(
                    $"No 'fields' declaration found for event: line={context.Start.Line}");
            }

            if (!(fieldsDeclaration.Type is CtfStructDescriptor fieldsStruct))
            {
                throw new CtfMetadataException(
                    $"Event 'fields' declaration is not a struct: line={context.Start.Line}");
            }

            if (!(this.CurrentScope is CtfGlobalScope globalScope))
            {
                throw new CtfMetadataException(
                    $"Event declared outside global scope: line={context.Start.Line}");
            }

            var typeDescriptors = new Dictionary<string, ICtfTypeDescriptor>();
            foreach (var eventType in eventScope.Types)
            {
                typeDescriptors.Add(eventType.Key, eventType.Value.Type);
            }

            try
            {
                globalScope.AddEvent(eventScope.PropertyBag.Assignments, typeDescriptors);
            }
            catch(Exception e)
            {
                throw new CtfMetadataException($"{e.Message}, line={context.Start.Line}");
            }
        }

        public override void ExitTypeSpecifierEnum(CtfParser.TypeSpecifierEnumContext context)
        {
            var enumType = context.enum_type_specifier().Enum;
            if (enumType != null)
            {
                context.TypeSpecifier = enumType;
            }
        }

        public override void EnterAnonymousEnumTypeDefaultBase(CtfParser.AnonymousEnumTypeDefaultBaseContext context)
        {
            this.PushEnumScope();
        }

        /// <summary>
        /// An enum with a default base uses the type 'int', which must be declared prior to the enum.
        /// Anonymous enumerations may be used to define a field or as part of a typedef declaration.
        ///
        /// This is the first level where all of the necessary enum context is available to put the
        /// enum together, adding:
        ///     - the base integer type
        ///     - the enumerator id (opt)
        /// </summary>
        /// <param name="context">Parsing rule context</param>
        public override void ExitAnonymousEnumTypeDefaultBase(CtfParser.AnonymousEnumTypeDefaultBaseContext context)
        {
            this.PopEnumScope();

            var typeDeclaration = this.CurrentScope.FindTypeByName(DefaultEnumBaseType);
            if (typeDeclaration == null)
            {
                throw new CtfMetadataException(
                    "An enum without a specified base type uses 'int', which must be specified " +
                    $"prior to the enum: line={context.Start.Line}");
            }

            if (!(typeDeclaration.Type is CtfIntegerDescriptor integerBase))
            {
                throw new CtfMetadataException(
                    $"An enum specifies a base type not based on integer: line={context.Start.Line}");
            }

            context.Enum = new CtfEnumDescriptor(integerBase);
            this.ExtractEnumeratorValues(context, context.enumerator_list().Mappings);
        }

        public override void EnterAnonymousEnumTypeSpecifiedBase(CtfParser.AnonymousEnumTypeSpecifiedBaseContext context)
        {
            this.PushEnumScope();
        }

        public override void ExitAnonymousEnumTypeSpecifiedBase(CtfParser.AnonymousEnumTypeSpecifiedBaseContext context)
        {
            this.PopEnumScope();

            var integerBase = context.enum_integer_declaration_specifiers().IntegerType;
            if (integerBase == null)
            {
                Debug.Assert(false, "The integer base of an enum was not set. This should have thrown an exception.");
                throw new CtfMetadataException("The integer base of an exception was not set.",
                    new InvalidOperationException(
                        "An enum without a proper integer base should have thrown an exception before reaching this point."));
            }

            context.Enum = new CtfEnumDescriptor(integerBase);
            this.ExtractEnumeratorValues(context, context.enumerator_list().Mappings);
        }

        public override void EnterNamedEnumTypeDefaultBase(CtfParser.NamedEnumTypeDefaultBaseContext context)
        {
            var enumName = context.IDENTIFIER().GetText();
            this.PushEnumScope(enumName);
        }

        public override void ExitNamedEnumTypeDefaultBase(CtfParser.NamedEnumTypeDefaultBaseContext context)
        {
            this.PopEnumScope();
        }

        public override void EnterNamedEnumTypeSpecifiedBase(CtfParser.NamedEnumTypeSpecifiedBaseContext context)
        {
            var enumName = context.IDENTIFIER().GetText();
            this.PushEnumScope(enumName);
        }

        public override void ExitNamedEnumTypeSpecifiedBase(CtfParser.NamedEnumTypeSpecifiedBaseContext context)
        {
            this.PopEnumScope();

            string typeName = context.IDENTIFIER().GetText();

            var integerBase = context.enum_integer_declaration_specifiers().IntegerType;
            if (integerBase == null)
            {
                Debug.Assert(false, "The integer base of an enum was not set. This should have thrown an exception.");
                throw new CtfMetadataException("The integer base of an exception was not set.",
                    new InvalidOperationException(
                        "An enum without a proper integer base should have thrown an exception before reaching this point."));
            }

            context.Enum = new CtfEnumDescriptor(integerBase);
            this.ExtractEnumeratorValues(context, context.enumerator_list().Mappings);

            var typeDeclaration = new TypeDeclaration()
            {
                DeclarationMethod = DeclarationMethod.EnumDeclaration,
                Type = context.Enum,
                TypeName = new List<string> { typeName }
            };

            this.CurrentScope.AddType(typeDeclaration);
        }

        public override void ExitEnumerator_list(CtfParser.Enumerator_listContext context)
        {
            foreach (var enumerator in context.enumerator())
            {
                context.Mappings.Add(enumerator.Mapping);
            }
        }

        public override void ExitEnumIdentifierValue(CtfParser.EnumIdentifierValueContext context)
        {
            context.Mapping = new EnumeratorMapping() {EnumIdentifier = context.IDENTIFIER().GetText()};
        }

        public override void ExitEnumIdentifierAssignedValue(CtfParser.EnumIdentifierAssignedValueContext context)
        {
            context.Mapping = new EnumeratorMapping() { EnumIdentifier = context.IDENTIFIER().GetText() };

            this.ProcessEnumMappedValues(context, context.enumerator_mapping());
        }

        public override void ExitEnumStringLiteralValue(CtfParser.EnumStringLiteralValueContext context)
        {
            string enumIdentifier = context.STRING_LITERAL().GetText().Trim('"');
            context.Mapping = new EnumeratorMapping() { EnumIdentifier = enumIdentifier };
        }

        public override void ExitEnumStringLiteralAssignedValue(CtfParser.EnumStringLiteralAssignedValueContext context)
        {
            string enumIdentifier = context.STRING_LITERAL().GetText().Trim('"');
            context.Mapping = new EnumeratorMapping() { EnumIdentifier = enumIdentifier };

            this.ProcessEnumMappedValues(context, context.enumerator_mapping());
        }

        public override void ExitEnumKeywordValue(CtfParser.EnumKeywordValueContext context)
        {
            context.Mapping = new EnumeratorMapping() { EnumIdentifier = context.keywords().GetText() };
        }

        public override void ExitEnumKeywordAssignedValue(CtfParser.EnumKeywordAssignedValueContext context)
        {
            context.Mapping = new EnumeratorMapping() { EnumIdentifier = context.keywords().GetText() };

            this.ProcessEnumMappedValues(context, context.enumerator_mapping());
        }

        public override void ExitEnumeratorMappingRange(CtfParser.EnumeratorMappingRangeContext context)
        {
            // todo:currently only allowing integer literals to be used as enum mapping values

            IntegerLiteral firstMappingValue = null;
            IntegerLiteral endMappingValue = null;

            try
            {
                foreach (var postfixExpression in context.unary_expression(0).PostfixValues)
                {
                    firstMappingValue = this.ConvertToIntegerLiteral(postfixExpression, firstMappingValue);
                }

                foreach (var postfixExpression in context.unary_expression(1).PostfixValues)
                {
                    endMappingValue = this.ConvertToIntegerLiteral(postfixExpression, endMappingValue);
                }

                context.MappingStart = firstMappingValue;
                context.MappingStop = endMappingValue;
            }
            catch (Exception e)
            {
                throw new CtfMetadataException(
                    $"Unable to process enumerator mapping range: line={context.Start.Line}", e);
            }
        }

        public override void ExitEnumeratorMappingSimple(CtfParser.EnumeratorMappingSimpleContext context)
        {
            try
            {
                IntegerLiteral mappingValue = null;
                foreach (var postfixExpression in context.unary_expression().PostfixValues)
                {
                    mappingValue = this.ConvertToIntegerLiteral(postfixExpression, mappingValue);
                }

                context.MappingStart = mappingValue;
                context.MappingStop = null;
            }
            catch (Exception e)
            {
                throw new CtfMetadataException(
                    $"Unable to process enumerator mapping value: {e.Message} line={context.Start.Line}, " + 
                    $"value='{context.unary_expression().GetText()}'");
            }
        }

        public override void ExitEnumIntegerDeclarationTypeSpecifier(CtfParser.EnumIntegerDeclarationTypeSpecifierContext context)
        {
            if (context.IntegerType != null)
            {
                throw new CtfMetadataException(
                    $"Multiple integer base types declared for enum: line={context.Start.Line}");
            }

            context.IntegerType = context.enum_integer_type_specifier().IntegerType;
        }

        public override void ExitEnumIntegerDeclarationsAndTypeSpecifier(CtfParser.EnumIntegerDeclarationsAndTypeSpecifierContext context)
        {
            if (context.IntegerType != null)
            {
                throw new CtfMetadataException(
                    $"Multiple integer base types declared for enum: line={context.Start.Line}");
            }

            if (context.enum_integer_declaration_specifiers().IntegerType != null)
            {
                throw new CtfMetadataException(
                    $"Multiple integer base types declared for enum: line={context.Start.Line}");
            }

            context.IntegerType = context.enum_integer_type_specifier().IntegerType;
        }

        public override void ExitEnumIntegerSpecifierFromType(CtfParser.EnumIntegerSpecifierFromTypeContext context)
        {
            string integerTypeName = context.IDENTIFIER().GetText();

            TypeDeclaration typeDeclaration = this.CurrentScope.FindTypeByName(integerTypeName);
            if (typeDeclaration == null)
            {
                throw new CtfMetadataException(
                    $"The specified type was not found: type={integerTypeName}, line={context.Start.Line}");
            }

            if (typeDeclaration.Type is CtfIntegerDescriptor integerType)
            {
                context.IntegerType = integerType;
                return;
            }

            throw new CtfMetadataException(
                $"The specified type is not an integer type: type={integerTypeName}, line={context.Start.Line}");
        }

        public override void ExitEnumIntegerSpecifierWithDefaults(
            CtfParser.EnumIntegerSpecifierWithDefaultsContext context)
        {
            var defaultInt = this.CurrentScope.FindTypeByName(DefaultEnumBaseType);
            if (defaultInt == null)
            {
                throw new CtfMetadataException(
                    $"Enum uses default integer ('int'), which hasn't been defined: line={context.Start.Line}");
            }

            if (!(defaultInt.Type is CtfIntegerDescriptor intDescriptor))
            {
                throw new CtfMetadataException(
                    "Enum uses default integer ('int'), which isn't defined as an integer class: " +
                    $"line={context.Start.Line}");
            }

            context.IntegerType = intDescriptor;
        }

        // This is used in an enum declaration, to specify the underlying enum storage type
        public override void EnterEnumIntegerSpecifier(CtfParser.EnumIntegerSpecifierContext context)
        {
            this.PushIntegerScope();
        }

        public override void ExitEnumIntegerSpecifier(CtfParser.EnumIntegerSpecifierContext context)
        {
            var enumScope = this.CurrentScope;

            this.PopIntegerScope();

            if (enumScope.Types.Any())
            {
                throw new CtfMetadataException(
                    $"Types may not be specified in an enumeration: line={context.Start.Line}");
            }

            try
            {
                context.IntegerType = new CtfIntegerDescriptor(enumScope.PropertyBag);
            }
            catch (Exception)
            {
                throw new CtfMetadataException(
                    $"The integer base type is invalid: line={context.Start.Line}");
            }
        }

        public override void ExitTypeSpecifierStruct(CtfParser.TypeSpecifierStructContext context)
        {
            var structTypeSpecifier = context.struct_type_specifier().TypeSpecifier;
            context.TypeSpecifier = structTypeSpecifier;
        }

        public override void ExitStructAsType(CtfParser.StructAsTypeContext context)
        {
            string structName = context.IDENTIFIER().Symbol.Text;
            var typeDeclaration = this.CurrentScope.FindTypeByName(structName);
            if (typeDeclaration == null)
            {
                throw new CtfMetadataException(
                    $"Structure was not found: {structName}, line={context.Start.Line}");
            }

            if (!(typeDeclaration.Type is CtfStructDescriptor structType))
            {
                throw new CtfMetadataException(
                    $"Error: identifier is not a struct: {structName}, line={context.Start.Line}");
            }

            context.TypeSpecifier = structType;
        }

        // 'struct IDENTIFIER { struct_or_variant_declaration_list }
        public override void EnterNamedStruct(CtfParser.NamedStructContext context)
        {
            this.PushNamedStructScope(context.IDENTIFIER().GetText());
        }

        public override void ExitNamedStruct(CtfParser.NamedStructContext context)
        {
            this.ProcessNamedStruct(context);
        }

        // 'struct IDENTIFIER { struct_or_variant_declaration_list } align(unary_expression)
        public override void EnterNamedAlignedStruct(CtfParser.NamedAlignedStructContext context)
        {
            this.PushNamedStructScope(context.IDENTIFIER().GetText());
        }

        public override void ExitNamedAlignedStruct(CtfParser.NamedAlignedStructContext context)
        {
            int alignment = this.DetermineAlignment(context.unary_expression());

            this.ProcessNamedStruct(context);
            context.TypeSpecifier.SetAlignmentProperty(alignment);
        }

        public override void EnterAnonymousAlignedStruct(CtfParser.AnonymousAlignedStructContext context)
        {
            this.PushAnonymousStructScope();
        }

        public override void ExitAnonymousAlignedStruct(CtfParser.AnonymousAlignedStructContext context)
        {
            if (context.unary_expression().PostfixValues.Count < 1)
            {
                throw new CtfMetadataException($"Missing alignment value: line={context.Start.Line}");
            }

            if (context.unary_expression().PostfixValues.Count > 1)
            {
                Debug.Assert(false, "figure out what to do with this");
                throw new CtfMetadataException(
                    $"Multiple postfix values is not currently supported: line={context.Start.Line}",
                    new NotSupportedException("This functionality is not currently supported."));
            }

            string alignment = context.unary_expression().PostfixValues[0].ValueAsString;
            this.CurrentScope.PropertyBag.AddValue("align", alignment);

            this.ProcessAnonymousStruct(context);
        }

        // struct { fields... }
        // used in anonymous types: struct { integer { size=16; } short; }
        // used in typealias: typealias struct { integer { size=16; } my_struct;
        // used in typedef: typedef struct { integer { size=16; } } my_16structs[16];
        public override void EnterAnonymousStruct(CtfParser.AnonymousStructContext context)
        {
            this.PushAnonymousStructScope();
        }

        public override void ExitAnonymousStruct(CtfParser.AnonymousStructContext context)
        {
            this.ProcessAnonymousStruct(context);
        }

        public override void ExitTypeSpecifierVariant(CtfParser.TypeSpecifierVariantContext context)
        {
            var variantTypeSpecifier = context.variant_type_specifier().TypeSpecifier;
            context.TypeSpecifier = variantTypeSpecifier;
        }

        public override void EnterNamedVariant(CtfParser.NamedVariantContext context)
        {
            this.PushNamedVariantScope(context.IDENTIFIER(0).GetText());
        }

        public override void ExitNamedVariant(CtfParser.NamedVariantContext context)
        {
            this.PopScope();
        }

        public override void EnterNamedVariantNoTag(CtfParser.NamedVariantNoTagContext context)
        {
            this.PushNamedVariantScope(context.IDENTIFIER().GetText());
        }

        public override void ExitNamedVariantNoTag(CtfParser.NamedVariantNoTagContext context)
        {
            this.PopScope();
        }

        public override void EnterNamedVariantNoBody(CtfParser.NamedVariantNoBodyContext context)
        {
            this.PushNamedVariantScope(context.IDENTIFIER(0).GetText());
        }

        public override void ExitNamedVariantNoBody(CtfParser.NamedVariantNoBodyContext context)
        {
            this.PopScope();
        }

        public override void EnterAnonymousVariant(CtfParser.AnonymousVariantContext context)
        {
            this.PushAnonymousVariantScope();
        }

        public override void ExitAnonymousVariant(CtfParser.AnonymousVariantContext context)
        {
            string enumTag = context.IDENTIFIER().GetText();
            this.ProcessAnonymousVariant(context, enumTag);
        }

        public override void EnterAnonymousVariantNoTag(CtfParser.AnonymousVariantNoTagContext context)
        {
            this.PushAnonymousVariantScope();
        }

        public override void ExitAnonymousVariantNoTag(CtfParser.AnonymousVariantNoTagContext context)
        {
            this.PopScope();
        }

        public override void ExitStructOrVariantDeclaration(CtfParser.StructOrVariantDeclarationContext context)
        {
            Debug.Assert(this.CurrentScope is CtfCompoundTypeScope);
            if (!(this.CurrentScope is CtfCompoundTypeScope compoundScope))
            {
                throw new CtfMetadataException(
                    "An internal error has occurred while parsing the metadata.", 
                    new InvalidOperationException($"Mismatched scope operation: line={context.Start.Line}"));
            }

            // Note: This ignores all of the type prefixes and suffixes for now.
            // For now that means we're just ignoring 'const' declarations, which is fine.
            //
            var declarationType = context.declaration_specifiers().MetadataType;
            if (declarationType == null)
            {
                string typeName = this.parser.TokenStream.GetText(context.declaration_specifiers());

                throw new CtfMetadataException(
                    "The specified type does not exist in the current scope: " +
                    $"type={typeName}, line={context.Start.Line}");
            }

            int fieldNameCount = context.struct_or_variant_declarator_list().struct_or_variant_declarator().Length;
            for (int fieldIndex = 0; fieldIndex < fieldNameCount; fieldIndex++)
            {
                var fieldContext = context.struct_or_variant_declarator_list().struct_or_variant_declarator(fieldIndex);
                string identifier = fieldContext.declarator().Identifier;

                // handle an array/sequence declaration on the identifier
                var arrayExpressionContext = fieldContext.declarator().unary_expression();
                if (arrayExpressionContext != null)
                {
                    if (arrayExpressionContext.PostfixValues.Count > 1)
                    {
                        throw new CtfMetadataException(
                            "Arrays/sequences with multiple postfix values are not currently implemented: " +
                                $"line={context.Start.Line}.",
                            new NotSupportedException("This functionality is not currently supported."));
                    }

                    string arrayIndex;

                    try
                    {
                        arrayIndex = this.ProcessArrayIndex(
                            arrayExpressionContext.PostfixValues[0], 
                            null, 
                            compoundScope.Fields, 
                            compoundScope.Parent);
                    }
                    catch (Exception e)
                    {
                        throw new CtfMetadataException($"{e.Message}: line={context.Start.Line}", e);
                    }

                    if (arrayIndex == null)
                    {
                        var arrayIndexAsString = this.parser.TokenStream.GetText(arrayExpressionContext);
                        throw new CtfMetadataException(
                            $"Unable to parse array index value: {arrayIndexAsString}, " + 
                            $"line={arrayExpressionContext.Start.Line}");
                    }

                    var arrayType = new CtfArrayDescriptor(declarationType, arrayIndex);
                    var arrayField = new CtfFieldDescriptor(arrayType, identifier);
                    compoundScope.Fields.Add(arrayField);
                    continue;
                }

                // handle the bitfield declaration on the declarator
                var bitfieldExpression = fieldContext.unary_expression();
                if (bitfieldExpression != null)
                {
                    throw new CtfMetadataException(
                        $"Bitfields are not currently implemented, line={context.Start.Line}",
                        new NotSupportedException("This functionality is not currently supported."));
                }

                // create a new CtfField to hold this declaration
                var field = new CtfFieldDescriptor(declarationType, identifier);
                compoundScope.Fields.Add(field);
            }
        }

        public override void ExitDeclarationSpecifierTypeSpecifier(CtfParser.DeclarationSpecifierTypeSpecifierContext context)
        {
            CtfParser.Type_specifierContext childContext = context.type_specifier();
            if (childContext.TypeSpecifier != null)
            {
                context.MetadataType = childContext.TypeSpecifier;
            }
            else
            {
                throw new CtfMetadataException(
                    $"type_specifier does not have TypeSpecifier: line={childContext.Start.Line}, " + 
                    $"startToken={childContext.Start.Text}");
            }
        }

        // typealias declaration_specifiers abstract_declarator_list ':=' alias_declaration_specifiers alias_abstract_declarator_list ';'
        public override void ExitTypealias_declaration(CtfParser.Typealias_declarationContext context)
        {
            if (context.declaration_specifiers().MetadataType != null)
            {
                var typeTokens = this.GetTerminalNodeSymbols(context.declared_type());
                if (typeTokens == null || !typeTokens.Any())
                {
                    throw new CtfMetadataException($"Missing typealias type name: line={context.Start.Line}");
                }

                var typeDeclaration = new TypeDeclaration
                {
                    TypeName = typeTokens,
                    DeclarationMethod = DeclarationMethod.TypeAlias,
                    Type = context.declaration_specifiers().MetadataType
                };

                this.CurrentScope.AddType(typeDeclaration);
            }
            else
            {
                throw new CtfMetadataException($"Missing typealias type: line={context.Start.Line}");
            }
        }

        public override void ExitCtfIdentifierAssignment(CtfParser.CtfIdentifierAssignmentContext context)
        {
            if (context.unary_expression().PostfixValues.Count < 1)
            {
                // this should be an error when finished, but until then, I don't want a bunch of errors
                throw new CtfMetadataException(
                    $"Error: missing dynamic scope assignment value: line={context.Start.Line}");
            }

            if (context.unary_expression().PostfixValues.Count > 1)
            {
                Debug.Assert(false, "figure out what to do with this");
                throw new CtfMetadataException(
                    $"Multiple postfix values is not currently supported: line={context.Start.Line}",
                    new NotSupportedException("This functionality is not currently supported."));
            }

            var unaryExpression = context.unary_expression().PostfixValues[0];

            var leftRuleValue = context.IDENTIFIER().Symbol.Text;

            this.CurrentScope.PropertyBag.AddValue(leftRuleValue, unaryExpression.ValueAsString);
        }

        public override void ExitCtfDynamicScopeAssignment(CtfParser.CtfDynamicScopeAssignmentContext context)
        {
            if (context.unary_expression().PostfixValues.Count < 1)
            {
                // this should be an error when finished, but until then, I don't want a bunch of errors
                throw new CtfMetadataException(
                    $"Error: missing dynamic scope assignment value: line={context.Start.Line}");
            }

            if (context.unary_expression().PostfixValues.Count > 1)
            {
                Debug.Assert(false, "figure out what to do with this");
                throw new CtfMetadataException(
                    $"Multiple postfix values is not currently supported: line={context.Start.Line}",
                    new NotSupportedException("This functionality is not currently supported."));
            }

            var rightRuleValue = context.unary_expression().PostfixValues[0].ValueAsString;

            var leftRuleValueTokens = this.GetTerminalNodeSymbols(context.dynamic_reference());
            var leftRuleValue = string.Join(".", leftRuleValueTokens);

            this.CurrentScope.PropertyBag.AddValue(leftRuleValue, rightRuleValue);
        }

        public override void ExitCtfKeywordAssignment(CtfParser.CtfKeywordAssignmentContext context)
        {
            if (context.unary_expression().PostfixValues.Count < 1)
            {
                // this should be an error when finished, but until then, I don't want a bunch of errors
                throw new CtfMetadataException($"Error: missing keyword assignment value: line={context.Start.Line}");
            }

            if (context.unary_expression().PostfixValues.Count > 1)
            {
                Debug.Assert(false, "figure out what to do with this");
                throw new CtfMetadataException(
                    $"Multiple postfix values is not currently supported: line={context.Start.Line}",
                    new NotSupportedException("This functionality is not currently supported."));
            }

            var rightRuleValue = context.unary_expression().PostfixValues[0].ValueAsString;
            var leftRuleValue = context.keywords().GetText();

            this.CurrentScope.PropertyBag.AddValue(leftRuleValue, rightRuleValue);
        }

        public override void ExitCtfTypeAssignment(CtfParser.CtfTypeAssignmentContext context)
        {
            // we expect the unaryExpressionContext to be an identifier (or a dynamic reference?)
            var unaryExpressionContext = context.unary_expression();
            int postfixValuesCount = unaryExpressionContext.PostfixValues.Count;
            if (postfixValuesCount > 1)
            {
                throw new CtfMetadataException(
                    "Unable to parse type assignment identifier:" +
                    $"{this.parser.TokenStream.GetText(unaryExpressionContext)}, line={unaryExpressionContext.Start.Line}");
            }

            if (postfixValuesCount == 0)
            {
                throw new CtfMetadataException(
                    $"No identifier specified in type assignment: line={context.Start.Line}");
            }

            var declarationSpecifiersContext = context.declaration_specifiers();
            if (declarationSpecifiersContext.MetadataType == null)
            {
                throw new CtfMetadataException(
                    $"No type specified in type assignment: line={context.Start.Line}");
            }

            string typeIdentifier = unaryExpressionContext.PostfixValues[0].ValueAsString;
            CtfMetadataTypeDescriptor ctfType = declarationSpecifiersContext.MetadataType;

            var typeDeclaration = new TypeDeclaration
            {
                DeclarationMethod = DeclarationMethod.TypeAssignment,
                TypeName = new[] {typeIdentifier},
                Type = ctfType
            };

            this.CurrentScope.AddType(typeDeclaration);
        }

        public override void EnterTypeSpecifierInteger(CtfParser.TypeSpecifierIntegerContext context)
        {
            this.PushIntegerScope();
        }

        public override void ExitTypeSpecifierInteger(CtfParser.TypeSpecifierIntegerContext context)
        {
            var propertyBag = this.CurrentScope.PropertyBag;

            this.PopIntegerScope(true);

            if (!propertyBag.TryGetString("size", out var sizeValue))
            {
                throw new CtfMetadataException(
                    $"integer must have a size defined. line={context.Start.Line}");
            }

            IntegerLiteral integerLiteral;

            try
            {
                var integerString = new IntegerLiteralString(sizeValue);
                integerLiteral = new IntegerLiteral(integerString);
            }
            catch (Exception)
            {
                throw new CtfMetadataException(
                    $"Unable to parse integer size: {sizeValue}, line={context.Start.Line}");
            }

            if (integerLiteral.Signed)
            {
                if (integerLiteral.ValueAsLong <= 0)
                {
                    throw new CtfMetadataException(
                        "An integer type must have a positive size: " + 
                        $"{sizeValue}, line={context.Start.Line}");
                }
            }
            else
            {
                if (integerLiteral.ValueAsUlong == 0)
                {
                    throw new CtfMetadataException(
                        "An integer type must have a positive size: " +
                        $"{sizeValue}, line={context.Start.Line}");
                }
            }

            var integer = new CtfIntegerDescriptor(propertyBag);
            context.TypeSpecifier = integer;
        }

        //The CTF 1.82 grammar allows for an empty integer declaration, but it has no meaning because the "size" value
        // has no default value for an integer. See specification 1.82 section 4.1.5.
        //
        public override void EnterTypeSpecifierEmptyInteger(CtfParser.TypeSpecifierEmptyIntegerContext context)
        {
            throw new CtfMetadataException(
                $"An integer specifier without a 'size' value is undefined: line={context.Start.Line}.");
        }

        // Float
        public override void EnterTypeSpecifierFloatingPoint(CtfParser.TypeSpecifierFloatingPointContext context)
        {
            this.PushFloatScope();
        }

        // See CTF 1.82 section 4.1.7 FLOATING POINT
        public override void ExitTypeSpecifierFloatingPoint(CtfParser.TypeSpecifierFloatingPointContext context)
        {
            this.PopFloatScope(true);
            throw new CtfMetadataException(
                $"An floating_point specifier without fields is undefined: line={context.Start.Line}.");
        }

        public override void EnterTypeSpecifierFloatingPointWithFields(CtfParser.TypeSpecifierFloatingPointWithFieldsContext context)
        {
            this.PushFloatScope();
        }

        public override void ExitTypeSpecifierFloatingPointWithFields(CtfParser.TypeSpecifierFloatingPointWithFieldsContext context)
        {
            var propertyBag = this.CurrentScope.PropertyBag;
            this.PopFloatScope(true);

            context.TypeSpecifier = new CtfFloatingPointDescriptor(propertyBag);
        }

        // string {}
        public override void ExitTypeSpecifierEmptyString(CtfParser.TypeSpecifierEmptyStringContext context)
        {
            context.TypeSpecifier = new CtfStringDescriptor(new CtfPropertyBag());
        }

        // string { encoding=<value>; }
        public override void EnterTypeSpecifierString(CtfParser.TypeSpecifierStringContext context)
        {
            this.PushStringScope();
        }

        public override void ExitTypeSpecifierString(CtfParser.TypeSpecifierStringContext context)
        {
            var propertyBag = this.CurrentScope.PropertyBag;
            this.PopStringScope(true);

            if (!CtfStringDescriptor.IsValidEncoding(propertyBag))
            {
                throw new CtfMetadataException(
                    $"Invalid encoding value for string type: line={context.Start.Line}");
            }

            context.TypeSpecifier = new CtfStringDescriptor(propertyBag);
        }

        public override void ExitTypeSpecifierIdentifier(CtfParser.TypeSpecifierIdentifierContext context)
        {
            int identifierCount = context.IDENTIFIER().Length;
            Debug.Assert(identifierCount >= 1);
            if (identifierCount < 1)
            {
                throw new CtfMetadataException(
                    $"No identifiers provided for declaration: line={context.Start.Line}");
            }

            var sb = new StringBuilder(context.IDENTIFIER(0).Symbol.Text);
            for (int index = 1; index < identifierCount; index++)
            {
                var terminalNode = context.IDENTIFIER(index);
                sb.Append(" ");
                sb.Append(terminalNode.Symbol.Text);
            }

            string typeName = sb.ToString();

            var typeDeclaration = this.CurrentScope.FindTypeByName(typeName);
            if (typeDeclaration != null)
            {
                context.TypeSpecifier = typeDeclaration.Type;
            }
        }

        public override void ExitDynamic_scope_type_assignment(CtfParser.Dynamic_scope_type_assignmentContext context)
        {
            string dynamicId = context.IDENTIFIER(0) + "." + context.IDENTIFIER(1);
            var declarationSpecifierContext = context.declaration_specifiers();
            if (declarationSpecifierContext.MetadataType == null)
            {
                throw new CtfMetadataException(
                    $"Unable to parse metadataType for {dynamicId}: line={context.Start.Line}");
            }
            else
            {
                var typeDeclaration = new TypeDeclaration
                {
                    DeclarationMethod = DeclarationMethod.TypeAssignment,
                    TypeName = new [] {dynamicId},
                    Type = declarationSpecifierContext.MetadataType
                };

                this.CurrentScope.AddType(typeDeclaration);
            }
        }

        public override void ExitPostfixExpressionUnaryExpression(CtfParser.PostfixExpressionUnaryExpressionContext context)
        {
            context.PostfixValues.AddRange(context.postfix_expression().PostfixValues);
        }

        public override void ExitPositiveUnaryExpression(CtfParser.PositiveUnaryExpressionContext context)
        {
            context.PostfixValues.Add(new PositivePostfixExpressionValue());
            context.PostfixValues.AddRange(context.unary_expression().PostfixValues);
        }

        public override void ExitNegativeUnaryExpression(CtfParser.NegativeUnaryExpressionContext context)
        {
            context.PostfixValues.Add(new NegativePostfixExpressionValue());
            context.PostfixValues.AddRange(context.unary_expression().PostfixValues);
        }

        public override void ExitPostfixExpressionIntegerLiteral(CtfParser.PostfixExpressionIntegerLiteralContext context)
        {
            var integerLiteral = context.integerLiteral().Value;
            if (integerLiteral != null)
            {
                context.PostfixValues.Add(new IntegerLiteralPostfixExpressionValue(integerLiteral));
            }
            // todo:do we need an error here?
            // todo:should we fill in a Default IntegerLiteral?
        }

        public override void ExitPostfixExpressionStringLiteral(CtfParser.PostfixExpressionStringLiteralContext context)
        {
            string value = context.STRING_LITERAL().GetText().Trim('"');
            context.PostfixValues.Add(new StringLiteralPostfixExpressionValue(value));
        }

        public override void ExitPostfixExpressionCharacterLiteral(CtfParser.PostfixExpressionCharacterLiteralContext context)
        {
            context.PostfixValues.Add(new CharacterLiteralPostfixExpressionValue(context.CHARACTER_LITERAL().Symbol.Text[0]));
        }

        public override void ExitPostfixExpressionComplex(CtfParser.PostfixExpressionComplexContext context)
        {
            context.PostfixValues.AddRange(context.postfix_expression_complex().PostfixValues);
        }

        public override void ExitPostfixExpressionIdentifier(CtfParser.PostfixExpressionIdentifierContext context)
        {
            context.PostfixValues.Add(new IdentifierPostfixExpressionValue(context.IDENTIFIER().GetText()));
        }

        public override void ExitPostfixExpressionDynamicReference(CtfParser.PostfixExpressionDynamicReferenceContext context)
        {
            context.PostfixValues.Add(new DynamicReferencePostfixExpressionValue(context.dynamic_reference().DynamicScopePath));
        }

        public override void ExitTraceDynamicReference(CtfParser.TraceDynamicReferenceContext context)
        {
            context.DynamicScopePath = this.BuildDynamicReference("trace", context.IDENTIFIER());
        }

        public override void ExitEventDynamicReference(CtfParser.EventDynamicReferenceContext context)
        {
            context.DynamicScopePath = this.BuildDynamicReference("event", context.IDENTIFIER());
        }

        public override void ExitStreamDynamicReference(CtfParser.StreamDynamicReferenceContext context)
        {
            context.DynamicScopePath = this.BuildDynamicReference("stream.event", context.IDENTIFIER());
        }

        public override void ExitEnvDynamicReference(CtfParser.EnvDynamicReferenceContext context)
        {
            context.DynamicScopePath = this.BuildDynamicReference("env", context.IDENTIFIER());
        }

        public override void ExitClockDynamicReference(CtfParser.ClockDynamicReferenceContext context)
        {
            context.DynamicScopePath = this.BuildDynamicReference("clock", context.IDENTIFIER());
        }

        public override void ExitDecimalLiteral(CtfParser.DecimalLiteralContext context)
        {
            try
            {
                var integerLiteralString = new IntegerLiteralString(context.DECIMAL_LITERAL().GetText());
                context.Value = new IntegerLiteral(integerLiteralString);
            }
            catch (ArgumentException e)
            {
                throw new CtfMetadataException(
                    $"{e.Message}, line={context.Start.Line}", e);
            }
        }

        public override void ExitDeclarator(CtfParser.DeclaratorContext context)
        {
            context.Identifier = context.IDENTIFIER().GetText();

            var unaryExpression = context.unary_expression();
            if (unaryExpression == null)
            {
                return;
            }

            context.UnaryExpressionValues.AddRange(context.unary_expression().PostfixValues);
        }

        public override void ExitTypeSpecifierSimpleString(CtfParser.TypeSpecifierSimpleStringContext context)
        {
            context.TypeSpecifier = new CtfStringDescriptor(new CtfPropertyBag());
        }

        private void PushScope(string name)
        {
            var scope = new CtfScope(name, this.CurrentScope);
            this.CurrentScope.Children.Add(name, scope);
            this.CurrentScope = scope;
        }

        private void PushCompoundScope(string name)
        {
            var scope = new CtfCompoundTypeScope(name, this.CurrentScope);
            this.CurrentScope.Children.Add(name, scope);
            this.CurrentScope = scope;
        }

        private void PopScope(bool removeFromParent = false)
        {
            Debug.Assert(this.CurrentScope != this.GlobalScope);

            CtfScope currentScope = this.CurrentScope;
            this.CurrentScope = this.CurrentScope.Parent;

            if (removeFromParent)
            {
                this.CurrentScope.Children.Remove(currentScope.Name);
            }
        }

        private void PushTraceScope()
        {
            Debug.Assert(this.CurrentScope == this.GlobalScope);
            this.PushScope("trace");
        }

        private void PushClockScope()
        {
            Debug.Assert(this.CurrentScope == this.GlobalScope);
            this.PushScope("clock");
        }

        private void PushEnvScope()
        {
            Debug.Assert(this.CurrentScope == this.GlobalScope);
            this.PushScope("env");
        }

        private void PushNamedStructScope(string name)
        {
            var scopeName = $"{name}";
            this.PushCompoundScope(scopeName);
        }

        private void PushAnonymousStructScope()
        {
            string name = $"[struct]_{this.anonymousStructCount}";
            this.anonymousStructCount++;
            this.PushCompoundScope(name);
        }

        private void PopStructScope(bool removeFromParent = true)
        {
            this.PopScope(removeFromParent);
        }

        private void PushNamedVariantScope(string name)
        {
            var scopeName = $"{name}";
            this.PushCompoundScope(scopeName);
        }

        private void PushAnonymousVariantScope()
        {
            string name = $"[variant]_{this.anonymousVariantCount}";
            this.anonymousVariantCount++;
            this.PushCompoundScope(name);
        }

        private void PopVariantScope(bool removeFromParent = true)
        {
            this.PopScope(removeFromParent);
        }

        private void PushStreamScope()
        {
            // a stream doesn't have a name as part of the declaration - only as a field named "id".
            // so it has to go in as an anonymous stream, to be named later
            string name = $"[stream]_{this.streamCount}";
            this.streamCount++;
            this.PushScope(name);
        }

        private void PopStreamScope()
        {
            this.PopScope();
        }

        private void PushEventScope()
        {
            string name = $"[event]_{this.eventCount}";
            this.eventCount++;
            this.PushScope(name);
        }

        private void PopEventScope()
        {
            this.PopScope();
        }

        // Integer
        private void PushIntegerScope()
        {
            string name = $"[integer]_{this.integerCount}";
            this.integerCount++;
            this.PushScope(name);
        }

        private void PopIntegerScope(bool removeFromParent = false)
        {
            this.PopScope(removeFromParent);
        }

        // Float
        private void PushFloatScope()
        {
            string name = $"[floating_point]_{this.floatCount}";
            this.floatCount++;
            this.PushScope(name);
        }

        private void PopFloatScope(bool removeFromParent = false)
        {
            this.PopScope(removeFromParent);
        }

        // String
        private void PushStringScope()
        {
            string name = $"[string]_{this.stringCount}";
            this.stringCount++;
            this.PushScope(name);
        }

        private void PopStringScope(bool removeFromParent = false)
        {
            this.PopScope(removeFromParent);
        }

        private void PushEnumScope()
        {
            string name = $"[enum]_{this.enumCount}";
            this.enumCount++;
            this.PushScope(name);
        }

        private void PushEnumScope(string name)
        {
            this.enumCount++;
            this.PushScope(name);
        }

        private void PopEnumScope(bool removeFromParent = true)
        {
            this.PopScope(removeFromParent);
        }

        private string[] GetTerminalNodeSymbols(RuleContext context)
        {
            List<string> tokens = new List<string>();
            this.GetTerminalNodeSymbols(context, tokens);
            return tokens.ToArray();
        }

        private void GetTerminalNodeSymbols(RuleContext context, List<string> tokens)
        {
            for (int x = 0; x < context.ChildCount; x++)
            {
                var child = context.GetChild(x);
                if (child is RuleContext ruleContext)
                {
                    this.GetTerminalNodeSymbols(ruleContext, tokens);
                }
                else if (child is TerminalNodeImpl terminalNode)
                {
                    tokens.Add(terminalNode.Symbol.Text);
                }
            }
        }

        private void ProcessAnonymousStruct(CtfParser.Struct_type_specifierContext context)
        {
            Debug.Assert(this.CurrentScope is CtfCompoundTypeScope);
            if (!(this.CurrentScope is CtfCompoundTypeScope compoundScope))
            {
                throw new CtfMetadataException($"Mismatched scope operation: line={context.Start.Line}.");
            }

            var propertyBag = compoundScope.PropertyBag;
            var fields = compoundScope.Fields;

            this.PopStructScope();

            var structType = new CtfStructDescriptor(propertyBag, fields.ToArray());
            context.TypeSpecifier = structType;
        }

        private void ProcessNamedStruct(CtfParser.Struct_type_specifierContext context)
        {
            string typeName = this.CurrentScope.Name;

            this.ProcessAnonymousStruct(context);

            var structTypeDeclaration = new TypeDeclaration
            {
                DeclarationMethod = DeclarationMethod.StructDeclaration,
                Type = context.TypeSpecifier,
                TypeName = new [] { typeName }
            };

            this.CurrentScope.AddType(structTypeDeclaration);
        }

        private void ProcessAnonymousVariant(CtfParser.Variant_type_specifierContext context, string tagName)
        {
            Debug.Assert(this.CurrentScope is CtfCompoundTypeScope);
            if (!(this.CurrentScope is CtfCompoundTypeScope compoundScope))
            {
                throw new CtfMetadataException($"Mismatched scope operation: line={context.Start.Line}.");
            }

            var fields = compoundScope.Fields;

            this.PopVariantScope();

            // todo:validate the variant lies within a scope (specification 1.82 section 4.2.2)
            // todo:validate the tagName

            var variantType = new CtfVariantDescriptor(tagName, fields);
            context.TypeSpecifier = variantType;
        }

        private void ProcessNamedVariant(CtfParser.Variant_type_specifierContext context, string tagName)
        {
            string typeName = this.CurrentScope.Name;

            this.ProcessAnonymousVariant(context, tagName);

            var variantTypeDeclaration = new TypeDeclaration
            {
                DeclarationMethod = DeclarationMethod.VariantDeclaration,
                Type = context.TypeSpecifier,
                TypeName = new [] { typeName }
            };

            this.CurrentScope.AddType(variantTypeDeclaration);
        }

        private string BuildDynamicReference(string referenceBase, IEnumerable<ITerminalNode> identifiers)
        {
            StringBuilder sb = new StringBuilder(referenceBase);

            foreach (var identifier in identifiers)
            {
                sb.Append("." + identifier.Symbol.Text);
            }

            return sb.ToString();
        }

        private void ProcessEnumMappedValues(
            CtfParser.EnumeratorContext context,
            CtfParser.Enumerator_mappingContext mappingRangeContext)
        {
            if (mappingRangeContext.MappingStart == null)
            {
                throw new CtfMetadataException(
                    $"Unexpected enumerator mapped value: line={mappingRangeContext.Start.Line}");
            }

            context.Mapping.StartingValue = mappingRangeContext.MappingStart;
            context.Mapping.EndingValue = mappingRangeContext.MappingStop;
        }

        private string ProcessArrayIndex(
            PostfixExpressionValue value, 
            IntegerLiteral existingInteger, 
            IList<ICtfFieldDescriptor> fieldsInStaticScope,
            CtfScope parentScope)
        {
            switch (value.Type)
            {
                case PostfixExpressionType.DynamicReference:
                    // todo:validate dynamic reference
                    throw new NotSupportedException(
                        "Support has not been added to convert a dynamically scoped value into an integer.");

                case PostfixExpressionType.Identifier:
                    foreach (var field in fieldsInStaticScope)
                    {
                        if (StringComparer.InvariantCulture.Equals(field.Name, value.ValueAsString))
                        {
                            return value.ValueAsString;
                        }
                    }
                    // todo:search through previous scopes
                    throw new NotSupportedException(
                        "Support has not been added to convert an identifier into an integer.");

                case PostfixExpressionType.Negative:
                    return this.ConvertToIntegerLiteral(value, existingInteger).ToString();

                case PostfixExpressionType.Positive:
                    return this.ConvertToIntegerLiteral(value, existingInteger).ToString();

                case PostfixExpressionType.IntegerLiteral:
                    return this.ConvertToIntegerLiteral(value, existingInteger).ToString();

                default:
                    throw new CtfMetadataException(
                        $"Unable to interpret array index value '{value.ValueAsString}'.");
            }
        }

        private IntegerLiteral ConvertToIntegerLiteral(PostfixExpressionValue value, IntegerLiteral existingInteger)
        {
            switch (value.Type)
            {
                case PostfixExpressionType.DynamicReference:
                    throw new NotSupportedException(
                        "Support has not been added to convert a dynamically scoped value into an integer.");

                case PostfixExpressionType.Identifier:
                    throw new NotSupportedException(
                        "Support has not been added to convert an identifier into an integer.");

                case PostfixExpressionType.IndexedExpression:
                    throw new NotSupportedException(
                        "Support has not been added to convert an indexed postfix expression into an integer.");

                case PostfixExpressionType.Negative:
                    if (existingInteger == null)
                    {
                        throw new CtfMetadataException("Unary '-' operator must be applied to an integer.");
                    }

                    if (existingInteger.Signed)
                    {
                        if (existingInteger.ValueAsLong < 0)
                        {
                            return new IntegerLiteral(Math.Abs(existingInteger.ValueAsLong));
                        }
                        return new IntegerLiteral(-existingInteger.ValueAsLong);
                    }
                    else
                    {
                        // this is currently an unsigned value, it must be converted to signed.
                        // this conversion will require an extra bit for the signed representation, so make sure
                        // it's still valid.
                        //
                        if (existingInteger.RequiredBitCount == IntegerLiteral.MaximumSupportedBitCount)
                        {
                            throw new CtfMetadataException($"Negating {existingInteger.ValueAsUlong} causes an overflow.");
                        }

                        return new IntegerLiteral(-(long)existingInteger.ValueAsUlong);
                    }

                case PostfixExpressionType.Positive:
                    if (existingInteger == null)
                    {
                        throw new CtfMetadataException("Unary '+' operator must be applied to an integer.");
                    }

                    if (!existingInteger.Signed)
                    {
                        // the value isn't signed, so it's already positive
                        return existingInteger;
                    }

                    if (existingInteger.ValueAsLong > 0)
                    {
                        // the value is already positive
                        return existingInteger;
                    }

                    // make existingInteger positive
                    return new IntegerLiteral(Math.Abs(existingInteger.ValueAsLong));

                default:
                    var integerAsString = new IntegerLiteralString(value.ValueAsString);
                    return new IntegerLiteral(integerAsString);
            }
        }

        private void ExtractEnumeratorValues(
            CtfParser.Enum_type_specifierContext context,
            IEnumerable<EnumeratorMapping> enumeratorMappings)
        {

            CtfEnumDescriptor enumValue = context.Enum;
            var integerBase = enumValue.BaseType;

            foreach (EnumeratorMapping enumMapping in enumeratorMappings)
            {
                IntegerLiteral begin = enumMapping.StartingValue;

                if (begin == null)
                {
                    // no value was specified, so use a default value
                    try
                    {
                        var defaultValue = new IntegerLiteral(enumValue.GetNextDefaultValue());
                        if (!enumValue.AddRange(
                            enumMapping.EnumIdentifier,
                            new CtfIntegerRange(integerBase, defaultValue, defaultValue)))
                        {
                            throw new CtfMetadataException(
                                $"Duplicate enum value names: line={context.Start.Line}");
                        }
                    }
                    catch (OverflowException)
                    {
                        throw new CtfMetadataException(
                            $"The enumerator values overflow the integer base: line={context.Start.Line}");
                    }

                    continue;
                }

                if (!this.ValidateEnumValueToBaseType(integerBase, begin, enumMapping.EnumIdentifier, context))
                {
                    return;
                }

                IntegerLiteral end;
                if (enumMapping.EndingValue == null)
                {
                    end = begin;
                }
                else
                {
                    end = enumMapping.EndingValue;
                    if (!this.ValidateEnumValueToBaseType(integerBase, end, enumMapping.EnumIdentifier, context))
                    {
                        return;
                    }

                    string beginEndMismatch =
                        "The enumeration mapping is not well formatted. " +
                        $"The end value is larger than the start: line={context.Start.Line}";

                    if (integerBase.Signed)
                    {
                        if (end.ValueAsLong < begin.ValueAsLong)
                        {
                            throw new CtfMetadataException(beginEndMismatch);
                        }
                    }
                    else
                    {
                        if (end.ValueAsUlong < begin.ValueAsUlong)
                        {
                            throw new CtfMetadataException(beginEndMismatch);
                        }
                    }
                }

                if (!enumValue.AddRange(enumMapping.EnumIdentifier, new CtfIntegerRange(integerBase, begin, end)))
                {
                    throw new CtfMetadataException($"Duplicate enum value names: line={context.Start.Line}");
                }
            }
        }

        private bool ValidateEnumValueToBaseType(
            ICtfIntegerDescriptor baseType, 
            IntegerLiteral enumValue,
            string enumIdentifier,
            ParserRuleContext context)
        {
            if (enumValue.Signed != baseType.Signed)
            {
                if (baseType.Signed)
                {
                    if (!enumValue.ConvertToSigned())
                    {
                        throw new CtfMetadataException(
                            "The mapped value does not match the signed property of the integer class: enum " +
                            $"value={enumIdentifier},{enumValue}: line={context.Start.Line}");
                    }
                }
                else
                {
                    if (!enumValue.ConvertToUnsigned())
                    {
                        throw new CtfMetadataException(
                            "The mapped value does not match the signed property of the integer class: enum " +
                            $"value={enumIdentifier},{enumValue}: line={context.Start.Line}");
                    }
                }
            }

            if (enumValue.RequiredBitCount > baseType.Size)
            {
                throw new CtfMetadataException(
                    "The mapped value is too large for the enum base type: enum " +
                    $"value={enumIdentifier},{enumValue} on line={context.Start.Line}");
            }

            return true;
        }

        private int DetermineAlignment(CtfParser.Unary_expressionContext context)
        {
            int alignment;

            var postfixValues = context.PostfixValues;
            if (postfixValues.Count > 1)
            {
                Debug.Assert(false, "figure out what to do with this");
                throw new CtfMetadataException(
                    "Support hasn't been added for converting multiple postfix values into an integer:" +
                        $" line ={context.Start.Line}",
                    new NotSupportedException("This functionality is not currently supported."));
            }

            IntegerLiteral alignValue;

            try
            {
                alignValue = this.ConvertToIntegerLiteral(postfixValues[0], null);
            }
            catch (Exception e)
            {
                throw new CtfMetadataException($"Unable to convert integer literal: line={context.Start.Line}", e);
            }

            if (alignValue.RequiredBitCount > 32)
            {
                throw new CtfMetadataException("Support hasn't been added for alignment > 32 bits (signed).");
            }

            if (alignValue.Signed)
            {
                alignment = (int)alignValue.ValueAsLong;
                if (alignment <= 0)
                {
                    throw new CtfMetadataException(
                        $"Non-positive alignment value isn't supported: line={context.Start.Line}");
                }
            }
            else
            {
                ulong unsignedAlignment = alignValue.ValueAsUlong;
                if (unsignedAlignment > int.MaxValue)
                {
                    throw new CtfMetadataException("Support hasn't been added for alignment > 32 bits (signed).");
                }

                alignment = (int) unsignedAlignment;
            }

            return alignment;
        }
    }
}