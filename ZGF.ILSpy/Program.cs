using ZGF.ILSpy;

var commandLine = CommandLine.Parse(args);
var settings = AppSettings.Load(commandLine.SettingsPath);
var workspace = new Workspace(settings);
var app = new IlSpyApp(workspace);

var assembliesToOpen = settings.ListExistingAssemblies();
assembliesToOpen.AddRange(commandLine.AssemblyPaths);
app.OpenOnStartup(assembliesToOpen);

var windowConfig = settings.CreateWindowConfig();
app.Run(windowConfig, commandLine.RenderBackend);

settings.RememberWindow(app.WindowBounds);
workspace.SaveSettings();
