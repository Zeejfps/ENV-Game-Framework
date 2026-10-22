// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis
{
	/// <summary>
	/// Shows properties that override a property.
	/// </summary>
	class PropertyOverriddenByAnalyzer : IAnalyzer
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
				if (!property.IsOverride)
					continue;
				var baseMembers = InheritanceHelper.GetBaseMembers(property, false);
				if (baseMembers.Any(p => p.MetadataToken == token && p.ParentModule?.MetadataFile == module))
				{
					yield return property;
				}
			}
		}

		public bool Show(ISymbol? entity)
		{
			return entity is IProperty property && property.IsOverridable && property.DeclaringType.Kind != TypeKind.Interface;
		}
	}
}
