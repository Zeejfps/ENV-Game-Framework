using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

/// <summary>
/// Asks what to export, in a window of its own, on two pages that each scroll as one. The main page
/// lists the ticked assemblies with their project names, then the folder, for several the solution's
/// name, and the options; it warns in place about a folder that holds files, says beside the buttons
/// what stops the export, and exports only once nothing does. The Choose page ticks assemblies among
/// all the open ones, filtered by name or path.
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
            Folder(ui, setup);
            SolutionName(ui, setup);
            ProjectOptions(ui, w);
            CodeOptions(ui, w);
            Signing(ui, setup);
            Pdbs(ui, setup);
        }
    }

    /// <summary>A heading and the controls under it, set apart from the next section.</summary>
    private static Ui.Scope Section(Ui ui)
    {
        return ui.Column(new Box { Gap = ui.Theme.GapTight });
    }

    /// <summary>The ticked assemblies, each with its project name and a way to drop it, under a
    /// heading that counts them and opens the Choose page.</summary>
    private static void Chosen(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (Section(ui))
        {
            using (ui.Row())
            {
                ui.Label(t.ExportAssemblies, bold: true);
                ui.Leaf(new Box { Width = Sizing.Grow(), Height = 0f });
                SelectedCount(ui, setup);
                if (ui.Button(t.ExportChoose, id: "choose"))
                    setup.Choose();
            }
            var chosen = setup.IncludedChoices();
            if (chosen.Count == 0)
            {
                ui.Text(t.ExportNoneChosen, color: theme.TextMuted);
                return;
            }
            var clashing = setup.ClashingChoice;
            foreach (var choice in chosen)
                ChosenRow(ui, choice, clashing);
        }
    }

    private static void SelectedCount(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        ui.Text(t.ExportSelectedCount(setup.IncludedCount, setup.Choices.Count), color: theme.TextMuted, size: theme.FontSizeSmall);
    }

    /// <summary>An assembly being exported: its name and version, its project's name, and a button
    /// that unticks it; under it, when its name clashes with another's, what to do.</summary>
    private static void ChosenRow(Ui<UiState> ui, ExportChoice choice, ExportChoice? clashing)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Id(choice.Assembly.FilePath))
        {
            using (ui.Row())
            {
                AssemblyName(ui, choice);
                ui.TextField("name", ref choice.Name, width: Sizing.Fixed(ProjectNameWidth));
                if (ui.IconButton("remove", LucideIcons.X, LucideIcons.Family, t.ExportRemove))
                    choice.Included = false;
            }
            if (choice == clashing)
                ui.Text(t.ExportNameClash(choice.Name.Trim()), color: theme.Danger, size: theme.FontSizeSmall, wrap: true);
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

    /// <summary>All the open assemblies to tick, ticked ones first, narrowed by a filter. Only the list
    /// scrolls, filling the window; Done or Escape goes back.</summary>
    private static void ChoosePage(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        if (ui.Input.TakeKey(KeyboardKey.Escape))
            setup.FinishChoosing();
        using (ui.Column(new Box { Height = Sizing.Grow(), Gap = theme.Gap }))
        {
            FilterRow(ui, setup.Picker);
            SelectShownRow(ui, setup);
            PickList(ui, setup.Picker);
        }
        using (ui.Row(new Box { Gap = theme.Gap, AlignX = Align.End }))
        {
            if (ui.Button(t.ExportDone, id: "done", variant: ButtonVariant.Primary))
                setup.FinishChoosing();
        }
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

    private static void SelectShownRow(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Row(new Box { Padding = new Spacing(theme.Gap + theme.BorderWidth, 0f) }))
        {
            var all = setup.Picker.AllShownIncluded;
            if (ui.Checkbox(t.ExportSelectShown, ref all, id: "all"))
                setup.Picker.IncludeShown(all == true);
            ui.Leaf(new Box { Width = Sizing.Grow(), Height = 0f });
            SelectedCount(ui, setup);
        }
    }

    private static void PickList(Ui<UiState> ui, AssemblyPicker picker)
    {
        var theme = ui.Theme;
        using (ui.Column(PickListFrame(theme)))
        using (ui.Scroll("export-choices", new Box { Height = Sizing.Grow() }))
        {
            foreach (var choice in picker.Shown())
                PickRow(ui, choice);
        }
    }

    private static Box PickListFrame(UiTheme theme) => new()
    {
        Height = Sizing.Grow(),
        Gap = 0f,
        Background = theme.Field,
        BorderColor = theme.Border,
        BorderWidth = theme.BorderWidth,
        Radius = theme.RadiusSmall,
        Clip = true,
    };

    private static void PickRow(Ui<UiState> ui, ExportChoice choice)
    {
        var theme = ui.Theme;
        using (ui.Id(choice.Assembly.FilePath))
        using (ui.Row(new Box { Gap = theme.Gap, Padding = new Spacing(theme.Gap, 0f) }))
        {
            ui.Checkbox(choice.Assembly.Name, ref choice.Included, id: "include", width: Sizing.Grow());
            var version = choice.Assembly.Version;
            if (version != null)
                ui.Text(version.ToString(), color: theme.TextMuted, size: theme.FontSizeSmall);
        }
    }

    private static void Folder(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (Section(ui))
        {
            ui.Label(t.ExportFolder, bold: true);
            using (ui.Row(new Box { Gap = theme.GapTight }))
            {
                ui.TextField("folder", ref setup.Folder, width: Sizing.Grow());
                if (ui.Button(t.ExportBrowse, id: "browse"))
                    ui.PickFolder(t.ExportChooseFolderTitle, setup.Folder, path => setup.Folder = path);
            }
            if (setup.FolderHoldsFiles)
                ui.Text(t.ExportFolderWarning, color: theme.Warning, size: theme.FontSizeSmall, wrap: true);
        }
    }

    private static void SolutionName(Ui<UiState> ui, ExportSetup setup)
    {
        if (!setup.IsSolution)
            return;
        var t = ui.State.Strings;
        var placeholder = ui.Memo("export-solution-default", setup.Folder, static f => ExportSetup.DefaultSolutionNameFor(f));
        using (Section(ui))
        {
            ui.Label(t.ExportSolution, bold: true);
            ui.TextField("solution", ref setup.SolutionName, placeholder, width: Sizing.Grow());
        }
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
        var theme = ui.Theme;
        using (Section(ui))
        {
            ui.Label(t.ExportOptionsCode, bold: true);
            ui.Text(t.ExportOptionsCodeNote, color: theme.TextMuted, size: theme.FontSizeSmall, wrap: true);
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
        if (ui.Checkbox(label, ref on, id: name))
            w.SetDecompilerToggle(toggle, on);
    }

    private static void Signing(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (Section(ui))
        {
            ui.Label(t.ExportKeyFile, bold: true);
            using (ui.Row(new Box { Gap = theme.GapTight }))
            {
                ui.TextField("key-file", ref setup.StrongNameKeyFile, t.ExportKeyFileNone, width: Sizing.Grow());
                if (ui.Button(t.ExportBrowse, id: "browse-key"))
                    ui.PickFile(t.ExportChooseKeyTitle, null, [new FileFilter(t.FileFilterSnk, ["*.snk"])], path => setup.StrongNameKeyFile = path);
            }
            if (setup.KeyFileMissing)
                ui.Text(t.ExportKeyFileMissing, color: theme.Danger, size: theme.FontSizeSmall, wrap: true);
        }
    }

    /// <summary>Whether to write PDBs, and only once they are written, whether they embed the sources.</summary>
    private static void Pdbs(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (Section(ui))
        {
            ui.Checkbox(t.ExportGeneratePdb, ref setup.GeneratePdb, id: "pdb");
            if (!setup.GeneratePdb)
                return;
            using (ui.Row(new Box { Padding = new Spacing(theme.CheckboxSize + theme.Gap, 0f, 0f, 0f) }))
                ui.Checkbox(t.ExportEmbedSources, ref setup.EmbedSourcesInPdb, id: "embed");
        }
    }

    /// <summary>What stops the export, if anything, beside the buttons.</summary>
    private static void Footer(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var setup = w.ExportSetup;
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
}
