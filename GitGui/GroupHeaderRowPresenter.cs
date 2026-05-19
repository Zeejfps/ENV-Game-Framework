using ZGF.Gui;

namespace GitGui;

internal sealed class GroupHeaderRowPresenter : IDisposable
{
    private readonly IGroupHeaderRowView _view;
    private readonly Group _group;
    private readonly IRepoRegistry _registry;

    public GroupHeaderRowPresenter(IGroupHeaderRowView view, Group group, IRepoRegistry registry)
    {
        _view = view;
        _group = group;
        _registry = registry;

        _view.ToggleCollapsedRequested += OnToggleCollapsedRequested;
        _view.MenuItemsProvider = BuildMenuItems;
        _view.IsRenamingProvider = () => _registry.RenamingGroupId.Value == _group.Id;
        _view.BindName(
            () => new[] { _registry.RenamingGroupId.Value == _group.Id },
            isRenaming => isRenaming
                ? new GroupRenameField(_group, _registry)
                : new TextView
                {
                    Text = _group.Name,
                    TextColor = DialogPalette.SectionHeaderText,
                    FontSize = 18f,
                    HorizontalTextAlignment = TextAlignment.Start,
                    VerticalTextAlignment = TextAlignment.Center,
                });
    }

    public void Dispose()
    {
        _view.ToggleCollapsedRequested -= OnToggleCollapsedRequested;
    }

    private void OnToggleCollapsedRequested()
    {
        _registry.ToggleGroupCollapsed(_group.Id);
    }

    private IReadOnlyList<RepoBarContextMenu.Item> BuildMenuItems()
    {
        var items = new List<RepoBarContextMenu.Item>
        {
            new("Rename group", () => _registry.BeginRenameGroup(_group.Id)),
        };

        if (_registry.Groups.Count > 1)
        {
            items.Add(new RepoBarContextMenu.Item("Delete group", () => _registry.DeleteGroup(_group.Id)));
        }

        items.Add(new RepoBarContextMenu.Item("New group", () =>
        {
            var id = _registry.CreateGroup("New Group");
            _registry.BeginRenameGroup(id);
        }));

        return items;
    }
}
