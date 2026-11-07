using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using FreeTypeSharp;
using static FreeTypeSharp.FT;

namespace ZGF.Fonts.SystemFonts;

/// <summary>
/// The fonts CoreText lists as available: the ones installed for every user and for this one.
/// Hidden system families (their names start with a dot) and faces with no file are left out, since
/// there is nothing to load for them.
/// </summary>
/// <remarks>
/// CoreText gives each face's file but not where in it the face is, which matters for collections
/// (.ttc) and for the named instances of a variable font. FreeType opens each file once to find it,
/// matching the face by its PostScript name, else its style name. CoreText and CoreFoundation are
/// thread-safe for these calls, so this runs on any thread.
/// </remarks>
[SupportedOSPlatform("macos")]
internal sealed unsafe class MacSystemFonts : ISystemFonts
{
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string CoreText = "/System/Library/Frameworks/CoreText.framework/CoreText";

    private const int kCFURLPOSIXPathStyle = 0;
    private const int kCFNumberSInt64Type = 4;
    private const int kCFNumberDoubleType = 13;

    private const uint kCTFontTraitItalic = 1u << 0;
    private const uint kCTFontTraitMonoSpace = 1u << 10;
    private const int kCTFontClassMaskShift = 28;
    private const uint kCTFontSymbolicClass = 12u;

    /// <summary>CoreText's normalized weights (-1 to 1) for the standard weights 100 to 900, as
    /// NSFontWeight defines them.</summary>
    private static readonly (double Normalized, int Weight)[] Weights =
    [
        (-0.80, 100),
        (-0.60, 200),
        (-0.40, 300),
        (0.00, 400),
        (0.23, 500),
        (0.30, 600),
        (0.40, 700),
        (0.56, 800),
        (0.62, 900),
    ];

    public IReadOnlyList<SystemFontFamily> ListFamilies()
    {
        var keys = Keys.Load();
        var collection = CTFontCollectionCreateFromAvailableFonts(IntPtr.Zero);
        if (collection == IntPtr.Zero)
            return [];
        try
        {
            var descriptors = CTFontCollectionCreateMatchingFontDescriptors(collection);
            if (descriptors == IntPtr.Zero)
                return [];
            try
            {
                return ListFamilies(descriptors, keys);
            }
            finally
            {
                CFRelease(descriptors);
            }
        }
        finally
        {
            CFRelease(collection);
        }
    }

    private static IReadOnlyList<SystemFontFamily> ListFamilies(IntPtr descriptors, Keys keys)
    {
        var faces = new Dictionary<string, List<SystemFontFace>>(StringComparer.Ordinal);
        using var indices = new FaceIndices();
        var count = CFArrayGetCount(descriptors);
        for (nint i = 0; i < count; i++)
        {
            var descriptor = CFArrayGetValueAtIndex(descriptors, i);
            var family = CopyString(descriptor, keys.FamilyName);
            if (string.IsNullOrEmpty(family) || family.StartsWith('.'))
                continue;
            var face = ReadFace(descriptor, keys, indices);
            if (face == null)
                continue;
            if (!faces.TryGetValue(family, out var list))
            {
                list = new List<SystemFontFace>();
                faces.Add(family, list);
            }
            if (!list.Any(f => f.File == face.File))
                list.Add(face);
        }
        return faces
            .Select(p => new SystemFontFamily(p.Key, p.Value))
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static SystemFontFace? ReadFace(IntPtr descriptor, Keys keys, FaceIndices indices)
    {
        var path = CopyPath(descriptor, keys.Url);
        if (path == null)
            return null;
        var postScriptName = CopyString(descriptor, keys.Name);
        var styleName = CopyString(descriptor, keys.StyleName) ?? "";
        var index = indices.Find(path, postScriptName, styleName);
        if (index < 0)
            return null;
        var traits = CTFontDescriptorCopyAttribute(descriptor, keys.Traits);
        if (traits == IntPtr.Zero)
            return null;
        try
        {
            var symbolic = (uint)ReadLong(CFDictionaryGetValue(traits, keys.SymbolicTrait));
            var weight = ReadDouble(CFDictionaryGetValue(traits, keys.WeightTrait));
            var width = ReadDouble(CFDictionaryGetValue(traits, keys.WidthTrait));
            return new SystemFontFace(
                styleName,
                new SystemFontFile(path, index),
                ToWeight(weight),
                ToStretch(width),
                (symbolic & kCTFontTraitItalic) != 0 ? SystemFontStyle.Italic : SystemFontStyle.Normal,
                (symbolic & kCTFontTraitMonoSpace) != 0,
                symbolic >> kCTFontClassMaskShift == kCTFontSymbolicClass);
        }
        finally
        {
            CFRelease(traits);
        }
    }

    /// <summary>The standard weight nearest CoreText's <paramref name="normalized"/> one.</summary>
    internal static int ToWeight(double normalized)
    {
        return Weights
            .OrderBy(w => Math.Abs(w.Normalized - normalized))
            .First()
            .Weight;
    }

    /// <summary>CoreText's width, -1 (narrowest) to 1 (widest), as a stretch from 1 to 9.</summary>
    internal static int ToStretch(double normalized)
    {
        var stretch = SystemFontFace.NormalStretch + (int)Math.Round(normalized * 4);
        return Math.Clamp(stretch, 1, 9);
    }

    /// <summary>The string attribute <paramref name="key"/> of <paramref name="descriptor"/>, or null.</summary>
    private static string? CopyString(IntPtr descriptor, IntPtr key)
    {
        var value = CTFontDescriptorCopyAttribute(descriptor, key);
        if (value == IntPtr.Zero)
            return null;
        try
        {
            return ReadString(value);
        }
        finally
        {
            CFRelease(value);
        }
    }

    /// <summary>The path of the file the URL attribute <paramref name="key"/> points to, or null when
    /// there is none.</summary>
    private static string? CopyPath(IntPtr descriptor, IntPtr key)
    {
        var url = CTFontDescriptorCopyAttribute(descriptor, key);
        if (url == IntPtr.Zero)
            return null;
        try
        {
            var path = CFURLCopyFileSystemPath(url, kCFURLPOSIXPathStyle);
            if (path == IntPtr.Zero)
                return null;
            try
            {
                return ReadString(path);
            }
            finally
            {
                CFRelease(path);
            }
        }
        finally
        {
            CFRelease(url);
        }
    }

    private static string ReadString(IntPtr text)
    {
        var length = CFStringGetLength(text);
        if (length == 0)
            return "";
        var buffer = new char[length];
        fixed (char* chars = buffer)
            CFStringGetCharacters(text, new CFRange(0, length), chars);
        return new string(buffer);
    }

    private static double ReadDouble(IntPtr number)
    {
        double value = 0;
        if (number != IntPtr.Zero)
            CFNumberGetValue(number, kCFNumberDoubleType, &value);
        return value;
    }

    private static long ReadLong(IntPtr number)
    {
        long value = 0;
        if (number != IntPtr.Zero)
            CFNumberGetValue(number, kCFNumberSInt64Type, &value);
        return value;
    }

    /// <summary>The CoreText attribute and trait names, CFStringRef constants exported as variables
    /// rather than functions.</summary>
    private readonly struct Keys
    {
        public required IntPtr FamilyName { get; init; }
        public required IntPtr StyleName { get; init; }
        public required IntPtr Name { get; init; }
        public required IntPtr Url { get; init; }
        public required IntPtr Traits { get; init; }
        public required IntPtr SymbolicTrait { get; init; }
        public required IntPtr WeightTrait { get; init; }
        public required IntPtr WidthTrait { get; init; }

        public static Keys Load()
        {
            var coreText = NativeLibrary.Load(CoreText);
            return new Keys
            {
                FamilyName = Read(coreText, "kCTFontFamilyNameAttribute"),
                StyleName = Read(coreText, "kCTFontStyleNameAttribute"),
                Name = Read(coreText, "kCTFontNameAttribute"),
                Url = Read(coreText, "kCTFontURLAttribute"),
                Traits = Read(coreText, "kCTFontTraitsAttribute"),
                SymbolicTrait = Read(coreText, "kCTFontSymbolicTrait"),
                WeightTrait = Read(coreText, "kCTFontWeightTrait"),
                WidthTrait = Read(coreText, "kCTFontWidthTrait"),
            };
        }

        private static IntPtr Read(IntPtr library, string name) =>
            Marshal.ReadIntPtr(NativeLibrary.GetExport(library, name));
    }

    /// <summary>
    /// Where each face is in its file, as FreeType numbers them: the face of a collection in the low
    /// 16 bits, and a variable font's named instance, counted from 1, in the high ones. Each file is
    /// opened once, its faces and instances listed by PostScript and style name.
    /// </summary>
    private sealed class FaceIndices : IDisposable
    {
        private readonly FreeTypeLibrary _library = new();
        private readonly Dictionary<string, List<(string? PostScriptName, string StyleName, int Index)>> _files = new(StringComparer.Ordinal);

        /// <summary>The index of the face named <paramref name="postScriptName"/>, else of the one styled
        /// <paramref name="styleName"/>, in <paramref name="path"/>; -1 when FreeType finds neither.</summary>
        public int Find(string path, string? postScriptName, string styleName)
        {
            if (!_files.TryGetValue(path, out var faces))
            {
                faces = ListFaces(path);
                _files.Add(path, faces);
            }
            if (faces.Count == 1)
                return faces[0].Index;
            foreach (var face in faces)
            {
                if (postScriptName != null && face.PostScriptName == postScriptName)
                    return face.Index;
            }
            foreach (var face in faces)
            {
                if (string.Equals(face.StyleName, styleName, StringComparison.OrdinalIgnoreCase))
                    return face.Index;
            }
            return -1;
        }

        private List<(string?, string, int)> ListFaces(string path)
        {
            var faces = new List<(string?, string, int)>();
            var first = Open(path, 0);
            if (first == null)
                return faces;
            var count = (int)first->num_faces;
            FT_Done_Face(first);
            for (var face = 0; face < count; face++)
            {
                var opened = Open(path, face);
                if (opened == null)
                    continue;
                var instances = (int)((long)opened->style_flags >> 16);
                faces.Add(Describe(opened, face));
                FT_Done_Face(opened);
                for (var instance = 1; instance <= instances; instance++)
                {
                    var index = (instance << 16) | face;
                    var named = Open(path, index);
                    if (named == null)
                        continue;
                    faces.Add(Describe(named, index));
                    FT_Done_Face(named);
                }
            }
            return faces;
        }

        private static (string?, string, int) Describe(FT_FaceRec_* face, int index)
        {
            var postScriptName = Marshal.PtrToStringUTF8((IntPtr)FT_Get_Postscript_Name(face));
            var styleName = Marshal.PtrToStringUTF8((IntPtr)face->style_name) ?? "";
            return (postScriptName, styleName, index);
        }

        private FT_FaceRec_* Open(string path, int index)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(path + "\0");
            FT_FaceRec_* face = null;
            fixed (byte* name = bytes)
            {
                if (FT_New_Face(_library.Native, name, index, &face) != FT_Error.FT_Err_Ok)
                    return null;
            }
            return face;
        }

        public void Dispose() => _library.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct CFRange(nint location, nint length)
    {
        public readonly nint Location = location;
        public readonly nint Length = length;
    }

    [DllImport(CoreFoundation)]
    private static extern void CFRelease(IntPtr value);

    [DllImport(CoreFoundation)]
    private static extern nint CFArrayGetCount(IntPtr array);

    [DllImport(CoreFoundation)]
    private static extern IntPtr CFArrayGetValueAtIndex(IntPtr array, nint index);

    [DllImport(CoreFoundation)]
    private static extern IntPtr CFDictionaryGetValue(IntPtr dictionary, IntPtr key);

    [DllImport(CoreFoundation)]
    private static extern nint CFStringGetLength(IntPtr text);

    [DllImport(CoreFoundation)]
    private static extern void CFStringGetCharacters(IntPtr text, CFRange range, char* buffer);

    [DllImport(CoreFoundation)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool CFNumberGetValue(IntPtr number, int type, void* value);

    [DllImport(CoreFoundation)]
    private static extern IntPtr CFURLCopyFileSystemPath(IntPtr url, int style);

    [DllImport(CoreText)]
    private static extern IntPtr CTFontCollectionCreateFromAvailableFonts(IntPtr options);

    [DllImport(CoreText)]
    private static extern IntPtr CTFontCollectionCreateMatchingFontDescriptors(IntPtr collection);

    [DllImport(CoreText)]
    private static extern IntPtr CTFontDescriptorCopyAttribute(IntPtr descriptor, IntPtr attribute);
}
