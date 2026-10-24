namespace ZGF.ILSpy;

public sealed partial class UiState
{
    /// <summary>Does what the features asked of the view, in the order they asked.</summary>
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

    private void RestoreView(DocumentTab tab, CodeViewPosition position)
    {
        if (!_tabViews.TryGetValue(tab, out var view))
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
        if (!_tabViews.TryGetValue(tab, out var view))
            return;
        view.Code.RevealLine(line);
    }

    private void RevealRow(DocumentTab tab, int row)
    {
        if (!_tabViews.TryGetValue(tab, out var view))
            return;
        view.Grid.Reveal(row);
    }
}
