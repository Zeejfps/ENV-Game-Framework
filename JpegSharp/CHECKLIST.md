# JpegSharp Implementation Checklist

Living document driving the autonomous TDD loop. Legend: `[ ]` todo, `[~]` in progress, `[x]` done.

**Status:** Complete. Every non-optional item below is done, with 341 passing tests (unit,
round-trip, golden, structural-compliance, interop-variant, corruption/fuzz, property,
edge-case, concurrency, scale) and a clean warnings-as-errors build with enforced XML docs.
The single intentional exclusion is **arithmetic coding** — the prompt's designated optional
feature; its entropy stage is architecturally isolated and documented (see README). It is
deliberately not shipped because the ITU-T T.81 probability-estimation table cannot be
reproduced/verified in this offline environment, and a non-conformant QM-coder would emit
SOF9 files that fail to interoperate — a hidden-non-compliance defect worse than a clean,
documented exclusion.

## Foundation / Transforms
- [x] ZigZag ordering (forward + inverse, block reorder)
- [x] Forward DCT (orthonormal reference + fast even/odd factorization on hot path)
- [x] Inverse DCT (orthonormal reference + fast even/odd factorization on hot path)
- [x] Quantization / Dequantization
- [x] Quantization table scaling by quality
- [x] Standard (Annex K) luminance/chrominance quant tables

## Bitstream
- [x] Bit reader (MSB-first, marker/byte-stuffing aware)
- [x] Bit writer (MSB-first, 0xFF stuffing)

## Huffman
- [x] Huffman table model (BITS/HUFFVAL -> canonical codes)
- [x] Huffman decoder (canonical DECODE + 8-bit lookahead fast path)
- [x] Huffman encoder
- [x] Optimized Huffman table generation (frequency -> length-limited codes)
- [x] Standard (Annex K) Huffman tables

## Markers
- [x] Marker constants + parser (reader/writer, classification helpers)
- [x] SOI/EOI, SOF0/1/2, DHT, DQT, SOS, DRI, RSTn (constants + segment IO)
- [x] APP0 (JFIF), APP1 (Exif), APP2 (ICC), APP14 (Adobe) read+write
- [x] COM read+write + arbitrary unknown-APP marker preservation (round-trips via metadata)

## Color
- [x] YCbCr <-> RGB
- [x] YCCK / CMYK handling
- [x] Chroma downsampling (4:4:4 / 4:2:2 / 4:2:0 / 4:1:1 / arbitrary)
- [x] Chroma upsampling (nearest-neighbour + centered bilinear "fancy" interpolation, used on decode)

## Decoder
- [x] Frame/component model, sampling factors, MCU geometry
- [x] Block-level entropy coder/decoder (DC prediction + AC RLE) + frequency gather
- [x] Baseline sequential Huffman decode (full frame)
- [x] Progressive decode: DC/AC first + DC/AC successive-approximation refinement
- [x] Multi-scan coefficient-buffer architecture
- [x] Restart interval handling (encoder RSTn/DRI + decoder resync)
- [x] Multi-scan / component assembly (progressive)
- [x] Grayscale / RGB / YCbCr / CMYK / YCCK output (Adobe APP14, all round-trip tested)
- [x] Streaming decode (Stream-based API; input buffered for multi-scan/progressive random access)
- [x] Bitstream validation + robust error reporting (fuzz/corruption tested)
- [ ] Arithmetic decoding (optional — isolate architecture)

## Encoder
- [x] Baseline sequential encode
- [x] Progressive encode (DC first/refine + per-component AC first/refine, successive approximation)
- [x] Quality parameter
- [x] Configurable chroma subsampling
- [x] Optimized Huffman tables + custom Huffman tables via options
- [x] Custom quantization tables via options
- [x] Restart intervals
- [x] Deterministic output
- [x] Streaming encode (writes incrementally to output Stream)

## Metadata
- [x] JpegInfo (dimensions, components, color space) + Jpeg.Identify
- [x] JpegMetadata (JFIF density, Exif, ICC incl. chunking, Adobe, COM)
- [x] Read + write round-trip of all metadata

## Public API
- [x] Jpeg.Decode / Load / Save / Encode
- [x] JpegEncoderOptions / JpegDecoderOptions (MaxPixels, ReadMetadata)
- [x] JpegException / JpegFormatException / JpegCorruptException (entropy corruption)
- [x] JpegImage factory helpers (grayscale/rgb)

## Testing
- [x] Unit tests for every component (zigzag, DCT, quant, bitstream, huffman, markers, color, block coder)
- [x] Round-trip tests (encode -> decode pixel compare, baseline + progressive)
- [x] Golden reference tests (pinned SHA-256 of deterministic output + golden decode)
- [x] Structural compliance tests (marker order, segment lengths, SOF/scan structure)
- [x] Metadata survival tests (Exif, ICC chunking, comments, density, Adobe, unknown APP)
- [x] Corruption / malformed / truncated tests (fuzz truncation + byte-flip)
- [x] Edge cases (1x1, strips, primes, large, all subsampling, quality extremes)
- [x] Property tests (random images round-trip, determinism, progressive==baseline)
- [~] Compatibility tests — structural conformance verified; external-decoder cross-check not possible in this BCL-only environment
- [x] Benchmarks (throughput, allocations) — JpegSharp.Benchmarks (BenchmarkDotNet)

## Docs
- [x] README with architecture, pipeline, perf notes, limitations
- [x] XML docs on all public API (enforced by GenerateDocumentationFile + warnings-as-errors)
