using System.Globalization;
using System.Runtime.CompilerServices;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>How a node reads in the UI: a short name for rows and tabs, its full identity for
/// tooltips and its metadata token, built once per node.</summary>
public static class NodeText
{
    private const string TokenPrefix = "@";
    private const string HexTokenFormat = "X8";

    private static readonly ConditionalWeakTable<BrowseNode, string> Tooltips = new();
    private static readonly ConditionalWeakTable<BrowseNode, string> Secondaries = new();
    private static readonly ConditionalWeakTable<BrowseNode, string> HexTokens = new();
    private static readonly ConditionalWeakTable<BrowseNode, string> DecimalTokens = new();

    /// <summary>The node's own name: an assembly's simple name, any other node's label.</summary>
    public static string Name(BrowseNode node) => node.Kind == NodeKind.Assembly ? node.Assembly.Name : node.Text;

    /// <summary>The node's full identity: for an assembly its display name, framework and file; for
    /// an entity its full name and, unless public, its accessibility.</summary>
    public static string Tooltip(BrowseNode node) => Tooltips.GetValue(node, Describe);

    /// <summary>What a row shows after the node's name in muted text: an assembly's version and
    /// target framework, e.g. <c>10.0.0.0 · net10.0</c>. Null for other nodes.</summary>
    public static string? Secondary(BrowseNode node) =>
        node.Kind == NodeKind.Assembly && Secondaries.GetValue(node, static n => Identity(n.Assembly)) is { Length: > 0 } text ? text : null;

    /// <summary>The node's metadata token as <paramref name="format"/> writes it, e.g. <c>@06000001</c>
    /// in hex. Null when the node has none or tokens are hidden.</summary>
    public static string? Token(BrowseNode node, TokenFormat format)
    {
        if (node.Token == null)
            return null;
        if (format == TokenFormat.Hex)
            return HexTokens.GetValue(node, WriteHexToken);
        if (format == TokenFormat.Decimal)
            return DecimalTokens.GetValue(node, WriteDecimalToken);
        return null;
    }

    private static string WriteHexToken(BrowseNode node) => TokenPrefix + node.Token!.Value.ToString(HexTokenFormat, CultureInfo.InvariantCulture);

    private static string WriteDecimalToken(BrowseNode node) => TokenPrefix + node.Token!.Value.ToString(CultureInfo.InvariantCulture);

    private static string Identity(LoadedAssembly assembly) =>
        string.Join(" · ", new[] { assembly.Version?.ToString(), assembly.FrameworkMoniker }.OfType<string>());

    private static string Describe(BrowseNode node)
    {
        if (node.Kind == NodeKind.Assembly)
        {
            var assembly = node.Assembly;
            return string.Join("\n", new[] { assembly.FullName, assembly.FrameworkName, assembly.FilePath }.OfType<string>());
        }
        if (node.Kind == NodeKind.FailedAssembly)
            return node.Assembly.LoadError!;
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
