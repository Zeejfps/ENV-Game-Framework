using EasyGameFramework.Api.Rendering;
using EasyGameFramework.GUI;
using OpenGLSandbox;
using Raylib_CsLo;
using Color = Raylib_CsLo.Color;

namespace Bricks.RaylibBackend;

public sealed class RaylibBuildContext : IBuildContext
{
    public IPanelRenderer PanelRenderer => _raylibPanelRenderer;
    public ITextRenderer TextRenderer { get; }
    public FocusTree FocusTree { get; }

    private readonly RaylibPanelRenderer _raylibPanelRenderer;
    
    public RaylibBuildContext()
    {
        _raylibPanelRenderer = new RaylibPanelRenderer();
    }

    public void Render()
    {
        _raylibPanelRenderer.Update();
    }
}

public sealed class RaylibPanelRenderer : IPanelRenderer
{
    private readonly HashSet<RaylibPanel> _renderablePanels = new();
    
    public IRenderedPanel Render(Rect screenPosition, PanelStyle style)
    {
        var panel = new RaylibPanel(this, screenPosition, style);
        _renderablePanels.Add(panel);
        return panel;
    }

    public void Update()
    {
        foreach (var panel in _renderablePanels)
            panel.Render();
    }

    public void Remove(RaylibPanel raylibPanel)
    {
        _renderablePanels.Remove(raylibPanel);
    }
}

public sealed class RaylibPanel : IRenderedPanel
{
    private readonly RaylibPanelRenderer _renderer;
    
    public RaylibPanel(RaylibPanelRenderer renderer, Rect screenPosition, PanelStyle style)
    {
        _renderer = renderer;
        ScreenRect = screenPosition;
        Style = style;
    }

    public Rect ScreenRect { get; set; }
    public PanelStyle Style { get; set; }

    public void Dispose()
    {
        _renderer.Remove(this);
    }

    public void Render()
    {
        var screenRect = ScreenRect;
        Console.WriteLine(screenRect);
        var backgroundColor = Style.BackgroundColor;
        Raylib.DrawRectangle(
            (int)screenRect.X, (int)screenRect.Y,
            (int)screenRect.Width, (int)screenRect.Height,
            new Color(
                (byte)(backgroundColor.R * 255), 
                (byte)(backgroundColor.G * 255), 
                (byte)(backgroundColor.B * 255), 
                (byte)(backgroundColor.A * 255)
            )
        );
    }
}