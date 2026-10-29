using System.Diagnostics;
using ICSharpCode.Decompiler.TypeSystem;
using ZGF.ILSpy.Core.Resources;

namespace ZGF.ILSpy.Core;

/// <summary>How visible what a node stands for is: its declared accessibility; whether it is visible
/// outside its assembly, taking enclosing types into account, as non-public nodes are usually shown
/// dimmed; and whether the compiler made it up rather than the source declaring it, such as a lambda's
/// closure class or a property's backing field, or it sits inside something the compiler made up, as
/// such nodes are usually hidden.</summary>
public readonly record struct NodeVisibility(NodeAccessibility Accessibility, bool IsPublicApi, bool IsCompilerGenerated)
{
    /// <summary>What a node that declares no accessibility of its own shows as: visible, and declared.</summary>
    internal static readonly NodeVisibility Open = new(NodeAccessibility.None, true, false);

    /// <summary>How visible a definition is, compiler-generated when the compiler made it up or what holds it.</summary>
    internal static NodeVisibility OfDefinition(IEntity entity) =>
        new(EntityText.Map(entity.Accessibility), EntityText.IsPublicApi(entity), Members.IsOrSitsInCompilerGenerated(entity));

    /// <summary>How visible a type another node points at is: as declared, and never compiler-generated.</summary>
    internal static NodeVisibility OfReferenced(IEntity entity) => new(EntityText.Map(entity.Accessibility), EntityText.IsPublicApi(entity), false);

    /// <summary>How visible what a node of <paramref name="subject"/> stands for is.</summary>
    public static NodeVisibility Of(NodeSubject subject) => subject switch
    {
        NodeSubject.TypeDefinition type => type.Definition.Visibility,
        NodeSubject.MemberDefinition member => member.Definition.Visibility,
        NodeSubject.ImportedMethod import => import.Value.Shown.Visibility with { IsCompilerGenerated = false },
        NodeSubject.DerivedType derived => derived.Value.Visibility,
        NodeSubject.BaseType baseType => baseType.Value.Visibility,
        NodeSubject.Namespace ns => new NodeVisibility(NodeAccessibility.None, ns.Value.IsPublicApi, ns.Value.IsCompilerGenerated),
        NodeSubject.ResourceContainer container => OfResource(container.Value.Resource),
        NodeSubject.ResourceEntry entry => entry.Value.Source switch
        {
            ResourceSource.Manifest manifest => OfResource(manifest.Resource),
            ResourceSource.Entry or ResourceSource.Baml => Open,
            _ => throw new UnreachableException(),
        },
        NodeSubject.References or NodeSubject.AssemblyReference or NodeSubject.ReferencedTypes or NodeSubject.TypeReference
            or NodeSubject.MemberReference or NodeSubject.ModuleReference or NodeSubject.ForwardedType or NodeSubject.BaseTypes
            or NodeSubject.DerivedTypes or NodeSubject.Resources or NodeSubject.Metadata or NodeSubject.MetadataTables
            or NodeSubject.MetadataTable or NodeSubject.Package or NodeSubject.PackageFile or NodeSubject.Module
            or NodeSubject.FailedAssembly => Open,
        _ => throw new UnreachableException(),
    };

    private static NodeVisibility OfResource(ICSharpCode.Decompiler.Metadata.Resource resource)
    {
        var (isPublic, accessibility) = ResourceNodes.Visibility(resource);
        return new NodeVisibility(accessibility, isPublic, false);
    }
}
