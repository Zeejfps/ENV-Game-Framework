using ZGF.ILSpy.Core;

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

/// <summary>The recent errors, newest last, and the error dialog's state. UI thread only.</summary>
public sealed class ErrorLog
{
    private const int Capacity = 50;

    private readonly List<AppError> _entries = new();
    private AppError? _documentFor;
    private DecompiledDocument _document = DecompiledDocument.Empty;

    /// <summary>Whether the error dialog is showing.</summary>
    public bool IsOpen;

    public IReadOnlyList<AppError> Entries => _entries;

    /// <summary>The entry the dialog shows, an index into <see cref="Entries"/>.</summary>
    public int Selected { get; private set; } = -1;

    public AppError? SelectedError => Selected >= 0 && Selected < _entries.Count ? _entries[Selected] : null;

    /// <summary>The dialog's view of <see cref="SelectedError"/>'s text.</summary>
    public CodeViewState View { get; } = new();

    public void Add(string title, string? details = null)
    {
        _entries.Add(new AppError(title, details?.Trim() ?? "", DateTime.Now));
        if (_entries.Count > Capacity)
        {
            _entries.RemoveAt(0);
            Selected = Math.Max(0, Selected - 1);
        }
        if (!IsOpen)
            Selected = _entries.Count - 1;
    }

    /// <summary>Opens the dialog on the newest error.</summary>
    public void Open()
    {
        if (_entries.Count == 0) return;
        Selected = _entries.Count - 1;
        IsOpen = true;
    }

    public void Select(int index)
    {
        if (index >= 0 && index < _entries.Count)
            Selected = index;
    }

    /// <summary>Forgets every error and closes the dialog.</summary>
    public void Clear()
    {
        _entries.Clear();
        Selected = -1;
        IsOpen = false;
        _documentFor = null;
        _document = DecompiledDocument.Empty;
    }

    /// <summary><see cref="SelectedError"/>'s full text as a document for the dialog's code view.</summary>
    public DecompiledDocument SelectedDocument()
    {
        var error = SelectedError;
        if (error is null) return DecompiledDocument.Empty;
        if (!ReferenceEquals(error, _documentFor))
        {
            _documentFor = error;
            _document = DecompiledDocument.FromText(error.Title, error.FullText);
        }
        return _document;
    }
}
