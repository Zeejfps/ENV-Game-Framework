namespace ZGF.ILSpy;

/// <summary>How an export ended. One that <see cref="Finished"/> may still have problems: parts it
/// could not decompile, or PDBs it could not write.</summary>
public enum ExportOutcome
{
    Finished,
    Cancelled,
    Failed,
}

/// <summary>Something an export could not do: a headline, and the full error when there is one.</summary>
public sealed record ExportProblem(string Headline, string? Details = null);

/// <summary>What one export did: what it was named after, how it ended, the folder it wrote into,
/// the files it wrote, what went wrong and how long it took.</summary>
public sealed record ExportReport(
    string Name,
    ExportOutcome Outcome,
    string Folder,
    IReadOnlyList<string> Written,
    IReadOnlyList<ExportProblem> Problems,
    TimeSpan Elapsed)
{
    public bool HasProblems => Outcome == ExportOutcome.Failed || Problems.Count > 0;

    /// <summary>The report of an export named <paramref name="name"/> into <paramref name="folder"/>
    /// that ended with <paramref name="error"/>: finished, with nothing written yet, when null;
    /// cancelled when it was cancelled; else failed, with <paramref name="failure"/> as its problem.</summary>
    public static ExportReport Of(string name, string folder, Exception? error, string failure, TimeSpan elapsed)
    {
        if (error == null)
            return new ExportReport(name, ExportOutcome.Finished, folder, [], [], elapsed);
        if (error is OperationCanceledException)
            return new ExportReport(name, ExportOutcome.Cancelled, folder, [], [], elapsed);
        var problem = new ExportProblem(failure, error.ToString());
        return new ExportReport(name, ExportOutcome.Failed, folder, [], [problem], elapsed);
    }
}
