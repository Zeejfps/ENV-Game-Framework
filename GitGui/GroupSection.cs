using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class GroupSection : MultiChildView, IGroupSectionView
{
    private readonly MultiChildView _headerSlot;
    private readonly FlexColumnView _rows;

    public GroupSection(Group group)
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

        this.UseController(ctx => new GroupSectionController(this, ctx, group.Id));
        this.UsePresenter(ctx => new GroupSectionPresenter(this, group, ctx.Require<IRepoRegistry>()));
    }

    public void SetHeader(View header)
    {
        _headerSlot.Children.Clear();
        _headerSlot.Children.Add(header);
    }

    public void BindRows(Func<IEnumerable<Repo>> compute, Func<Repo, View> rowFactory)
    {
        _rows.BindChildren(compute, rowFactory);
    }
}
