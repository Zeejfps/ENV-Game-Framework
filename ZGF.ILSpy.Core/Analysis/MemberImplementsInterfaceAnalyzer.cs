// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis
{
	/// <summary>
	/// Shows members from all corresponding interfaces the selected member implements.
	/// </summary>
	class MemberImplementsInterfaceAnalyzer : IAnalyzer
	{
		public IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context)
		{
			Debug.Assert(analyzedSymbol is IMember);
			var member = (IMember)analyzedSymbol;

			Debug.Assert(!member.IsStatic);

			var baseMembers = InheritanceHelper.GetBaseMembers(member, includeImplementedInterfaces: true);
			return baseMembers.Where(m => m.DeclaringTypeDefinition?.Kind == TypeKind.Interface);
		}

		public bool Show(ISymbol? symbol)
		{
			switch (symbol?.SymbolKind)
			{
				case SymbolKind.Event:
				case SymbolKind.Indexer:
				case SymbolKind.Method:
				case SymbolKind.Property:
					var member = (IMember)symbol;
					var type = member.DeclaringTypeDefinition;
					return !member.IsStatic && type is not null && (type.Kind == TypeKind.Class || type.Kind == TypeKind.Struct);

				default:
					return false;
			}
		}
	}
}
