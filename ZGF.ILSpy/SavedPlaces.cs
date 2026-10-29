using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy;

/// <summary>A step down the tree as builds before versioned paths save and read it: the name of the
/// node's kind and its label.</summary>
public sealed record LegacyPlaceStep(string Kind, string Text);

/// <summary>A place as the settings and bookmark files keep it, in the format of
/// <see cref="Version"/>: a step per node down from the root of the listed file.</summary>
public sealed record SavedPath(int Version, List<SavedPathStep> Steps)
{
    public const int CurrentVersion = 1;
}

/// <summary>A step of a <see cref="SavedPath"/>: what the node is, written as
/// <see cref="SavedPlaces.Write"/> writes it, and the label it showed.</summary>
public sealed record SavedPathStep(string Step, string Label);

/// <summary>Writing places for the settings and bookmark files, and reading back what they wrote.</summary>
public static class SavedPlaces
{
    /// <summary><paramref name="place"/> as the files keep it; null for a place read from a build before
    /// versioned paths, which keeps only its <see cref="LegacyPlaceStep"/>s.</summary>
    public static SavedPath? PathOf(NodePath place) => place switch
    {
        NodePath.Semantic semantic => new SavedPath(SavedPath.CurrentVersion,
            semantic.Steps.Select(static s => new SavedPathStep(Write(s.Segment), s.Label)).ToList()),
        NodePath.Legacy => null,
        _ => throw new UnreachableException(),
    };

    /// <summary>The steps to <paramref name="node"/> as builds before versioned paths read them, one per
    /// node down from the root of its tree.</summary>
    public static List<LegacyPlaceStep> LegacyPlaceOf(BrowseNode node)
    {
        var steps = new List<LegacyPlaceStep>();
        for (var n = node; n.Parent != null; n = n.Parent)
            steps.Add(new LegacyPlaceStep(n.Kind.ToString(), n.Text));
        steps.Reverse();
        return steps;
    }

    /// <summary>The path <paramref name="steps"/> lead along from the root of the file at
    /// <paramref name="filePath"/>; null when a step names a kind no build saved.</summary>
    public static NodePath.Legacy? LegacyPathOf(AssemblyPath filePath, List<LegacyPlaceStep> steps)
    {
        var read = new List<LegacyPathStep>(steps.Count);
        foreach (var step in steps)
        {
            var legacy = LegacyPathStep.Read(step.Kind, step.Text);
            if (legacy == null)
                return null;
            read.Add(legacy.Value);
        }
        return NodePath.FromLegacy(filePath, read);
    }

    /// <summary><paramref name="segment"/> as a <see cref="SavedPathStep"/> holds it: its case, a space,
    /// then what it names, such as <c>Definition T:Ns.Calculator`1</c>.</summary>
    public static string Write(NodePathSegment segment) => segment switch
    {
        NodePathSegment.Namespace ns => $"Namespace {ns.Name}",
        NodePathSegment.Definition definition => $"Definition {definition.Id}",
        NodePathSegment.Folder folder => $"Folder {folder.Role}",
        NodePathSegment.Metadata metadata => $"Metadata {metadata.Which}",
        NodePathSegment.MetadataPart part => $"MetadataPart {Write(part.Part)}",
        NodePathSegment.AssemblyReference reference => $"AssemblyReference {reference.Name}",
        NodePathSegment.ModuleReference reference => $"ModuleReference {reference.Name}",
        NodePathSegment.ImportedMethod import => $"ImportedMethod {import.Id}",
        NodePathSegment.UsedType type => $"UsedType {type.FullName}",
        NodePathSegment.UsedMember member => $"UsedMember {member.Id}",
        NodePathSegment.ForwardedType forwarded => $"ForwardedType {forwarded.FullName}",
        NodePathSegment.BaseType baseType => $"BaseType {baseType.Name}",
        NodePathSegment.DerivedType derived => $"DerivedType {derived.Id}",
        NodePathSegment.Resource resource => $"Resource {resource.Name}",
        NodePathSegment.PackageFolder folder => $"PackageFolder {folder.Path}",
        NodePathSegment.PackageFile file => $"PackageFile {file.Path}",
        NodePathSegment.EmbeddedModule module => $"EmbeddedModule {module.Path}",
        _ => throw new UnreachableException(),
    };

    private static string Write(MetadataPartKey part) => part switch
    {
        MetadataPartKey.Header header => $"Header {header.Which}",
        MetadataPartKey.DataDirectories => "DataDirectories",
        MetadataPartKey.DebugDirectory => "DebugDirectory",
        MetadataPartKey.Table table => $"Table {table.Index}",
        MetadataPartKey.Heap heap => $"Heap {heap.Name}",
        MetadataPartKey.DebugEntry entry => $"DebugEntry {entry.Index} {entry.Type}",
        _ => throw new UnreachableException(),
    };

    /// <summary>The segment <paramref name="text"/> holds, as <see cref="Write(NodePathSegment)"/> writes
    /// one; null when it holds none.</summary>
    public static NodePathSegment? ParseSegment(string text)
    {
        var (tag, key) = Split(text);
        if (tag == "Namespace")
            return new NodePathSegment.Namespace(key);
        if (key.Length == 0)
            return null;
        return tag switch
        {
            "Definition" => new NodePathSegment.Definition(key),
            "Folder" => ParseFolder(key),
            "Metadata" => ParseMetadata(key),
            "MetadataPart" => ParseMetadataPart(key),
            "AssemblyReference" => new NodePathSegment.AssemblyReference(key),
            "ModuleReference" => new NodePathSegment.ModuleReference(key),
            "ImportedMethod" => new NodePathSegment.ImportedMethod(key),
            "UsedType" => new NodePathSegment.UsedType(key),
            "UsedMember" => new NodePathSegment.UsedMember(key),
            "ForwardedType" => new NodePathSegment.ForwardedType(key),
            "BaseType" => new NodePathSegment.BaseType(key),
            "DerivedType" => new NodePathSegment.DerivedType(key),
            "Resource" => new NodePathSegment.Resource(key),
            "PackageFolder" => new NodePathSegment.PackageFolder(key),
            "PackageFile" => new NodePathSegment.PackageFile(key),
            "EmbeddedModule" => new NodePathSegment.EmbeddedModule(key),
            _ => null,
        };
    }

    private static NodePathSegment.Folder? ParseFolder(string text)
    {
        var role = ParseName<NodeFolder>(text);
        if (role == null)
            return null;
        return new NodePathSegment.Folder(role.Value);
    }

    private static NodePathSegment.Metadata? ParseMetadata(string text)
    {
        var which = ParseName<MetadataSource>(text);
        if (which == null)
            return null;
        return new NodePathSegment.Metadata(which.Value);
    }

    private static NodePathSegment.MetadataPart? ParseMetadataPart(string text)
    {
        var part = ParsePart(text);
        if (part == null)
            return null;
        return new NodePathSegment.MetadataPart(part);
    }

    private static MetadataPartKey? ParsePart(string text)
    {
        var (tag, key) = Split(text);
        return tag switch
        {
            "Header" => ParseHeader(key),
            "DataDirectories" when key.Length == 0 => new MetadataPartKey.DataDirectories(),
            "DebugDirectory" when key.Length == 0 => new MetadataPartKey.DebugDirectory(),
            "Table" => ParseTable(key),
            "Heap" when key.Length > 0 => new MetadataPartKey.Heap(key),
            "DebugEntry" => ParseDebugEntry(key),
            _ => null,
        };
    }

    private static MetadataPartKey.Header? ParseHeader(string text)
    {
        var which = ParseName<PEHeaderKind>(text);
        if (which == null)
            return null;
        return new MetadataPartKey.Header(which.Value);
    }

    private static MetadataPartKey.Table? ParseTable(string text)
    {
        var table = ParseName<TableIndex>(text);
        if (table == null)
            return null;
        return new MetadataPartKey.Table(table.Value);
    }

    /// <summary>A debug directory entry as <c>2 CodeView</c>: where it stands, then its type, which for a
    /// type .NET does not name is its number.</summary>
    private static MetadataPartKey.DebugEntry? ParseDebugEntry(string text)
    {
        var (index, type) = Split(text);
        if (!int.TryParse(index, out var at) || at < 0)
            return null;
        if (!Enum.TryParse<DebugDirectoryEntryType>(type, out var parsed))
            return null;
        return new MetadataPartKey.DebugEntry(at, parsed);
    }

    /// <summary>The value of <typeparamref name="T"/> named <paramref name="name"/>; null for any other
    /// text, a number among them.</summary>
    private static T? ParseName<T>(string name) where T : struct, Enum
    {
        if (!Enum.GetNames<T>().Contains(name))
            return null;
        return Enum.Parse<T>(name);
    }

    /// <summary>The text before the first space, and what follows it; all of it and nothing without one.</summary>
    private static (string Head, string Tail) Split(string text)
    {
        var space = text.IndexOf(' ');
        if (space < 0)
            return (text, "");
        return (text.Substring(0, space), text.Substring(space + 1));
    }
}
