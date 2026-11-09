using PngSharp.Api;
using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>A red window and a green popup anchored inside it where it was last frame, wherever the platform put it,
/// declared in either order.</summary>
internal sealed class StackingApp(bool windowFirst) : ImmediateGuiApp<DefaultUiState>(new DefaultUiState())
{
    public const uint WindowColor = 0xFFFF0000;
    public const uint PopupColor = 0xFF00FF00;

    private bool _open = true;
    private WindowBounds? _bounds;
    private UiRect _anchor;

    protected override void OnUpdate(Ui<DefaultUiState> ui)
    {
        ui.WindowBackground = 0xFF0000FF;
        if (windowFirst)
            Window(ui);
        if (ui.TryGetRect(ui.GetId("window"), out var window))
        {
            var anchor = new UiRect(window.X + 40f, window.Y + 20f, 10f, 10f);
            if (anchor != _anchor)
                ui.RequestUpdate();
            _anchor = anchor;
        }
        using (ui.Popup(ui.GetId("popup"), _anchor, new Box { Background = PopupColor }))
            ui.Text("popup", new Box { Width = 120f, Height = 100f });
        if (!windowFirst)
            Window(ui);
    }

    private void Window(Ui<DefaultUiState> ui)
    {
        using (ui.Window(ui.GetId("window"), "Window", ref _open, 160f, 120f, ref _bounds, out _))
        using (ui.Box(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Background = WindowColor }))
            ui.Text("window");
    }

    /// <summary>The middle of where the popup and the window overlap in <paramref name="snapshot"/>, in device pixels
    /// of the main window.</summary>
    public static (int X, int Y) OverlapPixel(SemanticSnapshot snapshot)
    {
        var window = snapshot.Surfaces.Single(s => s.Kind == SurfaceKind.Window).Rect;
        var popup = snapshot.Surfaces.Single(s => s.Kind == SurfaceKind.Popup).Rect;
        var overlap = window.Intersect(popup);
        Assert.True(overlap.Width > 4f && overlap.Height > 4f, $"The window {window} and the popup {popup} do not overlap.");
        var scale = snapshot.Surfaces[0].DpiScale;
        return ((int)((overlap.X + overlap.Width - 2f) * scale), (int)((overlap.Y + overlap.Height - 2f) * scale));
    }

    /// <summary>The RGBA of a decoded RGBA8 PNG's pixel.</summary>
    public static (int R, int G, int B, int A) PixelAt(IRawPng png, int x, int y)
    {
        var i = (y * (int)png.Ihdr.Width + x) * 4;
        var data = png.PixelData;
        return (data[i], data[i + 1], data[i + 2], data[i + 3]);
    }
}

/// <summary>An app that zooms itself to 125%, with a popup.</summary>
internal sealed class ZoomedApp() : ImmediateGuiApp<DefaultUiState>(new DefaultUiState())
{
    public const float Zoom = 1.25f;

    private string _name = "";

    protected override void OnUpdate(Ui<DefaultUiState> ui)
    {
        ui.Scale = Zoom;
        ui.WindowBackground = 0xFF2060A0;
        using (ui.Column(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Padding = 8f, Gap = 8f }))
        {
            ui.Button("Zoomed", id: "zoomed");
            ui.Label("A label at 125%");
            ui.TextField("name", ref _name, width: Sizing.Grow());
        }
        using (ui.Popup(ui.GetId("popup"), new UiRect(40f, 60f, 10f, 10f), new Box { Background = 0xFFFFFFFF }))
            ui.Text("In popup", new Box { Width = 100f, Height = 30f });
    }

    /// <summary>What two harnesses must agree on: every surface's kind, size and scale, and every node's role, label
    /// and bounds on its surface. Where a popup is on the main window is left out: a real monitor can move it.</summary>
    public static string Layout(SemanticSnapshot snapshot) =>
        string.Join("\n", snapshot.Surfaces.Select(s => FormattableString.Invariant($"{s.Kind} {s.Rect.Width}x{s.Rect.Height} @{s.DpiScale}"))
            .Concat(snapshot.Nodes.Select(n => FormattableString.Invariant(
                $"{n.Role} {n.Label} [{n.Bounds.X - n.Surface.Rect.X},{n.Bounds.Y - n.Surface.Rect.Y} {n.Bounds.Width}x{n.Bounds.Height}] on {n.Surface.Index}"))));
}
