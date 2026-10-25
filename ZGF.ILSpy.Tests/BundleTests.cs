using System.IO.Compression;
using System.Text;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class BundleTests
{
    private const string BundleName = "App.exe";
    private const string TestAssemblyName = "ZGF.ILSpy.Tests";

    /// <summary>The SHA-256 of ".net core bundle", which the host carries right after the offset of the manifest.</summary>
    private static readonly byte[] Signature =
    [
        0x8b, 0x12, 0x02, 0xb9, 0x6a, 0x61, 0x20, 0x38,
        0x72, 0x7b, 0x93, 0x02, 0x14, 0xd7, 0xa0, 0x32,
        0x13, 0xf5, 0xb9, 0xe6, 0xef, 0xae, 0x33, 0x18,
        0xee, 0x3b, 0x2d, 0xce, 0x24, 0xb3, 0x6a, 0xae,
    ];

    private static string CoreAssemblyPath => typeof(AssemblyList).Assembly.Location;

    [Fact]
    public void A_single_file_bundle_opens_as_a_package_of_its_files()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();

        var bundle = list.Open(WriteBundle(temp));

        Assert.True(bundle.IsPackage);
        Assert.Equal(BundleName, bundle.Root.Text);
        Assert.Equal([NodeKind.Resource, NodeKind.Assembly, NodeKind.Assembly], bundle.Root.Children.Select(c => c.Kind));
        Assert.Equal("App.deps.json", bundle.Root.Children[0].Text);
        Assert.Equal(["ZGF.ILSpy.Core", TestAssemblyName], list.Assemblies.Select(a => a.Name));
    }

    [Fact]
    public void A_compressed_assembly_in_a_bundle_decompiles_and_finds_the_assembly_beside_it()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();
        var bundle = list.Open(WriteBundle(temp));
        var assembly = list.Assemblies.Single(a => a.Name == TestAssemblyName);

        var calculator = assembly.Calculator().Decompile(DecompilationLanguage.CSharp);
        var core = list.FindNode(assembly.Root.Child("References", "ZGF.ILSpy.Core").Target!);

        Assert.False(calculator.IsError);
        Assert.Contains("class Calculator<T>", calculator.GetText());
        Assert.Equal(Path.Combine(bundle.FilePath, "ZGF.ILSpy.Core.dll"), core!.Assembly.FilePath);
    }

    [Fact]
    public void A_file_of_a_bundle_that_is_no_assembly_shows_its_contents()
    {
        using var temp = new TempDir();
        var bundle = new AssemblyList().Open(WriteBundle(temp));

        var deps = bundle.Root.Child("App.deps.json");

        Assert.Equal("{}", Encoding.UTF8.GetString(deps.ReadResource()));
    }

    [Fact]
    public void An_entry_the_manifest_places_past_the_end_of_the_file_is_not_read()
    {
        using var temp = new TempDir();
        var path = WriteBundle(temp, ("Broken.dll", [1, 2, 3], false, 1_000_000));
        var bundle = new AssemblyList().Open(path);

        var broken = bundle.Root.Child("Broken.dll");

        Assert.Equal(NodeKind.Resource, broken.Kind);
        Assert.Throws<InvalidDataException>(() => broken.ReadResource());
    }

    [Fact]
    public void A_native_executable_that_is_no_bundle_fails_to_load()
    {
        using var temp = new TempDir();
        var path = temp.Write("Host.exe", Encoding.ASCII.GetBytes("MZ this is no bundle and no assembly"));

        var failed = new AssemblyList().Open(path);

        Assert.True(failed.HasLoadError);
    }

    private static string WriteBundle(TempDir temp)
    {
        return WriteBundle(temp,
            ("ZGF.ILSpy.Tests.dll", File.ReadAllBytes(Trees.TestAssemblyPath), true, 0),
            ("ZGF.ILSpy.Core.dll", File.ReadAllBytes(CoreAssemblyPath), false, 0),
            ("App.deps.json", Encoding.UTF8.GetBytes("{}"), false, 0));
    }

    /// <summary>A bundle as the SDK lays one out: the host, which carries the manifest's offset and the
    /// signature; each file, deflated when asked; then a version 6 manifest. A file's declared size is
    /// its own unless <c>ClaimedSize</c> says otherwise.</summary>
    private static string WriteBundle(TempDir temp, params (string Path, byte[] Data, bool Compress, long ClaimedSize)[] files)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);
        writer.Write(new byte[16]);
        var offsetSlot = stream.Position;
        writer.Write(0L);
        writer.Write(Signature);
        var entries = new List<(long Offset, long Size, long CompressedSize, string Path)>();
        foreach (var file in files)
            entries.Add(WriteFile(writer, file));
        var manifestOffset = stream.Position;
        WriteManifest(writer, entries);
        stream.Position = offsetSlot;
        writer.Write(manifestOffset);
        writer.Flush();
        return temp.Write(BundleName, stream.ToArray());
    }

    private static (long Offset, long Size, long CompressedSize, string Path) WriteFile(BinaryWriter writer, (string Path, byte[] Data, bool Compress, long ClaimedSize) file)
    {
        var offset = writer.BaseStream.Position;
        var stored = file.Compress ? Deflate(file.Data) : file.Data;
        writer.Write(stored);
        var size = file.ClaimedSize != 0 ? file.ClaimedSize : file.Data.Length;
        var compressedSize = file.Compress ? stored.Length : 0;
        return (offset, size, compressedSize, file.Path);
    }

    private static byte[] Deflate(byte[] data)
    {
        using var compressed = new MemoryStream();
        using (var deflate = new DeflateStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            deflate.Write(data);
        return compressed.ToArray();
    }

    private static void WriteManifest(BinaryWriter writer, List<(long Offset, long Size, long CompressedSize, string Path)> entries)
    {
        writer.Write(6u);
        writer.Write(0u);
        writer.Write(entries.Count);
        writer.Write("bundle-id");
        writer.Write(0L);
        writer.Write(0L);
        writer.Write(0L);
        writer.Write(0L);
        writer.Write(0UL);
        foreach (var entry in entries)
        {
            writer.Write(entry.Offset);
            writer.Write(entry.Size);
            writer.Write(entry.CompressedSize);
            writer.Write((byte)0);
            writer.Write(entry.Path);
        }
    }
}
