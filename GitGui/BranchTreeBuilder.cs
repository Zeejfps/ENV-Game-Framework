namespace GitGui;

/// <summary>
/// Pure flattening of a <see cref="BranchListing"/> plus collapse/expand state into the
/// linear <see cref="BranchRow"/> sequence that <see cref="BranchesView"/> renders.
/// Branch names containing "/" become folder nodes (e.g. "feature/login" lives inside
/// a "feature" folder). Lives outside the view because none of this depends on layout
/// pixels, the canvas, or the view tree — it's a deterministic function of
/// (listing, ui-state).
/// </summary>
internal static class BranchTreeBuilder
{
    // Section headers ("Local" / "Remote" / "Stashes") and the per-row indent math are
    // render concerns the VM produces no input for beyond the raw listing and the
    // open/closed flags, so the constants live with the builder rather than the VM.
    private const float IndentLevel = TreeMetrics.IndentLevel;
    private const float IndentSection = 0f;                  // section header (Local / Remote / Stashes)
    private const float IndentRemoteHeader = IndentLevel;    // one level under the Remote section
    private const float IndentLocalTreeBase = IndentLevel;   // local branches: one level under their section
    private const float IndentRemoteTreeBase = IndentLevel * 2; // remote branches: under section → remote
    private const float IndentStashBase = IndentLevel;       // stashes: one level under their section

    public static IReadOnlyList<BranchRow> BuildRows(BranchListing? listing, BranchesUiState ui)
    {
        var rows = new List<BranchRow>();
        if (listing == null) return rows;

        rows.Add(new BranchRow(BranchRowKind.LocalHeader, "Local", IndentSection, ui.LocalOpen));
        if (ui.LocalOpen)
        {
            var localTree = BuildTree(listing.LocalBranches);
            EmitTreeRows(rows, localTree, ui, isRemote: false, remoteName: null, IndentLocalTreeBase, depth: 0);
        }

        rows.Add(new BranchRow(BranchRowKind.RemotesHeader, "Remote", IndentSection, ui.RemotesOpen));
        if (ui.RemotesOpen)
        {
            foreach (var rg in listing.Remotes)
            {
                var isOpen = ui.RemoteOpen.TryGetValue(rg.Name, out var v) ? v : true;
                rows.Add(new BranchRow(BranchRowKind.RemoteHeader, rg.Name, IndentRemoteHeader, isOpen)
                {
                    RemoteName = rg.Name,
                });
                if (!isOpen) continue;
                var remoteTree = BuildTree(rg.Branches);
                EmitTreeRows(rows, remoteTree, ui, isRemote: true, rg.Name, IndentRemoteTreeBase, depth: 0);
            }
        }

        if (listing.Stashes.Count > 0)
        {
            rows.Add(new BranchRow(BranchRowKind.StashesHeader, "Stashes", IndentSection, ui.StashesOpen));
            if (ui.StashesOpen)
            {
                foreach (var s in listing.Stashes)
                {
                    var label = $"stash@{{{s.Index}}}";
                    rows.Add(new BranchRow(BranchRowKind.Stash, s.Subject, IndentStashBase, isOpen: false)
                    {
                        TipSha = s.Sha,
                        FullPath = label,
                        StashIndex = s.Index,
                    });
                }
            }
        }
        return rows;
    }

    private static void EmitTreeRows(List<BranchRow> rows, IReadOnlyList<TreeNode> nodes, BranchesUiState ui, bool isRemote, string? remoteName, float treeBase, int depth)
    {
        var indent = treeBase + depth * IndentLevel;
        foreach (var node in nodes)
        {
            if (node.Entry is { } entry)
            {
                rows.Add(new BranchRow(isRemote ? BranchRowKind.RemoteBranch : BranchRowKind.LocalBranch, node.Segment, indent, isOpen: false)
                {
                    TipSha = entry.TipSha,
                    IsHead = entry.IsHead,
                    RemoteName = remoteName,
                    FullPath = entry.Name,
                    AheadBy = entry.AheadBy,
                    BehindBy = entry.BehindBy,
                    UpstreamState = entry.UpstreamState,
                });
            }
            else
            {
                var key = MakeFolderKey(isRemote, remoteName, node.FullPath);
                var open = ui.FolderOpen.TryGetValue(key, out var v) ? v : true;
                rows.Add(new BranchRow(BranchRowKind.Folder, node.Segment, indent, open)
                {
                    RemoteName = remoteName,
                    FullPath = node.FullPath,
                    FolderKey = key,
                });
                if (open) EmitTreeRows(rows, node.Children, ui, isRemote, remoteName, treeBase, depth + 1);
            }
        }
    }

    private static string MakeFolderKey(bool isRemote, string? remoteName, string path) =>
        isRemote ? $"remote:{remoteName}:{path}" : $"local:{path}";

    private static IReadOnlyList<TreeNode> BuildTree(IReadOnlyList<BranchEntry> branches)
    {
        var root = new TreeNode("", "");
        foreach (var b in branches)
        {
            var segments = b.Name.Split('/');
            var current = root;
            for (var i = 0; i < segments.Length; i++)
            {
                var seg = segments[i];
                var isLeaf = i == segments.Length - 1;
                if (!current.ChildIndex.TryGetValue(seg, out var child))
                {
                    var path = i == 0 ? seg : current.FullPath + "/" + seg;
                    child = new TreeNode(seg, path);
                    current.ChildIndex[seg] = child;
                    current.Children.Add(child);
                }
                if (isLeaf) child.Entry = b;
                current = child;
            }
        }
        SortNode(root);
        return root.Children;
    }

    // Folders first, then leaves; alphabetical within each group. The leaf-of-the-same-path
    // case (a branch named "feature" alongside "feature/login") cannot occur in git, so we
    // don't try to handle a node that's simultaneously a folder and a branch.
    private static void SortNode(TreeNode node)
    {
        node.Children.Sort((a, b) =>
        {
            var aFolder = a.Entry == null;
            var bFolder = b.Entry == null;
            if (aFolder != bFolder) return aFolder ? -1 : 1;
            return string.Compare(a.Segment, b.Segment, StringComparison.OrdinalIgnoreCase);
        });
        foreach (var c in node.Children) SortNode(c);
    }

    private sealed class TreeNode
    {
        public TreeNode(string segment, string fullPath)
        {
            Segment = segment;
            FullPath = fullPath;
        }
        public string Segment { get; }
        public string FullPath { get; }
        public BranchEntry? Entry { get; set; }
        public Dictionary<string, TreeNode> ChildIndex { get; } = new();
        public List<TreeNode> Children { get; } = new();
    }
}
