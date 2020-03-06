﻿// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading;
using RapidXamlToolkit.VisualStudioIntegration;
using RapidXamlToolkit.XamlAnalysis.Tags;

namespace RapidXamlToolkit.XamlAnalysis.Actions
{
    public class CustomAnalysisAction : BaseSuggestedAction
    {
        public CustomAnalysisAction(string file, CustomAnalysisTag tag)
            : base(file)
        {
            this.Tag = tag;
            this.DisplayText = tag.ActionText;

            this.CustomFeatureUsageOverride = tag.CustomFeatureUsageOverride;
        }

        public CustomAnalysisTag Tag { get; }

        public static CustomAnalysisAction Create(CustomAnalysisTag tag, string file)
        {
            return new CustomAnalysisAction(file, tag);
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            var vs = new VisualStudioTextManipulation(ProjectHelpers.Dte);
            vs.StartSingleUndoOperation(this.DisplayText);
            try
            {
                switch (this.Tag.Action)
                {
                    case RapidXaml.ActionType.AddAttribute:
                        var lineNumber = this.Tag.Snapshot.GetLineNumberFromPosition(this.Tag.InsertPosition) + 1;

                        var before = $"<{this.Tag.ElementName} ";
                        var after = $"<{this.Tag.ElementName} {this.Tag.Name}=\"{this.Tag.Value}\" ";

                        vs.ReplaceInActiveDocOnLine(before, after, lineNumber);

                        break;

                    case RapidXaml.ActionType.AddChild:

                        var origXaml = this.Tag.AnalyzedElement.OriginalString;

                        // Allow for self-closing elements
                        if (origXaml.EndsWith("/>"))
                        {
                            var replacementXaml = $">{Environment.NewLine}{this.Tag.Content}{Environment.NewLine}</{this.Tag.ElementName}>";

                            var insertLine = this.Tag.Snapshot.GetLineNumberFromPosition(this.Tag.InsertPosition) + 1;
                            vs.ReplaceInActiveDocOnLine("/>", replacementXaml, insertLine);
                        }
                        else
                        {
                            // Allows for opening and closing tags on same or different lines
                            var insertLine = this.Tag.Snapshot.GetLineNumberFromPosition(this.Tag.InsertPosition) + 1;
                            vs.InsertIntoActiveDocOnLineAfterClosingTag(insertLine, this.Tag.Content);
                        }

                        break;

                    case RapidXaml.ActionType.HighlightWithoutAction:
                        // As the name implies, do nothing.
                        break;

                    case RapidXaml.ActionType.RemoveAttribute:
                        if (this.Tag.IsInlineAttribute ?? false)
                        {
                            var currentAttribute = $"{this.Tag.Name}=\"{this.Tag.Value}\"";
                            vs.RemoveInActiveDocOnLine(currentAttribute, this.Tag.GetDesignerLineNumber());
                        }
                        else
                        {
                            var attrs = this.Tag.AnalyzedElement.GetAttributes(this.Tag.Name).ToList();

                            if (attrs.Count() == 1)
                            {
                                var attr = attrs.First();
                                var toRemove =
                                    this.Tag.AnalyzedElement.OriginalString.Substring(
                                        attr.Location.Start - this.Tag.InsertPosition,
                                        attr.Location.Length);

                                vs.RemoveInActiveDocOnLine(toRemove, this.Tag.GetDesignerLineNumber());
                            }
                        }

                        break;

                    case RapidXaml.ActionType.RemoveChild:
                        vs.RemoveInActiveDocOnLine(this.Tag.Element.OriginalString, this.Tag.GetDesignerLineNumber());
                        break;

                    case RapidXaml.ActionType.ReplaceElement:
                        vs.ReplaceInActiveDocOnLine(
                            this.Tag.AnalyzedElement.OriginalString,
                            this.Tag.Content,
                            this.Tag.Snapshot.GetLineNumberFromPosition(this.Tag.AnalyzedElement.Location.Start));
                        break;

                    case RapidXaml.ActionType.RenameElement:
                        // Just change opening tags as Visual Studio will change closing tags automatically
                        var renameLineNumber = this.Tag.Snapshot.GetLineNumberFromPosition(this.Tag.InsertPosition);
                        vs.ReplaceInActiveDocOnLine(this.Tag.ElementName, this.Tag.Name, renameLineNumber);

                        foreach (var childAttr in this.Tag.AnalyzedElement.ChildAttributes)
                        {
                            renameLineNumber = this.Tag.Snapshot.GetLineNumberFromPosition(childAttr.Location.Start);
                            vs.ReplaceInActiveDocOnLine($"{this.Tag.ElementName}.{childAttr.Name}", $"{this.Tag.Name}.{childAttr.Name}", renameLineNumber);
                        }

                        break;
                }

                RapidXamlDocumentCache.TryUpdate(this.File);
            }
            finally
            {
                vs.EndSingleUndoOperation();
            }
        }
    }
}
