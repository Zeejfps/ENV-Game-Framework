# Huffman entropy decode: DC diff, AC run/size, sign-extend, EOB, ZRL

**Section key:** `huffman-decode`  
**Compliance:** PASS · **Adversarial justified:** true · **Final:** **PASS**

## Findings

- **[info]** AC: byte RRRR SSSS; SSSS=0 & RRRR=0 -> EOB; RRRR=15,SSSS=0 -> ZRL; else run/size
  - Observed: BlockScanCoder.DecodeBlock line 111-122 treats ANY (category==0, run!=15) as EOB, including the reserved/undefined RRRR=1..14 with SSSS=0. Per T.81 these symbols are not legal; the decoder silently ends the block instead of flagging corruption. This matches libjpeg's de-facto behavior (break on s==0 && r!=15), so it is not a conformance defect, but it can mask corrupt entropy data rather than throwing.
  - Spec: T.81 F.2.2.2 (Table F.4 / RS byte semantics)
- **[info]** DC: decode category S via Huffman
  - Observed: BlockScanCoder.DecodeBlock line 96 accepts dcCategory up to 16 (is <0 or >16). For 8-bit baseline the DC difference category is bounded by 11 (Annex F, Table F.1). Accepting up to 16 is lenient (supports higher precision, and ReadBits/Extend handle magnitude 16 safely) but is looser than the 8-bit baseline table permits; a truly precision-aware validator would cap at 11 for P=8.
  - Spec: T.81 F.1.2.1 / Table F.1
- **[info]** EXTEND sign-extension and coefficient value range
  - Observed: AC coefficients (BlockScanCoder.DecodeBlock line 128) are cast to short without an explicit range check, unlike the DC path (line 100). This is safe because AC category is <=15 so Extend yields at most +/-32767 (fits short); noted only to document that the safety relies on the 4-bit SSSS field width, not on an explicit guard.
  - Spec: T.81 F.1.2.2 / Figure F.12 (EXTEND)

## Counterexamples

- ZRL exactly filling the block: enter AC loop at k=48, symbol RS=0xF0 (run=15,cat=0). Code does k+=16 -> 64, check `k > 64` is false, `continue`, loop exits. Result: coefficients 48..63 zero, block terminated with no EOB. This is legal per T.81 (16 zero coeffs reaching index 63) and the decoder accepts it correctly. NOT a bug.
- ZRL overrunning by one: enter at k=49, RS=0xF0 -> k=65, `65 > 64` true -> throws 'Zero-run extends past the end of the block.' Correct rejection. NOT a bug.
- Full AC block with coefficient at index 63: reach k=63 via RS=0x0A (run=0,cat=10), k+=0=63 (<64), place block[63], k=64, loop exits with no EOB consumed. Matches EncodeBlock which emits no EOB when the last nonzero is at 63. Round-trips cleanly. NOT a bug.
- Normal run landing exactly out of range: k=60, RS=0x41 (run=4,cat=1) -> k+=4=64, `64 >= 64` true -> throws 'AC coefficient index out of range.' Correct. NOT a bug.
- DC magnitude 16: dcCategory=16, ReadBits(16), Extend(v,16) yields v-65535 for v<32768. dcValue range-checked against short; overflow throws 'DC coefficient out of range'. For a baseline (P=8) file a compliant encoder never emits cat>11, so this is unreachable leniency, not a defect.
- AC magnitude 15 sign-extend: RS=0x0F, ReadBits(15), Extend yields values in [-32767,-16384] U [16384,32767], all fit `short` without the DC path's explicit guard. Confirmed within range; safe by the 4-bit SSSS bound.
- EOF/marker mid-block padding: BitReader.FillByte at a marker returns false and ReadBits pads 1-bits; DecodeSymbol slow path then hits length>=16 and throws 'Invalid Huffman code' rather than silently misdecoding. Bounded and safe. NOT a bug.

## Code locations

- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Coding/BlockScanCoder.cs:91 DecodeBlock (entry, block.Clear at 93)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Coding/BlockScanCoder.cs:95-102 DC: category decode, receive+EXTEND, predictor add, store`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Coding/BlockScanCoder.cs:104-130 AC: RS decode, run/size split, ZRL (113-119), EOB (121), skip-run+read+EXTEND+place (124-129)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Bitstream/BitReader.cs:147-153 BitReader.Extend (sign-extend / F.12)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Bitstream/BitReader.cs:51-68 BitReader.ReadBits (receive S bits)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Huffman/HuffmanTable.cs:203-227 HuffmanTable.DecodeSymbol (Huffman category/RS decode, F.16)`

## Test coverage

**Existing:**
- BlockCoderTests.cs::SingleBlock_RoundTrips — DC category decode + receive S bits + EXTEND + predictor add, and AC run/size skip + read S bits + EXTEND with negative AC coefficients (block[1]=-5, block[40]=-2)
- BlockCoderTests.cs::DcOnlyBlock_EmitsEob — EOB immediately after DC (first AC symbol 0x00) fills all 63 AC coefficients with zero and preserves DC
- BlockCoderTests.cs::LongZeroRun_UsesZrlAndRoundTrips — ZRL (16 zeros) followed by a residual run round-trips
- BlockCoderTests.cs::DcPrediction_ChainsAcrossBlocks — DC differential prediction chain across blocks, including negative diffs (105->90)
- BlockCoderTests.cs::ManyRandomBlocks_RoundTripWithChainedPrediction — 200-block fuzz round-trip with chained predictor and standard tables
- BlockCoderTests.cs::GatheredFrequencies_ProduceWorkingOptimizedTables — round-trip through BuildOptimized (optimized) Huffman tables
- BlockCoderTests.cs::FullNonZeroBlock_RoundTrips — densely populated block round-trip exercising run/size + EXTEND
- BlockCoderTests.cs::DecodeBlock_BadRunPastBlockEnd_Throws — repeated ZRL overrunning index 63 throws JpegCorruptException ('Zero-run extends past the end of the block')
- BlockCoderTests.cs::MagnitudeCategory_MatchesSpec — SSSS magnitude category mapping for values incl. negatives up to category 11
- BitstreamTests.cs::Extend_ProducesSignedDiff — EXTEND sign-extension pivot/smallest/largest, but only for categories S=1,2,3
- BitstreamTests.cs::ReadBits_ReadsMsbFirstAcrossByteBoundary / ReadBits_Zero_ReturnsZeroWithoutConsuming — MSB-first ReadBits and zero-count no-op
- BitstreamTests.cs::ByteStuffing_Ff00_DecodesToSingleFfByte / Marker_StopsBitConsumptionAndIsExposed / Marker_LeavesBytePositionAtMarkerStart / FillBytes_BeforeMarkerAreSkipped — de-stuffing, marker detection, marker padding-with-1s
- BitstreamTests.cs::ResetForRestart_SkipsMarkerAndContinues — RSTn skip + buffer reset resumes decode
- HuffmanLookaheadTests.cs::PeekByte_DoesNotConsume / SkipBits_AdvancesByExactCount / PeekByte_PadsWithOnesAtEnd — lookahead/peek/skip fixed-example correctness
- HuffmanLookaheadTests.cs::DecodeSymbol_WithLookahead_MatchesEverySymbol / DecodeSymbol_HandlesCodesLongerThanLookahead — full-alphabet decode incl. >8-bit slow path
- HuffmanTests.cs::DecodeSymbol_ReadsCanonicalCode / EncodeThenDecode_RoundTripsEverySymbol_* — Huffman symbol decode used by DC category and AC RS decode
- HuffmanTests.cs::DecodeSymbol_InvalidCode_Throws — unmatched code throws JpegCorruptException

**Gaps:**
- EXTEND correctness across the full magnitude range: existing Extend test only covers S=1,2,3; S in 4..15 (incl. the smallest-value -> -(2^S)+1, pivot at 2^(S-1), largest -> 2^S-1) is unproven
- AC run overflow via coefficient placement (run pushing k to >=64) that hits the distinct 'AC coefficient index out of range' branch (line 125-126) is never triggered; only the ZRL zero-run overflow branch is tested
- ZRL boundary that lands k exactly at 64 (e.g. k=48 then ZRL) must decode WITHOUT throwing — the accept side of the boundary is untested (only the throwing overflow case exists)
- Full block of 63 nonzero AC coefficients with NO EOB symbol terminating cleanly when k reaches 64 is not deterministically exercised (random test may still emit zeros/EOB)
- Reserved AC symbols RS with SSSS=0 and RRRR in 1..14 are treated as EOB (line 111/121); no test documents/locks this deliberate libjpeg-compatible leniency
- DC-out-of-range guard (line 100-101, 'DC coefficient out of range') has no test
- Invalid DC magnitude category (<0 or >16) guard (line 96-97) has no test
- 12-bit-precision DC diffs producing categories 12..15 decoded through DecodeBlock (Extend + short storage + predictor accumulation) are not directly asserted at the block-coder level
- Truncated entropy stream ending mid-block (no EOB, no marker) — DecodeBlock must fill gracefully or throw, never loop or write out of bounds — is untested
- BitReader property/reference test: for arbitrary count 0..16 and arbitrary interleaving of ReadBits/PeekByte/SkipBits, bits must equal an MSB-first reference over the de-stuffed stream (only fixed hand-picked examples exist)
- BitReader.SkipRestartMarker realignment to the byte after RSTn (with 0xFF fill runs / stuffed 0x00) is not unit-tested (only ResetForRestart is)

**Required new tests:**
- `Extend_FullMagnitudeRange_SmallestPivotLargest` (decoder, T.81 F.1.2.2 / Figure F.12 (EXTEND)): For each S in 1..15 assert Extend(0,S) == -(2^S)+1, Extend(2^(S-1)-1,S) is the last negative, Extend(2^(S-1),S) is the first positive, and Extend(2^S-1,S) == 2^S-1 → All computed values match the EXTEND formula exactly across S=1..15
- `DecodeBlock_AcRunPlacesCoefficientPastEnd_Throws` (decoder, T.81 F.2.2.1 (must not exceed 63 coefficients)): Emit a DC then an AC RS symbol whose RRRR run advances k to >=64 before placing a coefficient, hitting the 'AC coefficient index out of range' branch distinct from the ZRL zero-run branch → JpegCorruptException with message 'AC coefficient index out of range'
- `DecodeBlock_ZrlLandingExactlyAt64_DoesNotThrow` (decoder, T.81 F.2.2.1 (k>64 is the overflow condition, k==64 is legal)): Encode nonzero AC coefficients up to index 47 (k=48) then a ZRL so k reaches exactly 64; verify positions 48..63 are zero and no exception is raised → Block decodes cleanly, indices 48..63 == 0, DC preserved
- `DecodeBlock_63NonZeroAcNoEob_TerminatesAtK64` (round-trip, T.81 F.1.2.2 (loop bound k<64, no EOB required for a full block)): Encode a block whose AC coefficients 1..63 are all nonzero (no EOB emitted) and assert the decoder terminates when k reaches 64 without reading a trailing symbol → Decoded block bit-exactly equals the 64-coefficient input
- `DecodeBlock_ReservedRsZeroSizeNonZeroRun_TreatedAsEob` (regression, T.81 F.2.2.2 (Table F.4 / RS byte semantics)): Feed an AC RS symbol with SSSS=0 and RRRR in 1..14 and assert the block ends as EOB (remaining coefficients zero) without throwing, locking the deliberate libjpeg-compatible leniency → Block terminates as EOB, no exception, trailing coefficients zero
- `DecodeBlock_DcCategoryOutOfRange_Throws` (decoder, T.81 F.1.2.1 / Table F.1): Provide a DC Huffman symbol greater than 16 (or negative) and assert the DC-category validation guard fires → JpegCorruptException 'Invalid DC magnitude category'
- `DecodeBlock_DcAccumulationOutOfShortRange_Throws` (decoder, T.81 F.1.2.1 (DC differential accumulation)): Chain DC diffs (via predictor + large diff) so the accumulated DC exceeds short range and assert the out-of-range guard throws → JpegCorruptException 'DC coefficient out of range; corrupt entropy data.'
- `DecodeBlock_TwelveBitDcCategories_RoundTrip` (round-trip, T.81 F.1.2.1 (12-bit precision, categories up to 15)): Build DC diffs producing categories 12..15, encode with a table admitting those symbols, and verify DecodeBlock reproduces the DC values and predictor chain without a spurious range reject → Decoded DC values and returned predictors match the originals for categories 12..15
- `DecodeBlock_TruncatedMidBlockStream_TerminatesSafely` (regression, T.81 F.2.2 (graceful handling of exhausted entropy data)): Feed an entropy segment that ends mid-block with no EOB and no marker; assert DecodeBlock fills-with-1s-padding to completion or throws JpegCorruptException, never looping or writing out of bounds → Returns a decoded block or throws JpegCorruptException; no infinite loop, no out-of-bounds write
- `BitReader_ReadBits_MatchesMsbFirstReference` (decoder, T.81 F.2.2.5 / Annex B (MSB-first entropy bit ordering)): Property test: for a random de-stuffed byte stream and arbitrary interleaving of ReadBits(0..16)/PeekByte/SkipBits, assert returned bits equal an independent MSB-first reference implementation → Every read matches the reference across many random seeds
- `BitReader_SkipRestartMarker_RealignsPastRstn` (decoder, T.81 B.2.1 / F.1.1.1.2 (restart markers)): Construct a stream with 0xFF fill runs, stuffed 0x00 bytes, and an RSTn marker; assert SkipRestartMarker discards buffered bits and advances BytePosition to the byte immediately after RSTn, and that a missing marker throws → BytePosition lands just after the RSTn code; JpegCorruptException when no restart marker is present
- `DecodeBlock_ZrlPlusEobFuzz_RoundTripBitExact` (round-trip, T.81 F.1.2 (full DC+AC entropy coding round-trip)): Round-trip a corpus of random 64-coefficient blocks (forcing >=16 consecutive zeros for ZRL and a trailing zero run for EOB) through EncodeBlock/DecodeBlock with BOTH standard and BuildOptimized tables and assert bit-exact recovery plus identical returned DC predictor → Decoded blocks and returned predictors are bit-exactly equal to the encoder inputs for every seed and both table sets

