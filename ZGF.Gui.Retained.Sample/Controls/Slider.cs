using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.Views;
using ZGF.Gui.Widgets;
using ZGF.Observable;

namespace ZGF.Gui.Retained.Sample.Controls;

public sealed record Slider : Widget
{
    private const int ThumbRadius = 8;
    private const int TrackHeight = 4;

    public required Prop<float> Value { get; init; }
    public float Min { get; init; }
    public float Max { get; init; } = 1f;

    protected override IWidget Build(Context ctx)
    {
        var value = Value.ToReadable(ctx);
        var fraction = new Derived<float>(() => Max > Min ? Math.Clamp((value.Value - Min) / (Max - Min), 0f, 1f) : 0f);
        var rest = fraction.Bind(f => 1f - f);
        var input = ctx.Require<InputSystem>();
        var trackInset = ((int)Palette.ControlHeight - TrackHeight) / 2;

        return new KbmInput
        {
            Controller = view => new SliderController(view, input, ThumbRadius, f => Value.Write(Min + f * (Max - Min))),
            Child = new Stack
            {
                Height = Palette.ControlHeight,
                Children =
                [
                    new Padding
                    {
                        Amount = new PaddingStyle { Left = ThumbRadius, Right = ThumbRadius, Top = trackInset, Bottom = trackInset },
                        Children =
                        [
                            new Box
                            {
                                Background = Palette.Control,
                                BorderRadius = BorderRadiusStyle.All(TrackHeight / 2f),
                                Children =
                                [
                                    new Row
                                    {
                                        CrossAxis = CrossAxisAlignment.Stretch,
                                        Children =
                                        [
                                            new Grow
                                            {
                                                Factor = Prop.Bind(fraction),
                                                Child = new Box { Background = Palette.Accent, BorderRadius = BorderRadiusStyle.All(TrackHeight / 2f) },
                                            },
                                            new Grow { Factor = rest, Child = Empty.Widget },
                                        ],
                                    },
                                ],
                            },
                        ],
                    },
                    new Row
                    {
                        CrossAxis = CrossAxisAlignment.Center,
                        Children =
                        [
                            new Grow { Factor = Prop.Bind(fraction), Child = Empty.Widget },
                            new Box
                            {
                                Width = ThumbRadius * 2,
                                Height = ThumbRadius * 2,
                                Background = Palette.Text,
                                BorderRadius = BorderRadiusStyle.All(ThumbRadius),
                            },
                            new Grow { Factor = rest, Child = Empty.Widget },
                        ],
                    },
                ],
            },
        };
    }
}

internal sealed class SliderController(View view, InputSystem input, float inset, Action<float> setFraction) : KeyboardMouseController
{
    private bool _dragging;

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling || e.Button != MouseButton.Left)
            return;

        if (e.State == InputState.Pressed)
        {
            _dragging = true;
            input.StealFocus(this);
            Update(e.Mouse.Point.X);
            e.Consume();
        }
        else if (_dragging)
        {
            _dragging = false;
            input.Blur(this);
            e.Consume();
        }
    }

    public override void OnMouseMoved(ref MouseMoveEvent e)
    {
        if (!_dragging) return;
        Update(e.Mouse.Point.X);
        e.Consume();
    }

    private void Update(float x)
    {
        var position = view.Position;
        var track = position.Width - inset * 2;
        if (track <= 0f) return;
        setFraction(Math.Clamp((x - position.Left - inset) / track, 0f, 1f));
    }
}
