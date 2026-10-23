using System.Globalization;
using System.Text;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Exports as text: what kind each is and how it ended, in a few words for the exports
/// panel, and whole reports, which a tab shows.</summary>
internal static class ExportReportText
{
    private const string Indent = "    ";

    public static string KindOf(Strings t, ExportKind kind) => kind switch
    {
        ExportKind.CSharpFile => t.ExportKindCsharpFile,
        ExportKind.ILFile => t.ExportKindIlFile,
        ExportKind.Project => t.ExportKindProject,
        ExportKind.Solution => t.ExportKindSolution,
        ExportKind.Pdb => t.ExportKindPdb,
        _ => t.ExportKindPdbs,
    };

    /// <summary>How <paramref name="report"/>'s export ended, in a few words: whether it finished,
    /// with how many problems, and how long it took.</summary>
    public static string DescribeOutcome(Strings t, ExportReport report)
    {
        if (report.Outcome == ExportOutcome.Cancelled)
            return t.ExportsCancelled;
        if (report.Outcome == ExportOutcome.Failed)
            return t.ExportsFailed;
        var duration = FormatDuration(t, report.Elapsed);
        if (report.Problems.Count == 0)
            return t.ExportsFinishedIn(duration);
        var problems = t.ExportProblemCount(report.Problems.Count);
        return $"{t.ExportsFinishedWith(problems)} · {duration}";
    }

    /// <summary><paramref name="elapsed"/> as people say it: seconds with a tenth under a minute,
    /// then minutes and seconds, then hours and minutes.</summary>
    public static string FormatDuration(Strings t, TimeSpan elapsed)
    {
        if (elapsed.TotalMinutes < 1)
            return t.DurationSeconds(elapsed.TotalSeconds.ToString("F1", CultureInfo.CurrentCulture));
        if (elapsed.TotalHours < 1)
            return t.DurationMinutes((int)elapsed.TotalMinutes, elapsed.Seconds);
        return t.DurationHours((int)elapsed.TotalHours, elapsed.Minutes);
    }

    private static string Summarize(Strings t, ExportReport report)
    {
        if (report.Outcome == ExportOutcome.Cancelled)
            return t.ExportSummaryCancelled(report.Name);
        if (report.Outcome == ExportOutcome.Failed)
            return t.ExportSummaryFailed(report.Name);
        var summary = t.ExportSummaryFinished(report.Name);
        if (report.Problems.Count == 0)
            return summary;
        return $"{summary} · {t.ExportProblemCount(report.Problems.Count)}";
    }

    /// <summary>Each of <paramref name="reports"/> in full: its summary, kind, folder and time, then
    /// the files it wrote and its problems with their errors.</summary>
    public static DecompiledDocument Write(Strings t, IReadOnlyList<ExportReport> reports)
    {
        var text = new StringBuilder();
        foreach (var report in reports)
            WriteReport(text, t, report);
        return DecompiledDocument.FromText(t.ExportReportTitle, text.ToString());
    }

    private static void WriteReport(StringBuilder text, Strings t, ExportReport report)
    {
        text.AppendLine(Summarize(t, report));
        text.AppendLine(KindOf(t, report.Job.Kind));
        text.AppendLine(report.Folder);
        text.AppendLine(t.ExportTook(FormatDuration(t, report.Elapsed)));
        text.AppendLine();
        WriteWritten(text, t, report.Written);
        WriteProblems(text, t, report.Problems);
    }

    private static void WriteWritten(StringBuilder text, Strings t, IReadOnlyList<string> written)
    {
        if (written.Count == 0)
            return;
        text.AppendLine(t.ExportWritten);
        foreach (var path in written)
            text.Append(Indent).AppendLine(path);
        text.AppendLine();
    }

    private static void WriteProblems(StringBuilder text, Strings t, IReadOnlyList<ExportProblem> problems)
    {
        if (problems.Count == 0)
            return;
        text.AppendLine(t.ExportProblemsHeading);
        foreach (var problem in problems)
            WriteProblem(text, problem);
    }

    private static void WriteProblem(StringBuilder text, ExportProblem problem)
    {
        text.Append(Indent).AppendLine(problem.Headline);
        if (problem.Details != null)
            WriteDetails(text, problem.Details);
        text.AppendLine();
    }

    private static void WriteDetails(StringBuilder text, string details)
    {
        var lines = details.Split('\n');
        foreach (var line in lines)
            text.Append(Indent).Append(Indent).AppendLine(line.TrimEnd('\r'));
    }
}
