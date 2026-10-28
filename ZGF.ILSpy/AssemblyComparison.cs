using ZGF.ILSpy.Core.Comparison;

namespace ZGF.ILSpy;

/// <summary>The two assemblies a comparison tab compares, by file, on the left and on the right, what it takes in, and
/// the newest comparison of them that finished, or why it failed. A comparison started cancels the one
/// before, whose result is then dropped; once closed, what is in flight is cancelled and its result
/// dropped. Mutated on the UI thread only.</summary>
public sealed class AssemblyComparison
{
    private readonly CancellationTokenSource _open = new();
    private CancellationTokenSource? _work;

    public AssemblyComparison(string leftPath, string rightPath)
    {
        LeftPath = leftPath;
        RightPath = rightPath;
    }

    public string LeftPath { get; private set; }

    public string RightPath { get; private set; }

    public ApiScope Scope { get; private set; } = ApiScope.PublicApi;

    /// <summary>The newest comparison that finished, or null before the first or after a failure.</summary>
    public ApiDiff? Diff { get; private set; }

    /// <summary>Why the newest comparison failed, or null when none did.</summary>
    public ComparisonFailure? Failure { get; private set; }

    /// <summary>True while a comparison runs in the background.</summary>
    public bool IsComparing => _work != null;

    public bool IsClosed => _open.IsCancellationRequested;

    /// <summary>Counts up whenever the sides are swapped or the comparison takes in more or less; it never counts down.</summary>
    public int Version { get; private set; }

    /// <summary>Puts the right assembly on the left and the left one on the right.</summary>
    public void Swap()
    {
        (LeftPath, RightPath) = (RightPath, LeftPath);
        Version++;
    }

    /// <summary>Takes in what <paramref name="scope"/> says from now on.</summary>
    public void TakeIn(ApiScope scope)
    {
        Scope = scope;
        Version++;
    }

    /// <summary>Starts a comparison, cancelling the one in flight, which is then dropped, until
    /// <see cref="FinishComparing"/>.</summary>
    public LoadTicket StartComparing()
    {
        _work?.Cancel();
        var work = CancellationTokenSource.CreateLinkedTokenSource(_open.Token);
        _work = work;
        return new LoadTicket(work);
    }

    /// <summary>Ends the comparison <paramref name="ticket"/> was given for. False when another started since,
    /// or the comparison was closed, and what it brought should be dropped.</summary>
    public bool FinishComparing(LoadTicket ticket)
    {
        ticket.Work.Dispose();
        if (_work != ticket.Work)
            return false;
        _work = null;
        return !IsClosed;
    }

    public void Show(ApiDiff diff)
    {
        Diff = diff;
        Failure = null;
    }

    /// <summary>Shows why the assemblies cannot be compared, cancelling the comparison in flight, if any.</summary>
    public void ShowFailure(ComparisonFailure failure)
    {
        _work?.Cancel();
        _work = null;
        Diff = null;
        Failure = failure;
    }

    /// <summary>Closes the comparison for good, cancelling what is in flight.</summary>
    public void Close()
    {
        _open.Cancel();
    }
}

/// <summary>Why two assemblies could not be compared.</summary>
public abstract record ComparisonFailure
{
    private ComparisonFailure()
    {
    }

    /// <summary>The assembly read from <see cref="File"/> is no longer open.</summary>
    public sealed record AssemblyClosed(string File) : ComparisonFailure;

    /// <summary>Comparing the assemblies <see cref="Left"/> and <see cref="Right"/> failed.</summary>
    public sealed record Failed(string Left, string Right) : ComparisonFailure;
}
