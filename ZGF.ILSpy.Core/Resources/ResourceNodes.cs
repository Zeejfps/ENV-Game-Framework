using System.Globalization;
using System.Reflection;
using System.Text;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Util;
using ZGF.ILSpy.Core.Metadata;
using MetadataResource = ICSharpCode.Decompiler.Metadata.Resource;

namespace ZGF.ILSpy.Core.Resources;

/// <summary>A node standing for a resource's bytes, which can be read to save them.</summary>
internal interface IResourceData
{
    /// <summary>What the resource is, as a resources folder lists it.</summary>
    string Description { get; }

    /// <summary>The name a saved copy is offered under.</summary>
    string FileName { get; }

    /// <summary>What a saved copy holds.</summary>
    byte[] ReadData();
}

internal static class ResourceNodes
{
    public static string Describe(MetadataResource resource)
    {
        var length = resource.TryGetLength();
        var size = length != null ? $", {length} bytes" : "";
        return $"{resource.Name} ({resource.ResourceType}{size})";
    }

    public static byte[] Read(MetadataResource resource)
    {
        using var stream = resource.TryOpenStream() ?? throw new IOException($"{resource.Name} cannot be read.");
        stream.Position = 0;
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    public static (bool IsPublic, NodeAccessibility Accessibility) Visibility(MetadataResource resource) =>
        (resource.Attributes & ManifestResourceAttributes.VisibilityMask) == ManifestResourceAttributes.Public
            ? (true, NodeAccessibility.Public)
            : (false, NodeAccessibility.Private);

    /// <summary>The entries in name order. The containers WPF generates key their entries by a
    /// URI-escaped path, which is unescaped as ILSpy does.</summary>
    public static List<KeyValuePair<string, object?>> ReadEntries(MetadataResource resource)
    {
        using var stream = new MemoryStream(Read(resource));
        IEnumerable<KeyValuePair<string, object?>> entries = new ResourcesFile(stream).ToList();
        if (WholeProjectDecompiler.IsWpfGeneratedResourceContainer(resource.Name))
            entries = entries.Select(static e => new KeyValuePair<string, object?>(Uri.UnescapeDataString(e.Key), e.Value));
        return entries.OrderBy(static e => e.Key, NaturalStringComparer.Instance).ToList();
    }

    /// <summary>The grid of the entries: each one's name, its value as text, and the type of value.</summary>
    public static GridSchema<(string Name, string Value, string Type)> Schema(LoadedModule assembly, Lazy<List<KeyValuePair<string, object?>>> entries) =>
        new GridSchema<(string Name, string Value, string Type)>("Resources", assembly.File, () => entries.Value.Select(static e => Row(e.Key, e.Value)))
            .Value("Name", static r => r.Name)
            .Value("Value", static r => r.Value, tooltip: static r => r.Value.Length > 80 || r.Value.Contains('\n') ? r.Value : null)
            .Value("Type", static r => r.Type);

    private static (string Name, string Value, string Type) Row(string name, object? value) =>
        value switch
        {
            null => (name, "null", ""),
            string text => (name, text, "System.String"),
            byte[] bytes => (name, $"{bytes.Length} bytes", "System.Byte[]"),
            MemoryStream stream => (name, $"{stream.Length} bytes", "System.IO.MemoryStream"),
            ResourceSerializedObject serialized => (name, "<serialized>", serialized.TypeName ?? ""),
            _ => (name, Convert.ToString(value, CultureInfo.InvariantCulture) ?? "", value.GetType().FullName ?? value.GetType().Name),
        };
}
