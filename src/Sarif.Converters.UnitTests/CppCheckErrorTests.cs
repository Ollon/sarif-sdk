﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml;
using FluentAssertions;
using Xunit;

namespace Microsoft.CodeAnalysis.Sarif.Converters
{
    public class CppCheckErrorTests
    {
        private readonly ImmutableArray<CppCheckLocation> _dummyLocations = ImmutableArray.Create(new CppCheckLocation("file.cpp", 42));

        [Fact]
        public void CppCheckError_PassesThroughConstructorParameters()
        {
            var uut = new CppCheckError("id", "message", "verbose", "style", _dummyLocations);
            AssertOuterPropertiesAreExampleError(uut);
            Assert.Equal(_dummyLocations, uut.Locations);
        }

        [Fact]
        public void CppCheckError_RequiresNonEmptyLocations()
        {
            Assert.Throws<ArgumentException>(() => new CppCheckError("id", "message", "verbose", "style", ImmutableArray<CppCheckLocation>.Empty));
        }

        [Fact]
        public void CppCheckError_MinimalErrorCanBeConvertedToSarifIssue()
        {
            Result result = new CppCheckError("id", "message", "verbose", "style", _dummyLocations)
                .ToSarifIssue();
            Assert.Equal("id", result.RuleId);
            Assert.Equal("verbose", result.Message);
            result.PropertyNames.Count.Should().Be(1);
            result.GetProperty("Severity").Should().Be("style");
            Assert.Equal("file.cpp", result.Locations.First().ResultFile.Uri.ToString());
        }

        [Fact]
        public void CppCheckError_ErrorWithSingleLocationIsConvertedToSarifIssue()
        {
            Result result = new CppCheckError("id", "message", "verbose", "my fancy severity", ImmutableArray.Create(
                new CppCheckLocation("foo.cpp", 1234)
                )).ToSarifIssue();
            Assert.Equal("id", result.RuleId);
            Assert.Equal("verbose", result.Message);
            result.PropertyNames.Count.Should().Be(1);
            result.GetProperty("Severity").Should().Be("my fancy severity");
            result.Locations.SequenceEqual(new[] { new Location {
                    ResultFile = new PhysicalLocation
                    {
                        Uri = new Uri("foo.cpp", UriKind.RelativeOrAbsolute),
                        Region = new Region { StartLine = 1234 }
                    }
                }
            }, Location.ValueComparer).Should().BeTrue();
            Assert.Null(result.CodeFlows);
        }

        [Fact]
        public void CppCheckError_ErrorWithMultipleLocationsFillsOutCodeFlow()
        {
            Result result = new CppCheckError("id", "message", "verbose", "my fancy severity", ImmutableArray.Create(
                new CppCheckLocation("foo.cpp", 1234),
                new CppCheckLocation("bar.cpp", 5678)
                )).ToSarifIssue();

            result.Locations.SequenceEqual(new[] { new Location {
                        ResultFile = new PhysicalLocation
                        {
                            Uri = new Uri("bar.cpp", UriKind.RelativeOrAbsolute),
                            Region = new Region { StartLine = 5678 }
                        }
                    }
                }, Location.ValueComparer).Should().BeTrue();

            Assert.Equal(1, result.CodeFlows.Count);
            result.CodeFlows.First().Locations.SequenceEqual(new[]
                {
                    new AnnotatedCodeLocation {
                        PhysicalLocation = new PhysicalLocation {
                            Uri = new Uri("foo.cpp", UriKind.RelativeOrAbsolute),
                            Region = new Region { StartLine = 1234 },
                        },
                        Importance = AnnotatedCodeLocationImportance.Essential
                    },
                    new AnnotatedCodeLocation {
                        PhysicalLocation = new PhysicalLocation {
                            Uri = new Uri("bar.cpp", UriKind.RelativeOrAbsolute),
                            Region = new Region { StartLine = 5678 }
                        },
                        Importance = AnnotatedCodeLocationImportance.Essential
                    }
                }, AnnotatedCodeLocation.ValueComparer).Should().BeTrue();
        }

        [Fact]
        public void CppCheckError_DoesNotEmitShortMessageWhenVerboseMessageIsTheSame()
        {
            Result result = new CppCheckError("id", "message", "message", "style", _dummyLocations)
                .ToSarifIssue();
            Assert.Equal("message", result.Message);
        }

        [Fact]
        public void CppCheckError_RejectsSelfClosingError()
        {
            using (XmlReader xml = Utilities.CreateXmlReaderFromString(exampleErrorXmlSelfClosed))
            {
                Assert.Throws<XmlException>(() => Parse(xml));
                //AssertOuterPropertiesAreExampleError(uut.);
                //uut.Locations.Should().BeEmpty();
            }
        }

        [Fact]
        public void CppCheckError_RejectsErrorWithNoLocations()
        {
            using (XmlReader xml = Utilities.CreateXmlReaderFromString(exampleErrorXmlOpen + exampleErrorClose))
            {
                Assert.Throws<XmlException>(() => Parse(xml));
                //AssertOuterPropertiesAreExampleError(uut);
                //uut.Locations.Should().BeEmpty();
            }
        }

        [Fact]
        public void CppCheckError_CanParseErrorWithSingleLocation()
        {
            string errorXml = exampleErrorXmlOpen + " <location file=\"foo.cpp\" line=\"42\" /> " + exampleErrorClose;
            using (XmlReader xml = Utilities.CreateXmlReaderFromString(errorXml))
            {
                var uut = Parse(xml);
                AssertOuterPropertiesAreExampleError(uut);
                uut.Locations.Should().Equal(new[] { new CppCheckLocation("foo.cpp", 42) });
            }
        }

        [Fact]
        public void CppCheckError_CanParseErrorWithMultipleLocations()
        {
            string errorXml = exampleErrorXmlOpen + " <location file=\"foo.cpp\" line=\"42\" />  <location file=\"bar.cpp\" line=\"1729\" /> " + exampleErrorClose;
            using (XmlReader xml = Utilities.CreateXmlReaderFromString(errorXml))
            {
                var uut = Parse(xml);
                AssertOuterPropertiesAreExampleError(uut);
                uut.Locations.Should().Equal(new[] {
                    new CppCheckLocation("foo.cpp", 42),
                    new CppCheckLocation("bar.cpp", 1729)
                });
            }
        }

        [Fact]
        public void CppCheckError_InvalidParse_BadRootNodeDetected()
        {
            using (XmlReader xml = Utilities.CreateXmlReaderFromString("<foobar />"))
            {
                Assert.Throws<XmlException>(() => Parse(xml));
            }
        }

        [Fact]
        public void CppCheckError_InvalidParse_BadChildrenNodeDetected()
        {
            using (XmlReader xml = Utilities.CreateXmlReaderFromString(exampleErrorXmlOpen + "<foobar />" + exampleErrorClose))
            {
                Assert.Throws<XmlException>(() => Parse(xml));
            }
        }

        private const string exampleErrorXmlBase = "<error id=\"id\" msg=\"message\" verbose=\"verbose\" severity=\"style\"";
        private const string exampleErrorXmlOpen = exampleErrorXmlBase + ">";
        private const string exampleErrorClose = "</error>";
        private const string exampleErrorXmlSelfClosed = exampleErrorXmlBase + " />";
        private static void AssertOuterPropertiesAreExampleError(CppCheckError uut)
        {
            Assert.Equal("id", uut.Id);
            Assert.Equal("message", uut.Message);
            Assert.Equal("verbose", uut.VerboseMessage);
            Assert.Equal("style", uut.Severity);
        }

        private static CppCheckError Parse(XmlReader xml)
        {
            return CppCheckError.Parse(xml, new CppCheckStrings(xml.NameTable));
        }
    }
}
