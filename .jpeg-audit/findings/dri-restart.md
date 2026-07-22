# DRI interval + RSTn restart resync

**Section key:** `dri-restart`  
**Compliance:** PARTIAL · **Adversarial justified:** n/a · **Final:** **PARTIAL**

## Findings

- **[major]** Decoder must handle missing/misordered RST gracefully.
  - Observed: SkipRestartMarker (BitReader.cs:129-138) scans forward for the next 0xFF-run and, if the following byte is not 0xD0-0xD7 or the data ends, throws JpegCorruptException (BitReader.cs:134-135). There is no attempt to resync/continue (e.g. reset predictors and proceed) on a missing or damaged restart marker, so a single lost/corrupt RSTn aborts the whole decode. This is inconsistent with the decoder's otherwise-lenient truncation handling (BitReader.ReadBits pads with 1-bits at EOF/markers).
  - Spec: ITU-T T.81 F.1.2.3 / E.1.1 (restart markers, graceful resync)
- **[minor]** Decoder must expect RSTm, cycling RST0..RST7 modulo 8.
  - Observed: The restart handler never tracks or validates the expected modulo-8 marker number. SkipRestartMarker accepts ANY marker in 0xD0-0xD7 (BitReader.cs:134) and DecodeScan (BaselineDecoder.cs:421-425) never computes an expected RSTm. A misordered, skipped, or duplicated RST (e.g. RST3 where RST0 is expected) is silently accepted, so entropy-stream desynchronization goes undetected and produces silent garbage instead of a diagnosable error.
  - Spec: ITU-T T.81 F.1.2.3 (RSTn cycles 0..7 mod 8)
- **[minor]** Decoder must discard partial bits and byte-align before the restart marker.
  - Observed: SkipRestartMarker discards the whole bit buffer (_buffer=0;_count=0, BitReader.cs:125-126) then LINEAR-SCANS forward for the next 0xFF (BitReader.cs:129-132) instead of asserting the marker is at the immediate next byte boundary. Because _pos may point at the padded final byte, this works for well-formed streams, but on desynced data the scan can silently skip an arbitrary run of entropy bytes (a whole interval's data) and latch onto a later RST marker, masking the corruption rather than reporting it. A byte-aligned expectation (align, then require 0xFF Dn at the current position) would be spec-truer.
  - Spec: ITU-T T.81 B.1.1.5 / F.1.2.3 (marker at padded byte boundary)
- **[info]** Consistent restart-resync implementation.
  - Observed: Two divergent restart routines exist: the baseline path uses forward-scanning SkipRestartMarker (BitReader.cs:122-138), while ResetForRestart (BitReader.cs:107-114) assumes the marker was already reached and blindly does _pos+=2. Baseline never validates MarkerReached/Marker before resync, so the two mechanisms encode different assumptions about stream state; a future caller mixing them could advance past the wrong byte.
  - Spec: ITU-T T.81 F.1.2.3

## Required fixes

- Track the expected restart marker number (restartCount mod 8) across the MCU loop and compare against the RSTn actually found in SkipRestartMarker; on mismatch, surface a diagnosable condition (option-gated warning/exception) instead of silently accepting any RST0-RST7.
- Replace the unbounded forward-scan in SkipRestartMarker with byte-alignment plus a bounded expectation: after AlignToByte, require 0xFF followed by 0xD0-0xD7 at the current byte boundary; only consume fill (0xFF) runs, not arbitrary entropy bytes.
- Handle a missing/damaged RST marker gracefully to match the decoder's lenient truncation behavior: reset DC predictors, clear the marker state, and continue (or fail only when JpegDecoderOptions requests strict mode) rather than unconditionally throwing JpegCorruptException.
- Unify the two restart routines (SkipRestartMarker vs ResetForRestart) or document which state (MarkerReached/BytePosition) each requires, so baseline and progressive paths cannot advance past the wrong byte.

## Code locations

- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:182-186 (DRI parse; Ri = (segment[0]<<8)|segment[1])`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:421-425 (restart trigger: _restartInterval>0 && mcuCount%Ri==0; SkipRestartMarker + Array.Clear(predictors))`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Bitstream/BitReader.cs:122-138 (SkipRestartMarker: forward-scan resync used by baseline)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Bitstream/BitReader.cs:107-114 (ResetForRestart: alternate marker-driven resync, unused by baseline)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Bitstream/BitReader.cs:155-185 (FillByte: 0xFF stuffing + marker barrier that bounds _pos)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Coding/BlockScanCoder.cs:91-133 (DecodeBlock: DC predictor threaded through restart resets)`

## Test coverage

**Existing:**
- RestartIntervalTests.cs::Grayscale_WithRestartInterval_RoundTrips — proves well-formed encode+decode resync and implicit DC-predictor reset for Ri=1,2,3,5 (grayscale, 1 comp/MCU)
- RestartIntervalTests.cs::Rgb420_WithRestartInterval_RoundTrips — proves resync/predictor reset across interleaved multi-block MCUs (4:2:0) for Ri=1,4
- RestartIntervalTests.cs::Output_ContainsDriAndRestartMarkers — proves encoder emits the DRI (0xFFDD) segment and at least one RST0-RST7 marker
- RestartIntervalTests.cs::RestartInterval_LargerThanMcuCount_StillDecodes — proves Ri greater than total MCUs emits no restart marker and still decodes (SkipRestartMarker never invoked)
- RestartIntervalTests.cs::RestartEncoding_IsDeterministic — proves restart-marker emission is byte-deterministic
- BitstreamTests.cs::ResetForRestart_SkipsMarkerAndContinues — proves BitReader.ResetForRestart clears a reached RSTn, advances _pos+=2, and resumes reading following entropy (note: this is the divergent path NOT used by baseline DecodeScan)
- BitstreamTests.cs::Marker_LeavesBytePositionAtMarkerStart — proves BytePosition points at the 0xFF preceding an RST0 code on marker detection
- BitstreamTests.cs::Marker_StopsBitConsumptionAndIsExposed — proves marker detection halts bit consumption and exposes Marker/MarkerReached (EOI case)
- BitstreamTests.cs::FillBytes_BeforeMarkerAreSkipped — proves runs of 0xFF fill bytes before a marker are skipped
- BitstreamTests.cs::ByteStuffing_Ff00_DecodesToSingleFfByte — proves 0xFF00 stuffing decodes to a single 0xFF data byte without marker detection
- BitstreamTests.cs::AlignToByte_DiscardsPartialBits — proves partial-bit discard / byte alignment (the pre-marker alignment primitive)
- BoundaryFieldTests.cs::RestartInterval_Above16Bit_Throws — proves Ri>65535 is rejected at encode (DRI field is 16-bit)
- BoundaryFieldTests.cs::RestartInterval_AtMax_EncodesAndDecodes — proves Ri=65535 (field max) round-trips on a small image
- MarkerTests.cs::IsRestartMarker_Classifies — proves 0xD0/0xD7 classify as restart markers and neighbors do not
- MarkerTests.cs::(DefineRestartInterval constant assertion) — proves JpegMarkers.DefineRestartInterval == 0xDD
- ProgressiveTests.cs::Progressive_WithRestartInterval_RoundTrips — proves restart resync on the progressive entropy path (Ri=3)
- ProgressiveRefinementTests.cs::Progressive_WithRestart_MatchesBaseline_WithSuccessiveApproximation — proves progressive+restart+successive-approx matches baseline (Ri=4)
- ApiCoverageTests.cs::ColorProgressive_WithRestart_MatchesBaseline — proves color progressive+restart matches baseline (Ri=5, 4:2:0)
- CombinationTests.cs::CustomHuffman_WithRestart_RoundTrips — proves restart resync with custom Huffman tables (Ri=5)
- CombinationTests.cs::Ycck_WithOptimizeHuffmanAndRestart_RoundTrips — proves restart resync with YCCK + optimized Huffman (Ri=3)
- OptionInteractionTests.cs::OptimizeHuffman_WithRestart_RoundTrips — proves restart resync with optimized Huffman tables (Ri=4)
- ScaleTests.cs::LargeGrayscale_WithRestart_RoundTrips — proves resync over many intervals on a large image (Ri=17)

**Gaps:**
- Decoder must handle a MISSING or CORRUPTED RSTn gracefully (major finding): no test deletes/overwrites an RSTn and asserts recovery. Current SkipRestartMarker throws JpegCorruptException (BitReader.cs:134-135) — no test even pins this current behavior, and none demonstrates the desired graceful-resync behavior.
- Decoder must validate the expected modulo-8 RSTm number (minor finding): no test injects a misnumbered/misordered/duplicated RST (e.g. RST5 where RST0 is expected). Current code accepts any 0xD0-0xD7 silently (BitReader.cs:134); neither the current silent-accept behavior nor a desired detection is tested.
- Ri=0 disables restarts: no explicit test asserts that a stream with DRI=0 (or no DRI) never calls SkipRestartMarker and never throws. Only implicitly exercised because RestartInterval defaults to 0 in every non-restart test; the disable path is never directly asserted.
- Byte-alignment / stuffing immediately before RST (minor finding): no test crafts an entropy byte that pads to 0xFF forcing 0xFF00 stuffing directly before 0xFFDn, to verify resync consumes the stuffing and latches the correct RST at the byte boundary.
- Forward-scan over-run masking corruption (minor finding): no test proves SkipRestartMarker's linear 0xFF scan (BitReader.cs:129-132) skips an arbitrary run of entropy bytes on desynced data instead of asserting the marker at the immediate byte boundary.
- Truncation exactly at a restart boundary: no test truncates a stream at the end of a full interval (no bytes after) to exercise the _pos>=_data.Length branch (BitReader.cs:134) for graceful handling.
- Explicit DC-predictor reset assertion at a restart boundary: predictor reset is only proven implicitly via lossy pixel round-trips; no test isolates and asserts predictors are zeroed exactly at each RSTn.
- Ri boundary values MCUsPerRow and exactly total-MCU-count: tested Ri values are 1,2,3,4,5,17,10000,65535 — the row-aligned (Ri==MCUsPerRow) and whole-image (Ri==totalMCUs, single final restart never emitted) boundaries are not specifically covered.
- Fuzz: random RST injection mid-interval with bounded resync (minor/info): no fuzz test injects spurious 0xFFDn markers inside an interval to confirm no unhandled exception and bounded (non-arbitrary) resync.
- Consistency of the two restart routines (info finding): no test guards that baseline DecodeScan uses SkipRestartMarker (forward scan) vs ResetForRestart (blind _pos+=2) — divergent state assumptions are unverified against a common stream.

**Required new tests:**
- `Decode_MissingRestartMarker_RecoversGracefully` (decoder, ITU-T T.81 F.1.2.3 / E.1.1 (graceful restart resync)): Encode with Ri=2, delete one RSTn marker pair from the entropy stream, decode. Pin current behavior (throws JpegCorruptException) and, after the fix, assert graceful recovery: no crash and a defined full-size output (predictors reset, decode continues). → CURRENT: JpegCorruptException from SkipRestartMarker (BitReader.cs:134-135). DESIRED (post-fix): decode returns a full-dimension image without throwing; downstream intervals may be degraded but bounded.
- `Decode_CorruptedRestartMarker_DoesNotAbortWholeDecode` (decoder, ITU-T T.81 F.1.2.3 (missing/misordered RST handled gracefully)): Overwrite an RSTn code byte with a non-restart value (e.g. 0xD0->0xE0) so the marker is damaged; assert the decoder does not abort the entire image on a single damaged restart. → CURRENT: JpegCorruptException (byte not in 0xD0-0xD7). DESIRED: no throw; bounded resync to the next valid RST, remaining MCUs decoded.
- `Decode_MisnumberedRestartMarker_IsDetected` (decoder, ITU-T T.81 F.1.2.3 (RSTn cycles 0..7 mod 8)): Rewrite a well-formed stream so an RST arrives out of the expected modulo-8 sequence (e.g. RST5 where RST0 is expected). Document that the current decoder silently accepts any 0xD0-0xD7 and produces silent garbage; assert the desired detectable diagnostic after adding expected-RSTm tracking. → CURRENT: silently accepted, no error (regression guard on the known defect). DESIRED: a diagnosable JpegCorruptException or explicitly-flagged resync rather than silent desync.
- `Decode_RestartIntervalZero_DisablesRestartsNoException` (round-trip, ITU-T T.81 B.2.4.4 (Ri=0 disables restarts)): Encode with no restart interval (Ri=0 / DRI absent) and decode; assert no RST marker is present, SkipRestartMarker is never reached, and decode succeeds pixel-close. → No 0xFFDn markers in the stream; clean decode equal to the non-restart baseline; no exception.
- `Decode_StuffedFfBeforeRestartMarker_ResyncsCorrectly` (decoder, ITU-T T.81 B.1.1.5 / F.1.2.3 (stuffing then marker at padded byte boundary)): Craft/select an interval whose final entropy byte is 0xFF (emitting 0xFF00 stuffing) immediately before 0xFFDn, so SkipRestartMarker must step over the stuffing to find the true RST at the byte boundary. → Resync consumes the 0x00 stuffing, latches the correct RSTn, and the image decodes pixel-identical to a stream without the boundary stuffing.
- `Decode_TruncatedAtRestartBoundary_HandledGracefully` (decoder, ITU-T T.81 F.1.2.3 (truncation at restart boundary)): Truncate the stream at the end of a complete restart interval (immediately after an RST, no following entropy or the RST itself missing) and decode; exercise the _pos>=_data.Length branch. → CURRENT: JpegCorruptException at BitReader.cs:134. DESIRED: consistent with lenient EOF handling — decode returns a defined partial/full-size image without an unhandled crash.
- `RoundTrip_RestartIntervalAtRowAndTotalMcuBoundaries` (round-trip, ITU-T T.81 F.1.2.3 (Ri MCUs between RSTn)): Round-trip with Ri set to exactly MCUsPerRow and to the exact total MCU count for a known image geometry; assert pixel-close output and correct behavior at row-aligned and whole-image boundaries (final interval emits no trailing RST). → Both configurations decode pixel-close to the source; no spurious trailing RST after the last MCU.
- `Decode_DcPredictorResetAtRestart_IsExact` (decoder, ITU-T T.81 F.1.2.3 (reset DC predictors to 0 at RSTn)): Build a controlled stream (e.g. flat/DC-only blocks) with Ri set so a boundary falls between blocks of differing DC; assert the decoded DC after the RST reflects a predictor reset to 0 rather than carrying the prior interval's predictor. → DC values in the first block after each RST decode as if the predictor were 0; mismatch would prove a missed reset.
- `Decode_FuzzRandomRestartInjection_BoundedResync` (regression, ITU-T T.81 F.1.2.3 (robust restart handling)): Fuzz: inject spurious 0xFFDn markers at random mid-interval offsets across many seeds; assert every case terminates with either a defined JpegFormatException or a bounded-size decode, never an unhandled exception or arbitrary data skip. → No unhandled exceptions (only JpegFormatException/JpegCorruptException) and output dimensions stay bounded to the frame; resync never silently swallows an arbitrary run of entropy bytes.
- `SkipRestartMarker_DoesNotOverrunToLaterRstOnDesync` (decoder, ITU-T T.81 B.1.1.5 / F.1.2.3 (marker at padded byte boundary)): Unit-test BitReader.SkipRestartMarker directly with a buffer containing extra entropy bytes and multiple 0xFFDn markers; assert it stops at the first byte-aligned restart rather than linear-scanning past an interval's data to a later marker. → CURRENT: forward scan may skip to a later 0xFF run (documents the over-run defect). DESIRED: consumes exactly to the immediate byte-aligned RST, BytePosition advanced by one marker only.

