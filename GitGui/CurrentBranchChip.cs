using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class CurrentBranchChip : MultiChildView
{
    private const float ChipHeight = 28f;

    private readonly TextView _iconView;
    private readonly TextView _prefixView;
    private readonly TextView _nameView;

    public CurrentBranchChip()
    {
        PreferredHeight = ChipHeight;

        _iconView = new TextView
        {
            Text = LucideIcons.Branch,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 15f,
            TextColor = Theme.TextStrong,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _prefixView = new TextView
        {
            Text = "on",
            TextColor = Theme.TextHeader,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _nameView = new TextView
        {
            Text = string.Empty,
            TextColor = Theme.TextStrong,
            FontSize = 18f,
            FontWeight = FontWeight.Bold,
            VerticalTextAlignment = TextAlignment.Center,
        };

        AddChildToSelf(new PaddingView
        {
            Padding = new PaddingStyle { Left = 6, Right = 6 },
            Children =
            {
                new RowView
                {
                    Gap = 6,
                    Children = { _iconView, _prefixView, _nameView },
                },
            },
        });
    }

    public string? BranchName
    {
        set => _nameView.Text = value ?? string.Empty;
    }

    public bool IsDetached
    {
        set
        {
            _prefixView.Text = value ? "at" : "on";
            var nameColor = value ? DialogPalette.RowTextMissing : Theme.TextStrong;
            _nameView.TextColor = nameColor;
            _iconView.TextColor = nameColor;
        }
    }
}
