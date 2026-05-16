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
            create: repo => new RepoRow(repo, registry));

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
        var reposById = registry.Repos.ToDictionary(r => r.Id);

        if (group.IsCollapsed)
        {
            // Only show the active repo when collapsed. Reading Active here registers it
            // as a dep so the list re-seeds when the active selection changes.
            var activeId = registry.Active.Value?.Id;
            foreach (var repoId in group.RepoIds)
            {
                if (reposById.TryGetValue(repoId, out var repo) && repo.Id == activeId)
                    yield return repo;
            }
            yield break;
        }

        // Not collapsed: list contents are independent of Active. Don't read Active —
        // that way Active changes don't trigger a list re-seed; each RepoRow's own
        // BindBackgroundColor / BindTextColor handles the highlight update reactively.
        foreach (var repoId in group.RepoIds)
        {
            if (reposById.TryGetValue(repoId, out var repo))
                yield return repo;
        }
    }
}
