﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.CodeAnalysis.Driver;
using Microsoft.CodeAnalysis.StaticAnalysisResultsInterchangeFormat.Writers;
using Microsoft.CodeAnalysis.StaticAnalysisResultsInterchangeFormat.DataContracts;

namespace Microsoft.CodeAnalysis.StaticAnalysisResultsInterchangeFormat.Converters
{
    internal class FortifyConverter : IToolFileConverter
    {
        private readonly NameTable _nameTable;
        private readonly FortifyStrings _strings;

        /// <summary>Initializes a new instance of the <see cref="FortifyConverter"/> class.</summary>
        public FortifyConverter()
        {
            _nameTable = new NameTable();
            _strings = new FortifyStrings(_nameTable);
        }

        /// <summary>
        /// Interface implementation for converting a stream of Fortify report in XML format to an OES
        /// json format stream.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
        /// <param name="input">Stream of the Fortify report.</param>
        /// <param name="output">Stream of OES json format.</param>
        public void Convert(Stream input, IIssueLogWriter output)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            output.WriteToolAndRunInfo(new ToolInfo
            {
                ToolName = "Fortify"
            }, null);

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                IgnoreWhitespace = true,
                NameTable = _nameTable
            };

            using (XmlReader reader = XmlReader.Create(input, settings))
            {
                while (reader.Read())
                {
                    while (Ref.Equal(reader.LocalName, _strings.Issue))
                    {
                        FortifyIssue fortify = FortifyIssue.Parse(reader, _strings);
                        output.WriteIssue(ConvertFortifyIssueToSarifIssue(fortify));
                    }
                }
            }
        }

        /// <summary>Converts a Fortify issue to a static analysis results interchange format issue.</summary>
        /// <param name="fortify">The Fortify issue convert.</param>
        /// <returns>
        /// A Unified Issue Store <see cref="Issue"/> containing the same content as the supplied
        /// <see cref="FortifyIssue"/>.
        /// </returns>
        public static Issue ConvertFortifyIssueToSarifIssue(FortifyIssue fortify)
        {
            var result = new Issue();
            result.RuleId = fortify.Category;
            result.ToolFingerprint = fortify.InstanceId;
            List<string> messageComponents = new List<string>();
            if (fortify.Abstract != null)
            {
                messageComponents.Add(fortify.Abstract);
            }

            if (fortify.AbstractCustom != null)
            {
                messageComponents.Add(fortify.AbstractCustom);
            }

            if (messageComponents.Count == 0)
            {
                result.FullMessage = String.Format(CultureInfo.InvariantCulture, SarifResources.FortifyFallbackMessage, result.RuleId);
            }
            else
            {
                result.FullMessage = String.Join(Environment.NewLine, messageComponents);
            }

            var extraProperties = new Dictionary<string, string>();
            extraProperties.Add("kingdom", fortify.Kingdom);
            if (fortify.Priority != null)
            {
                extraProperties.Add("priority", fortify.Priority);
            }

            if (!fortify.CweIds.IsDefaultOrEmpty)
            {
                extraProperties.Add("cwe", String.Join(", ",
                    fortify.CweIds.Select(id => id.ToString(CultureInfo.InvariantCulture))));
            }

            if (fortify.RuleId != null)
            {
                extraProperties.Add("fortifyRuleId", fortify.RuleId);
            }

            result.Properties = extraProperties;

            List<PhysicalLocationComponent> primaryOrSink = ConvertFortifyLocationToPhysicalLocation(fortify.PrimaryOrSink);
            result.Locations = new[]
            {
                new Location
                {
                    IssueFile = primaryOrSink
                }
            };

            if (fortify.Source != null)
            {
                List<PhysicalLocationComponent> source = ConvertFortifyLocationToPhysicalLocation(fortify.Source);
                result.ExecutionFlows = new[]
                {
                    new[]
                    {
                        new ExecutionFlowEntry { PhysicalLocations = new[] { source }},
                        new ExecutionFlowEntry { PhysicalLocations = new[] { primaryOrSink }}
                    }
                };
            }

            return result;
        }

        private static List<PhysicalLocationComponent> ConvertFortifyLocationToPhysicalLocation(FortifyPathElement element)
        {
            return new List<PhysicalLocationComponent>
            {
                new PhysicalLocationComponent
                {
                    Uri = new Uri(element.FilePath, UriKind.RelativeOrAbsolute),
                    MimeType = MimeType.DetermineFromFileExtension(element.FilePath),
                    Region = Extensions.CreateRegion(element.LineStart)
                }
            };
        }
    }
}