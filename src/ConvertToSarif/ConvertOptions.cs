﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using CommandLine;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Sarif.ConvertToSarif
{
    [Verb("convert", HelpText = "Convert a tool output log to SARIF format.")]
    internal class ConvertOptions
    {
        [Value(0,
               HelpText = "A file path to a tool log file that should be converted to the SARIF format.")]
        public string InputFilePath { get; internal set; }

        [Option(
            't',
            "tool",
            HelpText = "The tool format of the input file.")]
        public string ToolFormat { get; internal set; }

        [Option(
            'c',
            "converters",
            Required = false,
            HelpText = "Path to a plug-in file which contains the SARIF log file converter.")]
        public string ConverterFilePath { get; set; }

        [Option(
            'o',
            "output",
            HelpText = "A file path to the converted SARIF log. Defaults to <input file name>.sarif.")]
        public string OutputFilePath { get; internal set; }

        [Option(
            'p',
            "pretty",
            Default = false,
            HelpText = "Produce pretty-printed JSON output rather than compact form.")]
        public bool PrettyPrint { get; internal set; }

        [Option(
            'f',
            "force",
            Default = false,
            HelpText = "Force overwrite of output file if it exists.")]
        public bool Force { get; internal set; }      
    }
}