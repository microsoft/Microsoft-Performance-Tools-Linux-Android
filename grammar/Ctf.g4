// Install java runtime such as from https://www.openlogic.com/openjdk-downloads?field_java_parent_version_target_id=406&field_operating_system_target_id=436&field_architecture_target_id=391&field_java_package_target_id=401
// Download Antlr4 jar from http://antlr.org
// run: java -jar antlr-4.11.1-complete.jar -Dlanguage=CSharp Ctf.g4

// this is almost a direct port from the BabelTrace reference implementation which
// currently uses flex/bison.

// the Antlr implementation doesn't use the ID_TYPE token, as that would require
// interaction between the lexer/parser, which isn't possible. rather, IDENTIFIER
// is used in place of ID_TYPE, and the tree walker may check for errors.

// finally, if you're reading this and know anything about Antlr or grammars in general,
// and something seems wrong... it probably is. I'm throwing this together just to try
// and get it working, without any deep understanding of what I'm doing.

// NOTE
// For CSharp, this will generate an interface ICtfListener, but in a file named CtfListener.cs.
// Be sure to rename this to ICtfListener.cs before checking in. 

grammar Ctf;
import Lexer;

file:
        declaration+
    ;

keywords:
    //     VOID
    // |   CHAR
    // |   SHORT
    // |   INT
    // |   LONG
    // |   FLOAT
    // |   DOUBLE
    // |   SIGNED
    // |   UNSIGNED
    // |   BOOL
    // |   COMPLEX
    // |   IMAGINARY
        FLOATING_POINT
    |   INTEGER
    |   STRING
    |   ENUM
    |   VARIANT
    |   STRUCT
//    |   CONST
    |   TYPEDEF
    |   EVENT
    |   STREAM
    |   ENV
    |   TRACE
    |   CLOCK
    |   CALLSITE
    |   ALIGN
    ;

declaration:   
        declaration_specifiers ';'
    |   event_declaration
    |   stream_declaration
    |   env_declaration
    |   trace_declaration
    |   clock_declaration
    |   callsite_declaration
    |   typedef_declaration ';'
    |   typealias_declaration ';'
    ;

// typealias can be assigned to something like this:
// typealias : integer { signed=false; size=32; } const unsigned int32;
// the text from this rule should include all of "const unsigned int32", which is
// exact text that will need to be used in other locations
declared_type:
        alias_declaration_specifiers
    |   alias_abstract_declarator_list
    ;

typealias_declaration:
        TYPEALIAS 
        declaration_specifiers
        abstract_declarator_list 
        ':=' 
        declared_type
    ;

typedef_declaration:
        // declaration_specifiers 
        // TYPEDEF 
        // declaration_specifiers 
        // type_declarator_list ';'                    #TypeDefDeclarationWithSpecifiersBeforeAndAfter
        TYPEDEF 
        declaration_specifiers 
        type_declarator_list                        #TypeDefDeclaration
    // |   declaration_specifiers 
    //     TYPEDEF 
    //     type_declarator_list ';'                    #TypeDefDeclarationWithSpecifiersBefore
    ;

declaration_specifiers:
//        CONST                                       #DeclarationSpecifierPrefixConst
        type_specifier                              #DeclarationSpecifierTypeSpecifier
//    |   declaration_specifiers CONST                #DeclarationSpecifierSuffixConst
//    |   declaration_specifiers type_specifier       #DeclarationSpecifierCompound
    ;

type_specifier:
    //     VOID                                        #IgnoredTypeSpecifier
    // |   CHAR                                        #IgnoredTypeSpecifier
    // |   SHORT                                       #IgnoredTypeSpecifier
    // |   INT                                         #IgnoredTypeSpecifier
    // |   LONG                                        #IgnoredTypeSpecifier
    // |   FLOAT                                       #IgnoredTypeSpecifier
    // |   DOUBLE                                      #IgnoredTypeSpecifier
    // |   SIGNED                                      #IgnoredTypeSpecifier
    // |   UNSIGNED                                    #IgnoredTypeSpecifier
    // |   BOOL                                        #IgnoredTypeSpecifier
    // |   COMPLEX                                     #IgnoredTypeSpecifier
    // |   IMAGINARY                                   #IgnoredTypeSpecifier
        FLOATING_POINT '{' '}'                      #TypeSpecifierFloatingPoint
    |   FLOATING_POINT 
        '{' ctf_expression_list '}'                 #TypeSpecifierFloatingPointWithFields
    |   INTEGER '{' '}'                             #TypeSpecifierEmptyInteger
    |   INTEGER 
        '{' ctf_expression_list '}'                 #TypeSpecifierInteger
    |   STRING                                      #TypeSpecifierSimpleString
    |   STRING '{' '}'                              #TypeSpecifierEmptyString
    |   STRING 
        '{' ctf_expression_list '}'                 #TypeSpecifierString
    |   ENUM enum_type_specifier                    #TypeSpecifierEnum
    |   VARIANT variant_type_specifier              #TypeSpecifierVariant
    |   STRUCT struct_type_specifier                #TypeSpecifierStruct
// Note that we allow multiple IDENTIFIERs here. That is to handle type aliases
// like: typealias integer { signed=false; size=32 } const unsigned int;
// where the actual type then is 'const unsigned int'
    |   (IDENTIFIER)+                               #TypeSpecifierIdentifier
    ;

event_declaration:
        event_declaration_begin event_declaration_end
    |   event_declaration_begin ctf_expression_list event_declaration_end
    ;

stream_declaration:
        stream_declaration_begin stream_declaration_end
    |   stream_declaration_begin stream_assignment_expression_list stream_declaration_end
    ;
                        
env_declaration:
        env_declaration_begin env_declaration_end
    |   env_declaration_begin ctf_expression_list env_declaration_end
    ;

trace_declaration:
        trace_declaration_begin trace_declaration_end
    |   trace_declaration_begin trace_assignment_expression_list trace_declaration_end
    ;

clock_declaration:
        CLOCK clock_declaration_begin clock_declaration_end
    |   CLOCK clock_declaration_begin ctf_expression_list clock_declaration_end
    ;

callsite_declaration:
        CALLSITE callsite_declaration_begin callsite_declaration_end
    |   CALLSITE callsite_declaration_begin ctf_expression_list callsite_declaration_end
    ;

type_declarator_list:
        type_declarator ( ',' type_declarator )*
    ;

abstract_declarator_list:
        abstract_declarator ( ',' abstract_declarator )*
    ;

alias_declaration_specifiers:
//        CONST
        type_specifier
//    |   alias_declaration_specifiers CONST
//    |   alias_declaration_specifiers type_specifier
//    |   alias_declaration_specifiers IDENTIFIER
    ;

alias_abstract_declarator_list:
        alias_abstract_declarator (',' alias_abstract_declarator)*
    ;

dynamic_scope_type_assignment:
        IDENTIFIER '.' IDENTIFIER ':=' declaration_specifiers ';'
    ;

trace_assignment_expression_list:
        // allow PACKET.HEADER in the trace assignment expressions. this is the only time the 'trace'
        // keyword isn't needed first, because it's already within the context of the trace
        // there is no PACKET keyword, so we have to use IDENTIFIER here, then just check post parsing
        (ctf_expression | dynamic_scope_type_assignment)+
    ;

stream_assignment_expression_list:
        // allow PACKET.CONTEXT in the stream assignment expressions. this is the only time the 'stream'
        // keyword isn't needed first, because it's already within the context of the stream
        // there is no PACKET keyword, so we have to use IDENTIFIER here, then just check post parsing
        (ctf_expression | dynamic_scope_type_assignment)+
    ;

ctf_expression:
        (ctf_assignment_expression | typedef_declaration | typealias_declaration) ';'
    ;

ctf_expression_list:
        ctf_expression+
    ;

// Enums that do not specificy a base class use 'int' by default, which must
// be specified before the enum. See specification 1.8.2, section 4.1.8.
//
enum_type_specifier:
        '{' enumerator_list  '}'                        #AnonymousEnumTypeDefaultBase
    |   '{' enumerator_list ',' '}'                     #AnonymousEnumTypeDefaultBase
    |   ':' enum_integer_declaration_specifiers 
        '{' enumerator_list '}'                         #AnonymousEnumTypeSpecifiedBase
    |   ':' enum_integer_declaration_specifiers 
        '{' enumerator_list ',' '}'                     #AnonymousEnumTypeSpecifiedBase
    |   IDENTIFIER '{' enumerator_list '}'              #NamedEnumTypeDefaultBase
    |   IDENTIFIER '{' enumerator_list ',' '}'          #NamedEnumTypeDefaultBase
    |   IDENTIFIER ':'                                  
        enum_integer_declaration_specifiers 
        '{' enumerator_list '}'                         #NamedEnumTypeSpecifiedBase
    |   IDENTIFIER ':' 
        enum_integer_declaration_specifiers 
        '{' enumerator_list ',' '}'                     #NamedEnumTypeSpecifiedBase
// Not sure how this is used, commenting out for now.
//    |   IDENTIFIER
    ;

variant_type_specifier:
        variant_declaration_begin 
        struct_or_variant_declaration_list 
        variant_declaration_end                         #AnonymousVariantNoTag
    |   '<' IDENTIFIER '>' 
        variant_declaration_begin 
        struct_or_variant_declaration_list 
        variant_declaration_end                         #AnonymousVariant
    |   IDENTIFIER 
        variant_declaration_begin 
        struct_or_variant_declaration_list 
        variant_declaration_end                         #NamedVariantNoTag
    |   IDENTIFIER 
        '<' IDENTIFIER '>' 
        variant_declaration_begin 
        struct_or_variant_declaration_list 
        variant_declaration_end                         #NamedVariant
    |   IDENTIFIER 
        '<' IDENTIFIER '>'                              #NamedVariantNoBody
    ;

struct_type_specifier:
        struct_declaration_begin 
        struct_or_variant_declaration_list 
        struct_declaration_end                  #AnonymousStruct
    |   IDENTIFIER 
        struct_declaration_begin 
        struct_or_variant_declaration_list 
        struct_declaration_end                  #NamedStruct
    |   IDENTIFIER                              #StructAsType
    |   struct_declaration_begin 
        struct_or_variant_declaration_list 
        struct_declaration_end 
        ALIGN 
        '(' unary_expression ')'                #AnonymousAlignedStruct
    |   IDENTIFIER 
        struct_declaration_begin 
        struct_or_variant_declaration_list 
        struct_declaration_end 
        ALIGN 
        '(' unary_expression ')'                #NamedAlignedStruct
    ;

event_declaration_begin:
        EVENT '{'
    ;

event_declaration_end:
        '}' ';'
    ;

stream_declaration_begin:
        STREAM '{'
    ;

stream_declaration_end:
        '}' ';'
    ;

env_declaration_begin:
        ENV '{'
    ;

env_declaration_end:
        '}' ';'
    ;

trace_declaration_begin:
        TRACE '{'
    ;

trace_declaration_end:
        '}' ';'
    ;

clock_declaration_begin:
        '{'
    ;

clock_declaration_end:
        '}' ';'
    ;

callsite_declaration_begin:
        '{'
    ;

callsite_declaration_end:
        '}' ';'
    ;

type_declarator:
//        pointer? direct_type_declarator
        (IDENTIFIER | ('(' type_declarator ')'))? ('[' unary_expression ']')*
    ;

// direct_type_declarator:
//         (IDENTIFIER | ('(' type_declarator ')'))? ('[' unary_expression ']')*
//     ;

abstract_declarator:
//        pointer? direct_abstract_declarator
        ( IDENTIFIER | '(' abstract_declarator ')')? ('[' unary_expression? ']')*
    ;

// direct_abstract_declarator:
//         ( IDENTIFIER | '(' abstract_declarator ')')? ('[' unary_expression? ']')*
//     ;

alias_abstract_declarator:
//       pointer? ('(' alias_abstract_declarator ')')? ('[' unary_expression? ']')*
       ('(' alias_abstract_declarator ')')? ('[' unary_expression? ']')*
    ;

// CTF assignment expressions are used within CTF objects: 
//  these include: trace, stream, event, integer, clock, env, callsite
// This is similar to a struct, but the "fields" may be keywords.
//
ctf_assignment_expression:
        IDENTIFIER '=' unary_expression                     #CtfIdentifierAssignment
    |   dynamic_reference '=' unary_expression              #CtfDynamicScopeAssignment
    |   keywords '=' unary_expression                       #CtfKeywordAssignment
    |   unary_expression ':=' declaration_specifiers        #CtfTypeAssignment
    ;

enumerator_list:
        enumerator (',' enumerator)*
    ;

enum_integer_declaration_specifiers:
//        CONST                                               #EnumIntegerDeclarationConst
        enum_integer_type_specifier                         #EnumIntegerDeclarationTypeSpecifier
//    |   enum_integer_declaration_specifiers CONST           #EnumIntegerDeclarationsAndConst
    |   enum_integer_declaration_specifiers
        enum_integer_type_specifier                         #EnumIntegerDeclarationsAndTypeSpecifier
    ;

variant_declaration_begin:
        '{'
    ;

variant_declaration_end:
        '}'
    ;

struct_or_variant_declaration_list:
        struct_or_variant_declaration*
    ;

struct_declaration_begin:
        '{'
    ;

struct_declaration_end:
        '}'
    ;

unary_expression:
        postfix_expression      #PostfixExpressionUnaryExpression
    |   '+' unary_expression    #PositiveUnaryExpression
    |   '-' unary_expression    #NegativeUnaryExpression
    ;

//pointer:
//        '*'
//    |   '*' pointer
//    |   '*' type_qualifier_list pointer
//    ;

enumerator:
        IDENTIFIER                              #EnumIdentifierValue
    |   keywords                                #EnumKeywordValue
    |   STRING_LITERAL                          #EnumStringLiteralValue
    |   IDENTIFIER '=' 
        enumerator_mapping                      #EnumIdentifierAssignedValue
    |   keywords '=' 
        enumerator_mapping                      #EnumKeywordAssignedValue
    |   STRING_LITERAL '=' 
        enumerator_mapping                      #EnumStringLiteralAssignedValue
    ;

enum_integer_type_specifier:
// I don't believe enumerations may use anything other than integer, i've commented
// out these other options for now to simplify.
//        CHAR
//    |   SHORT
//    |   INT
//    |   LONG
//    |   SIGNED
//    |   UNSIGNED
//    |   BOOL
        IDENTIFIER                              #EnumIntegerSpecifierFromType
    |   INTEGER '{' '}'                         #EnumIntegerSpecifierWithDefaults
    |   INTEGER 
        '{' ctf_expression_list '}'  #EnumIntegerSpecifier
    ;

struct_or_variant_declaration:
        declaration_specifiers 
        struct_or_variant_declarator_list ';'   #StructOrVariantDeclaration
    // |   declaration_specifiers 
    //     TYPEDEF 
    //     declaration_specifiers 
	//     type_declarator_list ';'                #StructOrVariantTypedef1
    |   typedef_declaration ';'                 #StructOrVariantTypedef
    // |   declaration_specifiers 
    //     TYPEDEF 
    //     type_declarator_list ';'                #StructOrVariantTypedef3
    |   typealias_declaration ';'               #StructOrVariantTypealias
    ;

integerLiteral:
        DECIMAL_LITERAL                                 #DecimalLiteral
    |   HEXADECIMAL_LITERAL                             #HexadecimalLiteral
    |   OCTAL_LITERAL                                   #OctalLiteral
    ;

postfix_expression:
        integerLiteral                                  #PostfixExpressionIntegerLiteral
    |   STRING_LITERAL                                  #PostfixExpressionStringLiteral
    |   CHARACTER_LITERAL                               #PostfixExpressionCharacterLiteral
    |   postfix_expression_complex                      #PostfixExpressionComplex
    ;

postfix_expression_complex:
        IDENTIFIER                                              #PostfixExpressionIdentifier
    |   dynamic_reference                                       #PostfixExpressionDynamicReference
    |   '(' unary_expression ')'                                #PostfixExpressionParentheseUnaryExpression
    |   postfix_expression_complex '[' unary_expression ']'     #PostfixExpressionPostfixWithBrackets
    ;

// According to Spec 1.82 section 7.3.2, this is a superset of dynamic scope prefixes:
dynamic_reference:
        EVENT '.' IDENTIFIER ('.' IDENTIFIER)*                  #EventDynamicReference
    |   TRACE '.' IDENTIFIER ('.' IDENTIFIER)*                  #TraceDynamicReference
    |   STREAM '.' EVENT '.' IDENTIFIER ('.' IDENTIFIER)*       #StreamDynamicReference
    |   ENV '.'  IDENTIFIER ('.' IDENTIFIER)*                   #EnvDynamicReference
    |   CLOCK '.'  IDENTIFIER ('.' IDENTIFIER)*                 #ClockDynamicReference
    ;

// type_qualifier_list:
//         CONST
//     |   type_qualifier_list CONST
//     ;

// this is only used for an enumerator, which must match an integer type
enumerator_mapping:
        unary_expression '...' unary_expression                 #EnumeratorMappingRange
    |   unary_expression                                        #EnumeratorMappingSimple
    ;

struct_or_variant_declarator_list:
        struct_or_variant_declarator (',' struct_or_variant_declarator)*
    ;

struct_or_variant_declarator:
        declarator (':' unary_expression)?
// I'm not sure where this would be used. Commenting out until we need it.
//    |   ':' unary_expression
    ;

// I've simplified declarator until for now.
declarator:
        IDENTIFIER ('[' unary_expression ']')?
    ;

// I'm commenting out pointer support until we know we need it.
// I'm not sure what '(' declarator ')' is used for under direct_declarator. Commenting out until we need it.
// declarator:
//         pointer? direct_declarator
//     ;

// direct_declarator:
//         IDENTIFIER                                          #DirectDeclaratorIdentifier
//     |   '(' declarator ')'                                  #ParenthesesAroundDeclarator
//     |   direct_declarator 
//         '[' unary_expression ']'                            #DirectDeclaratorWithIndexer
//     ;
