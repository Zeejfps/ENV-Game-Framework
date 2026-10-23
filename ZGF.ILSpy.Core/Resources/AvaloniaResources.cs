using System.Text;
using System.Xml.Linq;

namespace ZGF.ILSpy.Core.Resources;

/// <summary>
/// The container Avalonia embeds an app's assets in, as its <c>AvaloniaResourcesIndexReaderWriter</c>
/// writes it: the index's length, the index of each asset's path, offset and size, then the assets'
/// bytes. Avalonia 0.10 wrote the index as XML (version 1); later versions write it as binary
/// (version 2).
/// </summary>
internal static class AvaloniaResources
{
    public const string ContainerName = "!AvaloniaResources";

    private const int XmlIndexVersion = 1;
    private const int BinaryIndexVersion = 2;

    private static readonly XNamespace XmlIndexNamespace = "http://schemas.datacontract.org/2004/07/Avalonia.Utilities";
    private static readonly XName XmlEntry = XmlIndexNamespace + "AvaloniaResourcesIndexEntry";
    private static readonly XName XmlPath = XmlIndexNamespace + "Path";
    private static readonly XName XmlOffset = XmlIndexNamespace + "Offset";
    private static readonly XName XmlSize = XmlIndexNamespace + "Size";

    private readonly record struct IndexEntry(string Path, int Offset, int Size);

    public static bool IsContainerName(string name) => name == ContainerName;

    /// <summary>Each asset's path and bytes, in index order. Throws when
    /// <paramref name="container"/> is not one Avalonia writes.</summary>
    public static List<(string Path, byte[] Data)> ReadEntries(byte[] container)
    {
        using var reader = new BinaryReader(new MemoryStream(container), Encoding.UTF8);
        var indexLength = reader.ReadInt32();
        var dataStart = sizeof(int) + indexLength;
        var index = ReadIndex(reader, container, indexLength);
        return index
            .Select(e => (e.Path, Slice(container, dataStart, e)))
            .ToList();
    }

    private static List<IndexEntry> ReadIndex(BinaryReader reader, byte[] container, int indexLength)
    {
        var version = reader.ReadInt32();
        if (version == BinaryIndexVersion)
            return ReadBinaryIndex(reader);
        if (version == XmlIndexVersion)
            return ReadXmlIndex(container, indexLength);
        throw new InvalidDataException($"{ContainerName} has an index of unknown version {version}.");
    }

    private static List<IndexEntry> ReadBinaryIndex(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        var entries = new List<IndexEntry>();
        for (var i = 0; i < count; i++)
        {
            var path = reader.ReadString();
            var offset = reader.ReadInt32();
            var size = reader.ReadInt32();
            entries.Add(new IndexEntry(path, offset, size));
        }
        return entries;
    }

    /// <summary>The index Avalonia 0.10 wrote with a <c>DataContractSerializer</c>, which follows
    /// the version up to the end of the index.</summary>
    private static List<IndexEntry> ReadXmlIndex(byte[] container, int indexLength)
    {
        var xmlStart = 2 * sizeof(int);
        var xmlLength = indexLength - sizeof(int);
        var document = XDocument.Load(new MemoryStream(container, xmlStart, xmlLength));
        return document
            .Descendants(XmlEntry)
            .Select(ReadXmlEntry)
            .ToList();
    }

    private static IndexEntry ReadXmlEntry(XElement entry)
    {
        var path = (string)Required(entry, XmlPath);
        var offset = (int)Required(entry, XmlOffset);
        var size = (int)Required(entry, XmlSize);
        return new IndexEntry(path, offset, size);
    }

    private static XElement Required(XElement entry, XName name) =>
        entry.Element(name) ?? throw new InvalidDataException($"{ContainerName} has an index entry without {name.LocalName}.");

    private static byte[] Slice(byte[] container, int dataStart, IndexEntry entry)
    {
        var start = (long)dataStart + entry.Offset;
        if (!IsWithin(container, start, entry))
            throw new InvalidDataException($"{entry.Path} lies outside {ContainerName}.");
        return container.AsSpan((int)start, entry.Size).ToArray();
    }

    private static bool IsWithin(byte[] container, long start, IndexEntry entry)
    {
        if (entry.Offset < 0 || entry.Size < 0)
            return false;
        return start + entry.Size <= container.Length;
    }
}
