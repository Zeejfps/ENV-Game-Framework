using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>An open tab: the node it shows and its decompiled document, which arrives from a
/// background decompilation. Mutated on the UI thread only.</summary>
public sealed class DocumentTab
{
    private static int _nextId;

    private CancellationTokenSource? _work;

    public int Id { get; } = Interlocked.Increment(ref _nextId);
    public BrowseNode? Node { get; private set; }
    public DecompilationLanguage Language { get; private set; }
    public DecompiledDocument Document { get; private set; } = DecompiledDocument.Empty;
    public CodeViewState View { get; } = new();
    public bool IsBusy => _work != null;
    public string Title => Node?.Text ?? "New tab";

    /// <summary>Starts decompiling <paramref name="node"/>, cancelling what was in flight, and shows a
    /// placeholder until it is done. Once it is, reveals <paramref name="line"/>, or the node's
    /// definition when -1. <paramref name="post"/> brings the result back to the UI thread.</summary>
    public void Show(BrowseNode node, DecompilationLanguage language, int line, Action<Action> post)
    {
        _work?.Cancel();
        var work = new CancellationTokenSource();
        _work = work;
        Node = node;
        Language = language;
        Document = DecompiledDocument.FromText(node.Text, "// Decompiling…", language);
        View.RevealLine(0);

        node.DecompileAsync(language, work.Token).ContinueWith(task => post(() => Complete(work, node, line, task)),
            CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }

    /// <summary>Decompiles the shown node again in <paramref name="language"/> if it was shown in
    /// another one.</summary>
    public void EnsureLanguage(DecompilationLanguage language, Action<Action> post)
    {
        if (Node != null && Language != language)
            Show(Node, language, -1, post);
    }

    public void Cancel()
    {
        _work?.Cancel();
        _work = null;
    }

    private void Complete(CancellationTokenSource work, BrowseNode node, int line, Task<DecompiledDocument> task)
    {
        if (_work != work) return;
        _work = null;
        work.Dispose();
        if (task.IsCanceled) return;

        Document = task.IsFaulted
            ? DecompiledDocument.FromText(node.Text, $"// Decompilation failed: {task.Exception!.GetBaseException().Message}", Language)
            : task.Result;
        var reveal = line >= 0 ? line : Document.FindDefinitionLine(node);
        View.RevealLine(Math.Max(0, reveal));
    }
}
