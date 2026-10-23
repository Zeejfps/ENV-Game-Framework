using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Asks what to export: the open assemblies to tick, each project's name, the folder, and
/// for several the solution's name. Warns in place about a folder that holds files and names that
/// clash, and exports only once nothing stops it.</summary>
internal static class ExportDialogView
{
    private const float ChoicesHeight = 180f;
    private const float ProjectNameWidth = 200f;

    public static void Render(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var setup = w.ExportSetup;
        var card = new Box { Width = Sizing.Grow(0f, AppLayout.PickerWidth) };
        using (var dialog = ui.Dialog("export-setup", t.ExportDialogTitle, ref setup.IsOpen, card))
        {
            if (!dialog.IsOpen)
                return;
            Choices(ui, setup);
            Folder(ui, setup);
            SolutionName(ui, setup);
            Clash(ui, setup);
            Actions(ui, w);
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
