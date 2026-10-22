using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Automation;
using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.Widgets;
using ZGF.Observable;

namespace ZGF.Gui.Tests;

public sealed class GuiDriverTests : IDisposable
{
    private const int WindowWidth = 400;
    private const int WindowHeight = 300;

    private readonly FreeTypeFontBackend _fonts = new();
    private readonly FontHandle _font;

    public GuiDriverTests()
    {
        _font = _fonts.LoadFontFromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf"), 16);
    }

    public void Dispose() => _fonts.Dispose();

    [Theory]
    [InlineData("push")]
    [InlineData("Push")]
    public void Click_finds_its_target_by_id_or_by_label(string selector)
    {
        var clicks = 0;
        var driver = DriverFor(context => BuildButton(context, "push", "Push", () => clicks++));

        driver.Click(selector);

        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Click_on_a_missing_selector_throws()
    {
        var driver = DriverFor(context => BuildButton(context, "push", "Push", () => { }));

        Assert.Throws<InvalidOperationException>(() => driver.Click("Pull"));
    }

    private static View BuildButton(Context context, string id, string label, Action onClick)
    {
        var button = new KbmInput
        {
            OnClick = onClick,
            Child = new Box { Children = [new Text { Value = label }] },
        };
        var view = button.BuildView(context);
        view.Id = id;
        return view;
    }

    private GuiDriver DriverFor(Func<Context, View> buildRoot)
    {
        var window = new FakeWindow(WindowWidth, WindowHeight);
        var scale = new FakeUiScale(1f);
        var input = new DesktopInputSystem(window, scale);
        var context = new Context();
        context.AddService(input.InputSystem);
        var host = new GuiWindowHost(window, new CaptureCanvas(_fonts, _font), input, context, scale, sizeRootToWindow: true);
        host.SetRoot(buildRoot(context));
        var surface = new GuiSurface("main", window, host.Root, input, 1f);
        return new GuiDriver(() => [surface], new InlineDispatcher(), IgnoreScreenshot);
    }

    private static void IgnoreScreenshot(IWindow window, string path, Action? done)
    {
    }

    private sealed class InlineDispatcher : IUiDispatcher
    {
        public void Post(Action action) => action();
    }
}
