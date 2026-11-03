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

/// <summary>What a node is among its siblings, as a <see cref="NodePath"/> names it: by what it
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

/// <summary>One step of a <see cref="NodePath"/>: what the node is, and the label it showed when
/// the path was taken, which describes the step but does not name it.</summary>
public readonly record struct NodePathStep(NodePathSegment Segment, string Label);

/// <summary>
/// Where a <see cref="BrowseNode"/> sits: the listed file whose tree it is in, and the steps down from that
/// tree's root, naming each node by what it is. Unlike the node, it stays meaningful after the assembly is
/// closed or reloaded; <see cref="BrowseSession.Find(LoadedAssembly, NodePath)"/> finds the node it names again.
/// </summary>
public sealed class NodePath : IEquatable<NodePath>
{
    private readonly NodePathStep[] _steps;

    private NodePath(AssemblyPath filePath, NodePathStep[] steps)
    {
        FilePath = filePath;
        _steps = steps;
    }

    /// <summary>The absolute path of the listed file the path starts from: an assembly, or the package
    /// an assembly was read out of.</summary>
    public AssemblyPath FilePath { get; }

    /// <summary>What each node down from the root is, and its label, the node itself last.</summary>
    public IReadOnlyList<NodePathStep> Steps => _steps;

    /// <summary>The label of the node this names, as it showed when the path was taken; the file's name
    /// for the root.</summary>
    public string Label => _steps.Length == 0 ? FilePath.FileName : _steps[^1].Label;

    /// <summary>The path to <paramref name="node"/> from the root of its tree: for a node of an assembly
    /// read out of a package, from the package's root, through one step naming the assembly's file.</summary>
    public static NodePath Of(BrowseNode node)
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
        return new NodePath(n.Assembly.FilePath, steps.ToArray());
    }

    /// <summary>The path to the node reached from the root of the file at <paramref name="filePath"/>
    /// through <paramref name="steps"/>, as <see cref="Steps"/> gives them.</summary>
    public static NodePath From(AssemblyPath filePath, IEnumerable<NodePathStep> steps) => new(filePath, steps.ToArray());

    /// <summary>Equal when it starts from the same file and names the same node at each step, whatever
    /// the labels.</summary>
    public bool Equals(NodePath? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other == null) return false;
        if (other._steps.Length != _steps.Length) return false;
        if (!FilePath.Equals(other.FilePath)) return false;
        for (var i = _steps.Length - 1; i >= 0; i--)
        {
            if (_steps[i].Segment != other._steps[i].Segment) return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is NodePath other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(FilePath);
        foreach (var step in _steps)
            hash.Add(step.Segment);
        return hash.ToHashCode();
    }

    public override string ToString() => _steps.Length == 0 ? FilePath.Text : $"{FilePath}: {string.Join(" › ", _steps.Select(static s => s.Label))}";
}
