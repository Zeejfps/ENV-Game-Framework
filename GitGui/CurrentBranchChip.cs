using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class CurrentBranchChip : MultiChildView
{
    private const float ChipHeight = 28f;

    private readonly TextView _prefixView;
    private readonly TextView _nameView;

    public CurrentBranchChip()
    {
        PreferredHeight = ChipHeight;

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
            VerticalTextAlignment = TextAlignment.Center,
        };

        AddChildToSelf(new PaddingView
        {
            Padding = new PaddingStyle { Left = 6, Right = 6 },
            Children =
            {
                new RowView
                {
                    Gap = 5,
                    Children = { _prefixView, _nameView },
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
            _nameView.TextColor = value ? DialogPalette.RowTextMissing : Theme.TextStrong;
        }
    }
}
