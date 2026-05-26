using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

internal sealed class GroupHeaderRowViewModel : IDisposable
{
    private readonly Group _group;
    private readonly IRepoRegistry _registry;
    private readonly Command _newGroup;
    private readonly Derived<bool> _isRenaming;

    public Group Group => _group;
    public IReadable<bool> IsRenaming => _isRenaming;

    public Command ToggleCollapsed { get; }
    public Command BeginRename { get; }
    public Command Delete { get; }
    public Command NewGroup => _newGroup;

    public GroupHeaderRowViewModel(Group group, IRepoRegistry registry, Command newGroup)
    {
        _group = group;
        _registry = registry;
        _newGroup = newGroup;
        _isRenaming = new Derived<bool>(() => _registry.RenamingGroupId.Value == _group.Id);

        ToggleCollapsed = new Command(() => _registry.ToggleGroupCollapsed(_group.Id));
        BeginRename = new Command(() => _registry.BeginRenameGroup(_group.Id));
        Delete = new Command(() => _registry.DeleteGroup(_group.Id));
    }

    public View CreateNameContent(bool isRenaming) =>
        isRenaming
            ? new GroupRenameField(_group, _registry)
            : new TextView
            {
                Text = _group.Name,
                TextColor = DialogPalette.SectionHeaderText,
                FontSize = 18f,
                HorizontalTextAlignment = TextAlignment.Start,
                VerticalTextAlignment = TextAlignment.Center,
            };

    public IReadOnlyList<RepoBarContextMenu.Item> BuildMenuItems()
    {
        var items = new List<RepoBarContextMenu.Item>
        {
            new("Rename group", BeginRename.Execute, LucideIcons.PencilLine),
        };

        if (_registry.Groups.Count > 1)
            items.Add(new RepoBarContextMenu.Item("Delete group", Delete.Execute, LucideIcons.Trash));

        items.Add(new RepoBarContextMenu.Item("New group", _newGroup.Execute, LucideIcons.FolderPlus));
        return items;
    }

    public void Dispose() => _isRenaming.Dispose();
}
