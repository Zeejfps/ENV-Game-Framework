using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ZGF.Fonts.SystemFonts;

/// <summary>
/// The fonts in DirectWrite's system collection: the ones installed for every user and for this
/// one. Faces DirectWrite only simulates, and faces with no file on this machine (cloud fonts not
/// downloaded yet), are left out, since there is nothing to load for them.
/// </summary>
/// <remarks>
/// Calls the DirectWrite interfaces through their COM vtables, as <c>WindowsFilePicker</c> does,
/// since the [ComImport] wrappers need runtime IL generation that AOT publishing turns off. The
/// shared factory is free-threaded, so this runs on any thread without initialising COM.
/// </remarks>
[SupportedOSPlatform("windows")]
internal sealed unsafe class WindowsSystemFonts : ISystemFonts
{
    private const int DWRITE_FACTORY_TYPE_SHARED = 0;
    private const int DWRITE_FONT_SIMULATIONS_NONE = 0;
    private const string EnglishLocale = "en-us";

    private static readonly Guid IidDWriteFactory = new("b859ee5a-d838-4b5b-a2e8-1adc7d93db48");
    private static readonly Guid IidDWriteFont1 = new("acd16696-8c14-4f5d-877e-fe3fc1d32738");
    private static readonly Guid IidDWriteLocalFontFileLoader = new("b2d9f3ec-c9fe-4a11-a2ec-d86208f7c0a2");

    public IReadOnlyList<SystemFontFamily> ListFamilies()
    {
        Marshal.ThrowExceptionForHR(DWriteCreateFactory(DWRITE_FACTORY_TYPE_SHARED, in IidDWriteFactory, out var factory));
        try
        {
            var collection = GetSystemFontCollection(factory);
            try
            {
                return ListFamilies(collection);
            }
            finally
            {
                Release(collection);
            }
        }
        finally
        {
            Release(factory);
        }
    }

    // IDWriteFactory: 3 GetSystemFontCollection
    private static IntPtr GetSystemFontCollection(IntPtr factory)
    {
        var getSystemFontCollection = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int, int>)Vtbl(factory)[3];
        IntPtr collection;
        Marshal.ThrowExceptionForHR(getSystemFontCollection(factory, &collection, 0));
        return collection;
    }

    // IDWriteFontCollection: 3 GetFontFamilyCount, 4 GetFontFamily
    private static IReadOnlyList<SystemFontFamily> ListFamilies(IntPtr collection)
    {
        var getFontFamilyCount = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Vtbl(collection)[3];
        var getFontFamily = (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>)Vtbl(collection)[4];
        var families = new List<SystemFontFamily>();
        var count = getFontFamilyCount(collection);
        for (var i = 0u; i < count; i++)
        {
            IntPtr family;
            if (getFontFamily(collection, i, &family) < 0)
                continue;
            try
            {
                var read = ReadFamily(family);
                if (read != null)
                    families.Add(read);
            }
            finally
            {
                Release(family);
            }
        }
        return families
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    // IDWriteFontFamily: 4 GetFontCount, 5 GetFont, 6 GetFamilyNames
    private static SystemFontFamily? ReadFamily(IntPtr family)
    {
        var getFontCount = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Vtbl(family)[4];
        var getFont = (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>)Vtbl(family)[5];
        var getFamilyNames = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)Vtbl(family)[6];
        var faces = new List<SystemFontFace>();
        var count = getFontCount(family);
        for (var i = 0u; i < count; i++)
        {
            IntPtr font;
            if (getFont(family, i, &font) < 0)
                continue;
            try
            {
                var face = ReadFace(font);
                if (face != null)
                    faces.Add(face);
            }
            finally
            {
                Release(font);
            }
        }
        if (faces.Count == 0)
            return null;
        var name = ReadNames(family, getFamilyNames);
        if (name == null)
            return null;
        return new SystemFontFamily(name, faces);
    }

    // IDWriteFont: 4 GetWeight, 5 GetStretch, 6 GetStyle, 7 IsSymbolFont, 8 GetFaceNames, 10 GetSimulations
    private static SystemFontFace? ReadFace(IntPtr font)
    {
        var getWeight = (delegate* unmanaged[Stdcall]<IntPtr, int>)Vtbl(font)[4];
        var getStretch = (delegate* unmanaged[Stdcall]<IntPtr, int>)Vtbl(font)[5];
        var getStyle = (delegate* unmanaged[Stdcall]<IntPtr, int>)Vtbl(font)[6];
        var isSymbolFont = (delegate* unmanaged[Stdcall]<IntPtr, int>)Vtbl(font)[7];
        var getFaceNames = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)Vtbl(font)[8];
        var getSimulations = (delegate* unmanaged[Stdcall]<IntPtr, int>)Vtbl(font)[10];
        if (getSimulations(font) != DWRITE_FONT_SIMULATIONS_NONE)
            return null;
        var file = FindFile(font);
        if (file == null)
            return null;
        var name = ReadNames(font, getFaceNames);
        if (name == null)
            return null;
        return new SystemFontFace(
            name,
            file.Value,
            getWeight(font),
            getStretch(font),
            (SystemFontStyle)getStyle(font),
            IsMonospaced(font),
            isSymbolFont(font) != 0);
    }

    // IDWriteFont1 (Windows 8+): 17 IsMonospacedFont
    private static bool IsMonospaced(IntPtr font)
    {
        var font1 = QueryInterface(font, IidDWriteFont1);
        if (font1 == IntPtr.Zero)
            return false;
        try
        {
            var isMonospacedFont = (delegate* unmanaged[Stdcall]<IntPtr, int>)Vtbl(font1)[17];
            return isMonospacedFont(font1) != 0;
        }
        finally
        {
            Release(font1);
        }
    }

    // IDWriteFont: 13 CreateFontFace. IDWriteFontFace: 4 GetFiles, 5 GetIndex
    private static SystemFontFile? FindFile(IntPtr font)
    {
        var createFontFace = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)Vtbl(font)[13];
        IntPtr face;
        if (createFontFace(font, &face) < 0)
            return null;
        try
        {
            var getFiles = (delegate* unmanaged[Stdcall]<IntPtr, uint*, IntPtr*, int>)Vtbl(face)[4];
            var getIndex = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Vtbl(face)[5];
            uint fileCount;
            if (getFiles(face, &fileCount, null) < 0)
                return null;
            if (fileCount != 1)
                return null;
            IntPtr file;
            if (getFiles(face, &fileCount, &file) < 0)
                return null;
            try
            {
                var path = ReadLocalPath(file);
                if (path == null)
                    return null;
                return new SystemFontFile(path, (int)getIndex(face));
            }
            finally
            {
                Release(file);
            }
        }
        finally
        {
            Release(face);
        }
    }

    // IDWriteFontFile: 3 GetReferenceKey, 4 GetLoader.
    // IDWriteLocalFontFileLoader: 4 GetFilePathLengthFromKey, 5 GetFilePathFromKey
    private static string? ReadLocalPath(IntPtr file)
    {
        var getReferenceKey = (delegate* unmanaged[Stdcall]<IntPtr, void**, uint*, int>)Vtbl(file)[3];
        var getLoader = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)Vtbl(file)[4];
        void* key;
        uint keySize;
        if (getReferenceKey(file, &key, &keySize) < 0)
            return null;
        IntPtr loader;
        if (getLoader(file, &loader) < 0)
            return null;
        var local = QueryInterface(loader, IidDWriteLocalFontFileLoader);
        Release(loader);
        if (local == IntPtr.Zero)
            return null;
        try
        {
            var getFilePathLength = (delegate* unmanaged[Stdcall]<IntPtr, void*, uint, uint*, int>)Vtbl(local)[4];
            var getFilePath = (delegate* unmanaged[Stdcall]<IntPtr, void*, uint, char*, uint, int>)Vtbl(local)[5];
            uint length;
            if (getFilePathLength(local, key, keySize, &length) < 0)
                return null;
            var buffer = new char[length + 1];
            fixed (char* path = buffer)
            {
                if (getFilePath(local, key, keySize, path, length + 1) < 0)
                    return null;
            }
            return new string(buffer, 0, (int)length);
        }
        finally
        {
            Release(local);
        }
    }

    /// <summary>Reads the names <paramref name="getNames"/> gives <paramref name="owner"/>, a family
    /// or a face: the English one, else the first.</summary>
    private static string? ReadNames(IntPtr owner, delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> getNames)
    {
        IntPtr names;
        if (getNames(owner, &names) < 0)
            return null;
        try
        {
            return ReadEnglishOrFirst(names);
        }
        finally
        {
            Release(names);
        }
    }

    // IDWriteLocalizedStrings: 3 GetCount, 4 FindLocaleName, 7 GetStringLength, 8 GetString
    private static string? ReadEnglishOrFirst(IntPtr names)
    {
        var getCount = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Vtbl(names)[3];
        var findLocaleName = (delegate* unmanaged[Stdcall]<IntPtr, char*, uint*, int*, int>)Vtbl(names)[4];
        var getStringLength = (delegate* unmanaged[Stdcall]<IntPtr, uint, uint*, int>)Vtbl(names)[7];
        var getString = (delegate* unmanaged[Stdcall]<IntPtr, uint, char*, uint, int>)Vtbl(names)[8];
        if (getCount(names) == 0)
            return null;
        uint index;
        int exists;
        fixed (char* locale = EnglishLocale)
        {
            if (findLocaleName(names, locale, &index, &exists) < 0)
                return null;
        }
        if (exists == 0)
            index = 0;
        uint length;
        if (getStringLength(names, index, &length) < 0)
            return null;
        var buffer = new char[length + 1];
        fixed (char* text = buffer)
        {
            if (getString(names, index, text, length + 1) < 0)
                return null;
        }
        return new string(buffer, 0, (int)length);
    }

    private static IntPtr* Vtbl(IntPtr p) => *(IntPtr**)p;

    // IUnknown: 0 QueryInterface
    private static IntPtr QueryInterface(IntPtr p, Guid iid)
    {
        var queryInterface = (delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>)Vtbl(p)[0];
        IntPtr result;
        if (queryInterface(p, &iid, &result) < 0)
            return IntPtr.Zero;
        return result;
    }

    // IUnknown: 2 Release
    private static void Release(IntPtr p)
    {
        if (p == IntPtr.Zero) return;
        var release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Vtbl(p)[2];
        release(p);
    }

    [DllImport("dwrite.dll", PreserveSig = true)]
    private static extern int DWriteCreateFactory(int factoryType, in Guid iid, out IntPtr factory);
}
