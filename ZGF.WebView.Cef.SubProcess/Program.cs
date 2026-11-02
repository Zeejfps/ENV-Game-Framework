using System.Runtime.InteropServices;
using Xilium.CefGlue;
using ZGF.WebView.Cef;

// The whole helper program: a subprocess gate. CEF re-launches this exe for its render / GPU / utility
// processes; ExecuteProcess runs Chromium's per-process logic and returns the exit code. It also hosts
// the renderer side of the JS bridge via the shared ZgfCefRenderApp (linked in from ZGF.WebView.Cef).

var argv = Environment.GetCommandLineArgs();

// The helper loads the native payload the main app shares rather than carrying its own copy of
// Chromium. On macOS CEF passes the framework bundle's path; elsewhere the helper lives in
// <app>/cef-helper/ and libcef sits in <app>/.
CefRuntime.Load(LibCefDir(argv));

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && argv.Length > 0)
    argv = argv[1..];

return CefRuntime.ExecuteProcess(new CefMainArgs(argv), new ZgfCefRenderApp());

static string LibCefDir(string[] argv)
{
    const string frameworkSwitch = "--framework-dir-path=";
    foreach (var arg in argv)
        if (arg.StartsWith(frameworkSwitch, StringComparison.Ordinal))
        {
            var frameworkDir = Path.GetDirectoryName(arg[frameworkSwitch.Length..].TrimEnd('/'));
            if (frameworkDir != null)
                return frameworkDir;
        }
    var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    return Directory.GetParent(baseDir)?.FullName ?? AppContext.BaseDirectory;
}
