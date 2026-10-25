namespace ZGF.ILSpy;

/// <summary>Something that went wrong, worth telling the user: a one-line <see cref="Title"/> and
/// the full <see cref="Details"/>, e.g. an exception with its stack trace.</summary>
public sealed record AppError(string Title, string Details, DateTime Time)
{
    private const int SummaryLength = 160;

    /// <summary>The title and details as one text, for copying.</summary>
    public string FullText { get; } = Details.Length == 0 ? Title : $"{Title}{Environment.NewLine}{Environment.NewLine}{Details}";

    /// <summary>The title and the first line of the details on one line, for a tooltip.</summary>
    public string Summary { get; } = Summarize(Title, Details);

    private static string Summarize(string title, string details)
    {
        var end = details.AsSpan().IndexOfAny('\r', '\n');
        var first = (end < 0 ? details : details[..end]).Trim();
        var summary = first.Length == 0 ? title : $"{title} — {first}";
        return summary.Length > SummaryLength ? summary[..SummaryLength] + "…" : summary;
    }
}

/// <summary>The recent errors, newest last, the oldest dropped past <see cref="Capacity"/>. UI thread only.</summary>
public sealed class ErrorLog
{
    private const int Capacity = 50;

    private readonly List<AppError> _entries = new();

    public IReadOnlyList<AppError> Entries => _entries;

    /// <summary>Changes whenever an error is added; clearing leaves it.</summary>
    public int Revision { get; private set; }

    public void Add(string title, string? details = null)
    {
        Revision++;
        _entries.Add(new AppError(title, details?.Trim() ?? "", DateTime.Now));
        if (_entries.Count > Capacity)
            _entries.RemoveAt(0);
    }

    /// <summary>Forgets every error.</summary>
    public void Clear()
    {
        _entries.Clear();
    }
}
