﻿// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.DragDrop;
using Microsoft.VisualStudio.Text.Operations;
using RapidXamlToolkit.Commands;
using RapidXamlToolkit.Logging;
using RapidXamlToolkit.Resources;
using RapidXamlToolkit.VisualStudioIntegration;

namespace RapidXamlToolkit.DragDrop
{
    internal class RapidXamlDropHandler : IDropHandler
    {
        private readonly ILogger logger;
        private readonly IWpfTextView view;
        private readonly ITextBufferUndoManager undoManager;
        private readonly IFileSystemAbstraction fileSystem;
        private readonly IVisualStudioAbstractionAndDocumentModelAccess vs;
        private readonly ProjectType projectType;
        private string draggedFilename;

        public RapidXamlDropHandler(ILogger logger, IWpfTextView view, ITextBufferUndoManager undoManager, IVisualStudioAbstractionAndDocumentModelAccess vs, ProjectType projectType, IFileSystemAbstraction fileSystem = null)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.view = view;
            this.undoManager = undoManager;
            this.vs = vs ?? throw new ArgumentNullException(nameof(vs));
            this.projectType = projectType;
            this.fileSystem = fileSystem ?? new WindowsFileSystem();
        }

        public DragDropPointerEffects HandleDataDropped(DragDropInfo dragDropInfo)
        {
            var position = dragDropInfo.VirtualBufferPosition.Position;

            // Get left padding (allowing for drop postion to not be on the start/end of a line)
            var insertLineStart = this.view.GetTextViewLineContainingBufferPosition(position).Start.Position;

            var insertLinePadding = position.Position - insertLineStart;

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                try
                {
                    this.logger?.RecordFeatureUsage(nameof(RapidXamlDropHandler));
                    this.logger?.RecordInfo(StringRes.Info_FileDropped.WithParams(this.draggedFilename));

                    var logic = new DropHandlerLogic(this.logger, this.vs, this.fileSystem);

                    var textOutput = await logic.ExecuteAsync(this.draggedFilename, insertLinePadding, this.projectType);

                    if (!string.IsNullOrEmpty(textOutput))
                    {
                        this.view.TextBuffer.Insert(position.Position, textOutput);
                    }
                }
                catch (Exception exc)
                {
                    this.logger?.RecordException(exc);
                }
            });

            return DragDropPointerEffects.Copy;
        }

        public void HandleDragCanceled()
        {
        }

        public DragDropPointerEffects HandleDragStarted(DragDropInfo dragDropInfo)
        {
            return DragDropPointerEffects.All;
        }

        public DragDropPointerEffects HandleDraggingOver(DragDropInfo dragDropInfo)
        {
            return DragDropPointerEffects.All;
        }

        public bool IsDropEnabled(DragDropInfo dragDropInfo)
        {
            var fileName = GetFilename(dragDropInfo);

            this.draggedFilename = fileName;

            return File.Exists(fileName) && (fileName.EndsWith(".cs") || fileName.EndsWith(".vb"));
        }

        private static string GetFilename(DragDropInfo info)
        {
            DataObject data = new DataObject(info.Data);

            // The drag and drop operation came from the VS solution explorer
            if (info.Data.GetDataPresent("CF_VSSTGPROJECTITEMS"))
            {
                return data.GetText(); // This is the file path
            }

            return null;
        }
    }
}
