# DQT quantization-table segment parsing

**Section key:** `dqt`  
**Compliance:** PARTIAL · **Adversarial justified:** n/a · **Final:** **PARTIAL**

## Findings

- **[major]** Decoder must reject Pq (precision nibble) not in {0,1}.
  - Observed: ParseQuantTables (BaselineDecoder.cs:227-233) computes `precision = pqTq >> 4` then branches `if (precision == 0) { 8-bit } else { 16-bit }`. Any Pq value of 2..15 is silently treated as 16-bit and 128 bytes are consumed, rather than being rejected. There is no validation that precision is in {0,1}. This violates the explicit T.81 B.2.4.1 requirement and allows malformed DQT segments to be misinterpreted.
  - Spec: T.81 B.2.4.1 (Pq = 0 -> 8-bit, Pq = 1 -> 16-bit; reject Pq not in {0,1})
- **[minor]** Pq (16-bit) precision must be consistent with frame process (an 8-bit DCT process shall not use a 16-bit quantization table).
  - Observed: ParseQuantTables accepts Pq=1 (16-bit) tables unconditionally and never cross-checks against the frame sample precision. T.81 B.2.4.1 note states an 8-bit DCT-based process shall not use 16-bit precision quantization tables; the decoder performs no such check (and DQT may legitimately precede SOF, so the check would need deferral).
  - Spec: T.81 B.2.4.1 note on 16-bit tables vs 8-bit processes
- **[info]** Length must be consistent with element count.
  - Observed: Element-count consistency is enforced only for truncation: `p + 64 > segment.Length` (line 235) and `p + 128 > segment.Length` (line 242) throw 'Truncated...'. A DQT whose declared length leaves a partial (non-65/non-129-byte) trailing block is rejected because the next loop iteration reads a header and then fails the truncation check. Full extra blocks are (correctly) treated as additional tables. Detection is therefore only indirect via truncation; there is no explicit end-of-segment alignment check, but no incorrect acceptance was found.
  - Spec: T.81 B.2.4.1 (segment length must match sum of table lengths)
- **[info]** Zig-zag ordering of the 64 elements.
  - Observed: Elements are read sequentially into `zig[0..63]` then converted via QuantizationTable.FromZigZag (QuantizationTable.cs:71-81), which applies `natural[ZigZag.Order[k]] = valuesZigZag[k]` using the standard T.81 Annex A Figure A.6 order (ZigZag.cs:17-27). Correct.
  - Spec: T.81 B.2.4.1 / Annex A Figure A.6 (zig-zag order)
- **[info]** Tq must be 0..3.
  - Observed: id = pqTq & 0x0F is validated by `if (id >= _quantTables.Length)` where _quantTables has length 4 (BaselineDecoder.cs:36, 230-231), correctly rejecting Tq 4..15. Correct.
  - Spec: T.81 B.2.4.1 (Tq in 0..3)
- **[info]** Handling of zero quantization values (spec assumption).
  - Observed: QuantizationTable constructor (QuantizationTable.cs:32-33) throws ArgumentException on any zero element, propagated as JpegFormatException 'Invalid quantization table' (BaselineDecoder.cs:255-258). T.81 does not explicitly forbid a zero quantization step in DQT; rejecting is a safe/stricter-than-spec assumption (avoids divide-by-zero in dequant) but could reject a technically-parseable stream.
  - Spec: T.81 B.2.4.1 (no explicit non-zero constraint on Qk)

## Required fixes

- In ParseQuantTables (BaselineDecoder.cs ~line 228-233), explicitly validate the precision nibble: `if (precision > 1) throw new JpegFormatException($"Invalid quantization table precision {precision}.");` before branching on 8- vs 16-bit, so Pq in {2..15} is rejected instead of silently treated as 16-bit.
- Optionally defer and enforce the T.81 constraint that a 16-bit (Pq=1) quantization table is not used with an 8-bit baseline/sequential frame process, once frame precision is known.

## Code locations

- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:221-260 (ParseQuantTables)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:227-233 (Pq/Tq extraction + precision branch)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:235-249 (8/16-bit element read + truncation checks)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:251-258 (FromZigZag construction + error wrap)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Quantization/QuantizationTable.cs:24-36 (ctor zero/size validation)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Quantization/QuantizationTable.cs:71-81 (FromZigZag)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Transforms/ZigZag.cs:17-35 (Order table)`

## Test coverage

**Existing:**
- DqtVariantTests.cs::SixteenBitQuantTable_DecodesIdentically — proves the decoder correctly parses Pq=1 (16-bit) DQT tables: rewrites the encoder's 8-bit DQT as 16-bit big-endian and asserts identical reconstruction (round-trip, decoder side).
- DqtVariantTests.cs::MultipleQuantTablesInOneSegment_Decode — proves multiple quantization tables sharing one DQT segment are all parsed (merges the encoder's separate DQT segments into one and asserts identical pixel output).
- DqtVariantTests.cs::TruncatedSixteenBitDqt_ThrowsFormatException — proves a Pq=1 table with only 64 (not 128) element bytes is rejected with JpegFormatException ('Truncated 16-bit quantization table', BaselineDecoder.cs:242-243).
- HighPrecisionQuantTests.cs::QuantValueAbove255_IsWrittenAs16BitAndRoundTrips — proves the ENCODER emits Pq=1 and writes 16-bit big-endian values at the correct zig-zag positions (checks dqt[0]&0xF0==0x10 and byte pairs vs CopyToZigZag).
- HighPrecisionQuantTests.cs::EightBitQuantTable_StillUses8BitPrecision — proves the ENCODER emits Pq=0 and a 65-byte (1+64) 8-bit DQT for standard tables.
- CustomTableTests.cs::CustomQuantTable_IsWrittenToDqt — proves the ENCODER lays out the 64 elements in zig-zag order after the Pq/Tq byte (matches CopyToZigZag).
- QuantizationTests.cs::ZigZagRoundTrip_PreservesTable — proves CopyToZigZag then FromZigZag round-trips a table (the mapping BaselineDecoder relies on at line 253), natural-order equality.
- QuantizationTests.cs::CustomTable_ZeroValue_Throws — proves QuantizationTable ctor throws ArgumentException on a zero divisor (the exception ParseQuantTables catches and rethrows as 'Invalid quantization table'), but only via the direct ctor, not the DQT byte-parse path.
- ZigZagTests.cs::Order_MatchesSpecSequence — proves ZigZag.Order equals the T.81 Annex A Fig A.6 sequence used to convert DQT elements to natural order.
- ZigZagTests.cs::Order_StartsAtDcAndIsAPermutation — proves the zig-zag order is a valid 0..63 permutation starting at DC.
- ZigZagTests.cs::InverseOrder_IsInverseOfOrder — proves Order/InverseOrder are true inverses (supports zig-zag correctness of DQT element placement).

**Gaps:**
- MAJOR: Decoder must reject Pq not in {0,1}. No test exists, and BaselineDecoder.ParseQuantTables (lines 233-249) has no such validation — any Pq of 2..15 falls into the 'else' branch and is silently treated as 16-bit. This is both an untested requirement and an actual code defect.
- DQT-segment-side Tq>3 rejection (BaselineDecoder.cs:230, 'Invalid quantization table id {id}') is not directly exercised. TableIdValidationTests.FrameQuantTableIdOutOfRange_ThrowsCleanly corrupts the SOF component Tq (a different code path), not the DQT header nibble.
- Truncated 8-bit table rejection (BaselineDecoder.cs:235-236, 'Truncated 8-bit quantization table') has no test; only the 16-bit truncation path is covered.
- Decoder-side parsing of two tables in ONE DQT with mixed precision (Tq=0 8-bit then Tq=1 16-bit) and per-table value verification is not covered. MultipleQuantTablesInOneSegment_Decode merges the encoder's tables (same precision) and only checks pixel equality, not distinct per-table stored values.
- Decoder-side 16-bit big-endian byte order placing a specific value (e.g. 0x01F4 -> 500) at the correct natural index is not asserted directly; only implied by the round-trip in SixteenBitQuantTable_DecodesIdentically (which uses zero high bytes, so it never proves the high byte is honored).
- Zero quantization value delivered through the DQT byte-parse path (BaselineDecoder.cs:251-258, JpegFormatException 'Invalid quantization table') is not covered; only the ctor-level ArgumentException is (QuantizationTests.CustomTable_ZeroValue_Throws).
- MINOR (deferred/unimplemented): 16-bit (Pq=1) table vs 8-bit DCT process consistency is neither implemented nor tested — no cross-check against frame sample precision.
- Full DQT-byte round trip (build known table -> CopyToZigZag -> hand-assemble DQT bytes -> ParseQuantTables -> assert natural-order equality) is not covered as a single end-to-end decoder test; existing coverage splits this across the encoder path and the table-level ZigZagRoundTrip.

**Required new tests:**
- `DqtPqEquals2_IsRejected` (decoder, T.81 B.2.4.1 (Pq must be in {0,1}; reject otherwise)): Assert a DQT header byte 0x20 (Pq=2, Tq=0) followed by 64 element bytes is rejected rather than silently interpreted as 16-bit and consuming 128 bytes. Primary test for the MAJOR finding; will FAIL against current code (documents the defect). → Jpeg.Decode throws JpegFormatException (an 'invalid/unsupported quantization precision' style message). Currently no such rejection exists, so this test fails until the decoder adds a precision-in-{0,1} check.
- `DqtPqEqualsF_IsRejected` (decoder, T.81 B.2.4.1 (Pq must be in {0,1}; reject otherwise)): Assert a DQT header byte 0xF3 (Pq=15, Tq=3) is rejected. Covers the upper boundary of the invalid Pq range distinct from Pq=2. → Jpeg.Decode throws JpegFormatException. Fails against current code (Pq=15 is silently treated as 16-bit).
- `DqtTableIdFour_IsRejected` (decoder, T.81 B.2.4.1 (Tq in 0..3)): Assert a DQT header byte 0x04 (Pq=0, Tq=4) is rejected by the DQT-segment id check (BaselineDecoder.cs:230), independent of the SOF frame-side check. → Jpeg.Decode throws JpegFormatException with message 'Invalid quantization table id 4.'
- `TruncatedEightBitDqt_ThrowsFormatException` (decoder, T.81 B.2.4.1 (length must be consistent with element count)): Assert a Pq=0 table declaring 8-bit precision but supplying only ~30 element bytes is rejected, covering the untested 8-bit truncation path (line 235-236). → Jpeg.Decode throws JpegFormatException 'Truncated 8-bit quantization table.'
- `TwoTablesOneSegment_MixedPrecision_BothStoredCorrectly` (decoder, T.81 B.2.4.1 (multiple tables may share one DQT; per-table precision)): Hand-assemble a single DQT containing Tq=0 as 8-bit and Tq=1 as 16-bit with distinct known values, decode a stream using both, and verify each table's values reach the decoder in natural order (e.g. via reconstruction differences or an inspectable decode path). Proves multi-table + mixed-precision parsing beyond the encoder's own uniform tables. → Both tables parse without error and each id holds its distinct values in natural order; a stream referencing both decodes consistently.
- `SixteenBitDqt_BigEndianValuePlacedAtNaturalIndex` (decoder, T.81 B.2.4.1 (Pq=1 -> 16-bit big-endian elements) / Annex A Fig A.6 (zig-zag)): Feed a Pq=1 DQT whose first zig-zag element is 0x01F4 (500, a genuinely >255 value requiring a non-zero high byte) and assert the value 500 lands at natural index 0 (DC). Unlike the existing round-trip (which zero-fills high bytes), this proves big-endian high-byte handling. → The parsed table exposes 500 at natural index 0 (and correspondingly correct dequantization); high byte is honored, not dropped.
- `DqtZeroElement_ThrowsInvalidQuantizationTable` (decoder, T.81 B.2.4.1 (no explicit non-zero constraint; implementation is intentionally stricter to avoid divide-by-zero)): Assemble a valid-length 8-bit DQT with one element byte set to 0x00 and decode, exercising the DQT-parse rejection path (BaselineDecoder.cs:251-258) rather than the direct ctor. Documents the stricter-than-spec non-zero assumption at the decoder boundary. → Jpeg.Decode throws JpegFormatException 'Invalid quantization table.' (wrapping the ctor ArgumentException).
- `DqtBytes_RoundTrip_ZigZagToNaturalMapping` (round-trip, T.81 B.2.4.1 / Annex A Fig A.6 (zig-zag ordering of the 64 elements)): Build a known QuantizationTable, emit its bytes with CopyToZigZag into a hand-built DQT segment, decode it, and assert the decoder's stored table equals the original in natural order. Validates the full zig-zag->natural mapping end-to-end through ParseQuantTables (currently only split across encoder + table-level tests). → Decoded table equals the original table at every natural index.

