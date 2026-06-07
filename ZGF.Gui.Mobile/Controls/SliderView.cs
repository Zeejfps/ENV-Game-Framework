using System;
using ZGF.Geometry;
using ZGF.Gui.Mobile.Controllers;

namespace ZGF.Gui.Mobile.Controls;

/// <summary>
/// A horizontal value slider: a track, a fill up to the current value, and a draggable thumb.
/// The first reusable input control built on the shared mobile input stack — drop it in a tree,
/// set <see cref="Min"/>/<see cref="Max"/>/<see cref="Value"/>, and subscribe to
/// <see cref="ValueChanged"/>. It self-wires a <see cref="SliderPointerController"/> as soon as a
/// Context is attached, so the host never touches the input system directly.
///
/// Drawn entirely from rounded rectangles (the canvas has no line/arc primitives), which is all a
/// linear slider needs. Touch captures to the slider on press, so a drag keeps updating the value
/// even if the finger drifts off the track.
/// </summary>
public sealed class SliderView : MultiChildView
{
    private float _min;
    private float _max = 1f;
    private float _value;

    public SliderView()
    {
        // Registered against this view in the MobileInputSystem the moment a Context attaches.
        this.UsePointerController(_ => new SliderPointerController(this));
    }

    public float Min
    {
        get => _min;
        set { if (SetField(ref _min, value)) ClampValue(); }
    }

    public float Max
    {
        get => _max;
        set { if (SetField(ref _max, value)) ClampValue(); }
    }

    public float Value
    {
        get => _value;
        set
        {
            var lo = MathF.Min(_min, _max);
            var hi = MathF.Max(_min, _max);
            var clamped = Math.Clamp(value, lo, hi);
            if (SetField(ref _value, clamped))
                ValueChanged?.Invoke(_value);
        }
    }

    public float TrackThickness { get; set; } = 6f;
    public float ThumbRadius { get; set; } = 13f;

    public uint TrackColor { get; set; } = 0xFF2A3142;
    public uint FillColor { get; set; } = 0xFF4C8DFF;
    public uint ThumbColor { get; set; } = 0xFFFFFFFF;
    public uint ThumbBorderColor { get; set; } = 0x33101522;
    public float ThumbBorderThickness { get; set; } = 1.5f;

    /// <summary>Raised whenever the value changes, whether by drag or by code.</summary>
    public Action<float>? ValueChanged { get; set; }

    public float Normalized => _max > _min ? (_value - _min) / (_max - _min) : 0f;

    private void ClampValue() => Value = _value;

    /// <summary>Set the value from a pointer X coordinate (GUI space) over the track.</summary>
    public void SetValueFromPointerX(float x)
    {
        var travel = Position.Width - 2f * ThumbRadius;
        if (travel <= 0f)
            return;
        var t = Math.Clamp((x - (Position.Left + ThumbRadius)) / travel, 0f, 1f);
        Value = _min + t * (_max - _min);
    }

    public override float MeasureHeight(float availableWidth) => MathF.Max(ThumbRadius * 2f, TrackThickness);

    protected override void OnDrawSelf(ICanvas c)
    {
        var z = GetDrawZIndex();
        var pos = Position;
        var centerY = pos.Bottom + pos.Height * 0.5f;
        var halfTrack = TrackThickness * 0.5f;

        // Inset the line endpoints by half the thickness so its rounded caps land exactly on the
        // slider's left/right edges.
        var trackLeft = pos.Left + halfTrack;
        var trackRight = pos.Right - halfTrack;
        var thumbCenterX = pos.Left + ThumbRadius + Normalized * (pos.Width - 2f * ThumbRadius);

        c.DrawLine(new DrawLineInputs
        {
            Start = new PointF(trackLeft, centerY),
            End = new PointF(trackRight, centerY),
            Thickness = TrackThickness,
            Color = TrackColor,
            ZIndex = z,
        });

        c.DrawLine(new DrawLineInputs
        {
            Start = new PointF(trackLeft, centerY),
            End = new PointF(MathF.Max(thumbCenterX, trackLeft), centerY),
            Thickness = TrackThickness,
            Color = FillColor,
            ZIndex = z + 1,
        });

        var thumbCenter = new PointF(thumbCenterX, centerY);
        c.DrawCircle(new DrawCircleInputs
        {
            Center = thumbCenter,
            Radius = ThumbRadius,
            Color = ThumbColor,
            ZIndex = z + 2,
        });

        // Subtle ring just inside the thumb edge for definition against the track.
        if (ThumbBorderThickness > 0f)
        {
            c.DrawCircle(new DrawCircleInputs
            {
                Center = thumbCenter,
                Radius = ThumbRadius - ThumbBorderThickness * 0.5f,
                Color = ThumbBorderColor,
                ZIndex = z + 3,
                Thickness = ThumbBorderThickness,
            });
        }
    }
}
