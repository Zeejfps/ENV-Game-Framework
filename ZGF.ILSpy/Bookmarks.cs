using System.Runtime.CompilerServices;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>A marked line of code: the place whose document it was made in, what names the line
/// there, and what the user calls it. With an <see cref="Anchor"/> it finds its line in any document
/// showing it, in either language; without one, only where it was made. <see cref="ILSpy"/> and
/// <see cref="ViewState"/> are what the real ILSpy keeps besides, so it can be written to its
/// bookmarks file and read back from one.</summary>
public sealed class Bookmark
{
    public Bookmark(NodePath place, DecompilationLanguage language, int line, CodeAnchor? anchor, string name, ILSpyBookmarkInfo ilspy,
        BookmarkViewState? viewState = null, bool enabled = true)
    {
        Place = place;
        Language = language;
        Line = line;
        Anchor = anchor;
        Name = name;
        ILSpy = ilspy;
        ViewState = viewState;
        Enabled = enabled;
        Location = $"{place.Text}:{line + 1}";
        Module = Path.GetFileName(place.FilePath);
    }

    /// <summary>The bookmark <paramref name="record"/> describes, made in the document of <paramref name="place"/>.</summary>
    public static Bookmark FromRecord(BookmarkRecord record, NodePath place)
    {
        CodeAnchor? anchor = record.Kind switch
        {
            BookmarkKind.Token => new CodeAnchor(CodeAnchorKind.Definition, record.FileName, (int)record.Token),
            BookmarkKind.Body => new CodeAnchor(CodeAnchorKind.Statement, record.FileName, (int)record.Token, record.ILOffset),
            _ => null,
        };
        var ilspy = new ILSpyBookmarkInfo(record.AssemblyFullName ?? "", record.ModuleName ?? Path.GetFileName(record.FileName), record.Token,
            record.MemberName ?? "", record.LocationNodeName, record.ViewState?.SelectedTreeNodePath);
        return new Bookmark(place, record.Language ?? DecompilationLanguage.CSharp, Math.Max(0, record.LineNumber - 1), anchor, record.Name ?? "", ilspy,
            record.ViewState, record.Enabled);
    }

    /// <summary>This bookmark as ILSpy's bookmarks file holds it, with this app's place and language.</summary>
    public BookmarkRecord ToRecord() => new(
        Name,
        Enabled,
        Anchor?.FilePath ?? Place.FilePath,
        ILSpy.AssemblyFullName,
        ILSpy.ModuleName,
        ILSpy.Token,
        Anchor?.Kind switch
        {
            CodeAnchorKind.Definition => BookmarkKind.Token,
            CodeAnchorKind.Statement => BookmarkKind.Body,
            _ => BookmarkKind.Line,
        },
        Anchor is { Kind: CodeAnchorKind.Statement } statement ? statement.ILOffset : 0,
        Line + 1,
        ILSpy.MemberName,
        ILSpy.LocationNodeName,
        ViewState ?? new BookmarkViewState(1, 0, 0d, 0d, null, null, null, ILSpy.TreePath?.ToList()),
        Place.Segments.ToList(),
        Language);

    public NodePath Place { get; }

    /// <summary>The language of the document it was made in.</summary>
    public DecompilationLanguage Language { get; }

    /// <summary>The line it was made on, from 0.</summary>
    public int Line { get; }

    public CodeAnchor? Anchor { get; }

    /// <summary>What the real ILSpy records about it besides its line.</summary>
    public ILSpyBookmarkInfo ILSpy { get; }

    /// <summary>ILSpy's view state, kept as it came from an ILSpy file to give back to one; null for a
    /// bookmark made here, which writes one holding only the tree path.</summary>
    public BookmarkViewState? ViewState { get; }

    public string Name { get; internal set; }

    /// <summary>A disabled bookmark stays listed and marked, dimmed, but next and previous skip it.</summary>
    public bool Enabled { get; internal set; }

    /// <summary>The place and line it was made on.</summary>
    public string Location { get; }

    /// <summary>The file name of the assembly it is in.</summary>
    public string Module { get; }

    /// <summary>Whether <paramref name="other"/> marks the same line: the same anchor, or for one
    /// without, the same line of the same place in the same language.</summary>
    public bool MarksSameLine(Bookmark other) =>
        Anchor is { } anchor ? other.Anchor == anchor
        : other.Anchor == null && Language == other.Language && Line == other.Line && Place.Equals(other.Place);

    /// <summary>The line it marks in <paramref name="document"/>, shown for <paramref name="place"/>, or -1.</summary>
    public int FindLine(DecompiledDocument document, NodePath place) =>
        Anchor is { } anchor ? document.FindLine(anchor)
        : Language == document.Language && Place.Equals(place) && Line < document.Lines.Length ? Line
        : -1;
}

/// <summary>What importing bookmarks does with the ones there are.</summary>
public enum BookmarkImportMode
{
    /// <summary>Drops them for the imported ones.</summary>
    Replace,

    /// <summary>Keeps them, adding the imported ones that mark a line none of them does.</summary>
    Merge,
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

    public List<BookmarkRecord> Save() => _items.ConvertAll(static b => b.ToRecord());

    /// <summary>Replaces the list with the bookmarks in <paramref name="saved"/> that know their place.</summary>
    public void Load(IEnumerable<BookmarkRecord>? saved) =>
        Import((saved ?? []).Where(static r => r is { Place: not null } && !string.IsNullOrEmpty(r.FileName))
            .Select(static r => Bookmark.FromRecord(r, NodePath.From(r.FileName, r.Place!))), BookmarkImportMode.Replace);

    /// <summary>Adds <paramref name="bookmarks"/>, after dropping every bookmark first when replacing,
    /// or skipping the ones that mark a line already marked when merging. Returns how many were added.</summary>
    public int Import(IEnumerable<Bookmark> bookmarks, BookmarkImportMode mode)
    {
        if (mode == BookmarkImportMode.Replace)
        {
            _items.Clear();
            Selected = -1;
            Renaming = null;
        }
        var added = 0;
        foreach (var bookmark in bookmarks)
        {
            if (mode == BookmarkImportMode.Merge && _items.Exists(bookmark.MarksSameLine)) continue;
            _items.Add(bookmark);
            added++;
        }
        Revision++;
        return added;
    }
}
