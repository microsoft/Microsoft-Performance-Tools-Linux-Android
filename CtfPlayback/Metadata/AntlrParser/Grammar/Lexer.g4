lexer grammar Lexer;

ALIGN                   :   'align' ;
CALLSITE                :   'callsite' ;
CLOCK                   :   'clock' ;
ENUM                    :   'enum' ;
ENV                     :   'env' ;
EVENT                   :   'event' ;
FLOATING_POINT          :   'floating_point' ;
INTEGER                 :   'integer' ;
STREAM                  :   'stream' ;
STRING                  :   'string' ;
STRUCT                  :   'struct' ;
TRACE                   :   'trace' ;
TYPEALIAS               :   'typealias' ;
TYPEDEF                 :   'typedef' ;
VARIANT                 :   'variant' ;
DECIMAL_LITERAL         :   ('0' | [1-9][0-9]*) IntegerSuffix? ;
OCTAL_LITERAL           :   '0' ('0'..'7')+ IntegerSuffix? ;
HEXADECIMAL_LITERAL     :   '0' ('x'|'X') HexadecimalDigit+ IntegerSuffix? ;
IDENTIFIER              :   IdNonDigit (IdNonDigit|'0'..'9')* ;

/** COMMENT, WS, LINE_COMMENT, STRING_LITERAL, CHARACTER_LITERAL and their fragments were 
    taken from the java antlr grammar, rather than trying to piece together one from the flex ctf.
 */
COMMENT
    : '/*' .*? '*/' -> channel(HIDDEN)
    ;

WS  :   [ \r\t\u000C\n]+ -> channel(HIDDEN)
    ;

LINE_COMMENT
    : '//' ~[\r\n]* '\r'? '\n' -> channel(HIDDEN)
    ;

STRING_LITERAL
    :  '"' ( EscapeSequence | ~('\\'|'"') )* '"'
    ;

CHARACTER_LITERAL
    :   '\'' ( EscapeSequence | ~('\''|'\\') ) '\''
    ;

GARBAGE : . -> skip ;

fragment
EscapeSequence
    :   '\\' ('b'|'t'|'n'|'f'|'r'|'"'|'\''|'\\')
    |   UnicodeEscape
    |   OctalEscape
    ;

fragment
OctalEscape
    :   '\\' ('0'..'3') ('0'..'7') ('0'..'7')
    |   '\\' ('0'..'7') ('0'..'7')
    |   '\\' ('0'..'7')
    ;

fragment
UnicodeEscape
    :   '\\' 'u' HexadecimalDigit HexadecimalDigit HexadecimalDigit HexadecimalDigit
    ;

fragment
IntegerSuffix          : ('U'|'UL'|'ULL'|'LU'|'LLU'|'Ul'|'Ull'|'lU'|'llU'|'u'|'uL'|'uLL'|'Lu'|'LLu'|'ul'|'ull'|'lu'|'llu') ;

fragment
HexadecimalDigit       : ('0'..'9'|'a'..'f'|'A'..'F') ;

fragment
NonDigit               : ('a'..'z'|'A'..'Z'|'_') ;

fragment
HexQuad                : HexadecimalDigit HexadecimalDigit HexadecimalDigit HexadecimalDigit ;

fragment
UcharLowercase         : '\\' 'u' HexQuad ;

fragment
UcharUppercase         : '\\' 'U' HexQuad HexQuad ;

fragment
IdNonDigit             : (NonDigit|UcharLowercase|UcharUppercase) ;
