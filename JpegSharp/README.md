# JpegSharp

A pure C# JPEG encoder and decoder with zero native dependencies. JpegSharp targets the
.NET Base Class Library only — no `libjpeg`, Skia, ImageSharp, or other image libraries.

## Features

**Decoder**
- Baseline sequential DCT (SOF0) and extended sequential (SOF1)
- Progressive DCT (SOF2): spectral selection *and* successive-approximation refinement
- Huffman entropy decoding with restart intervals
- Grayscale, RGB (from YCbCr), CMYK and Adobe YCCK output
- JFIF (APP0), Exif (APP1), ICC profile (APP2, multi-segment), Adobe (APP14) and comment (COM) metadata
- Arbitrary chroma sampling factors: 4:4:4, 4:2:2, 4:2:0, 4:1:1 and beyond
- Header-only inspection via `Jpeg.Identify`
- Robust, fuzz-tested error reporting — malformed input always raises a `JpegException`

**Encoder**
- Baseline and progressive output
- Quality factor (1–100) and configurable chroma subsampling
- RGB encoded as YCbCr (default) or stored directly (`JpegRgbEncoding.Rgb`, no color-transform loss)
- CMYK stored directly or as Adobe YCCK (`CmykAsYcck`)
- Standard, optimized, or fully custom Huffman tables
- Custom quantization tables
- Restart intervals
- Metadata writing (JFIF density, Exif, ICC, comments, arbitrary APPn)
- Deterministic output

## Quick start

```csharp
using JpegSharp.Api;

// Encode
var image = JpegImage.CreateRgb(width, height, rgbBytes);
byte[] jpeg = Jpeg.Encode(image, new JpegEncoderOptions { Quality = 90 });

// Decode
JpegImage decoded = Jpeg.Decode(jpeg);
byte[] pixels = decoded.PixelData; // interleaved, ComponentCount bytes per pixel

// Inspect without decoding pixels
JpegInfo info = Jpeg.Identify(jpeg);

// Files and streams
Jpeg.Save(image, "out.jpg");
JpegImage loaded = Jpeg.Load("out.jpg");

// Async stream/file I/O (the stream read/write is async; the codec work is CPU-bound)
await Jpeg.EncodeAsync(image, stream, options, cancellationToken);
JpegImage fromStream = await Jpeg.DecodeAsync(stream, cancellationToken: cancellationToken);
await Jpeg.SaveAsync(image, "out.jpg");
JpegImage fromFile = await Jpeg.LoadAsync("out.jpg");
```

Every I/O entry point has both a synchronous and an asynchronous (`…Async`, cancellable) form:
`Encode`/`Decode`/`Load`/`Save`.

## Pixel access

`JpegImage.PixelData` is the raw interleaved sample buffer (`ComponentCount` bytes per pixel).
When you need one packed 32-bit color per pixel — for a texture upload, a `WriteableBitmap`, or
similar — use the packing helpers instead of doing the shifts by hand:

```csharp
JpegImage image = Jpeg.Decode(jpeg);

int[] rgba = image.ToRgba8888();   // named wrappers, one per format
int[] argb = image.ToArgb8888();
int[] bgra = image.ToBgra8888();
int[] abgr = image.ToAbgr8888();

// Or choose the layout at runtime:
int[] pixels = image.ToPackedPixels(PackedPixelFormat.Bgra8888);
```

Each `PackedPixelFormat` is named most-significant-byte first, so the value is unambiguous
regardless of endianness — `Rgba8888` packs a pixel as `(R << 24) | (G << 16) | (B << 8) | A`.
`Argb8888` matches the `int` layout used by `System.Drawing`, and `Bgra8888` is the common
GPU/`WriteableBitmap` order. Alpha is always fully opaque (255) since JPEG carries no alpha, and
grayscale luminance is replicated across R, G and B.

Every helper has a zero-allocation overload that fills a caller-supplied `Span<int>` — reuse one
buffer across frames or decodes to avoid per-call allocation:

```csharp
int[] buffer = new int[image.Width * image.Height];
image.ToRgba8888(buffer);                                 // or image.ToPackedPixels(buffer, format)
```

CMYK images have no direct RGB packing and throw `NotSupportedException`; convert to RGB first.

## Architecture

The codec is organized into small, independently testable components:

| Namespace              | Responsibility |
|------------------------|----------------|
| `JpegSharp.Api`        | Public entry points (`Jpeg`, `JpegImage`, options, `JpegInfo`, `JpegMetadata`, exceptions) |
| `JpegSharp.Transforms` | ZigZag ordering and the reference forward/inverse DCT |
| `JpegSharp.Quantization` | Quantization tables (Annex K, quality scaling) and the quantizer |
| `JpegSharp.Bitstream`  | MSB-first `BitReader`/`BitWriter` with byte-stuffing and marker handling |
| `JpegSharp.Huffman`    | Canonical table model, decode/encode, optimized table generation, standard tables |
| `JpegSharp.Markers`    | Marker constants and the segment reader/writer |
| `JpegSharp.Color`      | YCbCr/RGB/CMYK/YCCK conversion and chroma up/downsampling |
| `JpegSharp.Coding`     | Sequential block entropy coding (DC prediction + AC run-length) |
| `JpegSharp.Encoder`    | Baseline and progressive encoders |
| `JpegSharp.Decoder`    | Baseline and progressive decoders |

## Pipeline

**Encode:** pixels → color transform (RGB→YCbCr) → chroma downsample → per-8×8-block level
shift → forward DCT → quantize → zig-zag → entropy code (Huffman) → marker assembly.

**Decode:** marker parse → entropy decode into coefficient buffers → dequantize → inverse DCT
→ level shift → chroma upsample → color transform → interleaved pixels. Progressive decoding
accumulates coefficients across multiple scans before reconstruction.

## Performance

Performance is a first-class concern. The implementation favors:
- `Span<T>`/`ReadOnlySpan<T>` and `stackalloc` throughout the hot paths — block processing is allocation-free
- 16-bit fixed-point color conversion (matching libjpeg coefficients)
- `ref struct` bit reader over `ReadOnlySpan<byte>` for zero-copy entropy decoding
- `AggressiveInlining` on the tightest inner routines
- No LINQ, boxing, or virtual dispatch in decoding/encoding loops

Benchmarks live in `JpegSharp.Benchmarks` (BenchmarkDotNet): `dotnet run -c Release --project
JpegSharp.Benchmarks -- --filter *`. For a self-contained throughput/allocation report that
does not depend on BenchmarkDotNet's project discovery, use `-- --measure`; `-- --smoke` is a
quick timing check. Indicative single-thread throughput (RGB, 4:2:0, quality 85): roughly
40–47 megapixels/second for both encode and decode of a 512×512 image, with allocation
proportional to image size.

## Limitations

- Sample precision is 8-bit (12-bit is reported by `Jpeg.Identify` but not decoded).
- Arithmetic coding (ISO/IEC 10918-1 Annex D) is not implemented; the architecture isolates
  the entropy stage so it can be added without disturbing the rest of the pipeline. This is
  the only optional feature intentionally left out.
- Chroma upsampling uses centered bilinear interpolation on decode.
- `OptimizeHuffman` applies to baseline encoding; progressive encoding uses the standard
  Annex K tables.

## Extension points

- **Custom tables:** supply `QuantizationTable` and `HuffmanTable` instances through
  `JpegEncoderOptions`.
- **Metadata:** attach a `JpegMetadata` to embed Exif/ICC/comments; the decoder always exposes
  what it finds via `JpegImage.Metadata`.
- **Decoder guards:** `JpegDecoderOptions` bounds allocation (`MaxPixels`) and can skip metadata.
