namespace ZGF.ILSpy.Core;

/// <summary>
/// Where a <see cref="BrowseNode"/> sits: its assembly's file and the kind and label of each node
/// down from the root. Unlike the node, it stays meaningful after the assembly is closed or
/// reloaded; <see cref="AssemblyList.FindNode(NodePath)"/> finds the node it names again.
/// </summary>
public sealed class NodePath : IEquatable<NodePath>
{
    private readonly (NodeKind Kind, string Text)[] _segments;

    private NodePath(string filePath, (NodeKind Kind, string Text)[] segments)
    {
        FilePath = filePath;
        _segments = segments;
    }

    /// <summary>The absolute path of the assembly file.</summary>
    public string FilePath { get; }

    /// <summary>How many nodes lie below the root on the way down, the node itself included.</summary>
    public int Depth => _segments.Length;

    /// <summary>The label of the node this names.</summary>
    public string Text => _segments.Length == 0 ? Path.GetFileName(FilePath) : _segments[^1].Text;

    public static NodePath Of(BrowseNode node)
    {
        var depth = 0;
        for (var n = node; n.Parent != null; n = n.Parent)
            depth++;
        var segments = new (NodeKind, string)[depth];
        for (var n = node; n.Parent != null; n = n.Parent)
            segments[--depth] = (n.Kind, n.Text);
        return new NodePath(node.Assembly.FilePath, segments);
    }

    internal BrowseNode? Resolve(BrowseNode root)
    {
        var node = root;
        foreach (var (kind, text) in _segments)
        {
            BrowseNode? next = null;
            foreach (var child in node.Children)
            {
                if (child.Kind == kind && child.Text == text)
                {
                    next = child;
                    break;
                }
            }
            if (next is null) return null;
            node = next;
        }
        return node;
    }

    public bool Equals(NodePath? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null || other._segments.Length != _segments.Length) return false;
        if (!string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase)) return false;
        for (var i = _segments.Length - 1; i >= 0; i--)
        {
            if (_segments[i] != other._segments[i]) return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is NodePath other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(FilePath, StringComparer.OrdinalIgnoreCase);
        foreach (var segment in _segments)
            hash.Add(segment);
        return hash.ToHashCode();
    }

    public override string ToString() =>
        _segments.Length == 0 ? FilePath : $"{FilePath}: {string.Join(" › ", _segments.Select(s => s.Text))}";
}
