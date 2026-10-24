using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>What a feature running with no view on the stack asks of the view, which
/// <see cref="UiState"/> takes, in the order asked, before it draws the next frame.</summary>
public abstract record ViewAsk
{
    private ViewAsk()
    {
    }

    /// <summary>Bring <see cref="Tab"/>'s panel to the front, docked beside the panel <see cref="Beside"/>
    /// when it is not docked yet.</summary>
    public sealed record ShowTab(DocumentTab Tab, string? Beside) : ViewAsk;

    /// <summary>Dock the view <see cref="Panel"/>, one of <see cref="DockPanels"/>, if it is not, and
    /// bring it to the front.</summary>
    public sealed record OpenPanel(string Panel) : ViewAsk;

    /// <summary>Decompile again what each tab in front of its group shows, when the language or the
    /// options changed since.</summary>
    public sealed record RefreshShownTabs : ViewAsk;

    /// <summary>Return <see cref="Tab"/>'s view to <see cref="Position"/>.</summary>
    public sealed record RestoreView(DocumentTab Tab, CodeViewPosition Position) : ViewAsk;

    /// <summary>Scroll <see cref="Tab"/>'s code to <see cref="Line"/> and flash it.</summary>
    public sealed record RevealLine(DocumentTab Tab, int Line) : ViewAsk;

    /// <summary>Select <see cref="Tab"/>'s grid row <see cref="Row"/> and scroll to it.</summary>
    public sealed record RevealRow(DocumentTab Tab, int Row) : ViewAsk;

    /// <summary>Expand the tree down to <see cref="Node"/>, select it and scroll to it.</summary>
    public sealed record RevealNode(BrowseNode Node) : ViewAsk;

    /// <summary>Expand the tree's <see cref="Nodes"/>.</summary>
    public sealed record ExpandNodes(IReadOnlyList<BrowseNode> Nodes) : ViewAsk;

    /// <summary>Expand the analyzer's <see cref="Root"/> and select it.</summary>
    public sealed record ShowAnalysis(AnalyzerSymbolNode Root) : ViewAsk;
}
