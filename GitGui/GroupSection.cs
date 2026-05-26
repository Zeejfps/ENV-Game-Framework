using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;

namespace GitGui;

internal sealed class GroupSection : MultiChildView, IBind<GroupSectionViewModel>
{
    private readonly MultiChildView _headerSlot;
    private readonly FlexColumnView _rows;

    public GroupSection()
    {
        _headerSlot = new MultiChildView();
        _rows = new FlexColumnView
        {
            Gap = 2,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
        };

        AddChildToSelf(new FlexColumnView
        {
            Gap = 2,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                _headerSlot,
                _rows,
            }
        });
    }

    public void Bind(GroupSectionViewModel vm)
    {
        this.UseController(ctx => new GroupSectionController(this, ctx, vm.GroupId));
        _headerSlot.Children.Add(vm.CreateHeader());
        _rows.BindChildren(vm.VisiblePrimaries, vm.CreateRepoRow);
    }
}
