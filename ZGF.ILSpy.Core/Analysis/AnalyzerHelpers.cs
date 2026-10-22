// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Reflection.Metadata;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis
{
	internal static class AnalyzerHelpers
	{
		public static bool IsPossibleReferenceTo(EntityHandle member, MetadataFile module, IMethod analyzedMethod)
		{
			if (member.IsNil)
				return false;
			MetadataReader metadata = module.Metadata;
			switch (member.Kind)
			{
				case HandleKind.MethodDefinition:
					return member == analyzedMethod.MetadataToken
						&& module == analyzedMethod.ParentModule?.MetadataFile;
				case HandleKind.MemberReference:
					var mr = metadata.GetMemberReference((MemberReferenceHandle)member);
					if (mr.GetKind() != MemberReferenceKind.Method)
						return false;
					return metadata.StringComparer.Equals(mr.Name, analyzedMethod.Name);
				case HandleKind.MethodSpecification:
					var ms = metadata.GetMethodSpecification((MethodSpecificationHandle)member);
					return IsPossibleReferenceTo(ms.Method, module, analyzedMethod);
				default:
					return false;
			}
		}

		public static ISymbol? GetParentEntity(DecompilerTypeSystem ts, CustomAttribute customAttribute)
		{
			var metadata = ts.MainModule.MetadataFile.Metadata;
			switch (customAttribute.Parent.Kind)
			{
				case HandleKind.MethodDefinition:
					IMethod parent = (IMethod)ts.MainModule.ResolveEntity(customAttribute.Parent);
					return parent?.AccessorOwner ?? parent;
				case HandleKind.FieldDefinition:
				case HandleKind.PropertyDefinition:
				case HandleKind.EventDefinition:
				case HandleKind.TypeDefinition:
					return ts.MainModule.ResolveEntity(customAttribute.Parent);
				case HandleKind.AssemblyDefinition:
				case HandleKind.ModuleDefinition:
					return ts.MainModule;
				case HandleKind.GenericParameterConstraint:
					var gpc = metadata.GetGenericParameterConstraint((GenericParameterConstraintHandle)customAttribute.Parent);
					var gp = metadata.GetGenericParameter(gpc.Parameter);
					return ts.MainModule.ResolveEntity(gp.Parent);
				case HandleKind.GenericParameter:
					gp = metadata.GetGenericParameter((GenericParameterHandle)customAttribute.Parent);
					return ts.MainModule.ResolveEntity(gp.Parent);
				default:
					return null;
			}
		}
	}
}
