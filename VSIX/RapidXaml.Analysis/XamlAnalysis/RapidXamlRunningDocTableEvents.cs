﻿// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using RapidXamlToolkit.ErrorList;
using System.IO;

namespace RapidXamlToolkit.XamlAnalysis
{
    internal class RapidXamlRunningDocTableEvents : IVsRunningDocTableEvents
    {
        private readonly AsyncPackage package;
        private readonly RunningDocumentTable runningDocumentTable;

        public RapidXamlRunningDocTableEvents(AsyncPackage package, RunningDocumentTable runningDocumentTable)
        {
            this.package = package;
            this.runningDocumentTable = runningDocumentTable;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
            var documentInfo = this.runningDocumentTable.GetDocumentInfo(docCookie);

            var documentPath = documentInfo.Moniker;

            if (Path.GetExtension(documentPath) == ".xaml"
             && RapidXamlAnalysisPackage.IsLoaded
             && RapidXamlAnalysisPackage.Options.AnalyzeWhenDocumentSaved)
            {
                RapidXamlDocumentCache.TryUpdate(documentPath);
            }

            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            var documentInfo = this.runningDocumentTable.GetDocumentInfo(docCookie);

            if (documentInfo.Moniker != null)
            {
                TableDataSource.Instance.CleanErrors(documentInfo.Moniker);
            }

            return VSConstants.S_OK;
        }
    }
}
