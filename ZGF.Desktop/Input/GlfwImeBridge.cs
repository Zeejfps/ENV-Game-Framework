using System.Text;
using GLFW;

namespace ZGF.Desktop.Input;

/// <summary>
/// Binds a GLFW window's IME to the framework's composition event. Both window backends compose one
/// of these, so the preedit path is written once.
/// <para>
/// Inert when the loaded GLFW has no IM-support patch (<see cref="GlfwIme.IsSupported"/>): the app
/// keeps working, it just cannot compose CJK. Callers need no support check of their own.
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
        if (!GlfwIme.IsSupported) return;

        _preeditCallback = HandlePreedit;
        GlfwIme.SetPreeditCallback(window, _preeditCallback);
        // The IME stays off until a text field asks for it. Left on, a Japanese IME would start
        // composing on the keys that drive list navigation.
        SetEnabled(false);
    }

    public void SetEnabled(bool enabled)
    {
        if (_preeditCallback == null) return;
        Glfw.SetInputMode(_window, GlfwIme.Ime, enabled ? 1 : 0);
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
