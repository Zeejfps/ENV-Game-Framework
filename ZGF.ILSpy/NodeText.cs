using System.Runtime.CompilerServices;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>How a node reads in the UI: a short name for rows and tabs, and its full identity for
/// tooltips, built once per node.</summary>
public static class NodeText
{
    private static readonly ConditionalWeakTable<BrowseNode, string> Tooltips = new();

    /// <summary>The node's own name: an assembly's simple name, any other node's label.</summary>
    public static string Name(BrowseNode node) => node.Kind == NodeKind.Assembly ? node.Assembly.Name : node.Text;

    /// <summary>The node's full identity: for an assembly its display name, framework and file; for
    /// an entity its full name and, unless public, its accessibility.</summary>
    public static string Tooltip(BrowseNode node) => Tooltips.GetValue(node, Describe);

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
