using EasyGameFramework.GUI;
using OpenGLSandbox;
using Raylib_CsLo;
using Color = Raylib_CsLo.Color;

namespace Bricks.RaylibBackend;

public sealed class RaylibPanel : IRenderedPanel, IRenderCommand
{
    private readonly CommandBuffer _commandBuffer;
    
    public RaylibPanel(CommandBuffer commandBuffer, Rect screenPosition, PanelStyle style)
    {
        _commandBuffer = commandBuffer;
        ScreenRect = screenPosition;
        Style = style;
    }

    public Rect ScreenRect { get; set; }
    public PanelStyle Style { get; set; }

    public void Dispose()
    {
        _commandBuffer.Remove(this);
    }

    public void Render()
    {
        var screenRect = ScreenRect;
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