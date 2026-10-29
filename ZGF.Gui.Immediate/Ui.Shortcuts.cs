using System.Runtime.InteropServices;
using ZGF.Desktop;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate;

public abstract partial class Ui
{
    private const int PressesKept = 8;

    // The last keys pressed in frames before this one, oldest first, for a shortcut of two chords
    // whose first was pressed in an earlier frame than its second. Modifier keys pressed on their own
    // are left out, so letting go of Ctrl and pressing it again does not break Ctrl+M, Ctrl+M.
    private readonly List<KeyChordPress> _earlierPresses = new();
    private readonly List<KeyChordPress> _presses = new();

    /// <summary>Whether a key pressed this frame completes <paramref name="shortcut"/>, as
    /// <see cref="Input"/> sees the keys.</summary>
    public bool IsPressed(Shortcut shortcut)
    {
        var keys = Input.Keys;
        for (var i = 0; i < keys.Count; i++)
        {
            if (IsPressedAt(shortcut, i))
                return true;
        }
        return false;
    }

    /// <summary>Whether this frame's key at <paramref name="index"/> of <see cref="UiInput.Keys"/>
    /// completes <paramref name="shortcut"/>, for code that handles the keys in the order pressed.</summary>
    public bool IsPressedAt(Shortcut shortcut, int index)
    {
        var keys = Input.Keys;
        var stroke = keys[index];
        if (IsModifierKey(stroke.Key))
            return shortcut.Second == null && shortcut.First.Matches(stroke.Key, stroke.Modifiers);
        _presses.Clear();
        _presses.AddRange(_earlierPresses);
        for (var i = 0; i <= index; i++)
        {
            if (!IsModifierKey(keys[i].Key))
                _presses.Add(new KeyChordPress(keys[i].Key, keys[i].Modifiers));
        }
        return shortcut.CompletedBy(CollectionsMarshal.AsSpan(_presses));
    }

    /// <summary>Keeps the frame's keys, for a shortcut the next frame's keys complete.</summary>
    private void KeepPresses()
    {
        foreach (var stroke in _input.Keys)
        {
            if (!IsModifierKey(stroke.Key))
                _earlierPresses.Add(new KeyChordPress(stroke.Key, stroke.Modifiers));
        }
        if (_earlierPresses.Count > PressesKept)
            _earlierPresses.RemoveRange(0, _earlierPresses.Count - PressesKept);
    }

    private static bool IsModifierKey(KeyboardKey key) => key is >= KeyboardKey.LeftShift and <= KeyboardKey.RightSuper;
}
