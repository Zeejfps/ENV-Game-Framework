namespace ZGF.Gui.Tests;

public sealed class App : IGuiApp
{
    public Container GuiContent { get; }

    public App()
    {
        GuiContent = new Container();
    }

    public void Run()
    {
        var canvas = new FakeCanvas();
        while (true)
        {
            canvas.BeginFrame();
            GuiContent.LayoutSelf();
            GuiContent.DrawSelf(canvas);
            canvas.EndFrame();
            EventSystem.Instance.Update();
        }
    }
}