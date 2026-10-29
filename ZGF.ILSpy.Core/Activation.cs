using System.Diagnostics;

namespace ZGF.ILSpy.Core;

/// <summary>What activating a node does: open what it stands for, or go where it points, as a reference,
/// a forwarded type, an imported method or a base or derived type does.</summary>
public abstract record Activation
{
    private Activation()
    {
    }

    public sealed record OpenSelf : Activation;

    public sealed record Navigate(NavigationAddress Address) : Activation;

    private static readonly OpenSelf Open = new();

    /// <summary>What activating a node of <paramref name="subject"/> does.</summary>
    public static Activation Of(NodeSubject subject) => subject switch
    {
        NodeSubject.AssemblyReference reference => new Navigate(NavigationAddress.To(CodeReferenceKind.Assembly, reference.Value.Reference.Name,
            reference.Value.Module.FilePath, reference.Value.Reference.Handle)),
        NodeSubject.TypeReference type => new Navigate(NavigationAddress.To(CodeReferenceKind.Type, type.Value.Label, type.Value.Module.FilePath,
            type.Value.Handle)),
        NodeSubject.MemberReference member => new Navigate(NavigationAddress.To(CodeReference.KindOf(member.Value.Declared.SymbolKind),
            member.Value.Declared.Name, member.Value.Module.FilePath, member.Value.Handle)),
        NodeSubject.ForwardedType forwarded => new Navigate(NavigationAddress.To(CodeReferenceKind.Type, forwarded.Value.Label,
            forwarded.Value.Module.FilePath, forwarded.Value.Handle)),
        NodeSubject.ImportedMethod import => new Navigate(import.Value.Shown.Address),
        NodeSubject.DerivedType derived => new Navigate(derived.Value.Address),
        NodeSubject.BaseType baseType => baseType.Value.Target switch
        {
            BaseTypeTarget.Navigable navigable => new Navigate(navigable.Address),
            BaseTypeTarget.Unaddressed or BaseTypeTarget.Unresolved => Open,
            _ => throw new UnreachableException(),
        },
        NodeSubject.TypeDefinition or NodeSubject.MemberDefinition or NodeSubject.References or NodeSubject.ReferencedTypes
            or NodeSubject.ModuleReference or NodeSubject.BaseTypes or NodeSubject.DerivedTypes or NodeSubject.Resources
            or NodeSubject.ResourceContainer or NodeSubject.ResourceEntry or NodeSubject.Metadata or NodeSubject.MetadataTables
            or NodeSubject.MetadataTable or NodeSubject.Package or NodeSubject.PackageFile or NodeSubject.Module or NodeSubject.Namespace
            or NodeSubject.FailedAssembly => Open,
        _ => throw new UnreachableException(),
    };

    /// <summary>The reference a node of <paramref name="subject"/> points at, as documents link to it;
    /// null for one that opens itself.</summary>
    internal static CodeReference? TargetOf(NodeSubject subject)
    {
        if (Of(subject) is not Navigate navigate)
            return null;
        return navigate.Address.ToReference();
    }
}
