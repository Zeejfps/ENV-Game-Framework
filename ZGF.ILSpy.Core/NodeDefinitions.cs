using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace ZGF.ILSpy.Core;

/// <summary>The definition a node of a type or member stands for, as documents and metadata name it.</summary>
public static class NodeDefinitions
{
    /// <summary>Where the type or member a node of <paramref name="subject"/> defines is navigated to;
    /// null for a node that defines none.</summary>
    public static NavigationAddress? AddressOf(NodeSubject subject) => subject switch
    {
        NodeSubject.TypeDefinition type => type.Definition.Address,
        NodeSubject.MemberDefinition member => member.Definition.Address,
        NodeSubject.References or NodeSubject.AssemblyReference or NodeSubject.ReferencedTypes or NodeSubject.TypeReference
            or NodeSubject.MemberReference or NodeSubject.ModuleReference or NodeSubject.ImportedMethod or NodeSubject.ForwardedType
            or NodeSubject.BaseTypes or NodeSubject.BaseType or NodeSubject.DerivedTypes or NodeSubject.DerivedType or NodeSubject.Resources
            or NodeSubject.ResourceContainer or NodeSubject.ResourceEntry or NodeSubject.Metadata or NodeSubject.MetadataTables
            or NodeSubject.MetadataTable or NodeSubject.Package or NodeSubject.PackageFile or NodeSubject.Module or NodeSubject.Namespace
            or NodeSubject.FailedAssembly => null,
        _ => throw new UnreachableException(),
    };

    /// <summary>A reference to the type or member a node of <paramref name="subject"/> defines, as documents
    /// point at it; null for a node that defines none.</summary>
    public static CodeReference? ReferenceOf(NodeSubject subject) => AddressOf(subject)?.ToReference();

    /// <summary>The metadata token of the type or member a node of <paramref name="subject"/> defines;
    /// null for a node that defines none.</summary>
    public static int? TokenOf(NodeSubject subject)
    {
        var address = AddressOf(subject);
        if (address == null)
            return null;
        return MetadataTokens.GetToken(address.Handle);
    }
}
