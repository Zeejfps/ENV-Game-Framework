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
    private const string ForwardedToPrefix = "→ ";
    private const string Separator = " · ";

    private static readonly ConditionalWeakTable<BrowseNode, string> Tooltips = new();
    private static readonly ConditionalWeakTable<BrowseNode, string> Secondaries = new();
    private static readonly ConditionalWeakTable<BrowseNode, string> HexTokens = new();
    private static readonly ConditionalWeakTable<BrowseNode, string> DecimalTokens = new();
    private static readonly ConditionalWeakTable<LoadedAssembly, string> AssemblyLabels = new();

    /// <summary>The node's own name: an assembly's simple name, any other node's label.</summary>
    public static string Name(BrowseNode node) => node.Kind == NodeKind.Assembly ? node.Assembly.Name : node.Text;

    /// <summary>A namespace's name as its row shows it nested in another: its last segment, e.g.
    /// <c>Generic</c> for <c>System.Collections.Generic</c>.</summary>
    public static string NestedName(BrowseNode node) => NamespaceNames.LastSegment(node.Text);

    /// <summary>The node's full identity: for an assembly its display name, framework and file; for
    /// an entity its full name and, unless public, its accessibility.</summary>
    public static string Tooltip(BrowseNode node) => Tooltips.GetValue(node, Describe);

    /// <summary>What a row shows after the node's name in muted text: an assembly's version and
    /// target framework, e.g. <c>10.0.0.0 · net10.0</c>, the framework left to <see cref="PickedFramework"/>
    /// when one was picked, the assembly a forwarded type is forwarded to, e.g. <c>→ System.Runtime</c>,
    /// the namespace of a type used from a reference, or the type declaring a method imported from a
    /// module. Null for other nodes.</summary>
    public static string? Secondary(BrowseNode node)
    {
        if (node.Kind == NodeKind.ForwardedType)
            return Secondaries.GetValue(node, WriteForwardedTo);
        if (node.Kind == NodeKind.TypeReference)
            return UsedNamespace(node);
        if (node.Kind == NodeKind.ImportedMethod)
            return node.DeclaringTypeName;
        if (node.Kind != NodeKind.Assembly)
            return null;
        var identity = Secondaries.GetValue(node, static n => Identity(n.Assembly));
        if (identity.Length == 0)
            return null;
        return identity;
    }

    private static string WriteForwardedTo(BrowseNode node) => ForwardedToPrefix + node.ForwardedTo;

    private static string? UsedNamespace(BrowseNode node)
    {
        if (string.IsNullOrEmpty(node.Namespace))
            return null;
        return node.Namespace;
    }

    /// <summary>The framework picked for an assembly to resolve its references against, as a moniker,
    /// which its row shows bold after <see cref="Secondary"/>; null for any other node, and for an
    /// assembly that resolves against the framework it was built for.</summary>
    public static string? PickedFramework(BrowseNode node)
    {
        if (node.Kind != NodeKind.Assembly)
            return null;
        if (!node.Assembly.IsTargetFrameworkPicked)
            return null;
        return node.Assembly.FrameworkMoniker;
    }

    /// <summary>An assembly as a column of results names it: its name and version, and for one read out
    /// of a package, the folder holding it there, e.g. <c>Newtonsoft.Json (13.0.0.0) · lib/net6.0</c>.</summary>
    public static string AssemblyLabel(LoadedAssembly assembly) => AssemblyLabels.GetValue(assembly, WriteAssemblyLabel);

    private static string WriteAssemblyLabel(LoadedAssembly assembly)
    {
        var place = assembly.InPackage;
        if (place == null)
            return assembly.NameAndVersion;
        return $"{assembly.NameAndVersion}{Separator}{place.File.Folder}";
    }

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

    /// <summary>An assembly's version and framework; with a picked framework, which its row shows bold
    /// on its own, the version and the separator before it.</summary>
    private static string Identity(LoadedAssembly assembly)
    {
        var version = assembly.Version?.ToString();
        if (!assembly.IsTargetFrameworkPicked)
            return string.Join(Separator, new[] { version, assembly.FrameworkMoniker }.OfType<string>());
        if (version == null)
            return "";
        return version + Separator.TrimEnd();
    }

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
