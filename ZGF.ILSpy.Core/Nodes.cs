using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Core.Resources;
using AssemblyReference = ICSharpCode.Decompiler.Metadata.AssemblyReference;
using MetadataResource = ICSharpCode.Decompiler.Metadata.Resource;

namespace ZGF.ILSpy.Core;

/// <summary>A node of a module that was read: one under its <see cref="AssemblyNode"/>, showing what
/// <see cref="Module"/> holds.</summary>
internal interface IModuleNode
{
    LoadedModule Module { get; }
}

internal sealed class AssemblyNode : BrowseNode, IModuleNode
{
    private Dictionary<string, NamespaceNode>? _namespaces;
    private BrowseNode[]? _nested;

    public LoadedModule Module { get; }

    public AssemblyNode(LoadedModule assembly, BrowseNode? parent) : base(assembly, parent, NodeKind.Assembly, LabelOf(assembly))
    {
        Module = assembly;
    }

    public override string FullName => Module.FullName;

    internal override bool MayHaveChildren => true;

    /// <summary>The namespace named <paramref name="name"/>, once the children are built; else null.</summary>
    internal NamespaceNode? NamespaceNamed(string name) => Volatile.Read(ref _namespaces)?.GetValueOrDefault(name);

    internal override BrowseNode[] CreateChildren()
    {
        var file = Module.File;
        var result = new List<BrowseNode> { new MetadataFolderNode(Module, this, file, "Metadata") };
        var symbols = Module.DebugSymbols;
        if (symbols != null)
            result.Add(new MetadataFolderNode(Module, this, symbols.File, symbols.Title));
        result.Add(new ReferencesFolderNode(Module, this));
        if (!file.Resources.IsEmpty)
            result.Add(new ResourcesFolderNode(Module, this));

        var byNamespace = new Dictionary<string, List<ITypeDefinition>>(StringComparer.Ordinal);
        foreach (var type in Module.MainModule.TopLevelTypeDefinitions)
        {
            if (Members.IsMissing(type)) continue;
            if (!byNamespace.TryGetValue(type.Namespace, out var types))
                byNamespace.Add(type.Namespace, types = new List<ITypeDefinition>());
            types.Add(type);
        }
        var forwardedByNamespace = ForwardedTypes.ListByNamespace(file.Metadata);

        var namespaces = new Dictionary<string, NamespaceNode>(StringComparer.Ordinal);
        var names = byNamespace.Keys
            .Union(forwardedByNamespace.Keys)
            .SelectMany(NamespaceNames.AndEnclosing)
            .Distinct()
            .Order(NaturalStringComparer.Instance);
        foreach (var name in names)
        {
            var types = byNamespace.GetValueOrDefault(name, []);
            var forwarded = forwardedByNamespace.GetValueOrDefault(name, []);
            var enclosing = FindEnclosing(namespaces, name);
            var node = new NamespaceNode(Module, this, name, enclosing, types, forwarded);
            if (enclosing != null)
                enclosing.Enclose(node);
            namespaces.Add(name, node);
            result.Add(node);
        }
        Volatile.Write(ref _namespaces, namespaces);
        _nested = LeaveOutNested(result);
        return result.ToArray();
    }

    /// <summary>The namespace <paramref name="name"/> extends by a segment, made already, as names
    /// sort after the names they extend; null for an outermost name.</summary>
    private static NamespaceNode? FindEnclosing(Dictionary<string, NamespaceNode> namespaces, string name)
    {
        var enclosing = NamespaceNames.EnclosingName(name);
        if (enclosing == null)
            return null;
        return namespaces[enclosing];
    }

    /// <summary>The children but the namespaces nested in others.</summary>
    private static BrowseNode[] LeaveOutNested(List<BrowseNode> children)
    {
        return children
            .Where(static c => c.EnclosingNamespace == null)
            .ToArray();
    }

    internal override IReadOnlyList<BrowseNode> Nest(IReadOnlyList<BrowseNode> children) => _nested ?? children;

    /// <summary>The assembly's name, version and the framework it was built for. The framework picked
    /// to resolve its references against is left out, so that the label, by which paths find the node,
    /// stays the same when another is picked.</summary>
    private static string LabelOf(LoadedModule assembly)
    {
        var details = new[] { assembly.Version?.ToString(), BuiltForName(assembly) }.OfType<string>().ToArray();
        return details.Length == 0 ? assembly.Name : $"{assembly.Name} ({string.Join(", ", details)})";
    }

    private static string? BuiltForName(LoadedModule assembly)
    {
        if (assembly.DetectedTargetFramework == null)
            return null;
        return FrameworkIds.ReadableName(assembly.DetectedTargetFramework);
    }
}

/// <summary>The root of a listed file that could not be read as a .NET assembly: labelled with the
/// file's name, without children, and showing why it failed.</summary>
internal sealed class FailedAssemblyNode(FailedAssembly assembly)
    : BrowseNode(assembly, null, NodeKind.FailedAssembly, assembly.FilePath.FileName)
{
    public FailedAssembly Failed { get; } = assembly;

    public override string FullName => Assembly.FilePath.Text;
}

internal sealed class ReferencesFolderNode(LoadedModule assembly, BrowseNode parent)
    : BrowseNode(assembly, parent, NodeKind.ReferencesFolder, "References"), IModuleNode
{
    public LoadedModule Module { get; } = assembly;

    internal override bool MayHaveChildren => HasReferences(Module);

    private static bool HasReferences(LoadedModule assembly)
    {
        if (!assembly.File.AssemblyReferences.IsEmpty)
            return true;
        return assembly.File.Metadata.GetTableRowCount(TableIndex.ModuleRef) > 0;
    }

    /// <summary>The assemblies referenced, then the modules, each sorted by name.</summary>
    internal override BrowseNode[] CreateChildren()
    {
        var assemblies = AssemblyReferenceNode.ListReferences(Module, this);
        var modules = ModuleReferenceNode.ListReferences(Module, this);
        return assemblies
            .Concat(modules)
            .ToArray();
    }
}

/// <summary>A module an assembly references: a native library it calls into, or another module of the
/// assembly, which holds metadata when the assembly's file table says so. It lists the methods the
/// assembly imports from it, sorted by name.</summary>
internal sealed class ModuleReferenceNode : BrowseNode, IModuleNode
{
    private readonly IReadOnlyList<MethodDefinitionHandle> _imports;

    public LoadedModule Module { get; }

    private ModuleReferenceNode(LoadedModule assembly, BrowseNode parent, string name, IReadOnlyList<MethodDefinitionHandle> imports)
        : base(assembly, parent, NodeKind.ModuleReference, name)
    {
        Module = assembly;
        _imports = imports;
        ContainsMetadata = IsModuleWithMetadata(assembly.File.Metadata, name);
    }

    /// <summary>Whether the assembly's file table lists the module as one holding metadata.</summary>
    public bool ContainsMetadata { get; }

    internal override bool MayHaveChildren => _imports.Count > 0;

    internal override BrowseNode[] CreateChildren()
    {
        var module = Module.TypeSystem.MainModule;
        return _imports
            .Select(h => new ImportedMethodNode(Module, this, module.GetDefinition(h)))
            .OrderBy(n => n.Text, NaturalStringComparer.Instance)
            .ThenBy(n => n.DeclaringTypeName, NaturalStringComparer.Instance)
            .Cast<BrowseNode>()
            .ToArray();
    }

    private static bool IsModuleWithMetadata(MetadataReader metadata, string name)
    {
        foreach (var handle in metadata.AssemblyFiles)
        {
            var file = metadata.GetAssemblyFile(handle);
            if (metadata.StringComparer.Equals(file.Name, name))
                return file.ContainsMetadata;
        }
        return false;
    }

    /// <summary>The modules <paramref name="assembly"/> references, sorted by name, under <paramref name="parent"/>.
    /// Which methods import from each is read here, off the UI thread, so that their rows can tell whether
    /// they have children without reading it there.</summary>
    internal static BrowseNode[] ListReferences(LoadedModule assembly, BrowseNode parent)
    {
        var metadata = assembly.File.Metadata;
        var imports = ListImports(metadata);
        return Enumerable.Range(1, metadata.GetTableRowCount(TableIndex.ModuleRef))
            .Select(MetadataTokens.ModuleReferenceHandle)
            .Select(h => Create(assembly, parent, h, imports))
            .OrderBy(n => n.Text, NaturalStringComparer.Instance)
            .Cast<BrowseNode>()
            .ToArray();
    }

    private static ModuleReferenceNode Create(LoadedModule assembly, BrowseNode parent, ModuleReferenceHandle handle,
        Dictionary<ModuleReferenceHandle, List<MethodDefinitionHandle>> imports)
    {
        var metadata = assembly.File.Metadata;
        var name = metadata.GetString(metadata.GetModuleReference(handle).Name);
        return new ModuleReferenceNode(assembly, parent, name, ImportsFrom(imports, handle));
    }

    /// <summary>The methods imported from each module, read from what each method's import names.</summary>
    private static Dictionary<ModuleReferenceHandle, List<MethodDefinitionHandle>> ListImports(MetadataReader metadata)
    {
        var imports = new Dictionary<ModuleReferenceHandle, List<MethodDefinitionHandle>>();
        foreach (var handle in metadata.MethodDefinitions)
        {
            var module = metadata.GetMethodDefinition(handle).GetImport().Module;
            if (module.IsNil)
                continue;
            AddImport(imports, module, handle);
        }
        return imports;
    }

    private static void AddImport(Dictionary<ModuleReferenceHandle, List<MethodDefinitionHandle>> imports, ModuleReferenceHandle module,
        MethodDefinitionHandle method)
    {
        if (!imports.TryGetValue(module, out var methods))
        {
            methods = new List<MethodDefinitionHandle>();
            imports.Add(module, methods);
        }
        methods.Add(method);
    }

    private static IReadOnlyList<MethodDefinitionHandle> ImportsFrom(Dictionary<ModuleReferenceHandle, List<MethodDefinitionHandle>> imports,
        ModuleReferenceHandle module)
    {
        if (imports.TryGetValue(module, out var methods))
            return methods;
        return [];
    }
}

/// <summary>A method an assembly imports from a module, listed under that module: labelled as the tree
/// labels the method, beside the type declaring it, and going to the method. A stub the compiler generates
/// as a local function, as <c>[LibraryImport]</c> does, is labelled and goes as the method declaring it,
/// with the stub's own parameters, the ones the module is called with.</summary>
internal sealed class ImportedMethodNode : BrowseNode, IModuleNode
{
    private const string LocalFunctionMarker = ">g__";

    public LoadedModule Module { get; }

    public ImportedMethodNode(LoadedModule assembly, BrowseNode parent, IMethod method)
        : base(assembly, parent, NodeKind.ImportedMethod, LabelOf(method))
    {
        Module = assembly;
        var declaring = method.DeclaringTypeDefinition!;
        var shown = FindEnclosingMethod(method);
        Accessibility = EntityText.Map(shown.Accessibility);
        IsPublicApi = EntityText.IsPublicApi(shown);
        DeclaringTypeName = EntityText.FullName(declaring);
        ModuleName = parent.Text;
        FullName = $"{DeclaringTypeName}.{Text}";
        EntryPoint = ReadEntryPoint(assembly.File.Metadata, method);
        Target = CodeReference.To(shown);
    }

    public override string FullName { get; }

    /// <summary>The tree's label for <paramref name="method"/>, named as the method enclosing it when it is a local function.</summary>
    private static string LabelOf(IMethod method)
    {
        var label = EntityText.Label(method);
        var enclosing = FindEnclosingName(method.Name);
        if (enclosing == null)
            return label;
        return enclosing + label.Substring(method.Name.Length);
    }

    /// <summary>The name of the method a local function is declared in, as the compiler writes it into the
    /// function's name, e.g. <c>CloseHandle</c> for <c>&lt;CloseHandle&gt;g____PInvoke|277_0</c>; null
    /// for a method that is no local function.</summary>
    private static string? FindEnclosingName(string name)
    {
        if (!name.StartsWith('<'))
            return null;
        var end = name.IndexOf(LocalFunctionMarker, StringComparison.Ordinal);
        if (end < 0)
            return null;
        return name.Substring(1, end - 1);
    }

    /// <summary>The method <paramref name="method"/> is declared in when it is a local function and its type
    /// declares a method of that name, else <paramref name="method"/> itself.</summary>
    private static IMethod FindEnclosingMethod(IMethod method)
    {
        var enclosing = FindEnclosingName(method.Name);
        if (enclosing == null)
            return method;
        var declared = method.DeclaringTypeDefinition!.Methods
            .FirstOrDefault(m => m.Name == enclosing);
        if (declared == null)
            return method;
        return declared;
    }

    /// <summary>The name of the module the method is imported from.</summary>
    public string ModuleName { get; }

    /// <summary>The name of the function the method calls in the module.</summary>
    public string EntryPoint { get; }

    /// <summary>The name the method's import gives, else the method's own, as the runtime looks it up.</summary>
    private static string ReadEntryPoint(MetadataReader metadata, IMethod method)
    {
        var handle = (MethodDefinitionHandle)method.MetadataToken;
        var import = metadata.GetMethodDefinition(handle).GetImport();
        if (import.Name.IsNil)
            return method.Name;
        return metadata.GetString(import.Name);
    }
}

/// <summary>A reference of an assembly: the types the assembly uses from it, then the references of the
/// assembly it resolves to, found where following it finds it but not listed; only the types when it
/// resolves to none.</summary>
internal sealed class AssemblyReferenceNode : BrowseNode, IModuleNode
{
    public LoadedModule Module { get; }

    public AssemblyReferenceNode(LoadedModule assembly, BrowseNode parent, AssemblyReference reference)
        : base(assembly, parent, NodeKind.AssemblyReference, reference.Name)
    {
        Module = assembly;
        AssemblyReference = reference;
        Target = CodeReference.ToEntity(CodeReferenceKind.Assembly, reference.Name, assembly.FilePath, reference.Handle);
    }

    public AssemblyReference AssemblyReference { get; }

    public override string FullName => AssemblyReference.FullName;

    internal override bool MayHaveChildren => true;

    internal override BrowseNode[] CreateChildren()
    {
        var children = new List<BrowseNode>();
        if (Module.ReferenceUsage.Uses(AssemblyReference.Handle))
            children.Add(new ReferencedTypesNode(Module, this, AssemblyReference.Handle));
        var referenced = ResolveReferenced();
        if (referenced != null)
            children.AddRange(ListReferences(referenced, this));
        return children.ToArray();
    }

    /// <summary>The assembly the reference resolves to, loaded if it is not; null when it cannot be found or read.</summary>
    private LoadedModule? ResolveReferenced()
    {
        if (Module.Resolver.ResolveAssembly(AssemblyReference) is not LoadedModule referenced)
            return null;
        return referenced;
    }

    /// <summary>The references of <paramref name="assembly"/>, sorted by name, under <paramref name="parent"/>.
    /// What the assembly uses of them is read here, off the UI thread, so that listing what each holds
    /// never reads it there.</summary>
    internal static BrowseNode[] ListReferences(LoadedModule assembly, BrowseNode parent)
    {
        assembly.ReadReferenceUsage();
        return assembly.File.AssemblyReferences
            .OrderBy(r => r.Name, NaturalStringComparer.Instance)
            .Select(r => (BrowseNode)new AssemblyReferenceNode(assembly, parent, r))
            .ToArray();
    }
}

internal sealed class ResourcesFolderNode(LoadedModule assembly, BrowseNode parent)
    : BrowseNode(assembly, parent, NodeKind.ResourcesFolder, "Resources"), IModuleNode
{
    public LoadedModule Module { get; } = assembly;

    internal override bool MayHaveChildren => true;

    internal override BrowseNode[] CreateChildren() =>
        Module.File.Resources
            .OrderBy(r => r.Name, NaturalStringComparer.Instance)
            .Select(r => ResourceNodes.Create(Module, this, r))
            .ToArray();
}

/// <summary>A namespace of an assembly: the types it defines there and those it forwards from
/// there to other assemblies, sorted together by name, then by how many type parameters they have.
/// When the tree nests namespaces, the namespaces nested in it sit among them, each where ILSpy sorts
/// the full names of the types in it.</summary>
internal sealed class NamespaceNode : BrowseNode, IModuleNode
{
    private const string NestedSortMark = ".";

    private readonly List<ITypeDefinition> _types;
    private readonly List<ForwardedType> _forwarded;
    private readonly List<NamespaceNode> _inner = new();
    private BrowseNode[]? _nested;

    public LoadedModule Module { get; }

    public NamespaceNode(LoadedModule assembly, BrowseNode parent, string name, NamespaceNode? enclosing, List<ITypeDefinition> types,
        List<ForwardedType> forwarded)
        : base(assembly, parent, NodeKind.Namespace, name.Length == 0 ? "-" : name)
    {
        Module = assembly;
        Name = name;
        _types = types;
        _forwarded = forwarded;
        EnclosingNamespace = enclosing;
        IsEnclosingOnly = types.Count == 0 && forwarded.Count == 0;
        IsPublicApi = forwarded.Count > 0 || types.Exists(EntityText.IsPublicApi);
        IsCompilerGenerated = forwarded.Count == 0 && types.TrueForAll(Members.IsCompilerGenerated);
    }

    public string Name { get; }

    /// <summary>The types the assembly defines in this namespace.</summary>
    public IEnumerable<ITypeDefinition> Types => _types;

    public override string FullName => Name;

    public override IReadOnlyList<BrowseNode> InnerNamespaces => _inner;

    /// <summary>Nests <paramref name="inner"/>, whose name extends this one's by a segment, in this namespace.</summary>
    internal void Enclose(NamespaceNode inner)
    {
        _inner.Add(inner);
    }

    internal override bool MayHaveChildren => !IsEnclosingOnly;

    internal override BrowseNode[] CreateChildren()
    {
        var sorted = SortChildren();
        _nested = Interleave(sorted);
        return sorted
            .Select(static c => c.Node)
            .ToArray();
    }

    private List<SortedChild> SortChildren()
    {
        var defined = _types
            .Select(t => new SortedChild(t.Name, t.TypeParameterCount, new TypeNode(Module, this, t)));
        var forwarded = _forwarded
            .Select(f => new SortedChild(f.SortName, f.Arity, new ForwardedTypeNode(Module, this, f)));
        return defined
            .Concat(forwarded)
            .OrderBy(static c => c.Name, NaturalStringComparer.Instance)
            .ThenBy(static c => c.Arity)
            .ToList();
    }

    /// <summary>The namespaces nested in this one among <paramref name="own"/>, the children sorted.</summary>
    private BrowseNode[] Interleave(List<SortedChild> own)
    {
        var result = new List<BrowseNode>(own.Count + _inner.Count);
        var inner = new Queue<NamespaceNode>(_inner);
        foreach (var child in own)
        {
            while (inner.Count > 0 && SortsBefore(inner.Peek(), child))
                result.Add(inner.Dequeue());
            result.Add(child.Node);
        }
        result.AddRange(inner);
        return result.ToArray();
    }

    /// <summary>Whether ILSpy, sorting the full names of the types, comes to those in
    /// <paramref name="inner"/> before <paramref name="child"/>.</summary>
    private static bool SortsBefore(NamespaceNode inner, SortedChild child)
    {
        var key = NamespaceNames.LastSegment(inner.Name) + NestedSortMark;
        return NaturalStringComparer.Instance.Compare(key, child.ReflectionName) < 0;
    }

    internal override IReadOnlyList<BrowseNode> Nest(IReadOnlyList<BrowseNode> children) => _nested ?? children;

    /// <summary>A type of the namespace with what it is sorted by: its name without type parameters,
    /// then how many it has.</summary>
    private readonly record struct SortedChild(string Name, int Arity, BrowseNode Node)
    {
        private const string ArityMark = "`";

        /// <summary>The name as its reflection name ends, e.g. <c>List`1</c>.</summary>
        public string ReflectionName
        {
            get
            {
                if (Arity == 0)
                    return Name;
                return Name + ArityMark + Arity;
            }
        }
    }
}

/// <summary>A type the assembly forwards to another: named as <c>typeof</c> names an open generic
/// type, and going to the type where it is defined.</summary>
internal sealed class ForwardedTypeNode : BrowseNode, IModuleNode
{
    public LoadedModule Module { get; }

    public ForwardedTypeNode(LoadedModule assembly, BrowseNode parent, ForwardedType type)
        : base(assembly, parent, NodeKind.ForwardedType, type.Label)
    {
        Module = assembly;
        FullName = type.Namespace.Length == 0 ? type.Label : $"{type.Namespace}.{type.Label}";
        ForwardedTo = type.Destination;
        Target = CodeReference.ToEntity(CodeReferenceKind.Type, type.Label, assembly.FilePath, type.Handle);
    }

    public override string FullName { get; }
}

internal abstract class EntityNode : BrowseNode, IModuleNode
{
    public LoadedModule Module { get; }

    private protected EntityNode(LoadedModule assembly, BrowseNode parent, IEntity entity)
        : base(assembly, parent, EntityText.KindOf(entity), EntityText.Label(entity))
    {
        Module = assembly;
        Entity = entity;
        Accessibility = EntityText.Map(entity.Accessibility);
        IsPublicApi = EntityText.IsPublicApi(entity);
        IsCompilerGenerated = parent.IsCompilerGenerated || Members.IsCompilerGenerated(entity);
        Reference = CodeReference.To(entity);
        Token = MetadataTokens.GetToken(entity.MetadataToken);
    }

    public IEntity Entity { get; }

    public override string FullName => EntityText.FullName(Entity);
}

internal sealed class TypeNode(LoadedModule assembly, BrowseNode parent, ITypeDefinition type)
    : EntityNode(assembly, parent, type)
{
    public ITypeDefinition Type => (ITypeDefinition)Entity;

    internal override bool MayHaveChildren
    {
        get
        {
            var definition = Module.File.Metadata.GetTypeDefinition((TypeDefinitionHandle)Type.MetadataToken);
            return !definition.BaseType.IsNil || definition.GetInterfaceImplementations().Count > 0 || definition.GetNestedTypes().Length > 0 || DerivedTypesNode.CanHave(Type) ||
                   definition.GetFields().Count > 0 || definition.GetMethods().Count > 0 || definition.GetProperties().Count > 0 ||
                   definition.GetEvents().Count > 0;
        }
    }

    internal override BrowseNode[] CreateChildren()
    {
        var type = Type;
        var result = new List<BrowseNode>();
        if (type.DirectBaseTypes.Any())
            result.Add(new BaseTypesNode(Module, this, type));
        if (DerivedTypesNode.CanHave(type))
            result.Add(new DerivedTypesNode(Module, this, type));

        foreach (var nested in type.NestedTypes.Where(t => !Members.IsMissing(t)).OrderBy(t => t.Name, NaturalStringComparer.Instance))
            result.Add(new TypeNode(Module, this, nested));

        var fields = type.Fields.Where(f => !Members.IsMissing(f) && !(type.Kind == TypeKind.Enum && !f.IsStatic));
        if (type.Kind != TypeKind.Enum)
            fields = fields.OrderBy(f => f.Name, NaturalStringComparer.Instance);
        foreach (var field in fields)
            result.Add(new MemberNode(Module, this, field));

        foreach (var property in type.Properties.Where(p => !Members.IsMissing(p)).OrderBy(p => p.Name, NaturalStringComparer.Instance))
            result.Add(new MemberNode(Module, this, property));

        foreach (var @event in type.Events.Where(e => !Members.IsMissing(e)).OrderBy(e => e.Name, NaturalStringComparer.Instance))
            result.Add(new MemberNode(Module, this, @event));

        foreach (var method in type.Methods.Where(m => !m.IsAccessor && !Members.IsMissing(m)).OrderBy(m => m.Name, NaturalStringComparer.Instance))
            result.Add(new MemberNode(Module, this, method));

        return result.ToArray();
    }
}

internal sealed class MemberNode(LoadedModule assembly, BrowseNode parent, IMember member)
    : EntityNode(assembly, parent, member)
{
    public IMember Member => (IMember)Entity;

    internal override bool MayHaveChildren => Member is IProperty or IEvent;

    internal override BrowseNode[] CreateChildren() =>
        Members.Accessors(Member)
            .Where(a => !a.MetadataToken.IsNil)
            .Select(a => (BrowseNode)new MemberNode(Module, this, a))
            .ToArray();
}

internal sealed class BaseTypesNode(LoadedModule assembly, BrowseNode parent, ITypeDefinition type)
    : BrowseNode(assembly, parent, NodeKind.BaseTypes, "Base Types"), IModuleNode
{
    public LoadedModule Module { get; } = assembly;

    public ITypeDefinition Type { get; } = type;

    internal override bool MayHaveChildren => true;

    internal override BrowseNode[] CreateChildren() =>
        Type.DirectBaseTypes.Select(t => (BrowseNode)new BaseTypeNode(Module, this, t)).ToArray();
}

internal sealed class BaseTypeNode : BrowseNode, IModuleNode
{
    public LoadedModule Module { get; }

    public BaseTypeNode(LoadedModule assembly, BrowseNode parent, IType type)
        : base(assembly, parent, NodeKind.BaseType, EntityText.TypeName(type))
    {
        Module = assembly;
        Type = type;
        var definition = type.GetDefinition();
        var target = definition != null ? CodeReference.To(definition) : null;
        if (definition != null && target != null)
        {
            Target = target;
            Accessibility = EntityText.Map(definition.Accessibility);
            IsPublicApi = EntityText.IsPublicApi(definition);
        }
    }

    public IType Type { get; }
}

/// <summary>ILSpy's "Derived Types": the types of the open assemblies that name
/// <see cref="Type"/> among their direct base types, found when first expanded.</summary>
internal sealed class DerivedTypesNode(LoadedModule assembly, BrowseNode parent, ITypeDefinition type)
    : BrowseNode(assembly, parent, NodeKind.DerivedTypes, "Derived Types"), IModuleNode
{
    public LoadedModule Module { get; } = assembly;

    public ITypeDefinition Type { get; } = type;

    internal override bool MayHaveChildren => true;

    internal override BrowseNode[] CreateChildren() => Find(Module, this, Type);

    /// <summary>Whether anything can derive from <paramref name="type"/>: a class that is not sealed,
    /// or an interface.</summary>
    public static bool CanHave(ITypeDefinition type) => !type.IsSealed && type.Kind is TypeKind.Class or TypeKind.Interface;

    /// <summary>The types in scope of <paramref name="type"/> that derive from it directly, as
    /// <see cref="DerivedTypes"/> finds them.</summary>
    internal static BrowseNode[] Find(LoadedModule assembly, BrowseNode parent, ITypeDefinition type)
    {
        return DerivedTypes.Find(assembly.List, type, CancellationToken.None)
            .Select(t => (BrowseNode)new DerivedTypeNode(assembly, parent, t))
            .ToArray();
    }
}

/// <summary>A type deriving from the one a <see cref="DerivedTypesNode"/> belongs to: it goes to
/// that type, and expands to the types deriving from it in turn.</summary>
internal sealed class DerivedTypeNode : BrowseNode, IModuleNode
{
    public LoadedModule Module { get; }

    public DerivedTypeNode(LoadedModule assembly, BrowseNode parent, ITypeDefinition type)
        : base(assembly, parent, NodeKind.DerivedType, EntityText.TypeName(type))
    {
        Module = assembly;
        Type = type;
        Accessibility = EntityText.Map(type.Accessibility);
        IsPublicApi = EntityText.IsPublicApi(type);
        Target = CodeReference.To(type);
    }

    public ITypeDefinition Type { get; }

    internal override bool MayHaveChildren => !Type.IsSealed;

    internal override BrowseNode[] CreateChildren() => DerivedTypesNode.Find(Module, this, Type);
}

internal static class Members
{
    private static readonly DecompilerSettings Settings = Core.Settings.Create();

    public static bool IsHidden(IEntity entity) => IsMissing(entity) || IsCompilerGenerated(entity);

    /// <summary>Whether <paramref name="entity"/> has no metadata of its own to show.</summary>
    public static bool IsMissing(IEntity entity) => entity.MetadataToken.IsNil;

    public static bool IsCompilerGenerated(IEntity entity)
    {
        var file = entity.ParentModule?.MetadataFile;
        return file != null && CSharpDecompiler.MemberIsHidden(file, entity.MetadataToken, Settings);
    }

    /// <summary>The accessors of a property or event, in ILSpy's order; none for other entities.</summary>
    public static IEnumerable<IMethod> Accessors(IEntity entity)
    {
        IEnumerable<IMethod?> accessors = entity switch
        {
            IProperty property => [property.Getter, property.Setter],
            IEvent @event => [@event.AddAccessor, @event.RemoveAccessor, @event.InvokeAccessor],
            _ => [],
        };
        return accessors.OfType<IMethod>();
    }
}
