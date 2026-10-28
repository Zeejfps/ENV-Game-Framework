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

/// <summary>What one export did: which it was, how it ended, the files it wrote, what went wrong,
/// how long it took, and whether its folder was there when it ended.</summary>
public sealed record ExportReport(
    int Run,
    ExportJob Job,
    ExportOutcome Outcome,
    IReadOnlyList<string> Written,
    IReadOnlyList<ExportProblem> Problems,
    TimeSpan Elapsed,
    bool FolderExists)
{
    public string Name => Job.Name;

    public string Folder => Job.Folder;

    public bool HasProblems => Outcome == ExportOutcome.Failed || Problems.Count > 0;

    /// <summary>The report of the export <paramref name="run"/> of <paramref name="job"/> that finished,
    /// with nothing written yet.</summary>
    public static ExportReport Finished(int run, ExportJob job, TimeSpan elapsed)
    {
        return new ExportReport(run, job, ExportOutcome.Finished, [], [], elapsed, Directory.Exists(job.Folder));
    }

    /// <summary>The report of the export <paramref name="run"/> of <paramref name="job"/> that ended
    /// with <paramref name="error"/>: cancelled when it was cancelled, else failed, with
    /// <paramref name="failure"/> as its problem.</summary>
    public static ExportReport Ended(int run, ExportJob job, Exception error, string failure, TimeSpan elapsed)
    {
        var folderExists = Directory.Exists(job.Folder);
        if (error is OperationCanceledException)
            return new ExportReport(run, job, ExportOutcome.Cancelled, [], [], elapsed, folderExists);
        var problem = new ExportProblem(failure, error.ToString());
        return new ExportReport(run, job, ExportOutcome.Failed, [], [problem], elapsed, folderExists);
    }
}
