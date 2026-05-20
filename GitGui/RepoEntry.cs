using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;

namespace GitGui;

// Composite that renders a single primary repo with its (collapsible) worktree
// children stacked below. Used as the row factory of GroupSection so the existing
// group-level list binding doesn't need to know about nesting.
public sealed class RepoEntry : MultiChildView
{
    public RepoEntry(Repo primary, IRepoRegistry registry)
    {
        var children = new FlexColumnView
        {
            Gap = 2,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
        };

        children.BindChildren(
            () =>
            {
                _ = registry.WorktreesChanged.Value;
                if (!registry.IsWorktreeExpanded(primary.Id))
                    return System.Linq.Enumerable.Empty<Repo>();
                var list = new List<Repo>();
                foreach (var r in registry.Repos)
                {
                    if (r.ParentRepoId == primary.Id) list.Add(r);
                }
                return list;
            },
            wt => new WorktreeRow(wt, registry));

        AddChildToSelf(new FlexColumnView
        {
            Gap = 2,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                new RepoRow(primary, registry),
                children,
            }
        });
    }
}
