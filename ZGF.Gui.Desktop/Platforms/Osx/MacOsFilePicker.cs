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

    /// <summary>Which of AppleScript's three choosers to run.</summary>
    private enum Chooser
    {
        File,
        Folder,
        SaveName,
    }

    public void PickFolder(string title, Action<string> onPicked) =>
        Pick(title, Chooser.Folder, null, null, null, onPicked);

    public void PickFile(string title, string? initialDirectory, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) =>
        Pick(title, Chooser.File, initialDirectory, null, filters, onPicked);

    public void PickSaveFile(
        string title,
        string? initialDirectory,
        string? suggestedFileName,
        IReadOnlyList<FileFilter>? filters,
        Action<string> onPicked) =>
        Pick(title, Chooser.SaveName, initialDirectory, suggestedFileName, filters, onPicked);

    private void Pick(
        string title,
        Chooser chooser,
        string? initialDirectory,
        string? suggestedFileName,
        IReadOnlyList<FileFilter>? filters,
        Action<string> onPicked)
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
                var path = RunPicker(title, chooser, initialDirectory, suggestedFileName, filters);
                if (chooser == Chooser.SaveName)
                    path = SaveFileNaming.ApplyDefaultExtension(path ?? "", suggestedFileName);

                if (!string.IsNullOrEmpty(path))
                    dispatcher.Post(() => onPicked(path));
            }
            finally
            {
                Interlocked.Exchange(ref _pickerOpen, 0);
            }
        });
    }

    private static string? RunPicker(
        string title,
        Chooser chooser,
        string? initialDirectory,
        string? suggestedFileName,
        IReadOnlyList<FileFilter>? filters)
    {
        var verb = chooser switch
        {
            Chooser.Folder => "choose folder",
            Chooser.SaveName => "choose file name",
            _ => "choose file",
        };

        // AppleScript raises rather than falling back when the default location is not a real
        // folder, which would turn a stale directory into a failed dialog.
        var location = !string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory)
            ? $" default location (POSIX file \"{EscapeForAppleScript(initialDirectory)}\")"
            : "";

        // Only "choose file" takes a type list; "choose file name" has no filter clause at all,
        // so a save dialog on macOS simply does not restrict what can be typed.
        var ofType = chooser == Chooser.File ? BuildOfTypeClause(filters) : "";

        var defaultName = chooser == Chooser.SaveName && !string.IsNullOrEmpty(suggestedFileName)
            ? $" default name \"{EscapeForAppleScript(suggestedFileName)}\""
            : "";

        var script =
            $"set chosen to {verb} with prompt \"{EscapeForAppleScript(title)}\"{defaultName}{location}{ofType}\n" +
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

    // Uniform Type Identifiers for the extensions we can express. "choose file of type" silently
    // ignores bare extensions on current macOS — the dialog then filters nothing at all — so every
    // pattern has to be translated to a UTI before it reaches AppleScript. One UTI often covers
    // several extensions (public.jpeg matches .jpg and .jpeg), hence the dedupe below.
    private static readonly Dictionary<string, string> UtiByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["png"] = "public.png",
        ["jpg"] = "public.jpeg",
        ["jpeg"] = "public.jpeg",
        ["gif"] = "com.compuserve.gif",
        ["bmp"] = "com.microsoft.bmp",
        ["tif"] = "public.tiff",
        ["tiff"] = "public.tiff",
        ["heic"] = "public.heic",
        ["webp"] = "org.webmproject.webp",
        ["pdf"] = "com.adobe.pdf",
        ["txt"] = "public.plain-text",
        ["json"] = "public.json",
        ["csv"] = "public.comma-separated-values-text",
        ["zip"] = "public.zip-archive",
    };

    // "choose file of type" greys out everything that doesn't match — macOS has no filter dropdown.
    // Only plain "*.ext" patterns with a known UTI are expressible; anything else is dropped from
    // the clause rather than dropping the whole clause, so one unmappable pattern can't quietly
    // turn the filter off. A "*" / "*.*" group does mean anything may be picked, so that omits it.
    private static string BuildOfTypeClause(IReadOnlyList<FileFilter>? filters)
    {
        if (filters == null || filters.Count == 0) return "";

        var utis = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var filter in filters)
        {
            foreach (var pattern in filter.Patterns)
            {
                if (pattern is "*" or "*.*") return "";
                if (!pattern.StartsWith("*.", StringComparison.Ordinal))
                {
                    Console.WriteLine($"[FilePicker] Pattern '{pattern}' is not a plain '*.ext' glob; ignored on macOS.");
                    continue;
                }
                var extension = pattern[2..];
                if (!UtiByExtension.TryGetValue(extension, out var uti))
                {
                    Console.WriteLine($"[FilePicker] No UTI known for '.{extension}'; ignored on macOS.");
                    continue;
                }
                if (seen.Add(uti)) utis.Add(uti);
            }
        }
        // Nothing survived the translation. AppleScript can't express "allow nothing", so the
        // dialog ends up unfiltered — say so, since that looks identical to a filter that failed.
        if (utis.Count == 0)
        {
            Console.WriteLine("[FilePicker] No filter pattern mapped to a UTI; showing all files.");
            return "";
        }

        // The UTIs come from the table above, so they need no AppleScript escaping.
        var sb = new StringBuilder(" of type {");
        for (var i = 0; i < utis.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append('"').Append(utis[i]).Append('"');
        }
        return sb.Append('}').ToString();
    }

    private static string EscapeForAppleScript(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
