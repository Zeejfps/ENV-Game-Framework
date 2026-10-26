using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Dialogs;

namespace ZGF.ILSpy;

/// <summary>The app's icon beside its name, its version and Check for updates, which waits for the
/// feature to come, over the versions of the decompiler, the runtime and the system; then the licenses
/// folder where the app ships one, and Close.</summary>
internal static class AboutDialogView
{
    private const string AppName = "ILSpy";
    private const string LicensesFolder = "licenses";
    private const float IconScale = 4f;
    private const float NameScale = 1.25f;

    private static readonly string? ShippedLicenses = FindShippedLicenses();

    public static void Render(Ui<UiState> ui)
    {
        var screen = ui.State;
        if (!screen.IsAboutOpen)
            return;
        var t = screen.Strings;
        var box = new Box { Width = Sizing.Grow(0f, AppLayout.AboutDialogWidth) };
        using var dialog = ui.Dialog("about", t.AboutTitle, ref screen.IsAboutOpen, box);
        if (!dialog.IsOpen)
            return;
        using (ui.Column(new Box { Width = Sizing.Grow(), Gap = ui.Theme.DialogGap }))
        {
            Identity(ui);
            Versions(ui);
        }
        Actions(ui);
    }

    /// <summary>The app's icon beside its name, its version and Check for updates.</summary>
    private static void Identity(Ui<UiState> ui)
    {
        var theme = ui.Theme;
        using var row = ui.Row(new Box { Width = Sizing.Grow(), Gap = theme.DialogGap, AlignY = Align.Center });
        ui.AppIcon(theme.IconSize * IconScale);
        using var text = ui.Column(new Box { Width = Sizing.Grow(), Gap = theme.Gap });
        NameAndVersion(ui);
        CheckForUpdatesButton(ui);
    }

    private static void NameAndVersion(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var version = ui.Memo("about-version", t, static s => s.AboutVersion(AppVersions.App));
        using var lines = ui.Column(new Box { Width = Sizing.Grow(), Gap = theme.GapTight });
        ui.Text(AppName, size: theme.FontSizeTitle * NameScale, bold: true);
        ui.Text(version, color: theme.TextMuted, wrap: true);
    }

    /// <summary>The button that will look for a newer version, shown disabled until it can.</summary>
    private static void CheckForUpdatesButton(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        ui.Button(t.AboutCheckForUpdates, enabled: false, id: "about-updates", variant: ButtonVariant.Neutral);
    }

    /// <summary>A small row for each version, its name in a column as wide as the widest name.</summary>
    private static void Versions(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var labelWidth = MeasureLabels(ui, t);
        using var rows = ui.Column(new Box { Width = Sizing.Grow(), Gap = ui.Theme.GapTight });
        VersionRow(ui, t.AboutDecompiler, AppVersions.Decompiler, labelWidth);
        VersionRow(ui, t.AboutRuntime, AppVersions.Runtime, labelWidth);
        VersionRow(ui, t.AboutSystem, AppVersions.System, labelWidth);
    }

    private static float MeasureLabels(Ui<UiState> ui, Strings t)
    {
        var size = ui.Theme.FontSizeSmall;
        var decompiler = ui.MeasureText(t.AboutDecompiler, size);
        var runtime = ui.MeasureText(t.AboutRuntime, size);
        var system = ui.MeasureText(t.AboutSystem, size);
        var widest = MathF.Max(decompiler, MathF.Max(runtime, system));
        return MathF.Ceiling(widest);
    }

    private static void VersionRow(Ui<UiState> ui, string label, string version, float labelWidth)
    {
        var theme = ui.Theme;
        var size = theme.FontSizeSmall;
        using (ui.Row(new Box { Width = Sizing.Grow(), Gap = theme.Gap, AlignY = Align.Start }))
        {
            ui.Text(label, new Box { Width = Sizing.Fixed(labelWidth) }, color: theme.TextMuted, size: size);
            ui.Text(version, size: size, wrap: true);
        }
    }

    /// <summary>The licenses folder at the left and Close at the right.</summary>
    private static void Actions(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        using (ui.DialogActions())
        {
            LicensesButton(ui);
            ui.Spacer();
            if (ui.Button(t.AboutClose, id: "about-close", variant: ButtonVariant.Neutral))
                CloseAbout(ui.State);
        }
    }

    /// <summary>Opens the folder of the licenses the app ships beside it, when it has one.</summary>
    private static void LicensesButton(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        if (ShippedLicenses == null)
            return;
        var icon = new UiIcon(ui.State.Icons.AboutOpenLicensesFolder);
        if (ui.Button(t.AboutLicenses, id: "about-licenses", variant: ButtonVariant.Neutral, icon: icon))
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
