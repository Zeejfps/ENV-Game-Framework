using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

internal sealed class OperationRow : HoverableButton
{
    // Semantic phase-color state: the override kind determines which token to project on
    // every theme tick, so a Mark*-followed-by-F12 reads the *new* theme's WarningText
    // rather than freezing on the old one.
    private enum PhaseColor { Default, Success, Failure }

    private readonly TextView _label;
    private readonly TextView _phase;
    private readonly TextView _elapsed;
    private readonly ProgressBarView _bar;
    private readonly RectView _background;
    private PhaseColor _phaseColor = PhaseColor.Default;

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
        _phase.BindTextColorFromTheme(t => _phaseColor switch
        {
            PhaseColor.Success => 0xFF7FB76A,        // semantic accent — green stays green
            PhaseColor.Failure => t.Commits.WarningText,
            _ => t.Text.Dim,
        });

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
        SetPhaseColor(PhaseColor.Success);
    }

    public void MarkFailure(string? message)
    {
        _bar.FillColor = 0xFFB3514B;
        _phase.Text = string.IsNullOrEmpty(message) ? "Failed" : message;
        SetPhaseColor(PhaseColor.Failure);
    }

    private void SetPhaseColor(PhaseColor kind)
    {
        _phaseColor = kind;
        // The Derived behind BindTextColorFromTheme only auto-tracks observable reads.
        // _phaseColor is a plain field, so we have to force a refresh.
        var ctx = Context;
        var tokens = ctx?.Get<IThemeService>()?.Tokens.Value;
        if (tokens != null)
        {
            _phase.TextColor = kind switch
            {
                PhaseColor.Success => 0xFF7FB76A,
                PhaseColor.Failure => tokens.Commits.WarningText,
                _ => tokens.Text.Dim,
            };
        }
    }
}
