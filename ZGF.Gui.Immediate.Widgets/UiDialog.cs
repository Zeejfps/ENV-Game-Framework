using ZGF.Desktop;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>Ends a <see cref="UiWidgets.Dialog"/>. Record the dialog's content only while
/// <see cref="IsOpen"/>; dispose either way.</summary>
public readonly struct DialogScope : IDisposable
{
    private readonly Ui.Scope _modal;
    private readonly Ui.Scope _card;
    private readonly Ui.Scope _body;

    internal DialogScope(Ui.Scope modal, Ui.Scope card, Ui.Scope body)
    {
        IsOpen = true;
        _modal = modal;
        _card = card;
        _body = body;
    }

    public bool IsOpen { get; }

    public void Dispose()
    {
        if (!IsOpen) return;
        _body.Dispose();
        _card.Dispose();
        _modal.Dispose();
    }
}

public static partial class UiWidgets
{
    /// <summary>
    /// A modal dialog while <paramref name="open"/>: a titled card centered over the window on a
    /// dimmed backdrop that takes every pointer, wheel and key event from what is behind it. What is
    /// recorded inside is its body, which scrolls when the card would not fit the window. Escape, the
    /// close button or a press on the backdrop set <paramref name="open"/> to false; keyboard focus
    /// returns to where it was once the dialog is gone. A dialog recorded while another is open
    /// stacks on top of it. The close button is a Tab stop; a dialog whose
    /// <see cref="DialogActions"/> offer a way out leaves it off with <paramref name="closeButton"/>.
    /// <paramref name="box"/> sizes and pads the card (the theme's <see cref="UiTheme.DialogWidth"/>
    /// wide by default).
    /// </summary>
    public static DialogScope Dialog(this Ui ui, string id, string title, ref bool open, Box box = default, bool closeButton = true)
    {
        if (!open) return default;

        var theme = ui.Theme;
        var dialogId = ui.GetId(id);
        var modal = ui.Modal(dialogId, new Box
        {
            Background = theme.Scrim,
            AlignX = Align.Center,
            AlignY = Align.Center,
            Padding = theme.DialogMargin,
        });

        var backdrop = ui.Interact(dialogId);
        var close = default(Interaction);
        var closeKeyed = false;
        var closeRing = false;
        if (closeButton)
            close = Pressable(ui, HashCode.Combine(dialogId, "close"), true, out closeKeyed, out closeRing);
        if (ui.Input.TakeKey(KeyboardKey.Escape) || close.Clicked || closeKeyed || backdrop.Pressed)
        {
            open = false;
            ui.RequestFrame();
        }

        var radius = box.Radius ?? theme.DialogRadius;
        var card = ui.Column(HashCode.Combine(dialogId, "card"), box with
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

        var closeSize = theme.IconButtonSize;
        using (ui.Row(new Box { Height = closeButton ? closeSize : Sizing.Fit(), Gap = theme.Gap }))
        {
            ui.Text(title, size: theme.FontSizeTitle, bold: true, overflow: TextOverflow.Ellipsis);
            ui.Leaf(new Box { Width = Sizing.Grow(), Height = 0f });
            if (closeButton)
            {
                using (ui.Box(close.Id, new Box
            {
                Width = closeSize,
                Height = closeSize,
                Radius = theme.CornerRadius,
                Background = close.Held ? theme.ControlPressed : close.Hovered ? theme.ControlHovered : 0u,
            }))
                {
                    ui.Paint((close.Hovered ? theme.Text : theme.TextMuted, theme.IconStrokeWidth), static (p, s) => DrawCross(p, s.Item1, s.IconStrokeWidth));
                    if (closeRing) FocusRing(ui, theme.CornerRadius);
                }
            }
        }
        ui.Leaf(new Box { Width = Sizing.Grow(), Height = theme.SeparatorWidth, Background = theme.Separator });

        var body = ui.Scroll(HashCode.Combine(dialogId, "body"), new Box { Height = Sizing.Grow(), Gap = theme.Gap });
        return new DialogScope(modal, card, body);
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
