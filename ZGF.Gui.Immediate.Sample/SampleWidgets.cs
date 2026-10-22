using ZGF.Geometry;
using ZGF.KeyboardModule;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Sample;

public static class SampleWidgets
{
    private const float ToggleWidth = 36f;
    private const float ToggleHeight = 20f;
    private const float KnobInset = 3f;

    public static bool Toggle(this Ui ui, string label, ref bool on, string? id = null)
    {
        var theme = ui.Theme;
        var i = ui.Pressable(id ?? label, true, out var keyed, out var ring);
        var flipped = i.Clicked || keyed;
        if (flipped) on = !on;

        var t = ui.Animate(i.Id, on ? 1f : 0f, Spring.Wobbly);
        var track = on ? i.Hovered ? theme.AccentHovered : theme.Accent : i.Hovered ? theme.BorderStrong : theme.Border;
        using (ui.Row(i.Id, new Box { Width = Sizing.Fit(), Height = theme.ControlHeight }))
        {
            using (ui.Box(new Box { Width = ToggleWidth, Height = ToggleHeight, Radius = ToggleHeight / 2, Background = track }))
            {
                ui.Paint((t, theme.OnAccent), static (p, knob) =>
                {
                    var r = p.Rect;
                    var radius = r.Height / 2 - KnobInset;
                    var travel = r.Width - r.Height;
                    p.Circle(new PointF(r.X + r.Height / 2 + knob.t * travel, r.Y + r.Height / 2), radius, knob.OnAccent);
                });
                if (ring) ui.FocusRing(ToggleHeight / 2);
            }
            ui.Text(label);
        }
        return flipped;
    }

    public static bool Stepper(this Ui ui, string id, ref int value, int min, int max)
    {
        var theme = ui.Theme;
        var i = ui.Clickable(id);
        Interaction decrement, increment;
        using (ui.Id(id))
        {
            decrement = value > min ? ui.Clickable("decrement") : Interaction.Inert(ui.GetId("decrement"));
            increment = value < max ? ui.Clickable("increment") : Interaction.Inert(ui.GetId("increment"));
        }
        if (i.Pressed || decrement.Pressed || increment.Pressed) ui.Focus(i.Id, takesText: false);

        var before = value;
        if (decrement.Clicked) value--;
        if (increment.Clicked) value++;

        var focused = ui.TabStop(i.Id);
        if (focused)
        {
            foreach (var stroke in ui.Input.Keys)
            {
                if (stroke.Key is KeyboardKey.LeftArrow or KeyboardKey.DownArrow) value--;
                if (stroke.Key is KeyboardKey.RightArrow or KeyboardKey.UpArrow) value++;
            }
        }
        value = Math.Clamp(value, min, max);

        var hovered = i.Hovered || decrement.Hovered || increment.Hovered;
        using (ui.Row(i.Id, new Box
        {
            Width = Sizing.Fit(),
            Height = theme.ControlHeight,
            Padding = new Spacing(theme.GapTight / 2, 0f),
            Gap = theme.GapTight / 2,
            Radius = theme.CornerRadius,
            Background = theme.Field,
            BorderColor = focused && ui.FocusVisible ? theme.FocusRing : hovered ? theme.BorderStrong : theme.Border,
            BorderWidth = focused && ui.FocusVisible ? theme.FocusRingWidth : theme.BorderWidth,
        }))
        {
            Chevron(ui, "‹", decrement, value > min);
            ui.Text(ui.Memo(id, value, static v => v.ToString()), new Box { Width = theme.ControlHeight - theme.GapTight }, align: Align.Center,
                bold: true);
            Chevron(ui, "›", increment, value < max);
        }
        return value != before;
    }

    private static void Chevron(Ui ui, string glyph, Interaction i, bool enabled)
    {
        var theme = ui.Theme;
        var size = theme.ControlHeight - theme.GapTight * 2;
        ui.Text(glyph, new Box
        {
            Width = size,
            Height = size,
            Radius = theme.CornerRadius,
            Background = theme.ControlFill(i.Held, i.Hovered),
        }, color: enabled ? theme.Text : theme.TextDisabled, align: Align.Center, id: i.Id);
    }
}
