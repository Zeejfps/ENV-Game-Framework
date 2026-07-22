using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using ZGF.Observable;

namespace ZGF.Gui.Desktop.Platforms.Osx;

[SupportedOSPlatform("macos")]
public sealed class MacOsFilePicker : IFilePicker
{
    private readonly Context _context;
    private int _pickerOpen;

    public MacOsFilePicker(Context context)
    {
        _context = context;
    }

    public void PickFolder(string title, Action<string> onPicked) => Pick(title, folder: true, null, null, onPicked);

    public void PickFile(string title, string? initialDirectory, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) =>
        Pick(title, folder: false, initialDirectory, filters, onPicked);

    private void Pick(string title, bool folder, string? initialDirectory, IReadOnlyList<FileFilter>? filters, Action<string> onPicked)
    {
        // The picker stays open until it exits — ignore Browse clicks made while one is up.
        if (Interlocked.CompareExchange(ref _pickerOpen, 1, 0) != 0) return;

        // osascript blocks until the user closes the dialog; waiting for it on the UI thread
        // stalls the event loop (beachball). Wait on a worker and post the result back.
        var dispatcher = _context.Require<IUiDispatcher>();
        Task.Run(() =>
        {
            try
            {
                var path = RunPicker(title, folder, initialDirectory, filters);
                if (!string.IsNullOrEmpty(path))
                    dispatcher.Post(() => onPicked(path));
            }
            finally
            {
                Interlocked.Exchange(ref _pickerOpen, 0);
            }
        });
    }

    private static string? RunPicker(string title, bool folder, string? initialDirectory, IReadOnlyList<FileFilter>? filters)
    {
        var chooser = folder ? "choose folder" : "choose file";
        var location = string.IsNullOrEmpty(initialDirectory)
            ? ""
            : $" default location (POSIX file \"{EscapeForAppleScript(initialDirectory)}\")";
        var ofType = folder ? "" : BuildOfTypeClause(filters);
        var script =
            $"set chosen to {chooser} with prompt \"{EscapeForAppleScript(title)}\"{location}{ofType}\n" +
            "return POSIX path of chosen";

        var psi = new ProcessStartInfo
        {
            FileName = "/usr/bin/osascript",
            ArgumentList = { "-e", script },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        using var process = Process.Start(psi);
        if (process == null) return null;

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            // User cancel returns "User canceled. (-128)" on stderr; treat as null.
            if (stderr.Contains("-128")) return null;
            Console.WriteLine($"[FilePicker] osascript failed ({process.ExitCode}): {stderr.Trim()}");
            return null;
        }

        var path = stdout.Trim();
        if (string.IsNullOrEmpty(path)) return null;

        // POSIX path of a folder ends with a trailing slash; trim it (but keep "/" itself).
        if (path.Length > 1 && path.EndsWith('/'))
            path = path.TrimEnd('/');

        return path;
    }

    // "choose file of type" takes bare extensions (or UTIs) and greys out everything else —
    // macOS has no filter dropdown. Only plain "*.ext" patterns are expressible, so others are
    // skipped; a "*" / "*.*" group means anything may be picked, so the clause is omitted.
    private static string BuildOfTypeClause(IReadOnlyList<FileFilter>? filters)
    {
        if (filters == null || filters.Count == 0) return "";

        var extensions = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var filter in filters)
        {
            foreach (var pattern in filter.Patterns)
            {
                if (pattern is "*" or "*.*") return "";
                if (!pattern.StartsWith("*.", StringComparison.Ordinal)) continue;
                var extension = pattern[2..];
                if (seen.Add(extension)) extensions.Add(extension);
            }
        }
        if (extensions.Count == 0) return "";

        var sb = new StringBuilder(" of type {");
        for (var i = 0; i < extensions.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append('"').Append(EscapeForAppleScript(extensions[i])).Append('"');
        }
        return sb.Append('}').ToString();
    }

    private static string EscapeForAppleScript(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
