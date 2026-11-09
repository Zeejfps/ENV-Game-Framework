using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

namespace ZGF.Desktop.FilePicking;

/// <summary>Which of the three dialogs to run.</summary>
internal enum LinuxChooser
{
    File,
    Folder,
    SaveName,
}

/// <summary>
/// Linux file dialogs: the desktop's own through the XDG desktop portal
/// (<see cref="PortalFilePicker"/>), else the first of <c>zenity</c>, <c>kdialog</c> and
/// <c>yad</c> found on PATH. Most desktops ship a portal but not necessarily any of the three
/// tools; XFCE, for one, ships none of them.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxFilePicker : IFilePicker
{
    /// <summary>A command-line picker and the arguments to run it with.</summary>
    private sealed record Command(string Tool, string[] Args);

    private readonly Action<Action> _post;
    private readonly string? _zenity;
    private readonly string? _kdialog;
    private readonly string? _yad;
    private int _pickerOpen;

    public LinuxFilePicker(Action<Action> post)
    {
        _post = post;
        _zenity = FindOnPath("zenity");
        _kdialog = FindOnPath("kdialog");
        _yad = FindOnPath("yad");
    }

    public void PickFolder(string title, string? initialDirectory, Action<string> onPicked) =>
        Pick(title, LinuxChooser.Folder, initialDirectory, null, null, onPicked);

    public void PickFile(string title, string? initialDirectory, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) =>
        Pick(title, LinuxChooser.File, initialDirectory, null, filters, onPicked);

    public void PickSaveFile(
        string title,
        string? initialDirectory,
        string? suggestedFileName,
        IReadOnlyList<FileFilter>? filters,
        Action<string> onPicked) =>
        Pick(title, LinuxChooser.SaveName, initialDirectory, suggestedFileName, filters, onPicked);

    private void Pick(
        string title,
        LinuxChooser chooser,
        string? initialDirectory,
        string? suggestedFileName,
        IReadOnlyList<FileFilter>? filters,
        Action<string> onPicked)
    {
        // The picker stays open until it exits — ignore Browse clicks made while one is up.
        if (Interlocked.CompareExchange(ref _pickerOpen, 1, 0) != 0) return;

        // Either picker can be left open indefinitely. Waiting for it on the UI thread stalls the
        // event loop, so the window manager marks the app unresponsive and offers to kill it.
        // Wait on a worker and post the result back.
        Task.Run(() =>
        {
            try
            {
                var path = PickBlocking(title, chooser, initialDirectory, suggestedFileName, filters);
                if (chooser == LinuxChooser.SaveName && path != null)
                    path = SaveFileNaming.ApplyDefaultExtension(path, suggestedFileName);

                if (!string.IsNullOrEmpty(path))
                    _post(() => onPicked(path));
            }
            finally
            {
                Interlocked.Exchange(ref _pickerOpen, 0);
            }
        });
    }

    /// <summary>The chosen path, or null on cancel or when no picker could be shown.</summary>
    private string? PickBlocking(
        string title,
        LinuxChooser chooser,
        string? initialDirectory,
        string? suggestedFileName,
        IReadOnlyList<FileFilter>? filters)
    {
        var outcome = PortalFilePicker.Pick(chooser, title, initialDirectory, suggestedFileName, filters);
        switch (outcome)
        {
            case PortalOutcome.Picked picked:
                return picked.Path;
            case PortalOutcome.Cancelled:
                return null;
            case PortalOutcome.Unavailable unavailable:
                var command = BuildCommand(title, chooser, initialDirectory, suggestedFileName, filters);
                if (command == null)
                {
                    Console.WriteLine(
                        $"[FilePicker] No file dialog available: {unavailable.Reason}, and none of zenity, " +
                        $"kdialog or yad is installed. Title: {title}");
                    return null;
                }
                Console.WriteLine(
                    $"[FilePicker] Desktop portal unavailable ({unavailable.Reason}); using {Path.GetFileName(command.Tool)}.");
                return RunPicker(command);
            default:
                throw new InvalidOperationException($"Unhandled portal outcome {outcome}");
        }
    }

    /// <summary>The first installed command-line picker's command, or null when none is.</summary>
    private Command? BuildCommand(
        string title,
        LinuxChooser chooser,
        string? initialDirectory,
        string? suggestedFileName,
        IReadOnlyList<FileFilter>? filters)
    {
        // yad takes zenity's file options under --file rather than --file-selection.
        if (_zenity != null)
            return new Command(_zenity, ZenityArgs("--file-selection", title, chooser, initialDirectory, suggestedFileName, filters));
        if (_kdialog != null)
            return new Command(_kdialog, KdialogArgs(title, chooser, initialDirectory, suggestedFileName, filters));
        if (_yad != null)
            return new Command(_yad, ZenityArgs("--file", title, chooser, initialDirectory, suggestedFileName, filters));
        return null;
    }

    private static string[] ZenityArgs(
        string modeFlag,
        string title,
        LinuxChooser chooser,
        string? initialDirectory,
        string? suggestedFileName,
        IReadOnlyList<FileFilter>? filters)
    {
        var folder = chooser == LinuxChooser.Folder;
        var saving = chooser == LinuxChooser.SaveName;
        var hasStart = !string.IsNullOrEmpty(initialDirectory);

        List<string> args = [modeFlag, $"--title={title}"];
        if (folder) args.Add("--directory");
        if (saving)
        {
            // --confirm-overwrite is a no-op on newer zenity, which always confirms; passing
            // it keeps older builds from silently clobbering.
            args.Add("--save");
            args.Add("--confirm-overwrite");
        }

        // A --filename ending in '/' is a starting directory; anything after it pre-fills the
        // name box, which is what a save dialog wants.
        if (hasStart || saving)
        {
            var start = hasStart ? initialDirectory!.TrimEnd('/') + "/" : "";
            args.Add($"--filename={start}{(saving ? suggestedFileName ?? "" : "")}");
        }
        if (!folder && filters != null)
        {
            // One repeatable "--file-filter=Name | *.a *.b" per group; shown as a dropdown.
            foreach (var filter in filters)
                args.Add($"--file-filter={filter.Name} | {string.Join(' ', filter.Patterns)}");
        }
        return [.. args];
    }

    private static string[] KdialogArgs(
        string title,
        LinuxChooser chooser,
        string? initialDirectory,
        string? suggestedFileName,
        IReadOnlyList<FileFilter>? filters)
    {
        // kdialog takes a start path positionally.
        var start = string.IsNullOrEmpty(initialDirectory) ? "." : initialDirectory;
        if (chooser == LinuxChooser.Folder)
            return ["--getexistingdirectory", start, "--title", title];

        var saving = chooser == LinuxChooser.SaveName;
        // kdialog takes the pre-filled name as part of the start path.
        if (saving && !string.IsNullOrEmpty(suggestedFileName))
            start = Path.Combine(start, suggestedFileName);

        List<string> args = [saving ? "--getsavefilename" : "--getopenfilename", start];
        if (filters != null && filters.Count > 0)
        {
            // kdialog (KF5+) takes Qt-style filters positionally after the start path:
            // newline-separated "Name (*.a *.b)" groups.
            var groups = new string[filters.Count];
            for (var i = 0; i < filters.Count; i++)
                groups[i] = $"{filters[i].Name} ({string.Join(' ', filters[i].Patterns)})";
            args.Add(string.Join('\n', groups));
        }
        args.Add("--title");
        args.Add(title);
        return [.. args];
    }

    private static string? RunPicker(Command command)
    {
        var toolName = Path.GetFileName(command.Tool);
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command.Tool,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            foreach (var arg in command.Args) psi.ArgumentList.Add(arg);

            using var process = Process.Start(psi);
            if (process == null) return null;

            // Read stderr concurrently: a tool that fills the stderr pipe while stdout is being
            // drained would otherwise block forever.
            var stderrTask = process.StandardError.ReadToEndAsync();
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = stderrTask.GetAwaiter().GetResult();
            process.WaitForExit();

            // Cancel returns a non-zero exit code with empty output for zenity, kdialog and yad.
            if (process.ExitCode != 0)
            {
                if (!string.IsNullOrWhiteSpace(stderr))
                    Console.WriteLine($"[FilePicker] {toolName} exited {process.ExitCode}: {stderr.Trim()}");
                return null;
            }

            var path = stdout.Trim();
            return string.IsNullOrEmpty(path) ? null : path;
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            Console.WriteLine($"[FilePicker] {toolName} failed: {e.Message}");
            return null;
        }
    }

    private static string? FindOnPath(string exe)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path)) return null;
        foreach (var dir in path.Split(Path.PathSeparator))
        {
            if (dir.Length == 0) continue;
            var full = Path.Combine(dir, exe);
            if (File.Exists(full)) return full;
        }
        return null;
    }
}
