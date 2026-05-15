using System.Runtime.InteropServices;
using GitGui;
using LLMit;
using ZGF.Core;
using ZGF.Gui;

var context = new Context();
context.AddService<IMessageBus>(new MessageBus());
context.AddService<IFolderPicker>(
    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? new WindowsFolderPicker()
        : new NoopFolderPicker());

var appView = new AppView();
var appHost = new GuiApp(new StartupConfig
{
    WindowTitle = "GitGui",
    WindowWidth = 1280,
    WindowHeight = 720,   
    IsUndecorated = false
}, context, appView);

appHost.Run();