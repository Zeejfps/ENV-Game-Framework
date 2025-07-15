// See https://aka.ms/new-console-template for more information

using ZGF.Core;
using ZGF.Gui;
using ZGF.Gui.Tests;

IGuiApp app = new App(new StartupConfig
{
    WindowWidth = 640,
    WindowHeight = 480,
    WindowTitle = "Test"
});
var columnLayout = new ColumnLayout();
columnLayout.Add(new Button());
columnLayout.Add(new Button());
columnLayout.Add(new Button());

app.GuiContent.Layout = columnLayout;
app.GuiContent.ApplyStyle(new StyleSheet());

app.Run();