// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;

using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis
{
	/// <summary>
	/// Shows methods that instantiate a type.
	/// </summary>
	class TypeInstantiatedByAnalyzer : IAnalyzer
	{
		const GetMemberOptions Options = GetMemberOptions.IgnoreInheritedMembers | GetMemberOptions.ReturnMemberDefinitions;

		public IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context)
		{
			Debug.Assert(analyzedSymbol is ITypeDefinition);
			var scope = context.GetScopeOf((ITypeDefinition)analyzedSymbol);
			foreach (var type in scope.GetTypesInScope(context.CancellationToken))
			{
				if (type.ParentModule?.MetadataFile == null)
					continue;
				var mappingInfo = context.Language.GetCodeMappingInfo(type.ParentModule.MetadataFile, type.MetadataToken);
				var methods = type.GetMembers(m => m is IMethod, Options).OfType<IMethod>();
				foreach (var method in methods)
				{
					if (IsUsedInMethod((ITypeDefinition)analyzedSymbol, method, mappingInfo, context))
						yield return method;
				}

				foreach (var property in type.Properties)
				{
					if (property.CanGet && IsUsedInMethod((ITypeDefinition)analyzedSymbol, property.Getter, mappingInfo, context))
					{
						yield return property;
						continue;
					}
					if (property.CanSet && IsUsedInMethod((ITypeDefinition)analyzedSymbol, property.Setter, mappingInfo, context))
					{
						yield return property;
						continue;
					}
				}

				foreach (var @event in type.Events)
				{
					if (@event.CanAdd && IsUsedInMethod((ITypeDefinition)analyzedSymbol, @event.AddAccessor, mappingInfo, context))
					{
						yield return @event;
						continue;
					}
					if (@event.CanRemove && IsUsedInMethod((ITypeDefinition)analyzedSymbol, @event.RemoveAccessor, mappingInfo, context))
					{
						yield return @event;
						continue;
					}
					if (@event.CanInvoke && IsUsedInMethod((ITypeDefinition)analyzedSymbol, @event.InvokeAccessor, mappingInfo, context))
					{
						yield return @event;
						continue;
					}
				}
			}
		}

		bool IsUsedInMethod(ITypeDefinition analyzedEntity, IMethod method, CodeMappingInfo mappingInfo, AnalyzerContext context)
		{
			return ScanMethodBody(analyzedEntity, method, context.GetMethodBody(method));
		}

		bool ScanMethodBody(ITypeDefinition analyzedEntity, IMethod method, MethodBodyBlock? methodBody)
		{
			if (methodBody == null || method.ParentModule?.MetadataFile == null)
				return false;
			var blob = methodBody.GetILReader();
			var module = (MetadataModule)method.ParentModule;
			var genericContext = new ICSharpCode.Decompiler.TypeSystem.GenericContext(); // type parameters don't matter for this analyzer

			while (blob.RemainingBytes > 0)
			{
				ILOpCode opCode;
				EntityHandle handle;
				try
				{
					opCode = blob.DecodeOpCode();
					switch (opCode)
					{
						case ILOpCode.Newobj:
						case ILOpCode.Call when analyzedEntity.Kind == TypeKind.Struct:
						case ILOpCode.Initobj:
							handle = MetadataTokenHelpers.EntityHandleOrNil(blob.ReadInt32());
							break;
						default:
							blob.SkipOperand(opCode);
							continue;
					}
				}
				catch (BadImageFormatException)
				{
					continue;
				}
				ITypeDefinition? foundTypeDefinition;
				try
				{
					switch (handle.Kind)
					{
						case HandleKind.MethodDefinition:
						case HandleKind.MemberReference:
							var ctor = module.ResolveMethod(handle, genericContext);
							if (ctor == null || !ctor.IsConstructor)
							{
								continue;
							}
							foundTypeDefinition = ctor.DeclaringTypeDefinition;
							break;
						case HandleKind.TypeDefinition:
						case HandleKind.TypeReference:
							foundTypeDefinition = module.ResolveType(handle, genericContext)?.GetDefinition();
							break;
						default:
							continue;
					}
				}
				catch (BadImageFormatException)
				{
					continue;
				}
				if (foundTypeDefinition?.ParentModule == null)
				{
					continue;
				}

				if (foundTypeDefinition.MetadataToken == analyzedEntity.MetadataToken
					&& foundTypeDefinition.ParentModule.MetadataFile == analyzedEntity.ParentModule!.MetadataFile)
				{
					return true;
				}
			}

			return false;
		}

		public bool Show(ISymbol? symbol) => symbol is ITypeDefinition entity && !entity.IsAbstract && !entity.IsStatic;
	}
}
