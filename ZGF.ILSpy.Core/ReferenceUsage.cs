using System.Reflection.Metadata;

namespace ZGF.ILSpy.Core;

/// <summary>What an assembly uses of the assemblies it references: the types it names in each
/// reference and those nested in them, the members of each type it uses, also through the type's
/// generic instances (<c>List&lt;string&gt;.Add</c> counts for <c>List&lt;T&gt;</c>), and the types it
/// forwards to each reference.</summary>
internal sealed class ReferenceUsage
{
    private static readonly List<TypeReferenceHandle> NoTypes = [];
    private static readonly List<MemberReferenceHandle> NoMembers = [];
    private static readonly List<ExportedTypeHandle> NoForwarders = [];

    private readonly Dictionary<EntityHandle, List<TypeReferenceHandle>> _types = new();
    private readonly Dictionary<EntityHandle, List<MemberReferenceHandle>> _members = new();
    private readonly Dictionary<EntityHandle, List<ExportedTypeHandle>> _forwarders = new();

    public ReferenceUsage(MetadataReader metadata)
    {
        foreach (var handle in metadata.TypeReferences)
            Add(_types, metadata.GetTypeReference(handle).ResolutionScope, handle);
        foreach (var handle in metadata.MemberReferences)
            AddMember(metadata, handle);
        foreach (var handle in metadata.ExportedTypes)
            AddForwarder(metadata, handle);
    }

    /// <summary>The types named in <paramref name="scope"/>: an assembly reference, or a type
    /// reference for the types nested in it.</summary>
    public IReadOnlyList<TypeReferenceHandle> TypesIn(EntityHandle scope) => _types.GetValueOrDefault(scope, NoTypes);

    /// <summary>The members used of the type <paramref name="type"/> names.</summary>
    public IReadOnlyList<MemberReferenceHandle> MembersOf(TypeReferenceHandle type) => _members.GetValueOrDefault(type, NoMembers);

    /// <summary>The top-level types forwarded to the assembly <paramref name="reference"/> names.</summary>
    public IReadOnlyList<ExportedTypeHandle> ForwardedTo(AssemblyReferenceHandle reference) => _forwarders.GetValueOrDefault(reference, NoForwarders);

    /// <summary>Whether anything is used of, or forwarded to, the assembly <paramref name="reference"/> names.</summary>
    public bool Uses(AssemblyReferenceHandle reference)
    {
        if (TypesIn(reference).Count > 0)
            return true;
        return ForwardedTo(reference).Count > 0;
    }

    private void AddMember(MetadataReader metadata, MemberReferenceHandle handle)
    {
        var type = FindUsedType(metadata, metadata.GetMemberReference(handle).Parent);
        if (type.IsNil)
            return;
        Add(_members, type, handle);
    }

    /// <summary>The type reference a member's <paramref name="parent"/> names: itself, or the generic
    /// type of a generic instance; nil for anything else.</summary>
    private static EntityHandle FindUsedType(MetadataReader metadata, EntityHandle parent)
    {
        if (parent.Kind == HandleKind.TypeReference)
            return parent;
        if (parent.Kind != HandleKind.TypeSpecification)
            return default;
        return FindGenericType(metadata, (TypeSpecificationHandle)parent);
    }

    private static EntityHandle FindGenericType(MetadataReader metadata, TypeSpecificationHandle handle)
    {
        try
        {
            var reader = metadata.GetBlobReader(metadata.GetTypeSpecification(handle).Signature);
            if (reader.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeInstance)
                return default;
            SkipClassOrValueType(ref reader);
            var type = reader.ReadTypeHandle();
            if (type.Kind != HandleKind.TypeReference)
                return default;
            return type;
        }
        catch (BadImageFormatException)
        {
            return default;
        }
    }

    private static void SkipClassOrValueType(ref BlobReader reader)
    {
        reader.ReadByte();
    }

    private void AddForwarder(MetadataReader metadata, ExportedTypeHandle handle)
    {
        var exported = metadata.GetExportedType(handle);
        if (!ForwardedTypes.IsTopLevelForwarder(exported))
            return;
        Add(_forwarders, exported.Implementation, handle);
    }

    private static void Add<T>(Dictionary<EntityHandle, List<T>> lists, EntityHandle key, T item)
    {
        if (!lists.TryGetValue(key, out var list))
        {
            list = new List<T>();
            lists.Add(key, list);
        }
        list.Add(item);
    }
}
