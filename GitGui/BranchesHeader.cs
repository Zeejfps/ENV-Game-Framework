using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

internal sealed class BranchesHeader : MultiChildView, IBind<BranchesHeaderViewModel>
{
    private const float HeaderHeight = 44f;
    private const int HorizontalPadding = 8;

    private readonly CurrentBranchChip _chip;
    private readonly FlexRowView _row;

    public BranchesHeader()
    {
        PreferredHeight = HeaderHeight;

        _chip = new CurrentBranchChip();
        _row = new FlexRowView
        {
            CrossAxisAlignment = CrossAxisAlignment.Center,
        };

        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.Background,
            BorderColor = new BorderColorStyle { Bottom = DialogPalette.Border },
            BorderSize = new BorderSizeStyle { Bottom = 1 },
            Padding = new PaddingStyle { Left = HorizontalPadding, Right = HorizontalPadding },
            Children = { _row },
        });

        this.UseViewModel(this);
    }

    public void Bind(BranchesHeaderViewModel vm)
    {
        vm.Snapshot.Subscribe(s => SetBranch(s.BranchName, s.IsDetached));
    }

    private void SetBranch(string? name, bool isDetached)
    {
        var attached = _row.Children.Contains(_chip);
        if (string.IsNullOrEmpty(name))
        {
            if (attached) _row.Children.Remove(_chip);
            return;
        }
        _chip.BranchName = name;
        _chip.IsDetached = isDetached;
        if (!attached) _row.Children.Add(_chip);
    }
}
