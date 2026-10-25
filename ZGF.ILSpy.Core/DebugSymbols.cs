// Adapted from ILSpy's PortableDebugInfoProvider and DebugInfoUtils (MIT License, Copyright (c) AlphaSierraPapa for the SharpDevelop Team).

using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Metadata;
using SequencePoint = ICSharpCode.Decompiler.DebugInfo.SequencePoint;

namespace ZGF.ILSpy.Core;

/// <summary>
/// The portable PDB that goes with an assembly: the one embedded in it, else the one next to it
/// whose id matches its CodeView entry; next to it in the package, for an assembly read out of one.
/// The decompiler takes local names from it, and the tree shows its metadata beside the assembly's.
/// </summary>
internal sealed class DebugSymbols : IDebugInfoProvider
{
    private const string EmbeddedTitle = "Debug Metadata (Embedded)";
    private const string BesideTitle = "Debug Metadata (From portable PDB)";
    private const string EmbeddedDescription = "Embedded in this assembly";
    private const string BesideDescription = "Loaded from portable PDB: ";
    private const string PdbExtension = ".pdb";
    private const int BitsPerByte = 8;

    private DebugSymbols(MetadataFile file, string title, string description)
    {
        File = file;
        Title = title;
        Description = description;
    }

    /// <summary>The PDB's own metadata.</summary>
    public MetadataFile File { get; }

    /// <summary>What the tree calls <see cref="File"/>.</summary>
    public string Title { get; }

    public string Description { get; }

    public string SourceFileName => File.FileName;

    private MetadataReader Metadata => File.Metadata;

    /// <summary>The symbols for <paramref name="assembly"/>, or null when it has none or they cannot be
    /// read. A PDB beside it is looked for in <paramref name="package"/> when it was read out of one.</summary>
    public static DebugSymbols? Load(PEFile assembly, AssemblyPackage? package)
    {
        try
        {
            var entries = assembly.Reader.ReadDebugDirectory();
            var embedded = LoadEmbedded(assembly, entries);
            if (embedded != null)
                return embedded;
            return LoadBeside(assembly, package, entries);
        }
        catch (Exception e) when (IsUnreadable(e))
        {
            return null;
        }
    }

    private static DebugSymbols? LoadEmbedded(PEFile assembly, ImmutableArray<DebugDirectoryEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (entry.Type != DebugDirectoryEntryType.EmbeddedPortablePdb)
                continue;
            var provider = assembly.Reader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
            var file = new MetadataFile(MetadataFile.MetadataFileKind.ProgramDebugDatabase, assembly.FileName, provider, isEmbedded: true);
            return new DebugSymbols(file, EmbeddedTitle, EmbeddedDescription);
        }
        return null;
    }

    private static DebugSymbols? LoadBeside(PEFile assembly, AssemblyPackage? package, ImmutableArray<DebugDirectoryEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (entry.Type != DebugDirectoryEntryType.CodeView)
                continue;
            var codeView = assembly.Reader.ReadCodeViewDebugDirectoryData(entry);
            var pdb = ReadBeside(assembly.FileName, codeView.Path, package);
            if (pdb == null)
                continue;
            var provider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(pdb.Image));
            if (!IsBuiltWith(provider, codeView, entry))
                continue;
            var file = new MetadataFile(MetadataFile.MetadataFileKind.ProgramDebugDatabase, pdb.Path, provider);
            return new DebugSymbols(file, BesideTitle, BesideDescription + pdb.Path);
        }
        return null;
    }

    private sealed record PdbImage(string Path, byte[] Image);

    /// <summary>The PDB the CodeView entry names in the assembly's folder, else the one named
    /// after the assembly; null when neither is there.</summary>
    private static PdbImage? ReadBeside(string assemblyPath, string codeViewPath, AssemblyPackage? package)
    {
        var folder = Path.GetDirectoryName(assemblyPath) ?? "";
        var named = Path.Combine(folder, Path.GetFileName(codeViewPath));
        var image = TryReadFile(named, package);
        if (image != null)
            return new PdbImage(named, image);
        var sibling = Path.ChangeExtension(assemblyPath, PdbExtension);
        image = TryReadFile(sibling, package);
        if (image != null)
            return new PdbImage(sibling, image);
        return null;
    }

    /// <summary>The bytes of the file at <paramref name="path"/>: on disk, or in <paramref name="package"/>
    /// when there is one, where the path runs through it. Null when there is no such file.</summary>
    private static byte[]? TryReadFile(string path, AssemblyPackage? package)
    {
        if (package == null)
            return TryReadFromDisk(path);
        var file = package.FindFileAt(path);
        if (file == null)
            return null;
        return package.ReadFile(file);
    }

    private static byte[]? TryReadFromDisk(string path)
    {
        if (!System.IO.File.Exists(path))
            return null;
        return System.IO.File.ReadAllBytes(path);
    }

    private static bool IsBuiltWith(MetadataReaderProvider provider, CodeViewDebugDirectoryData codeView, DebugDirectoryEntry entry)
    {
        var header = provider.GetMetadataReader().DebugMetadataHeader!;
        var id = new BlobContentId(header.Id);
        if (id.Guid != codeView.Guid)
            return false;
        return id.Stamp == entry.Stamp;
    }

    private static bool IsUnreadable(Exception e) =>
        e is BadImageFormatException or IOException or UnauthorizedAccessException or InvalidOperationException or InvalidDataException;

    public IList<SequencePoint> GetSequencePoints(MethodDefinitionHandle method)
    {
        var points = new List<SequencePoint>();
        var debugInformation = Metadata.GetMethodDebugInformation(method);
        foreach (var point in debugInformation.GetSequencePoints())
            points.Add(ToSequencePoint(point));
        return points;
    }

    private SequencePoint ToSequencePoint(System.Reflection.Metadata.SequencePoint point)
    {
        return new SequencePoint
        {
            Offset = point.Offset,
            StartLine = point.StartLine,
            StartColumn = point.StartColumn,
            EndLine = point.EndLine,
            EndColumn = point.EndColumn,
            DocumentUrl = DocumentNameOf(point.Document),
        };
    }

    private string DocumentNameOf(DocumentHandle handle)
    {
        if (handle.IsNil)
            return "";
        var document = Metadata.GetDocument(handle);
        return Metadata.GetString(document.Name);
    }

    public IList<Variable> GetVariables(MethodDefinitionHandle method)
    {
        var variables = new List<Variable>();
        foreach (var handle in LocalsOf(method))
        {
            var local = Metadata.GetLocalVariable(handle);
            variables.Add(new Variable(local.Index, Metadata.GetString(local.Name)));
        }
        return variables;
    }

    public bool TryGetName(MethodDefinitionHandle method, int index, out string name)
    {
        name = null!;
        var handle = FindLocal(method, index);
        if (handle.IsNil)
            return false;
        var local = Metadata.GetLocalVariable(handle);
        name = Metadata.GetString(local.Name);
        return true;
    }

    /// <summary>The tuple element names and <c>dynamic</c> flags the compiler recorded for local
    /// <paramref name="index"/>, which its signature cannot carry.</summary>
    public bool TryGetExtraTypeInfo(MethodDefinitionHandle method, int index, out PdbExtraTypeInfo extraTypeInfo)
    {
        extraTypeInfo = default;
        var local = FindLocal(method, index);
        if (local.IsNil)
            return false;
        foreach (var handle in Metadata.GetCustomDebugInformation(local))
        {
            var information = Metadata.GetCustomDebugInformation(handle);
            var kind = Metadata.GetGuid(information.Kind);
            if (kind == KnownGuids.TupleElementNames)
                extraTypeInfo.TupleElementNames = ReadTupleElementNames(information.Value);
            else if (kind == KnownGuids.DynamicLocalVariables)
                extraTypeInfo.DynamicFlags = ReadDynamicFlags(information.Value);
        }
        if (extraTypeInfo.TupleElementNames != null)
            return true;
        return extraTypeInfo.DynamicFlags != null;
    }

    private LocalVariableHandle FindLocal(MethodDefinitionHandle method, int index)
    {
        foreach (var handle in LocalsOf(method))
        {
            if (Metadata.GetLocalVariable(handle).Index == index)
                return handle;
        }
        return default;
    }

    private IEnumerable<LocalVariableHandle> LocalsOf(MethodDefinitionHandle method)
    {
        foreach (var scopeHandle in Metadata.GetLocalScopes(method))
        {
            var scope = Metadata.GetLocalScope(scopeHandle);
            foreach (var local in scope.GetLocalVariables())
                yield return local;
        }
    }

    private string?[] ReadTupleElementNames(BlobHandle blob)
    {
        var reader = Metadata.GetBlobReader(blob);
        var names = new List<string?>();
        while (reader.RemainingBytes > 0)
            names.Add(ReadTupleElementName(ref reader));
        return names.ToArray();
    }

    /// <summary>A null-terminated UTF-8 name; null for an element left unnamed.</summary>
    private static string? ReadTupleElementName(ref BlobReader reader)
    {
        var length = reader.IndexOf(0);
        var name = reader.ReadUTF8(length);
        reader.ReadByte();
        if (string.IsNullOrWhiteSpace(name))
            return null;
        return name;
    }

    /// <summary>One flag per bit, lowest bit of each byte first.</summary>
    private bool[] ReadDynamicFlags(BlobHandle blob)
    {
        var bytes = Metadata.GetBlobBytes(blob);
        var flags = new bool[bytes.Length * BitsPerByte];
        for (var i = 0; i < flags.Length; i++)
        {
            var bit = 1 << (i % BitsPerByte);
            flags[i] = (bytes[i / BitsPerByte] & bit) != 0;
        }
        return flags;
    }
}
