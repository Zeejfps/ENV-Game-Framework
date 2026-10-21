using System.Runtime.CompilerServices;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>How a node reads in the UI: a short name for rows and tabs, and its full identity for
/// tooltips, built once per node.</summary>
public static class NodeText
{
    private static readonly ConditionalWeakTable<BrowseNode, string> Tooltips = new();
    private static readonly ConditionalWeakTable<BrowseNode, string> Secondaries = new();

    /// <summary>The node's own name: an assembly's simple name, any other node's label.</summary>
    public static string Name(BrowseNode node) => node.Kind == NodeKind.Assembly ? node.Assembly.Name : node.Text;

    /// <summary>The node's full identity: for an assembly its display name, framework and file; for
    /// an entity its full name and, unless public, its accessibility.</summary>
    public static string Tooltip(BrowseNode node) => Tooltips.GetValue(node, Describe);

    /// <summary>What a row shows after the node's name in muted text: an assembly's version and
    /// target framework, e.g. <c>10.0.0.0 · net10.0</c>. Null for other nodes.</summary>
    public static string? Secondary(BrowseNode node) =>
        node.Kind == NodeKind.Assembly && Secondaries.GetValue(node, static n => Identity(n.Assembly)) is { Length: > 0 } text ? text : null;

    private static string Identity(LoadedAssembly assembly)
    {
        var version = assembly.Version?.ToString();
        var framework = ShortFramework(assembly.TargetFramework);
        return version == null ? framework ?? "" : framework == null ? version : $"{version} · {framework}";
    }

    private static string? ShortFramework(string? id)
    {
        if (id == null) return null;
        var at = id.IndexOf(",Version=v", StringComparison.Ordinal);
        if (at < 0) return id;
        var name = id[..at];
        var version = id[(at + ",Version=v".Length)..];
        return name switch
        {
            ".NETCoreApp" => System.Version.TryParse(version, out var v) && v.Major >= 5 ? $"net{version}" : $"netcoreapp{version}",
            ".NETStandard" => $"netstandard{version}",
            ".NETFramework" => $"net{version.Replace(".", "")}",
            _ => $"{name} {version}",
        };
    }

    private static string Describe(BrowseNode node)
    {
        if (node.Kind == NodeKind.Assembly)
        {
            var assembly = node.Assembly;
            var framework = assembly.TargetFramework?.Replace(",Version=", ", ");
            return framework == null ? $"{assembly.FullName}\n{assembly.FilePath}" : $"{assembly.FullName}\n{framework}\n{assembly.FilePath}";
        }
        var name = node.FullName;
        return Accessibility(node.Accessibility) is { } accessibility ? $"{name}\n{accessibility}" : name;
    }

    private static string? Accessibility(NodeAccessibility accessibility) => accessibility switch
    {
        NodeAccessibility.Protected => "protected",
        NodeAccessibility.ProtectedInternal => "protected internal",
        NodeAccessibility.Internal => "internal",
        NodeAccessibility.PrivateProtected => "private protected",
        NodeAccessibility.Private => "private",
        _ => null,
    };
}
