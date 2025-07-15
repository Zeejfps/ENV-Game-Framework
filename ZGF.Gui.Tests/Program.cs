// See https://aka.ms/new-console-template for more information

using ZGF.Gui;

IGuiApp app = new App();
var columnLayout = new ColumnLayout();
columnLayout.Add(new Button());
columnLayout.Add(new Button());
columnLayout.Add(new Button());

app.GuiContent.Layout = columnLayout;
app.GuiContent.ApplyStyle(new StyleSheet());

app.Run();