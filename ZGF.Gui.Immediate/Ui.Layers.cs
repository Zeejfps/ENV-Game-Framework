namespace ZGF.Gui.Immediate;

public abstract partial class Ui
{
    private const int ModalZ = 500_000;
    private const int ModalZStep = 10_000;

    private enum InputBlock : byte
    {
        None,
        Keys,
        All,
    }

    private readonly record struct LayerEntry(int Id, bool Modal);

    private readonly record struct SavedFocus(int Layer, int Focused, bool TakesText);

    private List<LayerEntry> _layers = new();
    private List<LayerEntry> _previousLayers = new();
    private readonly List<InputBlock> _blockAbove = new();
    private readonly Stack<int> _openLayers = new();
    private readonly List<SavedFocus> _savedFocus = new();
    private readonly UiInput _keyless = new();
    private readonly UiInput _inert = new();
    private InputBlock _block;
    private int _modalCount;

    /// <summary>
    /// Opens an input layer — a menu, a popup that takes keys. While it is the topmost layer, keys
    /// and text reach only what is recorded inside it, so <see cref="Input"/> elsewhere has none.
    /// Keyboard focus is put aside when it opens and given back when it stops being recorded,
    /// unless something else took focus meanwhile. Layers stack in the order they are recorded.
    /// </summary>
    public Scope Layer(int id)
    {
        BeginLayer(id, false);
        return BeginScope(ScopeKind.Layer);
    }

    /// <summary>
    /// A modal <see cref="Layer"/>: an element covering the whole window above everything recorded
    /// outside it, hit-testable as <paramref name="id"/>, laid out by <paramref name="box"/> (give it
    /// a backdrop color and center its content with <see cref="Box.AlignX"/>/<see cref="Box.AlignY"/>).
    /// While it is the topmost layer, everything outside it also sees no pointer, buttons or wheel.
    /// </summary>
    public Scope Modal(int id, Box box = default)
    {
        BeginLayer(id, true);
        var layer = BeginScope(ScopeKind.Layer);
        var e = Describe(id, box with { Width = Sizing.Fixed(Width), Height = Sizing.Fixed(Height) }, null, null);
        e.Parent = 0;
        e.Floating = true;
        e.Z = ModalZ + _modalCount++ * ModalZStep;
        Push(e);
        BeginScope(ScopeKind.Element);
        return layer;
    }

    /// <summary>True while the pointer is over the element with <paramref name="id"/> and nothing
    /// above it, even while another element holds the pointer — for picking on release after a drag.</summary>
    public bool IsPointerOver(int id) => id != 0 && _hovered == id;

    private void BeginLayer(int id, bool modal)
    {
        _layers.Add(new LayerEntry(id, modal));
        var rank = IndexOfLayer(_previousLayers, id);
        if (rank < 0)
        {
            _savedFocus.Add(new SavedFocus(id, _focused, _focusTakesText));
            _focused = 0;
            rank = int.MaxValue;
        }
        _openLayers.Push(rank);
        _block = BlockAbove(rank);
    }

    private void EndLayer()
    {
        _openLayers.Pop();
        _block = BlockAbove(_openLayers.Count > 0 ? _openLayers.Peek() : -1);
    }

    private InputBlock BlockAbove(int rank) => rank == int.MaxValue ? InputBlock.None : _blockAbove[rank + 1];

    private void BeginLayers(UiInput input)
    {
        (_layers, _previousLayers) = (_previousLayers, _layers);
        _layers.Clear();
        _openLayers.Clear();
        _modalCount = 0;

        _blockAbove.Clear();
        for (var i = 0; i <= _previousLayers.Count; i++)
            _blockAbove.Add(InputBlock.None);
        for (var k = _previousLayers.Count - 1; k >= 0; k--)
        {
            var own = _previousLayers[k].Modal ? InputBlock.All : InputBlock.Keys;
            _blockAbove[k] = own > _blockAbove[k + 1] ? own : _blockAbove[k + 1];
        }
        _block = _blockAbove[0];

        _keyless.CopyPointerFrom(input);
        _inert.CopyInertFrom(input);
    }

    private void EndLayers()
    {
        for (var i = _savedFocus.Count - 1; i >= 0; i--)
        {
            var saved = _savedFocus[i];
            if (IndexOfLayer(_layers, saved.Layer) >= 0) continue;
            _savedFocus.RemoveAt(i);
            if (_focusClaimed) continue;
            _focused = saved.Focused;
            _focusTakesText = saved.TakesText;
        }
    }

    private bool LayersChanged()
    {
        if (_layers.Count != _previousLayers.Count) return true;
        for (var i = 0; i < _layers.Count; i++)
        {
            if (_layers[i].Id != _previousLayers[i].Id)
                return true;
        }
        return false;
    }

    private static int IndexOfLayer(List<LayerEntry> layers, int id)
    {
        for (var i = 0; i < layers.Count; i++)
        {
            if (layers[i].Id == id)
                return i;
        }
        return -1;
    }
}
