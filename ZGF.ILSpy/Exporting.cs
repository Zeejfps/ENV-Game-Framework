using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>An export waiting for the user to say whether it may write into <see cref="Folder"/>,
/// which already holds other files.</summary>
public sealed record ExportRequest(string Folder, Action Start);

/// <summary>Writing code, projects, solutions and portable PDBs to files in the background, one
/// export at a time, which <see cref="Workspace.Export"/> tracks and can cancel.</summary>
public static class Exporting
{
    private const string ProjectExtension = ".csproj";

    /// <summary>Whether <paramref name="node"/>'s code can be saved now: it shows code, and no
    /// other export is running.</summary>
    public static bool CanSaveCode(this Workspace w, BrowseNode? node)
    {
        if (node == null)
            return false;
        if (w.IsExporting)
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
        var start = w.Export.Begin(node.Text);
        var saved = await Background.Run(() => SaveCodeFile(node, language, options, path, start.Token));
        w.Export.End();
        if (!HasFailed(saved.Error))
            return;
        w.ReportError(Strings.For(w.Locale).ErrorSaveCode(node.Text), saved.Error!.Message);
    }

    private static bool SaveCodeFile(BrowseNode node, DecompilationLanguage language, DecompilerOptions options, string path,
        CancellationToken cancellationToken)
    {
        node.SaveCode(language, options, path, cancellationToken);
        return true;
    }

    /// <summary>Whether an export that ended with <paramref name="error"/> failed, rather than
    /// finished or was cancelled.</summary>
    private static bool HasFailed(Exception? error)
    {
        if (error == null)
            return false;
        return error is not OperationCanceledException;
    }

    /// <summary>Decompiles <paramref name="assembly"/> into a C# project whose file is
    /// <paramref name="projectPath"/>, asking first when its folder already holds other files.</summary>
    public static void ExportProject(this Workspace w, LoadedAssembly assembly, string projectPath)
    {
        var folder = Path.GetDirectoryName(projectPath)!;
        w.ConfirmFolder(folder, projectPath, () => w.StartProjectExport(assembly, projectPath));
    }

    private static async void StartProjectExport(this Workspace w, LoadedAssembly assembly, string projectPath)
    {
        var options = w.DecompilerOptions;
        var start = w.Export.Begin(assembly.Name);
        var exported = await Background.Run(() => AssemblyExport.ExportProject(assembly, options, projectPath, start.Progress, start.Token));
        w.Export.End();
        if (HasFailed(exported.Error))
        {
            w.ReportError(Strings.For(w.Locale).ErrorExportProject(assembly.Name), exported.Error!.Message);
            return;
        }
        if (exported.Value == null)
            return;
        w.ReportIncompleteProject(exported.Value);
    }

    /// <summary>Decompiles every open assembly into a C# project beside the solution at
    /// <paramref name="solutionPath"/>, asking first when its folder already holds other files.</summary>
    public static void ExportSolution(this Workspace w, string solutionPath)
    {
        var folder = Path.GetDirectoryName(solutionPath)!;
        w.ConfirmFolder(folder, solutionPath, () => w.StartSolutionExport(solutionPath));
    }

    private static async void StartSolutionExport(this Workspace w, string solutionPath)
    {
        var options = w.DecompilerOptions;
        var assemblies = w.Assemblies.Assemblies;
        var start = w.Export.Begin(Path.GetFileName(solutionPath));
        var exported = await Background.Run(() => AssemblyExport.ExportSolution(assemblies, options, solutionPath, start.Progress, start.Token));
        w.Export.End();
        if (HasFailed(exported.Error))
        {
            w.ReportError(Strings.For(w.Locale).ErrorExportSolution, exported.Error!.Message);
            return;
        }
        if (exported.Value == null)
            return;
        foreach (var project in exported.Value)
            w.ReportIncompleteProject(project);
    }

    private static void ReportIncompleteProject(this Workspace w, ExportedProject project)
    {
        if (project.Errors.Count == 0)
            return;
        var details = string.Join(Environment.NewLine, project.Errors);
        w.ReportError(Strings.For(w.Locale).ErrorProjectIncomplete(project.Assembly.Name), details);
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
        var start = w.Export.Begin(assembly.Name);
        var generated = await Background.Run(() => GeneratePdbFile(assembly, options, path, start));
        w.Export.End();
        if (!HasFailed(generated.Error))
            return;
        w.ReportError(Strings.For(w.Locale).ErrorGeneratePdb(assembly.Name), generated.Error!.Message);
    }

    private static bool GeneratePdbFile(LoadedAssembly assembly, DecompilerOptions options, string path, ExportStart start)
    {
        AssemblyExport.GeneratePdb(assembly, options, path, start.Progress, start.Token);
        return true;
    }

    /// <summary>Writes a portable PDB of each open assembly into <paramref name="folder"/>, named
    /// after the assembly. Assemblies that cannot have one, or fail, are reported together.</summary>
    public static async void GeneratePdbs(this Workspace w, string folder)
    {
        var options = w.DecompilerOptions;
        var assemblies = w.Assemblies.Assemblies;
        var start = w.Export.Begin(Path.GetFileName(folder));
        var failures = await Background.Run(() => AssemblyExport.GeneratePdbs(assemblies, options, folder, start.Progress, start.Token));
        w.Export.End();
        w.ReportPdbFailures(failures);
    }

    private static void ReportPdbFailures(this Workspace w, Outcome<List<PdbFailure>> failures)
    {
        var t = Strings.For(w.Locale);
        if (HasFailed(failures.Error))
        {
            w.ReportError(t.ErrorGeneratePdbs, failures.Error!.Message);
            return;
        }
        if (failures.Value == null)
            return;
        if (failures.Value.Count == 0)
            return;
        var lines = failures.Value.Select(f => DescribePdbFailure(t, f));
        var details = string.Join(Environment.NewLine, lines);
        w.ReportError(t.ErrorGeneratePdbs, details);
    }

    private static string DescribePdbFailure(Strings t, PdbFailure failure)
    {
        var name = failure.Assembly.Name;
        if (failure.Error == null)
            return t.ErrorNoPdbEntry(name);
        return $"{name}: {failure.Error}";
    }
}
