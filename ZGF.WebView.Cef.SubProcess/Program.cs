using System.Runtime.InteropServices;
using Xilium.CefGlue;
using ZGF.WebView.Cef;

// The whole helper program: a subprocess gate. CEF re-launches this exe for its render / GPU / utility
// processes; ExecuteProcess runs Chromium's per-process logic and returns the exit code. It also hosts
// the renderer side of the JS bridge via the shared ZgfCefRenderApp (linked in from ZGF.WebView.Cef).

// The helper lives in <app>/cef-helper/; libcef and the CEF resources sit in <app>/. Point the loader
// at the parent directory so the subprocess finds the native payload the main app shares, rather than
// carrying its own copy of Chromium.
var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
var parent = Directory.GetParent(baseDir)?.FullName ?? AppContext.BaseDirectory;
CefRuntime.Load(parent);

var argv = Environment.GetCommandLineArgs();
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && argv.Length > 0)
    argv = argv[1..];

return CefRuntime.ExecuteProcess(new CefMainArgs(argv), new ZgfCefRenderApp());
