using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.KeyboardModule;

namespace GitGui;

/// <summary>
/// One row in the CommitDetails change list for a submodule pointer change. Renders
/// "&lt;path&gt; · abc..def (+5 commits)" with an "S" status badge. Clicking activates
/// the submodule's repo (if it's been initialized locally) and broadcasts a
/// JumpToSubmoduleCommitMessage so listeners can scroll history to the range.
/// </summary>
public sealed class SubmodulePointerRowView : MultiChildView
{
    public SubmodulePointerRowView(FileChange file)
    {
        var pc = file.PointerChange;
        var text = pc is null
            ? file.Path
            : $"{file.Path}  ·  {ShortSha(pc.FromSha)}..{ShortSha(pc.ToSha)}{RangeSummary(pc)}";

        var pathView = new TextView { Text = text };
        pathView.BindThemedTextColor(s => s.FileChangeRow.RowText);

        // Inset matches SelectableFileRow's internal padding so submodule rows align with
        // the content of selectable rows above/below them in the list.
        AddChildToSelf(new PaddingView
        {
            Padding = new PaddingStyle { Left = 14, Right = 14, Top = 2, Bottom = 2 },
            Children =
            {
                new FlexRowView
                {
                    Gap = 8f,
                    CrossAxisAlignment = CrossAxisAlignment.Start,
                    Children =
                    {
                        FileChangesUI.CreateStatusBadge(file),
                        new FlexItem { Grow = 1, Child = pathView },
                    },
                },
            },
        });

        if (pc is not null)
            this.UseController(ctx => new SubmodulePointerRowController(ctx, file.Path, pc));
    }

    private static string ShortSha(string sha)
    {
        if (string.IsNullOrEmpty(sha)) return "·";
        if (IsAllZeros(sha)) return "(none)";
        return sha.Length >= 7 ? sha.Substring(0, 7) : sha;
    }

    private static string RangeSummary(SubmodulePointerChange pc)
    {
        // The submodule isn't initialized locally → we couldn't compute counts. Leave the
        // summary blank rather than print misleading "(+0)".
        if (pc.AheadCount == 0 && pc.BehindCount == 0) return string.Empty;
        if (pc.AheadCount > 0 && pc.BehindCount == 0) return $"  (+{pc.AheadCount})";
        if (pc.BehindCount > 0 && pc.AheadCount == 0) return $"  (-{pc.BehindCount})";
        return $"  (+{pc.AheadCount}/-{pc.BehindCount})";
    }

    private static bool IsAllZeros(string s)
    {
        for (var i = 0; i < s.Length; i++)
            if (s[i] != '0') return false;
        return s.Length > 0;
    }
}

internal sealed class SubmodulePointerRowController : KeyboardMouseController
{
    private readonly Context _context;
    private readonly string _submodulePath;
    private readonly SubmodulePointerChange _change;

    public SubmodulePointerRowController(Context context, string submodulePath, SubmodulePointerChange change)
    {
        _context = context;
        _submodulePath = submodulePath;
        _change = change;
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling) return;
        if (e.Button != MouseButton.Left || e.State != InputState.Released) return;

        var registry = _context.Get<IRepoRegistry>();
        var bus = _context.Get<IMessageBus>();
        if (registry is null) return;

        // Find the submodule Repo registered under whichever primary the active repo
        // belongs to. We compare by relative path because that's the stable identity
        // (the submodule's absolute path can vary across worktrees).
        var active = registry.Active.Value;
        if (active is null) return;
        var primaryId = active.IsPrimary ? active.Id : (active.ParentRepoId ?? active.Id);
        var matchAbs = System.IO.Path.GetFullPath(System.IO.Path.Combine(active.IsPrimary ? active.Path : FindParentPath(registry, primaryId) ?? active.Path, _submodulePath));

        foreach (var r in registry.GetSubmodules(primaryId))
        {
            if (string.Equals(System.IO.Path.GetFullPath(r.Path), matchAbs, PathCmp))
            {
                if (!r.IsMissing) registry.SetActive(r.Id);
                bus?.Broadcast(new JumpToSubmoduleCommitMessage(r.Id, _change.FromSha, _change.ToSha));
                e.Consume();
                return;
            }
        }
    }

    private static string? FindParentPath(IRepoRegistry registry, Guid primaryId)
    {
        foreach (var r in registry.Repos)
            if (r.Id == primaryId) return r.Path;
        return null;
    }

    private static readonly StringComparison PathCmp =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
