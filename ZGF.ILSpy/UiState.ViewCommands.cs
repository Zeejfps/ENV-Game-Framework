using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

public sealed partial class UiState
{
    /// <summary>Runs the commands the features queued for the view, in the order they were queued, dropping
    /// commands about a node, tab, line or row that is gone since.</summary>
    private void RunViewCommands()
    {
        var commands = _appState.ViewCommands;
        while (commands.TryDequeue(out var command))
            Run(command);
    }

    private void Run(ViewCommand command)
    {
        switch (command)
        {
            case ViewCommand.ShowTab show:
                ShowTab(show.Tab, show.Beside);
                break;
            case ViewCommand.OpenPanel open:
                OpenPanel(open.Panel);
                break;
            case ViewCommand.RevealNode reveal:
                RevealNode(reveal.Node);
                break;
            case ViewCommand.ExpandNodes expand:
                ExpandNodes(expand.Nodes);
                break;
            case ViewCommand.ShowAnalysis show:
                ShowAnalysis(show.Root);
                break;
            case ViewCommand.RestoreView restore:
                RestoreView(restore.Tab, restore.Position);
                break;
            case ViewCommand.RevealLine reveal:
                RevealLine(reveal.Tab, reveal.Line);
                break;
            case ViewCommand.RevealRow reveal:
                RevealRow(reveal.Tab, reveal.Row);
                break;
        }
    }

    private void RevealNode(BrowseNode node)
    {
        if (!_appState.IsListed(node))
            return;
        Tree.Reveal(node);
    }

    private void ExpandNodes(IReadOnlyList<BrowseNode> nodes)
    {
        var listed = nodes
            .Where(_appState.IsListed)
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
        if (!_appState.Tabs.Contains(tab))
            return null;
        return _tabViews.GetValueOrDefault(tab);
    }
}
