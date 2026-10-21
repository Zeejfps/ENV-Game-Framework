using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.KeyboardModule;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Sample;

public static class SampleWidgets
{
    public static bool Toggle(this Ui ui, string label, ref bool on, string? id = null)
    {
        var theme = ui.Theme;
        var i = ui.Interact(id ?? label);
        if (i.Hovered) ui.SetCursor(MouseCursor.Hand);
        if (i.Clicked) on = !on;

        var t = ui.Animate(i.Id, on ? 1f : 0f, Spring.Wobbly);
        var knobColor = theme.OnAccent;
        using (ui.Row(i.Id, new Box { Width = Sizing.Fit(), Height = theme.ControlHeight }))
        {
            using (ui.Box(new Box { Width = 36f, Height = 20f, Radius = 10f, Background = on ? theme.Accent : i.Hovered ? theme.Hovered(theme.Border) : theme.Border }))
                ui.Paint((t, knobColor), static (p, knob) => p.Circle(new PointF(p.Rect.X + 10f + knob.t * 16f, p.Rect.Y + 10f), 7f, knob.knobColor));
            ui.Text(label);
        }
        return i.Clicked;
    }

    public static bool Stepper(this Ui ui, string id, ref int value, int min, int max)
    {
        var theme = ui.Theme;
        var i = ui.Interact(id);
        Interaction decrement, increment;
        using (ui.Id(id))
        {
            decrement = value > min ? ui.Interact("decrement") : Interaction.Inert(ui.GetId("decrement"));
            increment = value < max ? ui.Interact("increment") : Interaction.Inert(ui.GetId("increment"));
        }
        if (i.Hovered || decrement.Hovered || increment.Hovered) ui.SetCursor(MouseCursor.Hand);
        if (i.Pressed || decrement.Pressed || increment.Pressed) ui.Focus(i.Id);

        var before = value;
        if (decrement.Clicked) value--;
        if (increment.Clicked) value++;

        var focused = ui.HasFocus(i.Id);
        if (focused)
        {
            foreach (var stroke in ui.Input.Keys)
            {
                if (stroke.Key is KeyboardKey.LeftArrow or KeyboardKey.DownArrow) value--;
                if (stroke.Key is KeyboardKey.RightArrow or KeyboardKey.UpArrow) value++;
            }
        }
        value = Math.Clamp(value, min, max);

        using (ui.Row(i.Id, new Box
        {
            Width = Sizing.Fit(),
            Height = theme.ControlHeight,
            Padding = new Spacing(3f, 0f),
            Gap = 2f,
            Radius = theme.CornerRadius,
            Background = theme.Field,
            BorderColor = focused ? theme.Accent : theme.Border,
            BorderWidth = 1f,
        }))
        {
            Chevron(ui, "‹", decrement, value > min);
            ui.Text(value.ToString(), new Box { Width = 24f }, align: Align.Center, bold: true);
            Chevron(ui, "›", increment, value < max);
        }
        return value != before;
    }

    private static void Chevron(Ui ui, string glyph, Interaction i, bool enabled)
    {
        var theme = ui.Theme;
        ui.Text(glyph, new Box
        {
            Width = 22f,
            Height = 22f,
            Radius = 4f,
            Background = i.Held ? theme.Pressed(theme.Field) : i.Hovered ? theme.Hovered(theme.Field) : 0u,
        }, color: enabled ? theme.Text : theme.TextMuted, align: Align.Center, id: i.Id);
    }
}
