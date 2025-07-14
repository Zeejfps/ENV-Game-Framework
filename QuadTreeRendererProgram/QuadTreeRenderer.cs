using SoftwareRendererModule;
using ZGF.Geometry;
using ZnvQuadTree;

namespace SoftwareRendererOpenGlBackend;

public sealed class Item
{
    public PointF Position { get; set; }
}

public sealed class QuadTreeRenderer : IDisposable
{
    public int FramebufferWidth { get; }
    public int FramebufferHeight { get; }

    private readonly Bitmap _colorBuffer;
    private readonly BitmapRenderer _bitmapRenderer;
    private readonly QuadTreePointF<Item> _quadTree;

    private bool _isDisposed;
    private PointF _mousePosition;

    public QuadTreeRenderer(int framebufferWidth, int framebufferHeight, QuadTreePointF<Item> quadTree)
    {
        FramebufferWidth = framebufferWidth;
        FramebufferHeight = framebufferHeight;

        _colorBuffer = new Bitmap(framebufferWidth, framebufferHeight);
        _bitmapRenderer = new BitmapRenderer(_colorBuffer);
        _quadTree = quadTree;
    }

    ~QuadTreeRenderer()
    {
        Dispose(false);
    }

    public void Render()
    {
        var colorBuffer = _colorBuffer;
        colorBuffer.Fill(0x000000);

        var quadTreeInfo = _quadTree.GetInfo();

        var deepestNodeBounds = new RectF();
        var deepestDepthSoFar = -1;
        var mouseInNode = false;
        foreach (var nodeInfo in quadTreeInfo.Nodes)
        {
            var bounds = nodeInfo.Bounds;
            uint color = 0x00FF00;
            if (bounds.ContainsPoint(_mousePosition))
            {
                mouseInNode = true;
                if (nodeInfo.Depth > deepestDepthSoFar)
                {
                    deepestDepthSoFar =  nodeInfo.Depth;
                    deepestNodeBounds = nodeInfo.Bounds;
                }
            }

            Graphics.DrawRect(colorBuffer,
                (int)bounds.Left, (int)bounds.Bottom,
                (int)bounds.Width, (int)bounds.Height, color);
        }

        if (mouseInNode)
        {
            var bounds = deepestNodeBounds;
            Graphics.DrawRect(colorBuffer,
                (int)bounds.Left, (int)bounds.Bottom,
                (int)bounds.Width, (int)bounds.Height, 0xFF0000);
        }

        foreach (var item in _quadTree.GetAllItems())
        {
            var position = item.Position;
            _colorBuffer.SetPixel((int)position.X, (int)position.Y, 0xFF00FF);
        }

        if (_quadTree.TryFindNearest(_mousePosition, float.MaxValue, out var closestItem))
        {
            var position = closestItem.Position;
            _colorBuffer.SetPixel((int)position.X, (int)position.Y, 0x00FF00);
        }

        _bitmapRenderer.Render();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            _bitmapRenderer.Dispose();
        }
        
        _isDisposed = true;
    }

    public void SetMousePosition(int x, int y)
    {
        _mousePosition =  new PointF(x, y);
    }
}