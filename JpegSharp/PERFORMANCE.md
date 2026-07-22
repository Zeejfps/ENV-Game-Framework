# JpegSharp — Performance Opportunities

Analysis of the codec hot path, focused on the **8-bit baseline sequential YCbCr
Huffman** case (the overwhelmingly common 8-bit JPEG). Ordered by impact-to-risk.

## Progress tracker (autonomous optimization loop)

| # | Item | Tier | Status |
|---|------|------|--------|
| 1 | BitWriter bulk buffering | 1 | ✅ Completed |
| 2 | FastDct scalar constants + unroll | 1 | ✅ Completed |
| 3 | Quantize manual rounding | 1 | 🚫 Blocked (regresses on .NET 10) |
| 4 | Fuse zig-zag into (de)quant | 2 | ✅ Completed |
| 5 | ExtractBlock interior fast path | 2 | ✅ Completed |
| 6 | BitReader ulong bulk refill | 2 | ✅ Completed |
| 7 | SIMD color conversion | 3 | ✅ Completed |
| 8 | Integer DCT (AAN/Loeffler) | 3 | ⏸ Awaiting user decision (output-changing) |
| 9 | Faster level-shift rounding | 3 | ⏸ Awaiting user decision (output-changing) |

Baseline gate (2026-07-22): Release build clean; **615/615 JpegSharp.Tests pass**.

**Legend:** ⬜ Todo · 🔄 In progress · ✅ Completed · 🚫 Blocked

## The pipeline, and where time goes

**Encode:** `ExtractBlock` → `FastDct.Forward` → `Quantizer.Quantize` → `ZigZag.FromNatural` → `EncodeBlock` + `BitWriter`

**Decode:** `DecodeBlock` + `BitReader` → `ZigZag.ToNatural` → `Quantizer.Dequantize` → `FastDct.Inverse` → `StoreBlock` → upsample → `YCbCrToRgb`

Everything downstream of entropy coding runs on `double`, which is the single
biggest structural cost.

**Legend:**
- **Bit-exact** — output is byte-for-byte identical (pure speed).
- **Output-changing** — alters coefficients/pixels; needs golden regeneration.

---

## Tier 1 — high impact, bit-exact (do these first)

### 1. `BitWriter` writes one byte at a time through a virtual `Stream.WriteByte` (encode)
`JpegSharp/Bitstream/BitWriter.cs:39-41` — every output byte is a virtual call,
and every `0xFF` triggers a second one. On a typical image this is millions of
virtual calls and is very likely the dominant encode cost after the DCT.

**Fix:** buffer into a local `byte[]` (e.g. 8 KB), do `0xFF` stuffing in the
buffer, flush in bulk with one `Stream.Write`. Zero output change; typically a
large win on entropy writing.

> ✅ **Completed 2026-07-22.** `BitWriter.cs` now buffers into an 8 KB `byte[]`,
> stuffs `0x00` after `0xFF` in-buffer, and bulk-drains via `Stream.Write` on fill
> and in `Flush()` (which drains after padding so caller-written markers/EOI stay
> ordered). No call-site changes. **Bit-exact:** 615/615 tests pass, and a
> stash-based before/after byte comparison on 512px images (baseline/restart/
> progressive/q100, up to 507 KB) produced identical SHA256 across the mid-stream
> drain path. **Benchmark** (Release Stopwatch, 20-warmup + 200 iters; BDN blocked
> by a locked-worktree `.csproj` glob collision): whole-encode Mean improved
> +2.5%→+9.2% across sizes (512×420 +9.2%: 5475→4972 µs), largest on entropy-heavy
> 4:2:0. Small % is expected — DCT dominates encode and the sink is an in-memory
> stream. Tradeoff: +8 KB buffer per writer instance. **Independently verified:**
> APPROVE (diff scope, boundary math no off-by-one, flush-ordering, no benchmark
> manipulation). Post-verify cleanup: removed 2 explanatory comments per house style.

### 2. `FastDct` indexes `static readonly double[,] Even/Odd` in the innermost loop
`JpegSharp/Transforms/FastDct.cs:20-21,107-123` — 2D array access in C# does two
bounds checks and can't be hoisted well. These are 4×4 constant matrices hit 16×
per 1D transform, 16 transforms per block.

**Fix:** replace with 16 named `static readonly double` scalars (or a flat
`double[16]` read via `Unsafe.Add`) and fully unroll `Forward1D`/`Inverse1D`.
Same constants → bit-exact. Removes the bounds checks from the busiest arithmetic
in the codec. Helps **both** encode and decode.

> ✅ **Completed 2026-07-22.** `FastDct.cs`: the two `static readonly double[,]`
> Even/Odd 4×4 matrices replaced by 32 named scalars (`E00..E33`, `O00..O33`,
> each keeping the identical `Basis()` value), and `Forward1D`/`Inverse1D` fully
> unrolled with the exact same operand order (no FP reassociation → bit-exact).
> 2D arrays + loops fully removed (no dead code). **Bit-exact:** 615/615 tests
> (incl. `DctTests`/`FastDctTests`), and stash-based before/after gave identical
> encoded AND decoded-RGB SHA256 on a 512px image. **Benchmark** (Release
> Stopwatch, best-of-5, 20-warmup + 200 iters): **encode +26–31%, decode +25–31%**
> (512 encode 8.52→6.29 ms, 512 decode 7.77→5.82 ms; 256 encode +31.2%). Biggest
> win to date — hits the hottest arithmetic on both paths. Tradeoff: more verbose
> (32 fields + explicit lines) but mechanical. **Independently verified:** APPROVE
> (all 32 coefficients + every expression re-derived operand-for-operand; SHA256
> identity re-executed; +26.7% encode / +31.5% decode re-measured independently).

### 3. `Quantize` uses the slow `Math.Round(double, MidpointRounding)` overload
`JpegSharp/Quantization/Quantizer.cs:26-28` — that overload is much slower than
manual rounding, and it is followed by `Math.Clamp(double, …)`.

**Fix:** bit-identical away-from-zero rounding, then clamp with an int compare:
```csharp
var v = coefficients[i] / table[i];
var r = v >= 0 ? Math.Floor(v + 0.5) : Math.Ceiling(v - 0.5);
```
Still one division per coefficient, but the rounding path collapses. Bit-exact.

> 🚫 **Blocked 2026-07-22 — not applicable on .NET 10 (measured regression).**
> The premise is false on this runtime: `Math.Round(double, MidpointRounding.AwayFromZero)`
> is already the fast, effectively-intrinsic path. A bit-exact manual replacement
> (`Math.Truncate` + fractional-part compare — deliberately avoiding the
> `v + 0.5` double-rounding trap; **fuzz-proven bit-identical over 60,084,346
> samples incl. 84,346 adversarial near-half/ULP cases, 0 mismatches**) measured a
> **+20–27% whole-encode regression** (512/444 6.23→7.90 ms), from branch-mispredict
> cost on ~786k quantize ops/encode. Swapping only `Math.Clamp` → branchless
> `Math.Min/Max` was perf-neutral (division dominates the loop). SHA256 of encode
> output was identical across all three variants (bit-exactness confirmed), and
> 615/615 tests passed — but with no gain, shipping would violate "never merge
> speculative optimizations." `Quantizer.cs` was reverted to its original state
> (working tree confirmed clean for this file). Revisit only if a future runtime
> makes `Math.Round(AwayFromZero)` slow again, or if fused into an integer-DCT
> rewrite (#8) where rounding folds into fixed-point scaling.

---

## Tier 2 — moderate impact, bit-exact

### 4. Fuse zig-zag into (de)quantization to remove a full 64-element pass per block
Decode does `DecodeBlock`→`zz`, then `ZigZag.ToNatural`→`natural`, then
`Dequantize(natural)` (`BaselineDecoder.cs:629-631`). Dequantize directly from
zig-zag order into a natural-order buffer using a **zig-zag-permuted quant table**
(precompute once per table), eliminating `ToNatural` entirely. Symmetric win on
encode: quantize + zig-zag in one pass. Bit-exact; removes one full-block copy
and loop per block.

> ✅ **Completed 2026-07-22.** Added a per-`QuantizationTable` zig-zag-permuted
> table (`_valuesZigZag[k] = _values[Order[k]]`, built once in the constructor,
> exposed via `AsZigZagSpan()`), and two fused `Quantizer` methods:
> `QuantizeToZigZag` (gather+divide→zig-zag out, no intermediate `quantized`+
> `FromNatural`) and `DequantizeFromZigZag` (scatter+multiply zig-zag→natural, no
> `ToNatural`). Wired into all four baseline+progressive encode/decode call sites,
> removing a 64-element copy/loop per block (progressive also drops an extra
> `CopyTo`). Old `Quantize`/`Dequantize`/`ZigZag.ToNatural`/`FromNatural` kept
> (still used by tests / possible external consumers). **Bit-exact** (mult/div are
> order-independent; same away-from-zero rounding): 615/615 tests incl. progressive
> golden; stash-based before/after gave identical encoded-byte AND decoded-RGB
> SHA256 across baseline 444/420 and progressive 444/420. **Benchmark** (Release
> Stopwatch): **encode −1.3→−6.4%, decode −7.6→−10.7%** (decode gains more — drops
> both a copy and a scatter loop). Tradeoff: one cached `ushort[64]` per table
> (negligible). **Independently verified:** APPROVE (permutation direction
> re-derived — same `Order[k]` for source/dest/table, no transposition; progressive
> scan/accumulation logic untouched; SHA identity + 615/615 re-executed).

### 5. `ExtractBlock` runs `Math.Min` on every sample even for interior blocks (encode)
`BaselineEncoder.cs:304-334` — edge clamping is only needed for blocks touching
the right/bottom edge. Add a fast path: when
`x0 + 8 <= PlaneWidth && y0 + 8 <= PlaneHeight`, do a straight strided copy with
no `Math.Min`. That covers the vast majority of blocks. Bit-exact.

> ✅ **Completed 2026-07-22.** Added an interior fast path at the top of
> `BaselineEncoder.ExtractBlock`: when `x0 + 8 <= PlaneWidth && y0 + 8 <=
> PlaneHeight`, strided-copy the 8×8 block with no per-sample `Math.Min` (edge
> blocks fall through to the unchanged clamped path via an early `return`). Covers
> both the 8-bit (`−128`) and 16-bit (`Plane16`, `center = 1<<(precision−1)`)
> branches. **Bit-exact** — for interior blocks the `Math.Min` is a proven no-op,
> so identical samples in identical order: 615/615 tests, and independent fast-path
> on/off SHA256 identical across 512/510/300/20px in 444+420 (interior AND edge
> blocks). **Benchmark:** marginal — ~1–2% faster on realistic sizes, mostly within
> noise, **never a regression** (ExtractBlock is a tiny fraction of encode cost).
> **Independently verified & ship-arbitrated:** APPROVE — zero-risk, mechanical
> (mirrors libjpeg-turbo's interior/edge split), bit-exact; marginal-but-positive
> clears the bar. Kept as a correctness-neutral tidy, not a needle-mover.

### 6. `BitReader` refills 8 bits at a time with a per-byte `0xFF` check (decode)
`JpegSharp/Bitstream/BitReader.cs:64-104,199-229` — widen the accumulator to
`ulong` and add a fast refill that, when the next several source bytes contain no
`0xFF`, loads them in bulk and only falls back to byte-at-a-time stuffing/marker
logic near a `0xFF`. This is the standard libjpeg-turbo approach and meaningfully
speeds Huffman decode. Bit-exact, but the most delicate change here — isolate it
behind the existing tests.

> ✅ **Completed 2026-07-22.** `BitReader.cs`: accumulator widened `uint`→`ulong`;
> the two 8-bit fill loops replaced by a new `Refill` fast path that scans a bounded
> window (`n ∈ [1,7]`, shift ≤ 56 so never the `<<64` mask-to-0 trap) of proven
> non-`0xFF`, in-bounds bytes and bulk-loads them MSB-first. The delicate
> `FillByte` (`0xFF 0x00` stuffing, fill-runs, marker detection + `_pos--`
> step-back, EOD→`0xD9`) is **byte-for-byte unchanged** and remains the sole
> fallback — so the fast path is bit-exact by construction (every byte it loads
> would take FillByte's plain-data branch identically). The `room==0` boundary is
> unreachable (max read count ≤16 ⇒ `_count<16` at entry ⇒ room ≥6). **Bit-exact:**
> 615/615 tests + 202/202 delicate (corrupt/truncated/marker/restart/trailing/
> progressive); **72 encode→decode configs** (512²/1024², Q85/Q95/Q100, 444/420,
> RST0/4/8/16, sequential+progressive) gave identical decoded-RGB SHA256 vs reverted
> code. **Benchmark** (Release Stopwatch): whole-decode +1.7→+4.9% (implementer);
> independent re-time showed parity-to-slightly-faster (whole-decode can't resolve
> the entropy-decode fraction) — faster-or-equal, never a regression. Tradeoff:
> subtle shift-cap invariant (documented with one concise comment). **Independently
> verified:** APPROVE (shift-safety + no-`0xFF` invariant re-derived; SHA identity
> + 615/615 + 202/202 re-executed; no unsafe shift or failing adversarial input).

---

## Tier 3 — larger efforts / bigger ceiling (output-changing or structural)

### 7. SIMD the plane color conversion
`YCbCrToRgb`/`RgbToYCbCr` over whole planes (`ColorConverter.cs:106-123`, decode
assembly at `BaselineDecoder.cs:749-768`) is scalar per pixel — a large share of
color-image decode. Vectorizing with `System.Numerics.Vector` / `Vector256` is
bit-exact if the same fixed-point integer math is kept.

> ✅ **Completed 2026-07-22.** `ColorConverter.cs` + `BaselineDecoder.cs`
> (`AssembleThreeComponent`): vectorized the plane `YCbCrToRgb`/`RgbToYCbCr` with
> `Vector128<int>` (NEON on ARM64 / SSE on x86, `Vector128.IsHardwareAccelerated`
> guard), processing 4 pixels/iteration and reproducing the scalar fixed-point int
> math lane-for-lane — same coefficients, `Half=1<<15`, `128<<16` bias, arithmetic
> `>>16`, `Min(Max(v,0),255)` clamp, zero-extend widen. Original scalar loop is the
> remainder tail AND the no-hardware fallback, so correctness is hardware-independent.
> Decode 8-bit YCbCr assembly rerouted to one per-row vectorized call; high-precision
> (12/16-bit `long`) and direct-RGB/CMYK paths untouched. **Bit-exact:** 615/615
> tests (+ `ColorConverterTests` 11/11); **60 encode→decode configs** (gradient/
> random/8 pure+saturated colors × 512²/517²/300×301 × 444/420) gave byte-identical
> encoded JPEG AND decoded RGB vs scalar, cross-checked by re-decoding SIMD-encoded
> bytes with reverted scalar code. **Benchmark** (Release Stopwatch): decode −4.3→
> −8.2% (512²/1024², 444/420); encode neutral (DCT-dominated). Tradeoff: ARM64 caps
> at 4-wide NEON; x86 AVX2 path deliberately left out (unvalidatable on this host).
> **Independently verified:** APPROVE (lane math re-derived incl. shift/clamp/overflow;
> 60-config SHA identity + 615/615 re-executed; routing offsets/stride confirmed).

### 8. Integer DCT (AAN / Loeffler fixed-point)
The whole `double` transform + quant chain is the real ceiling. libjpeg-turbo's
islow/ifast integer DCT is several times faster and SIMD-friendly, and it lets you
fold dequant scaling into the IDCT. **Output-changing** (re-baseline goldens; keep
the current `double` `Dct`/`FastDct` as the correctness oracle they already are)
and a substantial project — but it's where the order-of-magnitude lives.

### 9. Faster level-shift rounding in `StoreBlock`
`LevelShiftClamp8` calls `Math.Round(double)` (banker's) per sample
(`BaselineDecoder.cs:710-714`). Switching to `(int)(x + 128.5)`-style rounding is
faster but changes half-integer results — **output-changing**, so only bundle it
with an integer-DCT re-baseline.

---

## Suggested starting point

Start with **#1 and #2**: both are bit-exact, self-contained, and hit the two
hottest spots (entropy writing and the DCT).

**Caveat — no clean baseline exists yet.** The benchmark run in
`BenchmarkDotNet.Artifacts/` currently shows `NA` (the single
`DecodeBaseline` / 64px / 4:4:4 case failed to run). Before optimizing, get
`JpegSharp.Benchmarks` producing valid encode + decode numbers on a realistic
size (512–1024px, both 4:2:0 and 4:4:4) so each change can be measured.
