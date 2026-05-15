using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class GroupSection : MultiChildView
{
    private readonly GroupHeaderRow _header;
    private readonly FlexColumnView _rows;

    public GroupSection(Group group)
    {
        _header = new GroupHeaderRow(group);
        _rows = new FlexColumnView
        {
            Gap = 2,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
        };
        AddChildToSelf(new FlexColumnView
        {
            Gap = 2,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children = { _header, _rows }
        });
    }

    public void Update(Group group, Guid? activeId, Dictionary<Guid, Repo> reposById)
    {
        _header.Update(group);
        _rows.Children.Clear();
        foreach (var repoId in group.RepoIds)
        {
            if (!reposById.TryGetValue(repoId, out var repo)) continue;
            var isActive = repo.Id == activeId;
            if (group.IsCollapsed && !isActive) continue;
            _rows.Children.Add(new RepoRow(repo, isActive));
        }
    }
}