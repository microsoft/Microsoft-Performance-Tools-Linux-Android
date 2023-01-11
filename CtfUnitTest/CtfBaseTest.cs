// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Antlr4.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CtfUnitTest
{
    public class CtfBaseTest
    {
        protected TestErrorListener TestErrorListener { get; set; }

        protected static Stream StreamFromString(string value)
        {
            var memoryStream = new MemoryStream();

            var writer = new StreamWriter(memoryStream);
            writer.Write(value);
            writer.Flush();

            memoryStream.Position = 0;

            return memoryStream;
        }

        protected CtfParser GetParser(Stream inputStream)
        {
            var input = new AntlrInputStream(inputStream);
            var lexer = new CtfLexer(input);
            var tokens = new CommonTokenStream(lexer);
            var parser = new CtfParser(tokens);

            TestErrorListener = new TestErrorListener();
            parser.RemoveErrorListeners();
            parser.AddErrorListener(TestErrorListener);

            return parser;
        }

        protected CtfParser GetParser(string value)
        {
            using (var inputStream = StreamFromString(value))
            {
                return GetParser(inputStream);
            }
        }

        protected void ValidateEmptyErrorListener()
        {
            Assert.AreEqual(TestErrorListener.AmbiguityErrors.Count, 0);
            Assert.AreEqual(TestErrorListener.SyntaxErrors.Count, 0);
            Assert.AreEqual(TestErrorListener.AttemptingFullContextMessages.Count, 0);
            Assert.AreEqual(TestErrorListener.ContextSensitivityMessages.Count, 0);
        }
    }
}