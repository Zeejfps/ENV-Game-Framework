using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Asks what to export, in a window of its own: the open assemblies to tick, each project's
/// name, the folder, and for several the solution's name. Warns in place about a folder that holds
/// files, says beside the buttons what stops the export, and exports only once nothing does.</summary>
internal static class ExportDialogView
{
    private const float WindowWidth = 640f;
    private const float WindowHeight = 720f;
    private const float ChoicesHeight = 220f;
    private const float AssemblyColumnWidth = 300f;

    /// <summary>The decompiler's names for the toggles the dialog offers.</summary>
    private const string SdkStyleProjects = "UseSdkStyleProjectFormat";
    private const string FolderPerNamespace = "UseNestedDirectoriesForNamespaces";
    private const string RemoveDeadCode = "RemoveDeadCode";
    private const string RemoveDeadStores = "RemoveDeadStores";
    private const string DebugSymbolNames = "UseDebugSymbols";

    public static void Render(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var setup = w.ExportSetup;
        using (var dialog = ui.WindowDialog("export-setup", t.ExportDialogTitle, ref setup.IsOpen, WindowWidth, WindowHeight, ref setup.Window))
        {
            if (!dialog.IsOpen)
                return;
            Body(ui, w);
            Footer(ui, w);
        }
    }

    /// <summary>Everything above the buttons, scrolling when the window is too short for it, so the
    /// buttons stay in reach.</summary>
    private static void Body(Ui<UiState> ui, Workspace w)
    {
        var theme = ui.Theme;
        var setup = w.ExportSetup;
        using (ui.Scroll("export-body", new Box { Height = Sizing.Grow(), Gap = theme.DialogGap }))
        {
            Choices(ui, setup);
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

    private static void Choices(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (Section(ui))
        {
            using (ui.Row())
            {
                ui.Label(t.ExportAssemblies, bold: true);
                ui.Leaf(new Box { Width = Sizing.Grow(), Height = 0f });
                ui.Text(t.ExportSelectedCount(setup.IncludedCount, setup.Choices.Count), color: theme.TextMuted, size: theme.FontSizeSmall);
            }
            using (ui.Column(ChoicesFrame(theme)))
            {
                ChoicesHeader(ui, setup);
                ui.Separator();
                using (ui.Scroll("export-choices", new Box { Height = Sizing.Fixed(ChoicesHeight) }))
                {
                    foreach (var choice in setup.Choices)
                        Choice(ui, choice);
                }
            }
        }
    }

    private static Box ChoicesFrame(UiTheme theme) => new()
    {
        Gap = 0f,
        Background = theme.Field,
        BorderColor = theme.Border,
        BorderWidth = theme.BorderWidth,
        Radius = theme.RadiusSmall,
        Clip = true,
    };

    /// <summary>The list's columns: a box that ticks every assembly, over the assemblies, and the
    /// heading of the project names, both starting where the rows' do.</summary>
    private static void ChoicesHeader(Ui<UiState> ui, ExportSetup setup)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Row(ChoiceRow(theme)))
        {
            var all = setup.AllIncluded;
            if (ui.Checkbox(t.ExportSelectAll, ref all, id: "all", width: Sizing.Fixed(AssemblyColumnWidth)))
                setup.IncludeAll(all == true);
            ui.Text(t.ExportProjectName, ProjectNameCell(theme), color: theme.TextMuted, size: theme.FontSizeSmall);
        }
    }

    private static void Choice(Ui<UiState> ui, ExportChoice choice)
    {
        var theme = ui.Theme;
        using (ui.Id(choice.Assembly.FilePath))
        using (ui.Row(ChoiceRow(theme)))
        {
            ui.Checkbox(choice.Assembly.NameAndVersion, ref choice.Included, id: "include", width: Sizing.Fixed(AssemblyColumnWidth));
            ProjectName(ui, choice);
        }
    }

    private static Box ChoiceRow(UiTheme theme) => new()
    {
        Gap = theme.Gap,
        Padding = new Spacing(theme.Gap, 0f),
    };

    /// <summary>An editable name for a ticked assembly; for the others, the name it would get, muted,
    /// where the field's text would start.</summary>
    private static void ProjectName(Ui<UiState> ui, ExportChoice choice)
    {
        var theme = ui.Theme;
        if (choice.Included)
        {
            ui.TextField("name", ref choice.Name, width: Sizing.Grow());
            return;
        }
        ui.Text(choice.Name, ProjectNameCell(theme), color: theme.TextMuted, overflow: TextOverflow.Ellipsis);
    }

    private static Box ProjectNameCell(UiTheme theme) => new()
    {
        Width = Sizing.Grow(),
        Padding = new Spacing(theme.FieldPaddingX + theme.BorderWidth, 0f),
    };

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
