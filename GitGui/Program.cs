using System.Runtime.InteropServices;
using GitGui;
using LLMit;
using ZGF.Core;
using ZGF.Gui;

var context = new Context();
var messageBus = new MessageBus();
context.AddService<IMessageBus>(messageBus);
context.AddService<IFolderPicker>(
    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? new WindowsFolderPicker()
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
var appHost = new GuiApp(new StartupConfig
{
    WindowTitle = "GitGui",
    WindowWidth = 1280,
    WindowHeight = 720,   
    IsUndecorated = false
}, context, appView);

appHost.Run();