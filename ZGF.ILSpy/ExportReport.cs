using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>How an export ended. One that <see cref="Finished"/> may still have problems: parts it
/// could not decompile, or PDBs it could not write.</summary>
public enum ExportOutcome
{
    Finished,
    Cancelled,
    Failed,
}

/// <summary>Something an export could not do, for the screen to tell in the chosen language.</summary>
public abstract record ExportProblem
{
    private ExportProblem()
    {
    }

    /// <summary>The export failed as a whole with <see cref="Error"/>; <see cref="Subject"/> names what it
    /// was exporting, empty when the export's kind says it.</summary>
    public sealed record Failed(string Subject, Exception Error) : ExportProblem;

    /// <summary>Part of a project could not be decompiled.</summary>
    public sealed record NotDecompiled(ExportError Error) : ExportProblem;

    /// <summary>The assembly <see cref="Assembly"/> has no entry point a PDB can be written for.</summary>
    public sealed record NoPdbEntry(string Assembly) : ExportProblem;

    public sealed record PdbNotWritten(string Assembly, ExportError Error) : ExportProblem;

    /// <summary>The package file at <see cref="Path"/> was skipped: its path leads outside the folder.</summary>
    public sealed record OutsideFolder(string Path) : ExportProblem;

    public sealed record FileNotWritten(string Path, ExportError Error) : ExportProblem;
}

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
    /// with <paramref name="error"/>: cancelled when it was cancelled, else failed, exporting
    /// <paramref name="subject"/>.</summary>
    public static ExportReport Ended(int run, ExportJob job, Exception error, string subject, TimeSpan elapsed)
    {
        var folderExists = Directory.Exists(job.Folder);
        if (error is OperationCanceledException)
            return new ExportReport(run, job, ExportOutcome.Cancelled, [], [], elapsed, folderExists);
        var problem = new ExportProblem.Failed(subject, error);
        return new ExportReport(run, job, ExportOutcome.Failed, [], [problem], elapsed, folderExists);
    }
}
