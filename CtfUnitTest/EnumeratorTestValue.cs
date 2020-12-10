// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CtfUnitTest
{
    public class EnumeratorTestValue
    {
        public string Name { get; set; }

        public long StartValue { get; set; }

        public bool StartValueIsSigned { get; set; } = true;

        public long EndValue { get; set; }

        public bool EndValueIsSigned { get; set; } = true;

        public bool Range { get; set; }

        public bool ValueSpecified { get; set; } = true;

        public bool AddComma { get; set; }

        public override string ToString()
        {
            Assert.IsFalse(Range && !ValueSpecified);

            var sb = new StringBuilder($"{this.Name}");

            if (ValueSpecified)
            {
                sb.Append($"= {this.StartValue}{(StartValueIsSigned ? "" : "u")}");
            }

            if (this.Range)
            {
                sb.Append($"...{this.EndValue}{(EndValueIsSigned ? "" : "u")}");
            }

            if (this.AddComma)
            {
                sb.Append(", ");
            }

            return sb.ToString();
        }
    }
}