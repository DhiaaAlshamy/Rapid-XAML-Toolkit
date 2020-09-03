﻿// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using RapidXamlToolkit.Logging;

namespace RapidXamlToolkit.XamlAnalysis.Tags
{
    public abstract class RapidXamlDiscreteTag : RapidXamlAdornmentTag
    {
        protected RapidXamlDiscreteTag(Span span, ITextSnapshot snapshot, string fileName, ILogger logger)
            : base(span, snapshot, fileName, logger)
        {
        }

        public override ITagSpan<IErrorTag> AsErrorTag()
        {
            var span = new SnapshotSpan(this.Snapshot, this.Span);
            return new TagSpan<IErrorTag>(span, new RapidXamlSuggestionAdornmentTag(this.ToolTip));
        }
    }
}
