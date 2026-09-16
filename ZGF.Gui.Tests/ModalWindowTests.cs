using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

public sealed class ModalWindowTests : IDisposable
{
    private readonly FreeTypeFontBackend _fonts = new();
    private readonly FakeWindowedApp _app = new([new MonitorWorkArea(0, 0, 1920, 1080)]);
    private readonly PointerOwnershipArbiter _arbiter = new();
    private readonly SecondaryWindowFactory _factory;
    private readonly DesktopInputSystem _mainInput;

    public ModalWindowTests()
    {
        var font = _fonts.LoadFontFromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf"), 16);
        var scale = new FakeUiScale(1f);
        var ime = new ImeCoordinator(_arbiter);
        _mainInput = new DesktopInputSystem(_app.MainWindow, scale, _arbiter, _app, ime);
        _arbiter.Register(_mainInput, false);
        var context = new Context();
        context.AddService<IWindowModality>(new FakeModality());
        _factory = new SecondaryWindowFactory(_app, _fonts, font, new FakeRenderBackend(_fonts, font),
            new FakePopupDecorator(), context, _arbiter, ime, scale);
    }

    private ISecondaryWindow Open(bool modal = false) => _factory.Open(new SecondaryWindowRequest
    {
        Title = "Test", Width = 600, Height = 600, IsModal = modal, BuildRoot = _ => new ContainerView(),
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClosingDialogRestoresBackgroundWindowsAndAllowsReopening(bool nativeClose)
    {
        var other = Open();
        var dialog = Open(true);
        var main = (FakeWindow)_app.MainWindow;
        Assert.False(main.InputEnabled);
        Assert.False(((FakeWindow)other.Window).InputEnabled);
        Assert.True(((FakeWindow)dialog.Window).InputEnabled);
        Assert.Same(main, ((FakeWindow)dialog.Window).Owner);
        Assert.True(_arbiter.IsBlockedByDialog(_mainInput));
        ((FakeWindow)other.Window).RaiseClose();
        Assert.True(((FakeWindow)other.Window).CloseCancelled);

        if (nativeClose) ((FakeWindow)dialog.Window).RaiseClose();
        else dialog.Close();
        _factory.Update();
        Assert.True(main.InputEnabled);
        Assert.True(((FakeWindow)other.Window).InputEnabled);
        Assert.False(_arbiter.IsBlockedByDialog(_mainInput));
        Assert.True(((FakeWindow)dialog.Window).IsDisposed);
        Open(true);
        Assert.False(main.InputEnabled);
    }

    [Fact]
    public void NestedDialogsAndNewBackgroundWindowsRemainBlockedUntilLastDialogCloses()
    {
        var first = Open(true);
        var second = Open(true);
        var other = Open();
        Assert.Same(first.Window, ((FakeWindow)second.Window).Owner);
        Assert.False(((FakeWindow)first.Window).InputEnabled);
        Assert.False(((FakeWindow)other.Window).InputEnabled);
        second.Close();
        _factory.Update();
        Assert.True(((FakeWindow)first.Window).InputEnabled);
        Assert.False(((FakeWindow)_app.MainWindow).InputEnabled);
        _factory.Dispose();
        Assert.True(((FakeWindow)_app.MainWindow).InputEnabled);
        Assert.False(_arbiter.IsBlockedByDialog(_mainInput));
    }

    [Fact]
    public void DialogBlocksBackgroundInputButItsMenusStillWork()
    {
        var main = (FakeWindow)_app.MainWindow;
        var backgroundEvents = new Events();
        _mainInput.InputSystem.StealFocus(backgroundEvents);
        SendInput(main);
        Assert.Equal(6, backgroundEvents.Count);
        backgroundEvents.Count = 0;

        var dialog = (SecondaryWindowImpl)Open(true);
        var dialogEvents = new Events();
        dialog.Input.InputSystem.StealFocus(dialogEvents);
        _mainInput.Update();
        SendInput(main);
        Assert.Equal(0, backgroundEvents.Count);
        SendInput((FakeWindow)dialog.Window);
        Assert.Equal(6, dialogEvents.Count);

        var menu = new DesktopInputSystem(new FakeWindow(), new FakeUiScale(1f), _arbiter);
        _arbiter.Register(menu, true);
        Assert.True(_arbiter.IsBlockedByModal(dialog.Input));
        Assert.False(_arbiter.IsBlockedByModal(menu));
        Assert.True(_arbiter.OwnsPointer(menu));
        var dismissals = 0;
        _arbiter.OutsidePressDismiss += () => dismissals++;
        _arbiter.NotifyPress(dialog.Input);
        Assert.Equal(1, dismissals);
        _arbiter.Unregister(menu);
        Assert.False(_arbiter.IsBlockedByModal(dialog.Input));
        Assert.True(_arbiter.IsBlockedByModal(_mainInput));
        dialog.Close();
        _factory.Update();
        SendInput(main);
        Assert.Equal(6, backgroundEvents.Count);
    }

    [Fact]
    public void FailedDialogBuildDoesNotLeaveBackgroundBlocked()
    {
        Assert.Throws<InvalidOperationException>(() => _factory.Open(new SecondaryWindowRequest
        {
            Title = "Broken", Width = 600, Height = 600, IsModal = true,
            BuildRoot = _ => throw new InvalidOperationException(),
        }));
        Assert.True(((FakeWindow)_app.MainWindow).InputEnabled);
        Assert.False(_arbiter.IsBlockedByDialog(_mainInput));
        Assert.True(((FakeWindow)_app.Windows.Last()).IsDisposed);
    }

    private static void SendInput(FakeWindow window)
    {
        window.RaiseKey();
        window.RaiseText();
        window.RaisePreedit();
        window.RaiseScroll();
        window.RaiseMouseButton(0, InputAction.Press);
        window.RaiseMouseButton(0, InputAction.Release);
    }

    private sealed class Events : KeyboardMouseController
    {
        public int Count;
        public override void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e) => Count++;
        public override void OnTextInput(ref TextInputEvent e) => Count++;
        public override void OnComposition(ref CompositionEvent e) => Count++;
        public override void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e) => Count++;
        public override void OnMouseButtonStateChanged(ref MouseButtonEvent e) => Count++;
    }

    private sealed class FakeModality : IWindowModality
    {
        public void SetInputEnabled(IWindow window, bool enabled) => ((FakeWindow)window).SetInputEnabled(enabled);
        public void SetOwner(IWindow window, IWindow owner) => ((FakeWindow)window).SetOwner(owner);
    }

    public void Dispose()
    {
        _factory.Dispose();
        _fonts.Dispose();
    }
}
