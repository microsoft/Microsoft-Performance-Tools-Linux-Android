// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CtfUnitTest
{
    /// <summary>
    /// These tests parse metadata text with syntax errors, and are expected to report errors accordingly.
    /// </summary>
    [TestClass]
    public class SyntaxErrorTests
        : CtfBaseTest
    {
        [TestMethod]
        [TestCategory("UnitTest")]
        public void UnnamedTypeOutsideCompoundType()
        {
            var metadataText =
                "env {\n" +
                "    integer { size=32; } someField;\n" +
                "};";

            var parser = GetParser(metadataText);

            var metadataContext = parser.file();

            Assert.IsTrue(this.TestErrorListener.SyntaxErrors.Count > 1);
        }
    }
}