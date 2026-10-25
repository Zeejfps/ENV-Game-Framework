using System.Diagnostics;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Navigation;

namespace ZGF.ILSpy;


/// <summary>Writing code, projects, solutions, portable PDBs and the files of packages to files in the
/// background, one export at a time in the order they were asked for, which <see cref="AppState.Export"/>
/// queues. Each that runs ends in an <see cref="ExportReport"/> in <see cref="AppState.FinishedExports"/>.
/// Resources and single files of packages are written straight away.</summary>
public static class Exports
{
    private const string ProjectExtension = ".csproj";

    /// <summary>Whether <paramref name="node"/> can be saved: it shows code, or it is a resource,
    /// whose bytes are saved.</summary>
    public static bool CanSaveCode(AppState appState, BrowseNode? node)
    {
        if (node == null)
            return false;
        if (node.HasResource)
            return true;
        return node.DependsOnLanguage;
    }

    /// <summary>Saves <paramref name="node"/> to the file at <paramref name="path"/>: a resource's
    /// bytes as they are, else its code in the current language, as one file. Projects come from the
    /// export dialog.</summary>
    public static void SaveCode(AppState appState, BrowseNode node, string path)
    {
        if (node.HasResource)
        {
            SaveResource(appState, node, path);
            return;
        }
        WriteCodeFile(appState, node, path);
    }

    /// <summary>Writes the bytes of the resource <paramref name="node"/> stands for to the file at
    /// <paramref name="path"/>.</summary>
    public static void SaveResource(AppState appState, BrowseNode node, string path)
    {
        try
        {
            var bytes = node.ReadResource();
            File.WriteAllBytes(path, bytes);
        }
        catch (Exception e) when (IsFileError(e))
        {
            appState.ReportError(Strings.For(appState.Locale).ErrorSaveResource(node.Text), e.Message);
        }
    }

    private static bool IsFileError(Exception e)
    {
        if (e is IOException)
            return true;
        if (e is UnauthorizedAccessException)
            return true;
        return e is InvalidDataException;
    }

    /// <summary>Writes the file of a package <paramref name="place"/> stands for, as the package holds
    /// it, to the file at <paramref name="path"/>.</summary>
    public static void ExtractPackageFile(AppState appState, PackagePlace place, string path)
    {
        try
        {
            var bytes = place.Container.Package!.ReadFile(place.File);
            File.WriteAllBytes(path, bytes);
        }
        catch (Exception e) when (IsFileError(e))
        {
            appState.ReportError(Strings.For(appState.Locale).ErrorExtractFile(place.File.Name), e.Message);
        }
    }

    private static void WriteCodeFile(AppState appState, BrowseNode node, string path)
    {
        var language = appState.Language;
        var options = appState.DecompilerOptions;
        var job = new ExportJob(node.Text, CodeKindOf(language), FolderOf(path));
        var failure = Strings.For(appState.Locale).ErrorSaveCode(node.Text);
        RunExport(appState, job, failure, s => SaveCodeFile(node, language, options, path, s.Token), (r, _) => r.Listing(path));
    }

    private static ExportKind CodeKindOf(DecompilationLanguage language)
    {
        if (language == DecompilationLanguage.CSharp)
            return ExportKind.CSharpFile;
        return ExportKind.ILFile;
    }

    private static bool SaveCodeFile(BrowseNode node, DecompilationLanguage language, DecompilerOptions options, string path,
        CancellationToken cancellationToken)
    {
        node.SaveCode(language, options, path, cancellationToken);
        return true;
    }

    private static string FolderOf(string path) => Path.GetDirectoryName(path)!;

    /// <summary>Queues <paramref name="job"/> and shows the exports panel, where it waits, runs and
    /// ends. Once its turn comes, does <paramref name="work"/> in the background, then records how it
    /// ended, with <paramref name="failure"/> as the problem when it failed, and lets
    /// <paramref name="complete"/> add what a finished one wrote. One taken out of the queue leaves
    /// nothing behind.</summary>
    private static async void RunExport<T>(AppState appState, ExportJob job, string failure, Func<ExportStart, T> work,
        Func<ExportReport, T, ExportReport> complete)
    {
        appState.EnqueueCommand(new ViewCommand.OpenPanel(DockPanels.Exports));
        var start = await appState.Export.Enter(job);
        if (start.IsWithdrawn)
            return;
        var clock = Stopwatch.StartNew();
        var outcome = await Background.Run(() => work(start));
        appState.Export.Leave(start);
        var report = ExportReport.Of(start.Run, job, outcome.Error, failure, clock.Elapsed);
        if (outcome.Error == null)
            report = complete(report, outcome.Value!);
        appState.FinishedExports.Add(report);
    }

    /// <summary><paramref name="report"/> listing <paramref name="written"/>.</summary>
    private static ExportReport Listing(this ExportReport report, params string[] written) => report with { Written = written };

    /// <summary>Decompiles <paramref name="assembly"/> into a C# project whose file is
    /// <paramref name="projectPath"/>, with what <paramref name="extras"/> asks for besides. The export
    /// dialog warns about a folder that holds files before this is asked for.</summary>
    public static void ExportProject(AppState appState, LoadedAssembly assembly, string projectPath, ProjectExtras? extras = null)
    {
        var withExtras = extras ?? ProjectExtras.None;
        var options = appState.DecompilerOptions;
        var t = Strings.For(appState.Locale);
        var job = new ExportJob(Path.GetFileNameWithoutExtension(projectPath), ExportKind.Project, FolderOf(projectPath));
        var failure = t.ErrorExportProject(assembly.Name);
        RunExport(appState, job, failure, s => AssemblyExport.ExportProject(assembly, options, projectPath, withExtras, s.Progress, s.Token),
            (r, project) => r.WithProjects([project], projectPath, t));
    }

    /// <summary><paramref name="report"/> listing <paramref name="first"/>, each of
    /// <paramref name="projects"/> and the PDBs written beside them as written, with the parts the
    /// projects could not decompile and the PDBs that could not be written as its problems.</summary>
    private static ExportReport WithProjects(this ExportReport report, IReadOnlyList<ExportedProject> projects, string first, Strings t)
    {
        var pdbs = projects
            .Where(static p => p.PdbPath != null)
            .Select(static p => p.PdbPath!);
        var written = projects
            .Select(static p => p.ProjectPath)
            .Prepend(first)
            .Concat(pdbs)
            .Distinct()
            .ToList();
        var pdbProblems = projects
            .Where(static p => p.PdbFailure != null)
            .Select(p => DescribePdbFailure(t, p.PdbFailure!));
        var problems = projects
            .SelectMany(static p => p.Errors)
            .Select(static e => new ExportProblem(e.Message, e.Details))
            .Concat(pdbProblems)
            .ToList();
        return report with { Written = written, Problems = problems };
    }

    /// <summary>Decompiles each of <paramref name="projects"/> into a C# project in a folder of its own
    /// beside the solution at <paramref name="solutionPath"/>.</summary>
    private static void ExportSolution(AppState appState, List<SolutionProject> projects, string solutionPath, ProjectExtras extras)
    {
        var options = appState.DecompilerOptions;
        var t = Strings.For(appState.Locale);
        var job = new ExportJob(Path.GetFileNameWithoutExtension(solutionPath), ExportKind.Solution, FolderOf(solutionPath));
        RunExport(appState, job, t.ErrorExportSolution, s => AssemblyExport.ExportSolution(projects, options, solutionPath, extras, s.Progress, s.Token),
            (r, exported) => r.WithProjects(exported, solutionPath, t));
    }

    /// <summary>The assembly of the node the active tab shows, or null.</summary>
    public static LoadedAssembly? FindActiveAssembly(AppState appState)
    {
        var node = appState.Tabs.Active?.Node;
        if (node == null)
            return null;
        return node.Assembly;
    }

    /// <summary>Exports what <paramref name="request"/> asks for: one assembly as a project straight
    /// into the folder, several as a solution with a folder per project.</summary>
    public static void StartExport(AppState appState, ExportRequest request)
    {
        var folder = request.Folder;
        var projects = request.Projects;
        if (request.IsSolution)
        {
            var solutionPath = Path.Combine(folder, request.SolutionFileName);
            ExportSolution(appState, projects, solutionPath, request.Extras);
            return;
        }
        var project = projects[0];
        var projectPath = Path.Combine(folder, AssemblyExport.CleanProjectName(project.Name) + ProjectExtension);
        ExportProject(appState, project.Assembly, projectPath, request.Extras);
    }

    /// <summary>Writes a portable PDB of <paramref name="assembly"/>'s decompiled C# to
    /// <paramref name="path"/>.</summary>
    public static void GeneratePdb(AppState appState, LoadedAssembly assembly, string path)
    {
        var options = appState.DecompilerOptions;
        var job = new ExportJob(assembly.Name, ExportKind.Pdb, FolderOf(path));
        var failure = Strings.For(appState.Locale).ErrorGeneratePdb(assembly.Name);
        RunExport(appState, job, failure, s => GeneratePdbFile(assembly, options, path, s), (r, _) => r.Listing(path));
    }

    private static bool GeneratePdbFile(LoadedAssembly assembly, DecompilerOptions options, string path, ExportStart start)
    {
        AssemblyExport.GeneratePdb(assembly, options, path, start.Progress, start.Token);
        return true;
    }

    /// <summary>Writes a portable PDB of each open assembly into <paramref name="folder"/>, named
    /// after the assembly. Assemblies that cannot have one, or fail, are the report's problems.</summary>
    public static void GeneratePdbs(AppState appState, string folder)
    {
        var options = appState.DecompilerOptions;
        var assemblies = appState.Assemblies.Assemblies;
        var t = Strings.For(appState.Locale);
        var job = new ExportJob(Path.GetFileName(folder), ExportKind.Pdbs, folder);
        RunExport(appState, job, t.ErrorGeneratePdbs, s => AssemblyExport.GeneratePdbs(assemblies, options, folder, s.Progress, s.Token),
            (r, failures) => r.WithPdbs(assemblies, failures, folder, t));
    }

    /// <summary><paramref name="report"/> listing the PDB of each of <paramref name="assemblies"/> that
    /// is not among <paramref name="failures"/> as written, and the failures as its problems.</summary>
    private static ExportReport WithPdbs(this ExportReport report, IReadOnlyList<LoadedAssembly> assemblies, List<PdbFailure> failures,
        string folder, Strings t)
    {
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

    /// <summary>Writes every file in the folder of a package that <paramref name="node"/> lists into
    /// <paramref name="folder"/>, at its path from the folder <paramref name="node"/> lists. Files that
    /// cannot be written there are the report's problems.</summary>
    public static void ExtractPackageFolder(AppState appState, BrowseNode node, string folder)
    {
        var package = node.Assembly.Package!;
        var packageFolder = node.PackageFolder!;
        var t = Strings.For(appState.Locale);
        var job = new ExportJob(node.Text, ExportKind.Files, folder);
        var failure = t.ErrorExtractFiles(node.Text);
        RunExport(appState, job, failure, s => package.ExtractFolder(packageFolder, folder, s.Progress, s.Token),
            (r, extracted) => r.WithExtracted(extracted, t));
    }

    /// <summary><paramref name="report"/> listing the files <paramref name="extracted"/> wrote, and those
    /// it could not as its problems.</summary>
    private static ExportReport WithExtracted(this ExportReport report, ExtractedFiles extracted, Strings t)
    {
        var problems = extracted.Failures
            .Select(f => DescribeExtractFailure(t, f))
            .ToList();
        return report with { Written = extracted.Written, Problems = problems };
    }

    private static ExportProblem DescribeExtractFailure(Strings t, ExtractFailure failure)
    {
        var path = failure.File.FullName;
        if (failure.Error == null)
            return new ExportProblem(t.ExportOutsideFolder(path));
        return new ExportProblem($"{path}: {failure.Error.Message}", failure.Error.Details);
    }

    /// <summary>Shows the report of <paramref name="report"/> in a new tab.</summary>
    public static void ShowExportReport(AppState appState, ExportReport report)
    {
        var t = Strings.For(appState.Locale);
        var document = ExportReportText.Write(t, [report]);
        var tab = OpenNewTab(appState);
        appState.Tabs.MakeActive(tab);
        tab.ShowText(t.ExportReportTitle, document);
    }

    /// <summary>The runs of every export in the order the exports panel lists them: the running one,
    /// the waiting ones next first, then the finished ones newest first.</summary>
    public static List<int> ListExports(AppState appState)
    {
        var runs = new List<int>();
        var running = appState.Export.Running;
        if (running != null)
            runs.Add(running.Run);
        foreach (var waiting in appState.Export.Waiting)
            runs.Add(waiting.Run);
        for (var i = appState.FinishedExports.Count - 1; i >= 0; i--)
            runs.Add(appState.FinishedExports[i].Run);
        return runs;
    }

    /// <summary>The finished export <paramref name="run"/>, or null when it has not finished or is gone.</summary>
    public static ExportReport? FindFinishedExport(AppState appState, int run) => appState.FinishedExports.Find(r => r.Run == run);

    /// <summary>The folder the export <paramref name="run"/> writes into, wherever it is in the
    /// queue, or null when it is gone.</summary>
    public static string? FindExportFolder(AppState appState, int run)
    {
        var finished = FindFinishedExport(appState, run);
        if (finished != null)
            return finished.Folder;
        var queued = FindQueuedExport(appState, run);
        if (queued == null)
            return null;
        return queued.Job.Folder;
    }

    private static QueuedExport? FindQueuedExport(AppState appState, int run)
    {
        var running = appState.Export.Running;
        if (running != null && running.Run == run)
            return running;
        return appState.Export.Waiting.FirstOrDefault(q => q.Run == run);
    }

    /// <summary>Does what activating the export <paramref name="run"/> means: shows the report of a
    /// finished one; nothing for one still running or waiting.</summary>
    public static void ActivateExport(AppState appState, int run)
    {
        var finished = FindFinishedExport(appState, run);
        if (finished == null)
            return;
        ShowExportReport(appState, finished);
    }

    /// <summary>Takes the export <paramref name="run"/> off the panel: out of the queue when it waits,
    /// off the history when it finished. A running one stays; only cancelling stops it.</summary>
    public static void RemoveExport(AppState appState, int run)
    {
        var finished = FindFinishedExport(appState, run);
        if (finished != null)
        {
            RemoveFinishedExport(appState, finished);
            return;
        }
        appState.Export.Withdraw(run);
    }

    /// <summary>Takes <paramref name="report"/> off the list of finished exports.</summary>
    public static void RemoveFinishedExport(AppState appState, ExportReport report) => appState.FinishedExports.Remove(report);

    public static void ClearFinishedExports(AppState appState) => appState.FinishedExports.Clear();
}
