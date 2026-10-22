// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis
{
	/// <summary>
	/// Shows properties that implement an interface property.
	/// </summary>
	class PropertyImplementedByAnalyzer : IAnalyzer
	{
		public IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context)
		{
			Debug.Assert(analyzedSymbol is IProperty);
			var scope = context.GetScopeOf((IProperty)analyzedSymbol);
			foreach (var type in scope.GetTypesInScope(context.CancellationToken))
			{
				foreach (var result in AnalyzeType((IProperty)analyzedSymbol, type))
					yield return result;
			}
		}

		IEnumerable<IEntity> AnalyzeType(IProperty analyzedEntity, ITypeDefinition type)
		{
			if (analyzedEntity.DeclaringTypeDefinition?.ParentModule?.MetadataFile == null)
				yield break;
			var token = analyzedEntity.MetadataToken;
			var declaringTypeToken = analyzedEntity.DeclaringTypeDefinition.MetadataToken;
			var module = analyzedEntity.DeclaringTypeDefinition.ParentModule.MetadataFile;
			var allTypes = type.GetAllBaseTypeDefinitions();
			if (!allTypes.Any(t => t.MetadataToken == declaringTypeToken && t.ParentModule?.MetadataFile == module))
				yield break;

			foreach (var property in type.Properties)
			{
				var baseMembers = InheritanceHelper.GetBaseMembers(property, true);
				if (baseMembers.Any(m => m.MetadataToken == token && m.ParentModule?.MetadataFile == module))
					yield return property;
			}
		}

		public bool Show(ISymbol? symbol)
		{
			return symbol is IProperty entity && entity.DeclaringType.Kind == TypeKind.Interface;
		}
	}
}
