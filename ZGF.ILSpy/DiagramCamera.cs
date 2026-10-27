using ZGF.Geometry;
using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

/// <summary>Where a diagram is looked at from and how near: the point of the diagram at the top-left of
/// the view, and the zoom, kept between a tenth of the diagram's own size and four times it.
/// <see cref="Changes"/> counts the moves: each step of the zoom, the wheel, a fit or a centering, and a
/// move followed frame by frame, such as a drag, once when it ends.</summary>
public sealed class DiagramCamera
{
    private const float MinZoom = 0.1f;
    private const float MaxZoom = 4f;
    private const float ZoomStep = 1.25f;
    private const float FitMargin = 24f;

    private float _zoom = 1f;
    private bool _isMoving;

    /// <summary>The point of the diagram at the top-left of the view.</summary>
    public PointF Corner { get; private set; }

    public float Zoom => _zoom;

    public int Percent => (int)MathF.Round(_zoom * 100f);

    public bool CanZoomIn => _zoom < MaxZoom;

    public bool CanZoomOut => _zoom > MinZoom;

    public int Changes { get; private set; }

    /// <summary>Where <paramref name="point"/> of the diagram is drawn in <paramref name="view"/>.</summary>
    public PointF ToScreen(PointF point, UiRect view)
    {
        return new PointF(view.X + (point.X - Corner.X) * _zoom, view.Y + (point.Y - Corner.Y) * _zoom);
    }

    /// <summary>The point of the diagram drawn at <paramref name="point"/> of <paramref name="view"/>.</summary>
    public PointF ToDiagram(PointF point, UiRect view)
    {
        return new PointF(Corner.X + (point.X - view.X) / _zoom, Corner.Y + (point.Y - view.Y) / _zoom);
    }

    /// <summary>Looks at the diagram with <paramref name="corner"/> at the view's top-left, as one frame of
    /// a move that <see cref="FinishMoving"/> counts once it ends.</summary>
    public void LookFrom(PointF corner)
    {
        if (corner == Corner)
            return;
        Corner = corner;
        _isMoving = true;
    }

    /// <summary>Counts the move <see cref="LookFrom"/> followed as one change, once it ended.</summary>
    public void FinishMoving()
    {
        if (!_isMoving)
            return;
        _isMoving = false;
        Changes++;
    }

    /// <summary>Looks at the diagram from <paramref name="corner"/> at <paramref name="zoom"/>, as the
    /// settings kept it.</summary>
    public void Restore(PointF corner, float zoom)
    {
        Corner = corner;
        _zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
    }

    /// <summary>Moves the diagram by <paramref name="dx"/> and <paramref name="dy"/> on the screen.</summary>
    public void Pan(float dx, float dy)
    {
        Corner = new PointF(Corner.X - dx / _zoom, Corner.Y - dy / _zoom);
        Changes++;
    }

    /// <summary>Zooms by <paramref name="factor"/>, keeping the point of the diagram under
    /// <paramref name="anchor"/> of <paramref name="view"/> where it is.</summary>
    public void ZoomAt(PointF anchor, UiRect view, float factor)
    {
        var held = ToDiagram(anchor, view);
        _zoom = Math.Clamp(_zoom * factor, MinZoom, MaxZoom);
        Corner = new PointF(held.X - (anchor.X - view.X) / _zoom, held.Y - (anchor.Y - view.Y) / _zoom);
        Changes++;
    }

    public void ZoomIn(UiRect view) => ZoomAt(CenterOf(view), view, ZoomStep);

    public void ZoomOut(UiRect view) => ZoomAt(CenterOf(view), view, 1f / ZoomStep);

    /// <summary>Zooms out until a diagram <paramref name="width"/> by <paramref name="height"/> fits
    /// <paramref name="view"/> with a margin, but never in past its own size, and centers it.</summary>
    public void Fit(float width, float height, UiRect view)
    {
        var across = (view.Width - FitMargin * 2) / MathF.Max(width, 1f);
        var down = (view.Height - FitMargin * 2) / MathF.Max(height, 1f);
        var fitting = MathF.Min(MathF.Min(across, down), 1f);
        _zoom = Math.Clamp(fitting, MinZoom, MaxZoom);
        CenterOn(new PointF(width / 2, height / 2), view);
    }

    /// <summary>Moves the diagram so that <paramref name="point"/> of it is drawn at the middle of <paramref name="view"/>.</summary>
    public void CenterOn(PointF point, UiRect view)
    {
        Corner = new PointF(point.X - view.Width / 2 / _zoom, point.Y - view.Height / 2 / _zoom);
        Changes++;
    }

    /// <summary>Whether the part of the diagram <paramref name="width"/> by <paramref name="height"/> from
    /// <paramref name="topLeft"/> is drawn wholly inside <paramref name="view"/>.</summary>
    public bool Shows(PointF topLeft, float width, float height, UiRect view)
    {
        var start = ToScreen(topLeft, view);
        var end = ToScreen(new PointF(topLeft.X + width, topLeft.Y + height), view);
        var across = start.X >= view.X && end.X <= view.Right;
        var down = start.Y >= view.Y && end.Y <= view.Bottom;
        return across && down;
    }

    private static PointF CenterOf(UiRect view) => new(view.X + view.Width / 2, view.Y + view.Height / 2);
}
