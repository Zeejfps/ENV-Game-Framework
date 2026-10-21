using System.Runtime.CompilerServices;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>A marked line of code: the place whose document it was made in, what names the line
/// there, and what the user calls it. With an <see cref="Anchor"/> it finds its line in any document
/// showing it, in either language; without one, only where it was made.</summary>
public sealed class Bookmark
{
    public Bookmark(NodePath place, DecompilationLanguage language, int line, CodeAnchor? anchor, string name, bool enabled = true)
    {
        Place = place;
        Language = language;
        Line = line;
        Anchor = anchor;
        Name = name;
        Enabled = enabled;
        Location = $"{place.Text}:{line + 1}";
        Module = Path.GetFileName(place.FilePath);
    }

    public NodePath Place { get; }

    /// <summary>The language of the document it was made in.</summary>
    public DecompilationLanguage Language { get; }

    /// <summary>The line it was made on, from 0.</summary>
    public int Line { get; }

    public CodeAnchor? Anchor { get; }

    public string Name { get; internal set; }

    /// <summary>A disabled bookmark stays listed and marked, dimmed, but next and previous skip it.</summary>
    public bool Enabled { get; internal set; }

    /// <summary>The place and line it was made on.</summary>
    public string Location { get; }

    /// <summary>The file name of the assembly it is in.</summary>
    public string Module { get; }

    /// <summary>The line it marks in <paramref name="document"/>, shown for <paramref name="place"/>, or -1.</summary>
    public int FindLine(DecompiledDocument document, NodePath place) =>
        Anchor is { } anchor ? document.FindLine(anchor)
        : Language == document.Language && Place.Equals(place) && Line < document.Lines.Length ? Line
        : -1;
}

/// <summary>The user's bookmarks in the order they were made, the one selected in the panel, and the
/// one being renamed. Every change to the list or a bookmark bumps <see cref="Revision"/>.</summary>
public sealed class BookmarkList
{
    private sealed class Marks
    {
        public int Revision = -1;
        public NodePath? Place;
        public readonly Dictionary<int, bool> Lines = new();
    }

    private readonly List<Bookmark> _items = new();
    private readonly ConditionalWeakTable<DecompiledDocument, Marks> _marks = new();

    public IReadOnlyList<Bookmark> Items => _items;

    public int Revision { get; private set; }

    /// <summary>The index of the bookmark selected in the panel, or -1.</summary>
    public int Selected { get; private set; } = -1;

    public Bookmark? SelectedBookmark => Selected >= 0 ? _items[Selected] : null;

    /// <summary>Set when the selection moves: the panel should scroll it into view.</summary>
    public bool ScrollToSelected { get; set; }

    /// <summary>The bookmark whose name is being edited, and the name typed so far.</summary>
    public Bookmark? Renaming { get; private set; }

    public string RenameText = "";

    /// <summary>True once after a rename starts: the name field should take focus.</summary>
    public bool FocusRename { get; set; }

    public void Add(Bookmark bookmark)
    {
        _items.Add(bookmark);
        Select(_items.Count - 1);
        Revision++;
    }

    public void Remove(Bookmark bookmark)
    {
        var index = _items.IndexOf(bookmark);
        if (index < 0) return;
        _items.RemoveAt(index);
        if (Renaming == bookmark)
            Renaming = null;
        Selected = _items.Count == 0 ? -1 : Math.Min(Selected, _items.Count - 1);
        Revision++;
    }

    public void Select(int index)
    {
        Selected = index >= 0 && index < _items.Count ? index : -1;
        ScrollToSelected = Selected >= 0;
    }

    public void SetEnabled(Bookmark bookmark, bool enabled)
    {
        if (bookmark.Enabled == enabled) return;
        bookmark.Enabled = enabled;
        Revision++;
    }

    public void StartRename(Bookmark bookmark)
    {
        Renaming = bookmark;
        RenameText = bookmark.Name;
        FocusRename = true;
    }

    /// <summary>Gives the bookmark being renamed the name typed, unless it is blank, and ends the rename.</summary>
    public void CommitRename()
    {
        if (Renaming is { } bookmark && RenameText.Trim() is { Length: > 0 } name && name != bookmark.Name)
        {
            bookmark.Name = name;
            Revision++;
        }
        Renaming = null;
    }

    public void CancelRename() => Renaming = null;

    /// <summary>Selects the next enabled bookmark after the selected one, or the previous when
    /// <paramref name="direction"/> is negative, wrapping around; null when none is enabled.</summary>
    public Bookmark? Step(int direction)
    {
        var count = _items.Count;
        var start = Selected >= 0 ? Selected : direction > 0 ? -1 : count;
        for (var step = 1; step <= count; step++)
        {
            var index = ((start + Math.Sign(direction) * step) % count + count) % count;
            if (!_items[index].Enabled) continue;
            Select(index);
            return _items[index];
        }
        return null;
    }

    /// <summary>The first bookmark marking <paramref name="line"/> of <paramref name="document"/>,
    /// shown for <paramref name="place"/>.</summary>
    public Bookmark? At(DecompiledDocument document, NodePath place, int line)
    {
        foreach (var bookmark in _items)
        {
            if (bookmark.FindLine(document, place) == line)
                return bookmark;
        }
        return null;
    }

    /// <summary>The lines of <paramref name="document"/>, shown for <paramref name="place"/>, that
    /// bookmarks mark, each with whether one marking it is enabled.</summary>
    public IReadOnlyDictionary<int, bool> LinesIn(DecompiledDocument document, NodePath place)
    {
        var marks = _marks.GetOrCreateValue(document);
        if (marks.Revision == Revision && Equals(marks.Place, place)) return marks.Lines;
        marks.Revision = Revision;
        marks.Place = place;
        marks.Lines.Clear();
        foreach (var bookmark in _items)
        {
            var line = bookmark.FindLine(document, place);
            if (line >= 0)
                marks.Lines[line] = marks.Lines.GetValueOrDefault(line) || bookmark.Enabled;
        }
        return marks.Lines;
    }

    /// <summary>The first of <paramref name="format"/>(1), (2), … that no bookmark is called.</summary>
    public string NextName(Func<int, string> format)
    {
        for (var n = 1; ; n++)
        {
            var name = format(n);
            if (!_items.Exists(b => b.Name == name))
                return name;
        }
    }

    public List<SavedBookmark> Save() =>
        _items.Select(static b => new SavedBookmark(b.Name, b.Enabled, b.Place.FilePath, b.Place.Segments.ToList(), b.Language, b.Line, b.Anchor))
            .ToList();

    public void Load(IEnumerable<SavedBookmark>? saved)
    {
        _items.Clear();
        foreach (var b in saved ?? [])
        {
            if (string.IsNullOrEmpty(b.File) || b.Place == null || b.Line < 0) continue;
            _items.Add(new Bookmark(NodePath.From(b.File, b.Place), b.Language, b.Line, b.Anchor, b.Name ?? "", b.Enabled));
        }
        Selected = -1;
        Renaming = null;
        Revision++;
    }
}
