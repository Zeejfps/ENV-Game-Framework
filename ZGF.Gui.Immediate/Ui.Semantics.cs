using System.Runtime.InteropServices;

namespace ZGF.Gui.Immediate;

public abstract partial class Ui
{
    private const int KeyedSalt = 0x4B455944;

    private readonly record struct Annotation(SemanticRole Role, string? Label, SemanticStates States, string? ValueText, PathNode? Path);

    private bool _recordingSemantics;
    private readonly Stack<PathNode?> _paths = new();
    private readonly Dictionary<int, PathNode> _names = new();
    private readonly Dictionary<int, Annotation> _annotatedElements = new();
    private readonly Dictionary<int, Annotation> _annotatedIds = new();
    private readonly List<int> _elementLayers = new();
    private readonly Stack<int> _layerIndices = new();
    private readonly Dictionary<int, int> _modalElements = new();
    private readonly HashSet<int> _windowElements = new();
    private readonly List<ElementVisit> _visits = new();
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<ICanvas, System.Runtime.CompilerServices.StrongBox<long>> _surfaceIds = new();
    private long _nextSurfaceId;

    /// <summary>The identity of the surface <paramref name="canvas"/> draws, given the first time it is asked for
    /// and never given to another canvas.</summary>
    private long SurfaceIdOf(ICanvas canvas) =>
        _surfaceIds.GetValue(canvas, _ => new System.Runtime.CompilerServices.StrongBox<long>(++_nextSurfaceId)).Value;

    /// <summary>Whether each frame ends by building a <see cref="Snapshot"/>; until set, frames record no
    /// semantics, paths or snapshot at all. Takes effect from the next frame.</summary>
    internal bool BuildsSemantics { get; set; }

    /// <summary>Whether this frame records semantics for a snapshot a test asked for. A widget checks it before
    /// working out what it passes to <see cref="Semantics(int, SemanticRole, string?, SemanticStates, string?)"/>
    /// or <see cref="RecordTestPath(int, TestSegment, int)"/> costs anything, so a frame nobody inspects does no
    /// more than before.</summary>
    public bool WantsSemantics => _recordingSemantics;

    /// <summary>The last frame as a test sees it, when it was built with <see cref="BuildsSemantics"/>; else null.</summary>
    internal SemanticSnapshot? Snapshot { get; private set; }

    /// <summary>How many snapshot nodes every frame so far has built.</summary>
    internal long SemanticNodesBuilt { get; private set; }

    /// <summary>Scopes widget ids under <paramref name="key"/>, as <see cref="Id(string)"/> does, for an item of a
    /// collection told apart by a key of the app's, such as a row's: tests find what is inside it under
    /// <see cref="TestSegment.Keyed"/> <paramref name="key"/> wherever the item moves.</summary>
    public Scope KeyedId(string key)
    {
        _ids.Push(HashCode.Combine(_ids.Peek(), KeyedSalt, key));
        if (_recordingSemantics)
            _paths.Push(new PathNode(_paths.Peek(), new TestSegment.Keyed(key)));
        return BeginScope(ScopeKind.Id);
    }

    /// <summary>
    /// <see cref="UniqueId(int)"/> for what is opened from the element <paramref name="pathOf"/>, such as a
    /// menu's popup from its title: tests find what is inside under that element's <see cref="TestPath"/>.
    /// </summary>
    public Scope UniqueId(int id, int pathOf)
    {
        _ids.Push(id);
        if (_recordingSemantics)
            _paths.Push(_names.GetValueOrDefault(pathOf) ?? _names.GetValueOrDefault(id) ?? _paths.Peek());
        return BeginScope(ScopeKind.Id);
    }

    /// <summary>Says what the element being recorded is, for tests: its <paramref name="role"/>,
    /// <paramref name="label"/>, <paramref name="states"/> and <paramref name="value"/>, and with a
    /// <paramref name="name"/>, the <see cref="TestSegment.Named"/> it is found by under the current id
    /// scope. Costs nothing unless a test asked for a snapshot.</summary>
    public void Semantics(SemanticRole role, string? label = null, SemanticStates states = SemanticStates.None, string? value = null,
        string? name = null)
    {
        if (!_recordingSemantics) return;
        var path = name != null ? new PathNode(_paths.Peek(), new TestSegment.Named(name)) : null;
        _annotatedElements[_open.Peek()] = new Annotation(role, label, states, value, path);
    }

    /// <summary>As <see cref="Semantics(SemanticRole, string?, SemanticStates, string?, string?)"/>, for the
    /// element recorded this frame with <paramref name="id"/>, before or after it is: for a widget whose
    /// look an app's painter records.</summary>
    public void Semantics(int id, SemanticRole role, string? label = null, SemanticStates states = SemanticStates.None, string? value = null)
    {
        if (!_recordingSemantics || id == 0) return;
        _annotatedIds[id] = new Annotation(role, label, states, value, null);
    }

    /// <summary>Has tests find the element with <paramref name="id"/>, an id not taken from a string, by
    /// <paramref name="segment"/> under the test path of the element <paramref name="under"/>, or of the
    /// current id scope when that is 0 or has none. Costs nothing unless a test asked for a snapshot.</summary>
    public void RecordTestPath(int id, TestSegment segment, int under = 0)
    {
        if (!_recordingSemantics || id == 0) return;
        var parent = under != 0 && _names.TryGetValue(under, out var named) ? named : _paths.Peek();
        _names[id] = new PathNode(parent, segment);
    }

    /// <summary>As <see cref="RecordTestPath(int, TestSegment, int)"/>, for every segment of <paramref name="path"/> in turn.</summary>
    public void RecordTestPath(int id, TestPath path, int under = 0)
    {
        if (!_recordingSemantics || id == 0) return;
        var parent = under != 0 && _names.TryGetValue(under, out var named) ? named : _paths.Peek();
        var segments = path.Segments;
        var node = new PathNode(parent, segments[0]);
        for (var i = 1; i < segments.Count; i++)
            node = new PathNode(node, segments[i]);
        _names[id] = node;
    }

    /// <summary>The test path the element with <paramref name="id"/> was given this frame so far; null when it
    /// was given none, or no test asked for a snapshot.</summary>
    public TestPath? TestPathOf(int id)
    {
        if (!_recordingSemantics) return null;
        return _names.TryGetValue(id, out var named) ? named.ToPath() : null;
    }

    private void RecordName(int id, string label)
    {
        if (_recordingSemantics)
            _names[id] = new PathNode(_paths.Peek(), new TestSegment.Named(label));
    }

    private void PushPathOf(int id)
    {
        if (_recordingSemantics)
            _paths.Push(_names.GetValueOrDefault(id) ?? _paths.Peek());
    }

    private void PushCurrentPath()
    {
        if (_recordingSemantics)
            _paths.Push(_paths.Peek());
    }

    private void PopPath()
    {
        if (_recordingSemantics)
            _paths.Pop();
    }

    private void BeginSemantics()
    {
        _recordingSemantics = BuildsSemantics;
        _tree.Visits = _recordingSemantics ? _visits : null;
        if (!_recordingSemantics) return;
        _paths.Clear();
        _paths.Push(null);
        _names.Clear();
        _annotatedElements.Clear();
        _annotatedIds.Clear();
        _elementLayers.Clear();
        _layerIndices.Clear();
        _modalElements.Clear();
        _windowElements.Clear();
    }

    private void NoteElement()
    {
        if (_recordingSemantics)
            _elementLayers.Add(_layerIndices.Count > 0 ? _layerIndices.Peek() : -1);
    }

    private void NoteLayerBegun()
    {
        if (_recordingSemantics)
            _layerIndices.Push(_layers.Count - 1);
    }

    private void NoteLayerEnded()
    {
        if (_recordingSemantics)
            _layerIndices.Pop();
    }

    private void NoteModal(int element)
    {
        if (_recordingSemantics)
            _modalElements[_layers.Count - 1] = element;
    }

    private void NoteWindow(int element)
    {
        if (_recordingSemantics)
            _windowElements.Add(element);
    }

    private void EndSemantics()
    {
        if (!_recordingSemantics)
        {
            Snapshot = null;
            return;
        }
        Snapshot = new SnapshotBuilder(this).Build();
        SemanticNodesBuilt += Snapshot.Nodes.LongCount();
    }

    private sealed class SnapshotBuilder
    {
        private readonly Ui _ui;
        private readonly HitRegion[] _hits;
        private readonly int _hovered;
        private readonly Dictionary<int, SemanticSurface> _surfaceOfLayer = new();
        private readonly List<SemanticSurface> _surfaces = new();
        private readonly List<ICanvas> _canvases = new();
        private readonly HashSet<int> _popupElements = new();
        private readonly HashSet<int> _modalElements;
        private readonly Dictionary<int, SemanticNode> _nodeOfElement = new();
        private readonly Dictionary<int, SemanticNode> _byHitId = new();

        public SnapshotBuilder(Ui ui)
        {
            _ui = ui;
            _hits = ui._hits.ToArray();
            _hovered = ui._input.PointerLocked ? 0 : TopmostAt(_hits, ui._input.Pointer, out _);
            _modalElements = new HashSet<int>(ui._modalElements.Values);
            foreach (var popup in ui._popups)
                _popupElements.Add(popup.Element);
        }

        public SemanticSnapshot Build()
        {
            var ui = _ui;
            var tree = ui._tree;
            _surfaces.Add(new SemanticSurface(SurfaceKind.Main, 0, new UiRect(0f, 0f, ui.Width, ui.Height), ui._canvas.DpiScale)
            {
                Id = ui.SurfaceIdOf(ui._canvas),
                Owner = ui,
            });
            _canvases.Add(ui._canvas);
            for (var i = 0; i < tree.Count; i++)
                AddSurface(i);

            var roots = new List<SemanticNode>();
            if (tree.Count > 0)
                Visit(0, null, roots, 0, false);
            var root = roots[0];

            var modals = new SemanticNode?[ui._layers.Count];
            foreach (var (layer, element) in ui._modalElements)
                modals[layer] = _nodeOfElement.GetValueOrDefault(element);
            return new SemanticSnapshot(ui._frame, root, _surfaces, _canvases, _hits, modals, _byHitId, ui._input.PointerLocked);
        }

        private void AddSurface(int index)
        {
            var layer = _ui._tree[index].Layer;
            if (layer == 0 || _surfaceOfLayer.ContainsKey(layer)) return;
            var (canvas, rect) = _ui._tree.LayerAt(layer);
            if (canvas == null) return;
            var kind = _popupElements.Contains(index) ? SurfaceKind.Popup : SurfaceKind.Window;
            var surface = new SemanticSurface(kind, _surfaces.Count, rect, canvas.DpiScale) { Id = _ui.SurfaceIdOf(canvas), Owner = _ui };
            _surfaces.Add(surface);
            _canvases.Add(canvas);
            _surfaceOfLayer[layer] = surface;
        }

        private void Visit(int index, SemanticNode? parent, List<SemanticNode> into, int hitId, bool inScroll)
        {
            ref var e = ref _ui._tree[index];
            var ownHit = e.Id != 0 ? e.Id : hitId;
            var annotation = AnnotationOf(index, e.Id);
            var node = NodeOf(index, ref e, annotation, ownHit, inScroll);
            var childParent = parent;
            var childInto = into;
            if (node != null)
            {
                node.Parent = parent;
                into.Add(node);
                _nodeOfElement[index] = node;
                childParent = node;
                childInto = node.ChildList;
            }
            if (e.Id != 0 && childParent != null)
                _byHitId.TryAdd(e.Id, childParent);

            var childInScroll = inScroll || (e.Id != 0 && _ui._scrollIds.Contains(e.Id));
            for (var c = e.FirstChild; c >= 0; c = _ui._tree[c].NextSibling)
                Visit(c, childParent, childInto, ownHit, childInScroll);

            if (node != null && annotation != null)
                Finish(node);
        }

        private Annotation? AnnotationOf(int index, int id)
        {
            if (_ui._annotatedElements.TryGetValue(index, out var own))
                return own;
            if (id != 0 && _ui._annotatedIds.TryGetValue(id, out var byId))
                return byId;
            return null;
        }

        private SemanticNode? NodeOf(int index, ref Element e, Annotation? annotation, int hitId, bool inScroll)
        {
            var path = annotation?.Path;
            if (path == null && e.Id != 0)
                path = _ui._names.GetValueOrDefault(e.Id);
            var structural = StructuralRole(index);
            var text = TextOf(ref e);
            if (annotation == null && path == null && structural == null && text == null)
                return null;

            var visit = _ui._visits[index];
            var rect = new UiRect(e.X, e.Y, e.W, e.H);
            var (visibility, shown) = VisibilityOf(rect, visit, inScroll);
            return new SemanticNode
            {
                Path = path?.ToPath(),
                Role = annotation?.Role ?? structural ?? (text != null ? SemanticRole.Text : SemanticRole.Group),
                Label = annotation != null ? annotation.Value.Label ?? text : structural == null ? text : null,
                Value = annotation?.ValueText,
                States = (annotation?.States ?? SemanticStates.None) | InteractionStates(e.Id),
                Surface = SurfaceOf(visit),
                Bounds = rect,
                VisibleBounds = shown,
                Visibility = visibility,
                HitId = visit.PassThrough ? 0 : hitId,
                PassThrough = visit.PassThrough,
                Layer = _ui._elementLayers[index],
            };
        }

        private SemanticRole? StructuralRole(int index)
        {
            if (index == 0) return SemanticRole.Window;
            if (_modalElements.Contains(index)) return SemanticRole.Modal;
            if (_popupElements.Contains(index)) return SemanticRole.Popup;
            if (_ui._windowElements.Contains(index)) return SemanticRole.SubWindow;
            return null;
        }

        private static string? TextOf(ref Element e)
        {
            if (e.Text != null)
                return e.Text;
            if (e.Runs == null)
                return null;
            return string.Concat(e.Runs.Select(run => run.Text));
        }

        private SemanticStates InteractionStates(int id)
        {
            if (id == 0) return SemanticStates.None;
            var states = SemanticStates.None;
            if (id == _ui._focused) states |= SemanticStates.Focused;
            if (id == _hovered) states |= SemanticStates.Hovered;
            if (id == _ui._active) states |= SemanticStates.Pressed;
            return states;
        }

        private static (Visibility Visibility, UiRect Shown) VisibilityOf(UiRect rect, ElementVisit visit, bool inScroll)
        {
            var empty = new UiRect(rect.X, rect.Y, 0f, 0f);
            if (!visit.Emitted || rect.Width <= 0f || rect.Height <= 0f || visit.Opacity <= 0f)
                return (Visibility.Hidden, empty);
            var shown = visit.Clip.Intersect(rect);
            if (shown.Width <= 0f || shown.Height <= 0f)
                return (inScroll ? Visibility.ScrolledOut : Visibility.Hidden, empty);
            if (shown.Width < rect.Width - 0.5f || shown.Height < rect.Height - 0.5f)
                return (Visibility.Clipped, shown);
            return (Visibility.Visible, shown);
        }

        private SemanticSurface SurfaceOf(ElementVisit visit) =>
            visit.Layer != 0 && _surfaceOfLayer.TryGetValue(visit.Layer, out var surface) ? surface : _surfaces[0];

        /// <summary>Labels an annotated node that gave no label with the text directly inside it, and drops
        /// the texts that only repeat its label or value.</summary>
        private static void Finish(SemanticNode node)
        {
            if (node.Label == null)
            {
                var texts = node.ChildList.Where(c => c.Role == SemanticRole.Text).Select(c => c.Label).OfType<string>().ToList();
                if (texts.Count > 0)
                    node.Label = string.Join(" ", texts);
            }
            node.ChildList.RemoveAll(c => c.Role == SemanticRole.Text && c.Path == null && c.ChildList.Count == 0
                && (c.Label == node.Label || (node.Value != null && c.Label == node.Value)));
        }
    }
}
