using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;

namespace GitGui;

/// <summary>
/// One row in the <see cref="OperationsStatusBar"/>. Shows the op's icon, label,
/// current phase, a determinate progress bar, and elapsed time. The whole row is a
/// HoverableButton so clicking toggles the log popover for this op via the supplied
/// onToggleLog callback.
/// </summary>
internal sealed class OperationRow : HoverableButton
{
    private readonly TextView _label;
    private readonly TextView _phase;
    private readonly TextView _elapsed;
    private readonly ProgressBarView _bar;
    private readonly RectView _background;

    public OperationRow(string label, string icon, Action onToggleLog)
        : base(onToggleLog)
    {
        var iconView = new TextView
        {
            Text = icon,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 14,
            TextColor = Theme.TextPrimary,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            PreferredWidth = 18,
        };

        _label = new TextView
        {
            Text = label,
            TextColor = Theme.TextPrimary,
            VerticalTextAlignment = TextAlignment.Center,
        };

        _phase = new TextView
        {
            Text = string.Empty,
            TextColor = Theme.TextDim,
            VerticalTextAlignment = TextAlignment.Center,
        };

        _bar = new ProgressBarView { PreferredWidth = 120f };

        _elapsed = new TextView
        {
            Text = "0s",
            TextColor = Theme.TextDim,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.End,
            PreferredWidth = 36,
        };

        _background = new RectView
        {
            BackgroundColor = Theme.BgHeader,
            Padding = new PaddingStyle { Left = 12, Right = 12, Top = 6, Bottom = 6 },
            Children =
            {
                new FlexRowView
                {
                    Gap = 8,
                    CrossAxisAlignment = CrossAxisAlignment.Center,
                    Children =
                    {
                        iconView,
                        new FlexItem { Grow = 1, Child = _label },
                        _phase,
                        _bar,
                        _elapsed,
                    },
                },
            },
        };
        _background.BindBackgroundColor(IsHovered, h => h ? Theme.Border : Theme.BgHeader);
        SetBackground(_background);
    }

    public string Phase { set => _phase.Text = value ?? string.Empty; }
    public float Percent { set => _bar.Percent = value; }
    public string Elapsed { set => _elapsed.Text = value; }

    public void MarkSuccess()
    {
        _bar.Percent = 1f;
        _bar.FillColor = 0xFF4E8B3D;
        _phase.Text = "Done";
        _phase.TextColor = 0xFF7FB76A;
    }

    public void MarkFailure(string? message)
    {
        _bar.FillColor = 0xFFB3514B;
        _phase.Text = string.IsNullOrEmpty(message) ? "Failed" : message;
        _phase.TextColor = CommitsPalette.WarningText;
    }
}
