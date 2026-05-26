using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

/// <summary>
/// Short vertical hairline with breathing room on either side — used to mark zone
/// boundaries inside a button toolbar. The contrast between tight intra-cluster gaps and
/// this wider separator block is what creates the visual grouping; uniform gaps would
/// make the toolbar read as one long row regardless of how many separators we drew.
/// </summary>
internal sealed class SeparatorSpacer : MultiChildView
{
    private const float SeparatorWidth = 1f;
    private const float SeparatorBreathingRoom = 9f;
    private const float SeparatorHeight = 18f;

    public SeparatorSpacer()
    {
        PreferredWidth = SeparatorWidth + SeparatorBreathingRoom * 2;
        AddChildToSelf(new FlexRowView
        {
            CrossAxisAlignment = CrossAxisAlignment.Center,
            MainAxisAlignment = MainAxisAlignment.Center,
            Children =
            {
                new RectView
                {
                    BackgroundColor = DialogPalette.Border,
                    PreferredWidth = SeparatorWidth,
                    PreferredHeight = SeparatorHeight,
                },
            },
        });
    }
}
