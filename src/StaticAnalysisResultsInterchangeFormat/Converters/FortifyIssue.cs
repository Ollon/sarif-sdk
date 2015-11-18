﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.CodeAnalysis.Driver;

namespace Microsoft.CodeAnalysis.StaticAnalysisResultsInterchangeFormat.Converters
{
    /// <summary>A Fortify issue element.</summary>
    internal class FortifyIssue
    {
        private static readonly Regex s_cweRegex = new Regex("CWE ID (\\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>The Rule ID stored in the Fortify issue if present; otherwise, null.</summary>
        public readonly string RuleId;

        /// <summary>The Instance ID stored in the Fortify issue if present; otherwise, null.</summary>
        public readonly string InstanceId;

        /// <summary>The category of Fortify issue.</summary>
        public readonly string Category;

        /// <summary>The kingdom of the Fortify issue.</summary>
        public readonly string Kingdom;

        /// <summary>The abstract (description) message of the Fortify issue if present; otherwise, null.</summary>
        public readonly string Abstract;

        /// <summary>
        /// A custom user-provided abstract (description) message of the Fortify issue if it is present;
        /// otherwise, null.
        /// </summary>
        public readonly string AbstractCustom;

        /// <summary>
        /// The "friority" (which appears to be an intentional misspelling of "priority") of the Fortify
        /// issue if present; otherwise, null.
        /// </summary>
        public readonly string Priority;

        /// <summary>The primary location for the Fortify issue; this will be the sink for data flow rules.</summary>
        public readonly FortifyPathElement PrimaryOrSink;

        /// <summary>Source for the flagged data flow if present; otherwise, null.</summary>
        public readonly FortifyPathElement Source;

        /// <summary>List of CWE IDs stapled to the Fortify issue, if present.</summary>
        public readonly ImmutableArray<int> CweIds;

        /// <summary>Initializes a new instance of the <see cref="FortifyIssue"/> class.</summary>
        /// <param name="ruleId">The Rule ID stored in the Fortify issue.</param>
        /// <param name="iid">The Instance ID stored in the Fortify issue.</param>
        /// <param name="category">The category of Fortify issue.</param>
        /// <param name="kingdom">The kingdom of the Fortify issue.</param>
        /// <param name="abs">The abstract (description) message of the Fortify issue if present; otherwise, null.</param>
        /// <param name="abstractCustom">A custom user-provided abstract (description) message of the Fortify issue if it is present;
        /// otherwise, null.</param>
        /// <param name="priority">The "friority" (which appears to be an intentional misspelling of "priority") of the Fortify
        /// issue if present; otherwise, null.</param>
        /// <param name="primaryOrSink">The primary location for the Fortify issue; this will be the sink for data flow rules.</param>
        /// <param name="source">Source for the flagged data flow if present; otherwise, null.</param>
        /// <param name="cweIds">List of CWE IDs stapled to the Fortify issue, if present.</param>
        public FortifyIssue(string ruleId, string iid, string category, string kingdom, string abs, string abstractCustom, string priority, FortifyPathElement primaryOrSink, FortifyPathElement source, ImmutableArray<int> cweIds)
        {
            if (category == null)
            {
                throw new ArgumentNullException("category");
            }

            if (kingdom == null)
            {
                throw new ArgumentNullException("kingdom");
            }

            if (primaryOrSink == null)
            {
                throw new ArgumentNullException("primaryOrSink");
            }

            this.RuleId = ruleId;
            this.InstanceId = iid;
            this.Category = category;
            this.Kingdom = kingdom;
            this.Abstract = abs;
            this.AbstractCustom = abstractCustom;
            this.Priority = priority;
            this.PrimaryOrSink = primaryOrSink;
            this.Source = source;
            this.CweIds = cweIds;
        }

        /// <summary>
        /// Parses a Fortify Issue element from an <see cref="XmlReader"/>.
        /// </summary>
        /// <param name="xmlReader">The <see cref="XmlReader"/> from which an element containing a Fortify issue shall be
        /// consumed. When this method returns, this <see cref="XmlReader"/> is positioned on the following element.</param>
        /// <param name="strings">Strings used in processing a Fortify report.</param>
        /// <returns>A <see cref="FortifyIssue"/> containing data from the node on which <paramref name="xmlReader"/> was
        /// placed when this method was called.</returns>
        public static FortifyIssue Parse(XmlReader xmlReader, FortifyStrings strings)
        {
            //<xs:element name="Issue">
            //    <xs:complexType>
            //        <xs:sequence>
            //            <!-- Issue Description -->
            //            <xs:element name="Category" type="xs:string" minOccurs="1" maxOccurs="1"/>
            //            <xs:element name="Folder" type="xs:string" minOccurs="1" maxOccurs="1"/>
            //            <xs:element name="Kingdom" type="xs:string" minOccurs="1" maxOccurs="1"/>
            //            <xs:element name="Abstract" type="xs:string" minOccurs="0" maxOccurs="1"/>
            //            <xs:element name="AbstractCustom" type="xs:string" minOccurs="0" maxOccurs="1"/>
            //            <xs:element name="Friority" type="xs:string" minOccurs="0" maxOccurs="1"/>
            //            <!-- custom tags including Analysis -->
            //            <xs:element name="Tag" minOccurs="0" maxOccurs="unbounded">
            //                <xs:complexType>
            //                    <xs:sequence>
            //                        <xs:element name="Name" type="xs:string" minOccurs="1" maxOccurs="1"/>
            //                        <xs:element name="Value" type="xs:string" minOccurs="1" maxOccurs="1"/>
            //                    </xs:sequence>
            //                </xs:complexType>
            //            </xs:element>
            //            <xs:element name="Comment" minOccurs="0" maxOccurs="unbounded">
            //                <xs:complexType>
            //                    <xs:sequence>
            //                        <xs:element name="UserInfo" type="xs:string" minOccurs="1" maxOccurs="1"/>
            //                        <xs:element name="Comment" type="xs:string" minOccurs="1" maxOccurs="1"/>
            //                    </xs:sequence>
            //                </xs:complexType>
            //            </xs:element>
            //            <!-- primary or sink -->
            //            <xs:element name="Primary" type="PathElement" minOccurs="1" maxOccurs="1"/>
            //            <!-- source -->
            //            <xs:element name="Source" type="PathElement" minOccurs="0" maxOccurs="1"/>
            //            <xs:element name="TraceDiagramPath" type="xs:string" minOccurs="0" maxOccurs="1"/>
            //            <!-- optional external category (i.e. STIG) -->
            //            <xs:element name="ExternalCategory" minOccurs="0" maxOccurs="1">
            //                <xs:complexType>
            //                    <xs:simpleContent>
            //                        <xs:extension base="xs:string">
            //                            <xs:attribute name="type" type="xs:string" use="required"/>
            //                        </xs:extension>
            //                    </xs:simpleContent>
            //                </xs:complexType>
            //            </xs:element>
            //        </xs:sequence>
            //        <xs:attribute name="iid" type="xs:string" use="optional"/>
            //        <xs:attribute name="ruleID" type="xs:string" use="optional"/>
            //    </xs:complexType>
            //</xs:element>
            if (!xmlReader.IsStartElement(strings.Issue))
            {
                throw xmlReader.CreateException(SarifResources.FortifyNotValidIssue);
            }

            string iid = null;
            string ruleId = null;
            while (xmlReader.MoveToNextAttribute())
            {
                string name = xmlReader.LocalName;
                if (Ref.Equal(name, strings.Iid))
                {
                    iid = xmlReader.Value;
                }
                else if (Ref.Equal(name, strings.RuleId))
                {
                    ruleId = xmlReader.Value;
                }
            }

            xmlReader.MoveToElement();
            xmlReader.Read(); // reads start element

            string category = xmlReader.ReadElementContentAsString(strings.Category, String.Empty);
            xmlReader.IgnoreElement(strings.Folder, IgnoreOptions.Required);
            string kingdom = xmlReader.ReadElementContentAsString(strings.Kingdom, String.Empty);
            string abstract_ = xmlReader.ReadOptionalElementContentAsString(strings.Abstract);
            string abstractCustom = xmlReader.ReadOptionalElementContentAsString(strings.AbstractCustom);
            string friority = xmlReader.ReadOptionalElementContentAsString(strings.Friority);
            xmlReader.IgnoreElement(strings.Tag, IgnoreOptions.Optional | IgnoreOptions.Multiple);
            xmlReader.IgnoreElement(strings.Comment, IgnoreOptions.Optional | IgnoreOptions.Multiple);
            FortifyPathElement primary = FortifyPathElement.Parse(xmlReader, strings);
            FortifyPathElement source;
            if (xmlReader.NodeType == XmlNodeType.Element && Ref.Equal(xmlReader.LocalName, strings.Source))
            {
                source = FortifyPathElement.Parse(xmlReader, strings);
            }
            else
            {
                source = null;
            }

            xmlReader.IgnoreElement(strings.TraceDiagramPath, IgnoreOptions.Optional);
            ImmutableArray<int> cweIds = ImmutableArray<int>.Empty;
            if (xmlReader.NodeType == XmlNodeType.Element && Ref.Equal(xmlReader.LocalName, strings.ExternalCategory))
            {
                if (xmlReader.GetAttribute(strings.Type) == "CWE")
                {
                    cweIds = ParseCweIds(xmlReader.ReadElementContentAsString());
                }
                else
                {
                    xmlReader.Skip();
                }
            }

            xmlReader.ReadEndElement(); // </Issue>

            return new FortifyIssue(ruleId, iid, category, kingdom, abstract_, abstractCustom, friority, primary, source, cweIds);
        }

        /// <summary>
        /// Converts the CWE ID format from Fortify to a plain list of integers.
        /// </summary>
        /// <param name="cweIdSource">The string from which CWE IDs shall be parsed.</param>
        /// <returns>The CWE IDs from the supplied string as an immutable array.</returns>
        public static ImmutableArray<int> ParseCweIds(string cweIdSource)
        {
            var result = new SortedSet<int>();
            foreach (Match match in s_cweRegex.Matches(cweIdSource))
            {
                result.Add(Int32.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
            }

            return result.ToImmutableArray();
        }
    }
}
