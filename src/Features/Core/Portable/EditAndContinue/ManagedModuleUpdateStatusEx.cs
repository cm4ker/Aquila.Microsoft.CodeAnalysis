﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    // Temporary values in main branch - we do not sistinguish between RudeEdit and Blocked.
    // To be replaced with actual values from Debugger.Contracts in vs-deps branch.
    internal static class ManagedModuleUpdateStatusEx
    {
        public const ManagedModuleUpdateStatus RestartRequired = (ManagedModuleUpdateStatus)2;
        public const ManagedModuleUpdateStatus Blocked = (ManagedModuleUpdateStatus)3;
    }
}
