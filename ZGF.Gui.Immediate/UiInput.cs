using System.Text;
using ZGF.Desktop;
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

/// <summary>Everything that happened since the previous frame, plus the held state that carries
/// across frames. Coordinates are UI points, origin top-left.</summary>
public sealed class UiInput
{
    internal const int ButtonCount = 5;

    private readonly bool[] _down = new bool[ButtonCount];
    private readonly bool[] _pressed = new bool[ButtonCount];
    private readonly bool[] _released = new bool[ButtonCount];
    private readonly List<KeyStroke> _keys = new();
    private readonly StringBuilder _text = new();

    public PointF Pointer { get; private set; } = Offscreen;
    public float ScrollX { get; private set; }
    public float ScrollY { get; private set; }
    public KeyModifiers Modifiers { get; private set; }
    public string Text { get; private set; } = "";
    public IReadOnlyList<KeyStroke> Keys => _keys;
    public bool HasEvents { get; private set; }

    /// <summary>The platform asked open popups to close since the previous frame: the app lost
    /// focus, or a press landed outside every part of the app a frame sees.</summary>
    public bool PopupsDismissed { get; private set; }

    public bool IsDown(PointerButton button) => _down[(int)button];
    public bool WasPressed(PointerButton button) => _pressed[(int)button];
    public bool WasReleased(PointerButton button) => _released[(int)button];

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
        }
        Modifiers = KeyModifiers.None;
        HasEvents = true;
    }

    internal void Seal() => Text = _text.Length == 0 ? "" : _text.ToString();

    internal void BeginAfter(UiInput previous)
    {
        Array.Copy(previous._down, _down, ButtonCount);
        Array.Clear(_pressed);
        Array.Clear(_released);
        _keys.Clear();
        _text.Clear();
        Text = "";
        Pointer = previous.Pointer;
        Modifiers = previous.Modifiers;
        ScrollX = 0;
        ScrollY = 0;
        PopupsDismissed = false;
        HasEvents = false;
    }
}
