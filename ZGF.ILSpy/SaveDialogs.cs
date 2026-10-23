using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The dialogs that ask where code, projects, solutions and PDBs are written.</summary>
internal static class SaveDialogs
{
    private const string SuggestedSolutionName = "Assemblies.sln";

    public static readonly UiIcon SaveCodeIcon = new(LucideIcons.Save, LucideIcons.Family);
    public static readonly UiIcon ExportIcon = new(LucideIcons.FolderOutput, LucideIcons.Family);
    public static readonly UiIcon PdbIcon = new(LucideIcons.Bug, LucideIcons.Family);

    /// <summary>Asks where to save the code of the active tab's node, when it can be saved.</summary>
    public static void PickActiveCodeFile(Ui<UiState> ui, Workspace w)
    {
        var node = w.Tabs.Active?.Node;
        if (!w.CanSaveCode(node))
            return;
        PickCodeFile(ui, w, node!);
    }

    /// <summary>Asks where to save <paramref name="node"/>'s code; for an assembly in C#, a project
    /// file is offered first, which exports the whole project.</summary>
    public static void PickCodeFile(Ui<UiState> ui, Workspace w, BrowseNode node)
    {
        var t = ui.State.Strings;
        if (IsProjectCandidate(w, node))
        {
            var name = node.Assembly.ProjectFileName;
            ui.PickSaveFile(t.ExportSaveCodeTitle, null, name, ProjectFilters(t), path => w.SaveCode(node, path));
            return;
        }
        var fileName = node.SuggestCodeFileName(w.Language);
        ui.PickSaveFile(t.ExportSaveCodeTitle, null, fileName, CodeFilters(t, w.Language), path => w.SaveCode(node, path));
    }

    private static bool IsProjectCandidate(Workspace w, BrowseNode node)
    {
        if (node.Kind != NodeKind.Assembly)
            return false;
        return w.Language == DecompilationLanguage.CSharp;
    }

    private static FileFilter[] ProjectFilters(Strings t)
    {
        return
        [
            new FileFilter(t.FileFilterProject, ["*.csproj"]),
            new FileFilter(t.FileFilterCsharp, ["*.cs"]),
        ];
    }

    private static FileFilter[] CodeFilters(Strings t, DecompilationLanguage language)
    {
        if (language == DecompilationLanguage.CSharp)
            return [new FileFilter(t.FileFilterCsharp, ["*.cs"]), new FileFilter(t.FileFilterAll, ["*.*"])];
        return [new FileFilter(t.FileFilterIl, ["*.il"]), new FileFilter(t.FileFilterAll, ["*.*"])];
    }

    /// <summary>Asks where to write the solution that every open assembly is exported into.</summary>
    public static void PickSolutionFile(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        FileFilter[] filters = [new FileFilter(t.FileFilterSolution, ["*.sln"])];
        ui.PickSaveFile(t.ExportSolutionTitle, null, SuggestedSolutionName, filters, w.ExportSolution);
    }

    /// <summary>Asks where to write the portable PDB of <paramref name="assembly"/>.</summary>
    public static void PickPdbFile(Ui<UiState> ui, Workspace w, LoadedAssembly assembly)
    {
        var t = ui.State.Strings;
        FileFilter[] filters = [new FileFilter(t.FileFilterPdb, ["*.pdb"])];
        ui.PickSaveFile(t.ExportPdbTitle, null, assembly.PdbFileName, filters, path => w.GeneratePdb(assembly, path));
    }

    /// <summary>Asks for the folder the portable PDBs of every open assembly are written into.</summary>
    public static void PickPdbFolder(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        ui.PickFolder(t.ExportPdbsTitle, null, w.GeneratePdbs);
    }
}
