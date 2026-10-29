using System.Diagnostics;
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
    public static string Name(BrowseNode node)
    {
        if (node.Subject is NodeSubject.Module module)
            return module.Value.Name;
        return node.Text;
    }

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
    public static string? Secondary(BrowseNode node) => node.Subject switch
    {
        NodeSubject.ForwardedType => Secondaries.GetValue(node, WriteForwardedTo),
        NodeSubject.TypeReference type => EmptyToNull(type.Value.Namespace),
        NodeSubject.ImportedMethod import => import.Value.DeclaringTypeName,
        NodeSubject.Module => EmptyToNull(Secondaries.GetValue(node, WriteIdentity)),
        NodeSubject.TypeDefinition or NodeSubject.MemberDefinition or NodeSubject.References or NodeSubject.AssemblyReference
            or NodeSubject.ReferencedTypes or NodeSubject.MemberReference or NodeSubject.ModuleReference or NodeSubject.BaseTypes
            or NodeSubject.BaseType or NodeSubject.DerivedTypes or NodeSubject.DerivedType or NodeSubject.Resources or NodeSubject.ResourceContainer
            or NodeSubject.ResourceEntry or NodeSubject.Metadata or NodeSubject.MetadataTables or NodeSubject.MetadataTable or NodeSubject.Package
            or NodeSubject.PackageFile or NodeSubject.Namespace or NodeSubject.FailedAssembly => null,
        _ => throw new UnreachableException(),
    };

    private static string WriteForwardedTo(BrowseNode node)
    {
        if (node.Subject is not NodeSubject.ForwardedType forwarded)
            throw new UnreachableException();
        return ForwardedToPrefix + forwarded.Value.Destination;
    }

    private static string WriteIdentity(BrowseNode node)
    {
        if (node.Subject is not NodeSubject.Module module)
            throw new UnreachableException();
        return Identity(module.Value);
    }

    private static string? EmptyToNull(string text)
    {
        if (text.Length == 0)
            return null;
        return text;
    }

    /// <summary>The framework picked for an assembly to resolve its references against, as a moniker,
    /// which its row shows bold after <see cref="Secondary"/>; null for any other node, and for an
    /// assembly that resolves against the framework it was built for.</summary>
    public static string? PickedFramework(BrowseNode node)
    {
        if (node.Subject is not NodeSubject.Module module)
            return null;
        if (!module.Value.IsTargetFrameworkPicked)
            return null;
        return module.Value.FrameworkMoniker;
    }

    /// <summary>An assembly as a column of results names it: its name and version, and for one read out
    /// of a package, the folder holding it there, e.g. <c>Newtonsoft.Json (13.0.0.0) · lib/net6.0</c>.</summary>
    public static string AssemblyLabel(LoadedAssembly assembly) => AssemblyLabels.GetValue(assembly, WriteAssemblyLabel);

    private static string WriteAssemblyLabel(LoadedAssembly assembly)
    {
        if (assembly is not LoadedModule module || module.Place == null)
            return assembly.NameAndVersion;
        return $"{assembly.NameAndVersion}{Separator}{module.Place.File.Folder}";
    }

    /// <summary>The node's metadata token as <paramref name="format"/> writes it, e.g. <c>@06000001</c>
    /// in hex. Null when the node has none or tokens are hidden.</summary>
    public static string? Token(BrowseNode node, TokenFormat format)
    {
        if (NodeDefinitions.TokenOf(node.Subject) == null)
            return null;
        if (format == TokenFormat.Hex)
            return HexTokens.GetValue(node, WriteHexToken);
        if (format == TokenFormat.Decimal)
            return DecimalTokens.GetValue(node, WriteDecimalToken);
        return null;
    }

    private static string WriteHexToken(BrowseNode node) => TokenPrefix + DefinedToken(node).ToString(HexTokenFormat, CultureInfo.InvariantCulture);

    private static string WriteDecimalToken(BrowseNode node) => TokenPrefix + DefinedToken(node).ToString(CultureInfo.InvariantCulture);

    /// <summary>The token of what <paramref name="node"/> defines, asked for only of one that defines something.</summary>
    private static int DefinedToken(BrowseNode node) => NodeDefinitions.TokenOf(node.Subject) ?? throw new UnreachableException();

    /// <summary>An assembly's version and framework; with a picked framework, which its row shows bold
    /// on its own, the version and the separator before it.</summary>
    private static string Identity(LoadedModule assembly)
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
        if (node.Subject is NodeSubject.Module module)
        {
            var assembly = module.Value;
            return string.Join("\n", new[] { assembly.FullName, assembly.FrameworkName, assembly.FilePath.Text }.OfType<string>());
        }
        if (node.Subject is NodeSubject.FailedAssembly failed)
            return failed.Value.Error.Message;
        var name = node.FullName;
        var accessibility = Accessibility(NodeVisibility.Of(node.Subject).Accessibility);
        return accessibility != null ? $"{name}\n{accessibility}" : name;
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
