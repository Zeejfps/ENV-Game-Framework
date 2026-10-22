// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Diagnostics;

using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis
{
	/// <summary>
	/// Finds all entities that expose a type.
	/// </summary>
	class TypeExposedByAnalyzer : IAnalyzer
	{
		public bool Show(ISymbol? entity) => entity is ITypeDefinition;

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
			if (analyzedType.Kind == TypeKind.Enum
				&& type.MetadataToken == analyzedType.MetadataToken
				&& type.ParentModule?.MetadataFile == analyzedType.ParentModule?.MetadataFile)
				yield break;

			if (!context.Language.ShowMember(type))
				yield break;

			var visitor = new TypeDefinitionUsedVisitor(analyzedType, true);

			foreach (IField field in type.Fields)
			{
				if (TypeIsExposedBy(visitor, field))
					yield return field;
			}

			foreach (IProperty property in type.Properties)
			{
				if (TypeIsExposedBy(visitor, property))
					yield return property;
			}

			foreach (IEvent @event in type.Events)
			{
				if (TypeIsExposedBy(visitor, @event))
					yield return @event;
			}

			foreach (IMethod method in type.Methods)
			{
				if (TypeIsExposedBy(visitor, method))
					yield return method;
			}
		}

		bool TypeIsExposedBy(TypeDefinitionUsedVisitor visitor, IField field)
		{
			if (field.Accessibility == Accessibility.Private)
				return false;

			visitor.Found = false;
			field.ReturnType.AcceptVisitor(visitor);

			return visitor.Found;
		}

		bool TypeIsExposedBy(TypeDefinitionUsedVisitor visitor, IProperty property)
		{
			if (property.Accessibility == Accessibility.Private)
			{
				if (!property.IsExplicitInterfaceImplementation)
					return false;
			}

			visitor.Found = false;
			property.ReturnType.AcceptVisitor(visitor);

			foreach (var p in property.Parameters)
			{
				p.Type.AcceptVisitor(visitor);
			}

			return visitor.Found;
		}

		bool TypeIsExposedBy(TypeDefinitionUsedVisitor visitor, IEvent @event)
		{
			if (@event.Accessibility == Accessibility.Private)
			{
				if (!@event.IsExplicitInterfaceImplementation)
					return false;
			}

			visitor.Found = false;
			@event.ReturnType.AcceptVisitor(visitor);

			return visitor.Found;
		}

		bool TypeIsExposedBy(TypeDefinitionUsedVisitor visitor, IMethod method)
		{
			if (method.Accessibility == Accessibility.Private)
			{
				if (!method.IsExplicitInterfaceImplementation)
					return false;
			}

			visitor.Found = false;
			method.ReturnType.AcceptVisitor(visitor);

			foreach (var p in method.Parameters)
			{
				p.Type.AcceptVisitor(visitor);
			}

			return visitor.Found;
		}
	}
}