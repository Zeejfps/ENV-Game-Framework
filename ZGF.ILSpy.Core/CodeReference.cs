using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

/// <summary>What a <see cref="CodeReference"/> points at.</summary>
public enum CodeReferenceKind
{
    Assembly,
    Type,
    Method,
    Field,
    Property,
    Event,
    Local,
    Label,
}

/// <summary>
/// An immutable pointer from code or the tree to an assembly, entity, local or label. References to
/// the same target compare equal, so every occurrence of a symbol in a document can be matched.
/// Assembly and entity references can be resolved to a tree node with
/// <see cref="AssemblyList.FindNode(CodeReference)"/>.
/// </summary>
public sealed class CodeReference : IEquatable<CodeReference>
{
    private readonly int _localKey;
    private readonly CodeReference? _scope;

    private CodeReference(CodeReferenceKind kind, string name, string? filePath, EntityHandle handle, int localKey, CodeReference? scope)
    {
        Kind = kind;
        Name = name;
        FilePath = filePath;
        Handle = handle;
        _localKey = localKey;
        _scope = scope;
    }

    public CodeReferenceKind Kind { get; }

    /// <summary>The target's name as written where it was referenced.</summary>
    public string Name { get; }

    /// <summary>Whether <see cref="AssemblyList.FindNode(CodeReference)"/> can resolve this to a node:
    /// true for assemblies and entities, false for locals and labels.</summary>
    public bool IsNavigable => FilePath != null;

    internal string? FilePath { get; }
    internal EntityHandle Handle { get; }

    internal static CodeReference ToEntity(CodeReferenceKind kind, string name, string filePath, EntityHandle handle) =>
        new(kind, name, filePath, handle, 0, null);

    /// <summary>A reference to the definition <paramref name="entity"/>, named <paramref name="name"/>
    /// or else by its own name; null when it has no metadata file or token.</summary>
    internal static CodeReference? To(IEntity entity, string? name = null)
    {
        var file = entity.ParentModule?.MetadataFile;
        if (file == null || entity.MetadataToken.IsNil)
            return null;
        return ToEntity(KindOf(entity.SymbolKind), name ?? entity.Name, file.FileName, entity.MetadataToken);
    }

    /// <summary>What a reference to <paramref name="handle"/> of <paramref name="metadata"/> points
    /// at, or null for a handle no reference is made to.</summary>
    internal static CodeReferenceKind? KindOf(MetadataReader metadata, Handle handle) =>
        handle.Kind switch
        {
            HandleKind.AssemblyDefinition or HandleKind.AssemblyReference => CodeReferenceKind.Assembly,
            HandleKind.TypeDefinition or HandleKind.TypeReference or HandleKind.TypeSpecification => CodeReferenceKind.Type,
            HandleKind.MethodDefinition or HandleKind.MethodSpecification => CodeReferenceKind.Method,
            HandleKind.FieldDefinition => CodeReferenceKind.Field,
            HandleKind.PropertyDefinition => CodeReferenceKind.Property,
            HandleKind.EventDefinition => CodeReferenceKind.Event,
            HandleKind.MemberReference => metadata.GetMemberReference((MemberReferenceHandle)handle).GetKind() == MemberReferenceKind.Field
                ? CodeReferenceKind.Field
                : CodeReferenceKind.Method,
            _ => null,
        };

    internal static CodeReferenceKind KindOf(SymbolKind kind) =>
        kind switch
        {
            SymbolKind.TypeDefinition => CodeReferenceKind.Type,
            SymbolKind.Field => CodeReferenceKind.Field,
            SymbolKind.Property or SymbolKind.Indexer => CodeReferenceKind.Property,
            SymbolKind.Event => CodeReferenceKind.Event,
            _ => CodeReferenceKind.Method,
        };

    /// <summary>A reference to the assembly in the file at <paramref name="filePath"/> itself.</summary>
    internal static CodeReference ToAssembly(string name, string filePath) =>
        new(CodeReferenceKind.Assembly, name, filePath, EntityHandle.AssemblyDefinition, 0, null);

    internal static CodeReference ToLocal(CodeReferenceKind kind, string name, int key, CodeReference? scope) =>
        new(kind, name, null, default, key, scope);

    public bool Equals(CodeReference? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        if (FilePath != null || other.FilePath != null)
            return Handle == other.Handle && string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase);
        return Kind == other.Kind && _localKey == other._localKey && Equals(_scope, other._scope);
    }

    public override bool Equals(object? obj) => obj is CodeReference other && Equals(other);

    public override int GetHashCode() =>
        FilePath != null
            ? HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(FilePath), MetadataTokens.GetToken(Handle))
            : HashCode.Combine(Kind, _localKey, _scope);

    public static bool operator ==(CodeReference? left, CodeReference? right) => Equals(left, right);
    public static bool operator !=(CodeReference? left, CodeReference? right) => !Equals(left, right);

    public override string ToString() => $"{Kind} {Name}";
}
