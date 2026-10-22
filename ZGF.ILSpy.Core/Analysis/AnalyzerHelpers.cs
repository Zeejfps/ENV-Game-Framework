// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Reflection.Metadata;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis;

internal static class AnalyzerHelpers
{
    /// <summary>Whether both name the same definition: the same token in the same file.</summary>
    public static bool IsSame(this IEntity entity, IEntity other) =>
        entity.MetadataToken == other.MetadataToken && entity.ParentModule?.MetadataFile == other.ParentModule?.MetadataFile;

    /// <summary>The body of method <paramref name="handle"/>; null when it has none or it cannot be read.</summary>
    public static MethodBodyBlock? ReadBody(this MetadataFile module, MethodDefinitionHandle handle)
    {
        var definition = module.Metadata.GetMethodDefinition(handle);
        if (!definition.HasBody())
            return null;
        try
        {
            return module.GetMethodBody(definition.RelativeVirtualAddress);
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }

    /// <summary>The instructions of <paramref name="body"/> with a metadata token operand, up to the
    /// first that cannot be decoded.</summary>
    public static IEnumerable<(ILOpCode OpCode, EntityHandle Token)> Tokens(this MethodBodyBlock body)
    {
        var blob = body.GetILReader();
        while (blob.RemainingBytes > 0)
        {
            if (!TryReadToken(ref blob, out var opCode, out var token))
                yield break;
            if (!token.IsNil)
                yield return (opCode, token);
        }
    }

    /// <summary>Whether <paramref name="match"/> accepts a token operand of <paramref name="body"/>;
    /// a token it cannot resolve is not a match.</summary>
    public static bool AnyToken(this MethodBodyBlock? body, Func<ILOpCode, EntityHandle, bool> match) =>
        body != null && body.Tokens().Any(t => Matches(match, t.OpCode, t.Token));

    private static bool Matches(Func<ILOpCode, EntityHandle, bool> match, ILOpCode opCode, EntityHandle token)
    {
        try
        {
            return match(opCode, token);
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }

    private static bool TryReadToken(ref BlobReader blob, out ILOpCode opCode, out EntityHandle token)
    {
        token = default;
        try
        {
            opCode = blob.DecodeOpCode();
            if (opCode.GetOperandType() is OperandType.Field or OperandType.Method or OperandType.Sig or OperandType.Tok or OperandType.Type)
                token = MetadataTokenHelpers.EntityHandleOrNil(blob.ReadInt32());
            else
                blob.SkipOperand(opCode);
            return true;
        }
        catch (BadImageFormatException)
        {
            opCode = default;
            return false;
        }
    }

    public static bool IsPossibleReferenceTo(EntityHandle member, MetadataFile module, IMethod analyzedMethod)
    {
        if (member.IsNil)
            return false;
        var metadata = module.Metadata;
        switch (member.Kind)
        {
            case HandleKind.MethodDefinition:
                return member == analyzedMethod.MetadataToken && module == analyzedMethod.ParentModule?.MetadataFile;
            case HandleKind.MemberReference:
                var mr = metadata.GetMemberReference((MemberReferenceHandle)member);
                return mr.GetKind() == MemberReferenceKind.Method && metadata.StringComparer.Equals(mr.Name, analyzedMethod.Name);
            case HandleKind.MethodSpecification:
                var ms = metadata.GetMethodSpecification((MethodSpecificationHandle)member);
                return IsPossibleReferenceTo(ms.Method, module, analyzedMethod);
            default:
                return false;
        }
    }

    public static bool IsCustomAttributeOfType(EntityHandle customAttributeCtor, MetadataReader metadata, FindTypeDecoder decoder) =>
        decoder.GetTypeFromEntity(metadata, customAttributeCtor.GetDeclaringType(metadata));

    /// <summary>What carries each attribute of <paramref name="ts"/>'s module that
    /// <paramref name="matches"/> accepts, in attribute order, then the method, or the property or
    /// event it is an accessor of, for each parameter that carries one.</summary>
    public static IEnumerable<ISymbol> AttributeOwners(DecompilerTypeSystem ts, Func<CustomAttribute, bool> matches)
    {
        var metadata = ts.MainModule.MetadataFile.Metadata;
        var parameters = new HashSet<ParameterHandle>();
        foreach (var h in metadata.CustomAttributes)
        {
            var attribute = metadata.GetCustomAttribute(h);
            if (!matches(attribute))
                continue;
            if (attribute.Parent.Kind == HandleKind.Parameter)
                parameters.Add((ParameterHandle)attribute.Parent);
            else if (GetParentEntity(ts, attribute) is { } parent)
                yield return parent;
        }
        if (parameters.Count == 0)
            yield break;
        foreach (var h in metadata.MethodDefinitions)
        {
            if (!metadata.GetMethodDefinition(h).GetParameters().Any(parameters.Contains))
                continue;
            if (ts.MainModule.ResolveMethod(h, default) is { } method)
                yield return method.IsAccessor ? method.AccessorOwner : method;
        }
    }

    private static ISymbol? GetParentEntity(DecompilerTypeSystem ts, CustomAttribute customAttribute)
    {
        var metadata = ts.MainModule.MetadataFile.Metadata;
        switch (customAttribute.Parent.Kind)
        {
            case HandleKind.MethodDefinition:
                var parent = (IMethod)ts.MainModule.ResolveEntity(customAttribute.Parent);
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
