namespace ZGF.ILSpy;

/// <summary>
/// The export dialog's Choose page: the open assemblies with the ticked ones first, in an order
/// fixed when the page opens so rows stay put while they are ticked, narrowed by a filter on name
/// or path and, when asked, to the ticked ones.
/// </summary>
public sealed class AssemblyPicker
{
    private readonly List<ExportChoice> _order = new();

    public string Filter = "";
    public bool SelectedOnly;

    /// <summary>Lists <paramref name="choices"/> afresh, ticked first, with no filter.</summary>
    public void Arrange(IReadOnlyList<ExportChoice> choices)
    {
        _order.Clear();
        _order.AddRange(choices.Where(static c => c.Included));
        _order.AddRange(choices.Where(static c => !c.Included));
        ClearFilter();
    }

    /// <summary>Shows every assembly again.</summary>
    public void ClearFilter()
    {
        Filter = "";
        SelectedOnly = false;
    }

    /// <summary>The assemblies the page shows, in its order.</summary>
    public List<ExportChoice> Shown()
    {
        return _order
            .Where(Matches)
            .ToList();
    }

    private bool Matches(ExportChoice choice)
    {
        if (SelectedOnly && !choice.Included)
            return false;
        var filter = Filter.Trim();
        if (filter.Length == 0)
            return true;
        if (choice.Assembly.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            return true;
        return choice.Assembly.FilePath.Text.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Whether every shown assembly is ticked, none is, or some are (null).</summary>
    public bool? AllShownIncluded
    {
        get
        {
            var shown = Shown();
            var count = shown.Count(static c => c.Included);
            if (count == 0)
                return false;
            if (count == shown.Count)
                return true;
            return null;
        }
    }

    /// <summary>Ticks every shown assembly when <paramref name="all"/>, else unticks them; the hidden
    /// ones stay as they are.</summary>
    public void IncludeShown(bool all)
    {
        foreach (var choice in Shown())
            choice.Included = all;
    }
}
