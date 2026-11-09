using System.Text;

namespace ZGF.Gui.Immediate;

/// <summary>One step of a <see cref="TestPath"/>: a name a widget was given as its string id, or a
/// key that says which item of a collection it is.</summary>
public abstract record TestSegment
{
    private TestSegment()
    {
    }

    /// <summary>A widget or id scope given the string id <see cref="Name"/>.</summary>
    public sealed record Named(string Name) : TestSegment;

    /// <summary>An item of a collection, told apart from the others by <see cref="Key"/>: a
    /// <see cref="Ui.KeyedId"/> scope, or a tree row's key.</summary>
    public sealed record Keyed(string Key) : TestSegment;
}

/// <summary>
/// Where an element sits among the string ids and keys it was recorded under, for tests to find it
/// by. Written with '/' between segments, a keyed segment starting with '#'; '/', '#' and the escape
/// character '\' inside a name or key are escaped with '\', so a name "a/b" and the names "a" then
/// "b" never read the same, and an empty name or key is written "\e".
/// </summary>
public sealed class TestPath : IEquatable<TestPath>
{
    private const char Separator = '/';
    private const char KeyMark = '#';
    private const char Escape = '\\';
    private const char EmptyMark = 'e';

    private readonly TestSegment[] _segments;

    public TestPath(IEnumerable<TestSegment> segments)
    {
        _segments = segments.ToArray();
        if (_segments.Length == 0)
            throw new ArgumentException("A test path has at least one segment.", nameof(segments));
    }

    public IReadOnlyList<TestSegment> Segments => _segments;

    /// <summary>Reads a path written as <see cref="ToString"/> writes one.</summary>
    public static TestPath Parse(string text)
    {
        var segments = new List<TestSegment>();
        var current = new StringBuilder();
        var keyed = false;
        var empty = false;
        var atStart = true;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == Separator)
            {
                segments.Add(Segment(text, current, keyed, empty));
                current.Clear();
                keyed = false;
                empty = false;
                atStart = true;
                continue;
            }
            if (empty)
                throw new FormatException($"The test path \"{text}\" has more in a segment marked empty.");
            if (c == KeyMark && atStart)
            {
                keyed = true;
                atStart = false;
                continue;
            }
            atStart = false;
            if (c != Escape)
            {
                current.Append(c);
                continue;
            }
            if (i + 1 >= text.Length)
                throw new FormatException($"The test path \"{text}\" ends in an escape character.");
            var escaped = text[++i];
            if (escaped is Separator or KeyMark or Escape)
                current.Append(escaped);
            else if (escaped == EmptyMark && current.Length == 0)
                empty = true;
            else
                throw new FormatException($"The test path \"{text}\" escapes '{escaped}', which needs no escape.");
        }
        segments.Add(Segment(text, current, keyed, empty));
        return new TestPath(segments);
    }

    private static TestSegment Segment(string text, StringBuilder current, bool keyed, bool empty)
    {
        if (current.Length == 0 && !empty)
            throw new FormatException($"The test path \"{text}\" has a segment with nothing in it; an empty name is written \\e.");
        var value = current.ToString();
        if (keyed)
            return new TestSegment.Keyed(value);
        return new TestSegment.Named(value);
    }

    public override string ToString()
    {
        var text = new StringBuilder();
        for (var i = 0; i < _segments.Length; i++)
        {
            if (i > 0)
                text.Append(Separator);
            AppendSegment(text, _segments[i]);
        }
        return text.ToString();
    }

    private static void AppendSegment(StringBuilder text, TestSegment segment)
    {
        switch (segment)
        {
            case TestSegment.Named named:
                AppendEscaped(text, named.Name);
                break;
            case TestSegment.Keyed keyed:
                text.Append(KeyMark);
                AppendEscaped(text, keyed.Key);
                break;
            default:
                throw new InvalidOperationException($"Unknown segment {segment}.");
        }
    }

    private static void AppendEscaped(StringBuilder text, string value)
    {
        if (value.Length == 0)
        {
            text.Append(Escape).Append(EmptyMark);
            return;
        }
        foreach (var c in value)
        {
            if (c is Separator or KeyMark or Escape)
                text.Append(Escape);
            text.Append(c);
        }
    }

    public bool Equals(TestPath? other)
    {
        if (other == null) return false;
        return _segments.AsSpan().SequenceEqual(other._segments);
    }

    public override bool Equals(object? obj) => obj is TestPath other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var segment in _segments)
            hash.Add(segment);
        return hash.ToHashCode();
    }
}

/// <summary>A test path as recorded during a frame: a segment under the path of its enclosing scope,
/// shared by everything recorded under it and turned into a <see cref="TestPath"/> only when a
/// snapshot asks.</summary>
internal sealed class PathNode
{
    private TestPath? _path;

    public PathNode(PathNode? parent, TestSegment segment)
    {
        Parent = parent;
        Segment = segment;
    }

    public PathNode? Parent { get; }
    public TestSegment Segment { get; }

    public TestPath ToPath()
    {
        if (_path != null) return _path;
        var segments = new List<TestSegment>();
        for (var node = this; node != null; node = node.Parent)
            segments.Add(node.Segment);
        segments.Reverse();
        _path = new TestPath(segments);
        return _path;
    }
}
