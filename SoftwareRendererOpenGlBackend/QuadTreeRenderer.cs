using SoftwareRendererModule;
using ZnvQuadTree;

namespace SoftwareRendererOpenGlBackend;

sealed class Item
{
    public PointF Position { get; set; }
}

public sealed class QuadTreeRenderer : IDisposable
{
    public int Width { get; }
    public int Height { get; }

    private readonly Bitmap _colorBuffer;
    private readonly BitmapRenderer _bitmapRenderer;
    private readonly QuadTreePointF<Item> _quadTree;

    private bool _isDisposed;
    private PointF _mousePosition;

    public QuadTreeRenderer()
    {
        Width = 160;
        Height = 120;

        _colorBuffer = new Bitmap(Width, Height);
        _bitmapRenderer = new BitmapRenderer(_colorBuffer);

        _quadTree = new QuadTreePointF<Item>(new RectF
        {
            Bottom = 0,
            Left = 0,
            Width = Width,
            Height = Height
        }, 6, maxDepth: 4);
    }

    public void Render()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(QuadTreeRenderer));

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
            if (bounds.Contains(_mousePosition))
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
        if (_isDisposed)
            return;

        _isDisposed = true;
        _bitmapRenderer.Dispose();
    }

    public void SetMousePosition(int x, int y)
    {
        _mousePosition =  new PointF(x, y);
    }

    public void AddItemAt(int x, int y)
    {
        var item = new Item
        {
            Position = new PointF
            {
                X = x,
                Y = y
            }
        };
        _quadTree.Insert(item, item.Position);
    }
}