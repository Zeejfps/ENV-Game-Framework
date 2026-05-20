using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

/// <summary>
/// Read-only status text in the actions toolbar showing the currently checked-out
/// ref. Renders as "on <name>" — plain text with no background, border, or icon.
/// The deliberate *absence* of a button shape is what makes the eye read this as
/// ambient state rather than another control; the dim "on" prefix sets the bright
/// branch name up as the part that pops. The Branch button next door owns the verb;
/// this slot is here so the user can always see which ref the push/pull buttons are
/// about to act on without having to scan the sidebar.
/// </summary>
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
            // Modest side padding — the parent FlexRow's gap supplies most of the breathing
            // room. We want "on master" to feel tucked up against the mode switcher to its
            // left so the two read as a single "current view / current branch" status zone.
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

    // null/empty hides the chip's contents (caller is expected to also yank it from its
    // parent if it wants the chip's slot to collapse — see ActionsToolbar). A non-null
    // value renders as either the branch name or the "(detached HEAD)" placeholder,
    // depending on IsDetached.
    public string? BranchName
    {
        set => _nameView.Text = value ?? string.Empty;
    }

    public bool IsDetached
    {
        set
        {
            // "on (detached HEAD)" reads weird — switch the prefix to "at" so the sentence
            // works either way. Colour the name dim to match the rest of the detached
            // visual vocabulary in the sidebar.
            _prefixView.Text = value ? "at" : "on";
            _nameView.TextColor = value ? DialogPalette.RowTextMissing : Theme.TextStrong;
        }
    }
}
