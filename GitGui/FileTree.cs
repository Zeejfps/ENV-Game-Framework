namespace GitGui;

public enum FileViewMode { Flat, Tree }

internal enum FileRowKind { Folder, File }

/// <summary>
/// One rendered row of a local-changes file list — either a directory <see cref="FileRowKind.Folder"/>
/// node or a <see cref="FileRowKind.File"/> leaf. Produced by <see cref="FileTreeBuilder"/> from the
/// flat <see cref="FileChange"/> list plus the view mode and the collapsed-folder set; the panel renders
/// these and the view model navigates/selects against the same sequence so the two never diverge.
///
/// <see cref="Files"/> is the set of descendant file paths a row operates on: a single-element list for
/// a file row, every leaf beneath a folder row. That makes "stage/discard this folder" and folder
/// selection a path-list operation the existing git ops already understand.
/// </summary>
internal sealed class FileRow
{
    private FileRow(
        FileRowKind kind,
        string displayName,
        float indent,
        bool isOpen,
        string fullPath,
        DiffSide side,
        FileChange? file,
        IReadOnlyList<string> files)
    {
        Kind = kind;
        DisplayName = displayName;
        Indent = indent;
        IsOpen = isOpen;
        FullPath = fullPath;
        Side = side;
        File = file;
        Files = files;
    }

    public FileRowKind Kind { get; }
    public string DisplayName { get; }
    public float Indent { get; }
    public bool IsOpen { get; }
    public string FullPath { get; }
    public DiffSide Side { get; }
    public FileChange? File { get; }
    public IReadOnlyList<string> Files { get; }

    public FileRowRef Ref => new(Side, FullPath, Kind == FileRowKind.Folder);

    public static FileRow ForFile(FileChange file, string displayName, float indent, DiffSide side)
        => new(FileRowKind.File, displayName, indent, isOpen: false, file.Path, side, file, new[] { file.Path });

    public static FileRow ForFolder(
        string displayName, string fullPath, float indent, bool isOpen, IReadOnlyList<string> files, DiffSide side)
        => new(FileRowKind.Folder, displayName, indent, isOpen, fullPath, side, file: null, files);
}

/// <summary>
/// Stable identity for a row across rebuilds — a folder's full path or a file's path, scoped to a side.
/// Used as the selection anchor/cursor so keyboard navigation survives the row list being rebuilt
/// (collapse toggle, view-mode switch, working-tree reload).
/// </summary>
internal readonly record struct FileRowRef(DiffSide Side, string FullPath, bool IsFolder);

/// <summary>
/// Pure flattening of a <see cref="FileChange"/> list plus (view-mode, collapsed-folders) into the linear
/// <see cref="FileRow"/> sequence a <c>LocalChangesPanel</c> renders. Flat mode emits one file row per
/// file; tree mode splits paths on "/" into folder nodes, compacts single-child folder chains
/// (<c>Assets/Scripts/UI</c>), sorts folders-before-files alphabetically, and hides rows under collapsed
/// folders. Mirrors <see cref="BranchTreeBuilder"/>: no dependency on layout pixels or the view tree, so
/// both the panel (render) and the view model (navigation) can call it.
/// </summary>
internal static class FileTreeBuilder
{
    // One level of nesting is wider than a folder row's chevron column (chevron + gap),
    // so a child file's badge sits clearly to the right of its parent folder's icon
    // rather than lining up with it.
    public const float IndentLevel = 20f;

    private static readonly IReadOnlyList<FileRow> Empty = Array.Empty<FileRow>();

    public static IReadOnlyList<FileRow> BuildRows(
        IReadOnlyList<FileChange> files,
        DiffSide side,
        FileViewMode mode,
        IReadOnlySet<string> collapsed)
    {
        if (files.Count == 0) return Empty;

        if (mode == FileViewMode.Flat)
        {
            var flat = new List<FileRow>(files.Count);
            foreach (var f in files)
                flat.Add(FileRow.ForFile(f, FileChangeFormatting.FormatPath(f), 0f, side));
            return flat;
        }

        var root = BuildTree(files);
        SortNode(root);
        var rows = new List<FileRow>();
        EmitTreeRows(rows, root.Children, side, collapsed, depth: 0);
        return rows;
    }

    private static void EmitTreeRows(
        List<FileRow> rows, IReadOnlyList<Node> nodes, DiffSide side, IReadOnlySet<string> collapsed, int depth)
    {
        var indent = depth * IndentLevel;
        foreach (var node in nodes)
        {
            if (node.File is { } file)
            {
                rows.Add(FileRow.ForFile(file, FileChangeFormatting.FormatLeaf(file), indent, side));
                continue;
            }

            // Compact a chain of single-child folders into one row (Assets → Scripts → UI
            // becomes "Assets/Scripts/UI"). A folder whose only child is a file is not
            // compacted — the file keeps its own row beneath the folder.
            var display = node.Segment;
            var folder = node;
            while (folder.Children.Count == 1 && folder.Children[0].File == null)
            {
                folder = folder.Children[0];
                display += "/" + folder.Segment;
            }

            var open = !collapsed.Contains(folder.FullPath);
            var leaves = new List<string>();
            CollectLeaves(folder, leaves);
            rows.Add(FileRow.ForFolder(display, folder.FullPath, indent, open, leaves, side));
            if (open) EmitTreeRows(rows, folder.Children, side, collapsed, depth + 1);
        }
    }

    private static void CollectLeaves(Node node, List<string> into)
    {
        if (node.File is { } file) { into.Add(file.Path); return; }
        foreach (var c in node.Children) CollectLeaves(c, into);
    }

    private static Node BuildTree(IReadOnlyList<FileChange> files)
    {
        var root = new Node("", "");
        foreach (var f in files)
        {
            var segments = f.Path.Split('/');
            var current = root;
            for (var i = 0; i < segments.Length; i++)
            {
                var seg = segments[i];
                var isLeaf = i == segments.Length - 1;
                if (!current.ChildIndex.TryGetValue(seg, out var child))
                {
                    var path = i == 0 ? seg : current.FullPath + "/" + seg;
                    child = new Node(seg, path);
                    current.ChildIndex[seg] = child;
                    current.Children.Add(child);
                }
                if (isLeaf) child.File = f;
                current = child;
            }
        }
        return root;
    }

    // Folders first, then files; alphabetical within each group. A path can't be both a
    // file and a directory in git, so no node is simultaneously a leaf and a folder.
    private static void SortNode(Node node)
    {
        node.Children.Sort((a, b) =>
        {
            var aFolder = a.File == null;
            var bFolder = b.File == null;
            if (aFolder != bFolder) return aFolder ? -1 : 1;
            return string.Compare(a.Segment, b.Segment, StringComparison.OrdinalIgnoreCase);
        });
        foreach (var c in node.Children) SortNode(c);
    }

    private sealed class Node
    {
        public Node(string segment, string fullPath)
        {
            Segment = segment;
            FullPath = fullPath;
        }
        public string Segment { get; }
        public string FullPath { get; }
        public FileChange? File { get; set; }
        public Dictionary<string, Node> ChildIndex { get; } = new();
        public List<Node> Children { get; } = new();
    }
}
