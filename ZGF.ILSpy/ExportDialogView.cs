using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Asks what to export: the open assemblies to tick, each project's name, the folder, and
/// for several the solution's name. Warns in place about a folder that holds files and names that
/// clash, and exports only once nothing stops it.</summary>
internal static class ExportDialogView
{
    private const float ChoicesHeight = 180f;

    /// <summary>The decompiler's names for the toggles the dialog offers.</summary>
    private const string SdkStyleProjects = "UseSdkStyleProjectFormat";
    private const string FolderPerNamespace = "UseNestedDirectoriesForNamespaces";
    private const string RemoveDeadCode = "RemoveDeadCode";
    private const string RemoveDeadStores = "RemoveDeadStores";
    private const string DebugSymbolNames = "UseDebugSymbols";
    private const float ProjectNameWidth = 200f;

    public static void Render(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var setup = w.ExportSetup;
        var card = new Box { Width = Sizing.Grow(0f, AppLayout.PickerWidth), Height = Sizing.Percent(AppLayout.PickerHeight) };
        using (var dialog = ui.Dialog("export-setup", t.ExportDialogTitle, ref setup.IsOpen, card))
        {
            if (!dialog.IsOpen)
                return;
            Body(ui, w);
            Actions(ui, w);
        }
    }

    /// <summary>Everything above the buttons, scrolling when the window is too short for it, so the
    /// buttons stay in reach.</summary>
    private static void Body(Ui<UiState> ui, Workspace w)
    {
        var theme = ui.Theme;
        var setup = w.ExportSetup;
        using (ui.Scroll("export-body", new Box { Height = Sizing.Grow(), Gap = theme.Gap }))
        {
            Choices(ui, setup);
            Folder(ui, setup);
            SolutionName(ui, setup);
            Clash(ui, setup);
            ProjectOptions(ui, w);
            CodeOptions(ui, w);
            Signing(ui, setup);
            Pdbs(ui, setup);
        }
    }

    private static void Choices(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Row(new Box { Gap = theme.Gap, AlignY = Align.Center }))
        {
            var all = setup.AllIncluded;
            if (ui.Checkbox(t.ExportAssemblies, ref all, id: "all", width: Sizing.Grow(), bold: true))
                setup.IncludeAll(all == true);
            ui.Text(t.ExportProjectName, new Box { Width = Sizing.Fixed(ProjectNameWidth) }, color: theme.TextMuted, size: theme.FontSizeSmall);
        }
        var box = new Box { Height = Sizing.Fixed(ChoicesHeight), Padding = theme.BorderWidth, Background = theme.Field, Clip = true };
        using (ui.Scroll("export-choices", box))
        {
            foreach (var choice in setup.Choices)
                Choice(ui, choice);
        }
    }

    private static void Choice(Ui<UiState> ui, ExportChoice choice)
    {
        var theme = ui.Theme;
        using (ui.Id(choice.Assembly.FilePath))
        using (ui.Row(new Box { Gap = theme.Gap, Padding = new Spacing(theme.GapTight, 0f), AlignY = Align.Center }))
        {
            ui.Checkbox(choice.Assembly.NameAndVersion, ref choice.Included, id: "include", width: Sizing.Grow());
            ui.TextField("name", ref choice.Name, width: Sizing.Fixed(ProjectNameWidth));
        }
    }

    private static void Folder(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        ui.Label(t.ExportFolder, bold: true);
        using (ui.Row(new Box { Gap = theme.GapTight, AlignY = Align.Center }))
        {
            ui.TextField("folder", ref setup.Folder, width: Sizing.Grow());
            if (ui.Button(t.ExportBrowse, id: "browse"))
                ui.PickFolder(t.ExportChooseFolderTitle, setup.Folder, path => setup.Folder = path);
        }
        if (setup.FolderHoldsFiles)
            ui.Text(t.ExportFolderWarning, color: theme.Warning, size: theme.FontSizeSmall, wrap: true);
    }

    private static void SolutionName(Ui<UiState> ui, ExportSetup setup)
    {
        if (!setup.IsSolution)
            return;
        var t = ui.State.Strings;
        var placeholder = ui.Memo("export-solution-default", setup.Folder, static f => ExportSetup.DefaultSolutionNameFor(f));
        ui.Label(t.ExportSolution, bold: true);
        ui.TextField("solution", ref setup.SolutionName, placeholder, width: Sizing.Grow());
    }

    private static void Clash(Ui<UiState> ui, ExportSetup setup)
    {
        var clash = setup.Clash;
        if (clash == null)
            return;
        var t = ui.State.Strings;
        var theme = ui.Theme;
        ui.Text(t.ExportNameClash(clash), color: theme.Danger, size: theme.FontSizeSmall, wrap: true);
    }

    /// <summary>How the projects are laid out. Kept with the decompiler options, so they stay for the
    /// next export, but only shown here.</summary>
    private static void ProjectOptions(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        ui.Label(t.ExportOptionsProject, bold: true);
        Toggle(ui, w, SdkStyleProjects);
        Toggle(ui, w, FolderPerNamespace);
    }

    /// <summary>The decompiler options that matter most for exported code. They are the same as in
    /// the decompiler options panel, so changing them changes the code being browsed too, which the
    /// note under them says.</summary>
    private static void CodeOptions(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        ui.Label(t.ExportOptionsCode, bold: true);
        Toggle(ui, w, RemoveDeadCode);
        Toggle(ui, w, RemoveDeadStores);
        Toggle(ui, w, DebugSymbolNames);
        ui.Text(t.ExportOptionsCodeNote, color: theme.TextMuted, size: theme.FontSizeSmall, wrap: true);
    }

    /// <summary>A checkbox for the decompiler toggle named <paramref name="name"/>, labelled as the
    /// decompiler options panel labels it.</summary>
    private static void Toggle(Ui<UiState> ui, Workspace w, string name)
    {
        var t = ui.State.Strings;
        var toggle = DecompilerToggle.Find(name);
        if (toggle == null)
            return;
        var label = DecompilerOptionLabels.Label(t, name) ?? name;
        var on = w.DecompilerOptions.IsOn(toggle);
        if (ui.Checkbox(label, ref on, id: name))
            w.SetDecompilerToggle(toggle, on);
    }

    private static void Signing(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        ui.Label(t.ExportKeyFile, bold: true);
        using (ui.Row(new Box { Gap = theme.GapTight, AlignY = Align.Center }))
        {
            ui.TextField("key-file", ref setup.StrongNameKeyFile, t.ExportKeyFileNone, width: Sizing.Grow());
            if (ui.Button(t.ExportBrowse, id: "browse-key"))
                ui.PickFile(t.ExportChooseKeyTitle, null, [new FileFilter(t.FileFilterSnk, ["*.snk"])], path => setup.StrongNameKeyFile = path);
        }
        if (setup.KeyFileMissing)
            ui.Text(t.ExportKeyFileMissing, color: theme.Danger, size: theme.FontSizeSmall, wrap: true);
    }

    private static void Pdbs(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        ui.Checkbox(t.ExportGeneratePdb, ref setup.GeneratePdb, id: "pdb");
        using (ui.Row(new Box { Padding = new Spacing(theme.IconSize + theme.Gap, 0f, 0f, 0f) }))
            ui.Checkbox(t.ExportEmbedSources, ref setup.EmbedSourcesInPdb, id: "embed", enabled: setup.GeneratePdb);
    }

    private static void Actions(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var setup = w.ExportSetup;
        using (ui.DialogActions())
        {
            if (ui.Button(t.ExportCancel, id: "export-cancel", variant: ButtonVariant.Ghost))
                setup.Close();
            if (ui.Button(t.ExportConfirm, enabled: setup.CanExport, id: "export-start", variant: ButtonVariant.Primary))
                w.StartExport();
        }
    }
}
