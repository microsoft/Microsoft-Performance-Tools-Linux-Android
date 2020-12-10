// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Sharpen;

namespace CtfUnitTest
{
    public class TestErrorListener
        : BaseErrorListener
    {
        public List<string> SyntaxErrors = new List<string>();
        public List<string> SyntaxErrorStacks = new List<string>();
        public List<string> AmbiguityErrors = new List<string>();
        public List<string> AttemptingFullContextMessages = new List<string>();
        public List<string> ContextSensitivityMessages = new List<string>();

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

            this.SyntaxErrors.Add($"line {line}:{charPositionInLine} at {offendingSymbol}:{msg}");
            this.SyntaxErrorStacks.Add($"{stack}");
        }

        public override void ReportAmbiguity(Parser recognizer, DFA dfa, int startIndex, int stopIndex, bool exact, BitSet ambigAlts, ATNConfigSet configs)
        {
            string format = "reportAmbiguity d={0}: ambigAlts={1}, input='{2}'";
            string decision = GetDecisionDescription(recognizer, dfa);
            BitSet conflictingAlts = GetConflictingAlts(ambigAlts, configs);
            string text = ((ITokenStream)recognizer.InputStream).GetText(Interval.Of(startIndex, stopIndex));
            string message = string.Format(format, decision, conflictingAlts, text);
            this.AmbiguityErrors.Add(message);
        }

        public override void ReportAttemptingFullContext(
            Parser recognizer, 
            DFA dfa, 
            int startIndex, 
            int stopIndex, 
            BitSet conflictingAlts,
            SimulatorState conflictState)
        {
            //string format = "reportAttemptingFullContext d={0}, input='{1}'";
            //string decision = GetDecisionDescription(recognizer, dfa);
            //string text = ((ITokenStream)recognizer.InputStream).GetText(Interval.Of(startIndex, stopIndex));
            //string message = string.Format(format, decision, text);
            //AttemptingFullContextMessages.Add(message);
        }

        public override void ReportContextSensitivity(
            Parser recognizer, 
            DFA dfa, 
            int startIndex, 
            int stopIndex, 
            int prediction,
            SimulatorState acceptState)
        {
            //string format = "reportContextSensitivity d={0}, input='{1}'";
            //string decision = GetDecisionDescription(recognizer, dfa);
            //string text = ((ITokenStream)recognizer.InputStream).GetText(Interval.Of(startIndex, stopIndex));
            //string message = string.Format(format, decision, text);
            //ContextSensitivityMessages.Add(message);
        }

        protected internal virtual string GetDecisionDescription(Parser recognizer, DFA dfa)
        {
            int decision = dfa.decision;
            int ruleIndex = dfa.atnStartState.ruleIndex;
            string[] ruleNames = recognizer.RuleNames;
            if (ruleIndex < 0 || ruleIndex >= ruleNames.Length)
            {
                return decision.ToString();
            }
            string ruleName = ruleNames[ruleIndex];
            if (string.IsNullOrEmpty(ruleName))
            {
                return decision.ToString();
            }
            return string.Format("{0} ({1})", decision, ruleName);
        }

        /// <summary>
        /// Computes the set of conflicting or ambiguous alternatives from a
        /// configuration set, if that information was not already provided by the
        /// parser.
        /// </summary>
        /// <remarks>
        /// Computes the set of conflicting or ambiguous alternatives from a
        /// configuration set, if that information was not already provided by the
        /// parser.
        /// </remarks>
        /// <param name="reportedAlts">
        /// The set of conflicting or ambiguous alternatives, as
        /// reported by the parser.
        /// </param>
        /// <param name="configSet">The conflicting or ambiguous configuration set.</param>
        /// <returns>
        /// Returns
        /// <paramref name="reportedAlts"/>
        /// if it is not
        /// <see langword="null"/>
        /// , otherwise
        /// returns the set of alternatives represented in
        /// <paramref name="configSet"/>
        /// .
        /// </returns>
        [return: NotNull]
        protected internal virtual BitSet GetConflictingAlts(BitSet reportedAlts, ATNConfigSet configSet)
        {
            if (reportedAlts != null)
            {
                return reportedAlts;
            }
            BitSet result = new BitSet();
            foreach (ATNConfig config in configSet.configs)
            {
                result.Set(config.alt);
            }
            return result;
        }

    }
}