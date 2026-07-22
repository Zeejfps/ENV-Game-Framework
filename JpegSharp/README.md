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

// Files
image.EncodeToFile("out.jpg");
JpegImage loaded = Jpeg.DecodeFromFile("out.jpg");

// Streams
image.EncodeToStream(stream, options);
JpegImage fromStream = Jpeg.DecodeFromStream(stream);

// Async stream/file I/O (the stream read/write is async; the codec work is CPU-bound)
await image.EncodeToStreamAsync(stream, options, cancellationToken);
JpegImage decodedAsync = await Jpeg.DecodeFromStreamAsync(stream, cancellationToken: cancellationToken);
await image.EncodeToFileAsync("out.jpg");
JpegImage fromFile = await Jpeg.DecodeFromFileAsync("out.jpg");
```

Every file and stream entry point has both a synchronous and an asynchronous (`…Async`,
cancellable) form: `EncodeToStream`/`DecodeFromStream` and `EncodeToFile`/`DecodeFromFile`. The
file overloads also accept an `IFileSystem` to route reads and writes through custom storage
instead of disk.

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
GPU/`WriteableBitmap` order. Alpha is always fully opaque (255) since JPEG carries no alpha.
Grayscale luminance is replicated across R, G and B, and CMYK is converted to RGB with the
standard multiplicative model (the decoder has already undone any Adobe inversion / YCCK
transform, so the conversion is applied to normalized CMYK).

Every helper has a zero-allocation overload that fills a caller-supplied `Span<int>` — reuse one
buffer across frames or decodes to avoid per-call allocation:

```csharp
int[] buffer = new int[image.Width * image.Height];
image.ToRgba8888(buffer);                                 // or image.ToPackedPixels(buffer, format)
```

When you need RGB *bytes* rather than packed integers — for RGB24 interop, or to normalize a
grayscale or CMYK image before re-encoding it as RGB — use `ToRgb`, which returns a new
three-channel `JpegImage` (using the same conversions described above):

```csharp
JpegImage rgb = Jpeg.DecodeFromFile("cmyk.jpg").ToRgb();
byte[] rgb24 = rgb.PixelData;             // tightly packed R,G,B, three bytes per pixel
rgb.EncodeToFile("normalized.jpg");       // re-encode the CMYK/grayscale source as RGB
```

`ToRgb` always copies, so the result never shares its `PixelData` with the source even when the
source is already RGB.

Going the other way — encoding a packed pixel buffer such as a framebuffer or `WriteableBitmap` —
the `CreateFrom…` factories are the inverse of the packing helpers. Alpha is discarded, since JPEG
stores no alpha:

```csharp
int[] framebuffer = ...;   // width * height packed pixels
JpegImage image = JpegImage.CreateFromBgra8888(width, height, framebuffer);
image.EncodeToFile("frame.jpg");

// Or choose the layout at runtime:
JpegImage img = JpegImage.CreateFromPackedPixels(width, height, framebuffer, PackedPixelFormat.Rgba8888);
```

### High-precision (12-bit) images

JPEG's DCT modes allow 8- or 12-bit samples (ITU-T T.81). 12-bit images (medical/prepress) decode
to and encode from `JpegImage16`, which stores `ushort` samples right-aligned in
`[0, MaxSampleValue]` (0–4095 for 12-bit):

```csharp
JpegImage16 img = Jpeg.Decode16(twelveBitJpeg);   // grayscale or RGB/YCbCr
byte[] jpeg     = img.Encode16(new JpegEncoderOptions { Quality = 90 });
```

Both `JpegImage` and `JpegImage16` implement `IJpegImage`, so display code can be written once
against the interface and use `Precision` to detect which it holds. `Jpeg.DecodeAny` returns the
right concrete type per the file's precision:

```csharp
IJpegImage img = Jpeg.DecodeAny(anyJpeg);   // JpegImage (8-bit) or JpegImage16 (12-bit)
int[] preview  = img.ToRgba8888();          // precision-agnostic 8-bit preview, tone-mapped
```

`JpegImage16.To8Bit()` down-shifts to a plain `JpegImage`, which bridges to every 8-bit API
(`ToRgb`, the 32-bit packing helpers). For full-precision access, the 64-bit packing helpers
mirror the 8-bit ones with 16 bits per channel (`long` per pixel):

```csharp
long[] rgba16 = image16.ToRgba16161616();                     // or ToPackedPixels64(format)
JpegImage16 back = JpegImage16.CreateFromRgba16161616(w, h, 12, rgba16);
```

Channel values in the 64-bit packing are the native right-aligned samples (not rescaled to full
16-bit range); alpha is `MaxSampleValue`. The `JpegImage16` container permits 9–16 bit for
packing/interop, but the codec encodes/decodes 12-bit only (8/12 are the JPEG DCT precisions).
Grayscale, RGB/YCbCr, and CMYK/YCCK are all supported at 12-bit — the same color spaces, chroma
subsampling, and baseline/progressive modes as the 8-bit codec. The 64-bit packing helpers convert
CMYK to RGB (multiplicative model) just like the 8-bit `ToPackedPixels`.

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

- Sample precision is 8-bit or 12-bit (the JPEG DCT precisions). 12-bit decodes/encodes via
  `JpegImage16` across all color spaces (grayscale, RGB/YCbCr, CMYK/YCCK), baseline and
  progressive, with chroma subsampling — see *High-precision (12-bit) images*.
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
