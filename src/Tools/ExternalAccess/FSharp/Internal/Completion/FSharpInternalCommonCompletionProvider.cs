﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Completion
{
    internal sealed class FSharpInternalCommonCompletionProvider : CommonCompletionProvider
    {
#pragma warning disable CS0612 // Switch to FSharpCommonCompletionProviderBase after F# switches
        private readonly IFSharpCommonCompletionProvider _provider;

        public FSharpInternalCommonCompletionProvider(IFSharpCommonCompletionProvider provider)
        {
            _provider = provider;
        }
#pragma warning restore CS0612 // Type or member is obsolete

        public override Task ProvideCompletionsAsync(CompletionContext context)
        {
            return _provider.ProvideCompletionsAsync(context);
        }

        protected override Task<TextChange?> GetTextChangeAsync(CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            return _provider.GetTextChangeAsync(base.GetTextChangeAsync, selectedItem, ch, cancellationToken);
        }

        public override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
        {
            return _provider.IsInsertionTrigger(text, insertedCharacterPosition, options);
        }
    }
}
