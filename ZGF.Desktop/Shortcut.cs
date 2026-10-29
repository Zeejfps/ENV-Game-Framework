using System.Text;
using ZGF.KeyboardModule;

namespace ZGF.Desktop;

/// <summary>The modifiers a shortcut is held with, named for what they mean on every platform.</summary>
[Flags]
public enum ShortcutModifiers
{
    None = 0,

    /// <summary>The key app shortcuts are held with: Command on macOS, Ctrl elsewhere.</summary>
    Command = 1,

    /// <summary>The Control key itself on every platform, as in Ctrl+Tab: ⌃ on macOS.</summary>
    Control = 2,

    /// <summary>Alt, which macOS calls Option.</summary>
    Alt = 4,

    Shift = 8,
}

/// <summary>One press of <see cref="Key"/> with <see cref="Modifiers"/> held.</summary>
public readonly record struct KeyChord(KeyboardKey Key, ShortcutModifiers Modifiers)
{
    private const KeyModifiers Chorded = KeyModifiers.Control | KeyModifiers.Super | KeyModifiers.Alt | KeyModifiers.Shift;

    /// <summary>Whether pressing <paramref name="key"/> with <paramref name="modifiers"/> held is this
    /// chord: the key, with exactly these modifiers among Ctrl, Command, Alt and Shift.</summary>
    public bool Matches(KeyboardKey key, KeyModifiers modifiers) => Matches(key, modifiers, OperatingSystem.IsMacOS());

    internal bool Matches(KeyboardKey key, KeyModifiers modifiers, bool mac) =>
        key == Key && (modifiers & Chorded) == HeldOn(mac);

    /// <summary>The window system's modifiers this chord is held with.</summary>
    public KeyModifiers Held => HeldOn(OperatingSystem.IsMacOS());

    internal KeyModifiers HeldOn(bool mac)
    {
        var held = KeyModifiers.None;
        if (Modifiers.HasFlag(ShortcutModifiers.Command))
            held |= mac ? KeyModifiers.Super : KeyModifiers.Control;
        if (Modifiers.HasFlag(ShortcutModifiers.Control))
            held |= KeyModifiers.Control;
        if (Modifiers.HasFlag(ShortcutModifiers.Alt))
            held |= KeyModifiers.Alt;
        if (Modifiers.HasFlag(ShortcutModifiers.Shift))
            held |= KeyModifiers.Shift;
        return held;
    }

    /// <summary>The character <see cref="Key"/> types without Shift, for a key that types one; else null.</summary>
    public char? Character => Key switch
    {
        >= KeyboardKey.A and <= KeyboardKey.Z => (char)('a' + (Key - KeyboardKey.A)),
        >= KeyboardKey.Alpha0 and <= KeyboardKey.Alpha9 => (char)('0' + (Key - KeyboardKey.Alpha0)),
        KeyboardKey.Apostrophe => '\'',
        KeyboardKey.Comma => ',',
        KeyboardKey.Period => '.',
        KeyboardKey.Slash => '/',
        KeyboardKey.SemiColon => ';',
        KeyboardKey.Equals => '=',
        KeyboardKey.Minus => '-',
        KeyboardKey.LeftBracket => '[',
        KeyboardKey.RightBracket => ']',
        KeyboardKey.Backslash => '\\',
        KeyboardKey.GraveAccent => '`',
        _ => null,
    };

    /// <summary>The number of the function key <see cref="Key"/> is, F1 being 1; 0 for any other key.</summary>
    public int FunctionKey => Key is >= KeyboardKey.F1 and <= KeyboardKey.F25 ? Key - KeyboardKey.F1 + 1 : 0;

    internal string Label(bool mac)
    {
        var text = new StringBuilder();
        if (mac)
        {
            // macOS writes the modifiers as symbols, in the order Control, Option, Shift, Command.
            if (Modifiers.HasFlag(ShortcutModifiers.Control)) text.Append('⌃');
            if (Modifiers.HasFlag(ShortcutModifiers.Alt)) text.Append('⌥');
            if (Modifiers.HasFlag(ShortcutModifiers.Shift)) text.Append('⇧');
            if (Modifiers.HasFlag(ShortcutModifiers.Command)) text.Append('⌘');
            return text.Append(MacKeyName()).ToString();
        }
        if ((Modifiers & (ShortcutModifiers.Command | ShortcutModifiers.Control)) != 0) text.Append("Ctrl+");
        if (Modifiers.HasFlag(ShortcutModifiers.Alt)) text.Append("Alt+");
        if (Modifiers.HasFlag(ShortcutModifiers.Shift)) text.Append("Shift+");
        return text.Append(KeyName()).ToString();
    }

    private string KeyName() => Key switch
    {
        KeyboardKey.LeftArrow => "Left",
        KeyboardKey.RightArrow => "Right",
        KeyboardKey.UpArrow => "Up",
        KeyboardKey.DownArrow => "Down",
        KeyboardKey.Escape => "Esc",
        KeyboardKey.PageUp => "PgUp",
        KeyboardKey.PageDown => "PgDn",
        _ when Character is { } c => char.ToUpperInvariant(c).ToString(),
        _ => Key.ToString(),
    };

    private string MacKeyName() => Key switch
    {
        KeyboardKey.LeftArrow => "←",
        KeyboardKey.RightArrow => "→",
        KeyboardKey.UpArrow => "↑",
        KeyboardKey.DownArrow => "↓",
        KeyboardKey.Enter => "↩",
        KeyboardKey.Tab => "⇥",
        KeyboardKey.Delete => "⌦",
        KeyboardKey.Backspace => "⌫",
        KeyboardKey.Escape => "⎋",
        KeyboardKey.Space => "Space",
        _ => KeyName(),
    };
}

/// <summary>A key that does something: one <see cref="KeyChord"/>, or two in a row, as Ctrl+M, M. Holds
/// its <see cref="Label"/> in the platform's spelling, <c>⇧⌘E</c> on macOS and <c>Ctrl+Shift+E</c>
/// elsewhere.</summary>
public sealed record Shortcut
{
    private Shortcut(KeyChord first, KeyChord? second)
    {
        First = first;
        Second = second;
        Label = LabelOn(OperatingSystem.IsMacOS());
    }

    /// <summary>The window system's modifier <see cref="ShortcutModifiers.Command"/> stands for, as held
    /// with a click to open in a new tab: Super (⌘) on macOS, Control elsewhere.</summary>
    public static KeyModifiers CommandModifier { get; } = OperatingSystem.IsMacOS() ? KeyModifiers.Super : KeyModifiers.Control;

    public KeyChord First { get; }

    /// <summary>The chord after <see cref="First"/>, for a shortcut of two; null for one of one.</summary>
    public KeyChord? Second { get; }

    public string Label { get; }

    /// <summary>Pressing <paramref name="key"/> with <paramref name="modifiers"/> held.</summary>
    public static Shortcut Key(KeyboardKey key, ShortcutModifiers modifiers = ShortcutModifiers.None) =>
        new(new KeyChord(key, modifiers), null);

    /// <summary>Pressing <paramref name="key"/> with the command key held, and <paramref name="also"/>.</summary>
    public static Shortcut Command(KeyboardKey key, ShortcutModifiers also = ShortcutModifiers.None) =>
        Key(key, ShortcutModifiers.Command | also);

    /// <summary>This shortcut's chord, then <paramref name="key"/>, pressed with or without the first
    /// chord's modifiers still held.</summary>
    public Shortcut Then(KeyboardKey key)
    {
        if (Second != null)
            throw new InvalidOperationException($"{Label} is already two chords.");
        return new Shortcut(First, new KeyChord(key, ShortcutModifiers.None));
    }

    /// <summary>Whether the last of <paramref name="presses"/>, keys pressed in order, completes this
    /// shortcut. A press that completed a shortcut of two chords does not also begin it again, so
    /// Ctrl+M, Ctrl+M, Ctrl+M completes Ctrl+M, M once.</summary>
    public bool CompletedBy(ReadOnlySpan<KeyChordPress> presses) => CompletedBy(presses, OperatingSystem.IsMacOS());

    internal bool CompletedBy(ReadOnlySpan<KeyChordPress> presses, bool mac)
    {
        if (presses.IsEmpty)
            return false;
        var last = presses[^1];
        if (Second is not { } second)
            return First.Matches(last.Key, last.Modifiers, mac);
        if (presses.Length < 2)
            return false;
        var stillHeld = second with { Modifiers = second.Modifiers | First.Modifiers };
        if (!second.Matches(last.Key, last.Modifiers, mac) && !stillHeld.Matches(last.Key, last.Modifiers, mac))
            return false;
        var before = presses[^2];
        if (!First.Matches(before.Key, before.Modifiers, mac))
            return false;
        return !CompletedBy(presses[..^1], mac);
    }

    internal string LabelOn(bool mac)
    {
        if (Second is not { } second)
            return First.Label(mac);
        return First.Label(mac) + ", " + second.Label(mac);
    }

    public override string ToString() => Label;
}

/// <summary>A key pressed with modifiers held, as the window system reports it.</summary>
public readonly record struct KeyChordPress(KeyboardKey Key, KeyModifiers Modifiers);
