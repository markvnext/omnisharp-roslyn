using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using OmniSharp.Extensions;
using OmniSharp.Models;

namespace OmniSharp
{
    public partial class OmnisharpController
    {
        [HttpPost("findsymbols")]
        public async Task<QuickFixResponse> FindSymbols(FindSymbolsRequest request = null)
        {
            Func<string, bool> isMatch =
                candidate => request != null
                ? candidate.IsValidCompletionFor(request.Filter)
                : true;

            return await FindSymbols(isMatch);
        }

        private async Task<QuickFixResponse> FindSymbols(Func<string, bool> predicate)
        {
            var symbols = await SymbolFinder.FindSourceDeclarationsAsync(_workspace.CurrentSolution, predicate, SymbolFilter.TypeAndMember);

            var quickFixes = (from symbol in symbols
                              from location in symbol.Locations
                              select ConvertSymbol(symbol, location)).Distinct();

            return new QuickFixResponse(quickFixes);
        }

        private QuickFix ConvertSymbol(ISymbol symbol, Location location)
        {
            var lineSpan = location.GetLineSpan();
            var path = lineSpan.Path;
            var documents = _workspace.GetDocuments(path);

            var format = SymbolDisplayFormat.MinimallyQualifiedFormat;
            format = format.WithMemberOptions(format.MemberOptions
                                              ^ SymbolDisplayMemberOptions.IncludeContainingType
                                              ^ SymbolDisplayMemberOptions.IncludeType);
            return new SymbolLocation
            {
                Text = symbol.ToDisplayString(format),
                Kind = Enum.GetName(symbol.Kind.GetType(), symbol.Kind),
                FileName = path,
                Line = lineSpan.StartLinePosition.Line + 1,
                Column = lineSpan.StartLinePosition.Character + 1,
                EndLine = lineSpan.EndLinePosition.Line + 1,
                EndColumn = lineSpan.EndLinePosition.Character + 1,
                Projects = documents.Select(document => document.Project.Name).ToArray()
            };
        }
    }
}