using System.Reflection.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

/// <summary>What a node of the browse tree stands for.</summary>
public abstract class NodeSubject
{
    private NodeSubject()
    {
    }

    public sealed class TypeDefinition(DefinedType definition) : NodeSubject
    {
        public DefinedType Definition { get; } = definition;
    }

    /// <summary>A field, property, event or method, or the accessor of a property or event.</summary>
    public sealed class MemberDefinition(DefinedMember definition) : NodeSubject
    {
        public DefinedMember Definition { get; } = definition;
    }
}

/// <summary>A definition that can be navigated to: a type or member, by the file that defines it and
/// its handle there. Unlike a <see cref="CodeReference"/> it never names a local or a label.</summary>
public sealed record NavigationAddress
{
    private NavigationAddress(AssemblyPath filePath, EntityHandle handle, CodeReferenceKind kind, string name)
    {
        FilePath = filePath;
        Handle = handle;
        Kind = kind;
        Name = name;
    }

    public AssemblyPath FilePath { get; }

    public EntityHandle Handle { get; }

    public CodeReferenceKind Kind { get; }

    /// <summary>The definition's name, as references to it are shown.</summary>
    public string Name { get; }

    /// <summary>The address of <paramref name="entity"/>'s definition; null when it has no metadata file
    /// or token to be found by.</summary>
    internal static NavigationAddress? Of(IEntity entity)
    {
        var file = entity.ParentModule?.MetadataFile;
        if (file == null)
            return null;
        if (entity.MetadataToken.IsNil)
            return null;
        return new NavigationAddress(AssemblyPath.Of(file), entity.MetadataToken, CodeReference.KindOf(entity.SymbolKind), entity.Name);
    }

    /// <summary>A reference to the definition, as documents and the tree point at it.</summary>
    public CodeReference ToReference() => CodeReference.ToEntity(Kind, Name, FilePath, Handle);
}

/// <summary>A type a module defines, with the handle its node is found by and the address it is navigated to by.</summary>
public sealed class DefinedType
{
    private DefinedType(LoadedModule module, ITypeDefinition symbol, TypeDefinitionHandle handle, NavigationAddress address)
    {
        Module = module;
        Symbol = symbol;
        Handle = handle;
        Address = address;
    }

    public LoadedModule Module { get; }

    public ITypeDefinition Symbol { get; }

    public TypeDefinitionHandle Handle { get; }

    public NavigationAddress Address { get; }

    /// <summary>The definition of <paramref name="type"/> in <paramref name="module"/>; null when it has no
    /// metadata file or token, as a type the decompiler made up has not.</summary>
    internal static DefinedType? Of(LoadedModule module, ITypeDefinition type)
    {
        if (type.MetadataToken.Kind != HandleKind.TypeDefinition)
            return null;
        var address = NavigationAddress.Of(type);
        if (address == null)
            return null;
        return new DefinedType(module, type, (TypeDefinitionHandle)type.MetadataToken, address);
    }
}

/// <summary>A member a module defines, with the handle its node is found by and the address it is navigated to by.</summary>
public sealed class DefinedMember
{
    private DefinedMember(LoadedModule module, IMember symbol, NavigationAddress address)
    {
        Module = module;
        Symbol = symbol;
        Address = address;
    }

    public LoadedModule Module { get; }

    public IMember Symbol { get; }

    public EntityHandle Handle => Address.Handle;

    public NavigationAddress Address { get; }

    /// <summary>The definition of <paramref name="member"/> in <paramref name="module"/>; null when it has
    /// no metadata file or token.</summary>
    internal static DefinedMember? Of(LoadedModule module, IMember member)
    {
        var address = NavigationAddress.Of(member);
        if (address == null)
            return null;
        return new DefinedMember(module, member, address);
    }
}
