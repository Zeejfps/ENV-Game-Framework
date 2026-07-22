# JPEG Specification Compliance Report — JpegSharp (pure C#)

**Standards reviewed:** ITU-T T.81 (JPEG), ITU-T T.871 (JFIF), ITU-T T.872 (Exif/SPIFF metadata conventions)
**Codebase:** `JpegSharp/` (namespace `JpegSharp.*`, net10.0); tests in `JpegSharp.Tests/`
**Method:** each section reviewed by an independent compliance agent, PASS verdicts challenged by an adversarial agent, and cross-checked against existing tests by a test-coverage agent (37 agents, 17 sections).
**Date:** 2026-07-22

---

## Executive Summary

The implementation is a **substantially complete and well-tested JPEG codec** covering baseline (SOF0), extended-sequential 12-bit (SOF1), and progressive (SOF2) — encode and decode — with restart intervals, 4:4:4/4:2:2/4:2:0/4:1:1 subsampling, YCbCr/RGB/CMYK/YCCK color, and JFIF/Exif/ICC/Adobe metadata. Lossless (SOF3) and arithmetic coding are intentionally unsupported and rejected. On **valid, common 8-bit JPEG files the codec is correct** and the entropy/dequant core is solid.

Of 17 audited sections:

| Verdict | Count | Sections |
|---|---|---|
| **PASS** | 2 | Huffman entropy decode; Dequantization + zig-zag |
| **PARTIAL** | 13 | most file-format parsers, progressive, all encoder sections, metadata, robustness |
| **FAIL** | 2 | Inverse DCT level-shift (12-bit overflow); high-precision color conversion |
| **UNKNOWN** | 0 | — |

**Nature of the gaps.** The two FAILs are genuine correctness bugs, but both live in the **high-precision (≥12-bit) path**, not the mainstream 8-bit path. The 13 PARTIALs are overwhelmingly **strictness / input-validation gaps**: the decoder is *lenient* toward malformed or out-of-spec streams (it does not reject reserved/oversized table fields, out-of-range scan parameters, etc.) rather than mis-decoding valid input. This is a security/robustness and conformance concern, not a "wrong pixels on normal photos" concern.

### Top risks
1. **[FAIL] IDCT level-shift integer overflow (12-bit).** `StoreBlock` (`BaselineDecoder.cs:464/478`) adds the level-shift in unchecked `int` arithmetic; a large-positive dequantized IDCT output (pathological/hostile 12-bit quant tables) overflows and wraps negative, so `Math.Clamp` returns **0 (black) for a should-be-white (2^P−1) pixel** — the clamp requirement is inverted. Adversarial agent refuted the reviewer's PASS.
2. **[FAIL] High-precision YCbCr↔RGB centering.** Conversion must center at 2^(P−1) across supported precisions; high-precision path is incorrect (see `color-upsample.md`).
3. **[strictness] Missing structural validation** — over-full Huffman tables (Σ BITS > 256), reserved DHT `Tc`, `DQT` `Pq∉{0,1}`, `SOF` ΣHᵢVᵢ>10 / bad precision / DAC-not-rejected, `SOS` `Ns` range and baseline Ss/Se/Ah/Al — accepted instead of rejected. Fuzz/robustness exposure.
4. **[strictness] Restart-marker desync** not detected; missing/misordered RSTn not handled gracefully in all paths.
5. **[gap] Interoperability** — no real-world third-party JPEG fixtures in-repo (tests synthesize in-memory), and no automated cross-check that encoder output decodes in libjpeg or that libjpeg/camera files decode here.

---

## Compliance Matrix

| Specification Section | Compliance | Adversarial Review | Tests (new/existing) | Findings file |
|---|---|---|---|---|
| Marker structure & segment framing (SOI/EOI/length/stuffing) | PARTIAL | n/a (not PASS) | 5 new / 24 exist | `marker-framing.md` |
| DQT quantization-table segment parsing | PARTIAL | n/a (not PASS) | 8 new / 11 exist | `dqt.md` |
| DHT Huffman-table segment parsing & table build | PARTIAL | n/a (not PASS) | 9 new / 21 exist | `dht.md` |
| SOF frame-header parsing (SOF0/1/2, reject SOF3/9-11/DAC) | PARTIAL | n/a (not PASS) | 14 new / 14 exist | `sof.md` |
| SOS scan-header parsing | PARTIAL | n/a (not PASS) | 9 new / 7 exist | `sos.md` |
| DRI interval + RSTn restart resync | PARTIAL | n/a (not PASS) | 10 new / 22 exist | `dri-restart.md` |
| Component handling, sampling factors, MCU structure & block order | PARTIAL | n/a (not PASS) | 7 new / 13 exist | `component-mcu.md` |
| Huffman entropy decode: DC diff, AC run/size, sign-extend, EOB, ZRL | PASS | upheld | 12 new / 17 exist | `huffman-decode.md` |
| Dequantization + zig-zag reordering | PASS | upheld | 8 new / 15 exist | `dequant-zigzag.md` |
| Inverse DCT, level shift, clamp/round | FAIL | REFUTED PASS | 10 new / 14 exist | `idct-levelshift.md` |
| Color conversion (YCbCr/RGB/CMYK/YCCK) + chroma upsampling | FAIL | n/a (not PASS) | 7 new / 26 exist | `color-upsample.md` |
| Progressive: spectral selection + successive approximation + refinement | PARTIAL | n/a (not PASS) | 9 new / 26 exist | `progressive.md` |
| Encoder FDCT + quantization + zig-zag ordering | PARTIAL | n/a (not PASS) | 5 new / 17 exist | `enc-fdct-quant.md` |
| Encoder Huffman generation (standard+optimized) + bitstream + byte stuffing | PARTIAL | n/a (not PASS) | 8 new / 33 exist | `enc-huffman-bitstream.md` |
| Encoder marker emission + overall file-structure compliance | PARTIAL | n/a (not PASS) | 10 new / 32 exist | `enc-markers-structure.md` |
| Metadata: JFIF (APP0), Exif (APP1), ICC (APP2 multi-seg), Adobe (APP14), COM | PARTIAL | n/a (not PASS) | 16 new / 25 exist | `metadata.md` |
| Robustness: truncated / corrupt / malformed input | PARTIAL | n/a (not PASS) | 7 new / 44 exist | `robustness.md` |

---

## Test Coverage Matrix

| Specification Section | Existing Tests | Coverage Gaps | Required New Tests | Status |
|---|---|---|---|---|
| Marker structure & segment framing (SOI/EOI/length/stuffing) | 24 | 5 | 5 | partial |
| DQT quantization-table segment parsing | 11 | 8 | 8 | partial |
| DHT Huffman-table segment parsing & table build | 21 | 8 | 9 | partial |
| SOF frame-header parsing (SOF0/1/2, reject SOF3/9-11/DAC) | 14 | 11 | 14 | partial |
| SOS scan-header parsing | 7 | 7 | 9 | partial |
| DRI interval + RSTn restart resync | 22 | 10 | 10 | partial |
| Component handling, sampling factors, MCU structure & block order | 13 | 6 | 7 | partial |
| Huffman entropy decode: DC diff, AC run/size, sign-extend, EOB, ZRL | 17 | 11 | 12 | covered |
| Dequantization + zig-zag reordering | 15 | 7 | 8 | covered |
| Inverse DCT, level shift, clamp/round | 14 | 8 | 10 | **gap — bug unproven** |
| Color conversion (YCbCr/RGB/CMYK/YCCK) + chroma upsampling | 26 | 8 | 7 | **gap — bug unproven** |
| Progressive: spectral selection + successive approximation + refinement | 26 | 9 | 9 | partial |
| Encoder FDCT + quantization + zig-zag ordering | 17 | 5 | 5 | partial |
| Encoder Huffman generation (standard+optimized) + bitstream + byte stuffing | 33 | 8 | 8 | partial |
| Encoder marker emission + overall file-structure compliance | 32 | 8 | 10 | partial |
| Metadata: JFIF (APP0), Exif (APP1), ICC (APP2 multi-seg), Adobe (APP14), COM | 25 | 17 | 16 | partial |
| Robustness: truncated / corrupt / malformed input | 44 | 8 | 7 | partial |

**Totals:** 361 existing test references identified as relevant, **144 coverage gaps**, **154 new tests specified**, **55 required fixes** across sections. Per-section detail (test names, purpose, spec ref, classification, expected result) is in `.jpeg-audit/findings/<key>.md`.

---

## Specification Deviations (major / blocker)

| Section | Severity | Deviation | Spec reference |
|---|---|---|---|
| dqt | major | Decoder must reject Pq (precision nibble) not in {0,1}. | T.81 B.2.4.1 (Pq = 0 -> 8-bit, Pq = 1 -> 16-bit; reject Pq not in {0,1}) |
| dht | major | Tc (table class) must be in {0,1}; values 2..15 are reserved and must be rejected (T.81 B.2.4.2, Table B.5) | T.81 B.2.4.2 / Table B.5 (Tc definition) |
| dht | major | Sum of the 16 BITS counts (total number of codes / HUFFVAL bytes) must be <= 256 | T.81 B.2.4.2 (sum of Li <= 256) / Annex F.2.2 |
| sof | major | Sum of Hi*Vi <= 10 for interleaved scans (T.81 A.2.2 / B.2.2 max 10 data units per MCU) | ITU-T T.81 A.2.2 / B.2.2 |
| sof | major | Reject unsupported precisions; valid DCT precision is 8 (baseline) or 8/12 (extended/progressive) only | ITU-T T.81 B.2.2 (P: baseline 8; extended/progressive 8 or 12) |
| sof | major | Must reject DAC (Define Arithmetic Conditioning marker) | ITU-T T.81 B.2.4.3 (DAC); requirement to reject DAC |
| sos | major | Reject Ns=0 or Ns>4 (T.81 B.2.3: 1 <= Ns <= 4) | T.81 B.2.3 (Ns range 1..4) |
| sos | major | Baseline: Ss=0, Se=63, Ah=Al=0 | T.81 B.2.3 (baseline Ss=0,Se=63,Ah=Al=0) |
| dri-restart | major | Decoder must handle missing/misordered RST gracefully. | ITU-T T.81 F.1.2.3 / E.1.1 (restart markers, graceful resync) |
| component-mcu | major | Non-interleaved (single-component) scans must use per-component dims: blocksPerLine=ceil(xi/8), xi=ceil(X*Hi/Hmax); blocksPerCol=ceil(yi/8), yi=ceil(Y*Vi/Vmax); MCU = 1 data unit (T.81 A.2.2, A.2.4). | ITU-T T.81 A.2.2, A.2.4 |
| component-mcu | major | A baseline sequential frame may be coded as multiple (typically non-interleaved) scans; a decoder must process every scan (T.81 A.2, B.2.3). | ITU-T T.81 A.2, B.2.3 |
| idct-levelshift | major | StoreBlock (BaselineDecoder.cs:464 8-bit, :478 high-precision) computes `value = (int)Math.Round(spatial) + center` in unchecked int arithmetic (net10.0, no CheckForOverflowUnderflow). Dequantized coefficients reach ~2.147e9 each (short coeff up to 32767 times ushort quant step up to 65535, Quantizer.cs:45), so an IDCT output pixel can exceed int.MaxValue. The double->int conversion saturates to int.MaxValue, then `+ center` overflows and wraps to a large NEGATIVE int, so Math.Clamp(negative,0,max) returns 0 instead of the required 2^P-1. The explicit clamp-to-max requirement is inverted for large-positive IDCT results: a should-be-saturated-white pixel decodes as black (0). The reviewer's 'Correct. clamp performed after level shift' finding overlooked that the level-shift addition itself overflows before the clamp sees a sane value. | ITU-T T.81 A.3.1 (output clamped to [0, 2^P-1]) |
| color-upsample | blocker | T.871 YCbCr<->RGB centered at 2^(P-1); high-precision conversion must be correct across supported precisions (9-16 bit) | T.871 YCbCr/RGB conversion; internal support of P=9..16 |
| progressive | major | T.81 B.2.3 / G.1: a progressive DC scan may be non-interleaved (single component), in which case data units are traversed in that component's own block raster (ceil(actualW/8) x ceil(actualH/8)), not the full-image MCU grid. | T.81 B.2.3 (MCU order), G.1.2 |
| enc-fdct-quant | major | Quality scaling must clamp elements to [1,255] for 8-bit OR [1,65535] for 16-bit depending on output precision. | T.81 Annex K.1 scaling; B.2.4.1 (Pq/Qk element precision) |
| metadata | major | COM arbitrary bytes must round-trip losslessly | T.81 B.2.4.5 (COM = arbitrary bytes); task requirement 'COM arbitrary bytes' |
| metadata | major | ICC APP2 chunks must reassemble correctly; reader must tolerate duplicate/malformed chunk sets without producing corrupt output | ICC.1 Annex B / T.872 (ICC_PROFILE\0 + seqno + count, reassemble in order); task requirement 'reassemble in order' and 'tolerate duplicate' |
| robustness | major | restart marker desync detection | ITU-T T.81 E.1.4 / F.1.4 (restart marker sequence RSTm, m = 0..7 mod 8) |
| robustness | major | no unbounded allocation / fail safely with a typed exception | T.81 B.2.2 (dimensions up to 65535x65535); robustness: typed failure + bounded allocation |

---

## Remaining Work

### Correctness bugs (fix first)
- **idct-levelshift**: perform level-shift/clamp in a wider type (or clamp the IDCT result before the shift); add 12-bit hostile-quant regression test proving white saturates to 2^P−1, not 0.
- **color-upsample**: correct high-precision centering (2^(P−1)) for 9–16-bit; add high-precision color round-trip tests.

### Strictness / conformance (reject malformed input)
- DHT: reject `Tc`∉{0,1} and Σ BITS > 256 (over-full table).
- DQT: reject `Pq`∉{0,1}; validate segment length vs element count.
- SOF: reject ΣHᵢVᵢ>10 (interleaved), unsupported precision, and DAC; already rejects SOF3/SOF9–11.
- SOS: enforce `1≤Ns≤4` and baseline `Ss=0,Se=63,Ah=Al=0`.
- Markers: drive segment framing through `HasLengthField` (currently dead code); handle standalone markers in segment position; optionally verify trailing EOI.
- Restart: detect RSTm desync (m mod 8) and handle missing/misordered markers.
- Robustness: bound allocation from attacker-controlled length/dimension fields; typed exceptions on all malformed paths.

### Feature/spec completeness
- Non-interleaved multi-scan baseline frames and non-interleaved progressive DC scans: verify per-component block-raster traversal (`component-mcu`, `progressive`).
- Encoder quality-scaling clamp to [1,255]/[1,65535] by precision; guarantee no quant element becomes 0.
- Metadata: lossless COM round-trip; tolerant ICC multi-chunk reassembly with duplicate/malformed chunk sets.

### Interoperability / fixtures
- Add third-party JPEG fixtures (libjpeg, GIMP, camera) under `JpegSharp.Tests/Assets/`.
- Add encoder→libjpeg and libjpeg→decoder interop tests, plus baseline & progressive reference-image decode.

---

## Recommended Implementation Order
1. **Decoder correctness** — idct-levelshift overflow; high-precision color.
2. **Marker/structure strictness** — DHT/DQT/SOF/SOS validation; marker framing via `HasLengthField`.
3. **Entropy/restart robustness** — RSTn desync detection, bounded allocation, typed failures.
4. **Encoder correctness** — quant clamp, optimized-Huffman code-length limiting, byte-stuffing/interop tests.
5. **Progressive** — non-interleaved traversal, EOBRUN/refinement edge cases.
6. **Metadata** — COM round-trip, ICC reassembly tolerance.
7. **Performance** — after conformance is locked by tests.

---

## Artifacts
- `.jpeg-audit/state.json` — machine-readable audit state (17 sections, statuses, 19 major/blocker issues).
- `.jpeg-audit/findings/<key>.md` — full per-section findings, code locations, adversarial results, and specified tests.
- `.jpeg-audit/audit-data.json` — raw structured output from all 37 agents.
