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
context.AddService(new State<ThemeMode>(ThemeMode.Dark));
context.AddService<IPlatformShell>(
    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new WindowsPlatformShell()
    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? new MacOSPlatformShell()
    : new NoopPlatformShell());
context.AddService<IClipboard>(
    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new Win32Clipboard()
    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? new OsxClipboard()
    : new AppClipboard());

context.AddService<IPopupNativeDecorator>(
    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new WindowsPopupDecorator()
    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? new MacOsPopupDecorator()
    : new NoopPopupDecorator());

var statePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "GitGui",
    "state.json");
var initialState = RepoStateStore.Load(statePath);
var registry = new RepoRegistry(initialState, statePath);
context.AddService<IRepoRegistry>(registry);
var repoActivity = new RepoActivityTracker();
context.AddService<IRepoActivityTracker>(repoActivity);
context.AddService<IGitService>(new GitService(repoActivity));
context.AddService<IDragController>(new DragController(registry));

var appView = new AppView();
var appHost = GuiApp.CreateDefault(new StartupConfig
{
    WindowTitle = "GitGui",
    WindowWidth = 1400,
    WindowHeight = 900,
    IsUndecorated = false
}, context, appView);

appHost.RegisterFont(LucideIcons.FontFamily, "Assets/Fonts/Lucide/Lucide.ttf", 16);
appHost.RegisterFont(DiffOptions.MonoFontFamily, "Assets/Fonts/JetBrainsMono/JetBrainsMono-Regular.ttf", 13);

context.AddService<ITooltipService>(new PopupTooltipService(
    context.Require<IPopupWindowFactory>(),
    context.Require<IWindowCoordinates>(),
    measureContext: context));

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    appHost.SetIcon("Assets/commit_bench_icon.rgba");

using var repoWatchers = new RepoWatcherService(
    registry,
    context.Require<IUiDispatcher>(),
    messageBus,
    repoActivity);

using var worktreeSync = new WorktreeSyncService(
    registry,
    context.Require<IGitService>(),
    context.Require<IUiDispatcher>(),
    messageBus);

using var submoduleSync = new SubmoduleSyncService(
    registry,
    context.Require<IGitService>(),
    context.Require<IUiDispatcher>(),
    messageBus);

appHost.Run();
