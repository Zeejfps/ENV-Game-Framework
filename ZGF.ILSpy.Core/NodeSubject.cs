using System.Reflection.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Core.Resources;

namespace ZGF.ILSpy.Core;

/// <summary>What sort of type a type is.</summary>
public enum TypeShape
{
    Class,
    Interface,
    Struct,
    Enum,
    Delegate,
    Record,
    RecordStruct,
}

/// <summary>What sort of member a member is.</summary>
public enum MemberShape
{
    Field,
    Constant,
    Property,
    Indexer,
    Event,
    Method,
    Constructor,
    Operator,
}

/// <summary>What a node of the browse tree stands for.</summary>
public abstract class NodeSubject
{
    private NodeSubject()
    {
    }

    /// <summary>A module that was read: an assembly, or a WebCIL module; the root of its tree.</summary>
    public sealed class Module(LoadedModule value) : NodeSubject
    {
        public LoadedModule Value { get; } = value;
    }

    public sealed class Namespace(NamespaceInfo value) : NodeSubject
    {
        public NamespaceInfo Value { get; } = value;
    }

    /// <summary>A listed file that could not be read as a .NET assembly; the root of its tree.</summary>
    public sealed class FailedAssembly(Core.FailedAssembly value) : NodeSubject
    {
        public Core.FailedAssembly Value { get; } = value;
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

    /// <summary>The folder of the assemblies and modules <see cref="Owner"/> references.</summary>
    public sealed class References(LoadedModule owner) : NodeSubject
    {
        public LoadedModule Owner { get; } = owner;
    }

    public sealed class AssemblyReference(AssemblyReferenceInfo value) : NodeSubject
    {
        public AssemblyReferenceInfo Value { get; } = value;
    }

    /// <summary>The folder of the types a module uses from the assembly <see cref="Reference"/> names.</summary>
    public sealed class ReferencedTypes(AssemblyReferenceInfo reference) : NodeSubject
    {
        public AssemblyReferenceInfo Reference { get; } = reference;
    }

    public sealed class TypeReference(TypeReferenceInfo value) : NodeSubject
    {
        public TypeReferenceInfo Value { get; } = value;
    }

    public sealed class MemberReference(MemberReferenceInfo value) : NodeSubject
    {
        public MemberReferenceInfo Value { get; } = value;
    }

    public sealed class ModuleReference(ModuleReferenceInfo value) : NodeSubject
    {
        public ModuleReferenceInfo Value { get; } = value;
    }

    public sealed class ImportedMethod(ImportedMethodInfo value) : NodeSubject
    {
        public ImportedMethodInfo Value { get; } = value;
    }

    public sealed class ForwardedType(ForwardedTypeInfo value) : NodeSubject
    {
        public ForwardedTypeInfo Value { get; } = value;
    }

    /// <summary>The folder of the types <see cref="Type"/> derives from directly.</summary>
    public sealed class BaseTypes(DefinedType type) : NodeSubject
    {
        public DefinedType Type { get; } = type;
    }

    public sealed class BaseType(BaseTypeInfo value) : NodeSubject
    {
        public BaseTypeInfo Value { get; } = value;
    }

    /// <summary>The folder of the types of the open assemblies that derive from <see cref="Type"/> directly.</summary>
    public sealed class DerivedTypes(DefinedType type) : NodeSubject
    {
        public DefinedType Type { get; } = type;
    }

    public sealed class DerivedType(DerivedTypeInfo value) : NodeSubject
    {
        public DerivedTypeInfo Value { get; } = value;
    }

    /// <summary>The folder of <see cref="Owner"/>'s manifest resources.</summary>
    public sealed class Resources(LoadedModule owner) : NodeSubject
    {
        public LoadedModule Owner { get; } = owner;
    }

    /// <summary>A manifest resource that holds entries: a <c>.resources</c> file, or Avalonia's asset container.</summary>
    public sealed class ResourceContainer(ResourceContainerInfo value) : NodeSubject
    {
        public ResourceContainerInfo Value { get; } = value;
    }

    public sealed class ResourceEntry(ResourceEntryInfo value) : NodeSubject
    {
        public ResourceEntryInfo Value { get; } = value;
    }

    /// <summary>The raw CLI metadata of a file: a module's own, or its debug metadata.</summary>
    public sealed class Metadata(MetadataFolderInfo value) : NodeSubject
    {
        public MetadataFolderInfo Value { get; } = value;
    }

    /// <summary>The folder of the tables of the metadata <see cref="Folder"/> stands for.</summary>
    public sealed class MetadataTables(MetadataFolderInfo folder) : NodeSubject
    {
        public MetadataFolderInfo Folder { get; } = folder;
    }

    /// <summary>A part of a metadata folder: a header, directory, table or heap, or a debug directory entry.</summary>
    public sealed class MetadataTable(MetadataTableInfo value) : NodeSubject
    {
        public MetadataTableInfo Value { get; } = value;
    }

    /// <summary>A listed package or bundle, standing for its root folder, or a folder in it.</summary>
    public sealed class Package(PackageFolderInfo value) : NodeSubject
    {
        public PackageFolderInfo Value { get; } = value;
    }

    /// <summary>A file of a package that is no assembly.</summary>
    public sealed class PackageFile(PackageFileInfo value) : NodeSubject
    {
        public PackageFileInfo Value { get; } = value;
    }
}

/// <summary>An assembly <see cref="Module"/> references, as its metadata names it.</summary>
public sealed class AssemblyReferenceInfo
{
    internal AssemblyReferenceInfo(LoadedModule module, ICSharpCode.Decompiler.Metadata.AssemblyReference reference)
    {
        Module = module;
        Reference = reference;
    }

    public LoadedModule Module { get; }

    public ICSharpCode.Decompiler.Metadata.AssemblyReference Reference { get; }
}

/// <summary>A type <see cref="Module"/> uses from one of its references: labelled as <c>typeof</c> names
/// an open generic type, and fully named after the type it is nested in, else after its namespace.</summary>
public sealed class TypeReferenceInfo
{
    internal TypeReferenceInfo(LoadedModule module, TypeReferenceHandle handle, string ns, string label, string fullName)
    {
        Module = module;
        Handle = handle;
        Namespace = ns;
        Label = label;
        FullName = fullName;
    }

    public LoadedModule Module { get; }

    public TypeReferenceHandle Handle { get; }

    public string Namespace { get; }

    public string Label { get; }

    public string FullName { get; }
}

/// <summary>A member <see cref="Module"/> uses of a type from one of its references, as the type
/// declares it: for an accessor, the property or event it belongs to.</summary>
public sealed class MemberReferenceInfo
{
    internal MemberReferenceInfo(LoadedModule module, MemberReferenceHandle handle, IMember declared, string fullName)
    {
        Module = module;
        Handle = handle;
        Declared = declared;
        FullName = fullName;
    }

    public LoadedModule Module { get; }

    public MemberReferenceHandle Handle { get; }

    public IMember Declared { get; }

    public string FullName { get; }

    /// <summary>What sort of member is used, as its type declares it.</summary>
    public MemberShape Shape => EntityText.ShapeOf(Declared);
}

/// <summary>A module <see cref="Module"/> references: a native library it calls into, or another module
/// of the assembly, which holds metadata when the assembly's file table says so.</summary>
public sealed class ModuleReferenceInfo
{
    internal ModuleReferenceInfo(LoadedModule module, string name, bool containsMetadata, IReadOnlyList<MethodDefinitionHandle> imports)
    {
        Module = module;
        Name = name;
        ContainsMetadata = containsMetadata;
        Imports = imports;
    }

    public LoadedModule Module { get; }

    public string Name { get; }

    public bool ContainsMetadata { get; }

    /// <summary>The methods the module imports from it.</summary>
    public IReadOnlyList<MethodDefinitionHandle> Imports { get; }
}

/// <summary>A method imported from a module: <see cref="Shown"/> is the method it is labelled and goes
/// as, which for a stub the compiler generates as a local function is the method declaring it.</summary>
public sealed class ImportedMethodInfo
{
    internal ImportedMethodInfo(DefinedMember shown, string label, string declaringTypeName, string entryPoint, string moduleName)
    {
        Shown = shown;
        Label = label;
        DeclaringTypeName = declaringTypeName;
        EntryPoint = entryPoint;
        ModuleName = moduleName;
    }

    public DefinedMember Shown { get; }

    /// <summary>The method as the tree labels it, with the parameters the module is called with.</summary>
    public string Label { get; }

    public string DeclaringTypeName { get; }

    /// <summary>The name of the function the method calls in the module.</summary>
    public string EntryPoint { get; }

    public string ModuleName { get; }
}

/// <summary>A type <see cref="Module"/> forwards to the assembly <see cref="Destination"/> names.</summary>
public sealed class ForwardedTypeInfo
{
    internal ForwardedTypeInfo(LoadedModule module, ForwardedType type)
    {
        Module = module;
        Handle = type.Handle;
        Namespace = type.Namespace;
        Label = type.Label;
        SortName = type.SortName;
        Arity = type.Arity;
        Destination = type.Destination;
    }

    public LoadedModule Module { get; }

    public ExportedTypeHandle Handle { get; }

    public string Namespace { get; }

    /// <summary>The type as <c>typeof</c> names an open generic type, e.g. <c>Dictionary&lt;,&gt;</c>.</summary>
    public string Label { get; }

    /// <summary>The name without type parameters, which it sorts by, then by <see cref="Arity"/>.</summary>
    public string SortName { get; }

    /// <summary>How many type parameters it has.</summary>
    public int Arity { get; }

    public string Destination { get; }

    public string FullName => Namespace.Length == 0 ? Label : $"{Namespace}.{Label}";
}

/// <summary>A type a type derives from directly, as <see cref="Module"/> names it, and what it resolves to.</summary>
public sealed class BaseTypeInfo
{
    private BaseTypeInfo(LoadedModule module, IType type, BaseTypeTarget target)
    {
        Module = module;
        Type = type;
        Target = target;
    }

    public LoadedModule Module { get; }

    /// <summary>The type as it is derived from, with its type arguments.</summary>
    public IType Type { get; }

    public BaseTypeTarget Target { get; }

    /// <summary>The base type <paramref name="type"/> as <paramref name="module"/> names it, navigable when
    /// its definition is found and has an address.</summary>
    internal static BaseTypeInfo Of(LoadedModule module, IType type)
    {
        var definition = type.GetDefinition();
        if (definition == null)
            return new BaseTypeInfo(module, type, new BaseTypeTarget.Unresolved());
        var address = NavigationAddress.Of(definition);
        if (address == null)
            return new BaseTypeInfo(module, type, new BaseTypeTarget.Unaddressed(definition));
        return new BaseTypeInfo(module, type, new BaseTypeTarget.Navigable(definition, address));
    }
}

/// <summary>What a base type resolves to: a definition that can be navigated to, one that cannot, or none.</summary>
public abstract record BaseTypeTarget
{
    private BaseTypeTarget()
    {
    }

    public sealed record Navigable(ITypeDefinition Definition, NavigationAddress Address) : BaseTypeTarget;

    public sealed record Unaddressed(ITypeDefinition Definition) : BaseTypeTarget;

    public sealed record Unresolved : BaseTypeTarget;
}

/// <summary>A type of the open assemblies that derives directly from the type a folder of derived types
/// belongs to, and the address it is navigated to by.</summary>
public sealed class DerivedTypeInfo
{
    private DerivedTypeInfo(LoadedModule module, ITypeDefinition symbol, NavigationAddress address)
    {
        Module = module;
        Symbol = symbol;
        Address = address;
    }

    /// <summary>The module whose tree lists the type, under a type of its own.</summary>
    public LoadedModule Module { get; }

    public ITypeDefinition Symbol { get; }

    public NavigationAddress Address { get; }

    /// <summary>The derived type <paramref name="type"/>; null when it has no address to be navigated to.</summary>
    internal static DerivedTypeInfo? Of(LoadedModule module, ITypeDefinition type)
    {
        var address = NavigationAddress.Of(type);
        if (address == null)
            return null;
        return new DerivedTypeInfo(module, type, address);
    }
}

/// <summary>Where navigation can go: an assembly, type or member, by a file and a handle there, either
/// the definition itself or a reference to one, resolved when it is followed. Unlike a
/// <see cref="CodeReference"/> it never names a local or a label.</summary>
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

    /// <summary>The address of what <paramref name="handle"/> of the file at <paramref name="filePath"/>
    /// names, a <paramref name="kind"/> called <paramref name="name"/>: an assembly, type or member.</summary>
    internal static NavigationAddress To(CodeReferenceKind kind, string name, AssemblyPath filePath, EntityHandle handle)
    {
        if (kind is CodeReferenceKind.Local or CodeReferenceKind.Label)
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Locals and labels are not navigated to.");
        return new NavigationAddress(filePath, handle, kind, name);
    }

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

    public TypeShape Shape => EntityText.ShapeOf(Symbol);

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

    public MemberShape Shape => EntityText.ShapeOf(Symbol);

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

/// <summary>A namespace of <see cref="Module"/>: the types it defines there and those it forwards from
/// there to other assemblies. One that holds neither is listed only because others nest in it.</summary>
public sealed class NamespaceInfo
{
    internal NamespaceInfo(LoadedModule module, string name, IReadOnlyList<ITypeDefinition> types, IReadOnlyList<ForwardedTypeInfo> forwarded)
    {
        Module = module;
        Name = name;
        Types = types;
        Forwarded = forwarded;
    }

    public LoadedModule Module { get; }

    /// <summary>The full name; empty for the global namespace.</summary>
    public string Name { get; }

    public IReadOnlyList<ITypeDefinition> Types { get; }

    public IReadOnlyList<ForwardedTypeInfo> Forwarded { get; }

    /// <summary>The name as the tree shows it: <c>-</c> for the global namespace.</summary>
    public string Label => Name.Length == 0 ? "-" : Name;

    /// <summary>Whether the module puts no types here, listing it only because others nest in it.</summary>
    public bool IsEnclosingOnly => Types.Count == 0 && Forwarded.Count == 0;

    /// <summary>Whether a type here is visible outside the module: one it forwards, or a public one.</summary>
    public bool IsPublicApi => Forwarded.Count > 0 || Types.Any(EntityText.IsPublicApi);

    /// <summary>Whether everything here is something the compiler made up.</summary>
    public bool IsCompilerGenerated => Forwarded.Count == 0 && Types.All(Members.IsCompilerGenerated);
}
