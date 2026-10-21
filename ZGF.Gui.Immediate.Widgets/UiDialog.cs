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
    private const uint DialogScrim = 0x99000000;
    private const float DialogWidth = 480f;
    private const float DialogRadius = 10f;
    private const float DialogPadding = 20f;
    private const float DialogMargin = 32f;
    private const float DialogCloseSize = 28f;

    /// <summary>
    /// A modal dialog while <paramref name="open"/>: a titled card centered over the window on a
    /// dimmed backdrop that takes every pointer, wheel and key event from what is behind it. What is
    /// recorded inside is its body, which scrolls when the card would not fit the window. Escape, the
    /// close button or a press on the backdrop set <paramref name="open"/> to false; keyboard focus
    /// returns to where it was once the dialog is gone. A dialog recorded while another is open
    /// stacks on top of it. <paramref name="box"/> sizes and pads the card (480 wide by default).
    /// </summary>
    public static DialogScope Dialog(this Ui ui, string id, string title, ref bool open, Box box = default)
    {
        if (!open) return default;

        var theme = ui.Theme;
        var dialogId = ui.GetId(id);
        var modal = ui.Modal(dialogId, new Box
        {
            Background = DialogScrim,
            AlignX = Align.Center,
            AlignY = Align.Center,
            Padding = DialogMargin,
        });

        var backdrop = ui.Interact(dialogId);
        var close = ui.Interact(HashCode.Combine(dialogId, "close"));
        if (close.Hovered) ui.SetCursor(MouseCursor.Hand);
        if (ui.Input.TakeKey(KeyboardKey.Escape) || close.Clicked || backdrop.Pressed)
        {
            open = false;
            ui.RequestFrame();
        }

        var card = ui.Column(HashCode.Combine(dialogId, "card"), box with
        {
            Width = box.Width ?? DialogWidth,
            Height = box.Height ?? Sizing.Fit(0f, MathF.Max(0f, ui.Height - DialogMargin * 2)),
            Padding = box.Padding ?? DialogPadding,
            Gap = box.Gap ?? theme.Gap * 1.5f,
            Background = box.Background != 0 ? box.Background : theme.Surface,
            BorderColor = box.BorderColor != 0 ? box.BorderColor : theme.Border,
            BorderWidth = box.BorderWidth > 0f ? box.BorderWidth : 1f,
            Radius = box.Radius > 0f ? box.Radius : DialogRadius,
        });

        using (ui.Row(new Box { Height = theme.ControlHeight, Gap = 0f }))
        {
            ui.Leaf(new Box { Width = DialogCloseSize, Height = 0f });
            ui.Text(title, new Box { Width = Sizing.Grow() }, bold: true, align: Align.Center);
            using (ui.Box(close.Id, new Box
            {
                Width = DialogCloseSize,
                Height = DialogCloseSize,
                Radius = 4f,
                Background = close.Held ? theme.Pressed(theme.Control) : close.Hovered ? theme.Control : 0u,
            }))
            {
                ui.Paint(close.Hovered ? theme.Text : theme.TextMuted, static (p, color) => DrawCross(p, color));
            }
        }
        ui.Leaf(new Box { Width = Sizing.Grow(), Height = 1f, Background = theme.Border });

        var body = ui.Scroll(HashCode.Combine(dialogId, "body"), new Box { Height = Sizing.Grow(), Gap = theme.Gap });
        return new DialogScope(modal, card, body);
    }
}
