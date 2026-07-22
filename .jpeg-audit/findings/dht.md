# DHT Huffman-table segment parsing & table build

**Section key:** `dht`  
**Compliance:** PARTIAL · **Adversarial justified:** n/a · **Final:** **PARTIAL**

## Findings

- **[major]** Tc (table class) must be in {0,1}; values 2..15 are reserved and must be rejected (T.81 B.2.4.2, Table B.5)
  - Observed: ParseHuffmanTables reads tableClass = tcTh >> 4 but never validates it. Only tableClass==0 is treated as DC; ALL other values (1 and the reserved 2..15) fall through to the else branch and are stored as AC tables. A malformed DHT with Tc=2..15 is silently accepted and misclassified as AC instead of being rejected.
  - Spec: T.81 B.2.4.2 / Table B.5 (Tc definition)
- **[major]** Sum of the 16 BITS counts (total number of codes / HUFFVAL bytes) must be <= 256
  - Observed: Neither BaselineDecoder.ParseHuffmanTables nor the HuffmanTable(counts,symbols) constructor enforces total <= 256. The ctor only checks total == symbols.Length and total != 0 (HuffmanTable.cs:49-56). A table declaring 257..~65000 codes (e.g. counts[14]=2, counts[15]=255) passes the oversubscription check because 16-bit code space has up to 65534 slots, but forces duplicate symbol byte values which silently overwrite _codes/_sizes in Build (HuffmanTable.cs:272-273), yielding a corrupt encode table and an out-of-spec structure that should have been rejected.
  - Spec: T.81 B.2.4.2 (sum of Li <= 256) / Annex F.2.2
- **[minor]** For baseline (SOF0) frames Th must be 0..1 (only two DC and two AC tables permitted); extended/progressive allow 0..3
  - Observed: ParseHuffmanTables rejects only id >= 4 (BaselineDecoder.cs:270-271), so Th=2 or 3 is accepted even for a baseline SOF0 frame. The frame type is not consulted when validating table-destination ids.
  - Spec: T.81 B.2.4.2 / Table B.5 (Th range) and B.2.2 baseline constraints
- **[info]** Empty Huffman table (all 16 BITS counts zero) handling
  - Observed: HuffmanTable ctor throws for total==0 (HuffmanTable.cs:55-56), so an all-zero-count DHT sub-table is rejected as invalid. This is stricter than the letter of the spec but is a reasonable defensive choice; noted only as an assumption that could reject unusual-but-harmless encoder output.
  - Spec: T.81 B.2.4.2

## Required fixes

- In ParseHuffmanTables (BaselineDecoder.cs ~267), validate tableClass: throw JpegFormatException when tableClass > 1 (Tc must be 0 or 1) before dispatching to DC/AC arrays.
- Enforce sum(BITS) <= 256: add a check (in the HuffmanTable ctor after computing total, and/or in ParseHuffmanTables) that throws when total > 256, so over-limit tables are rejected rather than silently producing duplicate-symbol corruption.
- Optionally enforce the baseline Th 0..1 constraint when the active frame marker is SOF0, rejecting Th=2/3 for baseline while still allowing 0..3 for extended/progressive frames.
- Consider detecting duplicate symbol values in HUFFVAL (or reject any symbol assigned twice in Build) as defense-in-depth even once the <=256 check is added.

## Code locations

- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:262-301 (ParseHuffmanTables: DHT segment parse, Tc/Th split at 267-269, id check 270-271, truncation guards 272-284, table dispatch 296-299)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Huffman/HuffmanTable.cs:44-62 (ctor: counts==16 check, total==symbols check, total!=0 check; no total<=256 check)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Huffman/HuffmanTable.cs:253-298 (Build: HUFFSIZE/HUFFCODE gen C.2, oversubscription check line 269, MINCODE/MAXCODE/VALPTR build 281-295)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Huffman/HuffmanTable.cs:203-227 (DecodeSymbol: F.16 slow path, MAXCODE=-1 sentinel, length>=16 over-run reject at 219)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Huffman/HuffmanTable.cs:320-327 (FirstCodeOfLength / MINCODE derivation)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Huffman/StandardHuffmanTables.cs:9-88 (Annex K.3 standard tables)`

## Test coverage

**Existing:**
- HuffmanTests.cs::CanonicalCodes_MatchSpecForDcLuminance — spot-checks C.2 canonical code/length for DC-luminance symbols 0,1,6,11 (partial proof of MINCODE/HUFFCODE build correctness)
- HuffmanTests.cs::DecodeSymbol_ReadsCanonicalCode — decoder reads canonical codes back (F.16 fast path)
- HuffmanTests.cs::EncodeThenDecode_RoundTripsEverySymbol_DcLuminance — round-trips every symbol of the K.3 DC-luma table through Build+Decode
- HuffmanTests.cs::EncodeThenDecode_RoundTripsEverySymbol_DcChrominance — same for K.3 DC-chroma
- HuffmanTests.cs::EncodeThenDecode_RoundTripsEverySymbol_AcLuminance — same for K.3 AC-luma
- HuffmanTests.cs::EncodeThenDecode_RoundTripsEverySymbol_AcChrominance — same for K.3 AC-chroma
- HuffmanTests.cs::GeneratedCodes_ArePrefixFree — proves prefix-free property for AC-luminance table
- HuffmanTests.cs::Constructor_MismatchedCounts_Throws — ctor rejects sum(BITS) != symbols.Length
- HuffmanTests.cs::Constructor_WrongCountsLength_Throws — ctor rejects counts span not length 16
- HuffmanTests.cs::Constructor_OversubscribedTable_Throws — ctor rejects over-full table (3 codes of length 1) at Build (HuffmanTable.cs:269)
- HuffmanTests.cs::DecodeSymbol_InvalidCode_Throws — invalid code that never resolves within 16 bits throws JpegCorruptException (exercises DecodeSymbol.cs:219-220 exhaustion path)
- HuffmanTests.cs::CountsAndSymbols_AreExposedForDhtWriting — Counts(16)/Symbols exposure and sum==Symbols.Length invariant
- CorruptExceptionTests.cs::InvalidHuffmanCode_ThrowsCorruptException — duplicate proof that unresolvable code -> JpegCorruptException
- CorruptionTests.cs::OversubscribedHuffmanTable_ThrowsFormatException — full DHT decode path: oversubscribed DHT wrapped to JpegFormatException (BaselineDecoder.cs:287-294)
- CorruptionTests.cs::ScanReferencingMissingTable_ThrowsFormatException — scan referencing an absent Huffman table is rejected
- HuffmanLookaheadTests.cs::DecodeSymbol_HandlesCodesLongerThanLookahead — 9-bit DC-luma code exercises the slow canonical path with MAXCODE=-1 intermediate lengths
- HuffmanLookaheadTests.cs::DecodeSymbol_WithLookahead_MatchesEverySymbol — every symbol of all four K.3 tables decodes via lookahead+slow paths
- DhtVariantTests.cs::MultipleHuffmanTablesInOneSegment_Decode — multiple sub-tables packed in one DHT segment are parsed sequentially (BaselineDecoder.cs:265 loop)
- DhtVariantTests.cs::InterScanDhtRedefinition_IsHonored — DHT redefinition between scans is re-parsed and applied
- CustomTableTests.cs::CustomHuffmanTable_IsWrittenToDht — DHT byte layout (Tc/Th byte 0x00 + 16 counts) verified on the encode side
- CustomTableTests.cs::CustomHuffmanTables_RoundTrip — custom-built DC/AC tables survive a full encode/decode round-trip

**Gaps:**
- Tc (table class) reserved values 2..15 are never rejected and no test exercises this; a DHT with Tc=2 is silently misclassified as AC (BaselineDecoder.cs:268,296-299) — MAJOR, no test
- sum of the 16 BITS counts > 256 is never enforced and no test exercises it; e.g. counts[14]=2,counts[15]=255 (=257) passes ctor checks and corrupts encode tables via duplicate-symbol overwrite (HuffmanTable.cs:49-56,271-273) — MAJOR, no test
- Baseline (SOF0) Th must be 0..1: DHT with Th=2 or 3 in a baseline frame is accepted (only id>=4 is rejected, BaselineDecoder.cs:270-271); frame type is not consulted — MINOR, no test
- DHT destination id >= 4 rejection (BaselineDecoder.cs:270-271) is not directly exercised; TableIdValidationTests only covers SOF Tq and SOS Td/Ta, not the DHT Th field — no test
- Truncated DHT: too few HUFFVAL bytes ('Truncated Huffman table symbols', BaselineDecoder.cs:280-281) and too few count bytes ('Truncated Huffman table counts', :272-273) are not exercised by any test — no test
- Canonical-code build (C.2 MINCODE/MAXCODE/VALPTR / HUFFCODE) is only spot-checked for DC-luminance; there is no test verifying the full code-length assignment against the T.81 reference for all four K.3 tables — partial
- Explicit maximum-length 16-bit code round-trip through the slow DecodeSymbol path is not covered (existing slow-path test uses a 9-bit code) — partial
- Empty Huffman sub-table (all 16 BITS counts zero -> ctor throws total==0, HuffmanTable.cs:55-56) is not exercised by any test — no test (info-level assumption)

**Required new tests:**
- `DhtReservedTableClass_Tc2_IsRejected` (decoder, T.81 B.2.4.2 / Table B.5 (Tc must be 0 or 1)): Craft a minimal SOI+DHT segment with the Tc/Th byte = 0x20 (Tc=2 reserved, Th=0) plus 16 BITS and matching HUFFVAL, and assert Jpeg.Decode throws JpegFormatException instead of silently storing it as an AC table. → JpegFormatException (Tc=2..15 rejected as an invalid table class); currently FAILS — decoder accepts and misclassifies as AC.
- `DhtBitsSumExceeds256_IsRejected` (decoder, T.81 B.2.4.2 (sum of Li <= 256) / Annex F.2.2): Build a DHT sub-table whose 16 BITS counts sum to 257 (e.g. counts[14]=2, counts[15]=255) with 257 HUFFVAL bytes, feed it through Jpeg.Decode (and/or the HuffmanTable ctor directly), and assert rejection rather than silent symbol overwrite. → JpegFormatException / ArgumentException (total symbols > 256 rejected); currently FAILS — ctor accepts and Build overwrites _codes/_sizes producing a corrupt table.
- `BaselineDhtDestinationId_Th2_IsRejected` (decoder, T.81 B.2.4.2 / Table B.5 (Th) and B.2.2 baseline constraints (only two DC/AC tables)): Encode a baseline SOF0 grayscale image, mutate a DHT sub-table's Tc/Th byte to Th=2 (byte 0x02) or Th=3, and assert the baseline decoder rejects the out-of-range destination for a baseline frame. → JpegFormatException (baseline permits Th 0..1 only); currently FAILS — Th=2/3 accepted because only id>=4 is checked.
- `DhtDestinationId4_IsRejected` (decoder, T.81 B.2.4.2 / Table B.5 (Th range 0..3)): Construct a DHT segment with Tc/Th byte = 0x04 (Th=4) and assert Jpeg.Decode throws, directly exercising the existing id>=4 guard at BaselineDecoder.cs:270-271 which no test currently hits. → JpegFormatException('Invalid Huffman table id 4'); expected to PASS (locks in existing behavior).
- `TruncatedDhtSymbols_IsRejected` (decoder, T.81 B.2.4.2 (HUFFVAL length = sum of BITS)): Build a DHT whose 16 BITS declare N codes but supply fewer than N HUFFVAL bytes (segment length short), and assert Jpeg.Decode throws the truncation error, exercising BaselineDecoder.cs:280-281. → JpegFormatException('Truncated Huffman table symbols'); expected to PASS.
- `TruncatedDhtCounts_IsRejected` (decoder, T.81 B.2.4.2 (16 BITS bytes required)): Build a DHT segment that ends before all 16 BITS count bytes are present and assert rejection, exercising BaselineDecoder.cs:272-273. → JpegFormatException('Truncated Huffman table counts'); expected to PASS.
- `StandardTables_CanonicalCodeLengths_MatchT81Reference` (encoder, T.81 Annex C.2 / K.3 example tables): For all four K.3 tables (DcLuminance, DcChrominance, AcLuminance, AcChrominance) enumerate every symbol via GetCode and assert each code length equals the length implied by its position in BITS, and that codes are contiguous canonical values per length (full C.2 HUFFCODE verification, not just the DC-luma spot check). → All symbol code lengths and canonical code values match the T.81 reference for every K.3 table; expected to PASS.
- `MaxLength16BitCode_RoundTripsThroughSlowPath` (round-trip, T.81 Annex C.2 / F.16 (max code length 16)): Construct a table containing a symbol whose canonical code is exactly 16 bits (with empty/MAXCODE=-1 intermediate lengths), encode then DecodeSymbol it, confirming the slow canonical path resolves a full-length code without over-running the 16-bit guard. → The 16-bit-coded symbol round-trips exactly; expected to PASS (extends existing 9-bit slow-path coverage).
- `AllZeroBitsDhtSubTable_IsRejected` (regression, T.81 B.2.4.2 (empty table handling; implementation assumption)): Feed a DHT sub-table whose 16 BITS counts are all zero (no codes defined) and assert the current defensive rejection, documenting the total==0 behavior at HuffmanTable.cs:55-56. → JpegFormatException/ArgumentException ('A Huffman table must define at least one code'); expected to PASS (pins the stricter-than-spec assumption).

