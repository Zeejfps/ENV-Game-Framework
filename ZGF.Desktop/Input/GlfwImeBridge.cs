using System.Text;
using GLFW;

namespace ZGF.Desktop.Input;

/// <summary>
/// Binds a GLFW window's IME to the framework's composition event. Both window backends compose one
/// of these, so the preedit path is written once.
/// <para>
/// Degrades on its own when the loaded GLFW lacks either capability, so callers need no support
/// check: without the IM-support patch (<see cref="GlfwIme.IsSupported"/>) the app keeps working
/// but cannot compose CJK, and without <see cref="GlfwIme.IsTextInputFocusSupported"/> the IME
/// stays enabled window-wide as it did before that call existed.
/// </para>
/// </summary>
internal sealed class GlfwImeBridge
{
    private readonly Window _window;
    // GLFW keeps the raw pointer, so the delegate must outlive the callback registration.
    private readonly PreeditCallback? _preeditCallback;

    public event Action<PreeditText>? OnPreedit;

    public GlfwImeBridge(Window window)
    {
        _window = window;

        if (GlfwIme.IsSupported)
        {
            _preeditCallback = HandlePreedit;
            GlfwIme.SetPreeditCallback(window, _preeditCallback);
        }

        // GLFW treats a window as text-input-focused until told otherwise once, so a window that
        // only ever calls this on blur keeps the IME window-wide until its first blur. Arming here
        // rather than at the window-creation sites also means secondary and pooled popup windows
        // are covered by construction, and nothing an input system reset does can un-arm them.
        SetTextInputFocus(false);
    }

    /// <summary>
    /// Declares whether this window is editing text. With focus off the IME stops consuming
    /// keystrokes, so bare-letter shortcuts survive a CJK input method; with it on, composition
    /// works as normal.
    /// <para>
    /// This is the IME's routing, not its conversion mode. An earlier implementation toggled
    /// <c>GLFW_IME</c> instead, which is the input method's own Chinese-vs-alphanumeric setting —
    /// turning that off degraded Microsoft Pinyin to alphanumeric passthrough process-wide, so only
    /// the "on" direction was ever forwarded and the IME stayed enabled outside text fields. That
    /// was the wrong API rather than a broken one; this is the one GLFW provides for the job.
    /// </para>
    /// </summary>
    public void SetTextInputFocus(bool focused)
    {
        if (!GlfwIme.IsTextInputFocusSupported) return;
        GlfwIme.SetTextInputFocus(_window, focused);
    }

    /// <summary>Positions the OS candidate window against the caret. Coordinates are window-relative, top-left origin.</summary>
    public void SetCursorRectangle(int x, int y, int width, int height)
    {
        if (_preeditCallback == null) return;
        GlfwIme.SetPreeditCursorRectangle(_window, x, y, width, height);
    }

    /// <summary>Discards any in-flight composition without committing it.</summary>
    public void Reset()
    {
        if (_preeditCallback == null) return;
        GlfwIme.ResetPreeditText(_window);
    }

    private unsafe void HandlePreedit(Window window, int preeditCount, IntPtr preeditString, int blockCount,
        IntPtr blockSizes, int focusedBlock, int caret)
    {
        if (preeditCount <= 0 || preeditString == IntPtr.Zero)
        {
            OnPreedit?.Invoke(PreeditText.Empty);
            return;
        }

        var codePoints = (uint*)preeditString;
        var builder = new StringBuilder(preeditCount);
        // Where each code point begins once encoded as UTF-16, so the code-point offsets GLFW
        // reports (caret, block sizes) can be restated as UTF-16 offsets into the built string.
        // Astral characters take two chars, so the two indexings diverge.
        var utf16Offsets = new int[preeditCount + 1];
        Span<char> encoded = stackalloc char[2];

        for (var i = 0; i < preeditCount; i++)
        {
            utf16Offsets[i] = builder.Length;
            if (Rune.TryCreate(codePoints[i], out var rune))
                builder.Append(encoded[..rune.EncodeToUtf16(encoded)]);
        }

        utf16Offsets[preeditCount] = builder.Length;

        var blocks = BuildBlocks(blockCount, (int*)blockSizes, preeditCount, utf16Offsets);
        var caretUtf16 = utf16Offsets[Math.Clamp(caret, 0, preeditCount)];
        var focused = blocks.Length == 0 ? -1 : Math.Clamp(focusedBlock, -1, blocks.Length - 1);

        OnPreedit?.Invoke(new PreeditText(builder.ToString(), caretUtf16, blocks, focused));
    }

    private static unsafe PreeditBlock[] BuildBlocks(int blockCount, int* blockSizes, int preeditCount,
        int[] utf16Offsets)
    {
        if (blockCount <= 0 || blockSizes == null) return [];

        var blocks = new PreeditBlock[blockCount];
        var codePoint = 0;
        for (var i = 0; i < blockCount; i++)
        {
            var start = codePoint;
            // Clamped rather than trusted: a malformed block table must not index past the string.
            var end = Math.Clamp(codePoint + blockSizes[i], start, preeditCount);
            blocks[i] = new PreeditBlock(utf16Offsets[start], utf16Offsets[end] - utf16Offsets[start]);
            codePoint = end;
        }

        return blocks;
    }
}
