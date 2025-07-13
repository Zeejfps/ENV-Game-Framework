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

        foreach (var nodeInfo in quadTreeInfo.Nodes)
        {
            var bounds = nodeInfo.Bounds;
            Graphics.DrawRect(colorBuffer,
                (int)bounds.Left, (int)bounds.Bottom,
                (int)bounds.Width, (int)bounds.Height, 0x00FF00);
        }

        foreach (var item in _quadTree.GetAllItems())
        {
            var position = item.Position;
            _colorBuffer.SetPixel((int)position.X, (int)position.Y, 0xFF00FF);
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