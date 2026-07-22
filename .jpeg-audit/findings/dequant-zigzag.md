# Dequantization + zig-zag reordering

**Section key:** `dequant-zigzag`  
**Compliance:** PASS · **Adversarial justified:** true · **Final:** **PASS**

## Findings

- **[info]** F.2.1.5: each decoded coefficient Sq(k) multiplied by corresponding quantization element Q(k).
  - Observed: Quantizer.Dequantize (Quantizer.cs:38-46) computes output[i] = quantized[i] * (double)table[i] element-wise. The quant table supplied is natural (raster) order (QuantizationTable stores natural order via FromZigZag, QuantizationTable.cs:71-81; AsSpan returns natural, line 44), and the coefficients passed in are ALSO already de-zig-zagged to natural order before the multiply. Because the code dezigzags first (ZigZag.ToNatural) and then multiplies in natural order, the operand order differs from the literal spec formula (spec dequantizes in zig-zag order then reorders). This is a valid, mathematically identical equivalent since the quant-table permutation matches the coefficient permutation; result is correct. No defect.
  - Spec: ITU-T T.81 A.3.4 / F.2.1.5
- **[info]** Zig-zag sequence mapped back to the 8x8 raster grid using the Annex A.6 order before IDCT.
  - Observed: ZigZag.OrderTable (ZigZag.cs:17-27) matches the standard T.81 Figure A.6 zig-zag sequence exactly (verified all 64 entries). ZigZag.ToNatural (ZigZag.cs:65-73) scatters natural[order[k]] = zigzag[k], and InverseTable is derived consistently (BuildInverse, lines 75-81). In both decode paths the reorder happens before FastDct.Inverse (BaselineDecoder.cs:440-442; BaselineDecoder.Progressive.cs:337-339). Correct.
  - Spec: ITU-T T.81 Annex A.6 / F.2.1.5
- **[info]** Robustness: all 64 coefficients must be well-defined before reorder/dequant (no stale data on EOB truncation).
  - Observed: The zig-zag scratch buffer is reused across blocks (stackalloc once at BaselineDecoder.cs:410) but BlockScanCoder.DecodeBlock calls block.Clear() at BlockScanCoder.cs:93 before decoding, so coefficients past EOB are guaranteed zero. No leakage into ToNatural/Dequantize. Correct.
  - Spec: ITU-T T.81 F.1.2.2
- **[minor]** Quantization-table precision constraints (DQT Pq field).
  - Observed: ParseQuantTables (BaselineDecoder.cs:221-260) accepts 16-bit quant tables (Pq=1) regardless of frame sample precision. T.81 B.2.4.1 restricts Pq=1 to 12-bit frames and requires Pq=0 for baseline/8-bit. The decoder is lenient here. This does not affect dequant math correctness (values are validated non-zero and multiplied as ushort), so it is a conformance-strictness note rather than a decode defect; flagged for completeness as it lives adjacent to this section's quant-table setup.
  - Spec: ITU-T T.81 B.2.4.1

## Adversarial — overlooked violations

- **[minor]** ParseQuantTables (BaselineDecoder.cs:233) branches on `precision == 0` vs. else, so ANY nonzero Pq nibble (2..15) is silently treated as a valid 16-bit table and 128 bytes are consumed. T.81 B.2.4.1 restricts Pq to 0 or 1; Pq>=2 is malformed and should be rejected. The reviewer's minor note only covered the Pq=1-on-8-bit-frame leniency, not acceptance of the reserved/invalid Pq>=2 encoding. This is a wrongly-accepted invalid stream, and because Pq drives the byte-stride of the table read, a corrupt Pq desyncs subsequent DQT sub-tables within the same segment. (ITU-T T.81 B.2.4.1)
- **[minor]** Progressive DC-first stores the accumulated coefficient with no range guard: DecodeBlock (baseline) validates `dcValue in [short.MinValue, short.MaxValue]` (BlockScanCoder.cs:100-101), but DecodeDcFirst does `buffer[offset] = (short)(predictors[si] << scan.Al)` (BaselineDecoder.Progressive.cs:126) with no equivalent check. A corrupt/hostile progressive stream can drive the running DC predictor (or the <<Al shift) past 16 bits, which wraps silently and feeds a wrong Sq(0,0) straight into Quantizer.Dequantize, corrupting the reconstructed block rather than raising JpegCorruptException. The dequant stage assumes a well-formed coefficient but nothing upstream enforces it on this path. (ITU-T T.81 F.2.1.5)
- **[info]** In the progressive pipeline the quantization table applied at ReconstructComponents (BaselineDecoder.Progressive.cs:329, GetQuantTable(c.QuantId)) is whatever occupies that table slot AFTER all scans complete, since DQT segments are permitted and re-parsed between scans (lines 35-37). If a stream redefines the slot mid-stream, dequant uses the last definition for every block of the component rather than the table in effect when each scan was coded. Valid encoders don't do this, but it is an unstated assumption the reviewer's finding #1 didn't address. (ITU-T T.81 A.3.4)

## Counterexamples

- DQT with reserved precision: segment bytes `FF DB 00 84 20 <128 bytes>` (Pq=2, Tq=0). Spec-invalid per B.2.4.1, but ParseQuantTables takes the `else` (16-bit) branch, reads 128 bytes, and builds a table instead of rejecting. A conformant decoder should throw.
- Progressive DC-first overflow: a SOF2 stream whose first DC scan (Ss=0,Ah=0,Al=0) feeds DC differentials that accumulate `predictors[si]` beyond +32767 (e.g. repeated large positive category-15 diffs across a row of blocks). Line 126 casts to short and wraps to a large negative DC; ReconstructComponents then dequantizes the wrapped value, producing a grossly wrong block with no error, whereas the baseline path would throw JpegCorruptException for the same magnitude.
- Because dequant is correct only given a well-formed coefficient, the above two are the only ways to make the section produce a wrong result: both are input-validation gaps upstream of the (correct) multiply, not errors in the multiply/reorder itself.

## Code locations

- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Quantization/Quantizer.cs:38-46 (Quantizer.Dequantize)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Transforms/ZigZag.cs:17-27 (OrderTable, Annex A.6 sequence)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Transforms/ZigZag.cs:65-73 (ZigZag.ToNatural)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Transforms/ZigZag.cs:75-81 (BuildInverse)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:439-443 (baseline decode pipeline: DecodeBlock -> ToNatural -> Dequantize -> IDCT)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.Progressive.cs:337-339 (progressive reconstruct pipeline)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Quantization/QuantizationTable.cs:44 (AsSpan returns natural order)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Quantization/QuantizationTable.cs:71-81 (FromZigZag de-zigzags DQT to natural order)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Coding/BlockScanCoder.cs:93 (block.Clear() guarantees full 64-entry block)`

## Test coverage

**Existing:**
- JpegSharp.Tests/ZigZagTests.cs::Order_MatchesSpecSequence — proves ZigZag.Order equals the canonical T.81 Figure A.6 sequence element-for-element (Annex A.6).
- JpegSharp.Tests/ZigZagTests.cs::Order_StartsAtDcAndIsAPermutation — proves Order has 64 entries, starts at DC (0), and is a bijective permutation of 0..63.
- JpegSharp.Tests/ZigZagTests.cs::InverseOrder_IsInverseOfOrder — proves InverseOrder is a true two-way inverse of Order (InverseOrder[Order[k]]==k and Order[InverseOrder[k]]==k for all k).
- JpegSharp.Tests/ZigZagTests.cs::Dezigzag_ThenZigzag_RoundTrips — proves ToNatural(FromNatural(x))==x on a full 0..63 ramp (round-trip permutation identity).
- JpegSharp.Tests/ZigZagTests.cs::ToNatural_ScattersAccordingToOrder + FromNatural_PlacesDcFirst — prove the scatter maps zig-zag position k to natural index Order[k] (e.g. zig[2]->natural[8]) and DC lands first.
- JpegSharp.Tests/QuantizationTests.cs::Dequantize_MultipliesCoefficientsByTable — proves Quantizer.Dequantize computes output[i]=quantized[i]*(double)table[i] element-wise (F.2.1.5 multiply), though only with a natural-order table fed directly, small values.
- JpegSharp.Tests/QuantizationTests.cs::ZigZagRoundTrip_PreservesTable + CustomTable_PreservesValuesInNaturalOrder — prove QuantizationTable stores/returns natural order and survives CopyToZigZag/FromZigZag (the natural<->zigzag table permutation).
- JpegSharp.Tests/QuantizationTests.cs::QuantizeThenDequantize_ApproximatesOriginal — proves dequant reconstruction error is bounded by half a quant step (dequant vs quant consistency).
- JpegSharp.Tests/DqtVariantTests.cs::SixteenBitQuantTable_DecodesIdentically — proves a rewritten Pq=1 16-bit DQT decodes to identical pixels as the 8-bit encoding (16-bit dequant path parity).
- JpegSharp.Tests/DqtVariantTests.cs::TruncatedSixteenBitDqt_ThrowsFormatException — proves a Pq=1 DQT missing the second half is rejected (stride/bounds guard for the declared 16-bit width).
- JpegSharp.Tests/DqtVariantTests.cs::MultipleQuantTablesInOneSegment_Decode — proves multiple sub-tables merged into one DQT segment parse correctly (per-subtable stride advance).
- JpegSharp.Tests/HighPrecisionQuantTests.cs::QuantValueAbove255_IsWrittenAs16BitAndRoundTrips — exercises the >255 (16-bit) quant value through encode+decode dequant and asserts consistent reconstruction (guards the old 400->144 truncation).
- JpegSharp.Tests/CustomTableTests.cs::CustomQuantTable_IsWrittenToDqt — proves a custom natural-order table is emitted to the DQT in zig-zag order (table.CopyToZigZag == DQT bytes).
- JpegSharp.Tests/OptionInteractionTests.cs::CustomQuant_WithProgressive_RoundTrips — proves baseline and progressive decodes of the same image with a custom quant table yield identical pixels (progressive/baseline share dequant+reorder).
- JpegSharp.Tests/HighPrecisionCodecTests.cs::Grayscale12_RoundTrips / RgbYCbCr12_* / Grayscale12_Progressive_RoundTrips / Progressive12_Subsampled_RoundTrips — exercise the 12-bit (Pq=1 16-bit quant) path through Dequantize in both baseline and progressive, asserting self-consistent round-trip.

**Gaps:**
- Operand-order alignment across ALL 64 positions through the decode path: no test builds a DQT with 64 distinct zig-zag step values and a block of known zig-zag coefficients and asserts each raster cell equals the correctly de-zigzagged Q(v,u)*Sq. The existing Dequantize test feeds a natural-order table directly and never proves the DQT-zigzag-byte <-> coefficient-natural-position relationship for AC terms (only DC precision is checked in HighPrecisionQuantTests).
- Reserved/invalid Pq>=2 rejection: BaselineDecoder.cs:233 branches precision==0 vs else, so any Pq in 2..15 is silently treated as a valid 16-bit table and 128 bytes consumed. T.81 B.2.4.1 restricts Pq to {0,1}. No test rejects Pq>=2 (adversarial finding #1).
- Pq=1 (16-bit) DQT on a baseline 8-bit SOF0 frame: T.81 B.2.4.1 requires Pq=0 for 8-bit frames; the decoder accepts Pq=1 regardless of frame precision. No test asserts this conformance restriction (reviewer minor + adversarial finding).
- Progressive DC-first coefficient range guard: DecodeDcFirst (BaselineDecoder.Progressive.cs:126) stores (short)(predictors[si] << scan.Al) with no [short.Min,short.Max] check, unlike baseline DecodeBlock. No test drives the running DC predictor / Al shift past 16 bits to assert a corrupt-data exception (adversarial finding #2).
- EOB stale-data / block.Clear() contract: no test proves trailing coefficients past an early EOB are zero when the zig-zag scratch buffer is reused across blocks (BlockScanCoder.cs:93 clear before ToNatural/Dequantize).
- Dequant overflow/precision: no test feeds quantized=short.MaxValue (32767) with a 16-bit step (e.g. 65535) to assert Dequantize yields exactly 32767*65535 with no truncation (double accumulation). Existing test uses small in-range products.
- Reference/interop verification of the 16-bit quant path: existing 12-bit/16-bit coverage is self-consistent round-trip only; no golden or third-party reference decoder confirms the dequantized output of a Pq=1 stream.

**Required new tests:**
- `Dequant_ZigZagAlignment_AllPositionsMatchDeZigzaggedTable` (decoder, ITU-T T.81 A.3.4 / F.2.1.5 / Annex A.6): Build a DQT with 64 DISTINCT step values in zig-zag order and a decoded block whose zig-zag coefficients equal 1 (or index k); after ToNatural + Dequantize assert every raster cell natural[Order[k]] == coeff(k) * table.AsSpan()[Order[k]], proving the quant-table permutation and coefficient permutation are aligned for all 64 positions, not just DC. → For every k in 0..63, the raster output at natural index Order[k] equals the coefficient times the correctly de-zigzagged quant step; no off-by-permutation mismatch on any AC position.
- `ParseQuantTables_PqGreaterThanOne_ThrowsFormatException` (decoder, ITU-T T.81 B.2.4.1): Feed a DQT whose pqTq byte has Pq=2 (0x20) and assert the decoder rejects it instead of silently treating it as a 16-bit table and consuming 128 bytes (which also desyncs later sub-tables in the same segment). → JpegFormatException is thrown; currently the reserved Pq>=2 value is wrongly accepted as 16-bit.
- `ParseQuantTables_Pq1On8BitBaselineFrame_ThrowsFormatException` (decoder, ITU-T T.81 B.2.4.1): Feed a baseline 8-bit SOF0 stream containing a Pq=1 (16-bit) DQT and assert rejection, since B.2.4.1 requires Pq=0 for 8-bit sample precision. → JpegFormatException (conformance rejection); currently the 16-bit table is accepted on an 8-bit frame.
- `ProgressiveDcFirst_DcCoefficientOverflow_ThrowsCorruptException` (decoder, ITU-T T.81 F.2.1.5): Craft a progressive DC-first scan whose accumulated DC predictor (or <<Al shift) drives the stored coefficient past the signed-16-bit range and assert a corrupt-data exception, matching the baseline DecodeBlock range check that the progressive path lacks. → JpegCorruptException (a JpegFormatException) is raised; currently the value wraps silently and feeds a wrong Sq(0,0) into Dequantize.
- `Dequant_EobTruncation_TrailingCoefficientsAreZero` (decoder, ITU-T T.81 F.1.2.2): Decode two consecutive blocks reusing the same zig-zag scratch buffer where the second block hits EOB early after a coefficient-rich first block; assert the second block's trailing (post-EOB) natural coefficients are zero, guarding the block.Clear() contract before ToNatural/Dequantize. → All coefficients beyond the EOB position are exactly zero with no leakage from the previous block.
- `Dequantize_MaxCoefficientTimesMaxStep_NoTruncation` (decoder, ITU-T T.81 F.2.1.5): Call Quantizer.Dequantize with quantized=short.MaxValue (32767) and a 16-bit quant step of 65535 and assert the output equals exactly 32767*65535 as a double, validating full-precision accumulation rather than 16/32-bit truncation. → output[i] == 32767.0 * 65535.0 exactly (2,147,385,345), no overflow or truncation.
- `SixteenBitQuant_MatchesReferenceDecoder_Golden` (interop, ITU-T T.81 A.3.4 / B.2.4.1): Decode a stored 12-bit / Pq=1 extended-sequential golden JPEG (produced by or cross-checked against a reference decoder) and assert the dequantized+reordered reconstruction matches the reference output, giving interop confirmation of the 16-bit quant path beyond self-consistent round-trips. → Reconstructed pixels/coefficients match the reference decoder within the documented golden tolerance.
- `ZigZag_ToNatural_FromNatural_FullRampInverseRegression` (regression, ITU-T T.81 Annex A.6): Pin ToNatural(FromNatural(ramp))==ramp and Order/InverseOrder inverse relationship as an explicit regression guard so any future edit to the permutation tables is caught (extends existing round-trip with a locked-in expectation). → Full 0..63 ramp survives the round trip unchanged and InverseOrder remains the exact inverse of Order.

