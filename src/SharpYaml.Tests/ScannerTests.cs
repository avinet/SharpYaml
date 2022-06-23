﻿// Copyright (c) 2015 SharpYaml - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// -------------------------------------------------------------------------------
// SharpYaml is a fork of YamlDotNet https://github.com/aaubry/YamlDotNet
// published with the following license:
// -------------------------------------------------------------------------------
// 
// Copyright (c) 2008, 2009, 2010, 2011, 2012 Antoine Aubry
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using NUnit.Framework;
using SharpYaml.Tokens;

namespace SharpYaml.Tests
{
    public class ScannerTests : ScannerTestHelper
    {
        [Test]
        public void VerifyTokensOnExample1()
        {
            AssertSequenceOfTokensFrom(ScannerFor("test1.yaml"),
                StreamStart,
                VersionDirective(1, 1),
                TagDirective("!", "!foo"),
                TagDirective("!yaml!", "tag:yaml.org,2002:"),
                DocumentStart,
                StreamEnd);
        }

        [Test]
        public void VerifyTokensOnExample2()
        {
            AssertSequenceOfTokensFrom(ScannerFor("test2.yaml"),
                StreamStart,
                SingleQuotedScalar("a scalar"),
                StreamEnd);
        }

        [Test]
        public void VerifyTokensOnExample3()
        {
            var scanner = ScannerFor("test3.yaml");
            AssertSequenceOfTokensFrom(scanner,
                StreamStart,
                DocumentStart,
                SingleQuotedScalar("a scalar"),
                DocumentEnd,
                StreamEnd);
        }

        [Test]
        public void VerifyTokensOnExample4()
        {
            AssertSequenceOfTokensFrom(ScannerFor("test4.yaml"),
                StreamStart,
                SingleQuotedScalar("a scalar"),
                DocumentStart,
                SingleQuotedScalar("another scalar"),
                DocumentStart,
                SingleQuotedScalar("yet another scalar"),
                StreamEnd);
        }

        [Test]
        public void VerifyTokensOnExample5()
        {
            AssertSequenceOfTokensFrom(ScannerFor("test5.yaml"),
                StreamStart,
                Anchor("A"),
                FlowSequenceStart,
                AnchorAlias("A"),
                FlowSequenceEnd,
                StreamEnd);
        }

        [Test]
        public void VerifyTokensOnExample6()
        {
            AssertSequenceOfTokensFrom(ScannerFor("test6.yaml"),
                StreamStart,
                Tag("!!", "float"),
                DoubleQuotedScalar("3.14"),
                StreamEnd);
        }

        [Test]
        public void VerifyTokensOnExample7()
        {
            AssertSequenceOfTokensFrom(ScannerFor("test7.yaml"),
                StreamStart,
                DocumentStart,
                DocumentStart,
                PlainScalar("a plain scalar"),
                DocumentStart,
                SingleQuotedScalar("a single-quoted scalar"),
                DocumentStart,
                DoubleQuotedScalar("a double-quoted scalar"),
                DocumentStart,
                LiteralScalar("a literal scalar"),
                DocumentStart,
                FoldedScalar("a folded scalar"),
                StreamEnd);
        }

        [Test]
        public void VerifyTokensOnExample8()
        {
            AssertSequenceOfTokensFrom(ScannerFor("test8.yaml"),
                StreamStart,
                FlowSequenceStart,
                PlainScalar("item 1"),
                FlowEntry,
                PlainScalar("item 2"),
                FlowEntry,
                PlainScalar("item 3"),
                FlowSequenceEnd,
                StreamEnd);
        }

        [Test]
        public void VerifyTokensOnExample9()
        {
            AssertSequenceOfTokensFrom(ScannerFor("test9.yaml"),
                StreamStart,
                FlowMappingStart,
                Key,
                PlainScalar("a simple key"),
                Value,
                PlainScalar("a value"),
                FlowEntry,
                Key,
                PlainScalar("a complex key"),
                Value,
                PlainScalar("another value"),
                FlowEntry,
                FlowMappingEnd,
                StreamEnd);
        }

        [Test]
        public void VerifyTokensOnExample10()
        {
            AssertSequenceOfTokensFrom(ScannerFor("test10.yaml"),
                StreamStart,
                BlockSequenceStart,
                BlockEntry,
                PlainScalar("item 1"),
                BlockEntry,
                PlainScalar("item 2"),
                BlockEntry,
                BlockSequenceStart,
                BlockEntry,
                PlainScalar("item 3.1"),
                BlockEntry,
                PlainScalar("item 3.2"),
                BlockEnd,
                BlockEntry,
                BlockMappingStart,
                Key,
                PlainScalar("key 1"),
                Value,
                PlainScalar("value 1"),
                Key,
                PlainScalar("key 2"),
                Value,
                PlainScalar("value 2"),
                BlockEnd,
                BlockEnd,
                StreamEnd);
        }

        [Test]
        public void VerifyTokensOnExample11()
        {
            AssertSequenceOfTokensFrom(ScannerFor("test11.yaml"),
                StreamStart,
                BlockMappingStart,
                Key,
                PlainScalar("a simple key"),
                Value,
                PlainScalar("a value"),
                Key,
                PlainScalar("a complex key"),
                Value,
                PlainScalar("another value"),
                Key,
                PlainScalar("a mapping"),
                Value,
                BlockMappingStart,
                Key,
                PlainScalar("key 1"),
                Value,
                PlainScalar("value 1"),
                Key,
                PlainScalar("key 2"),
                Value,
                PlainScalar("value 2"),
                BlockEnd,
                Key,
                PlainScalar("a sequence"),
                Value,
                BlockSequenceStart,
                BlockEntry,
                PlainScalar("item 1"),
                BlockEntry,
                PlainScalar("item 2"),
                BlockEnd,
                BlockEnd,
                StreamEnd);
        }

        [Test]
        public void VerifyTokensOnExample12()
        {
            AssertSequenceOfTokensFrom(ScannerFor("test12.yaml"),
                StreamStart,
                BlockSequenceStart,
                BlockEntry,
                BlockSequenceStart,
                BlockEntry,
                PlainScalar("item 1"),
                BlockEntry,
                PlainScalar("item 2"),
                BlockEnd,
                BlockEntry,
                BlockMappingStart,
                Key,
                PlainScalar("key 1"),
                Value,
                PlainScalar("value 1"),
                Key,
                PlainScalar("key 2"),
                Value,
                PlainScalar("value 2"),
                BlockEnd,
                BlockEntry,
                BlockMappingStart,
                Key,
                PlainScalar("complex key"),
                Value,
                PlainScalar("complex value"),
                BlockEnd,
                BlockEnd,
                StreamEnd);
        }

        [Test]
        public void VerifyTokensOnExample13()
        {
            AssertSequenceOfTokensFrom(ScannerFor("test13.yaml"),
                StreamStart,
                BlockMappingStart,
                Key,
                PlainScalar("a sequence"),
                Value,
                BlockSequenceStart,
                BlockEntry,
                PlainScalar("item 1"),
                BlockEntry,
                PlainScalar("item 2"),
                BlockEnd,
                Key,
                PlainScalar("a mapping"),
                Value,
                BlockMappingStart,
                Key,
                PlainScalar("key 1"),
                Value,
                PlainScalar("value 1"),
                Key,
                PlainScalar("key 2"),
                Value,
                PlainScalar("value 2"),
                BlockEnd,
                BlockEnd,
                StreamEnd);
        }

        [Test]
        public void VerifyTokensOnExample14()
        {
            AssertSequenceOfTokensFrom(ScannerFor("test14.yaml"),
                StreamStart,
                BlockMappingStart,
                Key,
                PlainScalar("key"),
                Value,
                BlockEntry,
                PlainScalar("item 1"),
                BlockEntry,
                PlainScalar("item 2"),
                BlockEnd,
                StreamEnd);
        }


        [Test]
        public void VerifyTokensOnExample15()
        {
            AssertSequenceOfTokensFrom(ScannerFor("test15.yaml"),
                StreamStart,
                FlowMappingStart,
                Key,
                PlainScalar("field1"),
                Value,
                DoubleQuotedScalar("R \ud83d\ude0e"),
                FlowEntry,
                Key,
                PlainScalar("field2"),
                Value,
                DoubleQuotedScalar("R \u0100\u0101"),
                FlowEntry,
                Key,
                PlainScalar("field3"),
                Value,
                DoubleQuotedScalar("R \u0100\ud83d\ude0e\u0101"),
                FlowMappingEnd,
                StreamEnd);
        }

        private Scanner<LookAheadBuffer> ScannerFor(string name)
        {
            return new Scanner<LookAheadBuffer>(new LookAheadBuffer(YamlFile(name), Scanner<LookAheadBuffer>.MaxBufferLength));
        }

        private void AssertSequenceOfTokensFrom(Scanner<LookAheadBuffer> scanner, params Token[] tokens)
        {
            var tokenNumber = 1;
            foreach (var expected in tokens)
            {
                Assert.True(scanner.MoveNext(), "Missing token number {0}", tokenNumber);
                AssertToken(expected, scanner.Current, tokenNumber);
                tokenNumber++;
            }
            Assert.False(scanner.MoveNext(), "Found extra tokens");
        }

        private void AssertToken(Token expected, Token actual, int tokenNumber)
        {
            Dump.WriteLine(expected.GetType().Name);
            Assert.NotNull(actual);
            Assert.AreEqual(expected.GetType(), actual.GetType(), "Token {0} is not of the expected type", tokenNumber);

            foreach (var property in expected.GetType().GetProperties())
            {
                if (property.PropertyType != typeof(Mark) && property.CanRead)
                {
                    var value = property.GetValue(actual, null);
                    var expectedValue = property.GetValue(expected, null);
                    Dump.WriteLine("\t{0} = {1}", property.Name, value);
                    Assert.AreEqual(expectedValue, value, "Comparing property {0} in token {1}", property.Name, tokenNumber);
                }
            }
        }
    }
}
