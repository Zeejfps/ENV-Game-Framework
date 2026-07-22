# Encoder Huffman generation (standard+optimized) + bitstream + byte stuffing

**Section key:** `enc-huffman-bitstream`  
**Compliance:** PARTIAL · **Adversarial justified:** n/a · **Final:** **PARTIAL**

## Findings

- **[minor]** Optimized table generation per Annex K.2 must be robust to its declared input domain (256 frequencies).
  - Observed: HuffmanTable.BuildOptimized crashes with IndexOutOfRangeException when every input frequency is 0. With no nonzero real frequency, BuildCodeSizes finds c2<0 and breaks immediately, leaving codeSize (including the reserved index 256) all zero, so bits[] is all zero. The reserved-removal loop `while (bits[last]==0) last--` (line 103) walks past index 0 and evaluates bits[-1] at line 105. Not reachable via the encoder (luma/chroma DC always emits >=1 category symbol and AC always emits EOB for a flat block), but BuildOptimized is a public API entry point with no guard.
  - Spec: T.81 Annex K.2 (optimized Huffman construction)
- **[minor]** AC coefficients must be coded as an RRRRSSSS run/size byte where SSSS is a 4-bit category (T.81 F.1.2.2).
  - Observed: BlockScanCoder builds the AC symbol as `(run<<4)|category` (EncodeBlock line 69, GatherBlockFrequencies line 166) with no assertion that category<=15. MagnitudeCategory returns 16 for a coefficient of magnitude 32768 (short's -32768). For such a value the |16 (0x10) sets bit 4 and corrupts the run nibble (e.g. run=0,cat=16 becomes symbol 0x10 = run1/size0). Not reachable for valid 8-bit (AC<=cat10) or 12-bit (AC magnitude ~<=16384, cat<=15) coefficients, so it is a latent/defensive gap rather than an active defect.
  - Spec: T.81 F.1.2.2 (Huffman coding of AC coefficients, RRRRSSSS)
- **[info]** DC difference category (SSSS) range and standard-table applicability.
  - Observed: MagnitudeCategory can return 16 for a 12-bit DC difference (block[0] is short; diff up to +/-65535). Symbol 16 is a valid table entry and the decoder accepts categories up to 16 (BlockScanCoder line 96), and the encoder correctly forces optimized tables for precision>8 (BaselineEncoder line 216) since K.3 DC tables only cover 0..11. Encode/decode are self-consistent; noted only because SSSS=16 exceeds the 12-bit example-table range in T.81, though it is only reachable under pathological quant=1 extremes.
  - Spec: T.81 F.1.2.1 / Table F.6 (12-bit DC categories)

## Required fixes

- Guard HuffmanTable.BuildOptimized against an all-zero frequency table: after the reserved-removal loop, stop at length>=1 (or special-case an empty distribution) so bits[-1] is never indexed (HuffmanTable.cs line 103-105).
- Add a defensive check that MagnitudeCategory(coeff) <= 15 before packing the AC RRRRSSSS byte (BlockScanCoder.cs lines 69 and 166), or clamp/validate quantized coefficients so |coeff| < 32768, to prevent a category-16 value from corrupting the run nibble.

## Code locations

- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Huffman/HuffmanTable.cs:74 BuildOptimized (Annex K.2 optimized generation)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Huffman/HuffmanTable.cs:125 BuildCodeSizes (Figure K.1 frequency merge)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Huffman/HuffmanTable.cs:172 LimitCodeLengths (Figure K.3 BITS adjustment to 16)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Huffman/HuffmanTable.cs:79-105 reserved codepoint / bits[last]-- (no all-ones code)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Huffman/HuffmanTable.cs:253 Build (Annex C.2 HUFFSIZE/HUFFCODE + oversubscription check line 269)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Huffman/StandardHuffmanTables.cs:1 K.3 standard tables (DC 0..11, AC incl 0xF0/0x00)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Coding/BlockScanCoder.cs:43 EncodeBlock (DC diff + AC run/size, F.1.2)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Coding/BlockScanCoder.cs:185 Mantissa (matches BitReader.Extend)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Coding/BlockScanCoder.cs:144 GatherBlockFrequencies (two-pass frequency count)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Bitstream/BitWriter.cs:26 WriteBits (MSB-first + 0x00 stuffing after 0xFF)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Bitstream/BitWriter.cs:49 Flush (1-bit final padding)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Bitstream/BitReader.cs:147 Extend (EXTEND, F.12 — decoder counterpart)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.cs:351 BuildOptimizedTables (two-pass gather + restart predictor mirroring)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.cs:399 WriteEntropyData (restart flush/RSTn/predictor reset)`

## Test coverage

**Existing:**
- HuffmanTests.cs::CanonicalCodes_MatchSpecForDcLuminance - proves K.3 standard DC-luma canonical code/size values (T.81 Annex K.3/C.2)
- HuffmanTests.cs::DecodeSymbol_ReadsCanonicalCode - decoder reads canonical codes MSB-first
- HuffmanTests.cs::GeneratedCodes_ArePrefixFree - generated codes are prefix-free (Annex C)
- HuffmanTests.cs::EncodeThenDecode_RoundTripsEverySymbol_DcLuminance/DcChrominance/AcLuminance/AcChrominance - all four standard tables encode/decode every symbol symmetrically
- HuffmanTests.cs::Constructor_OversubscribedTable_Throws - oversubscription (all-ones/over-full length) rejected in HuffmanTable.Build
- HuffmanTests.cs::CountsAndSymbols_AreExposedForDhtWriting - BITS/HUFFVAL exposed and consistent for DHT emission
- OptimizedHuffmanTests.cs::MoreFrequentSymbols_GetShorterOrEqualCodes - K.2 frequency-count assigns shorter-or-equal codes to more frequent symbols
- OptimizedHuffmanTests.cs::OnlySymbolsWithFrequency_AppearInTable - zero-frequency symbols omitted
- OptimizedHuffmanTests.cs::SingleSymbol_GetsOneBitCode - degenerate single-symbol optimized table
- OptimizedHuffmanTests.cs::FibonacciFrequencies_AreLengthLimitedTo16 - K.2 code-size limiting to 16 engages and all sizes land in 1..16
- OptimizedHuffmanTests.cs::OptimizedTable_RoundTripsSymbolStream - optimized table encode/decode symmetry over a skewed stream
- OptimizedHuffmanTests.cs::OptimizedTable_IsNoWorseThanStandardTable - optimized bit cost <= standard table (K.2 optimality)
- OptimizedHuffmanTests.cs::WrongFrequencyLength_Throws - BuildOptimized rejects non-256 frequency spans (declared input domain guard)
- OptimizedHuffmanTests.cs::GeneratedCounts_SumToSymbolCount - Counts sum preserved to symbol total
- BitstreamTests.cs::ReadBits_ReadsMsbFirstAcrossByteBoundary - MSB-first bit packing across byte boundary
- BitstreamTests.cs::Writer_WriteBits_RoundTripsThroughReader - BitWriter/BitReader bit round-trip
- BitstreamTests.cs::Writer_StuffsZeroAfterFfByte - 0xFF data byte followed by 0x00 stuff byte (unit level)
- BitstreamTests.cs::Writer_Flush_PadsWithOneBits - final byte padded with 1-bits (F.1.2.3)
- BitstreamTests.cs::Extend_ProducesSignedDiff - decoder EXTEND for categories 1..3 sign reconstruction
- BitstreamTests.cs::Writer_IsDeterministic - deterministic bit emission
- BlockCoderTests.cs::MagnitudeCategory_MatchesSpec - SSSS category for representative values incl. cat 8 and 11
- BlockCoderTests.cs::SingleBlock_RoundTrips - DC+AC mantissa/EXTEND symmetry for a mixed block
- BlockCoderTests.cs::DcPrediction_ChainsAcrossBlocks - DC differential predictor chaining encode/decode
- BlockCoderTests.cs::DcOnlyBlock_EmitsEob - flat block emits EOB and decodes
- BlockCoderTests.cs::LongZeroRun_UsesZrlAndRoundTrips - ZRL(0xF0) run coding round-trips
- BlockCoderTests.cs::FullNonZeroBlock_RoundTrips / ManyRandomBlocks_RoundTripWithChainedPrediction - RRRRSSSS AC coding + mantissa symmetry over many blocks
- BlockCoderTests.cs::GatheredFrequencies_ProduceWorkingOptimizedTables - GatherBlockFrequencies -> BuildOptimized -> encode/decode consistency (non-degenerate distribution)
- BlockCoderTests.cs::DecodeBlock_BadRunPastBlockEnd_Throws - decoder rejects run past block end
- PropertyTests.cs::OptimizedHuffman_MatchesStandard_Reconstruction - full-pipeline OptimizeHuffman reconstruction equals standard-table reconstruction
- RestartIntervalTests.cs::Grayscale_WithRestartInterval_RoundTrips / Rgb420_WithRestartInterval_RoundTrips - DC predictor reset across RSTn round-trips (standard tables)
- RestartIntervalTests.cs::Output_ContainsDriAndRestartMarkers - DRI (0xDD) and RSTn markers emitted
- RestartIntervalTests.cs::RestartEncoding_IsDeterministic - restart-interval encoding deterministic
- HighPrecisionCodecTests.cs::Grayscale12_RoundTrips / RgbYCbCr12_RoundTrips / Cmyk12_RoundTrips - 12-bit precision forces optimized tables (BaselineEncoder precision>8) and round-trips (smooth content, low categories)

**Gaps:**
- No test asserts the all-ones code of the maximum used length is never assigned to a real symbol in an optimized table (the reserved-code-point invariant of Annex K.2). Only oversubscription of standard tables is checked.
- HuffmanTable.BuildOptimized is never exercised with an all-zero int[256]; the documented IndexOutOfRangeException (BuildCodeSizes finds no c2, reserved-removal loop walks bits[-1]) has no regression/guard test even though it is a public API entry point on its declared input domain.
- No test drives the minimal-symbol optimized path via the encoder: a single flat/all-128 MCU with OptimizeHuffman=true (DC category 0 + AC EOB only) that would exercise the reserved-removal walk with a near-empty bits[] table.
- Byte stuffing is only proven at the BitWriter unit level; no integration test encodes real image content that forces 0xFF entropy bytes and asserts every 0xFF within the SOS scan data is followed by 0x00 (or a valid marker) and still decodes.
- No explicit property test asserting BitReader.Extend(Mantissa(v,cat),cat) == v across ALL categories 1..16 with representative +/- values; high categories 12..16 (reachable only in 12-bit) are never exercised for encoder/decoder EXTEND symmetry.
- No test forces high DC/AC magnitude categories: 12-bit maximal-contrast content reaching DC category 15/16 and AC category 15 with optimized tables and surviving round-trip is untested (existing 12-bit tests use smooth gradients keeping categories low).
- OptimizeHuffman=true is never combined with RestartInterval>0; the consistency between GatherBlockFrequencies (with predictor reset at restart boundaries) and WriteEntropyData's emitted symbol stream across RSTn is unproven.
- AC RRRRSSSS assembly (run<<4)|category has no assertion/guard test that category<=15; the latent category==16 nibble-corruption path is defensive and untested.

**Required new tests:**
- `BuildOptimized_AllZeroFrequencies_HasDefinedBehavior` (regression, T.81 Annex K.2 (optimized Huffman construction)): Call HuffmanTable.BuildOptimized(new int[256]) and assert it does not throw IndexOutOfRangeException, returning either a defined empty-ish table or a documented ArgumentException, closing the reserved-removal walk-off-array gap on the declared input domain. → No IndexOutOfRangeException; a single deterministic, documented outcome (defined table or ArgumentException), stable across runs.
- `OptimizedTable_NeverAssignsAllOnesCodeOfMaxLength` (encoder, T.81 Annex K.2 / F.1.2.3 (all-ones code reserved)): For several distributions (skewed, Fibonacci, uniform), build an optimized table, compute each symbol's canonical code, find the max used length L, and assert no symbol holds the all-ones code ((1<<L)-1) of length L, verifying the reserved-code-point invariant. → No symbol is assigned the all-ones code of the maximum code length in any tested distribution.
- `FlatMcu_OptimizeHuffman_MinimalSymbolPath_RoundTrips` (round-trip, T.81 Annex K.2 + F.1.2 (minimal-symbol optimized encode)): Encode a single 8x8 all-128 (level-shift 0) grayscale MCU with OptimizeHuffman=true so the gathered frequencies contain only DC category 0 and AC EOB, exercising the minimal-symbol optimized-table build (reserved-removal on a near-empty bits[]) end to end. → Encode succeeds without exception and decode reproduces the flat block within tolerance; output contains a valid DHT and decodes cleanly.
- `EncodedScan_EveryFfByteIsStuffedWith00` (encoder, T.81 F.1.2.3 (byte stuffing)): Encode content engineered to produce 0xFF entropy bytes (and a flush that pads toward 0xFF), then walk the bytes between SOS and EOI and assert every 0xFF in the scan is immediately followed by 0x00 (or a legal RSTn/EOI marker), and that the stream still decodes. → Every 0xFF within entropy-coded scan data is followed by 0x00 (except genuine markers), and the image round-trips.
- `ExtendMantissa_Symmetry_AllCategories1To16` (round-trip, T.81 F.1.2.1/F.1.2.2 (EXTEND / mantissa symmetry)): For every category 1..16 and representative positive and negative boundary values in that category's range, assert BitReader.Extend(BlockScanCoder.Mantissa(v,cat),cat) == v, covering the high categories 12..16 that only 12-bit precision can reach. → Extend(Mantissa(v,cat),cat) == v for all tested v across categories 1..16, including 12..16.
- `HighContrast12Bit_OptimizedTables_EmitHighCategories_RoundTrip` (round-trip, T.81 F.1.2.1 / Table F.6 (12-bit DC/AC categories)): Encode a 12-bit maximal-contrast image (extreme sample deltas) at quality 100 with optimized tables; gather emitted symbols and assert DC categories reaching 15/16 and AC category 15 are actually produced, then confirm the image round-trips via Decode16. → High categories (DC 15/16, AC 15) are emitted with optimized tables and the 12-bit image reconstructs within tolerance.
- `OptimizeHuffman_WithRestartInterval_GatherMatchesEmittedStream` (round-trip, T.81 F.1.2.3 + B.2.4 (restart intervals with optimized Huffman)): Encode an image with both OptimizeHuffman=true and RestartInterval>0; verify the two-pass frequency gather (with DC predictor reset at each restart) yields tables that exactly decode the emitted stream, and that inserted RSTn markers align with predictor resets, by round-tripping and by comparing gathered vs emitted symbol counts. → Encode/decode round-trips with correct DC reset at each RSTn; gathered frequency symbol stream matches the emitted entropy symbol stream.
- `AcSymbolAssembly_CategoryNeverExceeds15` (encoder, T.81 F.1.2.2 (RRRRSSSS run/size byte)): Defensive guard test: exercise BlockScanCoder over blocks whose valid coefficient ranges (8-bit and 12-bit) are pushed to their extremes and assert every emitted AC RRRRSSSS byte has SSSS (category) <= 15 so the run nibble is never corrupted by a category-16 value. → All emitted AC symbols keep category in 1..15; no symbol overflows the size nibble into the run nibble.

