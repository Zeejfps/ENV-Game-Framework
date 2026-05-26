using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

internal sealed class OperationRow : HoverableButton
{
    private readonly TextView _label;
    private readonly TextView _phase;
    private readonly TextView _elapsed;
    private readonly ProgressBarView _bar;
    private readonly RectView _background;
    private ThemeTokens _tokens = ThemePresets.Dark;
    private bool _phaseColorOverridden;

    public OperationRow(string label, string icon, Action onToggleLog)
        : base(onToggleLog)
    {
        var iconView = new TextView
        {
            Text = icon,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 14,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            PreferredWidth = 18,
        };
        iconView.BindTextColorFromTheme(t => t.Text.Primary);

        _label = new TextView { Text = label, VerticalTextAlignment = TextAlignment.Center };
        _label.BindTextColorFromTheme(t => t.Text.Primary);

        _phase = new TextView { Text = string.Empty, VerticalTextAlignment = TextAlignment.Center };
        _phase.BindTextColorFromTheme(t => _phaseColorOverridden ? _phase.TextColor : t.Text.Dim);

        _bar = new ProgressBarView { PreferredWidth = 120f };

        _elapsed = new TextView
        {
            Text = "0s",
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.End,
            PreferredWidth = 36,
        };
        _elapsed.BindTextColorFromTheme(t => t.Text.Dim);

        var hoverBg = new State<uint>(ThemePresets.Dark.Surfaces.Border);
        var idleBg = new State<uint>(ThemePresets.Dark.Surfaces.BgHeader);

        _background = new RectView
        {
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
        _background.BindToTheme(t =>
        {
            _tokens = t;
            hoverBg.Value = t.Surfaces.Border;
            idleBg.Value = t.Surfaces.BgHeader;
        });
        _background.BindBackgroundColor(() => IsHovered.Value ? hoverBg.Value : idleBg.Value);
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
        _phaseColorOverridden = true;
    }

    public void MarkFailure(string? message)
    {
        _bar.FillColor = 0xFFB3514B;
        _phase.Text = string.IsNullOrEmpty(message) ? "Failed" : message;
        _phase.TextColor = _tokens.Commits.WarningText;
        _phaseColorOverridden = true;
    }
}
