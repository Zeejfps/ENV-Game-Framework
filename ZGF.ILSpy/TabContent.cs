using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Core.Resources;

namespace ZGF.ILSpy;

/// <summary>What a tab shows: nothing, its node while it decompiles, the node's code, grid or picture,
/// an export's report, a diagram or a comparison of its own, or why it shows none of these.</summary>
public abstract record TabContent
{
    private TabContent()
    {
    }

    /// <summary>Nothing, as a new tab.</summary>
    public sealed record Empty : TabContent;

    /// <summary><see cref="Node"/>, while it decompiles or its grid or picture is read.</summary>
    public sealed record Loading(BrowseNode Node) : TabContent;

    /// <summary><see cref="Node"/>'s decompiled code.</summary>
    public sealed record Code(BrowseNode Node, DecompiledDocument Document) : TabContent;

    /// <summary>The rows of <see cref="Node"/>, which shows a grid.</summary>
    public sealed record Table(BrowseNode Node, MetadataGrid Grid) : TabContent;

    /// <summary>The picture of <see cref="Node"/>, which shows one, with its markup when it is stored as such, such as SVG.</summary>
    public sealed record Picture(BrowseNode Node, ResourceImage Image, DecompiledDocument? Source) : TabContent;

    /// <summary>The report of an export, which the screen writes out.</summary>
    public sealed record Report(ExportReport Export) : TabContent;

    /// <summary>A diagram of types, which grows as types are shown in it.</summary>
    public sealed record Diagram(TypeDiagram Types) : TabContent;

    /// <summary>What changed in the API from one assembly to another.</summary>
    public sealed record Comparison(AssemblyComparison Assemblies) : TabContent;

    /// <summary>Decompiling <see cref="Node"/> failed with <see cref="Message"/>.</summary>
    public sealed record Failed(BrowseNode Node, string Message) : TabContent;

    /// <summary>The tab waits to find the node at <see cref="Location"/> again: its assembly is reloading,
    /// or the saved documents are reopening.</summary>
    public sealed record Reloading(NodePath Location) : TabContent;

    /// <summary>The saved diagram or comparison waits for the assemblies to be listed to be read again.</summary>
    public sealed record Reopening : TabContent;

    /// <summary>The reloaded assemblies hold nothing at <see cref="Location"/> any more.</summary>
    public sealed record Gone(NodePath Location) : TabContent;
}
