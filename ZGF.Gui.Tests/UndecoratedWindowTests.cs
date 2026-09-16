using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Testing;
using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

public sealed class UndecoratedWindowTests : IDisposable
{
    private readonly FreeTypeFontBackend _fonts = new();

    public void Dispose() => _fonts.Dispose();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BackgroundDragDoesNotStealAnUnconsumedPressFromAChildController(bool wheelOnlyBackground)
    {
        var window = new FakeWindow();
        window.SetPosition(100, 100);
        window.SetCursorPosition(40, 20);
        var childController = new PassiveController();
        using var harness = GuiTestHarness.Create(ctx =>
        {
            var input = ctx.Require<InputSystem>();
            var child = new ContainerView { Width = 100, Height = 100 };
            child.UseController(input, childController);
            var root = new ContainerView { Children = { child } };
            root.UseController(input, () => new WindowDragController(window, input)
            {
                IsBackgroundController = c => wheelOnlyBackground && ReferenceEquals(c, childController),
            });
            return root;
        }, width: 600, height: 600);

        harness.MoveTo(50, 50);
        Assert.Same(childController, harness.Input.HoveredComponent);
        harness.Press();
        window.SetCursorPosition(70, 35);
        harness.MoveTo(80, 65);
        Assert.Equal(wheelOnlyBackground ? 130 : 100, window.PositionX);
        harness.Release();

        // Bare background remains draggable regardless of the child's behavior.
        window.SetCursorPosition(200, 200);
        harness.MoveTo(300, 300);
        harness.Press();
        var before = window.PositionX;
        window.SetCursorPosition(225, 210);
        harness.MoveTo(325, 310);
        Assert.Equal(before + 25, window.PositionX);
        harness.Release();
    }

    private sealed class PassiveController : KeyboardMouseController;

    [Theory]
    [InlineData(false, 1f)]
    [InlineData(true, 0f)]
    public void ClientChromeUsesTransparencyAndTheWindowsOwnContext(bool undecorated, float alpha)
    {
        var app = new FakeWindowedApp([new MonitorWorkArea(0, 0, 1920, 1080)]);
        var font = _fonts.LoadFontFromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf"), 16);
        var backend = new FakeRenderBackend(_fonts, font);
        var arbiter = new PointerOwnershipArbiter();
        using var factory = new SecondaryWindowFactory(app, _fonts, font, backend, new FakePopupDecorator(),
            new Context(), arbiter, new ImeCoordinator(arbiter), new FakeUiScale(1f));
        IWindow? scopedWindow = null;
        var root = new ContainerView();
        var window = factory.Open(new SecondaryWindowRequest
        {
            Title = "Settings", Width = 600, Height = 600, IsUndecorated = undecorated,
            BuildRoot = ctx => { scopedWindow = ctx.Require<IWindow>(); return root; },
        });

        Assert.Equal(undecorated, app.LastWindowOptions!.Value.IsUndecorated);
        Assert.Equal(alpha, backend.LastClearAlpha);
        Assert.Same(window.Window, scopedWindow);
        Assert.True(root.IsMounted);
        var closed = 0;
        window.Closed += () => closed++;
        window.Close();
        factory.Update();
        factory.Update();
        Assert.False(root.IsMounted);
        Assert.Equal(1, closed);
    }

    [Theory]
    [InlineData(1920, 1080, 600, 600)]
    [InlineData(500, 400, 500, 400)]
    public void CenteredWindowFitsItsMonitorAndCanExceedTheMainWindow(int monitorWidth, int monitorHeight,
        int expectedWidth, int expectedHeight)
    {
        var app = new FakeWindowedApp([new MonitorWorkArea(0, 0, 1920, 1080),
            new MonitorWorkArea(1920, 0, monitorWidth, monitorHeight)]);
        app.MainWindow.SetPosition(1950, 20);
        app.MainWindow.SetSize(300, 200);
        var font = _fonts.LoadFontFromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf"), 16);
        var arbiter = new PointerOwnershipArbiter();
        using var factory = new SecondaryWindowFactory(app, _fonts, font, new FakeRenderBackend(_fonts, font),
            new FakePopupDecorator(), new Context(), arbiter, new ImeCoordinator(arbiter), new FakeUiScale(1f));
        var window = factory.Open(new SecondaryWindowRequest
        {
            Title = "Settings", Width = 600, Height = 600, IsUndecorated = true, CenterOnMainWindow = true,
            BuildRoot = _ => new ContainerView(),
        });
        Assert.Equal(expectedWidth, window.Window.Width);
        Assert.Equal(expectedHeight, window.Window.Height);
        window.Window.GetPosition(out var x, out var y);
        Assert.InRange(x, 1920, 1920 + monitorWidth - expectedWidth);
        Assert.InRange(y, 0, monitorHeight - expectedHeight);
        app.MainWindow.SetSize(200, 100);
        Assert.Equal(expectedWidth, window.Window.Width);
        Assert.Equal(expectedHeight, window.Window.Height);
    }

    [Fact]
    public void TitleDragKeepsTheGrabPointStableAndStopsOnRelease()
    {
        var window = new FakeWindow(contentScale: 1.5f);
        window.SetPosition(100, 100);
        window.SetCursorPosition(40, 20);
        var input = new InputSystem();
        var drag = new WindowDragController(window, input);
        var mouse = new Mouse();
        mouse.Press(MouseButton.Left);
        var press = new MouseButtonEvent
        {
            Mouse = mouse, Button = MouseButton.Left, State = InputState.Pressed, Phase = EventPhase.Bubbling,
        };
        drag.OnMouseButtonStateChanged(ref press);
        var move = new MouseMoveEvent { Mouse = mouse, Phase = EventPhase.Bubbling };
        window.SetCursorPosition(70, 35);
        drag.OnMouseMoved(ref move);
        Assert.Equal(130, window.PositionX);
        Assert.Equal(115, window.PositionY);
        // Moving the window changes local cursor coordinates even with a stationary OS pointer.
        window.SetCursorPosition(40, 20);
        drag.OnMouseMoved(ref move);
        Assert.Equal(130, window.PositionX);
        Assert.Equal(115, window.PositionY);
        var release = new MouseButtonEvent
        {
            Mouse = mouse, Button = MouseButton.Left, State = InputState.Released, Phase = EventPhase.Bubbling,
        };
        drag.OnMouseButtonStateChanged(ref release);
        window.SetCursorPosition(90, 60);
        drag.OnMouseMoved(ref move);
        Assert.Equal(130, window.PositionX);
        Assert.Equal(115, window.PositionY);
        Assert.False(input.HasFocus);
    }
}
