using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using ICSharpCode.Decompiler;

namespace ZGF.ILSpy.Core;

/// <summary>
/// The files of an app published as a single-file bundle: its host executable with the app's files
/// appended, listed in a manifest the host finds by a signature. The manifest's offsets and sizes are
/// checked against the file before anything is read or allocated by them.
/// </summary>
internal static class BundleContents
{
    private const int SignatureLength = 32;

    /// <summary>The files the bundle at <paramref name="path"/> lists, or null when it is no bundle.</summary>
    /// <exception cref="IOException">The file cannot be read.</exception>
    public static IReadOnlyList<SingleFileBundle.Entry>? TryReadManifest(string path)
    {
        if (new FileInfo(path).Length <= SignatureLength)
            return null;
        using var mapped = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        using var view = mapped.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        if (!SingleFileBundle.IsBundle(view, out var headerOffset))
            return null;
        return ReadEntries(view, headerOffset);
    }

    private static IReadOnlyList<SingleFileBundle.Entry>? ReadEntries(MemoryMappedViewAccessor view, long headerOffset)
    {
        try
        {
            return SingleFileBundle.ReadManifest(view, headerOffset).Entries;
        }
        catch (Exception e) when (e is InvalidDataException or EndOfStreamException)
        {
            return null;
        }
    }

    /// <summary>The bytes of <paramref name="entry"/>, inflated when the bundle stores it compressed.</summary>
    /// <exception cref="InvalidDataException">The manifest places or sizes the entry where it cannot be.</exception>
    public static byte[] Read(string path, SingleFileBundle.Entry entry)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var stored = StoredLength(entry);
        CheckWithin(stream, entry, stored);
        stream.Position = entry.Offset;
        var bytes = new byte[stored];
        stream.ReadExactly(bytes);
        if (!IsCompressed(entry))
            return bytes;
        return Inflate(bytes, entry);
    }

    private static bool IsCompressed(SingleFileBundle.Entry entry) => entry.CompressedSize != 0;

    private static int StoredLength(SingleFileBundle.Entry entry)
    {
        CheckSize(entry, entry.Size);
        if (!IsCompressed(entry))
            return (int)entry.Size;
        CheckSize(entry, entry.CompressedSize);
        return (int)entry.CompressedSize;
    }

    private static void CheckSize(SingleFileBundle.Entry entry, long size)
    {
        if (size < 0)
            throw Corrupt(entry, $"its size {size} is negative");
        if (size > int.MaxValue)
            throw Corrupt(entry, $"its size {size} is too large");
    }

    private static void CheckWithin(Stream stream, SingleFileBundle.Entry entry, int stored)
    {
        if (entry.Offset < 0)
            throw Corrupt(entry, $"it starts at {entry.Offset}");
        if (entry.Offset + stored > stream.Length)
            throw Corrupt(entry, $"it runs past the end of the file, at {entry.Offset + stored}");
    }

    /// <summary>Inflates <paramref name="compressed"/>, stopping one byte past the size the manifest
    /// declares, which is enough to tell that it lied.</summary>
    private static byte[] Inflate(byte[] compressed, SingleFileBundle.Entry entry)
    {
        using var deflate = new DeflateStream(new MemoryStream(compressed), CompressionMode.Decompress);
        using var inflated = new MemoryStream();
        var buffer = new byte[81920];
        var remaining = entry.Size + 1;
        while (remaining > 0)
        {
            var wanted = (int)Math.Min(buffer.Length, remaining);
            var read = deflate.Read(buffer, 0, wanted);
            if (read == 0)
                break;
            inflated.Write(buffer, 0, read);
            remaining -= read;
        }
        if (inflated.Length != entry.Size)
            throw Corrupt(entry, $"it inflates to {inflated.Length} bytes, not the {entry.Size} declared");
        return inflated.ToArray();
    }

    private static InvalidDataException Corrupt(SingleFileBundle.Entry entry, string why) =>
        new($"The bundle's entry '{entry.RelativePath}' cannot be read: {why}.");
}
