using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

public static partial class UiWidgets
{
    private const float MinWindowWidth = 320f;
    private const float MinWindowHeight = 240f;
    private const float ResizeGripSize = 16f;
    private const float ResizeGripStep = 4f;

    internal static int DialogCardId(int dialogId) => HashCode.Combine(dialogId, "card");

    /// <summary>The id of the close button of the dialog, tab or window with <paramref name="ownerId"/>.</summary>
    internal static int CloseButtonId(int ownerId) => HashCode.Combine(ownerId, "close");

    /// <summary>
    /// A modal dialog while <paramref name="open"/>: a titled card centered over the window on a
    /// dimmed backdrop that takes every pointer, wheel and key event from what is behind it. What is
    /// recorded inside is its body, which scrolls when the card would not fit the window. Escape, the
    /// close button or a press on the backdrop set <paramref name="open"/> to false; keyboard focus
    /// returns to where it was once the dialog is gone. A dialog recorded while another is open
    /// stacks on top of it. The close button is a Tab stop; a dialog whose
    /// <see cref="DialogActions"/> offer a way out leaves it off with <paramref name="closeButton"/>.
    /// A dialog that gives Escape a use of its own for a while, such as cancelling an edit, turns
    /// <paramref name="closeOnEscape"/> off meanwhile.
    /// <paramref name="box"/> sizes and pads the card (the theme's <see cref="UiTheme.DialogWidth"/>
    /// wide by default). Record the content only while <see cref="Ui.Scope.IsOpen"/>; dispose either way.
    /// </summary>
    public static Ui.Scope Dialog(this Ui ui, string id, string title, ref bool open, Box box = default, bool closeButton = true,
        bool closeOnEscape = true)
    {
        if (!open) return default;

        var theme = ui.Theme;
        var dialogId = ui.GetId(id);
        var dialog = ui.Modal(dialogId, new Box
        {
            Background = theme.Scrim,
            AlignX = Align.Center,
            AlignY = Align.Center,
            Padding = theme.DialogMargin,
        });

        var backdrop = ui.Interact(dialogId);
        var escaped = closeOnEscape && ui.Input.TakeKey(KeyboardKey.Escape);
        var dismissed = escaped || backdrop.Pressed;

        var radius = box.Radius ?? theme.DialogRadius;
        ui.Column(DialogCardId(dialogId), box with
        {
            Width = box.Width ?? theme.DialogWidth,
            Height = box.Height ?? Sizing.Fit(0f, MathF.Max(0f, ui.Height - theme.DialogMargin * 2)),
            Padding = box.Padding ?? theme.DialogPadding,
            Gap = box.Gap ?? theme.DialogGap,
            Background = box.Background ?? theme.SurfaceRaised,
            BorderColor = box.BorderColor ?? theme.Border,
            BorderWidth = box.BorderWidth ?? theme.BorderWidth,
            Radius = radius,
            Shadow = box.Shadow ?? theme.DialogShadow,
        });

        if (DialogHeader(ui, dialogId, title, closeButton))
            dismissed = true;
        if (dismissed)
            Dismiss(ui, ref open);

        ui.Scroll(HashCode.Combine(dialogId, "body"), new Box { Height = Sizing.Grow(), Gap = theme.Gap });
        return dialog;
    }

    /// <summary>
    /// A <see cref="Dialog"/> in a window of its own while <paramref name="open"/>, so its size does
    /// not depend on the main window's: <paramref name="width"/> by <paramref name="height"/> points
    /// when it opens, or where <paramref name="bounds"/> says it was left, which it keeps up to date
    /// for the app to save. The window has no frame of the platform's: the dialog draws its title and
    /// its close button, moves the window when dragged by its background anywhere but its controls, and a grip in its bottom-right corner that
    /// sizes it, down to <paramref name="minWidth"/> by <paramref name="minHeight"/> points, a smallest
    /// size the content then scrolls in. The main window is dimmed and takes no
    /// input until it closes, and a press on it keeps the dialog. The close button sets
    /// <paramref name="open"/> to false, and so does Escape unless not <paramref name="closeOnEscape"/>,
    /// which leaves Escape to the content — to step back from a page of the dialog. Where the platform
    /// has no windows to give, it is a <see cref="Dialog"/> card over the main window instead. Record the content only while
    /// <see cref="Ui.Scope.IsOpen"/>; dispose either way.
    /// </summary>
    public static Ui.Scope WindowDialog(this Ui ui, string id, string title, ref bool open, float width, float height, ref WindowBounds? bounds,
        bool closeOnEscape = true, float minWidth = MinWindowWidth, float minHeight = MinWindowHeight)
    {
        if (!open) return default;

        var theme = ui.Theme;
        var dialogId = ui.GetId(id);
        var dialog = ui.Modal(dialogId, new Box { Background = theme.Scrim });
        var dismissed = closeOnEscape && ui.Input.TakeKey(KeyboardKey.Escape);

        var windowId = DialogCardId(dialogId);
        ui.Window(windowId, title, ref open, width, height, ref bounds, out var native);
        var content = native ? WindowFrameBox(theme) : WindowCardBox(theme);
        ui.Column(content);
        var handle = ui.InteractBackground(HashCode.Combine(dialogId, "drag"), windowId);
        if (DialogHeader(ui, dialogId, title, closeButton: true))
            dismissed = true;
        if (native)
        {
            DragWindow(ui, windowId, handle);
            ResizeGrip(ui, dialogId, windowId, minWidth, minHeight);
        }
        if (dismissed)
            Dismiss(ui, ref open);

        ui.Scroll(HashCode.Combine(dialogId, "body"), new Box { Height = Sizing.Grow(), Gap = theme.Gap });
        return dialog;
    }

    /// <summary>Moves the window by as far as the pointer has moved since <paramref name="handle"/>
    /// was pressed.</summary>
    private static void DragWindow(Ui ui, int windowId, Interaction handle)
    {
        if (!ui.TryGetRect(windowId, out var rect)) return;
        if (!ui.TryGetDragStart(handle, new PointF(rect.X, rect.Y), out var start)) return;

        var pointer = ui.Input.Pointer;
        var press = ui.PressPosition;
        var x = start.X + pointer.X - press.X;
        var y = start.Y + pointer.Y - press.Y;
        if (x == rect.X && y == rect.Y) return;
        ui.PlaceWindow(windowId, rect with { X = x, Y = y });
    }

    /// <summary>A grip in the bottom-right corner that sizes the window by as far as the pointer has
    /// moved since it was pressed, no smaller than <paramref name="minWidth"/> by <paramref name="minHeight"/>.</summary>
    private static void ResizeGrip(Ui ui, int dialogId, int windowId, float minWidth, float minHeight)
    {
        var theme = ui.Theme;
        var grip = ui.Interact(HashCode.Combine(dialogId, "resize"));
        if (grip.Hovered || grip.Held)
            ui.SetCursor(MouseCursor.ResizeNwse);
        var inset = -(ResizeGripSize + theme.BorderWidth + ResizeGripStep / 2);
        using (ui.Float(1f, 1f, inset, inset, new Box { Width = ResizeGripSize, Height = ResizeGripSize }, id: grip.Id))
            ui.Paint(theme.TextMuted, PaintResizeGrip);
        ResizeWindow(ui, windowId, grip, minWidth, minHeight);
    }

    /// <summary>Three short diagonals stepping into the corner.</summary>
    private static void PaintResizeGrip(UiPainter p, uint color)
    {
        var r = p.Rect;
        for (var step = 1; step <= 3; step++)
        {
            var reach = step * ResizeGripStep;
            p.Line(new PointF(r.Right - reach, r.Bottom), new PointF(r.Right, r.Bottom - reach), 1f, color);
        }
    }

    private static void ResizeWindow(Ui ui, int windowId, Interaction grip, float minWidth, float minHeight)
    {
        if (!ui.TryGetRect(windowId, out var rect)) return;
        if (!ui.TryGetDragStart(grip, rect, out var start)) return;

        var pointer = ui.Input.Pointer;
        var press = ui.PressPosition;
        var width = MathF.Round(MathF.Max(minWidth, start.Width + pointer.X - press.X));
        var height = MathF.Round(MathF.Max(minHeight, start.Height + pointer.Y - press.Y));
        if (width == rect.Width && height == rect.Height) return;
        ui.PlaceWindow(windowId, rect with { Width = width, Height = height });
    }

    /// <summary>A dialog filling a window of its own: no shadow, which the window would cut off.</summary>
    private static Box WindowFrameBox(UiTheme theme) => new()
    {
        Height = Sizing.Grow(),
        Padding = theme.DialogPadding,
        Gap = theme.DialogGap,
        Background = theme.SurfaceRaised,
        BorderColor = theme.Border,
        BorderWidth = theme.BorderWidth,
        Radius = theme.DialogRadius,
    };

    private static Box WindowCardBox(UiTheme theme) => WindowFrameBox(theme) with
    {
        Shadow = theme.DialogShadow,
    };

    /// <summary>The dialog's title and close button over a separator. Returns whether the close
    /// button was clicked.</summary>
    private static bool DialogHeader(Ui ui, int dialogId, string title, bool closeButton)
    {
        var theme = ui.Theme;
        var closeSize = theme.IconButtonSize;
        var clicked = false;
        using (ui.Row(new Box { Height = closeButton ? closeSize : Sizing.Fit(), Gap = theme.Gap }))
        {
            ui.Text(title, size: theme.FontSizeTitle, bold: true, overflow: TextOverflow.Ellipsis);
            ui.Leaf(new Box { Width = Sizing.Grow(), Height = 0f });
            if (closeButton && ui.CloseButton(CloseButtonId(dialogId), closeSize))
                clicked = true;
        }
        ui.Leaf(new Box { Width = Sizing.Grow(), Height = theme.SeparatorWidth, Background = theme.Separator });
        return clicked;
    }

    private static void Dismiss(Ui ui, ref bool open)
    {
        open = false;
        ui.RequestUpdate();
    }

    /// <summary>The row of a dialog's buttons, last in its body: aligned to the end, set apart from
    /// the content above by the theme's <see cref="UiTheme.DialogGap"/>.</summary>
    public static Ui.Scope DialogActions(this Ui ui)
    {
        var theme = ui.Theme;
        return ui.Row(new Box
        {
            Width = Sizing.Grow(),
            Padding = new Spacing(0f, theme.DialogGap - theme.Gap, 0f, 0f),
            Gap = theme.Gap,
            AlignX = Align.End,
            AlignY = Align.Center,
        });
    }
}
