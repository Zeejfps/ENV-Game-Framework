using System.Diagnostics;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;


/// <summary>Writing code, projects, solutions and portable PDBs to files in the background, one
/// export at a time in the order they were asked for, which <see cref="Workspace.Export"/> queues.
/// Each that runs ends in an <see cref="ExportReport"/> in <see cref="Workspace.FinishedExports"/>.</summary>
public static class Exporting
{
    private const string ProjectExtension = ".csproj";

    /// <summary>Whether <paramref name="node"/> can be saved: it shows code, or it is a resource,
    /// whose bytes are saved.</summary>
    public static bool CanSaveCode(this Workspace w, BrowseNode? node)
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
    public static void SaveCode(this Workspace w, BrowseNode node, string path)
    {
        if (node.HasResource)
        {
            w.SaveResource(node, path);
            return;
        }
        w.WriteCodeFile(node, path);
    }

    private static void WriteCodeFile(this Workspace w, BrowseNode node, string path)
    {
        var language = w.Language;
        var options = w.DecompilerOptions;
        var job = new ExportJob(node.Text, CodeKindOf(language), FolderOf(path));
        var failure = Strings.For(w.Locale).ErrorSaveCode(node.Text);
        w.RunExport(job, failure, s => SaveCodeFile(node, language, options, path, s.Token), (r, _) => r.Listing(path));
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
    private static async void RunExport<T>(this Workspace w, ExportJob job, string failure, Func<ExportStart, T> work,
        Func<ExportReport, T, ExportReport> complete)
    {
        w.EnqueueCommand(new ViewCommand.OpenPanel(DockPanels.Exports));
        var start = await w.Export.Enter(job);
        if (start.IsWithdrawn)
            return;
        var clock = Stopwatch.StartNew();
        var outcome = await Background.Run(() => work(start));
        w.Export.Leave(start);
        var report = ExportReport.Of(start.Run, job, outcome.Error, failure, clock.Elapsed);
        if (outcome.Error == null)
            report = complete(report, outcome.Value!);
        w.FinishedExports.Add(report);
    }

    /// <summary><paramref name="report"/> listing <paramref name="written"/>.</summary>
    private static ExportReport Listing(this ExportReport report, params string[] written) => report with { Written = written };

    /// <summary>Decompiles <paramref name="assembly"/> into a C# project whose file is
    /// <paramref name="projectPath"/>, with what <paramref name="extras"/> asks for besides. The export
    /// dialog warns about a folder that holds files before this is asked for.</summary>
    public static void ExportProject(this Workspace w, LoadedAssembly assembly, string projectPath, ProjectExtras? extras = null)
    {
        var withExtras = extras ?? ProjectExtras.None;
        var options = w.DecompilerOptions;
        var t = Strings.For(w.Locale);
        var job = new ExportJob(Path.GetFileNameWithoutExtension(projectPath), ExportKind.Project, FolderOf(projectPath));
        var failure = t.ErrorExportProject(assembly.Name);
        w.RunExport(job, failure, s => AssemblyExport.ExportProject(assembly, options, projectPath, withExtras, s.Progress, s.Token),
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
    private static void ExportSolution(this Workspace w, List<SolutionProject> projects, string solutionPath, ProjectExtras extras)
    {
        var options = w.DecompilerOptions;
        var t = Strings.For(w.Locale);
        var job = new ExportJob(Path.GetFileNameWithoutExtension(solutionPath), ExportKind.Solution, FolderOf(solutionPath));
        w.RunExport(job, t.ErrorExportSolution, s => AssemblyExport.ExportSolution(projects, options, solutionPath, extras, s.Progress, s.Token),
            (r, exported) => r.WithProjects(exported, solutionPath, t));
    }

    /// <summary>The assembly of the node the active tab shows, or null.</summary>
    public static LoadedAssembly? ActiveAssembly(this Workspace w)
    {
        var node = w.Tabs.Active?.Node;
        if (node == null)
            return null;
        return node.Assembly;
    }

    /// <summary>Exports what <paramref name="request"/> asks for: one assembly as a project straight
    /// into the folder, several as a solution with a folder per project.</summary>
    public static void StartExport(this Workspace w, ExportRequest request)
    {
        var folder = request.Folder;
        var projects = request.Projects;
        if (request.IsSolution)
        {
            var solutionPath = Path.Combine(folder, request.SolutionFileName);
            w.ExportSolution(projects, solutionPath, request.Extras);
            return;
        }
        var project = projects[0];
        var projectPath = Path.Combine(folder, AssemblyExport.CleanProjectName(project.Name) + ProjectExtension);
        w.ExportProject(project.Assembly, projectPath, request.Extras);
    }

    /// <summary>Writes a portable PDB of <paramref name="assembly"/>'s decompiled C# to
    /// <paramref name="path"/>.</summary>
    public static void GeneratePdb(this Workspace w, LoadedAssembly assembly, string path)
    {
        var options = w.DecompilerOptions;
        var job = new ExportJob(assembly.Name, ExportKind.Pdb, FolderOf(path));
        var failure = Strings.For(w.Locale).ErrorGeneratePdb(assembly.Name);
        w.RunExport(job, failure, s => GeneratePdbFile(assembly, options, path, s), (r, _) => r.Listing(path));
    }

    private static bool GeneratePdbFile(LoadedAssembly assembly, DecompilerOptions options, string path, ExportStart start)
    {
        AssemblyExport.GeneratePdb(assembly, options, path, start.Progress, start.Token);
        return true;
    }

    /// <summary>Writes a portable PDB of each open assembly into <paramref name="folder"/>, named
    /// after the assembly. Assemblies that cannot have one, or fail, are the report's problems.</summary>
    public static void GeneratePdbs(this Workspace w, string folder)
    {
        var options = w.DecompilerOptions;
        var assemblies = w.Assemblies.Assemblies;
        var t = Strings.For(w.Locale);
        var job = new ExportJob(Path.GetFileName(folder), ExportKind.Pdbs, folder);
        w.RunExport(job, t.ErrorGeneratePdbs, s => AssemblyExport.GeneratePdbs(assemblies, options, folder, s.Progress, s.Token),
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

    /// <summary>Shows the report of <paramref name="report"/> in a new tab.</summary>
    public static void ShowExportReport(this Workspace w, ExportReport report)
    {
        var t = Strings.For(w.Locale);
        var document = ExportReportText.Write(t, [report]);
        var tab = w.OpenNewTab();
        w.Tabs.MakeActive(tab);
        tab.ShowText(t.ExportReportTitle, document);
    }

    /// <summary>The runs of every export in the order the exports panel lists them: the running one,
    /// the waiting ones next first, then the finished ones newest first.</summary>
    public static List<int> ListExports(this Workspace w)
    {
        var runs = new List<int>();
        var running = w.Export.Running;
        if (running != null)
            runs.Add(running.Run);
        foreach (var waiting in w.Export.Waiting)
            runs.Add(waiting.Run);
        for (var i = w.FinishedExports.Count - 1; i >= 0; i--)
            runs.Add(w.FinishedExports[i].Run);
        return runs;
    }

    /// <summary>The finished export <paramref name="run"/>, or null when it has not finished or is gone.</summary>
    public static ExportReport? FindFinishedExport(this Workspace w, int run) => w.FinishedExports.Find(r => r.Run == run);

    /// <summary>The folder the export <paramref name="run"/> writes into, wherever it is in the
    /// queue, or null when it is gone.</summary>
    public static string? FolderOfExport(this Workspace w, int run)
    {
        var finished = w.FindFinishedExport(run);
        if (finished != null)
            return finished.Folder;
        var queued = w.FindQueuedExport(run);
        if (queued == null)
            return null;
        return queued.Job.Folder;
    }

    private static QueuedExport? FindQueuedExport(this Workspace w, int run)
    {
        var running = w.Export.Running;
        if (running != null && running.Run == run)
            return running;
        return w.Export.Waiting.FirstOrDefault(q => q.Run == run);
    }

    /// <summary>Does what activating the export <paramref name="run"/> means: shows the report of a
    /// finished one; nothing for one still running or waiting.</summary>
    public static void ActivateExport(this Workspace w, int run)
    {
        var finished = w.FindFinishedExport(run);
        if (finished == null)
            return;
        w.ShowExportReport(finished);
    }

    /// <summary>Takes the export <paramref name="run"/> off the panel: out of the queue when it waits,
    /// off the history when it finished. A running one stays; only cancelling stops it.</summary>
    public static void RemoveExport(this Workspace w, int run)
    {
        var finished = w.FindFinishedExport(run);
        if (finished != null)
        {
            w.RemoveFinishedExport(finished);
            return;
        }
        w.Export.Withdraw(run);
    }

    /// <summary>Takes <paramref name="report"/> off the list of finished exports.</summary>
    public static void RemoveFinishedExport(this Workspace w, ExportReport report) => w.FinishedExports.Remove(report);

    public static void ClearFinishedExports(this Workspace w) => w.FinishedExports.Clear();
}
