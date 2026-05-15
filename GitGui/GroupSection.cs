using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class GroupSection : MultiChildView
{
    public GroupSection(Group group, IRepoRegistry registry)
    {
        var rows = new FlexColumnView
        {
            Gap = 2,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
        };
        rows.BindChildren(
            compute: () => VisibleRepos(group, registry),
            create: repo => new RepoRow(repo, repo.Id == registry.Active.Value?.Id, registry));

        AddChildToSelf(new FlexColumnView
        {
            Gap = 2,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                new GroupHeaderRow(group, registry),
                rows,
            }
        });
    }

    private static IEnumerable<Repo> VisibleRepos(Group group, IRepoRegistry registry)
    {
        var activeId = registry.Active.Value?.Id;
        var reposById = registry.Repos.ToDictionary(r => r.Id);
        foreach (var repoId in group.RepoIds)
        {
            if (!reposById.TryGetValue(repoId, out var repo)) continue;
            if (group.IsCollapsed && repo.Id != activeId) continue;
            yield return repo;
        }
    }
}
