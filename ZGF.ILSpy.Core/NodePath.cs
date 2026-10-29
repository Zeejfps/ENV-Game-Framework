using System.Diagnostics;
using System.Text;
using ICSharpCode.Decompiler.Documentation;
using ICSharpCode.Decompiler.TypeSystem;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy.Core;

/// <summary>A folder the tree lists by its role, not by what it holds.</summary>
public enum NodeFolder
{
    References,
    Resources,
    BaseTypes,
    DerivedTypes,
    ReferencedTypes,
    MetadataTables,
}

/// <summary>Whose metadata a metadata folder shows: the module's own, or its debug metadata.</summary>
public enum MetadataSource
{
    Own,
    Debug,
}

/// <summary>What a node is among its siblings, as a <see cref="NodePath.Semantic"/> names it: by what it
/// stands for, not by the label its row shows, so that a renamed generic parameter, a table's row count or
/// a class turned record leaves it the same.</summary>
public abstract record NodePathSegment
{
    private NodePathSegment()
    {
    }

    /// <summary>A namespace by its full name; empty for the global namespace.</summary>
    public sealed record Namespace(string Name) : NodePathSegment;

    /// <summary>A type or member the module defines, by its XML documentation id, such as <c>T:Ns.Calculator`1</c>.</summary>
    public sealed record Definition(string Id) : NodePathSegment;

    public sealed record Folder(NodeFolder Role) : NodePathSegment;

    public sealed record Metadata(MetadataSource Which) : NodePathSegment;

    public sealed record MetadataPart(MetadataPartKey Part) : NodePathSegment;

    /// <summary>A reference by the simple name of the assembly it names.</summary>
    public sealed record AssemblyReference(string Name) : NodePathSegment;

    public sealed record ModuleReference(string Name) : NodePathSegment;

    /// <summary>An import by the documentation id of the method that calls into the module.</summary>
    public sealed record ImportedMethod(string Id) : NodePathSegment;

    /// <summary>A type used from a reference by its reflection name: <c>System.Collections.Generic.List`1</c>.</summary>
    public sealed record UsedType(string FullName) : NodePathSegment;

    /// <summary>A member used of a type by the documentation id of the member as its type declares it.</summary>
    public sealed record UsedMember(string Id) : NodePathSegment;

    /// <summary>A forwarded type by its reflection name.</summary>
    public sealed record ForwardedType(string FullName) : NodePathSegment;

    /// <summary>A base type by its reflection name, type arguments included.</summary>
    public sealed record BaseType(string Name) : NodePathSegment;

    /// <summary>A derived type by the documentation id of the type that derives.</summary>
    public sealed record DerivedType(string Id) : NodePathSegment;

    /// <summary>A manifest resource by its name, or an entry of a container by its path there.</summary>
    public sealed record Resource(string Name) : NodePathSegment;

    /// <summary>A folder of a package by its full path there, whatever folders it is merged with.</summary>
    public sealed record PackageFolder(string Path) : NodePathSegment;

    public sealed record PackageFile(string Path) : NodePathSegment;

    /// <summary>An assembly read out of a package, by the full path of its file there.</summary>
    public sealed record EmbeddedModule(string Path) : NodePathSegment;

    /// <summary>Whether this names an entry of a package by its full path, from the package's root.</summary>
    internal bool IsPackageEntry => this is PackageFolder or PackageFile or EmbeddedModule;

    /// <summary>The segment naming a node of <paramref name="subject"/>; null for the root of a tree.</summary>
    internal static NodePathSegment? Of(NodeSubject subject) => subject switch
    {
        NodeSubject.TypeDefinition type => new Definition(IdStringProvider.GetIdString(type.Definition.Symbol)),
        NodeSubject.MemberDefinition member => new Definition(IdStringProvider.GetIdString(member.Definition.Symbol)),
        NodeSubject.References => new Folder(NodeFolder.References),
        NodeSubject.AssemblyReference reference => new AssemblyReference(reference.Value.Reference.Name),
        NodeSubject.ReferencedTypes => new Folder(NodeFolder.ReferencedTypes),
        NodeSubject.TypeReference type => new UsedType(type.Value.ReflectionName),
        NodeSubject.MemberReference member => new UsedMember(UsedMemberId(member.Value.Declared)),
        NodeSubject.ModuleReference module => new ModuleReference(module.Value.Name),
        NodeSubject.ImportedMethod import => new ImportedMethod(import.Value.Id),
        NodeSubject.ForwardedType forwarded => new ForwardedType(forwarded.Value.ReflectionName),
        NodeSubject.BaseTypes => new Folder(NodeFolder.BaseTypes),
        NodeSubject.BaseType baseType => new BaseType(baseType.Value.Type.ReflectionName),
        NodeSubject.DerivedTypes => new Folder(NodeFolder.DerivedTypes),
        NodeSubject.DerivedType derived => new DerivedType(IdStringProvider.GetIdString(derived.Value.Symbol)),
        NodeSubject.Resources => new Folder(NodeFolder.Resources),
        NodeSubject.ResourceContainer container => new Resource(container.Value.Resource.Name),
        NodeSubject.ResourceEntry entry => new Resource(entry.Value.Name),
        NodeSubject.Metadata metadata => new Metadata(ReferenceEquals(metadata.Value.File, metadata.Value.Module.File) ? MetadataSource.Own : MetadataSource.Debug),
        NodeSubject.MetadataTables => new Folder(NodeFolder.MetadataTables),
        NodeSubject.MetadataTable table => new MetadataPart(table.Value.Key),
        NodeSubject.Package package => package.Value.IsRoot ? null : new PackageFolder(package.Value.Folder.FullName),
        NodeSubject.PackageFile file => new PackageFile(file.Value.File.FullName),
        NodeSubject.Module module => module.Value.Place == null ? null : new EmbeddedModule(module.Value.Place.File.FullName),
        NodeSubject.Namespace ns => new Namespace(ns.Value.Name),
        NodeSubject.FailedAssembly => null,
        _ => throw new UnreachableException(),
    };

    /// <summary>The documentation id of <paramref name="member"/>, written from its signature as the type
    /// system reads it, as <see cref="IdStringProvider"/> writes one for a member backed by metadata, so that
    /// a member of an assembly that does not resolve has one too.</summary>
    private static string UsedMemberId(IMember member)
    {
        var id = new StringBuilder();
        id.Append(member.SymbolKind switch
        {
            SymbolKind.Field => "F:",
            SymbolKind.Property or SymbolKind.Indexer => "P:",
            SymbolKind.Event => "E:",
            _ => "M:",
        });
        id.Append(IdStringProvider.GetTypeName(member.DeclaringType)).Append('.');
        id.Append(member.Name.Replace('.', '#').Replace('<', '{').Replace('>', '}'));
        if (member is IMethod method && method.TypeParameters.Count > 0)
            id.Append("``").Append(method.TypeParameters.Count);
        if (member is IParameterizedMember parameterized && parameterized.Parameters.Count > 0)
            id.Append('(').AppendJoin(',', parameterized.Parameters.Select(static p => IdStringProvider.GetTypeName(p.Type))).Append(')');
        if (member.SymbolKind == SymbolKind.Operator && member.Name is "op_Implicit" or "op_Explicit")
            id.Append('~').Append(IdStringProvider.GetTypeName(member.ReturnType));
        return id.ToString();
    }
}

/// <summary>One step of a <see cref="NodePath.Semantic"/>: what the node is, and the label it showed when
/// the path was taken, which describes the step but does not name it.</summary>
public readonly record struct NodePathStep(NodePathSegment Segment, string Label);

/// <summary>What a node is, as builds before versioned paths told it by the kind they saved with each
/// step: a class, a record and a struct are all types, a constant is a field and an indexer a property.</summary>
public enum LegacyRole
{
    Module,
    FailedAssembly,
    Namespace,
    Type,
    Field,
    Property,
    Event,
    Method,
    ReferencesFolder,
    AssemblyReference,
    ReferencedTypes,
    TypeReference,
    MemberReference,
    ModuleReference,
    ImportedMethod,
    ForwardedType,
    BaseTypes,
    BaseType,
    DerivedTypes,
    DerivedType,
    ResourcesFolder,
    Resource,
    Metadata,
    MetadataTables,
    MetadataPart,
    Package,
}

/// <summary>One step of a <see cref="NodePath.Legacy"/>: the role the saved kind implies, and the label.</summary>
public readonly record struct LegacyPathStep(LegacyRole Role, string Label)
{
    /// <summary>The role each kind a build before versioned paths saved implies, by the kind's name.</summary>
    private static readonly Dictionary<string, LegacyRole> Kinds = new(StringComparer.Ordinal)
    {
        ["Assembly"] = LegacyRole.Module,
        ["FailedAssembly"] = LegacyRole.FailedAssembly,
        ["Namespace"] = LegacyRole.Namespace,
        ["Class"] = LegacyRole.Type,
        ["Interface"] = LegacyRole.Type,
        ["Struct"] = LegacyRole.Type,
        ["Enum"] = LegacyRole.Type,
        ["Delegate"] = LegacyRole.Type,
        ["Record"] = LegacyRole.Type,
        ["RecordStruct"] = LegacyRole.Type,
        ["Field"] = LegacyRole.Field,
        ["Constant"] = LegacyRole.Field,
        ["Property"] = LegacyRole.Property,
        ["Indexer"] = LegacyRole.Property,
        ["Event"] = LegacyRole.Event,
        ["Method"] = LegacyRole.Method,
        ["Constructor"] = LegacyRole.Method,
        ["Operator"] = LegacyRole.Method,
        ["ReferencesFolder"] = LegacyRole.ReferencesFolder,
        ["AssemblyReference"] = LegacyRole.AssemblyReference,
        ["ReferencedTypes"] = LegacyRole.ReferencedTypes,
        ["TypeReference"] = LegacyRole.TypeReference,
        ["MethodReference"] = LegacyRole.MemberReference,
        ["FieldReference"] = LegacyRole.MemberReference,
        ["PropertyReference"] = LegacyRole.MemberReference,
        ["EventReference"] = LegacyRole.MemberReference,
        ["ModuleReference"] = LegacyRole.ModuleReference,
        ["ImportedMethod"] = LegacyRole.ImportedMethod,
        ["ForwardedType"] = LegacyRole.ForwardedType,
        ["BaseTypes"] = LegacyRole.BaseTypes,
        ["BaseType"] = LegacyRole.BaseType,
        ["DerivedTypes"] = LegacyRole.DerivedTypes,
        ["DerivedType"] = LegacyRole.DerivedType,
        ["ResourcesFolder"] = LegacyRole.ResourcesFolder,
        ["Resource"] = LegacyRole.Resource,
        ["ResourcesFile"] = LegacyRole.Resource,
        ["ResourceImage"] = LegacyRole.Resource,
        ["Metadata"] = LegacyRole.Metadata,
        ["MetadataTables"] = LegacyRole.MetadataTables,
        ["MetadataHeader"] = LegacyRole.MetadataPart,
        ["MetadataDirectory"] = LegacyRole.MetadataPart,
        ["MetadataTable"] = LegacyRole.MetadataPart,
        ["MetadataHeap"] = LegacyRole.MetadataPart,
        ["Package"] = LegacyRole.Package,
        ["PackageFolder"] = LegacyRole.Package,
        ["Bundle"] = LegacyRole.Package,
    };

    /// <summary>The step a build before versioned paths saved as the kind named <paramref name="kind"/> and
    /// <paramref name="label"/>; null for a kind no build saved.</summary>
    public static LegacyPathStep? Read(string kind, string label)
    {
        if (!Kinds.TryGetValue(kind, out var role))
            return null;
        return new LegacyPathStep(role, label);
    }

    /// <summary>The role of a node of <paramref name="subject"/>.</summary>
    internal static LegacyRole RoleOf(NodeSubject subject) => subject switch
    {
        NodeSubject.TypeDefinition => LegacyRole.Type,
        NodeSubject.MemberDefinition member => member.Definition.Symbol switch
        {
            IField => LegacyRole.Field,
            IProperty => LegacyRole.Property,
            IEvent => LegacyRole.Event,
            IMethod => LegacyRole.Method,
            _ => throw new UnreachableException(),
        },
        NodeSubject.References => LegacyRole.ReferencesFolder,
        NodeSubject.AssemblyReference => LegacyRole.AssemblyReference,
        NodeSubject.ReferencedTypes => LegacyRole.ReferencedTypes,
        NodeSubject.TypeReference => LegacyRole.TypeReference,
        NodeSubject.MemberReference => LegacyRole.MemberReference,
        NodeSubject.ModuleReference => LegacyRole.ModuleReference,
        NodeSubject.ImportedMethod => LegacyRole.ImportedMethod,
        NodeSubject.ForwardedType => LegacyRole.ForwardedType,
        NodeSubject.BaseTypes => LegacyRole.BaseTypes,
        NodeSubject.BaseType => LegacyRole.BaseType,
        NodeSubject.DerivedTypes => LegacyRole.DerivedTypes,
        NodeSubject.DerivedType => LegacyRole.DerivedType,
        NodeSubject.Resources => LegacyRole.ResourcesFolder,
        NodeSubject.ResourceContainer or NodeSubject.ResourceEntry or NodeSubject.PackageFile => LegacyRole.Resource,
        NodeSubject.Metadata => LegacyRole.Metadata,
        NodeSubject.MetadataTables => LegacyRole.MetadataTables,
        NodeSubject.MetadataTable => LegacyRole.MetadataPart,
        NodeSubject.Package => LegacyRole.Package,
        NodeSubject.Module => LegacyRole.Module,
        NodeSubject.Namespace => LegacyRole.Namespace,
        NodeSubject.FailedAssembly => LegacyRole.FailedAssembly,
        _ => throw new UnreachableException(),
    };

    /// <summary>The step to <paramref name="node"/> from its parent.</summary>
    internal static LegacyPathStep Of(BrowseNode node) => new(RoleOf(node.Subject), node.Text);
}

/// <summary>
/// Where a <see cref="BrowseNode"/> sits: the listed file whose tree it is in, and the steps down from that
/// tree's root. Unlike the node, it stays meaningful after the assembly is closed or reloaded;
/// <see cref="BrowseSession.Find(LoadedAssembly, NodePath)"/> finds the node it names again. A path is
/// <see cref="Semantic"/>, naming each node by what it is, or <see cref="Legacy"/>, read from a place saved
/// by a build before paths were versioned, and naming each by its role and label.
/// </summary>
public abstract class NodePath : IEquatable<NodePath>
{
    private readonly string[] _labels;

    private NodePath(AssemblyPath filePath, string[] labels)
    {
        FilePath = filePath;
        _labels = labels;
    }

    /// <summary>The absolute path of the listed file the path starts from: an assembly, or the package
    /// an assembly was read out of.</summary>
    public AssemblyPath FilePath { get; }

    /// <summary>The label of the node this names, as it showed when the path was taken; the file's name
    /// for the root.</summary>
    public string Label => _labels.Length == 0 ? FilePath.FileName : _labels[^1];

    /// <summary>The path to <paramref name="node"/> from the root of its tree: for a node of an assembly
    /// read out of a package, from the package's root, through one step naming the assembly's file.</summary>
    public static Semantic Of(BrowseNode node)
    {
        var steps = new List<NodePathStep>();
        var n = node;
        while (n.Parent != null)
        {
            var segment = n.Segment ?? throw new UnreachableException();
            steps.Add(new NodePathStep(segment, n.Text));
            if (segment.IsPackageEntry)
            {
                n = n.Root;
                break;
            }
            n = n.Parent;
        }
        steps.Reverse();
        return new Semantic(n.Assembly.FilePath, steps.ToArray());
    }

    /// <summary>The path to the node reached from the root of the file at <paramref name="filePath"/>
    /// through <paramref name="steps"/>, as <see cref="Semantic.Steps"/> gives them.</summary>
    public static Semantic From(AssemblyPath filePath, IEnumerable<NodePathStep> steps) => new(filePath, steps.ToArray());

    /// <summary>The path a build before versioned paths saved: the root of the file at
    /// <paramref name="filePath"/>, then a step per node down.</summary>
    public static Legacy FromLegacy(AssemblyPath filePath, IEnumerable<LegacyPathStep> steps) => new(filePath, steps.ToArray());

    /// <summary>Whether this path names <paramref name="node"/>.</summary>
    public abstract bool Names(BrowseNode node);

    public abstract bool Equals(NodePath? other);

    public override bool Equals(object? obj) => obj is NodePath other && Equals(other);

    public abstract override int GetHashCode();

    public override string ToString() => _labels.Length == 0 ? FilePath.Text : $"{FilePath}: {string.Join(" › ", _labels)}";

    public sealed class Semantic : NodePath
    {
        private readonly NodePathStep[] _steps;

        internal Semantic(AssemblyPath filePath, NodePathStep[] steps) : base(filePath, steps.Select(static s => s.Label).ToArray())
        {
            _steps = steps;
        }

        /// <summary>What each node down from the root is, and its label, the node itself last.</summary>
        public IReadOnlyList<NodePathStep> Steps => _steps;

        public override bool Names(BrowseNode node) => Equals(node.Path);

        /// <summary>Equal when it starts from the same file and names the same node at each step, whatever
        /// the labels.</summary>
        public override bool Equals(NodePath? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is not Semantic semantic) return false;
            if (semantic._steps.Length != _steps.Length) return false;
            if (!FilePath.Equals(semantic.FilePath)) return false;
            for (var i = _steps.Length - 1; i >= 0; i--)
            {
                if (_steps[i].Segment != semantic._steps[i].Segment) return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(FilePath);
            foreach (var step in _steps)
                hash.Add(step.Segment);
            return hash.ToHashCode();
        }
    }

    public sealed class Legacy : NodePath
    {
        private readonly LegacyPathStep[] _steps;

        internal Legacy(AssemblyPath filePath, LegacyPathStep[] steps) : base(filePath, steps.Select(static s => s.Label).ToArray())
        {
            _steps = steps;
        }

        /// <summary>The role and label of each node down from the root, the node itself last.</summary>
        public IReadOnlyList<LegacyPathStep> Steps => _steps;

        public override bool Names(BrowseNode node)
        {
            var n = node;
            for (var i = _steps.Length - 1; i >= 0; i--)
            {
                if (n.Parent == null) return false;
                if (LegacyPathStep.Of(n) != _steps[i]) return false;
                n = n.Parent;
            }
            if (n.Parent != null) return false;
            return FilePath.Equals(n.Assembly.FilePath);
        }

        public override bool Equals(NodePath? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is not Legacy legacy) return false;
            if (!FilePath.Equals(legacy.FilePath)) return false;
            return _steps.AsSpan().SequenceEqual(legacy._steps);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(FilePath);
            foreach (var step in _steps)
                hash.Add(step);
            return hash.ToHashCode();
        }
    }
}
