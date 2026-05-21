using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

// Small visual divider rendered under a primary RepoRow when it has BOTH worktree and
// submodule children, to group them by kind. Not interactive — no chevron, no hover, no
// menu. Sits at the same horizontal indent as a child row's text so it reads as a label
// over the rows that follow.
public sealed class ChildKindSubHeader : MultiChildView
{
    public ChildKindSubHeader(string title)
    {
        PreferredHeight = 18;

        var label = new TextView
        {
            Text = title,
            TextColor = DialogPalette.RowTextMissing,
            FontSize = 10,
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Center,
        };

        var leftPad = RepoBar.RowPaddingLeft
                      + RepoBar.RowChevronWidth
                      + RepoBar.RowIconGap
                      + RepoBar.WorktreeRowExtraIndent;

        AddChildToSelf(new RectView
        {
            Padding = new PaddingStyle { Left = leftPad, Right = 12 },
            Children = { label },
        });
    }
}
