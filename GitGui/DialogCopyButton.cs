using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Icon-only "copy to clipboard" button styled to match <see cref="DialogCloseButton"/>.
/// On click, runs <paramref name="onCopy"/> and momentarily swaps the icon to a checkmark
/// + the tooltip to "Copied!" so the user gets feedback without needing to paste to verify.
/// </summary>
public sealed class DialogCopyButton : HoverableButton
{
    private const int FeedbackMs = 1200;

    private readonly TextView _label;
    private int _feedbackGen;

    private readonly Action _onCopy;

    public DialogCopyButton(Action onCopy, string tooltip = "Copy")
        : base(null, tooltip)
    {
        _onCopy = onCopy;
        PreferredWidth = 28;
        PreferredHeight = 28;

        var normalText = new State<uint>(ThemePresets.Dark.Dialog.CloseTextNormal);
        var hoverText = new State<uint>(ThemePresets.Dark.Dialog.CloseTextHover);
        var normalBg = new State<uint>(ThemePresets.Dark.Dialog.CloseNormal);
        var hoverBg = new State<uint>(ThemePresets.Dark.Dialog.CloseHover);
        this.BindToTheme(t =>
        {
            normalText.Value = t.Dialog.CloseTextNormal;
            hoverText.Value = t.Dialog.CloseTextHover;
            normalBg.Value = t.Dialog.CloseNormal;
            hoverBg.Value = t.Dialog.CloseHover;
        });

        _label = new TextView
        {
            Text = LucideIcons.Copy,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 14,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _label.BindTextColor(() => IsHovered.Value ? hoverText.Value : normalText.Value);

        var background = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(4),
            Children = { _label }
        };
        background.BindBackgroundColor(() => IsHovered.Value ? hoverBg.Value : normalBg.Value);

        SetBackground(background);
    }

    protected override void OnClicked()
    {
        _onCopy();
        ShowFeedback();
    }

    private void ShowFeedback()
    {
        // Bump generation so a rapid second click doesn't get reverted by the first
        // click's pending Task — only the most recent click controls the final state.
        var gen = ++_feedbackGen;
        _label.Text = LucideIcons.CheckSquare;
        var dispatcher = Context?.Get<ZGF.Observable.IUiDispatcher>();
        Task.Run(async () =>
        {
            await Task.Delay(FeedbackMs);
            if (dispatcher != null)
                dispatcher.Post(() => { if (gen == _feedbackGen) _label.Text = LucideIcons.Copy; });
        });
    }
}
