namespace ZGF.ILSpy.Core;

/// <summary>One step of a <see cref="NodePath"/>: the kind and label of a node.</summary>
public readonly record struct NodePathSegment(NodeKind Kind, string Text);

/// <summary>
/// Where a <see cref="BrowseNode"/> sits: its assembly's file and the kind and label of each node
/// down from the root. Unlike the node, it stays meaningful after the assembly is closed or
/// reloaded; <see cref="BrowseSession.Find(LoadedAssembly, NodePath)"/> finds the node it names again.
/// </summary>
public sealed class NodePath : IEquatable<NodePath>
{
    private readonly NodePathSegment[] _segments;

    private NodePath(AssemblyPath filePath, NodePathSegment[] segments)
    {
        FilePath = filePath;
        _segments = segments;
    }

    /// <summary>The absolute path of the listed file the path starts from: an assembly, or the package
    /// an assembly was read out of.</summary>
    public AssemblyPath FilePath { get; }

    /// <summary>How many nodes lie below the root on the way down, the node itself included.</summary>
    public int Depth => _segments.Length;

    /// <summary>The kind of the node this names.</summary>
    public NodeKind Kind => _segments.Length == 0 ? NodeKind.Assembly : _segments[^1].Kind;

    /// <summary>The kind and label of each node down from the root, the node itself last.</summary>
    public IReadOnlyList<NodePathSegment> Segments => _segments;

    /// <summary>The label of the node this names.</summary>
    public string Text => _segments.Length == 0 ? FilePath.FileName : _segments[^1].Text;

    /// <summary>The path to <paramref name="node"/> from the root of its tree: for a node of an
    /// assembly read out of a package, from the package's root.</summary>
    public static NodePath Of(BrowseNode node)
    {
        var depth = 0;
        var root = node;
        for (; root.Parent != null; root = root.Parent)
            depth++;
        var segments = new NodePathSegment[depth];
        for (var n = node; n.Parent != null; n = n.Parent)
            segments[--depth] = new NodePathSegment(n.Kind, n.Text);
        return new NodePath(root.Assembly.FilePath, segments);
    }

    /// <summary>The path to the node reached from the root of the assembly at
    /// <paramref name="filePath"/> through <paramref name="segments"/>, as <see cref="Segments"/> gives them.</summary>
    public static NodePath From(AssemblyPath filePath, IEnumerable<NodePathSegment> segments) => new(filePath, segments.ToArray());

    public bool Equals(NodePath? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null || other._segments.Length != _segments.Length) return false;
        if (!FilePath.Equals(other.FilePath)) return false;
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
        hash.Add(FilePath);
        foreach (var segment in _segments)
            hash.Add(segment);
        return hash.ToHashCode();
    }

    public override string ToString() =>
        _segments.Length == 0 ? FilePath.Text : $"{FilePath}: {string.Join(" › ", _segments.Select(s => s.Text))}";
}
