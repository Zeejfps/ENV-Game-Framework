// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis
{
	/// <summary>
	/// Shows entities that are used by a method.
	/// </summary>
	class MethodUsesAnalyzer : IAnalyzer
	{
		public bool Show(ISymbol? symbol) => symbol is IMethod method && method.HasBody;

		public IEnumerable<ISymbol> Analyze(ISymbol symbol, AnalyzerContext context)
		{
			if (symbol is IMethod method && method.ParentModule?.MetadataFile is MetadataFile corFile)
			{
				var typeSystem = context.GetOrCreateTypeSystem(corFile);
				return context.Language.GetCodeMappingInfo(corFile, method.MetadataToken)
					.GetMethodParts((MethodDefinitionHandle)method.MetadataToken)
					.SelectMany(h => ScanMethod(h, typeSystem)).Distinct();
			}
			throw new InvalidOperationException("Should never happen.");
		}

		IEnumerable<IEntity> ScanMethod(MethodDefinitionHandle handle, DecompilerTypeSystem typeSystem)
		{
			var module = typeSystem.MainModule;
			var md = module.MetadataFile.Metadata.GetMethodDefinition(handle);
			if (!md.HasBody())
				yield break;

			BlobReader blob;
			try
			{
				blob = module.MetadataFile.GetMethodBody(md.RelativeVirtualAddress).GetILReader();
			}
			catch (BadImageFormatException)
			{
				yield break;
			}
			var visitor = new TypeDefinitionCollector();
			var genericContext = new ICSharpCode.Decompiler.TypeSystem.GenericContext(); // type parameters don't matter for this analyzer

			while (blob.RemainingBytes > 0)
			{
				ILOpCode opCode;
				try
				{
					opCode = blob.DecodeOpCode();
				}
				catch (BadImageFormatException)
				{
					yield break;
				}
				switch (opCode.GetOperandType())
				{
					case OperandType.Field:
					case OperandType.Method:
					case OperandType.Sig:
					case OperandType.Tok:
						var member = MetadataTokenHelpers.EntityHandleOrNil(blob.ReadInt32());
						if (member.IsNil)
							continue;

						switch (member.Kind)
						{
							case HandleKind.StandaloneSignature:
								break;
							case HandleKind.TypeDefinition:
							case HandleKind.TypeReference:
							case HandleKind.TypeSpecification:
								IType? ty;
								try
								{
									ty = module.ResolveType(member, genericContext);
								}
								catch (BadImageFormatException)
								{
									ty = null;
								}
								ty?.AcceptVisitor(visitor);
								break;
							case HandleKind.MethodDefinition:
							case HandleKind.MethodSpecification:
							case HandleKind.MemberReference:
							case HandleKind.FieldDefinition:
								IEntity? m;
								try
								{
									m = module.ResolveEntity(member, genericContext);
								}
								catch (BadImageFormatException)
								{
									m = null;
								}
								if (m != null)
									yield return m;
								break;
						}
						break;
					default:
						try
						{
							ILParser.SkipOperand(ref blob, opCode);
						}
						catch (BadImageFormatException)
						{
							yield break;
						}
						break;
				}
			}

			foreach (var type in visitor.UsedTypes)
			{
				yield return type;
			}
		}

		class TypeDefinitionCollector : TypeVisitor
		{
			public readonly List<ITypeDefinition> UsedTypes = new List<ITypeDefinition>();

			public override IType VisitTypeDefinition(ITypeDefinition type)
			{
				UsedTypes.Add(type);
				return base.VisitTypeDefinition(type);
			}
		}
	}
}
