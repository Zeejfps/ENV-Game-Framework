# Encoder FDCT + quantization + zig-zag ordering

**Section key:** `enc-fdct-quant`  
**Compliance:** PARTIAL · **Adversarial justified:** n/a · **Final:** **PARTIAL**

## Findings

- **[major]** Quality scaling must clamp elements to [1,255] for 8-bit OR [1,65535] for 16-bit depending on output precision.
  - Observed: StandardQuantizationTables.ScaleValue clamps unconditionally to [1,255] regardless of sample precision. When encoding 12-bit images (supported via the JpegImage16 constructor, BaselineEncoder.cs:128-189), quality-scaled quantization tables are still hard-capped at 255 instead of the 16-bit [1,65535] range. The precision-dependent branch of the requirement is not implemented; the 16-bit DQT write path (BaselineEncoder.cs:456-475) is only reachable via custom user tables, never via quality scaling. Result: at low quality on 12-bit output, tables are coarser-capped than the spec allows (still decodable, but non-conformant to the stated scaling behavior).
  - Spec: T.81 Annex K.1 scaling; B.2.4.1 (Pq/Qk element precision)
- **[minor]** Divide each coeff by quant element with round-to-nearest, producing a representable coefficient.
  - Observed: Quantizer.Quantize (Quantizer.cs:24-28) casts the rounded quotient directly to short with no range clamp. For 12-bit samples the orthonormal DCT max coefficient magnitude is 0.25*64*2048 = 32768, which exceeds short.MaxValue (32767); with a quant step of 1 (quality 100) a full-amplitude 12-bit checkerboard block overflows to -32768, corrupting the coefficient (also out of DC/AC category 15 range). 8-bit is unaffected (max magnitude ~2048).
  - Spec: T.81 A.3.2, F.1.1 (coefficient/category ranges)
- **[info]** No quality-scaled element may become 0.
  - Observed: Verified satisfied: ScaleValue clamps values <1 to 1 (StandardQuantizationTables.cs:62-63) and the QuantizationTable constructor throws on any zero divisor (QuantizationTable.cs:32-33). Quality=100 yields scale=0 -> all-ones table, never zero.
  - Spec: T.81 Annex K.1

## Required fixes

- Make quantization-table quality scaling precision-aware: clamp the scaled value to [1,255] when writing an 8-bit (Pq=0) table but to [1,65535] when the sample precision is >8 (12-bit). Thread the target precision into StandardQuantizationTables.ScaleValue / QuantizationTable.Scaled (currently ScaleValue always clamps to 255, StandardQuantizationTables.cs:64-65), so 12-bit low-quality tables can use the full 16-bit range as the spec permits.
- In Quantizer.Quantize (Quantizer.cs:27), clamp the rounded quotient to the representable coefficient range before the short cast (e.g. Math.Clamp to short.MinValue/MaxValue, or to the JPEG category limit for the active precision) so a full-amplitude 12-bit block cannot overflow to a wrong value.

## Code locations

- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.cs:306-336 (ExtractBlock, level shift: 317 subtracts 128, 323/332 subtract 1<<(precision-1))`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.cs:291-294 (per-block ExtractBlock -> FastDct.Forward -> Quantizer.Quantize -> ZigZag.FromNatural pipeline)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Transforms/FastDct.cs:39-65 (Forward 2D DCT), 127-131 (Basis scaling)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Quantization/Quantizer.cs:18-29 (Quantize: double divide + Math.Round AwayFromZero, unchecked short cast at 27)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Transforms/ZigZag.cs:17-27 (OrderTable), 49-57 (FromNatural)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Quantization/StandardQuantizationTables.cs:42-50 (QualityToScale), 59-67 (ScaleValue: hard clamp [1,255])`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Quantization/QuantizationTable.cs:99-106 (Scaled), 32-33 (non-zero guard)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.cs:447-484 (WriteQuantTables 8/16-bit DQT selection)`

## Test coverage

**Existing:**
- FastDctTests.cs::FastForward_MatchesReference — proves FastDct.Forward (the encoder FDCT) matches the reference Dct oracle within tol 1e-6 across 50 random blocks (satisfies suggested test #5's oracle-equivalence half), but only over 8-bit-range inputs (-128..128)
- FastDctTests.cs::FastForward_FlatBlock_ProducesOnlyDc — DC term = 8*sample and all AC = 0 for a constant block (satisfies the DC=mean*8 half of suggested test #5)
- DctTests.cs::Forward_ConstantBlock_ProducesOnlyDc — orthonormal DCT-II DC = 8*sampleValue, AC = 0 for a flat block
- DctTests.cs::ForwardThenInverse_RoundTrips / Transform_IsEnergyPreserving / Forward_IsLinear / Forward_SingleVerticalRamp_MatchesReference — establish the reference DCT oracle's correctness that FastDct is validated against
- QuantizationTests.cs::Quantize_DividesAndRoundsToNearest — divide-by-table with round-to-nearest ties-away-from-zero: 10.4->10, 10.5->11, -10.5->-11, -10.4->-10 (fully satisfies suggested test #4)
- QuantizationTests.cs::QuantizationValues_AreClampedToByteRange — Luminance(1) all elements InRange[1,255] (8-bit lower+upper clamp, luminance only)
- QuantizationTests.cs::Quality100_ProducesAllOnes — Luminance(100) is all-ones, never zero (satisfies the info finding for luminance)
- QuantizationTests.cs::Quality_IsClamped / StandardLuminance_Quality50_EqualsBaseTable / StandardChrominance_Quality50_EqualsBaseTable / LowerQuality_ProducesLargerOrEqualQuantizationSteps — Annex K.1 scaling monotonicity and base-table anchors
- QuantizationTests.cs::CustomTable_ZeroValue_Throws / QuantizationTable.cs constructor — no zero divisor permitted (info finding)
- QuantizationTests.cs::QuantizeThenDequantize_ApproximatesOriginal / Dequantize_MultipliesCoefficientsByTable — quantize/dequantize inverse relationship within half a step
- ZigZagTests.cs::Order_MatchesSpecSequence — ZigZag.Order equals the exact T.81 Fig A.6 sequence (encoder reorder-to-zig-zag correctness)
- ZigZagTests.cs::FromNatural_PlacesDcFirst / ToNatural_ScattersAccordingToOrder / InverseOrder_IsInverseOfOrder / Dezigzag_ThenZigzag_RoundTrips / Order_StartsAtDcAndIsAPermutation — full zig-zag permutation coverage
- QuantizationTests.cs::ZigZagRoundTrip_PreservesTable + CustomTableTests.cs::CustomQuantTable_IsWrittenToDqt — table values are written into the DQT segment in zig-zag order
- HighPrecisionQuantTests.cs::QuantValueAbove255_IsWrittenAs16BitAndRoundTrips — the 16-bit DQT (Pq=1) write path emits true values and round-trips, but is reached ONLY via a custom table (values[0]=400), never via quality scaling
- HighPrecisionQuantTests.cs::EightBitQuantTable_StillUses8BitPrecision — standard 8-bit quality table emits Pq=0 with a 65-byte DQT
- HighPrecisionCodecTests.cs::Grayscale12_RoundTrips / RgbDirect12_RoundTrips / Cmyk12_RoundTrips (and progressive/subsampled variants) — indirectly prove the encoder level shift by 2^(P-1) for 12-bit input is correct (a wrong center would offset the reconstruction), at quality=100 only
- BaselineRoundTripTests.cs (grayscale/RGB round trips) — indirectly prove the 8-bit level shift by -128

**Gaps:**
- MAJOR compliance defect, zero coverage: quality-scaled quantization tables never use the 16-bit [1,65535] range for 12-bit output. StandardQuantizationTables.ScaleValue hard-caps at 255 unconditionally, and the DQT writer (BaselineEncoder.cs:456-475) only emits Pq=1 when an element exceeds 255, so a quality-scaled 12-bit encode can never reach the 16-bit path. No test encodes a 12-bit image at low quality and asserts Pq=1 or any element > 255.
- MINOR defect, zero coverage: Quantizer.Quantize (Quantizer.cs:24-28) casts the rounded quotient directly to short with no range clamp. A full-amplitude 12-bit block (max coeff magnitude 0.25*64*2048 = 32768) with a quant step of 1 (quality 100) overflows short.MaxValue and wraps to -32768, corrupting the coefficient and exceeding DC/AC category 15. No test exercises 12-bit coefficient magnitudes near/over the short range.
- No parametrized sweep across quality 1..100 for the CHROMINANCE base table asserting every scaled element is in [1,255] and non-zero; chrominance is only checked at quality=50. Luminance is checked only at q=1 and q=100, not swept.
- FDCT oracle-equivalence (FastForward_MatchesReference) is exercised only over 8-bit-range inputs (-128..128); no coverage of 12-bit-range level-shifted inputs (~ +/-2048) where fast-DCT accumulation and downstream short-range behavior differ.
- No DIRECT assertion of the encoder level shift by 2^(P-1) / -128 (ExtractBlock, BaselineEncoder.cs:308-335): correctness is only inferred from full round-trips at quality=100; nothing asserts the level-shift center itself (e.g. DC of a constant block == (sample - 2^(P-1))*8).

**Required new tests:**
- `QualityScaled12BitTable_UsesSixteenBitRange` (encoder, T.81 Annex K.1 scaling; B.2.4.1 (Pq/Qk element precision)): Encode a 12-bit image (JpegImage16, precision 12) at quality=1 via Jpeg.Encode16 with a standard quality table and assert the emitted DQT declares Pq=1 (0x10) and at least one quantization element exceeds 255, proving the 16-bit scaling range is actually used for 12-bit output. Directly exercises the MAJOR compliance defect; expected to FAIL against current ScaleValue which caps at 255. → DQT segment Pq nibble == 1 and max scaled element > 255; test currently fails (all elements capped at 255, Pq=0) until precision-dependent clamping is implemented
- `QualityScale_AllQualities_BothTables_InRangeNonZero` (encoder, T.81 Annex K.1 scaling): Parametrized over quality 1..100 for both Luminance and Chrominance 8-bit tables: assert every scaled element is in [1,255], no element is 0, and quality=100 yields an all-ones table. Closes the chrominance/full-sweep gap in the existing luminance-only checks. → All 64 elements InRange[1,255] for every quality and both tables; quality=100 -> all ones; no zero element
- `Quantize_12BitMaxAmplitudeBlock_NoShortOverflow` (encoder, T.81 A.3.2, F.1.1 (coefficient/category ranges)): Run FastDct.Forward on a synthetic maximal-alternating 12-bit checkerboard block (level-shifted +2047/-2048) then Quantizer.Quantize with an all-ones table, and assert every resulting coefficient stays within short range and within JPEG category-15 bounds (|coeff| <= 32767), i.e. no wrap to -32768. Exercises the MINOR overflow defect. → No coefficient equals -32768 / all within [-32767,32767]; test currently fails (DC term overflows to -32768) until Quantize clamps the range
- `FastForward_MatchesReference_TwelveBitRange` (encoder, T.81 A.3.2 (FDCT)): Extend the FDCT oracle-equivalence test to 12-bit level-shifted input magnitudes (random values in ~[-2048,2047]) and assert FastDct.Forward matches the Dct reference within floating tolerance, and that a constant 12-bit-centered block yields DC == mean(level-shifted samples)*8. → Fast vs reference agree to ~1e-6 across random 12-bit-range blocks; constant-block DC == (sample-2^(P-1))*8
- `Encoder_LevelShift_ConstantBlock_DcMatchesShiftedMean` (round-trip, T.81 A.3.2 (level shift by -2^(P-1))): Directly verify the encoder level shift: encode a flat image at both 8-bit (center 128) and 12-bit (center 2048) with an all-ones quant table, decode the DC coefficient (or dequantized DC), and assert it equals (sampleValue - 2^(P-1))*8 within rounding, confirming the -2^(P-1) shift rather than a hard-coded 128. → Reconstructed/inspected DC corresponds to the precision-correct center (128 for 8-bit, 2048 for 12-bit); flat image reconstructs to the original sample value

