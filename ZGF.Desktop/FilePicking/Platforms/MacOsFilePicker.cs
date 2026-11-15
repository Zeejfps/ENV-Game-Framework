using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

namespace ZGF.Desktop.FilePicking;

[SupportedOSPlatform("macos")]
internal sealed class MacOsFilePicker : IFilePicker
{
    private readonly Action<Action> _post;
    private int _pickerOpen;

    public MacOsFilePicker(Action<Action> post)
    {
        _post = post;
    }

    /// <summary>Which of AppleScript's three choosers to run.</summary>
    private enum Chooser
    {
        File,
        Folder,
        SaveName,
    }

    public void PickFolder(string title, string? initialDirectory, Action<string> onPicked) =>
        Pick(title, Chooser.Folder, initialDirectory, null, null, onPicked, null);

    public void PickFile(string title, string? initialDirectory, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) =>
        Pick(title, Chooser.File, initialDirectory, null, filters, onPicked, null);

    public void PickSaveFile(
        string title,
        string? initialDirectory,
        string? suggestedFileName,
        IReadOnlyList<FileFilter>? filters,
        Action<string> onPicked,
        Action onCancelled) =>
        Pick(title, Chooser.SaveName, initialDirectory, suggestedFileName, filters, onPicked, onCancelled);

    private void Pick(
        string title,
        Chooser chooser,
        string? initialDirectory,
        string? suggestedFileName,
        IReadOnlyList<FileFilter>? filters,
        Action<string> onPicked,
        Action? onCancelled)
    {
        // The picker stays open until it exits — ignore Browse clicks made while one is up, but
        // tell a caller waiting to hear (onCancelled) that this one was not shown.
        if (Interlocked.CompareExchange(ref _pickerOpen, 1, 0) != 0)
        {
            if (onCancelled != null)
                _post(onCancelled);
            return;
        }

        // osascript blocks until the user closes the dialog; waiting for it on the UI thread
        // stalls the event loop (beachball). Wait on a worker and post the result back.
        Task.Run(() =>
        {
            try
            {
                var path = RunPicker(title, chooser, initialDirectory, suggestedFileName, filters);
                if (chooser == Chooser.SaveName)
                    path = SaveFileNaming.ApplyDefaultExtension(path ?? "", suggestedFileName);

                if (!string.IsNullOrEmpty(path))
                    _post(() => onPicked(path));
                else if (onCancelled != null)
                    _post(onCancelled);
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

        // A type looked up by its extension (one the table below doesn't know, such as an app's own
        // document type) is worked out by UTType, from AppleScriptObjC; once a script uses a
        // framework, it must ask for the scripting additions that hold "choose file" too.
        var header = ofType.Contains(TypeLookup, StringComparison.Ordinal)
            ? "use framework \"Foundation\"\nuse framework \"UniformTypeIdentifiers\"\nuse scripting additions\n"
            : "";

        var defaultName = chooser == Chooser.SaveName && !string.IsNullOrEmpty(suggestedFileName)
            ? $" default name \"{EscapeForAppleScript(suggestedFileName)}\""
            : "";

        var script =
            header +
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
    // several extensions (public.jpeg matches .jpg and .jpeg), hence the dedupe below. An extension
    // missing here is looked up in the script itself (TypeLookup).
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

    // Marks a type the script looks up by extension; the script then needs AppleScriptObjC.
    private const string TypeLookup = "typeWithFilenameExtension:";

    // "choose file of type" greys out everything that doesn't match — macOS has no filter dropdown.
    // Only plain "*.ext" patterns are expressible; anything else is dropped from the clause rather
    // than dropping the whole clause, so one unmappable pattern can't quietly turn the filter off.
    // An extension the table doesn't know becomes a lookup of the type macOS gives files of that
    // extension (a dynamic "dyn." type for one no app declares), which matches them as the table's
    // UTIs match theirs. A "*" / "*.*" group does mean anything may be picked, so that omits it.
    private static string BuildOfTypeClause(IReadOnlyList<FileFilter>? filters)
    {
        if (filters == null || filters.Count == 0) return "";

        // Each an AppleScript expression for one type: a quoted UTI, or a lookup by extension.
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
                var uti = UtiByExtension.TryGetValue(extension, out var known)
                    ? $"\"{known}\""
                    : $"((current application's UTType's {TypeLookup}\"{EscapeForAppleScript(extension)}\")'s identifier() as text)";
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

        // Each is quoted or escaped already as it was built.
        var sb = new StringBuilder(" of type {");
        for (var i = 0; i < utis.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(utis[i]);
        }
        return sb.Append('}').ToString();
    }

    private static string EscapeForAppleScript(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
