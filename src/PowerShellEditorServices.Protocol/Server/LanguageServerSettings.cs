﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using Microsoft.PowerShell.EditorServices.Utility;
using System;
using System.Reflection;
using System.Collections;

namespace Microsoft.PowerShell.EditorServices.Protocol.Server
{
    public class LanguageServerSettings
    {
        public bool EnableProfileLoading { get; set; }

        public ScriptAnalysisSettings ScriptAnalysis { get; set; }

        public CodeFormattingSettings CodeFormatting { get; set; }

        public LanguageServerSettings()
        {
            this.ScriptAnalysis = new ScriptAnalysisSettings();
        }

        public void Update(
            LanguageServerSettings settings,
            string workspaceRootPath,
            ILogger logger)
        {
            if (settings != null)
            {
                this.EnableProfileLoading = settings.EnableProfileLoading;
                this.ScriptAnalysis.Update(
                    settings.ScriptAnalysis,
                    workspaceRootPath,
                    logger);
                this.CodeFormatting = new CodeFormattingSettings(settings.CodeFormatting);
            }
        }
    }

    public class ScriptAnalysisSettings
    {
        public bool? Enable { get; set; }

        public string SettingsPath { get; set; }

        public ScriptAnalysisSettings()
        {
            this.Enable = true;
        }

        public void Update(
            ScriptAnalysisSettings settings,
            string workspaceRootPath,
            ILogger logger)
        {
            if (settings != null)
            {
                this.Enable = settings.Enable;

                string settingsPath = settings.SettingsPath;

                if (string.IsNullOrWhiteSpace(settingsPath))
                {
                    settingsPath = null;
                }
                else if (!Path.IsPathRooted(settingsPath))
                {
                    if (string.IsNullOrEmpty(workspaceRootPath))
                    {
                        // The workspace root path could be an empty string
                        // when the user has opened a PowerShell script file
                        // without opening an entire folder (workspace) first.
                        // In this case we should just log an error and let
                        // the specified settings path go through even though
                        // it will fail to load.
                        logger.Write(
                            LogLevel.Error,
                            "Could not resolve Script Analyzer settings path due to null or empty workspaceRootPath.");
                    }
                    else
                    {
                        settingsPath = Path.GetFullPath(Path.Combine(workspaceRootPath, settingsPath));
                    }
                }

                this.SettingsPath = settingsPath;
            }
        }
    }

    public class CodeFormattingSettings
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public CodeFormattingSettings()
        {

        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="codeFormattingSettings">An instance of type CodeFormattingSettings.</param>
        public CodeFormattingSettings(CodeFormattingSettings codeFormattingSettings)
        {
            if (codeFormattingSettings == null)
            {
                throw new ArgumentNullException(nameof(codeFormattingSettings));
            }

            foreach (var prop in this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                prop.SetValue(this, prop.GetValue(codeFormattingSettings));
            }
        }

        public bool OpenBraceOnSameLine { get; set; }
        public bool NewLineAfterOpenBrace { get; set; }
        public bool NewLineAfterCloseBrace { get; set; }
        public bool WhitespaceBeforeOpenBrace { get; set; }
        public bool WhitespaceBeforeOpenParen { get; set; }
        public bool WhitespaceAroundOperator { get; set; }
        public bool WhitespaceAfterSeparator { get; set; }
        public bool IgnoreOneLineBlock { get; set; }
        public bool AlignPropertyValuePairs { get; set; }

        public Hashtable GetPSSASettingsHashTable(int tabSize, bool insertSpaces)
        {
            return new Hashtable
            {
                {"IncludeRules", new string[] {
                     "PSPlaceCloseBrace",
                     "PSPlaceOpenBrace",
                     "PSUseConsistentWhitespace",
                     "PSUseConsistentIndentation",
                     "PSAlignAssignmentStatement"
                }},
                {"Rules", new Hashtable {
                    {"PSPlaceOpenBrace", new Hashtable {
                        {"Enable", true},
                        {"OnSameLine", OpenBraceOnSameLine},
                        {"NewLineAfter", NewLineAfterOpenBrace},
                        {"IgnoreOneLineBlock", IgnoreOneLineBlock}
                    }},
                    {"PSPlaceCloseBrace", new Hashtable {
                        {"Enable", true},
                        {"NewLineAfter", NewLineAfterCloseBrace},
                        {"IgnoreOneLineBlock", IgnoreOneLineBlock}
                    }},
                    {"PSUseConsistentIndentation", new Hashtable {
                        {"Enable", true},
                        {"IndentationSize", tabSize}
                    }},
                    {"PSUseConsistentWhitespace", new Hashtable {
                        {"Enable", true},
                        {"CheckOpenBrace", WhitespaceBeforeOpenBrace},
                        {"CheckOpenParen", WhitespaceBeforeOpenParen},
                        {"CheckOperator", WhitespaceAroundOperator},
                        {"CheckSeparator", WhitespaceAfterSeparator}
                    }},
                    {"PSAlignAssignmentStatement", new Hashtable {
                        {"Enable", true},
                        {"CheckHashtable", AlignPropertyValuePairs}
                    }},
                }}
            };
        }
    }

    public class LanguageServerSettingsWrapper
        {
            // NOTE: This property is capitalized as 'Powershell' because the
            // mode name sent from the client is written as 'powershell' and
            // JSON.net is using camelCasing.

            public LanguageServerSettings Powershell { get; set; }
        }
    }
