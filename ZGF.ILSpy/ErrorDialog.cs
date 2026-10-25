using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>The error dialog: whether it shows, the error picked in it, and the view of that error's text.</summary>
public sealed class ErrorDialog
{
    private AppError? _documentFor;
    private DecompiledDocument _document = DecompiledDocument.Empty;
    private int _shownRevision;

    public bool IsOpen;

    /// <summary>The error the dialog shows.</summary>
    public ItemSelection<AppError> Selection { get; } = new();

    public AppError? SelectedError => Selection.Item;

    /// <summary>The view of <see cref="SelectedError"/>'s text.</summary>
    public CodeViewState View { get; } = new();

    /// <summary>Opens the dialog on the newest of <paramref name="errors"/>, unless there are none.</summary>
    public void Open(IReadOnlyList<AppError> errors)
    {
        if (errors.Count == 0)
            return;
        Selection.Select(errors[^1], errors);
        IsOpen = true;
    }

    /// <summary>Opens the dialog on the newest of <paramref name="errors"/> when an error was added since
    /// it last saw the log, at <paramref name="revision"/>.</summary>
    public void ShowNewErrors(int revision, IReadOnlyList<AppError> errors)
    {
        if (revision == _shownRevision)
            return;
        _shownRevision = revision;
        Open(errors);
    }

    public void Close()
    {
        IsOpen = false;
    }

    /// <summary><see cref="SelectedError"/>'s details, or its title when it has none, as a document
    /// for the dialog's code view.</summary>
    public DecompiledDocument SelectedDocument()
    {
        var error = SelectedError;
        if (error == null)
            return DecompiledDocument.Empty;
        if (!ReferenceEquals(error, _documentFor))
            _document = CreateDocument(error);
        _documentFor = error;
        return _document;
    }

    private static DecompiledDocument CreateDocument(AppError error)
    {
        var text = error.Details;
        if (text.Length == 0)
            text = error.Title;
        return DecompiledDocument.FromText(error.Title, text);
    }
}
