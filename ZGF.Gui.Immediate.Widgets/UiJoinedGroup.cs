namespace ZGF.Gui.Immediate.Widgets;

/// <summary>The controls recorded so far in an open <see cref="UiWidgets.JoinedGroup"/>.</summary>
public sealed class JoinedControls
{
    internal int Count;
}

/// <summary>Where a control goes in an open <see cref="UiWidgets.JoinedGroup"/>: <see cref="First"/> of its row, or
/// after another, which it is divided from by a line on its leading edge.</summary>
public readonly record struct JoinedSlot(bool First);

public static partial class UiWidgets
{
    /// <summary>
    /// Records the controls opened inside it as one joined strip, as a professional toolbar groups a transport or a
    /// pair of fields: one box on the control fill, outlined and rounded at its ends only, its controls square, side
    /// by side with a line between each and the next, and filling its height inside its outline. The stock painters of
    /// <see cref="IconButton"/>, <see cref="IconToggle"/> and <see cref="NumberField"/> join it; a painter of an
    /// app's own asks <see cref="TakeJoinedSlot"/> where its control goes.
    /// </summary>
    public static Ui.Scope JoinedGroup(this Ui ui, string id)
    {
        var theme = ui.Theme;
        var groupId = ui.GetId(id);
        var joined = ui.GetState<JoinedControls>(HashCode.Combine(groupId, "joined"));
        joined.Count = 0;
        // The group's scope is opened first, so disposing it ends the row recorded inside it too.
        var scope = ui.Group(joined, static _ => { });
        ui.Row(groupId, new Box
        {
            Width = Sizing.Fit(),
            Height = theme.ControlHeight,
            // Its controls sit inside its outline, so their fills don't cover it; they fill its height.
            Padding = new Spacing(theme.BorderWidth),
            Gap = 0f,
            AlignY = Align.Center,
            Background = theme.Control,
            BorderColor = theme.Border,
            BorderWidth = theme.BorderWidth,
            Radius = theme.CornerRadius,
            Clip = true,
        });
        return scope;
    }

    /// <summary>Where the control about to be recorded goes in the innermost open <see cref="JoinedGroup"/>, counting
    /// it in; null outside any. A painter calls it once per control it records.</summary>
    public static JoinedSlot? TakeJoinedSlot(this Ui ui)
    {
        var joined = ui.FindOpenGroup<JoinedControls>();
        if (joined == null)
            return null;
        var first = joined.Count == 0;
        joined.Count++;
        return new JoinedSlot(first);
    }

    /// <summary>The line on a joined control's leading edge that divides it from the one before; nothing for the first
    /// control of its group, or one outside any. Call it inside the control's element.</summary>
    public static void PaintJoinedDivider(this Ui ui, JoinedSlot? slot)
    {
        if (slot == null || slot.Value.First)
            return;
        ui.Paint((ui.Theme.Border, ui.Theme.BorderWidth), static (painter, line) =>
        {
            var r = painter.Rect;
            painter.FillRect(new UiRect(r.X, r.Y, line.BorderWidth, r.Height), line.Border);
        });
    }
}
