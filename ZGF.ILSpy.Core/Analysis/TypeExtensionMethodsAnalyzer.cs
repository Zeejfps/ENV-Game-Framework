// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Diagnostics;

using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis
{
	/// <summary>
	/// Finds all extension methods defined for a type.
	/// </summary>
	class TypeExtensionMethodsAnalyzer : IAnalyzer
	{
		public bool Show(ISymbol? symbol) => symbol is ITypeDefinition entity && !entity.IsStatic;

		public IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context)
		{
			Debug.Assert(analyzedSymbol is ITypeDefinition);
			var scope = context.GetScopeOf((ITypeDefinition)analyzedSymbol);
			foreach (var type in scope.GetTypesInScope(context.CancellationToken))
			{
				foreach (var result in ScanType((ITypeDefinition)analyzedSymbol, type, context))
					yield return result;
			}
		}

		IEnumerable<IEntity> ScanType(ITypeDefinition analyzedType, ITypeDefinition type, AnalyzerContext context)
		{
			if (!type.HasExtensions)
				yield break;

			if (analyzedType.ParentModule?.MetadataFile == null)
				yield break;

			foreach (IMethod method in type.Methods)
			{
				if (!method.IsExtensionMethod)
					continue;

				var firstParamType = method.Parameters[0].Type.GetDefinition();
				if (firstParamType != null &&
					firstParamType.MetadataToken == analyzedType.MetadataToken &&
					firstParamType.ParentModule?.MetadataFile == analyzedType.ParentModule.MetadataFile)
					yield return method;
			}
		}
	}
}
