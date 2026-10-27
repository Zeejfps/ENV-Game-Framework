using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>What a feature running with no view on the stack commands the view to do, which
/// <see cref="IlSpyApp"/> runs on the screen, in the order queued, before it draws the next frame.</summary>
public abstract record ViewCommand
{
    private ViewCommand()
    {
    }

    /// <summary>Bring <see cref="Tab"/>'s panel to the front, docked beside the tab <see cref="Beside"/>
    /// when it is not docked yet.</summary>
    public sealed record ShowTab(DocumentTab Tab, DocumentTab? Beside) : ViewCommand;

    /// <summary>Dock the view <see cref="Panel"/>, one of <see cref="DockPanels"/>, if it is not, and
    /// bring it to the front.</summary>
    public sealed record OpenPanel(string Panel) : ViewCommand;

    /// <summary>Return <see cref="Tab"/>'s view to <see cref="Position"/>.</summary>
    public sealed record RestoreView(DocumentTab Tab, CodeViewPosition Position) : ViewCommand;

    /// <summary>Scroll <see cref="Tab"/>'s code to <see cref="Line"/> and flash it.</summary>
    public sealed record RevealLine(DocumentTab Tab, int Line) : ViewCommand;

    /// <summary>Select <see cref="Tab"/>'s grid row <see cref="Row"/> and scroll to it.</summary>
    public sealed record RevealRow(DocumentTab Tab, int Row) : ViewCommand;

    /// <summary>Expand the tree down to <see cref="Node"/>, select it and scroll to it.</summary>
    public sealed record RevealNode(BrowseNode Node) : ViewCommand;

    /// <summary>Expand the tree's <see cref="Nodes"/>.</summary>
    public sealed record ExpandNodes(IReadOnlyList<BrowseNode> Nodes) : ViewCommand;

    /// <summary>Select the card of <see cref="Type"/> on the diagram <see cref="Tab"/> shows, and bring it
    /// into view once it is laid out.</summary>
    public sealed record FocusCard(DocumentTab Tab, CodeReference Type) : ViewCommand;

    /// <summary>Expand the analyzer's <see cref="Root"/> and select it.</summary>
    public sealed record ShowAnalysis(AnalyzerSymbolNode Root) : ViewCommand;

    /// <summary>Open the web page at <see cref="Url"/> in the browser.</summary>
    public sealed record OpenWebPage(string Url) : ViewCommand;

    /// <summary>Open the file at <see cref="Path"/> in the app the OS opens its kind of file with.</summary>
    public sealed record OpenFile(string Path) : ViewCommand;
}
