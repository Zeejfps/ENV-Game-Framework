using BenchmarkDotNet.Attributes;
using JpegSharp.Api;

namespace JpegSharp.Benchmarks;

/// <summary>
/// Measures encode and decode throughput and allocations across image sizes and sampling
/// modes. Run with <c>dotnet run -c Release -- --filter *</c>.
/// </summary>
[MemoryDiagnoser]
public class CodecBenchmarks
{
    private JpegImage _image = null!;
    private byte[] _baseline = null!;
    private byte[] _progressive = null!;

    /// <summary>The square edge length of the test image in pixels.</summary>
    [Params(64, 256, 512)]
    public int Size { get; set; }

    /// <summary>The chroma subsampling layout used for encoding.</summary>
    [Params(ChromaSubsampling.Samp444, ChromaSubsampling.Samp420)]
    public ChromaSubsampling Subsampling { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var pixels = new byte[Size * Size * 3];
        var rng = new Random(12345);
        // A blend of smooth gradient and noise approximates real photographic content.
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var i = (y * Size + x) * 3;
                pixels[i] = (byte)((x * 255 / Size + rng.Next(20)) & 0xFF);
                pixels[i + 1] = (byte)((y * 255 / Size + rng.Next(20)) & 0xFF);
                pixels[i + 2] = (byte)(((x + y) * 255 / (2 * Size) + rng.Next(20)) & 0xFF);
            }
        }

        _image = JpegImage.CreateRgb(Size, Size, pixels);
        _baseline = Jpeg.Encode(_image, new JpegEncoderOptions { Quality = 85, Subsampling = Subsampling });
        _progressive = Jpeg.Encode(_image, new JpegEncoderOptions { Quality = 85, Subsampling = Subsampling, Progressive = true });
    }

    [Benchmark]
    public byte[] EncodeBaseline() =>
        Jpeg.Encode(_image, new JpegEncoderOptions { Quality = 85, Subsampling = Subsampling });

    [Benchmark]
    public byte[] EncodeOptimized() =>
        Jpeg.Encode(_image, new JpegEncoderOptions { Quality = 85, Subsampling = Subsampling, OptimizeHuffman = true });

    [Benchmark]
    public byte[] EncodeProgressive() =>
        Jpeg.Encode(_image, new JpegEncoderOptions { Quality = 85, Subsampling = Subsampling, Progressive = true });

    [Benchmark]
    public JpegImage DecodeBaseline() => Jpeg.Decode(_baseline);

    [Benchmark]
    public JpegImage DecodeProgressive() => Jpeg.Decode(_progressive);
}
