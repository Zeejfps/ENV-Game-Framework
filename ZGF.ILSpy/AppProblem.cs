using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Analysis;

namespace ZGF.ILSpy;

/// <summary>Which themes a theme problem is about.</summary>
public enum ThemeKind
{
    Color,
    Icon,
}

/// <summary>Something that went wrong, worth telling the user, as what happened rather than words, for
/// the screen to tell in the chosen language. A <c>Reason</c> is as the system said it.</summary>
public abstract record AppProblem
{
    private AppProblem()
    {
    }

    public sealed record OpenFailed(Exception Error) : AppProblem;

    /// <summary>The PDB at <see cref="PdbPath"/> cannot be used for the assembly <see cref="Assembly"/>.</summary>
    public sealed record PdbUnusable(string PdbPath, string Assembly, PdbCheck Check) : AppProblem;

    public sealed record AssemblyNotReloaded(string Assembly, Exception Error) : AppProblem;

    public sealed record AssembliesNotReloaded(Exception Error) : AppProblem;

    /// <summary>What a tab showed is gone from the assemblies read again.</summary>
    public sealed record GoneAfterReload(NodePath Location) : AppProblem;

    public sealed record AnalysisFailed(string Subject, AnalysisKind Kind, Exception Error) : AppProblem;

    public sealed record AnalysesNotRestored(Exception Error) : AppProblem;

    /// <summary>No open assembly has what the documentation id <see cref="Id"/> names.</summary>
    public sealed record IdNotFound(string Id) : AppProblem;

    public sealed record NoApiReferencePage(string Name) : AppProblem;

    /// <summary>What <see cref="Reference"/> points at was not found, or finding it failed with <see cref="Error"/>.</summary>
    public sealed record Unresolved(CodeReference Reference, Exception? Error) : AppProblem;

    /// <summary>The place a history entry shows was not found, or finding it failed with <see cref="Error"/>.</summary>
    public sealed record HistoryPlaceGone(NodePath Place, Exception? Error) : AppProblem;

    /// <summary>The place a bookmark marks was not found, or finding it failed with <see cref="Error"/>.</summary>
    public sealed record BookmarkGone(string Bookmark, NodePath Place, Exception? Error) : AppProblem;

    public sealed record BookmarksNotExported(string Reason) : AppProblem;

    public sealed record BookmarksNotImported(string File, string Reason) : AppProblem;

    /// <summary>The places of the bookmarks <see cref="Lost"/> describes, read from <see cref="File"/>, were not found.</summary>
    public sealed record ImportedBookmarksLost(string File, IReadOnlyList<string> Lost) : AppProblem;

    /// <summary><see cref="Count"/> imported bookmarks were left out because others mark their lines.</summary>
    public sealed record BookmarksLeftOut(int Count) : AppProblem;

    public sealed record ComparisonFailed(string Left, string Right, Exception Error) : AppProblem;

    /// <summary>Decompiling <see cref="Node"/> failed with <see cref="Error"/>, or as <see cref="Document"/> tells.</summary>
    public sealed record DecompilationFailed(string Node, Exception? Error, string Document) : AppProblem;

    /// <summary>The type <see cref="Type"/> was not found for a diagram, or reading it failed with <see cref="Error"/>.</summary>
    public sealed record TypeNotDiagrammed(CodeReference Type, Exception? Error) : AppProblem;

    public sealed record DiagramNotReopened(string Diagram, Exception Error) : AppProblem;

    public sealed record DiagramNotLaidOut(Exception Error) : AppProblem;

    public sealed record ResourceNotSaved(string Resource, string Reason) : AppProblem;

    public sealed record PackageFileNotExtracted(string File, string Reason) : AppProblem;

    /// <summary>The settings file at <see cref="Path"/> was read as <see cref="Problem"/> tells.</summary>
    public sealed record SettingsUnreadable(string? Path, SettingsProblem Problem) : AppProblem;

    public sealed record SettingsNotSaved(string Reason) : AppProblem;

    /// <summary>What is wrong in the theme file <see cref="File"/>, or in the themes it extends.</summary>
    public sealed record ThemeFileProblems(ThemeKind Kind, string File, IReadOnlyList<ThemeError> Errors) : AppProblem;

    public sealed record ThemesFolderNotMade(ThemeKind Kind, string Reason) : AppProblem;

    public sealed record CustomThemeNotWritten(ThemeKind Kind, string Path, string Reason) : AppProblem;

    public sealed record ThemesNotWatched(ThemeKind Kind, string Reason) : AppProblem;

    public sealed record RuntimeNotScanned(Exception Error) : AppProblem;

    public sealed record NuGetConfigUnreadable(Exception Error) : AppProblem;

    public sealed record NuGetSearchFailed(string Feed, Exception Error) : AppProblem;

    public sealed record PackageNotDownloaded(string Package, Exception Error) : AppProblem;

    public sealed record SearchFailed(Exception Error) : AppProblem;
}
