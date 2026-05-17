using ZGF.Gui;

namespace GitGui;

/// <summary>
/// Sidebar (West region of the app shell) for local branches, remotes, tags, and stashes.
/// Placeholder content for now — the tree of refs is filled in later.
/// </summary>
public sealed class BranchesView : MultiChildView
{
    private const float BarWidth = 220f;

    public BranchesView()
    {
        PreferredWidth = BarWidth;

        var placeholder = new TextView
        {
            Text = "Branches & Remotes",
            TextColor = CommitsPalette.Placeholder,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        AddChildToSelf(new RectView
        {
            BackgroundColor = CommitsPalette.Background,
            BorderColor = new BorderColorStyle { Right = CommitsPalette.Border },
            BorderSize = new BorderSizeStyle { Right = 1 },
            Children = { placeholder },
        });
    }
}
