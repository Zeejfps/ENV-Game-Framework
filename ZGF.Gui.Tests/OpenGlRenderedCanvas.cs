using ZGF.Geometry;

namespace ZGF.Gui.Tests;

readonly record struct DrawCommand2(int Sequence, int ZIndex, int ClipIndex, CommandKind Kind)
{
    // --- BITS & SHIFTS CONFIGURATION ---
    // Note: These must sum to 64 or less
    private const int SequenceBits = 24;
    private const int KindBits = 8;
    private const int ClipIndexBits = 16;
    private const int ZIndexBits = 16;
    
    // These offsets are used to handle potentially negative ZIndex values.
    // We shift the range from [-32768, 32767] to [0, 65535]
    private const int ZIndexOffset = 32768; 

    // Calculate the shift amounts
    private const int KindShift = SequenceBits;
    private const int ClipIndexShift = KindShift + KindBits;
    private const int ZIndexShift = ClipIndexShift + ClipIndexBits;

    /// <summary>
    /// Generates a 64-bit sort key for this command.
    /// Commands can be sorted efficiently by sorting these keys.
    /// The sort order is: ZIndex -> ClipIndex -> Kind -> Sequence.
    /// </summary>
    public long GetSortKey()
    {
        // Important: We must mask the values to ensure they don't overflow their assigned bitfields.
        // For example, Sequence must not be larger than (1 << 24) - 1.
        // In a real-world renderer, you would add validation or use types that enforce these limits.
        
        var s = (long)Sequence;
        var k = (long)Kind;
        var c = (long)ClipIndex;
        var z = (long)ZIndex + ZIndexOffset; // Apply offset for negative numbers

        // Shift each component into its designated position and combine with bitwise OR
        return (z << ZIndexShift) | (c << ClipIndexShift) | (k << KindShift) | s;
    }
}

sealed class DrawCommandComparer : IComparer<DrawCommand2>
{
    public int Compare(DrawCommand2 x, DrawCommand2 y)
    {
        var xKey = x.GetSortKey();
        var yKey = y.GetSortKey();
        return xKey.CompareTo(yKey);
    }
}

public sealed class OpenGlRenderedCanvas : ICanvas
{
    private readonly SortedSet<DrawCommand2> _commands = new(new DrawCommandComparer());
    private readonly Dictionary<int, DrawRectInputs> _drawRectInputs = new();
    private readonly Dictionary<int, DrawTextInputs> _drawTextInputs = new();
    private readonly Dictionary<int, DrawImageInputs> _drawImageInputs = new();
    private readonly List<RectF> _clipRects = new();
    
    private readonly Stack<RectF> _clipStack = new();
    
    public void BeginFrame()
    {
        _commands.Clear();
        _drawRectInputs.Clear();
        _drawTextInputs.Clear();
        _drawImageInputs.Clear();
        _clipRects.Clear();
        _clipStack.Clear();
    }

    public void EndFrame()
    {
        
    }
    
    public void DrawRect(in DrawRectInputs inputs)
    {
        var cmd = new DrawCommand2
        {
            ZIndex = inputs.ZIndex,
            Sequence = _commands.Count,
            ClipIndex = _clipRects.Count - 1,
            Kind = CommandKind.DrawRect,
        };
        _commands.Add(cmd);
        _drawRectInputs.Add(cmd.Sequence, inputs);
    }

    public void DrawText(in DrawTextInputs inputs)
    {
        var cmd = new DrawCommand2
        {
            ZIndex = inputs.ZIndex,
            Sequence = _commands.Count,
            ClipIndex = _clipRects.Count - 1,
            Kind = CommandKind.DrawText,
        };
        _commands.Add(cmd);
        _drawTextInputs.Add(cmd.Sequence, inputs);
    }

    public void DrawImage(in DrawImageInputs inputs)
    {
        var cmd = new DrawCommand2
        {
            ZIndex = inputs.ZIndex,
            Sequence = _commands.Count,
            ClipIndex = _clipRects.Count - 1,
            Kind = CommandKind.DrawImage,
        };
        _commands.Add(cmd);
        _drawImageInputs.Add(cmd.Sequence, inputs);
    }

    public bool TryGetClip(out RectF rect)
    {
        return _clipStack.TryPeek(out rect);
    }

    public void PushClip(RectF rect)
    {
        _clipStack.Push(rect);
        _clipRects.Add(rect);
    }

    public void PopClip()
    {
        _clipStack.Pop();
    }

    public float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style)
    {
        throw new NotImplementedException();
    }

    public float MeasureTextHeight(ReadOnlySpan<char> text, TextStyle style)
    {
        throw new NotImplementedException();
    }

    public Size GetImageSize(string imageId)
    {
        throw new NotImplementedException();
    }

    public int GetImageWidth(string imageId)
    {
        throw new NotImplementedException();
    }

    public int GetImageHeight(string imageId)
    {
        throw new NotImplementedException();
    }
}