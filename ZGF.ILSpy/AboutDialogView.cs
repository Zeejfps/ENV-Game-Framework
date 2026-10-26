using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Dialogs;

namespace ZGF.ILSpy;

/// <summary>The app's name and version over the versions of the decompiler, the runtime and the system,
/// with Copy, the licenses folder where the app ships one, and Close.</summary>
internal static class AboutDialogView
{
    private const string AppName = "ILSpy";
    private const string LicensesFolder = "licenses";

    private static readonly string? ShippedLicenses = FindShippedLicenses();

    public static void Render(Ui<UiState> ui)
    {
        var screen = ui.State;
        if (!screen.IsAboutOpen)
            return;
        var t = screen.Strings;
        var box = new Box { Width = Sizing.Grow(0f, AppLayout.AboutDialogWidth) };
        using var dialog = ui.Dialog("about", t.HelpAbout, ref screen.IsAboutOpen, box);
        if (!dialog.IsOpen)
            return;
        Heading(ui);
        ui.Separator();
        Versions(ui);
        Actions(ui);
    }

    private static void Heading(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var version = ui.Memo("about-version", t, static s => s.AboutVersion(AppVersions.App));
        ui.Text(AppName, size: theme.FontSizeTitle, bold: true);
        ui.Text(version, color: theme.TextMuted, wrap: true);
    }

    /// <summary>A row for each version, its name in a column as wide as the widest name.</summary>
    private static void Versions(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var labelWidth = MeasureLabels(ui, t);
        VersionRow(ui, t.AboutDecompiler, AppVersions.Decompiler, labelWidth);
        VersionRow(ui, t.AboutRuntime, AppVersions.Runtime, labelWidth);
        VersionRow(ui, t.AboutSystem, AppVersions.System, labelWidth);
    }

    private static float MeasureLabels(Ui<UiState> ui, Strings t)
    {
        var decompiler = ui.MeasureText(t.AboutDecompiler);
        var runtime = ui.MeasureText(t.AboutRuntime);
        var system = ui.MeasureText(t.AboutSystem);
        var widest = MathF.Max(decompiler, MathF.Max(runtime, system));
        return MathF.Ceiling(widest);
    }

    private static void VersionRow(Ui<UiState> ui, string label, string version, float labelWidth)
    {
        var theme = ui.Theme;
        using (ui.Row(new Box { Width = Sizing.Grow(), Gap = theme.Gap }))
        {
            ui.Text(label, new Box { Width = Sizing.Fixed(labelWidth) }, color: theme.TextMuted);
            ui.Text(version, wrap: true);
        }
    }

    private static void Actions(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        using (ui.Row(new Box { Gap = ui.Theme.GapTight }))
        {
            if (ui.Button(t.AboutCopy, id: "about-copy"))
                ui.ClipboardText = DescribeVersions(t);
            ui.Tooltip(ui.GetId("about-copy"), t.AboutCopyTooltip);
            LicensesButton(ui);
            ui.Spacer();
            if (ui.Button(t.AboutClose, id: "about-close", variant: ButtonVariant.Primary))
                CloseAbout(ui.State);
        }
    }

    /// <summary>Every version on a line of its own, as the dialog shows them.</summary>
    private static string DescribeVersions(Strings t)
    {
        string[] lines =
        [
            $"{AppName} {AppVersions.App}",
            $"{t.AboutDecompiler}: {AppVersions.Decompiler}",
            $"{t.AboutRuntime}: {AppVersions.Runtime}",
            $"{t.AboutSystem}: {AppVersions.System}",
        ];
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>Opens the folder of the licenses the app ships beside it, when it has one.</summary>
    private static void LicensesButton(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        if (ShippedLicenses == null)
            return;
        if (ui.Button(t.AboutLicenses, id: "about-licenses", variant: ButtonVariant.Ghost))
            ui.OpenFolder(ShippedLicenses);
    }

    private static string? FindShippedLicenses()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, LicensesFolder);
        if (!Directory.Exists(folder))
            return null;
        return folder;
    }
}
