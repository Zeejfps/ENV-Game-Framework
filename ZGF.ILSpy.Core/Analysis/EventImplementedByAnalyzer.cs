// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis
{
	/// <summary>
	/// Shows events that implement an interface event.
	/// </summary>
	class EventImplementedByAnalyzer : IAnalyzer
	{
		public IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context)
		{
			Debug.Assert(analyzedSymbol is IEvent);
			var scope = context.GetScopeOf((IEvent)analyzedSymbol);
			foreach (var type in scope.GetTypesInScope(context.CancellationToken))
			{
				foreach (var result in AnalyzeType((IEvent)analyzedSymbol, type))
					yield return result;
			}
		}

		IEnumerable<IEntity> AnalyzeType(IEvent analyzedEntity, ITypeDefinition type)
		{
			if (analyzedEntity.DeclaringTypeDefinition?.ParentModule?.MetadataFile == null)
				yield break;
			var token = analyzedEntity.MetadataToken;
			var declaringTypeToken = analyzedEntity.DeclaringTypeDefinition.MetadataToken;
			var module = analyzedEntity.DeclaringTypeDefinition.ParentModule.MetadataFile;
			var allTypes = type.GetAllBaseTypeDefinitions();
			if (!allTypes.Any(t => t.MetadataToken == declaringTypeToken && t.ParentModule?.MetadataFile == module))
				yield break;

			foreach (var @event in type.Events)
			{
				var baseMembers = InheritanceHelper.GetBaseMembers(@event, true);
				if (baseMembers.Any(m => m.MetadataToken == token && m.ParentModule?.MetadataFile == module))
					yield return @event;
			}
		}

		public bool Show(ISymbol? symbol)
		{
			return symbol is IEvent entity && entity.DeclaringType.Kind == TypeKind.Interface;
		}
	}
}
