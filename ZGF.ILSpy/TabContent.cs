using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Core.Resources;

namespace ZGF.ILSpy;

/// <summary>What a tab shows: nothing, its node while it decompiles, the node's code, grid or picture,
/// a document of its own, or why it shows none of these.</summary>
public abstract record TabContent
{
    private TabContent()
    {
    }

    /// <summary>Nothing, as a new tab.</summary>
    public sealed record Empty : TabContent;

    /// <summary><see cref="Node"/>, while it decompiles or its grid or picture is read.</summary>
    public sealed record Loading(BrowseNode Node) : TabContent;

    /// <summary>The node's decompiled code.</summary>
    public sealed record Code(DecompiledDocument Document) : TabContent;

    /// <summary>The rows of a node that shows a grid.</summary>
    public sealed record Table(MetadataGrid Grid) : TabContent;

    /// <summary>The picture of a node that shows one, with its markup when it is stored as such, such as SVG.</summary>
    public sealed record Picture(ResourceImage Image, DecompiledDocument? Source) : TabContent;

    /// <summary>A document no node stands for, such as a report.</summary>
    public sealed record Report(DecompiledDocument Document) : TabContent;

    /// <summary>Decompiling the node failed with <see cref="Message"/>.</summary>
    public sealed record Failed(string Message) : TabContent;

    /// <summary>The node's assembly is reloading, and the tab waits to find the node again.</summary>
    public sealed record Reloading : TabContent;

    /// <summary>The reloaded assemblies hold nothing at <see cref="Location"/> any more.</summary>
    public sealed record Gone(NodePath Location) : TabContent;
}
