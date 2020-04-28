﻿// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using Microsoft.VisualStudio.Text;
using RapidXamlToolkit.Logging;
using RapidXamlToolkit.Resources;
using RapidXamlToolkit.VisualStudioIntegration;
using RapidXamlToolkit.XamlAnalysis.Actions;

namespace RapidXamlToolkit.XamlAnalysis.Tags
{
    public class AddTextBoxInputScopeTag : RapidXamlDisplayedTag
    {
        public AddTextBoxInputScopeTag(Span span, ITextSnapshot snapshot, string fileName, ILogger logger, IVisualStudioAbstraction vsa, string projectPath)
            : base(span, snapshot, fileName, "RXT150", TagErrorType.Suggestion, logger, vsa, projectPath)
        {
            this.SuggestedAction = typeof(AddTextBoxInputScopeAction);
            this.Description = StringRes.UI_XamlAnalysisTextBoxWithoutInputScopeDescription;
            this.ExtendedMessage = StringRes.UI_XamlAnalysisTextBoxWithoutInputScopeExtendedMessage;
        }

        public int InsertPosition { get; set; }
    }
}
