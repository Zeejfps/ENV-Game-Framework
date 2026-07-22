# WebPSharp Implementation Checklist

Pure C# WebP codec. Tracks progress across the autonomous implementation loop.
Legend: `[ ]` todo · `[~]` in progress · `[x]` done (implemented + tested).

## Foundation
- [x] Project scaffolding (WebPSharp, WebPSharp.Tests) + solution wiring
- [x] Exception hierarchy (WebPException / WebPFormatException / WebPCorruptException)
- [x] Public image type (WebPImage, RGBA/RGB)
- [x] WebPInfo (dimensions, format, alpha, animation flags)
- [x] WebPMetadata (ICC, EXIF, XMP, unknown chunks)
- [ ] WebPDecoderOptions / WebPEncoderOptions
- [~] WebP static entry API (Identify done; Decode/Encode/Load/Save pending)

## Container (RIFF)
- [x] FourCC type
- [x] RIFF reader (chunk enumeration, validation, padding)
- [x] RIFF writer (chunk emission, padding, size back-patching)
- [x] WebP chunk id constants (VP8 / VP8L / VP8X / ALPH / ANIM / ANMF / ICCP / EXIF / XMP)
- [x] VP8X extended header parse + write (dimensions + feature flags)
- [x] Unknown chunk preservation (round-trips through decode/encode)
- [x] Chunk ordering/validation (encoder emits spec order; decoder rejects duplicate image/ICCP/EXIF/XMP/VP8X and ANMF-before-ANIM)

## Header identify (done)
- [x] VP8 lossy dimension parse
- [x] VP8L lossless dimension + alpha parse
- [x] VP8X canvas + feature-flag parse

## IO / Bit primitives
- [x] Little-endian byte reader/writer helpers (BinaryPrimitives + WebPHeaderReader helpers)
- [x] VP8 boolean (arithmetic) decoder (RFC 6386, EOF-tolerant)
- [x] VP8 boolean (arithmetic) encoder (RFC 6386, carry propagation; round-trips with decoder)
- [x] VP8L LSB-first bit reader (Vp8LBitReader, 64-bit accumulator)
- [x] VP8L LSB-first bit writer (Vp8LBitWriter)

## VP8L (lossless)
- [x] Header parse (signature, dimensions, alpha, version) — dimensions done in WebPHeaderReader
- [x] Prefix (Huffman) code reading (HuffmanTree + canonical PrefixCode)
- [x] Prefix (Huffman) code building/writing (PrefixCodeWriter)
- [x] Prefix-code-group description read/write (Vp8LHuffman: simple + normal/code-length-code)
- [x] Color cache (hash insert/lookup/index) — decode + encode end-to-end (cache-aware event stream)
- [x] LZ77 length/distance prefix-value coding (LzPrefix, encode+decode)
- [x] Huffman code-length builder from frequencies (HuffmanLengthBuilder, length-limited)
- [x] LZ77 back-reference decode (copy loop; distance plane codes > 120)
- [x] LZ77 back-reference encode (hash-chain matcher, plane code > 120, deterministic; public default)
- [~] Distance mapping (plane code > 120 done; near-distance table ≤ 120 pending golden validation)
- [x] Predictor transform (inverse + forward) — 14 predictors, exact boundary rules, entropy sub-image, end-to-end
- [x] Color transform (inverse + forward) — cross-color decorrelation, entropy sub-image, end-to-end
- [x] Subtract-green transform (inverse + forward) — end-to-end encoder+decoder + decoder transform pipeline
- [x] Color-indexing (palette) transform (inverse + forward) — palette + pixel bundling + width threading, end-to-end
- [x] Entropy image / meta-huffman handling (multiple Huffman groups, per-tile selection) — decode + encode
- [x] Full lossless decode (all transforms + color cache + LZ77 + meta-Huffman; near-distance ≤120 pending)
- [x] Full lossless encode (all transforms + LZ77 + meta-Huffman)
- [x] Effort-driven transform selection (EncodeBest tries candidates, keeps smallest; wired to WebPEncoderOptions.Effort)
- [x] Lossless round-trip tests (exact): single pixel, odd dims, noise, gradient, solid, transparent, RGB

## Public API
- [x] WebPEncoderOptions / WebPDecoderOptions
- [x] WebP.Encode / Decode / Save / Load / Identify (lossless path end-to-end)
- [x] MaxPixels guard, lossy-requested clear error

## VP8 (lossy)
- [x] Boolean (arithmetic) entropy decoder + encoder (RFC 6386, round-trips)
- [x] Inverse WHT + inverse DCT (spec-exact, analytic + consistency tests)
- [x] Forward DCT (+ WHT forward, self-consistent; WHT to be validated vs reference for encode)
- [x] VP8 constant tables (dequant, coeff/update probs, B-mode probs, bands, zigzag, cat) — fetched from libwebp/RFC 6386, count-verified, in Vp8Tables.cs
- [ ] Frame header parse (segmentation, filter, quant, partitions, prob updates)
- [ ] Segment features
- [~] Quantization / dequantization (tables done; per-segment matrix computation logic captured)
- [ ] Boolean coefficient decode (GetCoeffs token loop + GetLargeValue) — logic captured, to implement
- [x] Intra prediction: 16x16 luma + 8x8 chroma (DC/V/H/TM) + 4x4 B_PRED (all 10 modes)
- [x] Loop (deblocking) filter: simple + subblock + macroblock (RFC 6386, per-line, tested)
- [~] YUV->RGB conversion (per-sample, spec-exact BT.601; plane conversion + chroma upsampling pending)
- [ ] Full lossy decode
- [ ] Full lossy encode (quality/effort)
- [ ] Lossy round-trip tests (thresholded)

## Alpha
- [ ] ALPH chunk parse
- [ ] Alpha decode (none / lossless-VP8L compression)
- [ ] Alpha encode
- [ ] Alpha filtering methods

## Animation
- [x] ANIM parse/write (bg color, loop count)
- [x] ANMF parse/write (frame rect, duration, blend, dispose)
- [x] Frame composition onto canvas (RenderFrames)
- [x] Blending (Over / Source)
- [x] Disposal (None / Background)
- [x] WebPAnimation / WebPFrame API
- [x] Timing (per-frame duration), loop count, background color, canvas management

## Metadata
- [x] ICCP extract/write (round-trips)
- [x] EXIF extract/write (round-trips)
- [x] XMP extract/write (round-trips)
- [x] ReadMetadata decoder option

## Cross-cutting
- [x] Streaming decode/encode APIs (sync Stream overloads + async DecodeAsync/EncodeAsync/LoadAsync/SaveAsync/IdentifyAsync with CancellationToken)
- [x] Corruption detection coverage (fuzz harness: ~10k truncation/bit-flip/garbage mutations fail cleanly, VP8L + container + animation)
- [x] Property/round-trip test harness (randomized round-trips + combinatorial sweep: 37 settings × 5 image types, all VP8L features/combos)
- [x] Integration tests on structured content (photographic/graphic/texture): exact round-trip + real compression (graphic < 10% raw)
- [ ] Golden tests vs reference files (needs external fixtures / VP8 tables)
- [x] Benchmarks (WebPSharp.Benchmarks: BenchmarkDotNet encode/decode/alloc + --smoke correctness check)
- [x] Architecture + API documentation (README: API, RIFF, VP8L/animation pipelines, perf, limitations, extension points; XML docs on all public members)

## Known blocker (VP8 lossy full decode)
- Algorithmic primitives complete + unit-tested: boolean coder, DCT/WHT, intra 16/8/4, loop filter, YUV->RGB.
- Remaining VP8 decode needs large spec constant tables (dequant 256, coeff/mode probabilities 1000+, token trees) that require a reference source or golden file to transcribe correctly; guessing would silently corrupt output.
