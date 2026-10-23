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
    private const float ProjectNameWidth = 240f;

    /// <summary>The decompiler's names for the toggles the dialog offers.</summary>
    private const string SdkStyleProjects = "UseSdkStyleProjectFormat";
    private const string FolderPerNamespace = "UseNestedDirectoriesForNamespaces";
    private const string RemoveDeadCode = "RemoveDeadCode";
    private const string RemoveDeadStores = "RemoveDeadStores";
    private const string DebugSymbolNames = "UseDebugSymbols";

    public static void Render(Ui<UiState> ui, Workspace w)
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
            Chosen(ui, setup);
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

    /// <summary>The ticked assemblies as a table of their project names, under a heading that counts
    /// them and opens the Choose page; with none ticked, a way to choose some.</summary>
    private static void Chosen(Ui<UiState> ui, ExportSetup setup)
    {
        var chosen = setup.IncludedChoices();
        using (Section(ui))
        {
            ChosenHeading(ui, setup, chosen.Count > 0);
            if (chosen.Count == 0)
            {
                NoneChosen(ui, setup);
                return;
            }
            ChosenTable(ui, chosen, setup.ClashingChoice);
        }
    }

    private static void ChosenHeading(Ui<UiState> ui, ExportSetup setup, bool anyChosen)
    {
        var t = ui.State.Strings;
        using (ui.Row())
        {
            ui.Label(t.ExportAssemblies, bold: true);
            ui.Leaf(new Box { Width = Sizing.Grow(), Height = 0f });
            if (!anyChosen)
                return;
            SelectedCount(ui, setup);
            if (ui.Button(t.ExportChoose, id: "choose"))
                setup.Choose();
        }
    }

    private static void SelectedCount(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        Caption(ui, t.ExportSelectedCount(setup.IncludedCount, setup.Choices.Count));
    }

    private static void NoneChosen(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Row(ListFrame(theme) with { Padding = new Spacing(theme.Gap, theme.GapTight) }))
        {
            ui.Text(t.ExportNoneChosen, new Box { Width = Sizing.Grow() }, color: theme.TextMuted);
            if (ui.Button(t.ExportChoose, id: "choose", variant: ButtonVariant.Primary))
                setup.Choose();
        }
    }

    private static void ChosenTable(Ui<UiState> ui, List<ExportChoice> chosen, ExportChoice? clashing)
    {
        var theme = ui.Theme;
        using (ui.Column(ListFrame(theme)))
        {
            ChosenColumns(ui);
            ui.Separator();
            foreach (var choice in chosen)
                ChosenRow(ui, choice, clashing);
        }
    }

    /// <summary>The table's column headings, over the name and the project name field.</summary>
    private static void ChosenColumns(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Row(TableRow(theme)))
        {
            ui.Text(t.ExportAssemblyColumn, new Box { Width = Sizing.Grow() }, color: theme.TextMuted, size: theme.FontSizeSmall);
            ui.Text(t.ExportProjectName, new Box { Width = Sizing.Fixed(ProjectNameWidth) }, color: theme.TextMuted, size: theme.FontSizeSmall);
            ui.Leaf(new Box { Width = theme.IconButtonSize, Height = 0f });
        }
    }

    private static Box TableRow(UiTheme theme) => new()
    {
        Padding = new Spacing(theme.Gap, theme.GapTight),
    };

    /// <summary>An assembly being exported: its name and version, its project's name, and a button
    /// that unticks it; under it, when its name clashes with another's, what to do.</summary>
    private static void ChosenRow(Ui<UiState> ui, ExportChoice choice, ExportChoice? clashing)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Id(choice.Assembly.FilePath))
        {
            using (ui.Row(TableRow(theme)))
            {
                AssemblyName(ui, choice);
                ui.TextField("name", ref choice.Name, t.ExportProjectName, Sizing.Fixed(ProjectNameWidth));
                if (ui.IconButton("remove", LucideIcons.X, LucideIcons.Family, t.ExportRemove))
                    choice.Included = false;
            }
            if (choice == clashing)
                ClashNote(ui, choice);
        }
    }

    /// <summary>The assembly's name, shortened to fit, and its version, muted.</summary>
    private static void AssemblyName(Ui<UiState> ui, ExportChoice choice)
    {
        var theme = ui.Theme;
        ui.Text(choice.Assembly.Name, new Box { Width = Sizing.Grow() }, overflow: TextOverflow.Ellipsis);
        var version = choice.Assembly.Version;
        if (version != null)
            ui.Text(version.ToString(), color: theme.TextMuted, size: theme.FontSizeSmall);
    }

    private static void ClashNote(Ui<UiState> ui, ExportChoice choice)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var box = new Box { Padding = new Spacing(theme.Gap, 0f, theme.Gap, theme.GapTight) };
        ui.Text(t.ExportNameClash(choice.Name.Trim()), box, color: theme.Danger, size: theme.FontSizeSmall, wrap: true);
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
            using (ui.Scroll("export-choices", new Box { Height = Sizing.Grow() }))
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
            ui.Tooltip(ui.GetId("include"), choice.Assembly.FilePath);
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
