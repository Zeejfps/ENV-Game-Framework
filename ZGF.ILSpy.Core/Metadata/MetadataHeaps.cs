// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata;

/// <summary>The four heaps (#Strings, #US, #GUID, #Blob) as grids of their entries.</summary>
internal static class MetadataHeaps
{
    /// <summary>Each heap of <paramref name="file"/>, titled with how many entries it holds.</summary>
    public static IEnumerable<(string Title, GridSchema Schema)> Create(MetadataFile file)
    {
        var m = file.Metadata;

        IEnumerable<StringHandle> Strings() => Walk(MetadataTokens.StringHandle(0), m.GetNextHandle, static h => h.IsNil);
        yield return ($"String Heap ({Strings().Count()})", new GridSchema<StringHandle>("String Heap", file, Strings)
            .Value("Offset", h => m.GetHeapOffset(h), "X8")
            .Value("Length", h => m.GetString(h).Length)
            .Value("Value", h => m.GetString(h)));

        IEnumerable<UserStringHandle> UserStrings() => Walk(MetadataTokens.UserStringHandle(0), m.GetNextHandle, static h => h.IsNil);
        yield return ($"UserString Heap ({UserStrings().Count()})", new GridSchema<UserStringHandle>("UserString Heap", file, UserStrings)
            .Value("Offset", h => m.GetHeapOffset(h), "X8")
            .Value("Length", h => m.GetUserString(h).Length)
            .Value("Value", h => m.GetUserString(h)));

        IEnumerable<GuidHandle> Guids() => Enumerable.Range(1, m.GetHeapSize(HeapIndex.Guid) >> 4).Select(MetadataTokens.GuidHandle);
        yield return ($"Guid Heap ({Guids().Count()})", new GridSchema<GuidHandle>("Guid Heap", file, Guids)
            .Value("Index", h => m.GetHeapOffset(h))
            .Value("Length", _ => 16)
            .Value("Value", h => m.GetGuid(h).ToString()));

        IEnumerable<BlobHandle> Blobs() => Walk(MetadataTokens.BlobHandle(0), m.GetNextHandle, static h => h.IsNil);
        yield return ($"Blob Heap ({Blobs().Count()})", new GridSchema<BlobHandle>("Blob Heap", file, Blobs)
            .Value("Offset", h => m.GetHeapOffset(h), "X8")
            .Value("Length", h => m.GetBlobReader(h).Length)
            .Value("Value", h => m.GetBlobReader(h).ToHexString()));
    }

    /// <summary>
    /// The entries of an offset-chained heap (#Strings, #US, #Blob). Handle 0 is a real first entry
    /// (the heap's empty slot) even though it is the nil handle, so each entry is added before
    /// <paramref name="next"/> is consulted; the walk ends at the next nil handle.
    /// </summary>
    private static IEnumerable<T> Walk<T>(T first, Func<T, T> next, Func<T, bool> isNil)
    {
        var handle = first;
        do
        {
            yield return handle;
            handle = next(handle);
        } while (!isNil(handle));
    }
}
