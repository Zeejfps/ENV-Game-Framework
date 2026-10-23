using System.Diagnostics;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>An export waiting for the user to say whether it may write into <see cref="Folder"/>,
/// which already holds other files.</summary>
public sealed record ExportRequest(string Folder, Action Start);

/// <summary>Writing code, projects, solutions and portable PDBs to files in the background, one
/// export at a time in the order they were asked for, which <see cref="Workspace.Export"/> queues.
/// Each ends in an <see cref="ExportReport"/> in <see cref="Workspace.FinishedExports"/>.</summary>
public static class Exporting
{
    private const string ProjectExtension = ".csproj";

    /// <summary>Whether <paramref name="node"/>'s code can be saved: it shows code.</summary>
    public static bool CanSaveCode(this Workspace w, BrowseNode? node)
    {
        if (node == null)
            return false;
        return node.DependsOnLanguage;
    }

    /// <summary>Saves <paramref name="node"/>'s code in the current language to the file at
    /// <paramref name="path"/>, or exports a project when the node is an assembly and the path names
    /// a project file.</summary>
    public static void SaveCode(this Workspace w, BrowseNode node, string path)
    {
        if (IsProjectFile(node, path))
        {
            w.ExportProject(node.Assembly, path);
            return;
        }
        w.WriteCodeFile(node, path);
    }

    private static bool IsProjectFile(BrowseNode node, string path)
    {
        if (node.Kind != NodeKind.Assembly)
            return false;
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ProjectExtension, StringComparison.OrdinalIgnoreCase);
    }

    private static async void WriteCodeFile(this Workspace w, BrowseNode node, string path)
    {
        var language = w.Language;
        var options = w.DecompilerOptions;
        var start = await w.Export.Enter(node.Text);
        var clock = Stopwatch.StartNew();
        var saved = await Background.Run(() => SaveCodeFile(node, language, options, path, start.Token));
        w.Export.Leave(start);
        var failure = Strings.For(w.Locale).ErrorSaveCode(node.Text);
        var report = ExportReport.Of(node.Text, FolderOf(path), saved.Error, failure, clock.Elapsed);
        w.FinishExport(report.WithWritten(saved.Error, path));
    }

    private static bool SaveCodeFile(BrowseNode node, DecompilationLanguage language, DecompilerOptions options, string path,
        CancellationToken cancellationToken)
    {
        node.SaveCode(language, options, path, cancellationToken);
        return true;
    }

    private static string FolderOf(string path) => Path.GetDirectoryName(path)!;

    /// <summary><paramref name="report"/> listing <paramref name="written"/>, when the export ended
    /// without <paramref name="error"/>.</summary>
    private static ExportReport WithWritten(this ExportReport report, Exception? error, params string[] written)
    {
        if (error != null)
            return report;
        return report with { Written = written };
    }

    private static void FinishExport(this Workspace w, ExportReport report) => w.FinishedExports.Add(report);

    /// <summary>Decompiles <paramref name="assembly"/> into a C# project whose file is
    /// <paramref name="projectPath"/>, asking first when its folder already holds other files.</summary>
    public static void ExportProject(this Workspace w, LoadedAssembly assembly, string projectPath)
    {
        var folder = FolderOf(projectPath);
        w.ConfirmFolder(folder, projectPath, () => w.StartProjectExport(assembly, projectPath));
    }

    private static async void StartProjectExport(this Workspace w, LoadedAssembly assembly, string projectPath)
    {
        var options = w.DecompilerOptions;
        var start = await w.Export.Enter(assembly.Name);
        var clock = Stopwatch.StartNew();
        var exported = await Background.Run(() => AssemblyExport.ExportProject(assembly, options, projectPath, start.Progress, start.Token));
        w.Export.Leave(start);
        var failure = Strings.For(w.Locale).ErrorExportProject(assembly.Name);
        var report = ExportReport.Of(assembly.Name, FolderOf(projectPath), exported.Error, failure, clock.Elapsed);
        w.FinishExport(report.WithProjects(exported.Value, projectPath));
    }

    /// <summary><paramref name="report"/> listing <paramref name="first"/> and each of
    /// <paramref name="projects"/> as written, with the parts the projects could not decompile as
    /// its problems.</summary>
    private static ExportReport WithProjects(this ExportReport report, IReadOnlyList<ExportedProject>? projects, string first)
    {
        if (projects == null)
            return report;
        var written = projects
            .Select(static p => p.ProjectPath)
            .Prepend(first)
            .Distinct()
            .ToList();
        var problems = projects
            .SelectMany(static p => p.Errors)
            .Select(static e => new ExportProblem(e.Message, e.Details))
            .ToList();
        return report with { Written = written, Problems = problems };
    }

    private static ExportReport WithProjects(this ExportReport report, ExportedProject? project, string first)
    {
        if (project == null)
            return report;
        return report.WithProjects([project], first);
    }

    /// <summary>Decompiles every open assembly into a C# project beside the solution at
    /// <paramref name="solutionPath"/>, asking first when its folder already holds other files.</summary>
    public static void ExportSolution(this Workspace w, string solutionPath)
    {
        var folder = FolderOf(solutionPath);
        var projects = AssemblyExport.NameProjects(w.Assemblies.Assemblies);
        w.ConfirmFolder(folder, solutionPath, () => w.StartSolutionExport(projects, solutionPath));
    }

    private static async void StartSolutionExport(this Workspace w, List<SolutionProject> projects, string solutionPath)
    {
        var options = w.DecompilerOptions;
        var name = Path.GetFileName(solutionPath);
        var start = await w.Export.Enter(name);
        var clock = Stopwatch.StartNew();
        var exported = await Background.Run(() => AssemblyExport.ExportSolution(projects, options, solutionPath, start.Progress, start.Token));
        w.Export.Leave(start);
        var failure = Strings.For(w.Locale).ErrorExportSolution;
        var report = ExportReport.Of(name, FolderOf(solutionPath), exported.Error, failure, clock.Elapsed);
        w.FinishExport(report.WithProjects(exported.Value, solutionPath));
    }

    /// <summary>The assembly of the node the active tab shows, or null.</summary>
    public static LoadedAssembly? ActiveAssembly(this Workspace w)
    {
        var node = w.Tabs.Active?.Node;
        if (node == null)
            return null;
        return node.Assembly;
    }

    /// <summary>Opens the export dialog on the open assemblies, with <paramref name="picked"/> ticked.</summary>
    public static void OpenExportSetup(this Workspace w, LoadedAssembly? picked)
    {
        if (w.Assemblies.Assemblies.Count == 0)
            return;
        w.ExportSetup.Open(w.Assemblies.Assemblies, picked);
    }

    /// <summary>Closes the export dialog and exports what it says: one assembly as a project
    /// straight into the folder, several as a solution with a folder per project.</summary>
    public static void StartExport(this Workspace w)
    {
        var setup = w.ExportSetup;
        if (!setup.CanExport)
            return;
        setup.Close();
        var folder = setup.Folder.Trim();
        var projects = setup.IncludedProjects();
        if (setup.IsSolution)
        {
            var solutionPath = Path.Combine(folder, setup.SolutionFileName);
            w.StartSolutionExport(projects, solutionPath);
            return;
        }
        var project = projects[0];
        var projectPath = Path.Combine(folder, AssemblyExport.CleanProjectName(project.Name) + ProjectExtension);
        w.StartProjectExport(project.Assembly, projectPath);
    }

    /// <summary>Starts <paramref name="start"/> at once when <paramref name="folder"/> holds nothing
    /// but <paramref name="file"/>, and otherwise asks through <see cref="Workspace.ConfirmingExport"/>.</summary>
    private static void ConfirmFolder(this Workspace w, string folder, string file, Action start)
    {
        if (!HoldsOtherEntries(folder, file))
        {
            start();
            return;
        }
        w.ConfirmingExport = new ExportRequest(folder, start);
    }

    private static bool HoldsOtherEntries(string folder, string file)
    {
        if (!Directory.Exists(folder))
            return false;
        var fullFile = Path.GetFullPath(file);
        return Directory.EnumerateFileSystemEntries(folder)
            .Any(e => !IsSamePath(e, fullFile));
    }

    private static bool IsSamePath(string entry, string fullFile)
    {
        var fullEntry = Path.GetFullPath(entry);
        return string.Equals(fullEntry, fullFile, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Starts the <see cref="Workspace.ConfirmingExport"/> when <paramref name="confirmed"/>,
    /// and drops it either way.</summary>
    public static void FinishExportConfirmation(this Workspace w, bool confirmed)
    {
        var request = w.ConfirmingExport;
        w.ConfirmingExport = null;
        if (!confirmed)
            return;
        if (request == null)
            return;
        request.Start();
    }

    /// <summary>Writes a portable PDB of <paramref name="assembly"/>'s decompiled C# to
    /// <paramref name="path"/>.</summary>
    public static async void GeneratePdb(this Workspace w, LoadedAssembly assembly, string path)
    {
        var options = w.DecompilerOptions;
        var start = await w.Export.Enter(assembly.Name);
        var clock = Stopwatch.StartNew();
        var generated = await Background.Run(() => GeneratePdbFile(assembly, options, path, start));
        w.Export.Leave(start);
        var failure = Strings.For(w.Locale).ErrorGeneratePdb(assembly.Name);
        var report = ExportReport.Of(assembly.Name, FolderOf(path), generated.Error, failure, clock.Elapsed);
        w.FinishExport(report.WithWritten(generated.Error, path));
    }

    private static bool GeneratePdbFile(LoadedAssembly assembly, DecompilerOptions options, string path, ExportStart start)
    {
        AssemblyExport.GeneratePdb(assembly, options, path, start.Progress, start.Token);
        return true;
    }

    /// <summary>Writes a portable PDB of each open assembly into <paramref name="folder"/>, named
    /// after the assembly. Assemblies that cannot have one, or fail, are the report's problems.</summary>
    public static async void GeneratePdbs(this Workspace w, string folder)
    {
        var options = w.DecompilerOptions;
        var assemblies = w.Assemblies.Assemblies;
        var name = Path.GetFileName(folder);
        var start = await w.Export.Enter(name);
        var clock = Stopwatch.StartNew();
        var failures = await Background.Run(() => AssemblyExport.GeneratePdbs(assemblies, options, folder, start.Progress, start.Token));
        w.Export.Leave(start);
        var t = Strings.For(w.Locale);
        var report = ExportReport.Of(name, folder, failures.Error, t.ErrorGeneratePdbs, clock.Elapsed);
        w.FinishExport(report.WithPdbs(assemblies, failures.Value, folder, t));
    }

    /// <summary><paramref name="report"/> listing the PDB of each of <paramref name="assemblies"/> that
    /// is not among <paramref name="failures"/> as written, and the failures as its problems.</summary>
    private static ExportReport WithPdbs(this ExportReport report, IReadOnlyList<LoadedAssembly> assemblies, List<PdbFailure>? failures,
        string folder, Strings t)
    {
        if (failures == null)
            return report;
        var failed = failures
            .Select(static f => f.Assembly)
            .ToHashSet();
        var written = assemblies
            .Where(a => !failed.Contains(a))
            .Select(a => Path.Combine(folder, a.PdbFileName))
            .ToList();
        var problems = failures.ConvertAll(f => DescribePdbFailure(t, f));
        return report with { Written = written, Problems = problems };
    }

    private static ExportProblem DescribePdbFailure(Strings t, PdbFailure failure)
    {
        var name = failure.Assembly.Name;
        if (failure.Error == null)
            return new ExportProblem(t.ErrorNoPdbEntry(name));
        return new ExportProblem($"{name}: {failure.Error.Message}", failure.Error.Details);
    }

    /// <summary>The folder the last finished export wrote into, or null when none has finished.</summary>
    public static string? LastExportFolder(this Workspace w)
    {
        var exports = w.FinishedExports;
        if (exports.Count == 0)
            return null;
        var last = exports[exports.Count - 1];
        return last.Folder;
    }

    /// <summary>Shows the reports of the finished exports in a new tab, and clears them.</summary>
    public static void ShowExportReport(this Workspace w)
    {
        var t = Strings.For(w.Locale);
        var document = ExportReportText.Write(t, w.FinishedExports);
        var tab = w.OpenNewTab();
        w.Tabs.MakeActive(tab);
        tab.ShowText(t.ExportReportTitle, document);
        w.FinishedExports.Clear();
    }

    public static void DismissExportNotice(this Workspace w) => w.FinishedExports.Clear();
}
