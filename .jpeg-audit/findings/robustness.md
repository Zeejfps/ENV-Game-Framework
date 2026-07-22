# Robustness: truncated / corrupt / malformed input

**Section key:** `robustness`  
**Compliance:** PARTIAL · **Adversarial justified:** n/a · **Final:** **PARTIAL**

## Findings

- **[major]** restart marker desync detection
  - Observed: BitReader.SkipRestartMarker (Bitstream/BitReader.cs:122-138) only verifies that SOME RST0-RST7 marker is present at the restart boundary; it never checks the expected modulo-8 restart number. Per T.81 restart markers cycle RST0..RST7, and a decoder must detect a wrong-numbered marker (evidence of lost/inserted MCUs). DecodeScan (BaselineDecoder.cs:421-425) and progressive passes (BaselineDecoder.Progressive.cs:101,152,208,252) call SkipRestartMarker without passing/checking the expected index. A stream that lost data but still contains a syntactically valid wrong-numbered RSTn is accepted and silently produces corrupt output instead of a typed error.
  - Spec: ITU-T T.81 E.1.4 / F.1.4 (restart marker sequence RSTm, m = 0..7 mod 8)
- **[major]** no unbounded allocation / fail safely with a typed exception
  - Observed: Geometry and buffer sizes use 32-bit int arithmetic with no overflow guard: SetupGeometry (BaselineDecoder.cs:393-398) computes c.PlaneWidth*c.PlaneHeight; DecodeProgressive (BaselineDecoder.Progressive.cs:20) computes BlocksWide*BlocksHigh*64; AssembleImage/AssembleCmyk (BaselineDecoder.cs:490,511,563,650) compute _width*_height*3 and *4. The only overflow guard is ValidatePixelBudget's default MaxPixels=500M (JpegDecoderOptions.cs:13). If a caller raises MaxPixels, or for a 4-component image where width*height*4 exceeds int.MaxValue, the multiplication wraps negative and 'new byte[negative]' throws OverflowException/OutOfMemoryException (a non-JpegException type), violating the typed-exception guarantee.
  - Spec: T.81 B.2.2 (dimensions up to 65535x65535); robustness: typed failure + bounded allocation
- **[minor]** fail safely with a typed exception (no untyped crash)
  - Observed: No top-level try/catch in Jpeg.Decode/Decode16/ReadInfo (Api/Jpeg.cs:69-102) or BaselineDecoder.Decode (BaselineDecoder.cs:76-114) normalizes unexpected exceptions into JpegException. Only ArgumentException from table construction is wrapped (BaselineDecoder.cs:251-258,287-294). Any IndexOutOfRangeException, OverflowException, or ArgumentException escaping a not-yet-covered edge case (the int overflow above, or ReadMetadata parsing) reaches the caller as a non-JpegException, so callers cannot catch a single typed base exception as the conformance goal requires.
  - Spec: Robustness requirement: decoder must fail with a typed exception on malformed input
- **[minor]** truncated file mid-scan must fail safely
  - Observed: For a baseline scan with no restart interval, a stream truncated mid-scan raises no error: BitReader.FillByte (Bitstream/BitReader.cs:155-185) supplies padding 1-bits at end-of-data, and DecodeScan (BaselineDecoder.cs:417-450) iterates a fixed MCU count, so remaining MCUs decode from padding into garbage and a fully-formed but corrupt image is returned. It does not crash or hang (loops bounded; Huffman 16-bit cap at HuffmanTable.cs:217-220 throws JpegCorruptException if all-ones is undecodable), but silent garbage on truncation is weaker than 'fail safely'; there is no EOI/end-of-data completeness check.
  - Spec: T.81 B.2.1 (compressed image data terminated by EOI)
- **[minor]** unknown / standalone markers handled safely in header stream
  - Observed: ParseHeaders (BaselineDecoder.cs:154-218) calls reader.ReadSegment() unconditionally for every non-SOI/EOI/SOS marker and never consults JpegMarkers.HasLengthField (Markers/JpegMarkers.cs:106-113). A standalone marker with no length field (RSTn 0xD0-0xD7 or TEM 0x01) appearing in the header region causes the next two data bytes to be misread as a segment length, desynchronizing the parser. It stays bounded (max ~64KB alloc, or JpegFormatException on truncation) so it is not a crash, but the error is misleading and a recoverable stream may be rejected.
  - Spec: T.81 B.1.1.3 / Table B.1 (standalone markers carry no length)
- **[minor]** dimension guards must bound memory, not just pixel count
  - Observed: ValidatePixelBudget (BaselineDecoder.cs:116-120) bounds only width*height against MaxPixels. Peak memory is larger by component count and MCU padding: a 4-component (CMYK) image at the budget allocates four padded planes plus a width*height*4 output array (BaselineDecoder.cs:563,650), roughly 4-8x the pixel count in bytes (multiple GB at the default 500M budget). The guard does not tightly bound allocation; a precision/component-aware or byte-based budget would better meet the 'no unbounded allocation' requirement.
  - Spec: Robustness requirement: bound allocation from attacker-controlled dimensions
- **[info]** invalid SOF fields rejected
  - Observed: ParseFrameHeader (BaselineDecoder.cs:303-338) rejects width/height<=0, so a frame with Y=0 (height defined later via a DNL marker, permitted by T.81 B.2.2) is rejected rather than supported. This is a safe typed rejection, not a crash, but a spec-completeness limitation worth noting.
  - Spec: T.81 B.2.2 / B.2.5 (Y may be 0 with DNL segment)

## Required fixes

- Verify the restart-marker number against the expected modulo-8 sequence in SkipRestartMarker (pass an expected index, throw JpegCorruptException on mismatch) so restart desync is detected rather than silently accepted.
- Guard geometry and buffer-size computations against 32-bit overflow: compute PlaneWidth*PlaneHeight, BlocksWide*BlocksHigh*64, and width*height*components in long/checked arithmetic and throw JpegFormatException on excess, independent of MaxPixels.
- Wrap the public decode entry points (Jpeg.Decode/Decode16/DecodeAnyPrecision and BaselineDecoder.Decode/Decode16) so any unexpected exception (IndexOutOfRange, Overflow, OutOfMemory, ArgumentException) is caught and rethrown as a JpegException subtype, guaranteeing a single typed failure contract.
- Detect end-of-entropy-data / truncation during scan decoding (surface BitReader marker/EOF state and throw JpegCorruptException when the stream ends before the expected MCU count) instead of silently decoding padding into garbage.
- Use JpegMarkers.HasLengthField in ParseHeaders so standalone markers (RSTn, TEM) in the header stream are skipped without consuming a bogus length field.
- Optionally tighten the allocation guard to account for component count and precision (byte-based budget) so peak memory, not just pixel count, is bounded.

## Code locations

- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Bitstream/BitReader.cs:122 (SkipRestartMarker - missing-marker throw but no modulo-8 check)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Bitstream/BitReader.cs:155 (FillByte - pads 1-bits at EOF/marker)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Huffman/HuffmanTable.cs:217 (DecodeSymbol slow-path 16-bit cap -> JpegCorruptException)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Coding/BlockScanCoder.cs:96 (DC category 0..16 check), :116 (ZRL past block), :125 (AC index out of range)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:116 (ValidatePixelBudget), :303 (ParseFrameHeader validation), :340 (ParseScanHeader validation), :393 (SetupGeometry int-overflow-prone allocations), :421 (restart call), :738-757 (missing-table typed throws)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.Progressive.cs:20 (coefficient buffer allocation), :66 (spectral-selection validation), :189 (progressive AC index check)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Markers/MarkerReader.cs:67 (ReadSegment length>=2, 64KB-bounded), :81 (ReadExact truncation throw)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Api/JpegDecoderOptions.cs:13 (MaxPixels)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Api/Jpeg.cs:69 (Decode - no top-level exception normalization)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Api/Exceptions/JpegFormatException.cs & JpegCorruptException.cs (typed exception hierarchy)`

## Test coverage

**Existing:**
- CorruptionTests::EmptyInput_ThrowsFormatException — empty input rejected with typed JpegFormatException
- CorruptionTests::RandomBytes_ThrowsFormatException — pure junk rejected typed
- CorruptionTests::MissingSoi_ThrowsFormatException — missing SOI rejected typed
- CorruptionTests::TruncatedAfterSoi_ThrowsFormatException — truncated right after SOI rejected typed
- CorruptionTests::TruncatedSegment_ThrowsFormatException — segment length overrun (APP0 declares 100 bytes, none present) rejected typed
- CorruptionTests::OversubscribedHuffmanTable_ThrowsFormatException — invalid DHT (3 one-bit codes) rejected typed
- CorruptionTests::ZeroDimensions_ThrowsFormatException — SOF with Y=0 rejected typed (safe rejection of the DNL-deferred-height case)
- CorruptionTests::InvalidComponentSamplingFactor_ThrowsFormatException — SOF sampling byte 0x00 (H=V=0) rejected typed
- CorruptionTests::ScanReferencingMissingTable_ThrowsFormatException — SOS references a DHT that was stripped, rejected typed
- CorruptionTests::TruncatedStream_NeverThrowsNonJpegException — baseline stream truncated at every offset never throws a non-JpegException and does not hang
- CorruptionTests::CorruptedEntropy_NeverThrowsNonJpegException — 50 single-byte entropy corruptions never throw a non-JpegException
- CorruptExceptionTests::JpegCorruptException_IsAJpegFormatException — typed exception hierarchy (Corrupt <: Format <: JpegException) proven
- CorruptExceptionTests::InvalidHuffmanCode_ThrowsCorruptException — Huffman code with no match raises JpegCorruptException
- CorruptExceptionTests::CorruptEntropy_ThrowsCorruptException_CaughtAsFormatException — fully overwritten entropy scan fails as a JpegFormatException subtype
- HeaderFuzzTests::CorruptingAnyHeaderByte_NeverThrowsNonJpegException — every header byte of 4 fixtures (gray/rgb420/cmyk/progressive) mutated to 4 values; only JpegException escapes, output fully materialized
- ProgressiveRobustnessTests::TruncatedProgressiveStream_NeverThrowsNonJpegException — progressive truncated at every offset stays typed
- ProgressiveRobustnessTests::CorruptedProgressiveEntropy_NeverThrowsNonJpegException — 100 bit-flips in progressive entropy stay typed
- ProgressiveRobustnessTests::CorruptedProgressiveDecode_Terminates — heavily corrupted progressive multi-scan/EOB-run decode terminates (no unbounded loop/hang)
- ProgressiveRobustnessTests::ProgressiveAcScanWithMultipleComponents_Throws — AC scan declaring Ns=2 (must be non-interleaved) rejected with JpegFormatException
- ProgressiveScanHeaderTests::SpectralSelectionEndAboveMax_ThrowsCleanly — Se=100 (>63) rejected typed
- ProgressiveScanHeaderTests::SpectralSelectionStartAboveEnd_ThrowsCleanly — Ss>Se rejected typed
- ProgressiveScanHeaderTests::DcScanWithNonZeroSe_ThrowsCleanly — DC scan (Ss=0) with Se!=0 rejected typed
- BlockCoderTests::DecodeBlock_BadRunPastBlockEnd_Throws — AC run/ZRL pushing coefficient index past 63 raises JpegCorruptException
- HuffmanTests::DecodeSymbol_InvalidCode_Throws — all-ones code that matches no entry raises JpegCorruptException
- HuffmanTests::Constructor_OversubscribedTable_Throws — oversubscribed code counts rejected (ArgumentException at table build)
- HuffmanTests::Constructor_MismatchedCounts_Throws — DHT counts/symbol-list mismatch rejected
- HuffmanTests::Constructor_WrongCountsLength_Throws — malformed counts length rejected
- SamplingFactorValidationTests::SamplingFactorAboveFour_Throws — SOF sampling factors 5/15 rejected with JpegFormatException
- TableIdValidationTests::FrameQuantTableIdOutOfRange_ThrowsCleanly — SOF Tq=5 (>3) rejected typed
- TableIdValidationTests::ScanHuffmanTableIdOutOfRange_ThrowsCleanly — SOS Td=Ta=4 rejected typed
- TableIdValidationTests::ScanAcTableIdOutOfRange_ThrowsCleanly — SOS Ta=5 rejected typed
- UnsupportedFrameTypeTests::UnsupportedFrameType_ThrowsClearFormatException — SOF3/5/7/9/10/11/13 rejected with a clear JpegFormatException
- UnsupportedFrameTypeTests::ArithmeticFrame_MessageMentionsUnsupported — arithmetic SOF9 message says 'not supported'
- DqtVariantTests::TruncatedSixteenBitDqt_ThrowsFormatException — DQT declaring 16-bit precision but missing half the table rejected typed
- MarkerTests::Reader_SegmentLengthTooSmall_Throws — segment length < 2 rejected typed
- MarkerTests::Reader_TruncatedPayload_Throws — declared payload longer than data rejected typed
- MarkerTests::Reader_NonFfWhereMarkerExpected_Throws — non-0xFF where a marker is required rejected typed
- MarkerTests::HasLengthField_Classifies — unit-level proof that RSTn/TEM/SOI/EOI are standalone (no length) — classification only, not exercised in a real header stream
- DecoderOptionsTests::MaxPixels_RejectsImagesThatExceedTheLimit — MaxPixels guard rejects oversized image with JpegFormatException
- DecoderOptionsTests::MaxPixels_AllowsImagesWithinTheLimit — within-budget image decodes
- BoundaryFieldTests::RestartInterval_AtMax_EncodesAndDecodes — DRI=65535 boundary round-trips without desync
- BitstreamTests::ResetForRestart_SkipsMarkerAndContinues — BitReader resumes after an RSTn at a restart boundary
- BitstreamTests::Marker_StopsBitConsumptionAndIsExposed — marker halts bit consumption, padding read as 1s (bounded, no OOB)
- DimensionLimitTests::MaximumAllowedDimension_Encodes — 65535x1 at the JPEG dimension limit round-trips

**Gaps:**
- Restart marker DESYNC detection: no test feeds a wrong-numbered RSTn (e.g. RST3 where RST0 is expected) into a restart-interval stream. SkipRestartMarker (BitReader.cs:122-138) accepts any RST0-RST7, so lost/inserted MCUs producing a syntactically valid but wrong-numbered marker go undetected and yield silent corruption. No existing test exercises the modulo-8 restart sequence (T.81 E.1.4/F.1.4).
- Missing restart marker at a restart boundary: no test replaces the entropy at a restart boundary with an immediate EOI (or a non-RST marker) to assert JpegCorruptException from SkipRestartMarker. RestartIntervalTests only round-trips valid streams and checks marker presence.
- Integer-overflow / unbounded-allocation on attacker-controlled dimensions: no test raises MaxPixels (e.g. long.MaxValue) and decodes a crafted 65535x65535 4-component SOF. 32-bit geometry math (SetupGeometry, AssembleCmyk width*height*4) wraps negative and 'new byte[negative]' throws OverflowException/OutOfMemoryException — a NON-JpegException — which no test currently catches. DecoderOptionsTests only exercises small MaxPixels values.
- Standalone marker (RSTn 0xD0-0xD7 or TEM 0x01) appearing in the header region before SOS: no integration test. ParseHeaders always calls ReadSegment without consulting JpegMarkers.HasLengthField, so the next two bytes are misread as a length, desyncing the parser. Only the pure classification unit (MarkerTests.HasLengthField_Classifies) exists.
- End-of-data / EOI completeness on a truncated baseline scan with no restart interval: existing truncation tests only assert 'no non-Jpeg exception / no hang'; they accept a silently-completed corrupt image. No test asserts a typed error (or completeness check) when the entropy stream is truncated mid-scan and MCUs are filled from padding bits.
- Top-level normalization of unexpected exceptions to JpegException on the not-yet-fuzzed edge (the int-overflow allocation path, ReadMetadata parsing edges): fuzz tests prove typed failure over broad byte mutations but not for the specific overflow path, where a non-JpegException still escapes.
- Segment-level random-length/field fuzzing of DQT/DHT/SOF/SOS (random lengths, zero components, oversubscribed Huffman counts, truncated symbol lists) — only single-byte header-position mutation fuzzing (HeaderFuzzTests) exists; structured field/length randomization is untested.
- SOF with Y=0 defined later via a DNL marker (T.81 B.2.5): unsupported; only the safe-rejection path (CorruptionTests.ZeroDimensions) is covered. Spec-completeness limitation, not a safety hole.

**Required new tests:**
- `RestartMarkerDesync_WrongNumberedRst_ThrowsCorruptException` (decoder, ITU-T T.81 E.1.4 / F.1.4 (restart markers cycle RSTm, m = 0..7 mod 8)): Encode a grayscale image with RestartInterval=1, locate an RSTn in the entropy stream and rewrite it to a wrong number (e.g. RST0 -> RST3) so the modulo-8 restart sequence is violated; assert a JpegCorruptException. Guards the major desync finding — SkipRestartMarker must verify the expected restart index, not merely that some RST0-RST7 is present. → Jpeg.Decode throws JpegCorruptException (desync detected). Currently FAILS: BitReader.SkipRestartMarker accepts any RSTn, so decode returns silent garbage — this test documents required behavior and will fail until the index check is added.
- `MissingRestartMarker_ImmediateEoiAtBoundary_ThrowsCorruptException` (decoder, ITU-T T.81 F.1.4 / B.2.1 (restart marker required at each restart interval boundary)): Encode with a restart interval, truncate/overwrite the entropy so the first restart boundary is reached with an immediate EOI (or a non-RST marker) instead of an RSTn; assert JpegCorruptException from SkipRestartMarker. Proves the 'expected restart marker missing' path. → Jpeg.Decode throws JpegCorruptException ('Expected a restart marker in the entropy stream').
- `MaxPixelsRaised_HugeFourComponentSof_ThrowsJpegException` (decoder, T.81 B.2.2 (dimensions up to 65535x65535); robustness: typed failure + bounded allocation): Hand-build a CMYK (4-component) SOF header at 65535x65535 and decode with JpegDecoderOptions.MaxPixels=long.MaxValue so the pixel-budget guard is bypassed; assert only a JpegException subtype escapes — never OverflowException or OutOfMemoryException from 32-bit width*height*4 wrapping to a negative 'new byte[...]'. → Jpeg.Decode throws a JpegException subtype. Currently FAILS: int overflow yields OverflowException/OutOfMemoryException (non-JpegException); requires a checked/long-based geometry guard.
- `StandaloneMarkerBeforeSos_ThrowsJpegFormatException` (decoder, T.81 B.1.1.3 / Table B.1 (standalone markers carry no length field)): Insert a standalone marker with no length field (RSTn 0xD0 or TEM 0x01) into the header region before SOS of a valid stream, then decode; assert a typed JpegFormatException rather than a misparsed segment length, IndexOutOfRangeException, or ArgumentException. Exercises the HasLengthField gap in ParseHeaders end-to-end. → Jpeg.Decode throws JpegFormatException (or a subtype); no non-JpegException escapes.
- `TruncatedBaselineScan_NoRestart_FailsTypedOnMissingEoi` (decoder, T.81 B.2.1 (compressed image data is terminated by EOI)): Encode a baseline image with no restart interval, cut the stream mid-entropy (EOI absent) and decode; assert the decoder signals incompleteness with a JpegException subtype rather than silently returning a fully-formed image assembled from padding bits. Adds the end-of-data completeness check the current 'no-crash' truncation tests do not enforce. → Jpeg.Decode throws a JpegException subtype (e.g. JpegCorruptException) indicating truncated entropy. Likely FAILS today: FillByte supplies 1-bit padding and a corrupt image is returned; requires an EOI/end-of-data guard.
- `SegmentFieldFuzz_OnlyJpegExceptionsEscape` (decoder, T.81 B.2 (segment structure); robustness: typed failure on malformed segments): Randomize the length prefix and field bytes of the DQT, DHT, SOF, and SOS segments of encoded gray/RGB/CMYK/progressive fixtures across many seeds (invalid table ids, zero components, sampling 0 and 5, oversubscribed Huffman counts, truncated symbol lists) and decode each; assert only JpegException subtypes escape. Extends the single-byte HeaderFuzzTests to structured segment-length/field fuzzing. → For every mutation, Jpeg.Decode either returns or throws a JpegException subtype; any other exception type fails the test.
- `DcScanWithNonZeroSe_AndAcScanMultiComponent_AreRejected_Regression` (regression, T.81 G.1.1.1.1 (Ss<=Se<=63; DC scans Ss=Se=0; AC scans single-component)): Consolidated regression guard that the four progressive spectral-selection rules (Ss>Se, Se>63, DC scan Se!=0, AC scan Ns>1) each throw JpegFormatException, pinning the behavior already implemented so future refactors cannot silently regress it. → Each malformed progressive SOS causes Jpeg.Decode to throw JpegFormatException (passes today; locks in existing coverage).

