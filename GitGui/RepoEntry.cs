using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;

namespace GitGui;

// Composite that renders a single primary repo with its (collapsible) child rows
// (worktrees + submodules) stacked below. Used as the row factory of GroupSection so
// the existing group-level list binding doesn't need to know about nesting.
//
// Child layout when both kinds exist:
//   [primary]
//   [Worktrees sub-header]
//     [worktree row...]
//   [Submodules sub-header]
//     [submodule row...]
// Sub-headers are hidden when only one kind is present — the icon alone is enough to
// distinguish a single category and the extra row would add visual noise.
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
                    return System.Linq.Enumerable.Empty<View>();

                var worktrees = new List<Repo>();
                var submodules = new List<Repo>();
                foreach (var r in registry.Repos)
                {
                    if (r.ParentRepoId != primary.Id) continue;
                    if (r.IsWorktree) worktrees.Add(r);
                    else if (r.IsSubmodule) submodules.Add(r);
                }

                if (worktrees.Count == 0 && submodules.Count == 0)
                    return System.Linq.Enumerable.Empty<View>();

                var both = worktrees.Count > 0 && submodules.Count > 0;
                var views = new List<View>();
                if (worktrees.Count > 0)
                {
                    if (both) views.Add(new ChildKindSubHeader("Worktrees"));
                    foreach (var wt in worktrees) views.Add(new WorktreeRow(wt, registry));
                }
                if (submodules.Count > 0)
                {
                    if (both) views.Add(new ChildKindSubHeader("Submodules"));
                    foreach (var sm in submodules) views.Add(new SubmoduleRow(sm, registry));
                }
                return views;
            },
            v => v);

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
