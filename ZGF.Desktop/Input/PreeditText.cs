namespace ZGF.Desktop.Input;

/// <summary>
/// One clause of a composition, as a range into <see cref="PreeditText.Text"/>. An IME divides the
/// composition into clauses it converts independently, and the platform draws them underlined; the
/// focused one is the clause the user is currently converting.
/// </summary>
public readonly record struct PreeditBlock(int Start, int Length);

/// <summary>
/// An in-flight IME composition: the text the user is composing but has not yet committed.
/// <para>
/// This is the IME's own formatted string, not a replay of the keystrokes that produced it — a
/// pinyin IME inserts syllable separators nobody typed ("ni'hao"). Render it exactly as given;
/// never reconstruct it from key events.
/// </para>
/// <para>
/// Composition text is not entered text. It must not reach the value of the field being edited:
/// only the committed text does, and that arrives separately on the normal character path. An
/// <see cref="IsEmpty"/> composition means the composition ended — it cannot say whether the user
/// committed or cancelled, so it means "drop the preedit" and nothing more.
/// </para>
/// </summary>
public sealed class PreeditText
{
    public static readonly PreeditText Empty = new(string.Empty, 0, [], -1);

    /// <summary>Offsets below are UTF-16 indices into this string, to match the rest of the text stack.</summary>
    public string Text { get; }

    public int Caret { get; }

    public IReadOnlyList<PreeditBlock> Blocks { get; }

    /// <summary>Index into <see cref="Blocks"/>, or -1 when the IME reports no focused clause.</summary>
    public int FocusedBlock { get; }

    public bool IsEmpty => Text.Length == 0;

    public PreeditText(string text, int caret, IReadOnlyList<PreeditBlock> blocks, int focusedBlock)
    {
        Text = text;
        Caret = caret;
        Blocks = blocks;
        FocusedBlock = focusedBlock;
    }
}
