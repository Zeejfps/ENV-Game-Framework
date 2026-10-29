using System.Reflection.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

/// <summary>The types an assembly uses from one of its references, sorted by namespace, then name,
/// and after them the types it forwards there.</summary>
internal sealed class ReferencedTypesNode(LoadedModule assembly, BrowseNode parent, AssemblyReferenceHandle reference)
    : BrowseNode(assembly, parent, NodeKind.ReferencedTypes, "Referenced types"), IModuleNode
{
    public LoadedModule Module { get; } = assembly;

    private protected override bool MayHaveChildren => Module.ReferenceUsage.Uses(reference);

    private protected override BrowseNode[] CreateChildren()
    {
        var usage = Module.ReferenceUsage;
        var metadata = Module.File.Metadata;
        var used = usage.TypesIn(reference)
            .Select(h => new TypeReferenceNode(Module, this, h))
            .OrderBy(n => n.Namespace, NaturalStringComparer.Instance)
            .ThenBy(n => n.Text, NaturalStringComparer.Instance)
            .Cast<BrowseNode>();
        var forwarded = usage.ForwardedTo(reference)
            .Select(h => ForwardedTypes.Read(metadata, h))
            .OrderBy(f => f.Namespace, NaturalStringComparer.Instance)
            .ThenBy(f => f.Label, NaturalStringComparer.Instance)
            .Select(f => new ForwardedTypeNode(Module, this, f));
        return used
            .Concat(forwarded)
            .ToArray();
    }
}

/// <summary>A type an assembly uses from one of its references: named as <c>typeof</c> names an open
/// generic type, holding the types nested in it and the members of it that the assembly uses, and going
/// to where it is defined.</summary>
internal sealed class TypeReferenceNode : BrowseNode, IModuleNode
{
    private readonly TypeReferenceHandle _handle;

    public LoadedModule Module { get; }

    public TypeReferenceNode(LoadedModule assembly, BrowseNode parent, TypeReferenceHandle handle)
        : base(assembly, parent, NodeKind.TypeReference, LabelOf(assembly.File.Metadata, handle))
    {
        Module = assembly;
        _handle = handle;
        var metadata = assembly.File.Metadata;
        Namespace = metadata.GetString(metadata.GetTypeReference(handle).Namespace);
        FullName = QualifiedName(parent, Namespace, Text);
        Target = CodeReference.ToEntity(CodeReferenceKind.Type, Text, assembly.FilePath, handle);
    }

    public override string FullName { get; }

    private static string LabelOf(MetadataReader metadata, TypeReferenceHandle handle)
    {
        var name = metadata.GetString(metadata.GetTypeReference(handle).Name);
        var bare = ReflectionHelper.SplitTypeParameterCountFromReflectionName(name, out var arity);
        return ForwardedTypes.LabelOf(bare, arity);
    }

    /// <summary>The name of a type nested in another after the other's, else after its namespace.</summary>
    private static string QualifiedName(BrowseNode parent, string ns, string label)
    {
        if (parent is TypeReferenceNode declaring)
            return $"{declaring.FullName}.{label}";
        if (ns.Length == 0)
            return label;
        return $"{ns}.{label}";
    }

    private protected override bool MayHaveChildren
    {
        get
        {
            var usage = Module.ReferenceUsage;
            if (usage.TypesIn(_handle).Count > 0)
                return true;
            return usage.MembersOf(_handle).Count > 0;
        }
    }

    private protected override BrowseNode[] CreateChildren()
    {
        var usage = Module.ReferenceUsage;
        var module = Module.TypeSystem.MainModule;
        var nested = usage.TypesIn(_handle)
            .Select(h => new TypeReferenceNode(Module, this, h))
            .OrderBy(n => n.Text, NaturalStringComparer.Instance)
            .Cast<BrowseNode>();
        var members = usage.MembersOf(_handle)
            .Select(h => new MemberReferenceNode(Module, this, h, MemberReferenceNode.Resolve(module, h)))
            .DistinctBy(n => n.Text)
            .OrderBy(n => n.Text, NaturalStringComparer.Instance);
        return nested
            .Concat(members)
            .ToArray();
    }
}

/// <summary>A member an assembly uses of a type from one of its references, labelled as the type
/// declares it, whatever the type arguments it is used with (<c>Add(T) : void</c> for
/// <c>List&lt;string&gt;.Add</c>), an accessor as its property or event, and going to where it is defined.</summary>
internal sealed class MemberReferenceNode : BrowseNode, IModuleNode
{
    public LoadedModule Module { get; }

    public MemberReferenceNode(LoadedModule assembly, BrowseNode parent, MemberReferenceHandle handle, IMember member)
        : base(assembly, parent, KindOf(member), EntityText.Label(member))
    {
        Module = assembly;
        FullName = $"{parent.FullName}.{Text}";
        Target = CodeReference.ToEntity(CodeReference.KindOf(member.SymbolKind), member.Name, assembly.FilePath, handle);
    }

    public override string FullName { get; }

    /// <summary>The member <paramref name="handle"/> uses as its type declares it: for an accessor, the
    /// property or event it belongs to.</summary>
    public static IMember Resolve(MetadataModule module, MemberReferenceHandle handle)
    {
        var declared = ResolveUsed(module, handle).MemberDefinition;
        if (declared is IMethod method && method.AccessorOwner != null)
            return method.AccessorOwner;
        return declared;
    }

    private static IMember ResolveUsed(MetadataModule module, MemberReferenceHandle handle)
    {
        var reference = module.MetadataFile.Metadata.GetMemberReference(handle);
        if (reference.GetKind() == MemberReferenceKind.Field)
            return (IMember)module.ResolveEntity(handle);
        return module.ResolveMethod(handle, default);
    }

    private static NodeKind KindOf(IMember member) => member switch
    {
        IField => NodeKind.FieldReference,
        IProperty => NodeKind.PropertyReference,
        IEvent => NodeKind.EventReference,
        _ => NodeKind.MethodReference,
    };
}
