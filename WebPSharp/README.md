# WebPSharp

A pure C# WebP encoder/decoder with zero native dependencies. Only the .NET Base Class Library is
used — no `libwebp`, no `ImageSharp`, no Skia. Targets .NET 10, `nullable enable`, warnings-as-errors,
XML docs, and analyzers.

WebPSharp is part of the same ecosystem as `PngSharp`/`JpegSharp` and mirrors their conventions:
a single static facade, a sealed image type, a small two-tier exception model, `internal sealed`
engine classes, and `InternalsVisibleTo` the test project.

## Status at a glance

| Feature | Decode | Encode |
| --- | --- | --- |
| Lossless (VP8L) | ✅ full — all 4 transforms, color cache, LZ77, meta-Huffman | ✅ transforms, LZ77, meta-Huffman |
| RIFF container | ✅ | ✅ |
| Extended (VP8X) | ✅ | ✅ |
| Metadata (ICC / EXIF / XMP) | ✅ | ✅ |
| Unknown chunk preservation | ✅ | ✅ |
| Animation (ANIM / ANMF) | ✅ blend, dispose, timing, loop, composition | ✅ |
| Lossy (VP8) | ✅ full key-frame decode, pixel-exact vs `dwebp` | ✅ intra key frames, pixel-exact vs `dwebp` (see Limitations) |

## Quick start

```csharp
using WebPSharp.Api;

// Encode an RGBA image losslessly.
var image = WebPImage.CreateRgba(width, height, rgbaBytes);
byte[] webp = WebP.Encode(image);
WebP.Save(image, "out.webp");

// Or encode lossily (VP8 intra key frame) at a target quality.
byte[] lossy = WebP.Encode(image, new WebPEncoderOptions { Lossless = false, Quality = 80 });

// Decode.
WebPImage decoded = WebP.Load("out.webp");           // or WebP.Decode(byte[]) / Decode(Stream)
ReadOnlySpan<byte> pixels = decoded.PixelData;       // RGBA, row-major

// Inspect without decoding pixels.
WebPInfo info = WebP.Identify(webp);                  // width, height, format, alpha, animation

// Metadata round-trips via WebPImage.Metadata.
image.Metadata = new WebPMetadata { IccProfile = icc, Exif = exif, Xmp = xmp };
byte[] extended = WebP.Encode(image);                 // emits a VP8X container

// Animation.
var anim = new WebPAnimation(canvasW, canvasH) { LoopCount = 0 };
anim.Frames.Add(new WebPFrame(frameImage, x: 0, y: 0, durationMs: 100,
    blend: WebPBlendMethod.Over, disposal: WebPDisposalMethod.Background));
byte[] animated = WebP.EncodeAnimation(anim);
WebPAnimation back = WebP.DecodeAnimation(animated);
IReadOnlyList<byte[]> composited = back.RenderFrames(); // full-canvas RGBA per frame
```

## Public API

- **`WebP`** — the stateless, thread-safe facade: `Identify`, `Decode`, `Encode`, `Load`, `Save`,
  `EncodeAnimation`, `DecodeAnimation`.
- **`WebPImage`** — an in-memory RGB/RGBA raster (`Width`, `Height`, `Format`, `PixelData`,
  `Metadata`), with `CreateRgb`/`CreateRgba` factories.
- **`WebPInfo`** — lightweight structural info from header parsing only.
- **`WebPMetadata`** / **`WebPUnknownChunk`** — ICC/EXIF/XMP payloads and preserved chunks.
- **`WebPAnimation`** / **`WebPFrame`** — animation model with `RenderFrames` composition.
- **`WebPEncoderOptions`** / **`WebPDecoderOptions`** — `Lossless`, `Quality`, `Effort`;
  `MaxPixels`, `ReadMetadata`.
- **Exceptions** (`WebPSharp.Api.Exceptions`): `WebPException` → `WebPFormatException` →
  `WebPCorruptException`. Format violations at the container/header level throw
  `WebPFormatException`; corrupt compressed payloads throw `WebPCorruptException`. Argument
  validation uses standard BCL exceptions.

## Architecture

```
WebPSharp/
  Api/          WebP facade, WebPImage/Info/Metadata, options, enums, exceptions
  Container/    RIFF reader/writer, FourCc, chunk ids, VP8X/header parsing
  Vp8L/         Lossless codec: bit IO, prefix codes, color cache, LZ77, decoder/encoder
    Transforms/ predictor, cross-color, subtract-green, color-indexing
  Vp8/          Lossy primitives: boolean coder, transforms, prediction, loop filter, YUV
```

### RIFF container

`RiffReader` is a forward-only, allocation-free `ref struct` that validates the outer
`RIFF….WEBP` wrapper and yields each chunk's payload as a `ReadOnlyMemory<byte>` view (no copy),
handling the even-size padding rule. `RiffWriter` streams chunks to a seekable stream and
back-patches the RIFF size on `Complete`, so payloads are never fully buffered. `WebPHeaderReader`
extracts dimensions and feature flags from the VP8/VP8L/VP8X headers for `Identify`.

### VP8L (lossless) pipeline

Decode: parse the 5-byte header → read the transform list → decode the entropy-coded ARGB image →
apply inverse transforms in reverse order → emit RGBA.

- **Bit IO** — `Vp8LBitReader`/`Vp8LBitWriter`, LSB-first with a 64-bit accumulator.
- **Prefix codes** — canonical Huffman (`PrefixCode`, `HuffmanTree`, `PrefixCodeWriter`) with
  full code-length-code serialization (`Vp8LHuffman`), plus a length-limited code builder
  (`HuffmanLengthBuilder`).
- **Entropy image** — optional color cache, optional meta-Huffman (per-tile group selection), and
  the literal / LZ77-copy / cache-index pixel loop (`LzPrefix`, `ColorCache`, `Vp8LLz77`).
- **Transforms** — predictor (14 modes), cross-color, subtract-green, and color-indexing (palette
  with pixel bundling). The palette transform changes the working image width; the decoder threads
  that width change through the inverse pipeline.

Encode mirrors this: optional transforms, a hash-chain LZ77 matcher, frequency-optimized Huffman
codes, and deterministic output. Distances are emitted as plane codes above the near-distance range,
so the output decodes on any compliant decoder without the near-distance lookup table.

### Animation pipeline

An extended container with the animation flag, an `ANIM` chunk (background color + loop count), and
one `ANMF` chunk per frame (16-byte header + a nested image chunk). `WebPAnimation.RenderFrames`
composites frames onto the canvas with source/over blending and background disposal.

## Performance decisions

- `ref struct` readers (`RiffReader`, `Vp8LBitReader`, `Vp8BooleanDecoder`) avoid heap allocation
  and copy payloads by reference.
- `Span<T>`/`ReadOnlySpan<T>`, `stackalloc` for small fixed buffers, and `MethodImpl`
  aggressive-inlining on hot arithmetic (predictors, bit ops, transforms).
- The bit reader refills a 64-bit accumulator a byte at a time, so a typical multi-bit read is a
  mask and a shift.
- The LZ77 matcher uses a bounded hash chain with a capped search depth for deterministic,
  predictable cost.
- Optimization was guided by the `--smoke` timings and the BenchmarkDotNet suite in
  `WebPSharp.Benchmarks` (encode/decode throughput and allocations).

## Robustness

The decoders are hardened against malformed input: readers are EOF-tolerant, every back-reference,
cache index, chunk size, and code-length run is bounds-checked, and `MaxPixels` guards against
allocation bombs. A fuzz harness applies ~10,000 truncation / bit-flip / garbage mutations and
asserts every one either decodes or throws a `WebPException` — never a crash or hang.

## Limitations

- **Lossy VP8 encoding is intra-only and not size-optimal.** The encoder produces correct,
  standards-compliant key frames (validated pixel-exact against `dwebp`) but favors simplicity over
  compression: whole-block 16×16/8×8 prediction only (no 4×4 `B_PRED`), no in-loop filter, no
  segmentation, default coefficient probabilities, and nearest-rounding quantization. It has no
  rate-distortion optimization or trellis quantization, so files are larger than libwebp's for the
  same quality. None of this affects correctness — the decoder reconstructs exactly what the encoder
  emits — only output size.
- **Lossy alpha and lossy animation are not yet written.** A lossy RGBA image is encoded as opaque
  (the `ALPH` chunk is not emitted), and `EncodeAnimation` writes only lossless frames.

## Extension points

- The lossy encoder (`Vp8/Vp8Encoder.cs`) is a direct bitstream mirror of the decoder; the natural
  next steps (in-loop filter, `B_PRED`, RD/token-cost search, probability adaptation) are all
  quality/size improvements that plug into the existing prediction → transform → quantize → token
  pipeline without changing the decoder contract.
- `WebPEncoderOptions.Effort` is the hook for transform/back-reference search strategies (lossless).
- Unknown RIFF chunks are preserved through decode/encode, so new chunk types round-trip without
  code changes.

## Testing

460+ xUnit tests: unit tests for every internal component, exact lossless round-trips (single
pixel, odd dimensions, noise, gradients, solid, transparent, per-transform and combined), lossy VP8
decode and encode validated pixel-exact against `dwebp`, lossy encode round-trip/PSNR/monotonicity
tests, metadata and animation round-trips, corruption/fuzz robustness, and property-style randomized
round-trips. `WebPSharp.Benchmarks --smoke` doubles as an end-to-end correctness check.
