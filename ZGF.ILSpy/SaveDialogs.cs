using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Exports;

namespace ZGF.ILSpy;

/// <summary>The dialogs that ask where code, resources, PDBs, class diagrams and the files of packages are written.</summary>
internal static class SaveDialogs
{
    /// <summary>Asks where to save the code of the active tab's node, when it can be saved.</summary>
    public static void PickActiveCodeFile(Ui<UiState> ui, AppState appState)
    {
        var node = appState.Tabs.Active?.Node;
        if (!CanSaveCode(appState, node))
            return;
        PickCodeFile(ui, appState, node!);
    }

    /// <summary>Asks where to save <paramref name="node"/>: a resource's bytes under its own name,
    /// else its code in the current language as one file.</summary>
    public static void PickCodeFile(Ui<UiState> ui, AppState appState, BrowseNode node)
    {
        if (node.HasResource)
        {
            ResourceImageView.PickExport(ui, appState, node);
            return;
        }
        var t = ui.State.Strings;
        var fileName = node.SuggestCodeFileName(appState.Language);
        ui.PickSaveFile(t.ExportSaveCodeTitle, null, fileName, CodeFilters(t, appState.Language), path => SaveCode(appState, node, path));
    }

    private static FileFilter[] CodeFilters(Strings t, DecompilationLanguage language)
    {
        if (language == DecompilationLanguage.CSharp)
            return [new FileFilter(t.FileFilterCsharp, ["*.cs"]), new FileFilter(t.FileFilterAll, ["*.*"])];
        return [new FileFilter(t.FileFilterIl, ["*.il"]), new FileFilter(t.FileFilterAll, ["*.*"])];
    }


    /// <summary>Asks where to write the portable PDB of <paramref name="assembly"/>.</summary>
    public static void PickPdbFile(Ui<UiState> ui, AppState appState, LoadedAssembly assembly)
    {
        var t = ui.State.Strings;
        FileFilter[] filters = [new FileFilter(t.FileFilterPdb, ["*.pdb"])];
        ui.PickSaveFile(t.ExportPdbTitle, null, assembly.PdbFileName, filters, path => GeneratePdb(appState, assembly, path));
    }

    /// <summary>Asks for the folder the class diagram of <paramref name="assembly"/> is written into.</summary>
    public static void PickDiagramFolder(Ui<UiState> ui, AppState appState, LoadedAssembly assembly)
    {
        var t = ui.State.Strings;
        ui.PickFolder(t.ExportDiagramTitle, null, folder => CreateDiagram(appState, assembly, folder));
    }

    /// <summary>Asks for the folder the portable PDBs of every open assembly are written into.</summary>
    public static void PickPdbFolder(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        ui.PickFolder(t.ExportPdbsTitle, null, folder => GeneratePdbs(appState, folder));
    }

    /// <summary>Asks where to write the file of a package <paramref name="place"/> stands for.</summary>
    public static void PickPackageFile(Ui<UiState> ui, AppState appState, PackagePlace place)
    {
        var t = ui.State.Strings;
        FileFilter[] filters = [new FileFilter(t.FileFilterAll, ["*.*"])];
        ui.PickSaveFile(t.ExportExtractFileTitle, null, place.File.Name, filters, path => ExtractPackageFile(appState, place, path));
    }

    /// <summary>Asks for the folder the files of the package folder <paramref name="place"/> stands for,
    /// shown as <paramref name="name"/>, are extracted into.</summary>
    public static void PickExtractFolder(Ui<UiState> ui, AppState appState, string name, PackageFolderPlace place)
    {
        var t = ui.State.Strings;
        ui.PickFolder(t.ExportExtractFolderTitle, null, folder => ExtractPackageFolder(appState, name, place, folder));
    }
}
