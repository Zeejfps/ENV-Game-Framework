using ZGF.Geometry;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;

namespace ZGF.Gui.Desktop.Components.HorizontalScrollBar;

public sealed class HorizontalScrollBarView : View
{
    private readonly HorizontalScrollBarThumbView _thumbView;
    private readonly RectView _slideArea;

    public event Action<float> ScrollPositionChanged
    {
        add => _thumbView.ScrollPositionChanged += value;
        remove => _thumbView.ScrollPositionChanged -= value;
    }

    public HorizontalScrollBarThumbView Thumb => _thumbView;

    public uint TrackBackgroundColor
    {
        get => _slideArea.BackgroundColor;
        set => _slideArea.BackgroundColor = value;
    }

    public BorderColorStyle TrackBorderColor
    {
        get => _slideArea.BorderColor;
        set => _slideArea.BorderColor = value;
    }

    public BorderSizeStyle TrackBorderSize
    {
        get => _slideArea.BorderSize;
        set => _slideArea.BorderSize = value;
    }

    public HorizontalScrollBarView(InputSystem input)
    {
        Height = 12;

        _thumbView = new HorizontalScrollBarThumbView();

        _slideArea = new RectView
        {
            BackgroundColor = 0xFFCECECE,
            BorderSize = BorderSizeStyle.All(1),
            BorderColor = new BorderColorStyle
            {
                Left = 0xFF9C9C9C,
                Top = 0xFF9C9C9C,
                Right = 0xFFFFFFFF,
                Bottom = 0xFFFFFFFF
            },
            Children =
            {
                _thumbView
            }
        };

        AddChildToSelf(_slideArea);

        var hovered = false;
        DragRecognizer? drag = null;
        _thumbView.UseController(input, () => drag = new DragRecognizer(input)
        {
            DragStarted = () => _thumbView.IsSelected = true,
            Dragged = delta => _thumbView.Move(delta.X),
            DragEnded = () =>
            {
                if (!hovered) _thumbView.IsSelected = false;
            },
        });
        _thumbView.UseController(input, new KbmHandlers
        {
            OnHoverEnter = () =>
            {
                hovered = true;
                _thumbView.IsSelected = true;
            },
            OnHoverExit = () =>
            {
                hovered = false;
                if (drag is not { IsDragging: true }) _thumbView.IsSelected = false;
            },
        });
    }

    public float Scale
    {
        get => _thumbView.Scale;
        set => _thumbView.Scale = value;
    }

    public void SetNormalizedScrollPosition(float normalizedPosition)
    {
        _thumbView.SetScrollPositionNormalized(normalizedPosition);
    }

    public void Scroll(float deltaX)
    {
        _thumbView.Move(deltaX);
    }

    public void ScrollToStart()
    {
        _thumbView.ScrollToStart();
    }

    public void ScrollToPoint(PointF point)
    {
        _thumbView.ScrollToPoint(point);
    }
}
