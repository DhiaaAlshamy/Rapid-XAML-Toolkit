﻿// Copyright (c) Matt Lacey Ltd. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using static Microsoft.VisualStudio.VSConstants;

namespace RapidXaml.Common
{
    [ProvideAutoLoad(UICONTEXT.CSharpProject_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UICONTEXT.VBProject_string, PackageAutoLoadFlags.BackgroundLoad)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuidString)]
    public sealed class RapidXamlCommonPackage : AsyncPackage
    {
        public const string PackageGuidString = "b6d30102-8ee9-4cdd-befe-6c8a0ae7a5ac";
    }
}
