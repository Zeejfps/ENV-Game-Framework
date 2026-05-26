using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

internal sealed class BranchesHeader : MultiChildView, IBind<BranchesHeaderViewModel>
{
    private const float HeaderHeight = 44f;
    private const int HorizontalPadding = 8;

    private readonly CurrentBranchChip _chip;

    public BranchesHeader()
    {
        PreferredHeight = HeaderHeight;

        _chip = new CurrentBranchChip();

        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.Background,
            BorderColor = new BorderColorStyle { Bottom = DialogPalette.Border },
            BorderSize = new BorderSizeStyle { Bottom = 1 },
            Padding = new PaddingStyle { Left = HorizontalPadding, Right = HorizontalPadding },
            Children =
            {
                new FlexRowView
                {
                    CrossAxisAlignment = CrossAxisAlignment.Center,
                    Children = { _chip },
                },
            },
        });

        this.UseViewModel(this);
    }

    public void Bind(BranchesHeaderViewModel vm)
    {
        _chip.BranchName.BindTo(vm.BranchName);
        _chip.IsDetached.BindTo(vm.IsDetached);
        _chip.BindIsVisible(vm.BranchName, n => !string.IsNullOrEmpty(n));
    }
}
