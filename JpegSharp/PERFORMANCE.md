# JpegSharp — Performance Opportunities

Analysis of the codec hot path, focused on the **8-bit baseline sequential YCbCr
Huffman** case (the overwhelmingly common 8-bit JPEG). Ordered by impact-to-risk.

## Progress tracker (autonomous optimization loop)

| # | Item | Tier | Status |
|---|------|------|--------|
| 1 | BitWriter bulk buffering | 1 | ✅ Completed |
| 2 | FastDct scalar constants + unroll | 1 | ✅ Completed |
| 3 | Quantize manual rounding | 1 | 🚫 Blocked (regresses on .NET 10) |
| 4 | Fuse zig-zag into (de)quant | 2 | 🔄 In progress |
| 5 | ExtractBlock interior fast path | 2 | ⬜ Todo |
| 6 | BitReader ulong bulk refill | 2 | ⬜ Todo |
| 7 | SIMD color conversion | 3 | ⬜ Todo |
| 8 | Integer DCT (AAN/Loeffler) | 3 | ⬜ Todo |
| 9 | Faster level-shift rounding | 3 | ⬜ Todo |

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

### 5. `ExtractBlock` runs `Math.Min` on every sample even for interior blocks (encode)
`BaselineEncoder.cs:304-334` — edge clamping is only needed for blocks touching
the right/bottom edge. Add a fast path: when
`x0 + 8 <= PlaneWidth && y0 + 8 <= PlaneHeight`, do a straight strided copy with
no `Math.Min`. That covers the vast majority of blocks. Bit-exact.

### 6. `BitReader` refills 8 bits at a time with a per-byte `0xFF` check (decode)
`JpegSharp/Bitstream/BitReader.cs:64-104,199-229` — widen the accumulator to
`ulong` and add a fast refill that, when the next several source bytes contain no
`0xFF`, loads them in bulk and only falls back to byte-at-a-time stuffing/marker
logic near a `0xFF`. This is the standard libjpeg-turbo approach and meaningfully
speeds Huffman decode. Bit-exact, but the most delicate change here — isolate it
behind the existing tests.

---

## Tier 3 — larger efforts / bigger ceiling (output-changing or structural)

### 7. SIMD the plane color conversion
`YCbCrToRgb`/`RgbToYCbCr` over whole planes (`ColorConverter.cs:106-123`, decode
assembly at `BaselineDecoder.cs:749-768`) is scalar per pixel — a large share of
color-image decode. Vectorizing with `System.Numerics.Vector` / `Vector256` is
bit-exact if the same fixed-point integer math is kept.

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
