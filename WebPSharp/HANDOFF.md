# WebPSharp — Handoff

Status snapshot for a new agent taking over. Read this, then `CHECKLIST.md` (fine-grained task
list) and `README.md` (architecture/API).

## Where things live

- **Worktree:** `.claude/worktrees/webpsharp` (branch `worktree-webpsharp`). Do all work here; run
  commands from this directory. Do **not** `cd` to the main repo.
- **PR:** #19 against `master` (`https://github.com/Zeejfps/ENV-Game-Framework/pull/19`). Push to
  `worktree-webpsharp` to update it.
- **Projects:** `WebPSharp` (library), `WebPSharp.Tests` (xUnit), `WebPSharp.Benchmarks`
  (BenchmarkDotNet + `--smoke`). All in `ENV Game Framework.sln`.
- **Test fixtures:** `WebPSharp.Tests/Assets/` — real `.webp` files from `cwebp` plus their
  `dwebp`-decoded `.rgba` references (committed).

## Build & test

```
dotnet test WebPSharp.Tests/WebPSharp.Tests.csproj        # 451 tests, ~2s
dotnet build WebPSharp/WebPSharp.csproj                    # warnings-as-errors; must be 0 warnings
dotnet run --project WebPSharp.Benchmarks -c Release -- --smoke   # end-to-end correctness
```

Do not commit unless asked. End commit messages with the `Co-Authored-By: Claude ...` trailer.

## What is DONE (complete + tested)

- **VP8L lossless — encode + decode.** All 4 transforms (predictor, cross-color, subtract-green,
  color-indexing w/ pixel bundling), color cache, LZ77, meta-Huffman, near-distance table.
  Encoder has effort-driven candidate selection. Round-trip + combinatorial + fuzz tested.
- **VP8 lossy — DECODE only.** Header, coefficient/mode/residual decode, reconstruction, in-loop
  deblocking filter, fancy (bilinear) chroma upsampling. **Validated pixel-exact vs `dwebp`** on 8
  diverse golden cases (`Vp8GoldenBatchTests`).
- **ALPH alpha (for lossy)** — parse + decode (raw / lossless) + unfilter. Pixel-exact vs `dwebp`
  (`WebPAlphaTests`).
- **Container:** RIFF read/write, VP8X, ICC/EXIF/XMP metadata, unknown-chunk preservation, strict
  chunk validation, VP8X canvas-consistency check.
- **Animation:** ANIM/ANMF encode+decode, blend/dispose/timing/loop, canvas composition.
- **Public API:** `WebP.Identify/Decode/Encode/Load/Save/EncodeAnimation/DecodeAnimation` + async
  variants; `WebPImage/Info/Metadata/Animation/Frame`, options. Enriched `Identify`.
- **Cross-cutting:** fuzz/corruption harness (~10k mutations), property/combinatorial tests,
  integration tests, benchmarks, README + XML docs.

## What is LEFT (priority order)

1. **Lossy VP8 ENCODE** — the biggest remaining piece. Needs: forward pipeline (RGB→YUV, FDCT/FWHT
   already exist and are tested, quantization, mode decision, boolean *encoder* already exists and
   round-trips, coefficient token *encoding*, header writing, rate control by quality). This is a
   large multi-iteration effort. The forward transforms and boolean encoder are done; the token
   encoding + mode search + header writing are not. Validate by encoding → `dwebp` decode → compare,
   or encode → my decode round-trip within a PSNR threshold.
2. **Alpha ENCODE** — pairs with lossy encode (ALPH chunk writing: filter + optional VP8L compress).
3. **Animated lossy frames** — `DecodeAnimation` currently throws for VP8 frames? (No — animation
   frames call `DecodeLossy` now via the wired path, but confirm/round-out `EncodeAnimation` still
   only writes VP8L frames.) Lossy animation frames need lossy encode first.
4. **VP8 profiles / advanced** — only profile 0 (bicubic reconstruction filter) is exercised by the
   fixtures; other reconstruction filters (profiles 1-3) use simpler interpolation but the
   coefficient path is the same. Not validated; generate fixtures with `cwebp -m`/segments to widen
   coverage if needed.
5. **Fancy-upsampling edge exactness** — matches `dwebp` default within ≤1 on all fixtures; the ≤1
   is RGB rounding. If you want exact 0, verify the YUV→RGB rounding path.

## THE GOLDEN-VALIDATION WORKFLOW (most important thing to know)

`cwebp`, `dwebp`, `webpinfo` are installed (`/opt/homebrew/bin`, libwebp 1.6.0). This is how every
VP8 piece was validated and how you should validate anything new:

```
# make a test image (PAM is trivial to write; cwebp reads PNM/PAM/PNG)
# encode lossy:            cwebp -q 80 in.pam -o out.webp
# reference decode:        dwebp out.webp -pam -o ref.pam           (default = fancy upsampling)
#   isolate reconstruction: dwebp -nofilter -nofancy out.webp -pam  (no loop filter, nearest)
# inspect:                 webpinfo out.webp
```

Strip the PAM header (everything through `ENDHDR\n`) to get raw RGBA and compare to
`WebP.Decode(...).PixelData`. Fetch libwebp source for exact constants/algorithms via `curl`
(deterministic — do NOT rely on model transcription for large tables):
`https://raw.githubusercontent.com/webmproject/libwebp/main/src/<path>`. Key files used:
`dec/vp8_dec.c` `dec/vp8l_dec.c` `dec/frame_dec.c` `dec/tree_dec.c` `dec/quant_dec.c`
`dec/alpha_dec.c` `dsp/dec.c` `dsp/upsampling.c` `dsp/filters.c`.

Extract large tables with a script that **validates the count** (see how `Vp8Tables.cs` was made) —
never hand-transcribe 100+ numbers.

## Gotchas / lessons learned (will save you hours)

- **16×16 mode enum aliasing.** VP8 aliases 16×16/chroma modes onto the 4×4 B-mode enum:
  `DC=0, TM=1(=B_TM), V=2(=B_VE), H=3(=B_HE)` — NOT 0,1,2,3. This is because a 16×16 macroblock's
  mode is used as the neighbor context for an adjacent 4×4 block's `kBModesProba`. See
  `Vp8Decoder.DcPred/TmPred/VPred/HPred`. Getting this wrong looks like small errors that propagate
  from top-left toward bottom-right.
- **4×4 top-right replication.** The rightmost 4×4 column's top-right samples come from the
  macroblock's top border (replicated down), NOT the plane (the next MB isn't reconstructed yet).
  See `GatherLuma4Top`.
- **Loop filter must be libwebp-exact.** My RFC-6386 `Vp8LoopFilter` (used only in its own unit
  tests) is NOT bit-exact with libwebp (different `NeedsFilter` threshold scaling). The real decode
  uses `Vp8FilterApply` ported verbatim from `dsp/dec.c`.
- **Chroma prediction/context uses the same enum** as luma for the DC edge variants; VP8 fills
  unavailable top border with 127 and left border with 129.
- **Alpha lossless is a *header-less* VP8L stream** (no signature/dims); the alpha value is the
  GREEN channel. Then the ALPH filter (h/v/gradient) is un-applied. See `Vp8AlphaDecoder` +
  `Vp8LDecoder.DecodeAlpha`.
- **`Vp8BooleanDecoder` is a class** (not a ref struct) so multiple partitions (MB-header + DCT
  partitions) can coexist during the MB loop.
- **zsh doesn't word-split** unquoted vars — do fixture generation loops in Python, not bash arrays.

## Map of the VP8 code

```
WebPSharp/Vp8/
  Vp8BooleanDecoder.cs / Vp8BooleanEncoder.cs  arithmetic coder (RFC 6386)
  Vp8Tables.cs                                 all constant tables (from libwebp, count-verified)
  Vp8Transform.cs                              DCT/WHT (inverse used by decode; forward for encode)
  Vp8Prediction.cs / Vp8Prediction4.cs         intra prediction 16x16/8x8 and 4x4
  Vp8Coefficients.cs                           coefficient token decode (GetCoeffs/GetLargeValue)
  Vp8Decoder.cs                                header parse + MB decode + reconstruction + upsample
  Vp8FilterApply.cs                            libwebp-exact loop filters (decode path)
  Vp8LoopFilter.cs                             RFC-6386 filters (unit-tested only, not decode path)
  Vp8Yuv.cs                                    YUV->RGB
  Vp8AlphaDecoder.cs                           ALPH chunk decode
```

The full decode entry is `Vp8Decoder.DecodeToRgba(applyFilter, fancyUpsampling)`, wired into
`WebP.DecodeLossy` (in `Api/WebP.cs`), with alpha applied by `WebP.ApplyAlpha`.
