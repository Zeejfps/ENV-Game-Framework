using System.Runtime.InteropServices;
using FreeTypeSharp;
using HarfBuzzSharp;
using static FreeTypeSharp.FT;
using static FreeTypeSharp.FT_LOAD;
using HbBuffer = HarfBuzzSharp.Buffer;

namespace ZGF.Fonts;

public sealed unsafe class FreeTypeFontBackend : IGlyphSource
{
    private const int ShapeCacheGenCap = 256;

    private readonly FreeTypeLibrary _library;
    private readonly GlyphAtlas _atlas;
    private readonly Dictionary<long, GlyphRenderInfo> _glyphCache = new();
    private readonly List<FontEntry> _fonts = new();
    private bool _disposed;

    public FreeTypeFontBackend(int atlasWidth = 2048, int atlasHeight = 2048)
    {
        _library = new FreeTypeLibrary();
        _atlas = new GlyphAtlas(atlasWidth, atlasHeight);
    }

    public int AtlasWidth => _atlas.Width;
    public int AtlasHeight => _atlas.Height;
    public ReadOnlySpan<byte> AtlasPixels => _atlas.Pixels;
    public bool AtlasDirty => _atlas.Dirty;
    public AtlasDirtyRect DirtyRect => _atlas.DirtyRect;
    public void ClearDirty() => _atlas.ClearDirty();

    public FontHandle LoadFontFromFile(string path, int pixelSize)
    {
        var bytes = File.ReadAllBytes(path);
        return LoadFontFromMemory(bytes, pixelSize);
    }

    public FontHandle LoadFontFromMemory(byte[] data, int pixelSize)
    {
        ThrowIfDisposed();

        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        FT_FaceRec_* face = null;
        FT_Error err;
        fixed (byte* dataPtr = data)
        {
            err = FT_New_Memory_Face(_library.Native, dataPtr, (IntPtr)data.Length, IntPtr.Zero, &face);
        }
        if (err != FT_Error.FT_Err_Ok)
        {
            handle.Free();
            throw new FreeTypeException(err);
        }

        err = FT_Set_Pixel_Sizes(face, 0, (uint)pixelSize);
        if (err != FT_Error.FT_Err_Ok)
        {
            FT_Done_Face(face);
            handle.Free();
            throw new FreeTypeException(err);
        }

        var hbBlob = new Blob(handle.AddrOfPinnedObject(), data.Length, MemoryMode.ReadOnly);
        var hbFace = new Face(hbBlob, 0);
        hbBlob.Dispose();
        var hbFont = new Font(hbFace);
        hbFont.SetFunctionsOpenType();
        hbFont.SetScale(pixelSize * 64, pixelSize * 64);
        var hbBuffer = new HbBuffer();

        var entry = new FontEntry
        {
            Face = face,
            PinnedData = handle,
            SourceData = data,
            PixelSize = pixelSize,
            HasKerning = (face->face_flags.ToInt64() & 0x02) != 0,
            HbFace = hbFace,
            HbFont = hbFont,
            HbBuffer = hbBuffer,
        };
        _fonts.Add(entry);
        return new FontHandle(_fonts.Count);
    }

    /// Returns a handle to the same font at <paramref name="pixelSize"/>, reusing the
    /// source byte buffer of <paramref name="baseFont"/>. Sized variants are cached on
    /// the source entry, so repeated calls for the same size return the same handle.
    public FontHandle GetSizedVariant(FontHandle baseFont, int pixelSize)
    {
        ThrowIfDisposed();
        var entry = GetEntry(baseFont);
        if (entry.PixelSize == pixelSize)
            return baseFont;

        entry.SizeVariants ??= new Dictionary<int, FontHandle>();
        if (entry.SizeVariants.TryGetValue(pixelSize, out var cached))
            return cached;

        var variant = LoadFontFromMemory(entry.SourceData, pixelSize);
        entry.SizeVariants[pixelSize] = variant;
        // Preserve the embolden flag across sized variants so e.g. a bold 18px variant of
        // a bold 16px font is still bold.
        if (entry.IsEmboldened)
            GetEntry(variant).IsEmboldened = true;
        return variant;
    }

    /// Returns a sibling handle that renders the same font with synthesized bold via
    /// FT_GlyphSlot_Embolden. Cached per source entry. If <paramref name="baseFont"/> is
    /// already emboldened the same handle is returned.
    public FontHandle GetEmboldenedVariant(FontHandle baseFont)
    {
        ThrowIfDisposed();
        var entry = GetEntry(baseFont);
        if (entry.IsEmboldened)
            return baseFont;
        if (entry.EmboldenedVariant.HasValue)
            return entry.EmboldenedVariant.Value;

        var variant = LoadFontFromMemory(entry.SourceData, entry.PixelSize);
        GetEntry(variant).IsEmboldened = true;
        entry.EmboldenedVariant = variant;
        return variant;
    }

    public FontMetrics GetMetrics(FontHandle font)
    {
        ThrowIfDisposed();
        var entry = GetEntry(font);
        ActivateSize(entry);

        var m = entry.Face->size->metrics;
        var ascender = m.ascender.ToInt64() / 64f;
        var descender = m.descender.ToInt64() / 64f;
        var height = m.height.ToInt64() / 64f;
        return new FontMetrics(ascender, descender, height);
    }

    public uint GetGlyphIndex(FontHandle font, int codePoint)
    {
        ThrowIfDisposed();
        var entry = GetEntry(font);
        return FT_Get_Char_Index(entry.Face, (UIntPtr)(uint)codePoint);
    }

    public bool TryGetGlyph(FontHandle font, uint glyphIndex, out GlyphRenderInfo info)
    {
        ThrowIfDisposed();
        var entry = GetEntry(font);

        var key = MakeKey(font.Id, glyphIndex);
        if (_glyphCache.TryGetValue(key, out info))
            return true;

        info = default;
        if (glyphIndex == 0)
            return false;

        ActivateSize(entry);

        FT_Error err;
        if (entry.IsEmboldened)
        {
            // Load the outline first, embolden it, then render. Bitmap-emboldening (calling
            // embolden after FT_LOAD_RENDER) also works but produces a blurrier result.
            err = FT_Load_Glyph(entry.Face, glyphIndex, FT_LOAD_TARGET_NORMAL);
            if (err != FT_Error.FT_Err_Ok)
                return false;
            FT_GlyphSlot_Embolden(entry.Face->glyph);
            err = FT_Render_Glyph(entry.Face->glyph, FT_Render_Mode_.FT_RENDER_MODE_NORMAL);
            if (err != FT_Error.FT_Err_Ok)
                return false;
        }
        else
        {
            err = FT_Load_Glyph(entry.Face, glyphIndex, FT_LOAD_RENDER | FT_LOAD_TARGET_NORMAL);
            if (err != FT_Error.FT_Err_Ok)
                return false;
        }

        var slot = entry.Face->glyph;
        var bm = slot->bitmap;
        var width = (int)bm.width;
        var height = (int)bm.rows;
        var advance = slot->advance.x.ToInt64() / 64f;

        if (width == 0 || height == 0)
        {
            info = new GlyphRenderInfo(slot->bitmap_left, slot->bitmap_top, 0, 0, advance, 0, 0);
            _glyphCache[key] = info;
            return true;
        }

        if (bm.pixel_mode != FT_Pixel_Mode_.FT_PIXEL_MODE_GRAY)
            return false;

        if (!_atlas.TryReserve(width, height, out var ax, out var ay))
            return false;

        // Atlas is Y-up: row 0 = bottom of texture. Flip FreeType bitmap rows
        // (which are typically top-down) so the atlas matches the canvas's Y-up
        // sampling convention.
        var pitch = bm.pitch;
        var absPitch = pitch >= 0 ? pitch : -pitch;
        for (var outRow = 0; outRow < height; outRow++)
        {
            byte* srcRow;
            if (pitch > 0)
            {
                var imageRow = height - 1 - outRow;
                srcRow = bm.buffer + imageRow * pitch;
            }
            else if (pitch < 0)
            {
                srcRow = bm.buffer + outRow * pitch;
            }
            else
            {
                srcRow = bm.buffer;
            }
            _atlas.Blit(ax, ay + outRow, width, 1, srcRow, absPitch);
        }

        info = new GlyphRenderInfo(slot->bitmap_left, slot->bitmap_top, width, height, advance, ax, ay);
        _glyphCache[key] = info;
        return true;
    }

    public float GetKerning(FontHandle font, uint prevGlyphIndex, uint glyphIndex)
    {
        ThrowIfDisposed();
        if (prevGlyphIndex == 0 || glyphIndex == 0)
            return 0f;

        var entry = GetEntry(font);
        if (!entry.HasKerning)
            return 0f;

        ActivateSize(entry);

        FT_Vector_ delta;
        var err = FT_Get_Kerning(entry.Face, prevGlyphIndex, glyphIndex, FT_Kerning_Mode_.FT_KERNING_DEFAULT, &delta);
        if (err != FT_Error.FT_Err_Ok)
            return 0f;
        return delta.x.ToInt64() / 64f;
    }

    public int ShapeText(FontHandle font, ReadOnlySpan<char> text, Span<ShapedGlyph> output)
        => ShapeText(font, text, output, FontFeatureSet.None);

    public int ShapeText(FontHandle font, ReadOnlySpan<char> text, Span<ShapedGlyph> output, in FontFeatureSet features)
    {
        ThrowIfDisposed();
        if (text.Length == 0)
            return 0;

        var entry = GetEntry(font);
        var sig = features.Signature;

        // Shaped runs are position-independent and immutable for a given (font, text, features),
        // so cache them and skip HarfBuzz on repeat lines (the common per-frame case). Each
        // distinct feature set gets its own cache bucket so e.g. tnum and non-tnum runs of the
        // same text never collide; sig 0 (the empty set) is the original, untouched bucket.
        if (TryGetCachedShape(entry, sig, text, out var cached))
            return CopyShaped(cached, output);

        var buf = entry.HbBuffer!;
        var hbFont = entry.HbFont!;

        buf.ClearContents();
        buf.AddUtf16(text);
        buf.GuessSegmentProperties();
        hbFont.Shape(buf, BuildHbFeatures(features));

        var infos = buf.GetGlyphInfoSpan();
        var positions = buf.GetGlyphPositionSpan();
        var shaped = new ShapedGlyph[infos.Length];

        for (var i = 0; i < infos.Length; i++)
        {
            var pos = positions[i];
            shaped[i] = new ShapedGlyph(
                infos[i].Codepoint,
                pos.XOffset / 64f,
                pos.YOffset / 64f,
                pos.XAdvance / 64f,
                pos.YAdvance / 64f,
                (int)infos[i].Cluster);
        }

        PutShape(entry, sig, text.ToString(), shaped);
        return CopyShaped(shaped, output);
    }

    private static Feature[] BuildHbFeatures(in FontFeatureSet features)
    {
        var src = features.Features;
        if (src.Length == 0)
            return Array.Empty<Feature>();

        var hb = new Feature[src.Length];
        for (var i = 0; i < src.Length; i++)
        {
            var t = src[i].Tag;
            var tag = new Tag((char)(byte)(t >> 24), (char)(byte)(t >> 16), (char)(byte)(t >> 8), (char)(byte)t);
            hb[i] = new Feature(tag, src[i].Value, 0, uint.MaxValue);
        }
        return hb;
    }

    private static int CopyShaped(ShapedGlyph[] shaped, Span<ShapedGlyph> output)
    {
        var n = Math.Min(shaped.Length, output.Length);
        shaped.AsSpan(0, n).CopyTo(output);
        return n;
    }

    private static bool TryGetCachedShape(FontEntry entry, ulong sig, ReadOnlySpan<char> text, out ShapedGlyph[] shaped)
    {
        shaped = null!;
        if (entry.ShapeBuckets is null || !entry.ShapeBuckets.TryGetValue(sig, out var bucket))
            return false;

        if (bucket.Cur is { } cur &&
            cur.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(text, out shaped!))
            return true;

        if (bucket.Old is { } old &&
            old.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(text, out var key, out shaped!))
        {
            // Hit in the cold generation: promote so a still-hot line survives the next sweep.
            PutShape(entry, sig, key, shaped);
            return true;
        }

        shaped = null!;
        return false;
    }

    private static void PutShape(FontEntry entry, ulong sig, string key, ShapedGlyph[] shaped)
    {
        var buckets = entry.ShapeBuckets ??= new Dictionary<ulong, ShapeBucket>();
        if (!buckets.TryGetValue(sig, out var bucket))
        {
            bucket = new ShapeBucket();
            buckets[sig] = bucket;
        }

        var cur = bucket.Cur ??= new Dictionary<string, ShapedGlyph[]>(StringComparer.Ordinal);
        if (cur.Count >= ShapeCacheGenCap && !cur.ContainsKey(key))
        {
            // Two-generation clock eviction: drop the cold generation, demote the current
            // one. Bounds memory at ~2*ShapeCacheGenCap entries per bucket while keeping hot lines.
            bucket.Old = cur;
            cur = bucket.Cur = new Dictionary<string, ShapedGlyph[]>(StringComparer.Ordinal);
        }
        cur[key] = shaped;
    }

    private FontEntry GetEntry(FontHandle handle)
    {
        if (!handle.IsValid || handle.Id > _fonts.Count)
            throw new ArgumentException($"Invalid font handle: {handle.Id}", nameof(handle));
        return _fonts[handle.Id - 1];
    }

    private static void ActivateSize(FontEntry entry)
    {
        // For now each font handle corresponds to one fixed pixel size set at load time,
        // and FreeType remembers that on the face. If multiple handles share a face we'd need
        // to re-set pixel sizes here; we don't share faces yet.
        _ = entry;
    }

    private static long MakeKey(int fontId, uint glyphIndex)
    {
        return ((long)fontId << 32) | glyphIndex;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FreeTypeFontBackend));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        foreach (var entry in _fonts)
        {
            entry.HbBuffer?.Dispose();
            entry.HbFont?.Dispose();
            entry.HbFace?.Dispose();
            FT_Done_Face(entry.Face);
            if (entry.PinnedData.IsAllocated)
                entry.PinnedData.Free();
        }
        _fonts.Clear();
        _library.Dispose();
    }

    private sealed class FontEntry
    {
        public FT_FaceRec_* Face;
        public GCHandle PinnedData;
        public byte[] SourceData = Array.Empty<byte>();
        public int PixelSize;
        public bool HasKerning;
        public bool IsEmboldened;
        public Face? HbFace;
        public Font? HbFont;
        public HbBuffer? HbBuffer;
        public Dictionary<int, FontHandle>? SizeVariants;
        public FontHandle? EmboldenedVariant;

        // Per-feature-set cache of shaped runs, keyed by the feature signature (see PutShape).
        public Dictionary<ulong, ShapeBucket>? ShapeBuckets;
    }

    // Two-generation cache of shaped runs keyed by line text, scoped to one feature set.
    private sealed class ShapeBucket
    {
        public Dictionary<string, ShapedGlyph[]>? Cur;
        public Dictionary<string, ShapedGlyph[]>? Old;
    }
}
