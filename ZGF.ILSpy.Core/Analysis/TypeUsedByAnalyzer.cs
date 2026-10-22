// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Immutable;
using System.Reflection.Metadata;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis;

/// <summary>Shows the symbols outside a type whose metadata or code uses it.</summary>
internal sealed class TypeUsedByAnalyzer : IAnalyzer
{
    public bool Show(ISymbol? symbol) => symbol is ITypeDefinition;

    public IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context)
    {
        var analyzedType = (ITypeDefinition)analyzedSymbol;
        var scope = context.GetScopeOf(analyzedType);
        return scope.GetModulesInScope(context.CancellationToken)
            .AsParallel().AsOrdered()
            .SelectMany(module => AnalyzeModule(analyzedType, scope.ConstructTypeSystem(module))
                .Distinct()
                .Where(s => !analyzedType.Equals((s as IEntity)?.DeclaringTypeDefinition)));
    }

    private static IEnumerable<ISymbol> AnalyzeModule(ITypeDefinition analyzedType, DecompilerTypeSystem typeSystem)
    {
        var module = typeSystem.MainModule;
        var metadata = module.MetadataFile.Metadata;
        var decoder = new FindTypeDecoder(module, analyzedType);
        var attributeDecoder = new FindTypeInAttributeDecoder(module, analyzedType);

        foreach (var result in AnalyzerHelpers.AttributeOwners(typeSystem, UsesType))
            yield return result;

        foreach (var h in metadata.TypeDefinitions)
        {
            var td = metadata.GetTypeDefinition(h);
            var found = decoder.GetTypeFromEntity(metadata, td.GetBaseTypeOrNil());
            foreach (var ih in td.GetInterfaceImplementations())
                found |= decoder.GetTypeFromEntity(metadata, metadata.GetInterfaceImplementation(ih).Interface);
            found |= FindUsesInGenericConstraints(metadata, td.GetGenericParameters(), decoder);
            if (found)
                yield return module.GetDefinition(h);
        }

        foreach (var h in metadata.MethodDefinitions)
        {
            var md = metadata.GetMethodDefinition(h);
            var found = FindTypeDecoder.AnyInMethodSignature(md.DecodeSignature(decoder, default));
            found |= FindUsesInGenericConstraints(metadata, md.GetGenericParameters(), decoder);
            if (found || ScanMethodBody(module.MetadataFile, h, decoder))
            {
                var method = module.GetDefinition(h);
                yield return method.AccessorOwner ?? method;
            }
        }

        foreach (var h in metadata.FieldDefinitions)
        {
            if (metadata.GetFieldDefinition(h).DecodeSignature(decoder, default))
                yield return module.GetDefinition(h);
        }

        foreach (var h in metadata.PropertyDefinitions)
        {
            if (FindTypeDecoder.AnyInMethodSignature(metadata.GetPropertyDefinition(h).DecodeSignature(decoder, default)))
                yield return module.GetDefinition(h);
        }

        foreach (var h in metadata.EventDefinitions)
        {
            if (decoder.GetTypeFromEntity(metadata, metadata.GetEventDefinition(h).Type))
                yield return module.GetDefinition(h);
        }

        bool UsesType(CustomAttribute attribute)
        {
            CustomAttributeValue<TokenSearchResult> value;
            try
            {
                value = attribute.DecodeValue(attributeDecoder);
            }
            catch (EnumUnderlyingTypeResolveException)
            {
                return false;
            }
            return AnalyzerHelpers.IsCustomAttributeOfType(attribute.Constructor, metadata, decoder)
                || value.FixedArguments.Any(a => Found(a.Value)) || value.NamedArguments.Any(a => Found(a.Value));
        }
    }

    private static bool Found(object? value) =>
        value switch
        {
            TokenSearchResult result => (result & TokenSearchResult.Found) != 0,
            ImmutableArray<CustomAttributeTypedArgument<IType>> array => array.Any(element => Found(element.Value)),
            _ => false,
        };

    private static bool FindUsesInGenericConstraints(MetadataReader metadata, GenericParameterHandleCollection parameters, FindTypeDecoder decoder) =>
        parameters.Any(h => metadata.GetGenericParameter(h).GetConstraints()
            .Any(c => decoder.GetTypeFromEntity(metadata, metadata.GetGenericParameterConstraint(c).Type)));

    private static bool ScanMethodBody(MetadataFile module, MethodDefinitionHandle handle, FindTypeDecoder decoder)
    {
        if (module.ReadBody(handle) is not { } body)
            return false;
        var metadata = module.Metadata;
        if (!body.LocalSignature.IsNil)
        {
            try
            {
                if (UsedBySignature(metadata.GetStandaloneSignature(body.LocalSignature)))
                    return true;
            }
            catch (BadImageFormatException)
            {
            }
        }
        return body.AnyToken((_, token) => UsedByMember(token));

        bool UsedByMember(EntityHandle member)
        {
            if (member.IsNil)
                return false;
            switch (member.Kind)
            {
                case HandleKind.TypeReference:
                    return decoder.GetTypeFromReference(metadata, (TypeReferenceHandle)member, 0);
                case HandleKind.TypeSpecification:
                    return decoder.GetTypeFromSpecification(metadata, default, (TypeSpecificationHandle)member, 0);
                case HandleKind.TypeDefinition:
                    return decoder.GetTypeFromDefinition(metadata, (TypeDefinitionHandle)member, 0);
                case HandleKind.FieldDefinition:
                    var fd = metadata.GetFieldDefinition((FieldDefinitionHandle)member);
                    return UsedByMember(fd.GetDeclaringType()) || fd.DecodeSignature(decoder, default);
                case HandleKind.MethodDefinition:
                    var md = metadata.GetMethodDefinition((MethodDefinitionHandle)member);
                    return UsedByMember(md.GetDeclaringType()) || FindTypeDecoder.AnyInMethodSignature(md.DecodeSignature(decoder, default));
                case HandleKind.MemberReference:
                    var mr = metadata.GetMemberReference((MemberReferenceHandle)member);
                    return UsedByMember(mr.Parent) || mr.GetKind() switch
                    {
                        MemberReferenceKind.Method => FindTypeDecoder.AnyInMethodSignature(mr.DecodeMethodSignature(decoder, default)),
                        MemberReferenceKind.Field => mr.DecodeFieldSignature(decoder, default),
                        _ => false,
                    };
                case HandleKind.MethodSpecification:
                    var ms = metadata.GetMethodSpecification((MethodSpecificationHandle)member);
                    return UsedByMember(ms.Method) || ms.DecodeSignature(decoder, default).Any(static t => t);
                case HandleKind.StandaloneSignature:
                    return UsedBySignature(metadata.GetStandaloneSignature((StandaloneSignatureHandle)member));
                default:
                    return false;
            }
        }

        bool UsedBySignature(StandaloneSignature signature) =>
            signature.GetKind() switch
            {
                StandaloneSignatureKind.Method => FindTypeDecoder.AnyInMethodSignature(signature.DecodeMethodSignature(decoder, default)),
                StandaloneSignatureKind.LocalVariables => signature.DecodeLocalSignature(decoder, default).Any(static t => t),
                _ => false,
            };
    }
}
