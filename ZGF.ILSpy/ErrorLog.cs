namespace ZGF.ILSpy;

/// <summary>Something that went wrong, worth telling the user, and when.</summary>
public sealed record AppError(AppProblem Problem, DateTime Time);

/// <summary>The recent errors, newest last, the oldest dropped past <see cref="Capacity"/>. UI thread only.</summary>
public sealed class ErrorLog
{
    private const int Capacity = 50;

    private readonly List<AppError> _entries = new();

    public IReadOnlyList<AppError> Entries => _entries;

    /// <summary>Changes whenever an error is added; clearing leaves it.</summary>
    public int Revision { get; private set; }

    public void Add(AppProblem problem)
    {
        Revision++;
        _entries.Add(new AppError(problem, DateTime.Now));
        if (_entries.Count > Capacity)
            _entries.RemoveAt(0);
    }

    /// <summary>Forgets every error.</summary>
    public void Clear()
    {
        _entries.Clear();
    }
}
