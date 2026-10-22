// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis
{
	/// <summary>
	/// Shows methods that override a method.
	/// </summary>
	class MethodOverriddenByAnalyzer : IAnalyzer
	{
		const GetMemberOptions Options = GetMemberOptions.IgnoreInheritedMembers | GetMemberOptions.ReturnMemberDefinitions;

		public IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context)
		{
			Debug.Assert(analyzedSymbol is IMethod);
			var scope = context.GetScopeOf((IEntity)analyzedSymbol);
			foreach (var type in scope.GetTypesInScope(context.CancellationToken))
			{
				foreach (var result in AnalyzeType((IMethod)analyzedSymbol, type))
					yield return result;
			}
		}

		IEnumerable<IEntity> AnalyzeType(IMethod analyzedEntity, ITypeDefinition type)
		{
			if (analyzedEntity.DeclaringTypeDefinition?.ParentModule?.MetadataFile == null)
				yield break;
			var token = analyzedEntity.MetadataToken;
			var declaringTypeToken = analyzedEntity.DeclaringTypeDefinition.MetadataToken;
			var module = analyzedEntity.DeclaringTypeDefinition.ParentModule.MetadataFile;
			var allTypes = type.GetAllBaseTypeDefinitions();
			if (!allTypes.Any(t => t.MetadataToken == declaringTypeToken && t.ParentModule?.MetadataFile == module))
				yield break;

			foreach (var method in type.Methods)
			{
				if (!method.IsOverride)
					continue;
				var baseMembers = InheritanceHelper.GetBaseMembers(method, false);
				if (baseMembers.Any(p => p.MetadataToken == token && p.ParentModule?.MetadataFile == module))
				{
					yield return method;
				}
			}
		}

		public bool Show(ISymbol? entity)
		{
			return entity is IMethod method && method.IsOverridable && method.DeclaringType.Kind != TypeKind.Interface;
		}
	}
}
