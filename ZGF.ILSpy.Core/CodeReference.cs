using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

/// <summary>What a <see cref="CodeReference.Entity"/> points at.</summary>
public enum EntityKind
{
    Type,
    Method,
    Field,
    Property,
    Event,
}

/// <summary>
/// An immutable pointer from code or the tree to an assembly, entity, local or label. References to
/// the same target compare equal, so every occurrence of a symbol in a document can be matched.
/// <see cref="Navigable"/> references can be resolved to a tree node with
/// <see cref="NodeLookup.Find(AssemblyList, BrowseSession, CodeReference.Navigable)"/>.
/// </summary>
public abstract class CodeReference : IEquatable<CodeReference>
{
    private CodeReference(string name)
    {
        Name = name;
    }

    /// <summary>The target's name as written where it was referenced.</summary>
    public string Name { get; }

    /// <summary>An assembly or entity, found by a handle in the file at <see cref="FilePath"/>.</summary>
    public abstract class Navigable : CodeReference
    {
        private protected Navigable(string name, AssemblyPath filePath, EntityHandle handle) : base(name)
        {
            FilePath = filePath;
            Handle = handle;
        }

        internal AssemblyPath FilePath { get; }
        internal EntityHandle Handle { get; }

        public override bool Equals(CodeReference? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is not Navigable navigable) return false;
            return Handle == navigable.Handle && FilePath.Equals(navigable.FilePath);
        }

        public override int GetHashCode() => HashCode.Combine(FilePath, MetadataTokens.GetToken(Handle));
    }

    /// <summary>The assembly in the file at <see cref="Navigable.FilePath"/> itself, or one that file
    /// references by <see cref="Referenced"/>.</summary>
    public sealed class Assembly : Navigable
    {
        internal Assembly(string name, AssemblyPath filePath, AssemblyReferenceHandle? referenced)
            : base(name, filePath, HandleOf(referenced))
        {
            Referenced = referenced;
        }

        /// <summary>The reference to the assembly from the file; null for the file's own assembly.</summary>
        internal AssemblyReferenceHandle? Referenced { get; }

        private static EntityHandle HandleOf(AssemblyReferenceHandle? referenced)
        {
            if (referenced == null)
                return EntityHandle.AssemblyDefinition;
            return referenced.Value;
        }

        public override string ToString() => $"Assembly {Name}";
    }

    /// <summary>A type or member, defined or referenced by its handle in the file.</summary>
    public sealed class Entity : Navigable
    {
        internal Entity(EntityKind kind, string name, AssemblyPath filePath, EntityHandle handle) : base(name, filePath, handle)
        {
            Kind = kind;
        }

        public EntityKind Kind { get; }

        public override string ToString() => $"{Kind} {Name}";
    }

    /// <summary>A local variable or parameter, numbered by <see cref="Key"/> within <see cref="Scope"/>.</summary>
    public sealed class Local : CodeReference
    {
        internal Local(string name, int key, Entity? scope) : base(name)
        {
            Key = key;
            Scope = scope;
        }

        internal int Key { get; }
        internal Entity? Scope { get; }

        public override bool Equals(CodeReference? other)
        {
            if (ReferenceEquals(this, other)) return true;
            return other is Local local && Key == local.Key && Equals(Scope, local.Scope);
        }

        public override int GetHashCode() => HashCode.Combine(nameof(Local), Key, Scope);

        public override string ToString() => $"Local {Name}";
    }

    /// <summary>An IL label, numbered by <see cref="Key"/> within <see cref="Scope"/>.</summary>
    public sealed class Label : CodeReference
    {
        internal Label(string name, int key, Entity? scope) : base(name)
        {
            Key = key;
            Scope = scope;
        }

        internal int Key { get; }
        internal Entity? Scope { get; }

        public override bool Equals(CodeReference? other)
        {
            if (ReferenceEquals(this, other)) return true;
            return other is Label label && Key == label.Key && Equals(Scope, label.Scope);
        }

        public override int GetHashCode() => HashCode.Combine(nameof(Label), Key, Scope);

        public override string ToString() => $"Label {Name}";
    }

    /// <summary>A reference to the definition <paramref name="entity"/>, named <paramref name="name"/>
    /// or else by its own name; null when it has no metadata file or token.</summary>
    internal static Entity? To(IEntity entity, string? name = null)
    {
        var file = entity.ParentModule?.MetadataFile;
        if (file == null || entity.MetadataToken.IsNil)
            return null;
        return new Entity(KindOf(entity.SymbolKind), name ?? entity.Name, AssemblyPath.Of(file), entity.MetadataToken);
    }

    /// <summary>A reference to what <paramref name="handle"/> of <paramref name="metadata"/> names, read
    /// from the file at <paramref name="filePath"/>, or null for a handle no reference is made to.</summary>
    internal static Navigable? To(MetadataReader metadata, Handle handle, string name, AssemblyPath filePath) =>
        handle.Kind switch
        {
            HandleKind.AssemblyDefinition => new Assembly(name, filePath, null),
            HandleKind.AssemblyReference => new Assembly(name, filePath, (AssemblyReferenceHandle)handle),
            HandleKind.TypeDefinition or HandleKind.TypeReference or HandleKind.TypeSpecification =>
                new Entity(EntityKind.Type, name, filePath, (EntityHandle)handle),
            HandleKind.MethodDefinition or HandleKind.MethodSpecification => new Entity(EntityKind.Method, name, filePath, (EntityHandle)handle),
            HandleKind.FieldDefinition => new Entity(EntityKind.Field, name, filePath, (EntityHandle)handle),
            HandleKind.PropertyDefinition => new Entity(EntityKind.Property, name, filePath, (EntityHandle)handle),
            HandleKind.EventDefinition => new Entity(EntityKind.Event, name, filePath, (EntityHandle)handle),
            HandleKind.MemberReference => new Entity(MemberKindOf(metadata, (MemberReferenceHandle)handle), name, filePath, (EntityHandle)handle),
            _ => null,
        };

    private static EntityKind MemberKindOf(MetadataReader metadata, MemberReferenceHandle handle) =>
        metadata.GetMemberReference(handle).GetKind() == MemberReferenceKind.Field ? EntityKind.Field : EntityKind.Method;

    internal static EntityKind KindOf(SymbolKind kind) =>
        kind switch
        {
            SymbolKind.TypeDefinition => EntityKind.Type,
            SymbolKind.Field => EntityKind.Field,
            SymbolKind.Property or SymbolKind.Indexer => EntityKind.Property,
            SymbolKind.Event => EntityKind.Event,
            _ => EntityKind.Method,
        };

    /// <summary>A reference to the assembly in the file at <paramref name="filePath"/> itself.</summary>
    internal static Assembly ToAssembly(string name, AssemblyPath filePath) => new(name, filePath, null);

    /// <summary>A reference to the assembly the file at <paramref name="filePath"/> references by <paramref name="handle"/>.</summary>
    internal static Assembly ToAssembly(string name, AssemblyPath filePath, AssemblyReferenceHandle handle) => new(name, filePath, handle);

    internal static Entity ToEntity(EntityKind kind, string name, AssemblyPath filePath, EntityHandle handle) => new(kind, name, filePath, handle);

    public abstract bool Equals(CodeReference? other);

    public sealed override bool Equals(object? obj) => obj is CodeReference other && Equals(other);

    public abstract override int GetHashCode();

    public static bool operator ==(CodeReference? left, CodeReference? right) => Equals(left, right);
    public static bool operator !=(CodeReference? left, CodeReference? right) => !Equals(left, right);
}
