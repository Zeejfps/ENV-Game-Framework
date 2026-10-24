using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

public sealed partial class UiState
{
    /// <summary>Does what the features asked of the view, in the order they asked, dropping what asks
    /// about a node, tab, line or row that is gone since.</summary>
    private void TakeViewAsks()
    {
        var asks = _workspace.ViewAsks;
        while (asks.TryDequeue(out var ask))
            Take(ask);
    }

    private void Take(ViewAsk ask)
    {
        switch (ask)
        {
            case ViewAsk.ShowTab show:
                ShowTab(show.Tab, show.Beside);
                break;
            case ViewAsk.OpenPanel open:
                OpenPanel(open.Panel);
                break;
            case ViewAsk.RevealNode reveal:
                RevealNode(reveal.Node);
                break;
            case ViewAsk.ExpandNodes expand:
                ExpandNodes(expand.Nodes);
                break;
            case ViewAsk.ShowAnalysis show:
                ShowAnalysis(show.Root);
                break;
            case ViewAsk.RestoreView restore:
                RestoreView(restore.Tab, restore.Position);
                break;
            case ViewAsk.RevealLine reveal:
                RevealLine(reveal.Tab, reveal.Line);
                break;
            case ViewAsk.RevealRow reveal:
                RevealRow(reveal.Tab, reveal.Row);
                break;
        }
    }

    private void RevealNode(BrowseNode node)
    {
        if (!_workspace.IsListed(node))
            return;
        Tree.Reveal(node);
    }

    private void ExpandNodes(IReadOnlyList<BrowseNode> nodes)
    {
        var listed = nodes
            .Where(_workspace.IsListed)
            .ToList();
        Tree.Expand(listed);
    }

    private void RestoreView(DocumentTab tab, CodeViewPosition position)
    {
        var view = FindOpenView(tab);
        if (view == null)
            return;
        if (tab.Grid == null)
        {
            view.Code.Restore(position);
            return;
        }
        view.Grid.Restore(position);
    }

    private void RevealLine(DocumentTab tab, int line)
    {
        var view = FindOpenView(tab);
        if (view == null)
            return;
        if (line >= tab.Document.Lines.Length)
            return;
        view.Code.RevealLine(line);
    }

    private void RevealRow(DocumentTab tab, int row)
    {
        var view = FindOpenView(tab);
        if (view == null)
            return;
        if (!HasRow(tab, row))
            return;
        view.Grid.Reveal(row);
    }

    private static bool HasRow(DocumentTab tab, int row)
    {
        var grid = tab.Grid;
        if (grid == null)
            return false;
        return row < grid.RowCount;
    }

    /// <summary>The view of <paramref name="tab"/> while it is open, or null.</summary>
    private TabView? FindOpenView(DocumentTab tab)
    {
        if (!_workspace.Tabs.Contains(tab))
            return null;
        return _tabViews.GetValueOrDefault(tab);
    }
}
