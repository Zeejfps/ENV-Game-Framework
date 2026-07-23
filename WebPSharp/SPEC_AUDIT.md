# WebP Specification Compliance Audit — Persistent State

**Coordinator loop.** This file is the single source of truth for audit progress. Each loop
iteration reads it, does one unit of work (fold in subagent findings OR advance one section
through compliance → adversarial → test review), and updates it. Keep coordinator context small;
delegate detailed RFC/code review to Opus subagents.

**Spec sources** (subagents WebFetch these; do not load into coordinator context):
- RIFF container: https://developers.google.com/speed/webp/docs/riff_container
- VP8 lossy bitstream: RFC 6386 (https://datatracker.ietf.org/doc/html/rfc6386)
- VP8L lossless: https://developers.google.com/speed/webp/docs/webp_lossless_bitstream_specification
- Reference impl for constants/algorithms: libwebp `main` (raw.githubusercontent.com/webmproject/libwebp/main/src/...)

**Rule:** a section is PASS only if (compliance = PASS) AND (adversarial finds no violation) AND
(tests exist and pass). Otherwise PARTIAL/FAIL → spawn fix-investigation agent, record fixes +
regression tests.

**Status legend:** ⬜ not started · 🔵 compliance review in flight · 🟡 adversarial in flight ·
🟢 test review in flight · ✅ PASS · 🟠 PARTIAL · 🔴 FAIL · ❓ UNKNOWN

---

## Compliance Matrix

| # | Section | Status | Compliance | Adversarial | Tests | Code Areas | Notes |
|---|---------|--------|-----------|-------------|-------|-----------|-------|
| 1 | RIFF container structure (`RIFF`/`WEBP` fourcc, file size, chunk framing, padding) | 🟠 | PASS | PASS (could not break) | INSUFFICIENT (6 must-add) | `Container/RiffReader,RiffWriter,FourCc,RiffChunk` | code solid; PARTIAL only for test gaps → PASS once T1/T3/T4/T5/T6/T7 added |
| 2 | Simple file format (lossy `VP8 `, lossless `VP8L`) header selection | ✅* | PASS | n/a (compliance clean) | PARTIAL (neg-path tests) | `Container/WebPHeaderReader`, `Api/WebP` | dispatch + `VP8 ` byte-exact + unknown-reject + inline-alpha reporting correct; ✅* pending decode-reject/VP8+NUL/inline-alpha-decode tests |
| 3 | Extended format `VP8X` (flags, canvas w/h, reserved bits) | 🟠 | PARTIAL (2 major) | n/a (compliance deep) | gap | `Container/WebPHeaderReader`, `Api/WebP,WebPInfo` | layout correct + canvas-consistency enforced; flag↔chunk agreement NOT validated → silent-accept + mis-report |
| 4 | Chunk parsing rules (order, unknown chunks, odd-size padding, bounds) | 🟠 | PASS (synthesized) | via §1/§3/§18 | gap | `Container/RiffReader`, `Api/WebP`, `WebPChunkValidation` | covered by §1(framing/pad/bounds)+§3(order/flags)+§18(unknown); residual gaps = those sections' |
| 5 | VP8 lossy frame header (key frame, dims/scale, color/clamp, partitions) | 🟠 | PARTIAL (1 med interop) | n/a (compliance = field-by-field libwebp) | gap | `Vp8/Vp8Decoder`, `Vp8Encoder` | fields bit-correct profile-0; profile 1-3 filter override not applied; scale bits dropped |
| 6 | VP8 segmentation / quantization / filter header | ✅-parse* | PASS (parse verified) | n/a | untested | `Vp8/Vp8Decoder` | seg/quant/filter/prob-update parse all bit-correct incl 1056 CoeffUpdateProbs; ✅ pending seg/multi-part/prob-update tests |
| 7 | VP8 boolean entropy coder (RFC 6386 §7) | 🟠 | PARTIAL | n/a (compliance was deep-adversarial) | gap | `Vp8/Vp8BooleanDecoder,Vp8BooleanEncoder` | code verbatim-correct & exact inverse; PARTIAL = missing interop KAT test + signed-helper naming |
| 8 | VP8 macroblock prediction modes (16x16, 8x8 chroma, 4x4 B_PRED) | ✅* | PASS (bit-exact libwebp) | n/a (compliance = per-mode libwebp verify) | PARTIAL (isolation) | `Vp8/Vp8Prediction,Vp8Prediction4` | all modes + 127/129 fill + top-right repl + enum aliasing verified; ✅* pending non-uniform VR4/VL4/HD4/HU4 vectors + top-right-edge + BModeToPred tests |
| 9 | VP8 coefficient/residual token decode + DCT/WHT/dequant | ✅* | PASS (bit-exact libwebp) | n/a (compliance = constant-level libwebp verify) | PARTIAL (isolation gaps) | `Vp8/Vp8Coefficients,Vp8Transform,Vp8Tables` | all tables/constants/rounding verified; ✅* pending KAT transform + cat6 + dequant-factor unit tests |
| 10 | VP8 in-loop deblocking filter | ✅* | PASS (bit-exact libwebp) | n/a (compliance was libwebp line-by-line) | PARTIAL (see gaps) | `Vp8/Vp8FilterApply,Vp8Decoder.ApplyLoopFilter` | correctness fully verified incl. f_inner rule; ✅* pending test-hardening of simple/sharpness/segment/lf-delta branches |
| 11 | VP8 reconstruction + chroma upsampling | ✅* | PASS (bit-exact libwebp) | n/a (compliance = constant-level verify) | PARTIAL (isolation) | `Vp8/Vp8Decoder,Vp8Yuv` | YUV consts + recon-clip + fancy/nearest upsample all bit-exact; ≤1 tol NOT from this path; ✅* pending non-tautological YUV/fancy-weight vectors |
| 12 | VP8L header + image-stream structure (signature, dims, alpha bit, version) | 🟠 | PASS* (1 major enc gap) | PARTIAL (no NEW issue; primitives solid) | gap | `Vp8L/Vp8LDecoder,Vp8LEncoder,Vp8LBitReader/Writer` | decoder/bit-IO confirmed correct; PARTIAL solely on enc dim>16384 guard → fix documented |
| 13 | VP8L transforms (predictor, cross-color, subtract-green, color-indexing) | 🔴 | FAIL (predictor EMISSION) | contradicted by §21 empirical | gap | `Vp8L/Transforms/*`, `Vp8LEncoder` | inverse/forward MATH bit-exact, but ENCODED predictor VP8L stream REJECTED by dwebp (§21 SEVERE) — the interop gap §13 flagged was a real bug |
| 14 | VP8L color cache | ✅* | PASS (impl, hash byte-exact) | n/a (compliance = libwebp verify) | PARTIAL (tests) | `Vp8L/ColorCache` | 1..11 reject + 0x1E35A7BD hash + insert-on-every-pixel + s-280 offset verified; ✅* pending hash-golden + decoder-range-reject + interop tests |
| 15 | VP8L Huffman coding (meta-huffman, code lengths, prefix codes) | ✅* | PASS (impl spec-correct) | n/a (compliance = per-req libwebp verify) | PARTIAL (verification) | `Vp8L/Huffman*,PrefixCode*` | all reqs correct incl over/under-subscription reject; ✅* pending repeat-code/max_symbol decode tests + cwebp-lossless interop |
| 16 | VP8L LZ77 backward references + distance mapping | ✅* | PASS (functional, table byte-exact) | n/a (compliance = libwebp diff) | PARTIAL (tests) | `Vp8L/Vp8LLz77,LzPrefix,Vp8LDistance` | prefix formula + 120-entry kCodeToPlane verified; memory-safe bounds; ✅* pending near-dist decode + OOB-reject tests; [LOW] delete stale comment `Vp8LDecoder.cs:10-12` |
| 17 | ALPH alpha chunk (method, filter, pre-processing, compression) | 🟠 | PARTIAL (1 major interop) | n/a (compliance deep) | gap | `Vp8/Vp8AlphaDecoder,Vp8AlphaEncoder` | **decoder rejects valid P=1 cwebp files** — 1-line fix; else all filters byte-exact |
| 18 | Metadata chunks (ICCP, EXIF, XMP) ordering/flags | 🟠 | PASS core / PARTIAL | n/a (compliance deep) | gap | `Api/WebPMetadata`, `Api/WebP`, `Container/*` | ICCP/EXIF/XMP mechanics correct incl `XMP ` space + ordering; [minor] unknown-chunk 4CC ToString-corruption |
| 19 | Animation (ANIM/ANMF, blend/dispose/timing/loop, canvas composition) | 🟠 | PARTIAL (1 HIGH + 3 MED) | n/a (compliance deep) | gap | `Api/WebPAnimation`, `Api/WebP` | layout+blend+dispose correct; frame-rect bounds NOT rejected (HIGH); bg-color diverges from libwebp; ALPH-in-ANMF dropped |
| 20 | Decoder error handling (malformed/truncated/OOB rejection) | 🟠 | PARTIAL (1 blocker) | n/a (compliance = whole-codebase sweep) | gap | `Api/Exceptions/*`, all decoders | core bounds-safe + no unbounded alloc; ANMF overflow = ONLY raw-exception escape (blocker); 1 taxonomy nit; fuzz only covers lossless |
| 21 | Encoder output conformance (chunks emitted, third-party decodable) | 🔴 | FAIL (1 SEVERE) | empirical dwebp/webpinfo | gap | all encoders | **default lossless (Predictor) output REJECTED by dwebp** — self-round-trip hid it; lossy/ALPH/anim/metadata all PASS interop |

---

## Known Issues / Deviations (from HANDOFF.md, to be verified during audit)

- VP8 lossy **encoder is correct but not size-optimal** (no i4x4/RD/trellis/skip signaling). Quality
  only — decoder reconstructs whatever is emitted. → check whether it produces *spec-valid* streams.
- `EncodeAnimation` emits **only VP8L frames** (no lossy animated frames). Feature gap, not a bug.
- Only VP8 **profile 0** reconstruction filter exercised by fixtures (profiles 1–3 unvalidated).
- Fancy upsampling matches dwebp **within ≤1** (RGB rounding). Verify §11 tolerance claim.

## In-Flight Subagent Work

- Section 1 (RIFF container): compliance review agent dispatched.
- Section 7 (VP8 boolean entropy coder): compliance review agent dispatched.
- Section 12 (VP8L header/image-stream): compliance review agent dispatched.
- Section 10 (VP8 in-loop deblocking filter): compliance review agent dispatched.

## Findings Log

_(chronological; compact one-liners with severity + regression-test status)_

**§1 RIFF container (compliance PASS, minor):**
- [minor] `RiffReader.cs:106-107` — "missing pad byte" guard is dead code (condition unsatisfiable); reader silently tolerates missing final pad (spec-lenient, OK) but the check does nothing → delete or rewrite. _fix pending, no regression test yet_
- ~~[minor] `RiffWriter.cs:82` writer size cap~~ — **RETRACTED by adversarial review:** `uint.MaxValue` is correct for a uint32 field; the 2^32−10 note conflated whole-file size with the field. No fix needed.
- **[low, NEW — belongs to §19/§20] `WebP.cs:421-434` `ParseFrameChunk` ANMF inner sub-chunk parser uses SIGNED `start + size`** (unlike RiffReader's unsigned checks). **CORRECTED SCOPE (test-eng verified):** only `size == 0x7FFFFFFF` leaks — it stays positive, `start+size` overflows int negative, bypasses the `>innerSpan.Length` check → `inner.Slice(8, 0x7FFFFFFF)` throws `ArgumentOutOfRangeException` (project compiles unchecked). `0x80000000`/`0xFFFFFFFF` become negative int → already caught by `size < 0` → `WebPFormatException`. FIX: compare `size > (uint)(innerSpan.Length - start)` unsigned before the add. Regression Theory T7 (`0x7FFFFFFF` row is RED until fixed; other two green). _fix pending_
- Adversarial verdict: **could not break RiffReader** — all V1-V8 vectors defended (unsigned bounds, no OOB/wrap/infinite-loop/valid-file-rejection). §1 RIFF-container structure = PASS. Dead-code guard `RiffReader.cs:106-107` confirmed harmless. +7 adversarial tests suggested (ANMF overflow, mid-file missing-pad graceful reject, fuzz-only-WebPFormatException).
- [minor] Pad byte value not verified `==0` on read (`RiffReader.cs:100-108`) — spec-permitted (readers must skip); only relevant if strict mode wanted.
- [minor] `FourCc.Read` (`FourCc.cs:55-56`) doesn't ASCII-validate on-wire IDs — spec-permitted (unknown chunks ignored); structural parse unaffected.
- 17 suggested tests captured (min-size/signature/oversize-chunk rejection, zero-size chunk, odd-pad skip, trailing-tolerance, writer size-field/backpatch, round-trips). → see Test Coverage Deltas.
- Verdict source: files `Container/{RiffReader,RiffWriter,FourCc,RiffChunk,WebPChunkIds,WebPHeaderReader}.cs`. Awaiting adversarial confirmation before ✅.

**§12 VP8L header/bit-IO (compliance PASS*, 1 major):**
- [major] `Vp8LEncoder.cs:132-133` — no dim upper-bound guard; width/height >16384 masks to 14 bits (`&0x3FFF`) → malformed stream, no error. `WebPImage.cs:24-26` only rejects ≤0. FIX: guard `1..16384` in encoder (or WebPImage ctor) + regression test `Encode_DimensionAbove16384_Throws`. _fix pending_
- [minor] Public `Vp8LDecoder.Decode` lacks post-header `IsEndOfStream` check (masked in real pipeline by `WebPHeaderReader.ReadVp8LDimensions` <5-byte check, but direct calls proceed on garbage). _fix pending_
- [minor] Two header parsers (byte-wise `WebPHeaderReader.ReadVp8LDimensions` + bit-wise `Vp8LDecoder.Decode`) must stay in lockstep; no shared source of truth.
- [minor/interop] Encoder sets `alpha_is_used` from pixel format not content (1 for opaque RGBA); spec-compliant (hint only) but differs from libwebp.
- Layout (sig 0x2F, 14-bit dim-minus-one, alpha bit, 3-bit version==0 reject) + LSB-first reader/writer verified correct vs libwebp `ReadImageInfo`. Tests: `Vp8LBitStreamTests` proves only bit-IO (req 5,6); header reqs 1-4,7 need: max-dim(16384) round-trip, signature-mismatch reject, alpha-bit round-trip, dim!=canvas reject. Adversarial in flight.

**§7 VP8 boolean coder (compliance PARTIAL — code correct, coverage gap):**
- [major-TEST] No on-the-wire interop/known-answer test vs dwebp/libwebp. Existing `Vp8BooleanCoderTests` only proves enc==inverse(dec); a shared-bug pair would pass. REQUIRED: KAT test decoding a real cwebp bool partition AND/OR byte-match encoder output vs libwebp `VP8BitWriter`. _test pending_
- [minor] `GetSigned/PutSigned` (`Vp8BooleanDecoder.cs:112-116`, `Vp8BooleanEncoder.cs:78-83`) are sign-magnitude (n magnitude bits + sign) — CORRECT for real VP8 header fields (matches libwebp `VP8GetSignedValue`), but misnamed vs RFC two's-complement `read_signed_literal`. Rename or add distinct RFC helper. _clarity, no behavior change_
- [minor] 2-byte constructor prefetch latches `_eos` for 0/1-byte partitions before any decode (`IsEndOfStream` spuriously true for tiny partitions). Read-past-end zero-padding is correct but untested.
- VERIFIED CORRECT: SPLIT `1+(((range-1)*prob)>>8)`, renorm loop, init range=255, encoder carry/flush verbatim WebM ref, prob=0/255 bounds. → route to test-engineering, not re-review.

**§17 ALPH alpha (compliance PARTIAL — 1 major DECODER-rejects-valid interop bug):**
- **[MAJOR — interop, decoder rejects valid file] `Vp8AlphaDecoder.cs:39-40`** throws on ALL `preprocessing != 0`. P is informative; libwebp accepts P∈{0,1} (rejects only `>1`) and cwebp emits P=1 at common quality → those valid alpha files are refused. FIX (1-line): `if (preprocessing > 1) throw new WebPFormatException(...)` else ignore P (no dither needed for correctness). Regression test `Decode_AcceptsPreprocessingP1`. _fix documented, ready to apply_
- [minor] Raw C=0 takes `data[..total]` ignoring trailing bytes (libwebp equally lenient — OK).
- [minor] ALPH-after-VP8 ordering silently drops alpha (`WebP.cs:552-561`) — spec mandates ALPH-before-image; tolerable but hides malformed input.
- VERIFIED CORRECT: header bitfield C/F/P/Rsv order + reserved!=0 reject, C=2/3 reject, all 4 filters byte-exact both directions incl. first-row/col edges + gradient clamp, green=alpha header-less lossless, opaque-omit. Tests thin: need per-filter round-trip, C=2/3 + reserved reject, raw-truncation, P=1 accept, 1×N/N×1, dwebp-reads-our-alpha interop.

**§10 VP8 loop filter (compliance PASS correctness / PARTIAL tests — NO production fix):**
- Decode-path filter (`Vp8FilterApply` + `Vp8Decoder.ApplyLoopFilter`/`PrecomputeFilterStrengths`) is bit-exact libwebp: f_inner all-zero-MB rule correct (`Vp8Decoder.cs:736-737`, hasCoeffs from MbNonZeroY|Uv), DoFilter2/4/6/Hev/NeedsFilter2 verbatim, strength/clamp/sharpness/mbedge-vs-inner limits verbatim, raster-order-after-reconstruction equivalent to libwebp FilterRow.
- [major-TEST] `Vp8LoopFilterTests` (9 tests) exercise `Vp8LoopFilter` — the RFC variant NOT on the decode path (only referenced by that test file); the shipped kernels have ZERO direct unit tests. (RFC variant is algebraically equivalent, just dead.)
- [major-TEST] f_inner all-zero-non-skip rule guarded by ONE ≤1-tolerance golden (`Vp8EncodeTests.Decode_FilteredEncoderOutput_MatchesDwebp`); cwebp fixtures can't see it (they mark all-zero MBs skip).
- [minor-TEST] simple filter (FilterType==1), sharpness>0, per-segment strength/AbsoluteDelta, use_lf_delta ref/mode deltas — all correct by inspection but no fixture. Need `cwebp -f 0`/segmented/sharpness fixtures. → all test-hardening, no code change.

**§12 VP8L header — adversarial confirmation:** no NEW decoder-accepts-invalid / rejects-valid defect. Accumulator is `ulong` (shift ≤56, no UB); perfect 5-byte header does not false-EOS; 0x2F cannot mis-route (dispatch on FourCC); canvas↔header off-by-one correct both sides; versions 1-7 all rejected. Extra dead-code note: `Vp8LBitReader.BytesConsumed` (line 42) is unused & would mis-report post-EOS. Section PARTIAL solely on the enc dim-guard major (confirmed bytes `16385×1`→`2F 00 00 00 10`→1×1).

**§19 Animation (PARTIAL — 1 HIGH + 3 MED; layout/blend/dispose correct):**
- [HIGH, deviation #5] No frame-rect-vs-canvas bounds rejection (`WebP.cs:404-440`, `:296-301`). Frame with `x+w>canvasW`/`y+h>canvasH` silently accepted (Blit clips); libwebp `CheckFrameBounds` rejects. FIX: validate in `DecodeAnimation` where canvas dims known → `WebPFormatException`. Test `Decode_FrameRectExceedsCanvas_Throws` (RED). _fix documented_
- [MED, deviation #4 confirmed] ANMF inner `0x7FFFFFFF` overflow (`WebP.cs:424-434`) → leaks ArgOOR. Same unsigned-compare fix. (shared with §1)
- [MED, deviation #6] Background-color rendering diverges from libwebp: WebPSharp fills canvas+dispose rects with ANIM bg color (RIFF spec text, `WebPAnimation.cs:48,56`); libwebp IGNORES bg, zero-fills transparent → composite won't match libwebp/browsers. DECISION NEEDED: libwebp-parity (fill 0x00000000) vs RIFF-literal. _needs product decision_
- [MED] ALPH-in-ANMF ignored on decode (`ParseFrameChunk` handles VP8L/VP8 but not ALPH sub-chunk) → lossy animated frame alpha silently dropped; lossy animated encode also unsupported (`EncodeAnimation` throws for !Lossless). Real files from img2webp/gif2webp decode wrong. FIX: add ALPH handling in ANMF inner loop + route BuildFrameChunk through VP8+ALPH lossy encoder.
- [LOW] reserved 6 flag bits not validated. [INFO] blend not bit-exact vs libwebp (`/255` truncate vs libwebp's rounded scale — possible ±1).
- VERIFIED CORRECT: ANIM 6-byte (BGRA bg + uint16 loop, loop0=infinite), ANMF 16-byte (x/2,y/2 ↔ *2; minus-one dims; duration; B/D bits: dispose=bit0, blend bit1 NO_BLEND); placement/duplicate/pre-ANMF-ordering rejection; even-offset invariant (WebPFrame rejects odd X/Y); source-over blend structure; dispose after-show-before-next. All tests self-round-trip — NO anim_dump interop; blend-math + bounds + overflow + BGRA-byte-order + lossy-frame all untested.

**§21 Encoder conformance (FAIL — 1 SEVERE default-path interop failure; empirically tested vs libwebp 1.6.0):**
- **[SEVERE, deviation #1] Default `WebP.Encode()` lossless output is REJECTED by dwebp (`Status: 3 BITSTREAM_ERROR`).** At default `Effort=4`, `Vp8LEncoder.EncodeBest` selects the **Predictor transform** (`Vp8LEncoder.cs:80-81,89-90` candidates; emission `:137-152,286-292`) for spatially-correlated images; dwebp rejects EVERY such file while WebPSharp self-decodes bit-exact (maxDiff=0) → all self-round-trip tests pass and HIDE it. Effort 0-3 (LZ77/SubtractGreen) + Palette = valid. `WebPIntegrationTests.Photographic` (128×128 plasma) triggers it but self-decodes so can't catch it. `webpinfo` says "no error" because it only parses the header, NOT the bitstream — dwebp is the real gate. ROOT: predictor VP8L emission bits diverge from libwebp (needs byte-level diff vs `cwebp -lossless`; §13 read-only couldn't pinpoint). MITIGATION: drop Predictor (+CrossColor, shares the branch, currently unreachable) from Candidates until fixed → falls back to valid LZ77/SubtractGreen/Palette (larger but correct). _fix-investigation REQUIRED (byte-level diff)._
- [MED, deviation #2 confirmed empirically] VP8L dim>16384: `16385×1` → webpinfo reports `1×1`, silent data loss. Guard both VP8L + VP8 14-bit dim fields.
- **[SEVERE-infra] NO encoder test shells out to dwebp/webpinfo** — grep of test project confirms all encoder "interop" is `Decode(Encode(x))` self-round-trip; the dwebp `.rgba` assets validate only the DECODER. This is why the predictor bug shipped. Add `[Fact]`-skippable dwebp/webpinfo integration tests.
- [PASS empirical] Lossy VP8 (21 sizes incl 1×1/65×33/1×N decode via dwebp), ALPH alpha exact (maxAlpha=0), animation frames valid (webpmux, correct offsets/dispose/blend/bg/loop), metadata chunk order correct. "No skip/no i4x4/single-partition/profile-0" gaps do NOT break lossy interop. VP8L opaque-RGBA alpha_is_used=1 quirk accepted by dwebp.

**§20 Decoder error-handling (PARTIAL — 1 blocker, else robust):**
- [BLOCKER — consolidates deviation #4] ANMF `0x7FFFFFFF` overflow (`WebP.cs:424-434`) is the **ONLY** place a raw non-library exception (`ArgumentOutOfRangeException`) escapes the public decode API (agent grepped ALL hand-rolled size parsers — no other instance). Same unsigned-compare fix. Highest-priority fix in the whole audit.
- [MINOR] `Vp8Decoder.cs:81` non-key-frame → base `WebPException` is the ONLY mislabeled structural error (should be `WebPFormatException`); the other 4 base-WebPException throws are legit unsupported-feature. FIX one-liner.
- [MINOR] `(int)ms.Length` truncation on >2GB streams (`WebP.cs:46,83`) → theoretical raw ArgOOR (hard to hit; MemoryStream caps near int.MaxValue).
- VERIFIED SAFE: RIFF parser (unsigned bounds), both bit readers (EOS→0+latch), boolean decoder (NextByte→0 past end), VP8L main loop (pos strictly advances, all indices range-checked→WebPCorrupt), HuffmanTree/PrefixCode/ColorCache/LzPrefix/Distance/ColorIndexing/Predictor (every bitstream index checked or in fixed table). **NO unbounded allocation from unchecked file size** — all gated by MaxPixels(500M); meta-Huffman amplification bounded + terminates via all-zero-code rejection.
- [TEST GAP] Fuzz harness (`WebPRobustnessTests`) enforces correct contract (only WebPException escapes = Assert.Fail otherwise) but fuzzes ONLY lossless; lossy/ALPH/VP8X/animation paths never fuzzed → the ANMF leak survived. Add lossy+ALPH truncation fuzz, animation multi-byte fuzz, VP8X+metadata truncation fuzz.

**§4 Chunk parsing rules (SYNTHESIZED from §1/§3/§16/§18 — no separate agent):**
- Odd-size padding + framing + chunk-overrun bounds + zero-size chunk: **PASS** (§1, RiffReader adversarially unbreakable).
- Chunk ORDER (VP8X first, ICCP before image, EXIF/XMP after) + duplicate rejection: correct on write; reader is order-AGNOSTIC (accepts canonical order, does not ENFORCE position) — spec-lenient, OK; but flag↔chunk agreement NOT validated (§3 major).
- Unknown-chunk preservation: works for ASCII 4CCs; [minor] non-printable-4CC ToString corruption + position-not-preserved (§18).
- Backward-ref/payload bounds: memory-safe (§16). Signed-overflow in ANMF inner parser (§1/§19, `0x7FFFFFFF`).
- Net: no NEW findings beyond those already logged in §1/§3/§16/§18. Residual = fix VP8X flag agreement (§3) + unknown-4CC bytes (§18) + ANMF overflow (§19).

**§5/§6 VP8 frame/segment/quant/filter header (PARTIAL — 1 med interop + minors; parse otherwise bit-correct):**
- [MED-interop, NEW deviation #4] Profile/version 1-3 accepted (`Vp8Decoder.cs:78-79` rejects only `>3`) but version's loop-filter override NOT applied. RFC §9.1: v1→simple filter, v2/v3→no filter. Valid profile-1/2/3 stream deblocked incorrectly. FIX: after ParseFilterHeader clamp `if(Profile>=2)FilterType=0; else if(Profile==1&&FilterType==2)FilterType=1;` OR reject `Profile!=0` explicitly + document. Test `Profile1ForcesSimpleFilter`/`Profile2DisablesFilter` (RED today). _fix documented_
- [LOW] x/y scale bits (`data[7]>>6`,`data[9]>>6`) not parsed/exposed (`Vp8Decoder.cs:88-89` masks off) — harmless for WebP (always 0) but incomplete §9.2.
- [LOW] non-key-frame rejection throws base `WebPException` not `WebPFormatException` (`Vp8Decoder.cs:81`) — type inconsistency.
- [LOW] `show==0` rejected (`:82-83`) — stricter than libwebp (fine for WebP).
- [LOW] encoder no max-dim guard (`Vp8Encoder.cs:660-663`) — same class as §12; oversized truncates to wrong 14-bit.
- VERIFIED CORRECT (field-by-field vs libwebp): frame tag key/profile/show/19-bit partlen, start code 9D 01 2A, 14-bit dims, color/clamp, segmentation (7-bit quant + 6-bit filter signed + 3 tree probs, abs/delta), filter header (simple/level6/sharp3/lf_delta 4ref+4mode), partition count `1<<L(2)` + 3-byte size table + bounds, quant base L(7)+5 signed deltas, refresh_entropy + 1056-entry CoeffUpdateProbs loop, skip prob L(8). Enc mirrors. **Multi-partition & segmentation parse correct but UNTESTED** (no fixture exercises 2/4/8-part, seg, profile≠0, prob-update, or any rejection path).

**§8 VP8 prediction (PASS — bit-exact, NO fix):** all whole-block DC/V/H/TM + 4 DC missing-edge variants + all 10 B_PRED (Avg2/Avg3 + every sample order verified); 127/129 fill row-first corner precedence; top-right replication (incl. right-edge last-col); enum aliasing DC=0/TM=1/V=2/H=3 + BModeToPred remap. Test gaps: VR4/VL4/HD4/HU4 only uniform-input tested; top-right-edge + BModeToPred golden-only.

**§9 VP8 coeff/DCT/dequant (compliance PASS — bit-exact libwebp, NO production fix):**
- VERIFIED CORRECT (constant-level vs libwebp `tree_dec/quant_dec/dsp/dec.c`): token tree + ctx transitions + `GetLargeValue` cat3-6 bases (11/19/35/67) & extra-bit probs; `Bands`(17)/`Zigzag`(16)/`Cat3-6` 0-terminated tables; `DcTable`/`AcTable` 128 entries incl. interior irregularities & endpoints; dequant Y1/Y2/UV factors (Y2Dc*2, Y2Ac=max(8,(Ac*101581)>>16), **UV-DC clamp 117**, others clamp 127) + per-segment/AbsoluteDelta; IDCT `Mul1=a+((a*20091)>>16)`/`Mul2=(a*35468)>>16`, col-then-row, `+4>>3`, DC path; IWHT `+3>>3` + strided scatter; Y2 dispatch (`nz>1`→WHT else dc-fill), nz bookkeeping.
- TEST GAPS (all LOW, isolation only — golden fixtures cover path end-to-end): (9) no known-answer IDCT/IWHT vectors pinning MUL constants; (10) no direct `GetCoeffs`/cat6/negative/zero-run unit test; (11) no dequant Y2/UV-factor unit test. → hardening tests specced.

**§3 VP8X (compliance PARTIAL — 2 major flag-consistency gaps):**
- [major] `WebP.DecodeExtended` (`WebP.cs:534-596`) reads ONLY canvas dims from VP8X; the flag byte is never checked against chunks. Alpha-flag-set-but-no-ALPH → decodes opaque silently; alpha-flag-clear-but-ALPH-present → alpha applied anyway; ICC/EXIF/XMP/Anim flag↔chunk disagreements undetected. FIX: after chunk-walk, read `vp8xPayload.Span[0]` and assert flag⇔chunk agreement, throw `WebPFormatException` on mismatch. _fix documented_
- [major] `WebPHeaderReader.ReadInfo` (`:41-72`) sets HasAlpha/HasIcc/HasExif/HasXmp straight from the flag byte, never confirming chunk presence → `WebPInfo` mis-reports on inconsistent files. FIX: walk extended chunks (not only when animated) and set flags from actual presence. _fix documented_
- [minor] VP8X size only floor-checked `>=10` (`WebPHeaderReader.cs:44,130`; `WebP.cs:584`), spec mandates exactly 10 → change to `!=10` reject. [minor] canvas product ≤2^32-1 not enforced.
- VERIFIED CORRECT: flag bit positions (ICC=0x20,Alpha=0x10,EXIF=0x08,XMP=0x04,Anim=0x02), 24-bit LE minus-one canvas (no off-by-one), canvas-vs-image consistency IS enforced+tested (`WebP.cs:583-591`), simple files emit no VP8X, chunk emission order, duplicate/ANMF-before-ANIM rejection, reserved-bit leniency (correct per spec "readers MUST ignore" — do NOT reject).
- Tests needed: each flag↔chunk disagreement (alpha/ICC/EXIF/XMP/anim both directions), Identify-mis-report, VP8X-size≠10, reserved-bits-ignored regression-guard, canvas-product-overflow, lossless-alpha-no-metadata-emits-bare-VP8L.

## Test Coverage Deltas

### ⚠️ CROSS-CUTTING FINDING — no VP8L (lossless) interop asset
**All committed `.webp` test assets are lossy VP8** (header `56 50 38 20`); there is **zero** `cwebp -lossless` VP8L file. Consequence: §12 (header), §13 (transforms), §14 (color cache), §15 (Huffman), §16 (LZ77) are each proven only by WebPSharp-encode↔WebPSharp-decode **self-round-trip**, which cannot catch a shared enc+dec formula error, and the encoder emits only a *subset* of the format (literal Huffman/no-repeat/use_length=0, literal predictor writes) so decoder paths for repeat-codes, max_symbol limiting, and libwebp-produced predictor/transform streams are **never exercised**. **HIGHEST-VALUE REMEDIATION:** add `cwebp -lossless` golden fixtures (predictor-heavy, palette, color-cache, chained-transform, plus a generic photo) + their `dwebp` `.rgba` refs, and a `Decode_CwebpLossless_*` test per transform/feature. One asset set closes the interop gap for 5 sections at once.

### §13 VP8L transforms (impl PASS / proof PARTIAL)
- VERIFIED bit-exact: all 14 predictors + Average/Select/ClampedAddSubtract(Full/Half)/Clip255; predictor edges (px0=0xff000000, row0=left, col0=top, last-col top-right wrap); cross-color `(sbyte*sbyte)>>5`, inverse uses NEW red / forward uses ORIG red; subtract-green R&B mod256; color-indexing bits(≤2→3/≤4→2/≤16→1)+cumulative palette+LSB bundling; glue: duplicate-type reject, `size_bits=ReadBits(3)+2`, reverse-order apply.
- [LOW-fix] enc: validate `PredictorBits/CrossColorBits∈[2,9]` (`Vp8LEncoder.cs:143`) & `PredictorMode∈[0,13]` (`:290` masks 0x0F → modes 14/15 alias to 0) — throw instead of emitting corrupt field.
- Tests needed: `Predictor_MatchesLibwebpVectors` (modes 5,6,8-13 absolute), `CrossColor_InverseMatchesReference`, `Bundle_PacksSpecBitOrder`, `Decode_DuplicateTransform_Throws`, `ColorIndexing_IndexBeyondPalette_MapsToZero`, `Predictor_SizeBits_Extremes`, + cwebp-lossless goldens.

### §15 VP8L Huffman (impl PASS / verification PARTIAL)
- VERIFIED correct: simple(1/2-sym) & normal code; `kCodeLengthCodeOrder`={17,18,0,1..16} exact; repeat 16(prev,3-6)/17(0,3-10)/18(0,11-138); `prev_code_len`=8 default, code-16 doesn't update prev; max_symbol `2+2*ReadBits(3)`/`2+ReadBits(nbits)`; alphabets green=256+24+cache, RGB A=256, dist=40; canonical MSB-first; **over- AND under-subscription rejected** (`PrefixCode.cs:55-66`) except single-symbol 0-bit; meta-huffman group count.
- [MED-test] repeat codes 16/17/18 decode NEVER exercised (encoder writes literal lengths only, `Vp8LHuffman.cs:130-151`); max_symbol limiting decode never exercised; malformed-set rejection proven only at `HuffmanTree.FromCodeLengths` API, not through `ReadPrefixCode` bitstream.
- Tests needed: `Decode_RepeatCode16/17/18`, `Decode_MaxSymbolLimiting`, `ReadPrefixCode_OverSubscribed/Incomplete_Throws`, + the cwebp-lossless interop decode (exercises repeat/max_symbol the encoder never emits).

## Test Coverage Deltas (per-section, cont.)

**Baseline (established this audit):** `dotnet test WebPSharp.Tests` → **472 passed, 0 failed, 0 skipped** (~2s, net10.0). This is the green baseline the "tests exist AND pass" PASS-criterion is measured against; re-run after any fix.

_(sections needing new tests, tracked as they're identified)_
- **§1 RIFF (test-eng done): PROVEN today** — too-short, bad-RIFF, bad-WEBP, riffSize>buffer, gross chunk-overflow, pad emission, riffSize back-patch, basic even+odd round-trip. **MISSING (must-add for PASS, priority order):** T7 ANMF inner size `0x7FFFFFFF`→WebPFormatException (RED, encodes live bug); T1 `riffSize<4` reject; T3 trailing-bytes-beyond-riffSize ignored; T4 zero-size chunk parses+continues; T6 trailing odd chunk missing pad tolerated; T5 interior odd-chunk pad-skip + offset. Recommended: T2 chunk-exceeds-body-but-fits-trailing, T8 direct `RiffReader.MoveNext` fuzz asserts only WebPFormatException. Full byte-level specs captured in agent transcript.
- **§7 boolean:** interop KAT test (decode real cwebp bool partition / byte-match enc vs libwebp) + read-past-end + empty-partition + signed-semantics pin.
- **§12 VP8L header:** `Encode_DimAbove16384_Throws` (RED until guard added), max-dim 16384 round-trip, signature-mismatch reject, version 1-7 sweep, alpha-bit round-trip, canvas≠header reject, ReadBits count 0/32/multi-byte.
- **§17 ALPH:** per-filter (0-3) round-trip incl 1×N/N×1, C=2/3 reject, reserved!=0 reject, `Decode_AcceptsPreprocessingP1` (RED until fix), raw-truncation reject, all-transparent/opaque, dwebp-reads-our-alpha interop.
- **§10 loop filter:** direct decode-kernel golden vectors (Vp8FilterApply H16/V16/H16i/H8), simple-filter fixture (`cwebp -f 0`), sharpness/segment/lf-delta fixtures, all-zero-non-skip inner-edge suppression assertion (tighten beyond the single ≤1 golden).

---
---

# ═══════════ WebP Specification Compliance Report (FINAL) ═══════════
_Audit complete: all 21 sections reviewed against the WebP spec + RFC 6386, cross-checked against libwebp 1.6.0 source, adversarially challenged, test-assessed, and (§21) empirically validated with `dwebp`/`webpinfo`/`webpmux`._

## Executive Summary

- **Sections reviewed:** 21 / 21 (100%). Baseline suite: **472 tests pass, 0 fail**.
- **Decoder:** Strong. The VP8 lossy decode pipeline (frame header, boolean coder, coefficients/DCT/dequant, prediction, loop filter, reconstruction/upsampling) and the VP8L lossless decode pipeline (header, transforms, color cache, Huffman, LZ77) are **verified bit-exact to libwebp constant-by-constant**. Container parsing is adversarially unbreakable and memory-safe; no unbounded allocation from untrusted sizes.
- **Encoder:** One **SEVERE interop failure** — the *default* lossless output is not decodable by libwebp/browsers.
- **Overall verdict:** **PARTIAL / NOT SHIP-READY at defaults.** The decoder is production-quality; the encoder's default lossless path must be fixed before output can be trusted by third parties.

**Status tally:** PASS(decoder core) — §2,§8,§9,§10,§11 (+ §6-parse); PASS-impl/PARTIAL-tests — §13→(now FAIL via §21),§14,§15,§16; PASS+adversarial, tests-pending — §1; PARTIAL(real deviation) — §3,§5,§7,§12,§17,§19,§20; **FAIL — §13(emission)/§21**.

## Severity-ranked deviations (the whole audit's findings, prioritized)

| # | Sev | Section | Deviation | Fix |
|---|-----|---------|-----------|-----|
| 1 | **SEVERE** | §21/§13 | **Default `WebP.Encode()` lossless (Predictor transform) output rejected by `dwebp` (BITSTREAM_ERROR)** — masked by self-round-trip tests | Byte-level diff predictor VP8L vs `cwebp -lossless`; fix emission. Interim: drop Predictor/CrossColor from candidates → valid LZ77/SubtractGreen/Palette |
| 2 | **BLOCKER** | §20/§19/§1 | ANMF inner sub-chunk `0x7FFFFFFF` → raw `ArgumentOutOfRangeException` escapes public `DecodeAnimation` (only raw-exception escape in codebase) | `WebP.cs:426` unsigned compare `size > (uint)(innerSpan.Length - start)` (or reuse RiffReader) |
| 3 | HIGH | §19 | Animation frame-rect exceeding canvas silently accepted (libwebp rejects) | Validate `x+w≤canvasW && y+h≤canvasH` in DecodeAnimation → WebPFormatException |
| 4 | MED | §17 | ALPH decoder rejects valid `P=1` (pre-processing) files that cwebp emits | `Vp8AlphaDecoder.cs:39` → reject only `P>1`, ignore P |
| 5 | MED | §12/§5/§21 | VP8L (and VP8) encoder: no dim>16384 guard → silent 14-bit truncation to wrong size | Throw for dim>16384 in `Vp8LEncoder`/`Vp8Encoder`/`WebPImage` |
| 6 | MED | §5 | VP8 profiles 1-3 accepted but version loop-filter override (v1→simple, v2/3→none) not applied | Clamp FilterType by profile, or reject Profile≠0 |
| 7 | MED | §3 | VP8X feature flags never validated vs actual chunks → silent-accept inconsistent + `WebPInfo` mis-report | Assert flag⇔chunk agreement in DecodeExtended; ReadInfo from real chunks |
| 8 | MED | §19 | Animation bg-color: WebPSharp fills bg color (RIFF text); libwebp zero-fills transparent → composite differs from browsers | Product decision: libwebp-parity (fill 0x00000000) recommended |
| 9 | MED | §19 | ALPH-in-ANMF alpha dropped on decode; lossy animated frames not encodable | Add ALPH handling in ParseFrameChunk; route BuildFrameChunk through VP8+ALPH |
| 10 | MINOR | §20/§5 | Non-key-frame throws base `WebPException` not `WebPFormatException` (only mislabeled structural error) | one-liner `Vp8Decoder.cs:81` |
| 11 | MINOR | §18 | Unknown-chunk FourCC with non-printable bytes corrupted via `ToString()` round-trip | store raw 4 bytes not string |
| 12 | MINOR | §16 | Stale doc comment `Vp8LDecoder.cs:10-12` (claims near-dist rejected; code handles them) | delete comment |
| — | LOW | §12,§13,§2 | dead `BytesConsumed`, unvalidated enc PredictorBits/mode, dead `MaxSimpleDimension` | cleanup |

## Remaining Work

- **Missing functionality:** lossy animated-frame encode + ALPH-in-ANMF decode (§19); i4x4/RD/trellis/skip encoder size-optimization (HANDOFF — quality only, spec-valid).
- **Known deviations:** #1 (SEVERE, blocks default lossless interop), #8 (bg-color semantics — needs a decision).
- **Interoperability risks:** #1 is the big one. Otherwise lossy/ALPH/animation/metadata verified interoperable with libwebp 1.6.0.
- **The pervasive root cause:** self-round-trip testing (WebPSharp encode → WebPSharp decode) is the *only* form of "interop" coverage across encoder + all VP8L sections. A symmetric enc/dec bug passes every such test — exactly how #1 shipped. **The highest-leverage remediation is adding real third-party gates** (dwebp/webpinfo shell-outs, skippable when absent) + committing `cwebp -lossless` golden assets (one asset set closes proof gaps for §12-16 simultaneously).

## Test Coverage Matrix (summary — per-section detail above in this file)

| Section | Existing coverage | Key missing tests | Status |
|---|---|---|---|
| §1 RIFF | strong (headline validations) | 6 must-add (T1,T3,T4,T5,T6,**T7 RED**) | PARTIAL |
| §2 simple fmt | decoder assets | decode-reject, VP8+NUL, inline-alpha decode | PASS-impl |
| §3 VP8X | flag surfacing on valid files | flag↔chunk disagreement (both dirs), size≠10 | PARTIAL |
| §5/6 VP8 hdr | profile-0 single-part only | multi-partition, segmentation, profile1-3 (**RED**), prob-update, all rejections | PARTIAL |
| §7 boolean | self-round-trip | **interop KAT** (dwebp partition / libwebp bytes), read-past-end | PARTIAL |
| §8 prediction | modes 0-5 absolute | VR4/VL4/HD4/HU4 non-uniform, top-right-edge, BModeToPred | PASS-impl |
| §9 coeff/DCT | golden end-to-end | KAT IDCT/IWHT vectors, cat6, dequant Y2/UV | PASS-impl |
| §10 loop filter | 1 ≤1-golden + wrong-class unit | decode-kernel vectors, simple/sharp/seg/lf-delta fixtures | PASS-impl |
| §11 recon/upsample | golden ≤1 | non-tautological YUV vectors, fancy-weight vectors, nofancy-odd | PASS-impl |
| §12-16 VP8L | self-round-trip only | **`cwebp -lossless` interop assets** (closes all 5), repeat/max_symbol decode, hash golden, near-dist decode, absolute predictor vectors | PARTIAL |
| §17 ALPH | 1 file + opaque-omit | per-filter, C=2/3 reject, **P=1 accept (RED)**, dwebp-reads-our-alpha | PARTIAL |
| §18 metadata | self-round-trip | byte-level fourcc (`XMP `), writer-ordering, dup-XMP, third-party | PASS-core |
| §19 animation | self-round-trip | **frame-rect-reject (RED)**, **ANMF-overflow (RED)**, blend-math, anim_dump interop, lossy-frame | PARTIAL |
| §20 errors | fuzz (lossless only) | **ANMF-overflow (RED)**, non-key WebPFormat, lossy/ALPH/VP8X/anim fuzz | PARTIAL |
| §21 encoder | **none via dwebp** | **dwebp/webpinfo interop gates (RED on predictor)**, dim>16384 reject | FAIL |

## Recommended Implementation Order

1. **Decoder correctness / robustness first:** #2 ANMF overflow (BLOCKER, raw-exception escape) → #3 frame-rect bounds → #4 ALPH P=1 (unblocks cwebp alpha files) → #10 exception taxonomy.
2. **Encoder correctness:** #1 predictor VP8L emission (SEVERE — do the byte-level diff; interim mitigation = drop predictor candidates) → #5 dim guard.
3. **Spec-compliance fixes:** #6 profile 1-3 filter override → #7 VP8X flag agreement.
4. **Error handling:** extend fuzz to lossy/ALPH/VP8X/animation; add the RED regression tests.
5. **Interoperability:** add dwebp/webpinfo integration gates + commit `cwebp -lossless` golden assets (this is what would have caught #1); decide #8 bg-color semantics; implement #9 lossy-animation.
6. **Performance / size:** i4x4, RD, trellis, skip signaling (HANDOFF — quality only, all spec-valid).

_All fixes above are documented (root cause + exact file:line + regression test) and staged — none applied, per "no unsolicited changes." Ready to apply on request; recommend starting with #1 and #2._

═══════════════════════ END OF AUDIT ═══════════════════════
