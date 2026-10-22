using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

/// <summary>Popup windows are pooled: a window that served one popup serves the next, which must
/// not inherit anything from it.</summary>
public sealed class PopupReuseTests : IDisposable
{
    private readonly FreeTypeFontBackend _fonts = new();
    private readonly PopupWindowFactory _factory;

    public PopupReuseTests()
    {
        var font = _fonts.LoadFontFromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf"), 16);
        var app = new FakeWindowedApp([new MonitorWorkArea(0, 0, 1920, 1080)]);
        var arbiter = new PointerOwnershipArbiter();
        _factory = new PopupWindowFactory(new FakeRenderBackend(app, _fonts, font), new Context(), arbiter,
            new ImeCoordinator(arbiter), new FakeUiScale(1f));
    }

    private IPopupWindow Open(bool passThrough) => _factory.Acquire(new PopupRequest
    {
        BuildRoot = _ => new ContainerView { Width = 100, Height = 100 },
        Place = size => (new ScreenRect(10, 10, size.Width, size.Height), null),
        MousePassThrough = passThrough,
    });

    private static FakeWindow WindowOf(IPopupWindow popup) => (FakeWindow)popup.Window;

    [Fact]
    public void AMenuReusingATooltipsWindowTakesClicks()
    {
        var tooltip = Open(passThrough: true);
        var window = WindowOf(tooltip);
        _factory.Release(tooltip);

        var menu = Open(passThrough: false);

        Assert.Same(window, menu.Window);
        Assert.False(window.MousePassThrough);
    }

    [Fact]
    public void CaptureMovesToTheSubmenuAndBackToItsParent()
    {
        var menu = Open(passThrough: false);
        var submenu = Open(passThrough: false);
        Assert.Null(WindowOf(menu).PointerCapture);
        Assert.NotNull(WindowOf(submenu).PointerCapture);

        _factory.Release(submenu);
        Assert.NotNull(WindowOf(menu).PointerCapture);

        _factory.Release(menu);
        Assert.Null(WindowOf(menu).PointerCapture);
    }

    [Fact]
    public void AnOutsideClickReachesOnlyTheMenuShowingNow()
    {
        var first = Open(passThrough: false);
        var firstClicks = new List<PointI>();
        first.OutsideClick += firstClicks.Add;
        _factory.Release(first);

        var second = Open(passThrough: false);
        var secondClicks = new List<PointI>();
        second.OutsideClick += secondClicks.Add;
        WindowOf(second).PointerCapture!(new PointI(0, 0));

        Assert.Empty(firstClicks);
        Assert.Single(secondClicks);
    }

    public void Dispose()
    {
        _factory.Dispose();
        _fonts.Dispose();
    }
}
