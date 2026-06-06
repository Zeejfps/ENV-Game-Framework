using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

using GlyphInstance = ZGF.Gui.RenderedCanvasBase.GlyphInstance;

[MemoryDiagnoser]
public class ArraysMatchBenchmarks
{
    // Sweep across realistic per-frame glyph counts. 16384 is MaxGlyphs.
    [Params(256, 2048, 16384)]
    public int Count;

    private GlyphInstance[] _cur = null!;
    private GlyphInstance[] _prev = null!;

    [GlobalSetup]
    public void Setup()
    {
        _cur = new GlyphInstance[Count];
        _prev = new GlyphInstance[Count];
        for (var i = 0; i < Count; i++)
        {
            var g = new GlyphInstance
            {
                Rect = new Vector4(i, i * 2, 12, 16),
                AtlasUV = new Vector4(0.1f, 0.2f, 0.3f, 0.4f),
                Color = 0xFFFFFFFF,
                ClipIndex = (uint)(i % 8),
                Rotation = 0f,
            };
            _cur[i] = g;
            _prev[i] = g;
        }
        // Worst case for a dirty check: arrays are equal, so the full length
        // is scanned with no early-out. This is the every-idle-frame path.
    }

    [Benchmark(Baseline = true)]
    public bool OldPerElementEquals() => OldMatch(_cur, Count, _prev, Count);

    [Benchmark]
    public bool NewSequenceEqual() => NewMatch(_cur, Count, _prev, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool OldMatch<T>(T[] cur, int curCount, T[] prev, int prevCount) where T : IEquatable<T>
    {
        if (curCount != prevCount) return false;
        for (var i = 0; i < curCount; i++)
            if (!cur[i].Equals(prev[i])) return false;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool NewMatch<T>(T[] cur, int curCount, T[] prev, int prevCount) where T : unmanaged
    {
        if (curCount != prevCount) return false;
        var a = MemoryMarshal.AsBytes(cur.AsSpan(0, curCount));
        var b = MemoryMarshal.AsBytes(prev.AsSpan(0, curCount));
        return a.SequenceEqual(b);
    }
}
