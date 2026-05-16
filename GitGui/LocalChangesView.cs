using ZGF.Gui;

namespace GitGui;

public sealed class LocalChangesView : MultiChildView
{
    public LocalChangesView()
    {
        AddChildToSelf(new RectView
        {
            BackgroundColor = CommitsPalette.Background,
            Children =
            {
                new TextView
                {
                    Text = "Local Changes — coming soon",
                    TextColor = CommitsPalette.Placeholder,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                },
            },
        });
    }
}
