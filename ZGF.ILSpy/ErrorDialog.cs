using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The error dialog: whether it shows, the error picked in it, and the view of that error's text.</summary>
public sealed class ErrorDialog
{
    private readonly Dictionary<AppError, ProblemText> _texts = new(ReferenceEqualityComparer.Instance);
    private Strings? _textsIn;
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

    /// <summary><paramref name="error"/> in the words of <paramref name="t"/>, put into them once per language.</summary>
    public ProblemText TextOf(AppError error, Strings t)
    {
        if (!ReferenceEquals(t, _textsIn))
        {
            _texts.Clear();
            _textsIn = t;
            _documentFor = null;
        }
        if (!_texts.TryGetValue(error, out var text))
        {
            text = ProblemTexts.Describe(t, error.Problem);
            _texts[error] = text;
        }
        return text;
    }

    /// <summary><see cref="SelectedError"/>'s details, or its title when it has none, in the words of
    /// <paramref name="t"/>, as a document for the dialog's code view.</summary>
    public DecompiledDocument SelectedDocument(Strings t)
    {
        var error = SelectedError;
        if (error == null)
            return DecompiledDocument.Empty;
        var text = TextOf(error, t);
        if (!ReferenceEquals(error, _documentFor))
            _document = CreateDocument(text);
        _documentFor = error;
        return _document;
    }

    private static DecompiledDocument CreateDocument(ProblemText text)
    {
        var body = text.Details;
        if (body.Length == 0)
            body = text.Title;
        return DecompiledDocument.FromText(text.Title, body);
    }
}
