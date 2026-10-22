// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Metadata;

namespace ZGF.ILSpy.Core.Metadata;

/// <summary>
/// The CustomDebugInformation table: extensible per-entity payloads used for async/iterator
/// state-machine info, embedded source, source-link JSON, and similar debug-time data. Each row's
/// Kind GUID shows as heap offset, raw GUID and decoded friendly name, and its parsed Value blob
/// as the row's details.
/// </summary>
internal static class CustomDebugInformationTable
{
    private static readonly (Guid Guid, string Name)[] KnownKindNames =
    [
        (KnownGuids.StateMachineHoistedLocalScopes, "State Machine Hoisted Local Scopes (C# / VB)"),
        (KnownGuids.DynamicLocalVariables, "Dynamic Local Variables (C#)"),
        (KnownGuids.DefaultNamespaces, "Default Namespaces (VB)"),
        (KnownGuids.EditAndContinueLocalSlotMap, "Edit And Continue Local Slot Map (C# / VB)"),
        (KnownGuids.EditAndContinueLambdaAndClosureMap, "Edit And Continue Lambda And Closure Map (C# / VB)"),
        (KnownGuids.EncStateMachineStateMap, "Edit And Continue State Machine State Map (C# / VB)"),
        (KnownGuids.EmbeddedSource, "Embedded Source (C# / VB)"),
        (KnownGuids.SourceLink, "Source Link (C# / VB)"),
        (KnownGuids.MethodSteppingInformation, "Method Stepping Information (C# / VB)"),
        (KnownGuids.CompilationOptions, "Compilation Options (C# / VB)"),
        (KnownGuids.CompilationMetadataReferences, "Compilation Metadata References (C# / VB)"),
        (KnownGuids.TupleElementNames, "Tuple Element Names (C#)"),
        (KnownGuids.TypeDefinitionDocuments, "Type Definition Documents (C# / VB)"),
    ];

    public static GridSchema<CustomDebugInformation> Columns(GridSchema<CustomDebugInformation> table, MetadataFile file)
    {
        var m = file.Metadata;
        return table.Offset()
            .Handle("Parent", r => r.Parent)
            .Value("Kind", r => MetadataTokens.GetHeapOffset(r.Kind), "X2")
            .Value("KindGUID", r => m.GetGuid(r.Kind), "D")
            .Value("KindString", r => KindString(m, r.Kind))
            .Value("Info", r => !r.Kind.IsNil && m.GetGuid(r.Kind) == KnownGuids.EmbeddedSource ? EmbeddedSourceFormat(m, r.Value) : null)
            .Blob("Value", r => r.Value, r => r.Value.IsNil ? "<nil>" : m.GetBlobReader(r.Value).ToHexString())
            .Value("ValueLength", r => m.GetBlobReader(r.Value).Length, "X8")
            .WithDetails(r => Details(m, r));
    }

    private static string KindString(MetadataReader metadata, GuidHandle kind)
    {
        if (kind.IsNil)
            return "";
        var guid = metadata.GetGuid(kind);
        foreach (var (knownGuid, knownName) in KnownKindNames)
        {
            if (guid == knownGuid)
                return knownName;
        }
        return "Unknown";
    }

    private static string EmbeddedSourceFormat(MetadataReader metadata, BlobHandle value)
    {
        if (value.IsNil)
            return "{nil blob}";
        var reader = metadata.GetBlobReader(value);
        if (reader.RemainingBytes < 4)
            return "{blob too short}";
        var format = reader.ReadInt32();
        return format switch
        {
            < 0 => $"Unknown format '{format}', {reader.Length} bytes",
            0 => $"Raw, {reader.Length} bytes",
            > 0 => $"DEFLATE, {reader.Length} bytes, {format} uncompressed",
        };
    }

    /// <summary>
    /// The parsed Value blob: structured kinds as a table; source-link blobs as the decoded JSON,
    /// embedded source as the (decompressed) document text tagged with its document's file
    /// extension, and everything else, including malformed blobs, as a hex dump.
    /// </summary>
    private static MetadataRowDetails? Details(MetadataReader metadata, CustomDebugInformation row)
    {
        if (row.Value.IsNil || row.Kind.IsNil)
            return null;
        var reader = metadata.GetBlobReader(row.Value);
        try
        {
            return Parse(metadata, row, ref reader);
        }
        catch (Exception ex) when (ex is BadImageFormatException or InvalidDataException)
        {
            return new MetadataTextDetails(metadata.GetBlobReader(row.Value).ToHexString(), null);
        }
    }

    private static MetadataRowDetails Parse(MetadataReader metadata, CustomDebugInformation row, ref BlobReader reader)
    {
        var kind = metadata.GetGuid(row.Kind);
        if (kind == KnownGuids.StateMachineHoistedLocalScopes)
        {
            var rows = new List<IReadOnlyList<string>>();
            while (reader.RemainingBytes > 0)
                rows.Add([reader.ReadUInt32().ToString(), reader.ReadUInt32().ToString()]);
            return new MetadataTableDetails(["Start Offset", "Length"], rows);
        }
        if (kind == KnownGuids.SourceLink)
            return new MetadataTextDetails(reader.ReadUTF8(reader.RemainingBytes), ".json");
        if (kind == KnownGuids.EmbeddedSource)
        {
            var embeddedSourceFormat = reader.ReadInt32();

            if (embeddedSourceFormat < 0) // unknown format, show raw data as hex
                return new MetadataTextDetails(reader.ToHexString(), null);

            var embeddedSourceBytes = reader.ReadBytes(reader.RemainingBytes);
            Stream embeddedSourceByteStream = new MemoryStream(embeddedSourceBytes);

            if (embeddedSourceFormat > 0) // positive length means the data is compressed using DEFLATE
                embeddedSourceByteStream = new System.IO.Compression.DeflateStream(embeddedSourceByteStream, System.IO.Compression.CompressionMode.Decompress);

            using var textReader = new StreamReader(embeddedSourceByteStream, detectEncodingFromByteOrderMarks: true);
            return new MetadataTextDetails(textReader.ReadToEnd(), EmbeddedSourceExtension(metadata, row.Parent));
        }
        if (kind == KnownGuids.CompilationOptions)
        {
            var rows = new List<IReadOnlyList<string>>();
            while (reader.RemainingBytes > 0)
                rows.Add([reader.ReadUTF8StringNullTerminated(), reader.ReadUTF8StringNullTerminated()]);
            return new MetadataTableDetails(["Name", "Value"], rows);
        }
        if (kind == KnownGuids.CompilationMetadataReferences)
        {
            var rows = new List<IReadOnlyList<string>>();
            while (reader.RemainingBytes > 0)
            {
                var fileName = reader.ReadUTF8StringNullTerminated();
                var aliases = reader.ReadUTF8StringNullTerminated();
                var flags = reader.ReadByte();
                var timestamp = reader.ReadUInt32();
                var fileSize = reader.ReadUInt32();
                var mvid = reader.ReadGuid();
                rows.Add([fileName, aliases, flags.ToString(), timestamp.ToString(), fileSize.ToString(), mvid.ToString()]);
            }
            return new MetadataTableDetails(["File Name", "Aliases", "Flags", "Timestamp", "File Size", "MVID"], rows);
        }
        if (kind == KnownGuids.TupleElementNames)
        {
            var rows = new List<IReadOnlyList<string>>();
            while (reader.RemainingBytes > 0)
                rows.Add([reader.ReadUTF8StringNullTerminated()]);
            return new MetadataTableDetails(["Element Name"], rows);
        }
        return new MetadataTextDetails(reader.ToHexString(), null);
    }

    /// <summary>The file extension of the Document an embedded-source row holds the text of, which
    /// selects the syntax highlighting for it.</summary>
    private static string? EmbeddedSourceExtension(MetadataReader metadata, EntityHandle parent)
    {
        if (parent.Kind != HandleKind.Document)
            return null;
        var document = metadata.GetDocument((DocumentHandle)parent);
        if (document.Name.IsNil)
            return null;
        var extension = Path.GetExtension(metadata.GetString(document.Name));
        return string.IsNullOrEmpty(extension) ? null : extension;
    }

    /// <summary>Reads a null-terminated UTF-8 string and advances past the terminator; throws
    /// <see cref="BadImageFormatException"/> when no terminator remains.</summary>
    private static string ReadUTF8StringNullTerminated(this ref BlobReader reader)
    {
        var length = reader.IndexOf(0);
        if (length < 0)
            throw new BadImageFormatException("Missing null terminator in blob.");
        var s = reader.ReadUTF8(length);
        reader.ReadByte();
        return s;
    }
}
