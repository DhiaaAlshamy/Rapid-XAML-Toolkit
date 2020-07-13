﻿// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System.Threading;
using RapidXamlToolkit.ErrorList;
using RapidXamlToolkit.Resources;
using RapidXamlToolkit.XamlAnalysis.Tags;

namespace RapidXamlToolkit.XamlAnalysis.Actions
{
    internal class SuppressWarningAction : BaseSuggestedAction
    {
        public SuppressWarningAction(string file)
            : base(file)
        {
        }

        public string ErrorCode { get; private set; }

        public RapidXamlDisplayedTag Tag { get; private set; }

        public SuggestedActionsSource Source { get; private set; }

        public static SuppressWarningAction Create(RapidXamlDisplayedTag tag, string file, SuggestedActionsSource suggestedActionsSource)
        {
            var result = new SuppressWarningAction(file)
            {
                Tag = tag,
                DisplayText = StringRes.UI_SuggestedActionDoNotWarn.WithParams(tag.ErrorCode),
                ErrorCode = tag.ErrorCode,
                Source = suggestedActionsSource,
            };

            return result;
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            this.Tag.SetAsHiddenInSettingsFile();
            this.Source.Refresh();
            RapidXamlDocumentCache.RemoveTags(this.Tag.FileName, this.ErrorCode);
            RapidXamlDocumentCache.Invalidate(this.Tag.FileName);
            RapidXamlDocumentCache.TryUpdate(this.Tag.FileName);
            TableDataSource.Instance.CleanErrors(this.Tag.FileName);
        }
    }
}
