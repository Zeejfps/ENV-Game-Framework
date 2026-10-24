using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

/// <summary>
/// Asks what to export, in a window of its own, on two pages that each scroll as one. The main page
/// lists the ticked assemblies with their project names, then where they go, and the options; it
/// warns in place about a folder that holds files, says beside the buttons what stops the export, and
/// exports only once nothing does. The Choose page ticks assemblies among all the open ones,
/// filtered by name or path.
/// </summary>
internal static class ExportDialogView
{
    private const float WindowWidth = 640f;
    private const float WindowHeight = 720f;

    /// <summary>The columns of the table of ticked assemblies.</summary>
    private const int AssemblyColumn = 0;
    private const int VersionColumn = 1;
    private const int ProjectNameColumn = 2;

    /// <summary>The decompiler's names for the toggles the dialog offers.</summary>
    private const string SdkStyleProjects = "UseSdkStyleProjectFormat";
    private const string FolderPerNamespace = "UseNestedDirectoriesForNamespaces";
    private const string RemoveDeadCode = "RemoveDeadCode";
    private const string RemoveDeadStores = "RemoveDeadStores";
    private const string DebugSymbolNames = "UseDebugSymbols";

    public static void Render(Ui<UiState> ui, Workspace w)
    {
        Dialog(ui, w);
        w.ExportSetup.SettleWindow(ui.Input.IsDown(PointerButton.Left));
    }

    private static void Dialog(Ui<UiState> ui, Workspace w)
    {
        var setup = w.ExportSetup;
        var choosing = setup.Page == ExportPage.Choose;
        var title = PageTitle(ui.State.Strings, setup.Page);
        using (var dialog = ui.WindowDialog("export-setup", title, ref setup.IsOpen, WindowWidth, WindowHeight, ref setup.Window,
            closeOnEscape: !choosing))
        {
            if (!dialog.IsOpen)
                return;
            if (choosing)
            {
                ChoosePage(ui, setup);
                return;
            }
            Body(ui, w);
            Footer(ui, w);
        }
    }

    private static string PageTitle(Strings t, ExportPage page)
    {
        return page switch
        {
            ExportPage.Choose => t.ExportChooseTitle,
            _ => t.ExportDialogTitle,
        };
    }

    /// <summary>Everything above the buttons, scrolling when the window is too short for it, so the
    /// buttons stay in reach.</summary>
    private static void Body(Ui<UiState> ui, Workspace w)
    {
        var theme = ui.Theme;
        var setup = w.ExportSetup;
        using (ui.Scroll("export-body", new Box { Height = Sizing.Grow(), Gap = theme.DialogGap }))
        {
            Chosen(ui, w);
            Destination(ui, setup);
            ProjectOptions(ui, w);
            CodeOptions(ui, w);
            BuildOutput(ui, setup);
        }
    }

    /// <summary>A heading and the controls under it, set apart from the next section.</summary>
    private static Ui.Scope Section(Ui ui)
    {
        return ui.Column(new Box { Gap = ui.Theme.GapTight });
    }

    /// <summary>A muted caption over a field in a section.</summary>
    private static void Caption(Ui ui, string text)
    {
        var theme = ui.Theme;
        ui.Text(text, color: theme.TextMuted, size: theme.FontSizeSmall);
    }

    /// <summary>A bordered list on the field color, its rows clipped to its rounded corners.</summary>
    private static Box ListFrame(UiTheme theme) => new()
    {
        Gap = 0f,
        Background = theme.Field,
        BorderColor = theme.Border,
        BorderWidth = theme.BorderWidth,
        Radius = theme.RadiusSmall,
        Clip = true,
    };

    /// <summary>The ticked assemblies as a table of their project names, its columns resized by
    /// dragging their borders and its rows picked out to remove together, over a strip that adds and
    /// removes assemblies and counts them.</summary>
    private static void Chosen(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var setup = w.ExportSetup;
        var chosen = setup.IncludedChoices();
        using (Section(ui))
        {
            ui.Label(t.ExportAssemblies, bold: true);
            using (ui.Column(ListFrame(theme)))
            {
                if (chosen.Count == 0)
                    NoneChosen(ui);
                else
                    ChosenTable(ui, w, chosen);
                ui.Separator();
                TableStrip(ui, setup);
            }
        }
    }

    private static void NoneChosen(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var box = new Box { Padding = new Spacing(theme.Gap, theme.GapTight) };
        ui.Text(t.ExportNoneChosen, box, color: theme.TextMuted);
    }

    /// <summary>The table, its header naming the columns, a row per ticked assembly; the rows picked
    /// out go once they are all recorded, when asked to.</summary>
    private static void ChosenTable(Ui<UiState> ui, Workspace w, List<ExportChoice> chosen)
    {
        var t = ui.State.Strings;
        var setup = w.ExportSetup;
        var table = ui.Table("chosen", ui.State.ExportColumns, setup.RowSelection);
        table.Header([t.ExportAssemblyColumn, t.ExportVersionColumn, t.ExportProjectName]);
        var removeRequested = false;
        using (var rows = table.FitBody(chosen))
        {
            removeRequested = rows.RemoveRequested;
            var clashing = setup.ClashingChoice;
            for (var i = 0; i < chosen.Count; i++)
                ChosenRow(ui, rows, i, setup, chosen, clashing);
        }
        if (removeRequested)
            setup.RemoveSelectedRows();
    }

    /// <summary>An assembly being exported: its name, its version, then its project's name, edited
    /// in place, with what to do under it when it clashes with another's. Its path shows on hover.</summary>
    private static void ChosenRow(Ui<UiState> ui, TableRows<ExportChoice> rows, int index, ExportSetup setup, List<ExportChoice> chosen,
        ExportChoice? clashing)
    {
        var t = ui.State.Strings;
        var choice = chosen[index];
        using var row = rows.Row(index);
        using (row.Cell(AssemblyColumn))
            AssemblyName(ui, choice);
        using (row.Cell(VersionColumn))
            VersionText(ui, choice);
        using (row.Cell(ProjectNameColumn))
        {
            using (ui.Column(new Box { Width = Sizing.Grow(), Gap = 0f }))
            {
                ui.TextField("name", ref choice.Name, t.ExportProjectName, Sizing.Grow(), look: FieldLook.Cell);
                if (choice == clashing)
                    ClashNote(ui, choice);
            }
        }
        ui.Tooltip(row.Id, choice.Assembly.FilePath, TooltipAnchor.Pointer);
        PickOutWhileEditing(ui, setup, chosen, choice);
    }

    /// <summary>Picks out the row of the project name being edited alone, so what Remove takes is
    /// never in doubt.</summary>
    private static void PickOutWhileEditing(Ui<UiState> ui, ExportSetup setup, List<ExportChoice> chosen, ExportChoice choice)
    {
        if (!ui.HasFocus(ui.GetId("name")))
            return;
        if (setup.RowSelection.IsOnly(choice))
            return;
        setup.RowSelection.Select(choice, SelectionChange.Replace, chosen);
    }

    private static void AssemblyName(Ui<UiState> ui, ExportChoice choice)
    {
        ui.Text(choice.Assembly.Name, new Box { Width = Sizing.Grow() }, overflow: TextOverflow.Ellipsis);
    }

    private static void VersionText(Ui<UiState> ui, ExportChoice choice)
    {
        var theme = ui.Theme;
        var version = choice.Assembly.Version;
        if (version == null)
            return;
        ui.Text(version.ToString(), new Box { Width = Sizing.Grow() }, color: theme.TextMuted, overflow: TextOverflow.Ellipsis);
    }

    private static void ClashNote(Ui<UiState> ui, ExportChoice choice)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var box = new Box { Padding = new Spacing(theme.FieldPaddingX + theme.BorderWidth, 0f, 0f, theme.GapTight) };
        ui.Text(t.ExportNameClash(choice.Name.Trim()), box, color: theme.Danger, size: theme.FontSizeSmall, wrap: true);
    }

    /// <summary>The table's foot: + opens the Choose page, − removes the picked-out rows, and the
    /// count of what is included.</summary>
    private static void TableStrip(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Row(new Box { Gap = theme.GapTight, Padding = new Spacing(theme.GapTight, theme.GapTight, theme.Gap, theme.GapTight) }))
        {
            if (ui.IconButton("add", LucideIcons.Plus, LucideIcons.Family, t.ExportAdd))
                setup.Choose();
            var anyPicked = setup.RowSelection.Count > 0;
            if (ui.IconButton("remove-selected", LucideIcons.Minus, LucideIcons.Family, t.ExportRemoveSelected, enabled: anyPicked))
                setup.RemoveSelectedRows();
            ui.Leaf(new Box { Width = Sizing.Grow(), Height = 0f });
            SelectedCount(ui, setup);
        }
    }

    private static void SelectedCount(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        Caption(ui, t.ExportSelectedCount(setup.IncludedCount, setup.Choices.Count));
    }

    /// <summary>Where the export goes: the folder, a warning when it holds files, and for several
    /// assemblies the solution's name.</summary>
    private static void Destination(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (Section(ui))
        {
            ui.Label(t.ExportDestination, bold: true);
            using (ui.Row(new Box { Gap = theme.GapTight }))
            {
                ui.TextField("folder", ref setup.Folder, t.ExportFolderPlaceholder, Sizing.Grow());
                if (ui.Button(t.ExportBrowse, id: "browse"))
                    ui.PickFolder(t.ExportChooseFolderTitle, setup.Folder, path => setup.Folder = path);
            }
            if (setup.FolderHoldsFiles)
                ui.Text(t.ExportFolderWarning, color: theme.Warning, size: theme.FontSizeSmall, wrap: true);
            SolutionName(ui, setup);
        }
    }

    private static void SolutionName(Ui<UiState> ui, ExportSetup setup)
    {
        if (!setup.IsSolution)
            return;
        var t = ui.State.Strings;
        var placeholder = ui.Memo("export-solution-default", setup.Folder, static f => ExportSetup.DefaultSolutionNameFor(f));
        Caption(ui, t.ExportSolution);
        ui.TextField("solution", ref setup.SolutionName, placeholder, width: Sizing.Grow());
    }

    /// <summary>How the projects are laid out. Kept with the decompiler options, so they stay for the
    /// next export, but only shown here.</summary>
    private static void ProjectOptions(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        using (Section(ui))
        {
            ui.Label(t.ExportOptionsProject, bold: true);
            Toggle(ui, w, SdkStyleProjects);
            Toggle(ui, w, FolderPerNamespace);
        }
    }

    /// <summary>The decompiler options that matter most for exported code. They are the same as in
    /// the decompiler options panel, so changing them changes the code being browsed too, which the
    /// note under the heading says before they are changed.</summary>
    private static void CodeOptions(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        using (Section(ui))
        {
            ui.Label(t.ExportOptionsCode, bold: true);
            Caption(ui, t.ExportOptionsCodeNote);
            Toggle(ui, w, RemoveDeadCode);
            Toggle(ui, w, RemoveDeadStores);
            Toggle(ui, w, DebugSymbolNames);
        }
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
        if (ui.Checkbox(label, ref on, id: name, compact: true))
            w.SetDecompilerToggle(toggle, on);
    }

    /// <summary>What is written besides the code: the strong-name key to sign with, and PDBs.</summary>
    private static void BuildOutput(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        using (Section(ui))
        {
            ui.Label(t.ExportBuildOutput, bold: true);
            Signing(ui, setup);
            Pdbs(ui, setup);
        }
    }

    private static void Signing(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        Caption(ui, t.ExportKeyFile);
        using (ui.Row(new Box { Gap = theme.GapTight }))
        {
            ui.TextField("key-file", ref setup.StrongNameKeyFile, t.ExportKeyFileNone, Sizing.Grow());
            if (ui.Button(t.ExportBrowse, id: "browse-key"))
                ui.PickFile(t.ExportChooseKeyTitle, null, [new FileFilter(t.FileFilterSnk, ["*.snk"])], path => setup.StrongNameKeyFile = path);
        }
        if (setup.KeyFileMissing)
            ui.Text(t.ExportKeyFileMissing, color: theme.Danger, size: theme.FontSizeSmall, wrap: true);
    }

    /// <summary>Whether to write PDBs, and only once they are written, whether they embed the sources.</summary>
    private static void Pdbs(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        ui.Checkbox(t.ExportGeneratePdb, ref setup.GeneratePdb, id: "pdb", compact: true);
        if (!setup.GeneratePdb)
            return;
        using (ui.Row(new Box { Padding = new Spacing(theme.CheckboxSize + theme.Gap, 0f, 0f, 0f) }))
            ui.Checkbox(t.ExportEmbedSources, ref setup.EmbedSourcesInPdb, id: "embed", compact: true);
    }

    /// <summary>What stops the export, if anything, beside the buttons, set off from the body by a rule.</summary>
    private static void Footer(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var setup = w.ExportSetup;
        ui.Separator();
        using (ui.Row(new Box { Gap = theme.Gap }))
        {
            var blocker = setup.Blocker;
            var color = IsMistake(blocker) ? theme.Danger : theme.TextMuted;
            ui.Text(BlockerText(t, setup, blocker), color: color, size: theme.FontSizeSmall, wrap: true);
            if (ui.Button(t.ExportCancel, id: "export-cancel", variant: ButtonVariant.Ghost))
                setup.Close();
            if (ui.Button(t.ExportConfirm, enabled: setup.CanExport, id: "export-start", variant: ButtonVariant.Primary))
                w.StartExport();
        }
    }

    /// <summary>Whether <paramref name="blocker"/> is something typed wrong rather than not yet given.</summary>
    private static bool IsMistake(ExportBlocker blocker)
    {
        return blocker == ExportBlocker.Clash || blocker == ExportBlocker.KeyFileMissing;
    }

    private static string BlockerText(Strings t, ExportSetup setup, ExportBlocker blocker)
    {
        return blocker switch
        {
            ExportBlocker.NoAssembly => t.ExportNeedsAssembly,
            ExportBlocker.NoFolder => t.ExportNeedsFolder,
            ExportBlocker.NamelessProject => t.ExportNeedsName,
            ExportBlocker.Clash => t.ExportNameClash(setup.Clash!),
            ExportBlocker.KeyFileMissing => t.ExportKeyFileMissing,
            _ => "",
        };
    }

    /// <summary>All the open assemblies to tick, ticked ones first, narrowed by a filter. Only the list
    /// scrolls, filling the window; Done or Escape goes back.</summary>
    private static void ChoosePage(Ui<UiState> ui, ExportSetup setup)
    {
        var theme = ui.Theme;
        if (ui.Input.TakeKey(KeyboardKey.Escape))
            setup.FinishChoosing();
        using (ui.Column(new Box { Height = Sizing.Grow(), Gap = theme.Gap }))
        {
            FilterRow(ui, setup.Picker);
            SelectShownRow(ui, setup.Picker);
            PickList(ui, setup.Picker);
        }
        ChooseFooter(ui, setup);
    }

    private static void FilterRow(Ui<UiState> ui, AssemblyPicker picker)
    {
        var t = ui.State.Strings;
        using (ui.Row())
        {
            ui.TextField("filter", ref picker.Filter, t.ExportFilter, Sizing.Grow(), LucideIcons.Search, LucideIcons.Family);
            ui.Checkbox(t.ExportSelectedOnly, ref picker.SelectedOnly, id: "selected-only");
        }
    }

    /// <summary>The box that ticks every shown assembly, lined up with the rows' boxes.</summary>
    private static void SelectShownRow(Ui<UiState> ui, AssemblyPicker picker)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Row(new Box { Padding = new Spacing(theme.Gap + theme.BorderWidth, 0f, 0f, 0f) }))
        {
            var all = picker.AllShownIncluded;
            if (ui.Checkbox(t.ExportSelectShown, ref all, id: "all", compact: true))
                picker.IncludeShown(all == true);
        }
    }

    private static void PickList(Ui<UiState> ui, AssemblyPicker picker)
    {
        var theme = ui.Theme;
        var shown = picker.Shown();
        using (ui.Column(ListFrame(theme) with { Height = Sizing.Grow() }))
        {
            if (shown.Count == 0)
            {
                NoMatch(ui, picker);
                return;
            }
            using (ui.Scroll("export-choices", new Box { Height = Sizing.Grow(), Padding = new Spacing(0f, theme.GapTight) }))
            {
                foreach (var choice in shown)
                    PickRow(ui, choice);
            }
        }
    }

    /// <summary>Says that the filter hides every assembly, with a way to clear it.</summary>
    private static void NoMatch(Ui<UiState> ui, AssemblyPicker picker)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Column(new Box { Height = Sizing.Grow(), Gap = theme.Gap, AlignX = Align.Center, AlignY = Align.Center }))
        {
            ui.Text(t.ExportNoMatch, color: theme.TextMuted);
            if (ui.Button(t.ExportClearFilter, id: "clear-filter", variant: ButtonVariant.Ghost))
                picker.ClearFilter();
        }
    }

    /// <summary>An assembly to tick, with its version, and its file's path on hover.</summary>
    private static void PickRow(Ui<UiState> ui, ExportChoice choice)
    {
        var theme = ui.Theme;
        using (ui.Id(choice.Assembly.FilePath))
        using (ui.Row(new Box { Gap = theme.Gap, Padding = new Spacing(theme.Gap, 0f) }))
        {
            ui.Checkbox(choice.Assembly.Name, ref choice.Included, id: "include", width: Sizing.Grow(), compact: true);
            ui.Tooltip(ui.GetId("include"), choice.Assembly.FilePath, TooltipAnchor.Pointer);
            var version = choice.Assembly.Version;
            if (version != null)
                Caption(ui, version.ToString());
        }
    }

    /// <summary>How many are ticked beside Done, set off from the list by a rule.</summary>
    private static void ChooseFooter(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        ui.Separator();
        using (ui.Row(new Box { Gap = theme.Gap }))
        {
            SelectedCount(ui, setup);
            ui.Leaf(new Box { Width = Sizing.Grow(), Height = 0f });
            if (ui.Button(t.ExportDone, id: "done", variant: ButtonVariant.Primary))
                setup.FinishChoosing();
        }
    }
}
