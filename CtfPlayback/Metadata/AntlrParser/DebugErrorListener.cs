// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Sharpen;

namespace CtfPlayback.Metadata.AntlrParser
{
    /// <summary>
    /// This class may be assigned to the parser when debugging parsing issues to print out more information to the console.
    /// </summary>
    internal class DebugErrorListener
        : BaseErrorListener
    {
        public override void SyntaxError(
            TextWriter output, 
            IRecognizer recognizer, 
            IToken offendingSymbol, 
            int line, 
            int charPositionInLine,
            string msg, 
            RecognitionException e)
        {
            IList<string> reverseStack = ((Parser) recognizer).GetRuleInvocationStack();
            var stack = reverseStack.Reverse().ToList();
            Console.WriteLine($"rule stack: {stack}");
            Console.WriteLine($"line {line}:{charPositionInLine} at {offendingSymbol}:{msg}");
        }

        public override void ReportAmbiguity(Parser recognizer, DFA dfa, int startIndex, int stopIndex, bool exact, BitSet ambigAlts,
            ATNConfigSet configs)
        {
            base.ReportAmbiguity(recognizer, dfa, startIndex, stopIndex, exact, ambigAlts, configs);
        }

        public override void ReportAttemptingFullContext(Parser recognizer, DFA dfa, int startIndex, int stopIndex, BitSet conflictingAlts,
            SimulatorState conflictState)
        {
            base.ReportAttemptingFullContext(recognizer, dfa, startIndex, stopIndex, conflictingAlts, conflictState);
        }

        public override void ReportContextSensitivity(Parser recognizer, DFA dfa, int startIndex, int stopIndex, int prediction,
            SimulatorState acceptState)
        {
            base.ReportContextSensitivity(recognizer, dfa, startIndex, stopIndex, prediction, acceptState);
        }
    }
}