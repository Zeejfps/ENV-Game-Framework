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
    /// <summary>Asks where to save the active tab's node, when it can be saved: a resource's bytes
    /// under its own name, else its code in the current language as one file.</summary>
    public static void PickActiveCodeFile(Ui<UiState> ui, DocumentTabs tabs, DecompilationLanguage language)
    {
        var node = tabs.Active?.Node;
        if (!CanSaveCode(node))
            return;
        if (node.HasResource)
        {
            ResourceImageView.PickExport(ui, node);
            return;
        }
        PickCodeFile(ui, language, node);
    }

    /// <summary>Asks where to save the code of <paramref name="node"/> in <paramref name="language"/> as one file.</summary>
    public static void PickCodeFile(Ui<UiState> ui, DecompilationLanguage language, BrowseNode node)
    {
        var t = ui.State.Strings;
        var screen = ui.State;
        var fileName = NodeContentLoader.SuggestCodeFileName(node, language);
        ui.PickSaveFile(t.ExportSaveCodeTitle, null, fileName, CodeFilters(t, language),
            path => screen.Later((node, path), static (a, s) => SaveCode(a, s.node, s.path)));
    }

    private static FileFilter[] CodeFilters(Strings t, DecompilationLanguage language)
    {
        if (language == DecompilationLanguage.CSharp)
            return [new FileFilter(t.FileFilterCsharp, ["*.cs"]), new FileFilter(t.FileFilterAll, ["*.*"])];
        return [new FileFilter(t.FileFilterIl, ["*.il"]), new FileFilter(t.FileFilterAll, ["*.*"])];
    }


    /// <summary>Asks where to write the portable PDB of <paramref name="assembly"/>.</summary>
    public static void PickPdbFile(Ui<UiState> ui, LoadedModule assembly)
    {
        var t = ui.State.Strings;
        var screen = ui.State;
        FileFilter[] filters = [new FileFilter(t.FileFilterPdb, ["*.pdb"])];
        ui.PickSaveFile(t.ExportPdbTitle, null, assembly.PdbFileName, filters,
            path => screen.Later((assembly, path), static (a, s) => GeneratePdb(a, s.assembly, s.path)));
    }

    /// <summary>Asks for the folder the class diagram of <paramref name="assembly"/> is written into.</summary>
    public static void PickDiagramFolder(Ui<UiState> ui, LoadedModule assembly)
    {
        var t = ui.State.Strings;
        var screen = ui.State;
        ui.PickFolder(t.ExportDiagramTitle, null, folder => screen.Later((assembly, folder), static (a, s) => CreateDiagram(a, s.assembly, s.folder)));
    }

    /// <summary>Asks for the folder the portable PDBs of every open assembly are written into.</summary>
    public static void PickPdbFolder(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var screen = ui.State;
        ui.PickFolder(t.ExportPdbsTitle, null, folder => screen.Later(folder, static (a, f) => GeneratePdbs(a, f)));
    }

    /// <summary>Asks where to write the file of a package <paramref name="place"/> stands for.</summary>
    public static void PickPackageFile(Ui<UiState> ui, PackagePlace place)
    {
        var t = ui.State.Strings;
        var screen = ui.State;
        FileFilter[] filters = [new FileFilter(t.FileFilterAll, ["*.*"])];
        ui.PickSaveFile(t.ExportExtractFileTitle, null, place.File.Name, filters,
            path => screen.Later((place, path), static (a, s) => ExtractPackageFile(a, s.place, s.path)));
    }

    /// <summary>Asks for the folder the files of the package folder <paramref name="place"/> stands for,
    /// shown as <paramref name="name"/>, are extracted into.</summary>
    public static void PickExtractFolder(Ui<UiState> ui, string name, PackageFolderPlace place)
    {
        var t = ui.State.Strings;
        var screen = ui.State;
        ui.PickFolder(t.ExportExtractFolderTitle, null,
            folder => screen.Later((name, place, folder), static (a, s) => ExtractPackageFolder(a, s.name, s.place, s.folder)));
    }
}
