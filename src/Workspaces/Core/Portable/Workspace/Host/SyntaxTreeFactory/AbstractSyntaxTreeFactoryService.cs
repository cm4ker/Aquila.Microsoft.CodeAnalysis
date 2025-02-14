﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal abstract partial class AbstractSyntaxTreeFactoryService : ISyntaxTreeFactoryService
    {
        // Recoverable trees only save significant memory for larger trees
        internal readonly int MinimumLengthForRecoverableTree;
        private readonly bool _canCreateRecoverableTrees;

        internal HostLanguageServices LanguageServices { get; }

        public AbstractSyntaxTreeFactoryService(
            IGlobalOptionService optionService,
            HostLanguageServices languageServices)
        {
            this.LanguageServices = languageServices;
            this.MinimumLengthForRecoverableTree = languageServices.WorkspaceServices.Workspace.Options.GetOption(CacheOptions.RecoverableTreeLengthThreshold);
            _canCreateRecoverableTrees =
                languageServices.WorkspaceServices.GetService<IProjectCacheHostService>() != null &&
                !optionService.GetOption(WorkspaceConfigurationOptions.DisableRecoverableTrees);
        }

        public abstract ParseOptions GetDefaultParseOptions();
        public abstract SyntaxTree CreateSyntaxTree(string filePath, ParseOptions options, Encoding encoding, SyntaxNode root);
        public abstract SyntaxTree ParseSyntaxTree(string filePath, ParseOptions options, SourceText text, CancellationToken cancellationToken);
        public abstract SyntaxTree CreateRecoverableTree(ProjectId cacheKey, string filePath, ParseOptions options, ValueSource<TextAndVersion> text, Encoding encoding, SyntaxNode root);
        public abstract SyntaxNode DeserializeNodeFrom(Stream stream, CancellationToken cancellationToken);
        public abstract ParseOptions GetDefaultParseOptionsWithLatestLanguageVersion();

        public virtual bool CanCreateRecoverableTree(SyntaxNode root)
            => _canCreateRecoverableTrees && root.FullSpan.Length >= this.MinimumLengthForRecoverableTree;

        protected static SyntaxNode RecoverNode(SyntaxTree tree, TextSpan textSpan, int kind)
        {
            var token = tree.GetRoot().FindToken(textSpan.Start, findInsideTrivia: true);
            var node = token.Parent;

            while (node != null)
            {
                if (node.Span == textSpan && node.RawKind == kind)
                {
                    return node;
                }

                if (node is IStructuredTriviaSyntax structuredTrivia)
                {
                    node = structuredTrivia.ParentTrivia.Token.Parent;
                }
                else
                {
                    node = node.Parent;
                }
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}
