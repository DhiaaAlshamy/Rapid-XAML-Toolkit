﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using RapidXamlToolkit.Resources;
using RapidXamlToolkit.XamlAnalysis.Actions;
using RapidXamlToolkit.XamlAnalysis.Tags;

namespace RapidXamlToolkit.XamlAnalysis.Processors
{
    public class ToggleMenuFlyoutItemProcessor : XamlElementProcessor
    {
        public override void Process(int offset, string xamlElement, string linePadding, ITextSnapshot snapshot, List<IRapidXamlAdornmentTag> tags)
        {
            var (uidExists, uid) = this.GetOrGenerateUid(xamlElement, Attributes.Text);

            this.CheckForHardCodedAttribute(
                Elements.ToggleMenuFlyoutItem,
                Attributes.Text,
                AttributeType.InlineOrElement,
                StringRes.Info_XamlAnalysisHardcodedStringToggleMenuFlyoutItemTextMessage,
                xamlElement,
                snapshot,
                offset,
                uidExists,
                uid,
                tags);
        }
    }
}
