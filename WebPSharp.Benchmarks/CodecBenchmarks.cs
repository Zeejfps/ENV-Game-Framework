using BenchmarkDotNet.Attributes;
using WebPSharp.Api;

namespace WebPSharp.Benchmarks;

/// <summary>
/// Throughput, allocation, and compression-ratio benchmarks for the lossless (VP8L) codec across
/// several image contents and sizes.
/// </summary>
[MemoryDiagnoser]
public class CodecBenchmarks
{
    /// <summary>The image edge length under test.</summary>
    [Params(64, 256, 512)]
    public int Size { get; set; }

    /// <summary>The synthetic image content: noise (worst case) or a photographic-like gradient.</summary>
    [Params("noise", "gradient")]
    public string Content { get; set; } = "noise";

    private WebPImage _image = null!;
    private byte[] _encoded = null!;

    [GlobalSetup]
    public void Setup()
    {
        var pixels = new byte[Size * Size * 4];
        if (Content == "noise")
        {
            new Random(1).NextBytes(pixels);
        }
        else
        {
            for (var y = 0; y < Size; y++)
                for (var x = 0; x < Size; x++)
                {
                    var i = (y * Size + x) * 4;
                    pixels[i] = (byte)(x ^ y);
                    pixels[i + 1] = (byte)(x + y);
                    pixels[i + 2] = (byte)(x * 2 - y);
                    pixels[i + 3] = 255;
                }
        }

        _image = WebPImage.CreateRgba(Size, Size, pixels);
        _encoded = WebP.Encode(_image);
    }

    /// <summary>Encodes the image to a lossless WebP.</summary>
    [Benchmark]
    public byte[] EncodeLossless() => WebP.Encode(_image);

    /// <summary>Decodes the pre-encoded lossless WebP.</summary>
    [Benchmark]
    public WebPImage DecodeLossless() => WebP.Decode(_encoded);

    /// <summary>The compression ratio of the pre-encoded image (reported, not timed).</summary>
    public double CompressionRatio => (double)(Size * Size * 4) / _encoded.Length;
}
