﻿// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell;
using RapidXaml;
using RapidXamlToolkit.Resources;
using RapidXamlToolkit.VisualStudioIntegration;

namespace RapidXamlToolkit.XamlAnalysis.CustomAnalysis
{
    public abstract class BuiltInXamlAnalyzer : RapidXaml.ICustomAnalyzer
    {
        private readonly IVisualStudioAbstraction vsa;

        public BuiltInXamlAnalyzer(IVisualStudioAbstraction vsa)
        {
            this.vsa = vsa;
        }

        public abstract AnalysisActions Analyze(RapidXamlElement element, ExtraAnalysisDetails extraDetails);

        public abstract string TargetType();

        protected AnalysisActions CheckForHardCodedString(string attributeName, AttributeType attributeType, RapidXamlElement element, ExtraAnalysisDetails extraDetails)
        {
            var result = AnalysisActions.None;

            if (element.ContainsAttribute(attributeName)
             || (attributeType.HasFlag(AttributeType.DefaultValue) && element.OriginalString.Contains("</")))
            {
                // TODO: if can't get framework or resource file, hint at issue without fix
                // If don't know framework then can't know how to fix issues
                if (extraDetails.TryGet(KnownExtraDetails.Framework, out ProjectFramework framework))
                {
                    // If don't know file path, can't find appropriate resource file
                    if (extraDetails.TryGet(KnownExtraDetails.FilePath, out string fileName))
                    {
                        var resourceFilePath = this.GetResourceFilePath(fileName);

                        // Only make a suggestion if no resource file in project as autofix won't be possible
                        var warningLevel = string.IsNullOrWhiteSpace(resourceFilePath)
                            ? RapidXamlErrorType.Suggestion
                            : RapidXamlErrorType.Warning;

                        var attr = element.GetAttributes(attributeName).FirstOrDefault();

                        var value = GetAttributeValue(element, attr, attributeType);

                        if (!string.IsNullOrWhiteSpace(value) && char.IsLetterOrDigit(value[0]))
                        {
                            switch (framework)
                            {
                                case ProjectFramework.Uwp:

                                    var addUid = NeedToAddUid(element, attributeName, out string uid);

                                    // Create the resource first as there will always be a need to do this
                                    result = AnalysisActions.CreateResource(
                                        errorType: warningLevel,
                                        code: "RXT200",
                                        description: StringRes.UI_XamlAnalysisGenericHardCodedStringDescription.WithParams(element.Name, attributeName, value),
                                        actionText: StringRes.UI_XamlAnalysisHardcodedStringTooltip,
                                        resFilePath: resourceFilePath,
                                        resourceKey: uid,
                                        resourceValue: value,
                                        extendedMessage: StringRes.UI_XamlAnalysisHardcodedStringExtendedMessage
                                        );

                                    if (addUid)
                                    {
                                        result.AndAddAttribute("x:Uid", uid);
                                    }

                                    // Only something to remove if not the default value
                                    if (attr != null)
                                    {
                                        result.AndRemoveAttribute(attr);
                                    }
                                    else
                                    {
                                        result.AndRemoveDefaultValue();
                                    }

                                    break;
                                case ProjectFramework.Wpf:
                                case ProjectFramework.XamarinForms:

                                    var resourceNs = this.GetResourceFileNamespace(resourceFilePath);

                                    var resourceName = !string.IsNullOrWhiteSpace(fileName)
                                        ? $"{Path.GetFileNameWithoutExtension(fileName)}{value}".RemoveNonAlphaNumerics()
                                        : value.RemoveNonAlphaNumerics();

                                    var xmlnsToUse = "properties"; // default/fallback
                                    var xmlnsExists = true; // Assume existence. (i.e. don't add it. It's better than douplicating or adding something wrong)

                                    if (extraDetails.TryGet(KnownExtraDetails.Xmlns, out Dictionary<string, string> xmlns))
                                    {
                                        bool foundXmlns = false;

                                        foreach (var alias in xmlns)
                                        {
                                            if (alias.Value.Equals($"clr-namespace:{resourceNs}"))
                                            {
                                                resourceNs = alias.Key;
                                                foundXmlns = true;
                                                break;  // foreach
                                            }
                                        }

                                        if (!foundXmlns)
                                        {
                                            xmlnsExists = false;
                                        }
                                    }

                                    // Create the resource first as there will always be a need to do this
                                    result = AnalysisActions.CreateResource(
                                        errorType: warningLevel,
                                        code: "RXT200",
                                        description: StringRes.UI_XamlAnalysisGenericHardCodedStringDescription.WithParams(element.Name, attributeName, value),
                                        actionText: StringRes.UI_XamlAnalysisHardcodedStringTooltip,
                                        resFilePath: resourceFilePath,
                                        resourceKey: resourceName,
                                        resourceValue: value,
                                        extendedMessage: StringRes.UI_XamlAnalysisHardcodedStringExtendedMessage
                                        );

                                    // Only something to remove if not the default value
                                    if (attr != null)
                                    {
                                        result.AndRemoveAttribute(attr);
                                    }
                                    else
                                    {
                                        result.AndRemoveDefaultValue();
                                    }

                                    result.AndAddAttribute(
                                        attributeName,
                                        $"{{x:Static {xmlnsToUse}:{Path.GetFileNameWithoutExtension(resourceFilePath)}.{resourceName}}}");

                                    if (!xmlnsExists)
                                    {
                                        result.AndAddXmlns(xmlnsToUse, $"clr-namespace:{resourceNs}");
                                    }

                                    break;
                                case ProjectFramework.Unknown:
                                default:
                                    break;
                            }
                        }
                    }
                }
            }

            return result;
        }

        // TODO: Add unit tests for NeedToAddUid
        public static bool NeedToAddUid(RapidXamlElement element, string attributeName, out string uid)
        {
            var uidAttr = element.GetAttributes(Attributes.Uid).FirstOrDefault();

            var uidExists = (uidAttr != null && uidAttr.HasStringValue);

            if (uidExists)
            {
                uid = uidAttr.StringValue;
            }
            else
            {
                // reuse `Name` or `x:Name` if exist
                var nameAttr = element.GetAttributes(Attributes.Name).FirstOrDefault();
                if (nameAttr != null && nameAttr.HasStringValue)
                {
                    uid = nameAttr.StringValue;
                }
                else
                {
                    // Use defined attribute value
                    var fbAttr = element.GetAttributes(attributeName).FirstOrDefault();
                    if (fbAttr != null && fbAttr.HasStringValue)
                    {
                        uid = $"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(fbAttr.StringValue)}{element.Name}";

                        uid = uid.RemoveAllWhitespace().RemoveNonAlphaNumerics();
                    }
                    else
                    {
                        // This is just a large random number created to hopefully avoid collisions
                        uid = $"{element.Name}{new Random().Next(1001, 8999)}";
                    }
                }
            }

            return !uidExists;
        }

        // TODO: Add unit tests for GetAttributeValue
        private static string GetAttributeValue(RapidXamlElement element, RapidXamlAttribute attr, AttributeType attributeTypesToCheck)
        {
            if (attributeTypesToCheck.HasFlag(AttributeType.Inline))
            {
                if (attr != null && attr.IsInline)
                {
                    return attr.StringValue;
                }
            }

            if (attributeTypesToCheck.HasFlag(AttributeType.Element))
            {
                if (attr != null && !attr.IsInline)
                {
                    return attr.StringValue;
                }
            }

            if (attributeTypesToCheck.HasFlag(AttributeType.DefaultValue))
            {
                return element.Content;
            }

            return string.Empty;
        }

        private string GetResourceFileNamespace(string resPath)
        {
            if (string.IsNullOrWhiteSpace(resPath))
            {
                return string.Empty;
            }

            // It's fine that this is C# only as WPFCore doesn't (yet) and XF doesn't support VB
            // https://developercommunity.visualstudio.com/idea/750543/add-visual-basic-support-to-net-core-3-wpfwindows.html
            var designerFileName = Path.Combine(Path.GetDirectoryName(resPath), Path.GetFileNameWithoutExtension(resPath) + ".Designer.cs");

            if (!System.IO.File.Exists(designerFileName))
            {
                return string.Empty;
            }

            var lines = System.IO.File.ReadAllLines(designerFileName);

            foreach (var line in lines)
            {
                if (line.StartsWith("namespace "))
                {
                    return line.Substring(10).Trim(' ', '\t', '{');
                }
            }

            return null;
        }

        // TODO: cache resource file path
        private string GetResourceFilePath(string fileName)
        {
            // Get either type of res file. Don't have a reason for a project to contain both.
            var resFiles = vsa.GetFilesFromContainingProject(fileName, new[] { ".resw", ".resx" });

            if (resFiles.Count == 0)
            {
                SharedRapidXamlPackage.Logger?.RecordInfo(StringRes.Info_NoResourceFileFound);
                return null;
            }
            else if (resFiles.Count == 1)
            {
                return resFiles.First();
            }
            else
            {
                var langOfInterest = vsa.GetLanguageFromContainingProject(fileName);

                if (!string.IsNullOrWhiteSpace(langOfInterest))
                {
                    return resFiles.FirstOrDefault(f => f.IndexOf(langOfInterest, StringComparison.OrdinalIgnoreCase) > 0);
                }
                else
                {
                    // Find neutral language file to return
                    // RegEx to match if lang identifier in path or file name
                    return resFiles.FirstOrDefault(f => Regex.Matches(f, "([\\.][a-zA-Z]{2}-[a-zA-Z]{2}[\\.])").Count == 0);
                }
            }
        }
    }
}
