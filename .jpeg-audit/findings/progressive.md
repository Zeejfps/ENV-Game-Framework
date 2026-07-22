# Progressive: spectral selection + successive approximation + refinement

**Section key:** `progressive`  
**Compliance:** PARTIAL · **Adversarial justified:** n/a · **Final:** **PARTIAL**

## Findings

- **[major]** T.81 B.2.3 / G.1: a progressive DC scan may be non-interleaved (single component), in which case data units are traversed in that component's own block raster (ceil(actualW/8) x ceil(actualH/8)), not the full-image MCU grid.
  - Observed: DecodeDcFirst and DecodeDcRefine unconditionally iterate the full MCU grid (_mcusPerCol x _mcusPerRow, expanding c.H x c.V blocks per MCU) and index predictors by scan component. For a single-component DC scan whose component is subsampled (c.H<_hmax or c.V<_vmax), the block count/positions are wrong, so such a spec-legal stream is misdecoded. It happens to be correct for interleaved DC scans (the only kind this encoder emits) and for non-subsampled single-component scans, which is why round-trip tests pass.
  - Spec: T.81 B.2.3 (MCU order), G.1.2
- **[minor]** AC first scan (G.1.2.2): run-length / ZRL must not advance the coefficient index past the end of the spectral band on malformed input; corrupt data should be rejected.
  - Observed: DecodeAcFirst (BaselineDecoder.Progressive.cs:184) does 'k += 16' for ZRL and 'k += r' for a run with no bounds check before the write path relies on the loop guard; an over-long run/ZRL is silently truncated (loop just exits) rather than raising JpegCorruptException. Baseline DecodeBlock (BlockScanCoder.cs:116,125) does validate this, so behavior is inconsistent.
  - Spec: T.81 G.1.2.2 / F.1.2.2
- **[minor]** AC refinement (G.1.2.3): a newly-nonzero coefficient has size exactly 1, and every signaled new coefficient must be placeable within Ss..Se.
  - Observed: DecodeAcRefine (BaselineDecoder.Progressive.cs:270-300) accepts any nonzero size nibble s as 'newly nonzero' (only bit sign is read) without checking s==1, and if the run advance reaches k>Se the pending new coefficient is silently dropped (line 299 guard) instead of flagging corruption. Tolerates malformed streams rather than rejecting them.
  - Spec: T.81 G.1.2.3
- **[minor]** DC magnitude category (SSSS) is bounded; for the supported precisions the DC category cannot exceed 15, and a decoded predictor must fit the coefficient storage.
  - Observed: DecodeDcFirst (BaselineDecoder.Progressive.cs:122) accepts categories up to 16 ('s is <0 or >16'); category 16 yields a 16-bit magnitude whose 'predictors[si] << scan.Al' is cast to short (line 126) and can overflow/wrap silently. Bound should be <=15 and/or the accumulated value range-checked (as DecodeBlock does at BlockScanCoder.cs:100).
  - Spec: T.81 Table F.1 / G.1.2.1
- **[info]** Successive approximation / spectral selection support (G.1).
  - Observed: EncodeProgressive (BaselineEncoder.Progressive.cs:31-43) emits a single fixed script: DC first Al=1 then one DC refine to Al=0, and per-component full-band AC (1..63) first Al=1 then one AC refine to Al=0. It cannot produce multi-stage successive approximation (e.g. Al 3->2->1->0) or split AC spectral bands. The decoder handles arbitrary Ss/Se/Ah/Al, but the encoder exercises only this one progression. Valid but limited.
  - Spec: T.81 G.1.1.1.1 / G.1.2
- **[info]** AC EOB-run coding (G.1.2.2): EOBn run-length batching of all-zero end-of-band blocks.
  - Observed: The decoder fully implements EOBRUN (BaselineDecoder.Progressive.cs:177-181 first, 262-314 refine) including reset on restart markers (lines 155,255). The encoder never batches: WriteAcScanEntropy/WriteAcRefineScan emit a plain EOB (0x00 = EOB0, run of 1) per block (lines 217,291). Spec-compliant and correctly decoded, but larger than optimal output; GatherAcFirstFrequencies (line 390) consequently ignores restart intervals, which is only safe because no EOB batching state crosses blocks.
  - Spec: T.81 G.1.2.2

## Required fixes

- Make DecodeDcFirst/DecodeDcRefine handle non-interleaved (single-component) DC scans: when scan.Components.Length==1, iterate that component's own block grid (CeilDiv(ComponentActualWidth,8) x CeilDiv(ComponentActualHeight,8)) with restart counted per data unit, instead of the full MCU grid. Otherwise a spec-legal subsampled non-interleaved DC scan is misdecoded.
- In DecodeAcFirst, validate coefficient index/ZRL against scan.Se (throw JpegCorruptException on overrun) to match the robustness of BlockScanCoder.DecodeBlock.
- In DecodeAcRefine, reject size nibbles other than 1 for newly-nonzero coefficients and raise a corruption error if a signaled new coefficient cannot be placed within Ss..Se (k>Se) rather than dropping it silently.
- Tighten the DC magnitude-category bound in DecodeDcFirst to <=15 and range-check the accumulated predictor before the (short) cast to avoid silent overflow on malformed 12-bit streams.
- (Optional / quality) Implement EOBRUN batching in WriteAcScanEntropy/WriteAcRefineScan for smaller output, and if done, make GatherAcFirstFrequencies and the restart logic reset EOBRUN at restart boundaries consistently.

## Code locations

- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.Progressive.cs:15 DecodeProgressive (single coefficient buffer allocated once, all scans accumulate, ReconstructComponents does final IDCT)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.Progressive.cs:64 DecodeProgressiveScan (Ss/Se validation + DC/AC first/refine dispatch)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.Progressive.cs:92 DecodeDcFirst (Al point transform: predictor<<Al)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.Progressive.cs:136 DecodeAcFirst (band Ss..Se + EOBRUN)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.Progressive.cs:199 DecodeDcRefine (1 correction bit per block, |= 1<<Al)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.Progressive.cs:234 DecodeAcRefine (history correction bits + newly-nonzero placement per G.1.2.3)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.Progressive.cs:319 ReconstructComponents (dequant + IDCT after all scans)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.Progressive.cs:14 EncodeProgressive (fixed DC first/refine + per-component AC first/refine script)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.Progressive.cs:80 WriteDcScanEntropy (DC point transform via >>al)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.Progressive.cs:129 WriteDcRefineScan (emits (coef>>al)&1)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.Progressive.cs:169 WriteAcScanEntropy (AC point transform via PointTransform)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.Progressive.cs:226 WriteAcRefineScan (buffered correction bits flushed after each ZRL/new coeff/EOB)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.Progressive.cs:303 PointTransform (toward-zero shift for AC)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.Progressive.cs:312 BuildProgressive12Tables + SeedProgressiveSymbols:340 (12-bit table completeness)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Coding/BlockScanCoder.cs:27 MagnitudeCategory / :185 Mantissa (shared magnitude coding)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:381 SetupGeometry (BlocksWide/High = MCU-padded; PlaneWidth padded) / :346 ComponentActualWidth (actual, used for non-interleaved AC block grid)`

## Test coverage

**Existing:**
- ProgressiveTests.cs::Progressive_UsesSof2Marker_AndIsIdentifiedAsProgressive - proves SOF2 marker emission + Identify.IsProgressive
- ProgressiveTests.cs::Progressive_Grayscale_DecodesIdenticallyToBaseline - proves multi-scan coefficient accumulation reconstructs identically to baseline (grayscale)
- ProgressiveTests.cs::Progressive_Rgb_DecodesIdenticallyToBaseline (444/420/422) - interleaved DC + per-component AC round-trip across subsampling
- ProgressiveTests.cs::Progressive_RoundTrips_CloseToOriginal - end-to-end fidelity
- ProgressiveTests.cs::Progressive_OddDimensions_Decode (1x1,7x3,17x15) - non-multiple-of-8/16 edge blocks (blocksPerLine/Col ceil geometry)
- ProgressiveTests.cs::Progressive_WithRestartInterval_RoundTrips - restart markers in DC/AC first+refine round-trip
- ProgressiveRefinementTests.cs::Progressive_Grayscale_MatchesBaseline_WithSuccessiveApproximation - DC refine (1-bit) + AC refine to Al=0 reconstructs baseline-identical
- ProgressiveRefinementTests.cs::Progressive_Rgb_MatchesBaseline_WithSuccessiveApproximation (444/420/422) - successive approximation across subsampling
- ProgressiveRefinementTests.cs::Progressive_NoisyImage_MatchesBaseline - many-nonzero AC + correction-bit ordering under random content
- ProgressiveRefinementTests.cs::Progressive_MatchesBaseline_AcrossQualities (50/75/95/100) - point-transform Al shift correctness across coefficient magnitudes
- ProgressiveRefinementTests.cs::Progressive_WithRestart_MatchesBaseline_WithSuccessiveApproximation - restart reset of predictors in refinement scans
- ProgressiveScanHeaderTests.cs::SpectralSelectionEndAboveMax_ThrowsCleanly - Se>63 rejected (JpegFormatException)
- ProgressiveScanHeaderTests.cs::SpectralSelectionStartAboveEnd_ThrowsCleanly - Ss>Se rejected
- ProgressiveScanHeaderTests.cs::DcScanWithNonZeroSe_ThrowsCleanly - DC scan (Ss=0) with Se!=0 rejected
- ProgressiveRobustnessTests.cs::ProgressiveAcScanWithMultipleComponents_Throws - AC scan with Ns>1 rejected (non-interleaved AC rule)
- ProgressiveRobustnessTests.cs::TruncatedProgressiveStream_NeverThrowsNonJpegException - truncation at every offset yields only JpegException
- ProgressiveRobustnessTests.cs::CorruptedProgressiveEntropy_NeverThrowsNonJpegException - single-bit flips in entropy yield only JpegException
- ProgressiveRobustnessTests.cs::CorruptedProgressiveDecode_Terminates - heavily corrupted multi-scan/EOB-run logic terminates (no infinite loop)
- EdgeCaseTests.cs::Progressive_TinyImages_AllSubsampling (444/422/420/411, 1x1..13x7) - tiny + 4:1:1 subsampled progressive round-trip vs baseline
- RealImageIntegrationTests.cs::RealImage_Progressive_MatchesBaseline (444/420) - progressive on real photographic content matches baseline
- HighPrecisionCodecTests.cs::Grayscale12_Progressive_RoundTrips - 12-bit progressive grayscale exercises BuildProgressive12Tables
- HighPrecisionCodecTests.cs::RgbYCbCr12_Progressive_RoundTrips - 12-bit progressive YCbCr 4:4:4
- HighPrecisionCodecTests.cs::Progressive12_Subsampled_RoundTrips - 12-bit progressive 4:2:0
- HighPrecisionCodecTests.cs::Cmyk12_Progressive_RoundTrips - 12-bit progressive 4-component
- HighPrecisionCodecTests.cs::Progressive12_IdentifyReportsProgressive - 12-bit SOF2 identification
- BlockCoderTests.cs::(ZRL-overrun test ~line 206-214) - proves BASELINE BlockScanCoder rejects run past index 63 with JpegCorruptException (adjacent; NOT the progressive DecodeAcFirst path)

**Gaps:**
- Non-interleaved (single-component) DC scan geometry: DecodeDcFirst/DecodeDcRefine's block-raster traversal for a single subsampled component (c.H<hmax or c.V<vmax) is never exercised - the encoder only emits interleaved DC scans, so this spec-legal (T.81 B.2.3/G.1.2) stream shape has zero coverage and is known-misdecoded.
- EOBRUN batching (EOBn, run>1) across multiple all-zero end-of-band blocks: decoder implements EOBRUN (first + refine) but the encoder always emits EOB0 (run of 1), so no round-trip proves EOBRUN>1 decode; needs a crafted or interop stream.
- EOBRUN reset at restart markers (RSTn): decoder clears eobRun on restart but this path is never reached because the encoder never produces cross-block EOB runs; restart round-trip tests don't cover it.
- AC-first over-long ZRL/run rejection in the PROGRESSIVE path: DecodeAcFirst's 'k += 16' ZRL has no bounds check and an over-long run is silently truncated by the loop guard rather than raising JpegCorruptException; only the baseline BlockScanCoder path has a targeted test.
- AC-refine size!=1 rejection: DecodeAcRefine accepts any nonzero size nibble as newly-nonzero without checking s==1; no malformed-stream test asserts rejection.
- DC magnitude category 16 overflow: DecodeDcFirst permits categories up to 16 and casts predictors<<Al to short; no test forces category 16 to prove range-check/rejection (should be <=15).
- Interop / bitstream compliance (cross-decode): there are NO tests that decode a progressive stream produced by libjpeg/ImageMagick, nor that verify a JpegSharp progressive stream decodes in an external decoder; all coverage is self-round-trip, so external bitstream conformance is unproven.
- Multi-stage successive approximation (e.g. Al 3->2->1->0) and split AC spectral bands (e.g. 1..5, 6..63): decoder claims support for arbitrary Ss/Se/Ah/Al but the encoder emits only a single fixed script, so these decode paths have no crafted/interop test.
- 12-bit progressive reaching high categories: existing 12-bit progressive tests use smooth gradients that likely stay in low DC/AC categories; the BuildProgressive12Tables completeness seeding for DC categories 12-15 and AC sizes >10 is not proven with content that actually emits those symbols.

**Required new tests:**
- `NonInterleavedDcScan_SubsampledComponent_DecodesCorrectly` (interop, T.81 B.2.3 (MCU/data-unit order), G.1.2): Decode a 4:2:0 progressive stream whose DC scans are non-interleaved single-component scans (as libjpeg cjpeg -scans emits) and confirm the component block-raster geometry is honored for subsampled components; repros/guards the major non-interleaved DC geometry gap. → Decoded pixels match the reference decoder output within JPEG tolerance; today this path misdecodes subsampled single-component DC scans so the test currently fails, pinning the defect.
- `Progressive_CrossDecode_LibjpegAndImageMagick_AllSubsampling` (interop, T.81 G.1): Round-trip progressive bitstream compliance: decode libjpeg/ImageMagick-produced progressive JPEGs (4:4:4, 4:2:2, 4:2:0) and non-multiple-of-16 dims (17x9) with JpegSharp, and decode JpegSharp progressive output with the external tool, proving external conformance and edge-block handling. → Both directions reconstruct matching pixels within tolerance for every subsampling and dimension; edge blocks (17x9) align.
- `EobRun_AcrossManyZeroBands_AndRestartReset_Decodes` (decoder, T.81 G.1.2.2): Feed a crafted (or libjpeg) AC-first scan that uses EOBn with a run spanning many all-zero end-of-band blocks and crosses an RSTn boundary; verify the decoder consumes the run, resets EOBRUN at the restart, and reconstructs identical coefficients. → Reconstructed coefficients equal the intended values; EOBRUN is reset at RSTn (blocks after the marker are decoded, not skipped by a stale run).
- `AcRefine_ManyNonzeroWithLongZrlRuns_RoundTripsExactly` (round-trip, T.81 G.1.2.3): Encode->decode blocks with many already-nonzero coefficients separated by 15+ zeros plus long trailing zero runs, forcing multi-ZRL while-loops with buffered correction bits, to guard the correction-bit ordering invariant in refinement. → Decoded coefficients/pixels reproduce the input exactly (correction bits applied in the same order they were buffered).
- `Progressive_MultiStageSuccessiveApproximation_And_SplitAcBands_Decode` (decoder, T.81 G.1.1.1.1 / G.1.2): Decode a crafted/libjpeg stream using multi-stage DC/AC successive approximation (Al 3->2->1->0) and split AC spectral bands (e.g. 1..5 then 6..63) to exercise the decoder's arbitrary-Ss/Se/Ah/Al support that the fixed encoder script never produces. → Multi-stage refinement and banded AC scans accumulate into the coefficient array yielding pixels matching a reference decode within tolerance.
- `MalformedProgressive_OverlongAcFirstRunOrZrl_Throws` (decoder, T.81 G.1.2.2 / F.1.2.2): Hand-build an AC-first entropy segment with a run/ZRL advancing k past Se and assert the progressive decoder raises JpegCorruptException instead of silently truncating, matching baseline BlockScanCoder behavior. → Jpeg.Decode throws JpegCorruptException (currently silently truncated - test fails, pinning the gap).
- `MalformedProgressive_AcRefineSizeNotOne_Throws` (decoder, T.81 G.1.2.3): Craft an AC-refine scan whose run/size symbol has size nibble != 1 and assert rejection, since a newly-nonzero refinement coefficient must have size exactly 1. → Jpeg.Decode throws JpegCorruptException/JpegFormatException (currently accepted - test fails, pinning the gap).
- `MalformedProgressive_DcCategory16_Throws` (decoder, T.81 Table F.1 / G.1.2.1): Craft a DC-first scan that decodes a Huffman symbol of category 16 and assert rejection/range-check, since the DC category must be <=15 and category 16 overflows the short coefficient store. → Jpeg.Decode throws JpegFormatException/JpegCorruptException (currently accepted up to 16 with silent short overflow - test fails, pinning the gap).
- `Progressive12_HighCategoryContent_RoundTrips` (round-trip, T.81 G.1 (12-bit precision)): 12-bit progressive round-trip on high-contrast/noisy content so DC-first diffs reach categories 12-15 and AC-first sizes exceed 10, exercising BuildProgressive12Tables completeness seeding beyond the smooth-gradient cases. → Encode succeeds with complete Huffman tables for the emitted high-category symbols and decode reproduces samples within 12-bit tolerance (no missing-code failure).

