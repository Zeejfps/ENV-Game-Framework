using System.Runtime.InteropServices;
using System.Text;
using ZGF.Desktop;
using ZGF.Desktop.Input;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate;

public enum PointerButton
{
    Left,
    Right,
    Middle,
    Back,
    Forward,
}

public readonly record struct KeyStroke(KeyboardKey Key, bool IsRepeat, KeyModifiers Modifiers);

/// <summary>This frame's key presses, in order. <c>foreach</c> over it allocates nothing.</summary>
public readonly struct KeyStrokes
{
    private readonly List<KeyStroke> _keys;

    internal KeyStrokes(List<KeyStroke> keys) => _keys = keys;

    public int Count => _keys.Count;

    public KeyStroke this[int index] => _keys[index];

    public ReadOnlySpan<KeyStroke> AsSpan() => CollectionsMarshal.AsSpan(_keys);

    public ReadOnlySpan<KeyStroke>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();
}

/// <summary>Everything that happened since the previous frame, plus the held state that carries
/// across frames. Coordinates are UI points, origin top-left.</summary>
public sealed class UiInput
{
    internal const int ButtonCount = 5;

    private readonly bool[] _down = new bool[ButtonCount];
    private readonly bool[] _pressed = new bool[ButtonCount];
    private readonly bool[] _released = new bool[ButtonCount];
    private readonly bool[] _cancelled = new bool[ButtonCount];
    private readonly List<KeyStroke> _keys = new();
    private readonly StringBuilder _text = new();

    public PointF Pointer { get; private set; } = Offscreen;
    public float ScrollX { get; private set; }
    public float ScrollY { get; private set; }
    public KeyModifiers Modifiers { get; private set; }
    public string Text { get; private set; } = "";
    public KeyStrokes Keys => new(_keys);
    public bool HasEvents { get; private set; }

    /// <summary>The IME composition in flight, kept across frames until the platform replaces or
    /// ends it. It is not entered text: what the user commits arrives on <see cref="Text"/>.</summary>
    public PreeditText Composition { get; private set; } = PreeditText.Empty;

    /// <summary>The platform asked open popups to close since the previous frame: the app lost
    /// focus, or a press landed outside every part of the app a frame sees.</summary>
    public bool PopupsDismissed { get; private set; }

    public bool IsDown(PointerButton button) => _down[(int)button];
    /// <summary>Whether <paramref name="button"/> went down this frame, wherever the pointer is —
    /// the app-wide hook for <see cref="PointerButton.Back"/>/<see cref="PointerButton.Forward"/>
    /// navigation. <see cref="Interaction.ClickedWith"/> answers only for the element under the pointer.</summary>
    public bool WasPressed(PointerButton button) => _pressed[(int)button];
    public bool WasReleased(PointerButton button) => _released[(int)button];

    /// <summary>True when the release this frame came from the app losing focus rather than the
    /// user: it ends drags but must not click or pick anything.</summary>
    public bool WasCancelled(PointerButton button) => _cancelled[(int)button];

    internal bool AnyDown => Array.IndexOf(_down, true) >= 0;

    public bool IsKeyPressed(KeyboardKey key)
    {
        foreach (var stroke in _keys)
        {
            if (stroke.Key == key)
                return true;
        }
        return false;
    }

    /// <summary>Whether <paramref name="key"/> was pressed this frame; removes it, so nothing
    /// that reads the input afterwards sees it. Don't call it while iterating <see cref="Keys"/>.</summary>
    public bool TakeKey(KeyboardKey key)
    {
        var taken = false;
        for (var i = _keys.Count - 1; i >= 0; i--)
        {
            if (_keys[i].Key != key) continue;
            _keys.RemoveAt(i);
            taken = true;
        }
        return taken;
    }

    internal static PointF Offscreen => new(float.MinValue, float.MinValue);

    internal void MovePointer(PointF point)
    {
        if (Pointer == point) return;
        Pointer = point;
        HasEvents = true;
    }

    internal void Button(int glfwButton, InputAction action, KeyModifiers modifiers)
    {
        if (glfwButton is < 0 or >= ButtonCount) return;
        Modifiers = modifiers;
        if (action == InputAction.Press)
        {
            _down[glfwButton] = true;
            _pressed[glfwButton] = true;
        }
        else if (action == InputAction.Release)
        {
            _down[glfwButton] = false;
            _released[glfwButton] = true;
        }
        HasEvents = true;
    }

    internal void Key(KeyboardKey key, InputAction action, KeyModifiers modifiers)
    {
        Modifiers = modifiers;
        if (action != InputAction.Release)
            _keys.Add(new KeyStroke(key, action == InputAction.Repeat, modifiers));
        HasEvents = true;
    }

    internal void Character(uint codePoint)
    {
        if (!Rune.IsValid(codePoint)) return;
        _text.Append(new Rune(codePoint).ToString());
        HasEvents = true;
    }

    internal void Compose(PreeditText preedit)
    {
        Composition = preedit;
        HasEvents = true;
    }

    internal void Scroll(double x, double y)
    {
        ScrollX += (float)x;
        ScrollY += (float)y;
        HasEvents = true;
    }

    internal void DismissPopups()
    {
        PopupsDismissed = true;
        HasEvents = true;
    }

    internal void ReleaseAll()
    {
        for (var i = 0; i < ButtonCount; i++)
        {
            if (!_down[i]) continue;
            _down[i] = false;
            _released[i] = true;
            _cancelled[i] = true;
        }
        Modifiers = KeyModifiers.None;
        HasEvents = true;
    }

    internal void CopyPointerFrom(UiInput source)
    {
        Array.Copy(source._down, _down, ButtonCount);
        Array.Copy(source._pressed, _pressed, ButtonCount);
        Array.Copy(source._released, _released, ButtonCount);
        Array.Copy(source._cancelled, _cancelled, ButtonCount);
        _keys.Clear();
        _text.Clear();
        Text = "";
        Composition = PreeditText.Empty;
        Pointer = source.Pointer;
        ScrollX = source.ScrollX;
        ScrollY = source.ScrollY;
        Modifiers = source.Modifiers;
        PopupsDismissed = source.PopupsDismissed;
        HasEvents = source.HasEvents;
    }

    internal void CopyInertFrom(UiInput source)
    {
        Array.Clear(_down);
        Array.Clear(_pressed);
        Array.Clear(_released);
        Array.Clear(_cancelled);
        _keys.Clear();
        _text.Clear();
        Text = "";
        Composition = PreeditText.Empty;
        Pointer = Offscreen;
        ScrollX = 0;
        ScrollY = 0;
        Modifiers = KeyModifiers.None;
        PopupsDismissed = source.PopupsDismissed;
        HasEvents = source.HasEvents;
    }

    internal void Seal() => Text = _text.Length == 0 ? "" : _text.ToString();

    internal void BeginAfter(UiInput previous)
    {
        Array.Copy(previous._down, _down, ButtonCount);
        Array.Clear(_pressed);
        Array.Clear(_released);
        Array.Clear(_cancelled);
        _keys.Clear();
        _text.Clear();
        Text = "";
        Composition = previous.Composition;
        Pointer = previous.Pointer;
        Modifiers = previous.Modifiers;
        ScrollX = 0;
        ScrollY = 0;
        PopupsDismissed = false;
        HasEvents = false;
    }
}
