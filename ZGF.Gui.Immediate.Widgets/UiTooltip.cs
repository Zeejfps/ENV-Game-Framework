namespace ZGF.Gui.Immediate.Widgets;

public static partial class UiWidgets
{
    private const int TooltipId = 0x544F4F4C;

    private sealed class TooltipState
    {
        public int Target;
        public double Since;
        public bool Suppressed;
    }

    public static void Tooltip(this Ui ui, Interaction target, string text) => ui.Tooltip(target.Id, text);

    /// <summary>Shows <paramref name="text"/> under the element with <paramref name="id"/> once the
    /// pointer has rested on it for the theme's tooltip delay, until it leaves or a button is
    /// pressed. Call it every frame the element is recorded.</summary>
    public static void Tooltip(this Ui ui, int id, string text)
    {
        var state = ui.GetState<TooltipState>(TooltipId);
        if (!ui.IsHovered(id))
        {
            if (state.Target == id) state.Target = 0;
            return;
        }
        if (state.Target != id)
        {
            state.Target = id;
            state.Since = ui.RealTime;
            state.Suppressed = false;
        }
        var input = ui.Input;
        if (input.IsDown(PointerButton.Left) || input.WasPressed(PointerButton.Left) || input.WasPressed(PointerButton.Right))
            state.Suppressed = true;
        if (state.Suppressed) return;

        var remaining = ui.Theme.TooltipDelaySeconds - (ui.RealTime - state.Since);
        if (remaining > 0)
        {
            ui.RequestFrameIn(remaining);
            return;
        }
        if (!ui.TryGetRect(id, out var anchor)) return;

        var theme = ui.Theme;
        using (ui.Popup(TooltipId, anchor, new Box
        {
            Padding = new Spacing(8f, 4f),
            Background = theme.Surface,
            BorderColor = theme.Border,
            BorderWidth = 1f,
            Radius = 4f,
        }, Align.Center, gap: 6f, passThrough: true))
        {
            ui.Text(text, color: theme.Text, size: MathF.Round(theme.FontSize * 0.9f));
        }
    }
}
