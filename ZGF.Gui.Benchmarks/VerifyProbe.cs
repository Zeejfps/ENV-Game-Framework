using System.Numerics;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui;

namespace ZGF.Gui.Benchmarks;

/// <summary>
/// Randomized correctness check for the draw-call batcher. Builds random scenes
/// of overlapping rects and text at varying z-index and clips, then verifies the
/// reconstructed draw order preserves paint order (z, then sequence) for every
/// pair of overlapping primitives. A batcher that reorders across kinds is only
/// correct if it never lets a later primitive that overlaps an earlier one draw
/// underneath it.
/// </summary>
static class VerifyProbe
{
    const int Width = 1280, Height = 720;

    readonly record struct Drawn(uint Tag, long Rank, float MinX, float MinY, float MaxX, float MaxY);

    public static void Run()
    {
        var fonts = new FreeTypeFontBackend();
        var font = fonts.LoadFontFromFile(ResolveFontPath(), 16);

        const int scenes = 20000;
        var failures = 0;
        var firstFailSeed = -1;
        var totalOverlapPairs = 0L;

        for (var seed = 0; seed < scenes; seed++)
        {
            var rng = new Random(seed);
            var canvas = new BenchCanvas(Width, Height, fonts, font) { Capture = true };
            var rank = new Dictionary<uint, long>();

            canvas.BeginFrame();
            var items = rng.Next(4, 12);
            uint tag = 1;
            for (var i = 0; i < items; i++)
            {
                var z = rng.Next(0, 3);
                rank[tag] = ((long)z << 24) | (uint)i;

                // Cluster into a small area to force frequent overlaps.
                var x = rng.Next(0, 240);
                var y = rng.Next(0, 160);
                var w = rng.Next(20, 120);
                var h = rng.Next(14, 60);

                var clipped = rng.Next(0, 3) == 0;
                if (clipped)
                    canvas.PushClip(new RectF(rng.Next(0, 200), rng.Next(0, 140), rng.Next(40, 160), rng.Next(30, 120)));

                if (rng.Next(0, 2) == 0)
                {
                    canvas.DrawRect(new DrawRectInputs
                    {
                        Position = new RectF(x, y, w, h),
                        Style = new RectStyle { BackgroundColor = tag },
                        ZIndex = z,
                    });
                }
                else
                {
                    canvas.DrawText(new DrawTextInputs
                    {
                        Position = new RectF(x, y, w, h),
                        Text = "Wgy",
                        Style = new TextStyle { TextColor = tag, FontSize = 16f },
                        ZIndex = z,
                    });
                }

                if (clipped)
                    canvas.PopClip();
                tag++;
            }
            canvas.EndFrame();

            var drawn = Reconstruct(canvas, rank);
            if (!CheckInvariant(drawn, ref totalOverlapPairs))
            {
                failures++;
                if (firstFailSeed < 0) firstFailSeed = seed;
            }
        }

        Console.WriteLine($"Verify: {scenes} scenes, {totalOverlapPairs} overlapping pairs checked, {failures} FAILED");
        if (failures > 0)
            Console.WriteLine($"  first failing seed: {firstFailSeed}");
        else
            Console.WriteLine("  PASS: paint order preserved for all overlapping primitives");
    }

    static List<Drawn> Reconstruct(BenchCanvas c, Dictionary<uint, long> rank)
    {
        var list = new List<Drawn>();
        foreach (var dc in c.CapturedDrawCalls)
        {
            for (var k = dc.InstanceStart; k < dc.InstanceStart + dc.InstanceCount; k++)
            {
                switch (dc.Kind)
                {
                    case RenderedCanvasBase.DrawKind.Rect:
                    {
                        var inst = c.CapturedRects[k];
                        Bounds(inst.Rect, 0f, inst.ClipIndex, c, out var a, out var b, out var d, out var e);
                        list.Add(new Drawn(inst.BgColor, rank[inst.BgColor], a, b, d, e));
                        break;
                    }
                    case RenderedCanvasBase.DrawKind.Glyph:
                    {
                        var inst = c.CapturedGlyphs[k];
                        Bounds(inst.Rect, inst.Rotation, inst.ClipIndex, c, out var a, out var b, out var d, out var e);
                        list.Add(new Drawn(inst.Color, rank[inst.Color], a, b, d, e));
                        break;
                    }
                }
            }
        }
        return list;
    }

    static void Bounds(Vector4 r, float rotation, uint clipIndex, BenchCanvas c,
        out float minx, out float miny, out float maxx, out float maxy)
    {
        minx = r.X; miny = r.Y; maxx = r.X + r.Z; maxy = r.Y + r.W;
        if (rotation != 0f)
        {
            var cx = (minx + maxx) * 0.5f;
            var cy = (miny + maxy) * 0.5f;
            var hw = (maxx - minx) * 0.5f;
            var hh = (maxy - miny) * 0.5f;
            var cs = MathF.Abs(MathF.Cos(rotation));
            var sn = MathF.Abs(MathF.Sin(rotation));
            var ex = hw * cs + hh * sn;
            var ey = hw * sn + hh * cs;
            minx = cx - ex; maxx = cx + ex; miny = cy - ey; maxy = cy + ey;
        }
        var clip = c.CapturedClips[(int)clipIndex];
        minx = MathF.Max(minx, clip.X); miny = MathF.Max(miny, clip.Y);
        maxx = MathF.Min(maxx, clip.Z); maxy = MathF.Min(maxy, clip.W);
    }

    static bool CheckInvariant(List<Drawn> d, ref long pairCount)
    {
        var ok = true;
        for (var i = 0; i < d.Count; i++)
        {
            if (d[i].MaxX <= d[i].MinX || d[i].MaxY <= d[i].MinY) continue;
            for (var j = i + 1; j < d.Count; j++)
            {
                if (d[i].Tag == d[j].Tag || d[i].Rank == d[j].Rank) continue;
                if (d[j].MaxX <= d[j].MinX || d[j].MaxY <= d[j].MinY) continue;
                var overlap = !(d[i].MaxX <= d[j].MinX || d[j].MaxX <= d[i].MinX ||
                                d[i].MaxY <= d[j].MinY || d[j].MaxY <= d[i].MinY);
                if (!overlap) continue;
                pairCount++;
                // i is before j in draw order (outer loop is draw order).
                // Require the earlier-painted (smaller rank) to be drawn first.
                if (d[i].Rank > d[j].Rank) ok = false;
            }
        }
        return ok;
    }

    static string ResolveFontPath()
    {
        const string rel = "ZGF.Gui/Assets/Fonts/Inter/Inter-Regular.ttf";
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            var candidate = Path.Combine(dir, rel);
            if (File.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        throw new FileNotFoundException(rel);
    }
}
