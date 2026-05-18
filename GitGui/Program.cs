using System.Runtime.InteropServices;
using GitGui;
using LLMit;
using ZGF.Core;
using ZGF.Gui;
using ZGF.Observable;

var context = new Context();
var messageBus = new MessageBus();
context.AddService<IMessageBus>(messageBus);
context.AddService(new State<MainViewMode>(MainViewMode.LocalChanges));
context.AddService<IFolderPicker>(
    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new WindowsFolderPicker()
    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? new MacOSFolderPicker()
    : new NoopFolderPicker());

var statePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "GitGui",
    "state.json");
var initialState = RepoStateStore.Load(statePath);
var registry = new RepoRegistry(initialState, statePath);
context.AddService<IRepoRegistry>(registry);
context.AddService<IGitService>(new GitService());
context.AddService<IDragController>(new DragController(registry));

var appView = new AppView(registry);
var appHost = GuiApp.CreateDefault(new StartupConfig
{
    WindowTitle = "GitGui",
    WindowWidth = 1400,
    WindowHeight = 1280,
    IsUndecorated = false
}, context, appView);

appHost.RegisterFont(LucideIcons.FontFamily, "Assets/Fonts/Lucide/Lucide.ttf", 16);
appHost.RegisterFont(DiffOptions.MonoFontFamily, "Assets/Fonts/JetBrainsMono/JetBrainsMono-Regular.ttf", 13);

appHost.Run();