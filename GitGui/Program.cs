using System.Runtime.InteropServices;
using GitGui;
using LLMit;
using ZGF.Core;
using ZGF.Gui;
using ZGF.Gui.Tests;
using ZGF.Observable;

var context = new Context();
var messageBus = new MessageBus();
context.AddService<IMessageBus>(messageBus);
context.AddService(new State<MainViewMode>(MainViewMode.LocalChanges));
context.AddService<IPlatformShell>(
    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new WindowsPlatformShell()
    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? new MacOSPlatformShell()
    : new NoopPlatformShell());
// AppClipboard is a process-local fallback — fine for Linux dev, not what we want on
// Win/macOS where IClipboard needs to reach the OS pasteboard.
context.AddService<IClipboard>(
    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new Win32Clipboard()
    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? new OsxClipboard()
    : new AppClipboard());

var statePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "GitGui",
    "state.json");
var initialState = RepoStateStore.Load(statePath);
var registry = new RepoRegistry(initialState, statePath);
context.AddService<IRepoRegistry>(registry);
context.AddService<IGitService>(new GitService());
context.AddService<IDragController>(new DragController(registry));

var tooltipSurfaceView = new TooltipSurfaceView();
context.AddService<ITooltipService>(tooltipSurfaceView);

var appView = new AppView(tooltipSurfaceView);
var appHost = GuiApp.CreateDefault(new StartupConfig
{
    WindowTitle = "GitGui",
    WindowWidth = 1400,
    WindowHeight = 900,
    IsUndecorated = false
}, context, appView);

appHost.RegisterFont(LucideIcons.FontFamily, "Assets/Fonts/Lucide/Lucide.ttf", 16);
appHost.RegisterFont(DiffOptions.MonoFontFamily, "Assets/Fonts/JetBrainsMono/JetBrainsMono-Regular.ttf", 13);

// IUiDispatcher is registered by GuiApp.CreateDefault, so the watcher service can
// only be constructed after appHost is built.
using var repoWatchers = new RepoWatcherService(
    registry,
    context.Require<IUiDispatcher>(),
    messageBus);

using var worktreeSync = new WorktreeSyncService(
    registry,
    context.Require<IGitService>(),
    context.Require<IUiDispatcher>(),
    messageBus);

appHost.Run();