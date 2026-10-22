// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Diagnostics;

using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis
{
	/// <summary>
	/// Shows the base members overridden by a member; the dual of the "Overridden By" analysis.
	/// </summary>
	class MemberOverridesAnalyzer : IAnalyzer
	{
		public IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context)
		{
			Debug.Assert(analyzedSymbol is IMember);
			var member = (IMember)analyzedSymbol;
			// Walk the override chain member by member instead of collecting all
			// signature-equal base members: this stops at 'new virtual' shadow
			// boundaries, which hide any further base members from overriding.
			var visitedMembers = new HashSet<IMember>();
			while (member.IsOverride)
			{
				member = member.MemberDefinition;
				if (!visitedMembers.Add(member))
				{
					// abort if we seem to be in an infinite loop (cyclic inheritance)
					break;
				}
				var baseMember = InheritanceHelper.GetBaseMember(member);
				if (baseMember == null)
					break;
				yield return baseMember;
				member = baseMember;
			}
		}

		public bool Show(ISymbol? symbol)
		{
			switch (symbol?.SymbolKind)
			{
				case SymbolKind.Event:
				case SymbolKind.Indexer:
				case SymbolKind.Method:
				case SymbolKind.Property:
					return ((IMember)symbol).IsOverride;

				default:
					return false;
			}
		}
	}
}
