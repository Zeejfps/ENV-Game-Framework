using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class CurrentBranchChip : MultiChildView
{
    private const float ChipHeight = 28f;

    public State<string?> BranchName { get; } = new(null);
    public State<bool> IsDetached { get; } = new(false);

    public CurrentBranchChip()
    {
        PreferredHeight = ChipHeight;

        var iconView = new TextView
        {
            Text = LucideIcons.Branch,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 15f,
            VerticalTextAlignment = TextAlignment.Center,
        };
        iconView.BindTextColor(IsDetached, d => d ? DialogPalette.RowTextMissing : Theme.TextStrong);

        var prefixView = new TextView
        {
            TextColor = Theme.TextHeader,
            VerticalTextAlignment = TextAlignment.Center,
        };
        prefixView.BindText(IsDetached, d => d ? "at" : "on");

        var nameView = new TextView
        {
            FontSize = 18f,
            FontWeight = FontWeight.Bold,
            VerticalTextAlignment = TextAlignment.Center,
        };
        nameView.BindText(BranchName);
        nameView.BindTextColor(IsDetached, d => d ? DialogPalette.RowTextMissing : Theme.TextStrong);

        AddChildToSelf(new PaddingView
        {
            Padding = new PaddingStyle { Left = 6, Right = 6 },
            Children =
            {
                new RowView
                {
                    Gap = 6,
                    Children = { iconView, prefixView, nameView },
                },
            },
        });
    }
}
