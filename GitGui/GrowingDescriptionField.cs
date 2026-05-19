using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;

namespace GitGui;

/// <summary>
/// A multi-line text input that auto-grows with its content between <c>min</c> and <c>max</c>.
/// Once content exceeds <c>max</c>, the field caps at that height and a vertical scroll bar
/// is shown so the rest is reachable by scrolling.
///
/// The desired height is recomputed in <see cref="OnLayoutChildren"/> (after the inner input
/// has been laid out — at which point its width is known and its <c>MeasureHeight</c> is
/// reliable) and stored as a <c>PreferredHeight</c>. The next layout pass reads that as the
/// desired size. This avoids the "measure before width is known" problem that would otherwise
/// make the field report a runaway height (every char treated as its own wrapped line).
/// </summary>
internal sealed class GrowingDescriptionField : MultiChildView
{
    private const float BoxBorderThickness = 1f;
    private const float BoxPaddingHorizontal = 6f;
    private const float BoxPaddingVertical = 4f;

    private readonly float _minHeight;
    private readonly float _maxHeight;

    private readonly TextInputView _input;
    private readonly ScrollPane _scrollPane;
    private readonly VerticalScrollBarView _scrollBar;

    public string? PlaceholderText
    {
        get => _input.PlaceholderText;
        set => _input.PlaceholderText = value;
    }

    public ReadOnlySpan<char> Text => _input.Text;

    public event Action? TextChanged
    {
        add => _input.TextChanged += value;
        remove => _input.TextChanged -= value;
    }

    public void Clear() => _input.Clear();

    public void SetText(ReadOnlySpan<char> text)
    {
        _input.Clear();
        if (text.Length > 0) _input.Enter(text);
    }

    public GrowingDescriptionField(float minHeight, float maxHeight)
    {
        _minHeight = minHeight;
        _maxHeight = maxHeight;

        _input = new TextInputView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            TextColor = DialogPalette.TitleText,
            CaretColor = DialogPalette.TitleText,
            SelectionRectColor = DialogPalette.RowActive,
            TextVerticalAlignment = TextAlignment.Start,
            TextWrap = TextWrap.Wrap,
            PlaceholderTextColor = DialogPalette.RowTextMissing,
        };
        _input.UseController(_ => new TextInputViewKbmController(_input) { IsMultiLine = true });

        _scrollPane = new ScrollPane();
        _scrollPane.Children.Add(_input);
        _scrollPane.UseController(_ => new ScrollPaneWheelController(_scrollPane));

        _scrollBar = ScrollBarStyles.CreateVertical();

        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            BorderColor = BorderColorStyle.All(DialogPalette.ButtonBorder),
            BorderSize = BorderSizeStyle.All((int)BoxBorderThickness),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle
            {
                Left = (int)BoxPaddingHorizontal,
                Right = (int)BoxPaddingHorizontal,
                Top = (int)BoxPaddingVertical,
                Bottom = (int)BoxPaddingVertical,
            },
            Children =
            {
                new BorderLayoutView
                {
                    Center = _scrollPane,
                    East = _scrollBar,
                },
            },
        });

        this.UseController(_ => new ScrollSyncController(_scrollPane, _scrollBar));

        // Start at the min size; the first OnLayoutChildren pass will refine this.
        PreferredHeight = _minHeight;
    }

    protected override void OnLayoutChildren()
    {
        base.OnLayoutChildren();

        // Now that the input has been laid out, its MaxWidthConstraint reflects the actual
        // viewport width — so its MeasureHeight is reliable. Cache the clamped desired height
        // as PreferredHeight; the next layout pass will pick it up.
        var chrome = 2f * (BoxBorderThickness + BoxPaddingVertical);
        var contentHeight = _input.MeasureHeight();
        var desired = Math.Clamp(contentHeight + chrome, _minHeight, _maxHeight);
        if (Math.Abs(desired - PreferredHeight) > 0.5f)
        {
            // Setting PreferredHeight via SetField marks us IsSelfDirty, so the next frame's
            // layout re-runs OnLayoutSelf with the new value.
            PreferredHeight = desired;
        }
    }
}