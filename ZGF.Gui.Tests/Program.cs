// See https://aka.ms/new-console-template for more information

using ZGF.Core;
using ZGF.Geometry;
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

app.GuiContent.Position = new RectF(0, 0, 640, 480);
app.GuiContent.Layout = columnLayout;
app.GuiContent.ApplyStyle(new StyleSheet());

app.Run();