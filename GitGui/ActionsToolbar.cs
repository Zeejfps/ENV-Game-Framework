using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class ActionsToolbar : MultiChildView
{
    private const float ToolbarHeight = 44f;
    private const int HorizontalPadding = 8;
    private const float GroupGap = 4f;
    private const float SeparatorBreathingRoom = 8f;
    private const float SeparatorWidth = 1f;
    private const float SeparatorHeight = 16f;

    public ActionsToolbar()
    {
        PreferredHeight = ToolbarHeight;

        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.Background,
            BorderColor = new BorderColorStyle { Bottom = DialogPalette.Border },
            BorderSize = new BorderSizeStyle { Bottom = 1 },
            Padding = new PaddingStyle
            {
                Left = HorizontalPadding,
                Right = HorizontalPadding,
            },
            Children =
            {
                new FlexRowView
                {
                    Gap = GroupGap,
                    CrossAxisAlignment = CrossAxisAlignment.Center,
                    Children =
                    {
                        new ModeSwitcherView(),
                        //new FlexItem { Grow = 1, Child = new RectView() },
                        new ActionButton(LucideIcons.Fetch, "Fetch", () => { }),
                        new ActionButton(LucideIcons.Pull, "Pull", () => { }),
                        new ActionButton(LucideIcons.Push, "Push", () => { }),
                        new SeparatorSpacer(),
                        new ActionButton(LucideIcons.Stash, "Stash", () => { }),
                        new ActionButton(LucideIcons.Branch, "Branch", () => { }),
                    }
                }
            }
        });
    }

    private sealed class SeparatorSpacer : MultiChildView
    {
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
}
