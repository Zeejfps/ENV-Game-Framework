# Marker structure & segment framing (SOI/EOI/length/stuffing)

**Section key:** `marker-framing`  
**Compliance:** PARTIAL · **Adversarial justified:** n/a · **Final:** **PARTIAL**

## Findings

- **[info]** Markers are 0xFF+code; only 0xFF fill bytes may precede a marker code.
  - Observed: MarkerReader.ReadMarker (MarkerReader.cs:29-47) requires the first byte to be exactly 0xFF (line 34-35), then loops skipping any additional 0xFF fill bytes (line 38-44) before returning the code. Non-0xFF leading bytes are rejected. Correct.
  - Spec: T.81 B.1.1.2 (marker = 0xFF + code, 0xFF fill allowed)
- **[info]** SOI must start the compressed data stream.
  - Observed: RequireSoi (BaselineDecoder.cs:148-152) calls ReadMarker and throws JpegFormatException if the first marker is not 0xD8. Fill 0xFF bytes before SOI are tolerated by ReadMarker. Correct.
  - Spec: T.81 B.2.1 / B.1.1.3 (SOI begins interchange format)
- **[info]** Non-standalone segments carry a 2-byte big-endian length that INCLUDES the length bytes; reject length < 2.
  - Observed: ReadSegment (MarkerReader.cs:67-76) reads a big-endian ushort via ReadUInt16 (line 52-58), throws when length < 2 (line 70-71), and allocates a payload of length-2 (line 73). Big-endian and inclusive-length semantics are correct.
  - Spec: T.81 B.1.1.4 (Lp includes the two length bytes, minimum 2)
- **[info]** Reject a segment length that overruns the buffer.
  - Observed: ReadSegment fills the payload via ReadExact (MarkerReader.cs:81-91), which loops on Stream.Read and throws JpegFormatException when the stream ends before the buffer is filled (line 87-88). Over a MemoryStream wrapping the input buffer, an over-long length correctly throws. Correct.
  - Spec: T.81 B.1.1.4 (segment must fit within the data)
- **[minor]** Standalone markers (SOI/EOI/RSTn/TEM) have no length field and must not be read as length-prefixed segments.
  - Observed: JpegMarkers.HasLengthField (JpegMarkers.cs:93-98) correctly classifies standalone markers, but a repo-wide grep shows it (and IsRestartMarker for this purpose) is never called. ParseHeaders (BaselineDecoder.cs:154-219) special-cases only EOI (line 159) and SOS (line 162); for every other marker it unconditionally calls reader.ReadSegment() (line 168). If a standalone marker such as RSTn (0xD0-0xD7) or TEM (0x01) appeared in segment position, its following two bytes would be misinterpreted as a length and payload, desynchronizing the parser instead of being handled/rejected as length-less. In normal streams these markers do not appear at header level (RSTn live inside entropy data, consumed by BitReader.SkipRestartMarker), so impact is limited to malformed input.
  - Spec: T.81 B.1.1.3 (SOI/EOI/RSTn/TEM are standalone, no parameters)
- **[minor]** EOI ends the compressed data; a well-formed stream terminates with EOI.
  - Observed: The baseline path never requires a trailing EOI. DecodeScan (BaselineDecoder.cs:402-451) drives entropy decoding purely by the MCU grid count (mcusPerRow x mcusPerCol) over a BitReader on data.AsSpan(entropyStart) (line 408); it stops when the grid is exhausted and does not verify that an EOI marker follows or that no stray data precedes it. A truncated stream missing EOI, or trailing garbage after EOI, is not detected. ParseHeaders does reject EOI encountered before any scan (line 159-160), which is correct.
  - Spec: T.81 B.2.1 / B.1.1.3 (EOI terminates the image)
- **[info]** Robust EOF handling when reading the 2-byte length.
  - Observed: ReadUInt16 (MarkerReader.cs:52-58) reads hi then lo but only checks lo < 0 before returning (hi<<8)|lo. Functionally safe because once a stream hits EOF ReadByte keeps returning -1, so a missing hi byte forces lo < 0 and throws; however the hi < 0 case is not checked explicitly, which is fragile if the helper is ever reused over a non-EOF-sticky stream.
  - Spec: T.81 B.1.1.4 (length field integrity)

## Required fixes

- In ParseHeaders (BaselineDecoder.cs:154-219), guard the reader.ReadSegment() call at line 168 with JpegMarkers.HasLengthField(marker): treat standalone markers (RSTn/TEM) encountered in segment position as an error or skip them without consuming a bogus length, instead of unconditionally reading a length-prefixed segment.
- Actually use the HasLengthField/IsRestartMarker classifiers (currently dead code) to drive segment framing, or remove them if intentionally unused, so marker classification is single-sourced.
- Add explicit EOF check for the high byte in ReadUInt16 (MarkerReader.cs:52-58): throw if hi < 0 as well as lo < 0, so the helper is correct independent of the underlying stream's EOF stickiness.
- Optionally validate stream termination in the baseline path: after DecodeScan consumes the entropy data, confirm the trailing marker is EOI so truncated streams / trailing garbage are surfaced rather than silently ignored.

## Code locations

- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Markers/MarkerReader.cs:29 ReadMarker (0xFF prefix + fill-byte skipping)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Markers/MarkerReader.cs:52 ReadUInt16 (big-endian length)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Markers/MarkerReader.cs:67 ReadSegment (length>=2, payload=length-2)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Markers/MarkerReader.cs:81 ReadExact (buffer-overrun rejection)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Markers/JpegMarkers.cs:93 HasLengthField (defined but unused)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Markers/JpegMarkers.cs:74 IsRestartMarker`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:148 RequireSoi`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:154 ParseHeaders (marker dispatch loop)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:402 DecodeScan (grid-driven, no EOI check)`

## Test coverage

**Existing:**
- MarkerTests.cs::Reader_ReadsMarkersAndSegmentPayload — proves ReadSegment interprets a 2-byte big-endian length that INCLUDES the length bytes (declares 0x0004, yields 2-byte payload) [T.81 B.1.1.4]
- MarkerTests.cs::Reader_ReadUInt16_IsBigEndian — proves the length field is decoded big-endian (0x12 0x34 -> 0x1234) [T.81 B.1.1.4]
- MarkerTests.cs::Reader_SegmentLengthTooSmall_Throws — proves ReadSegment rejects a declared length < 2 (0x0001) [T.81 B.1.1.4]
- MarkerTests.cs::Reader_TruncatedPayload_Throws — proves ReadExact throws when a declared length overruns the available bytes (declares 4 payload bytes, 2 present) [T.81 B.1.1.4]
- MarkerTests.cs::Reader_SkipsFillBytesBeforeMarker — proves 0xFF fill bytes preceding a marker code are skipped (FF FF FF D8 -> 0xD8) [T.81 B.1.1.2]
- MarkerTests.cs::Reader_NonFfWhereMarkerExpected_Throws — proves a non-0xFF byte in marker position is rejected (0x12) [T.81 B.1.1.2]
- MarkerTests.cs::HasLengthField_Classifies — proves the classification helper marks SOI/EOI/RST0/TEM as length-less and SOF0/SOS as length-bearing (helper unit only, not wired into the parser) [T.81 B.1.1.3]
- MarkerTests.cs::IsRestartMarker_Classifies — proves RSTn (0xD0-0xD7) classification [T.81 B.1.1.3]
- MarkerTests.cs::Constants_HaveExpectedValues — pins SOI=0xD8, EOI=0xD9 and other marker code constants [T.81 B.1.1.3]
- MarkerTests.cs::Writer_WritesStandaloneMarker — proves standalone SOI is emitted as 0xFF 0xD8 with no length field (encoder side of standalone framing) [T.81 B.1.1.3]
- MarkerTests.cs::Writer_WritesSegmentWithLengthPrefix — proves length-bearing segments are emitted with an inclusive big-endian length (3 payload bytes -> 0x0005) [T.81 B.1.1.4]
- MarkerTests.cs::ReaderWriter_RoundTrip — proves a SOI/APP0/COM/EOI marker+segment stream round-trips through writer and reader [T.81 B.1.1.2-B.1.1.4]
- CorruptionTests.cs::MissingSoi_ThrowsFormatException — proves RequireSoi rejects a stream whose first marker is not SOI (input FF D9) [T.81 B.2.1]
- CorruptionTests.cs::RandomBytes_ThrowsFormatException — proves a stream not beginning with a 0xFF marker prefix (leading 0x12) is rejected [T.81 B.1.1.2]
- CorruptionTests.cs::TruncatedAfterSoi_ThrowsFormatException — proves a stream that is only SOI (FF D8) with no following markers is rejected [T.81 B.2.1]
- CorruptionTests.cs::TruncatedSegment_ThrowsFormatException — proves an over-long segment length (APP0 declares 0x64 with no payload) is rejected at full-decode integration level [T.81 B.1.1.4]
- CorruptionTests.cs::EmptyInput_ThrowsFormatException — proves an empty buffer is rejected before any marker read [T.81 B.1.1.3]
- StructureComplianceTests.cs::EverySegmentLength_MatchesItsPayload — proves the encoder emits every length-bearing segment with length >= 2 and in-bounds (encoder side of B.1.1.4) [T.81 B.1.1.4]
- StructureComplianceTests.cs::Baseline_HasExpectedMarkerOrderAndSegments — proves encoder output starts with SOI and ends with EOI [T.81 B.2.1]
- TrailingDataTests.cs::GarbageAfterEoi_IsIgnored — documents current behavior: random trailing bytes after EOI are ignored, decode matches reference [T.81 B.2.1]
- TrailingDataTests.cs::FillBytesAfterEoi_AreIgnored — documents current behavior: 0xFF fill after EOI is ignored [T.81 B.2.1]
- TrailingDataTests.cs::AnotherFullJpegAppended_DecodesTheFirst — documents that a second concatenated image after EOI is ignored [T.81 B.2.1]
- HeaderFuzzTests.cs::CorruptingAnyHeaderByte_NeverThrowsNonJpegException — proves single-byte corruption of any header byte (including length/marker bytes) never surfaces a non-JpegException [T.81 B.1.1.4 robustness]
- CorruptionTests.cs::TruncatedStream_NeverThrowsNonJpegException — proves every truncation prefix fails cleanly as JpegException (does NOT assert missing-EOI is surfaced as an error) [T.81 B.2.1 robustness]

**Gaps:**
- Standalone marker (RSTn 0xD0-0xD7 or TEM 0x01) appearing in header/segment position is not tested against the parser. HasLengthField/IsRestartMarker are unit-tested but never wired into ParseHeaders, which unconditionally calls ReadSegment for every non-EOI/non-SOS marker; no test proves a stray standalone marker is handled as length-less rather than misframing the following two bytes as a length. [T.81 B.1.1.3]
- EOI encountered before any scan data (SOI present, then FF D9 before any SOS) is not tested. Existing MissingSoi_ThrowsFormatException feeds FF D9 as the FIRST marker (hits RequireSoi), never the ParseHeaders 'Reached EOI before any scan data' branch. [T.81 B.2.1]
- Truncated length field itself (stream ends after the first of two length bytes) is not tested; Reader_TruncatedPayload covers a truncated payload but not a half-read ushort, leaving ReadUInt16's fragile hi-byte EOF path unexercised. [T.81 B.1.1.4]
- A truncated/EOI-missing stream is not asserted to surface an error. Current tests only assert no NON-Jpeg exception (and TrailingDataTests shows trailing garbage is silently ignored); decoder does not validate stream termination (no EOI requirement, no stray-data-before-scan detection). Pending the termination-validation feature noted in the compliance findings. [T.81 B.2.1]
- Missing SOI where the first marker is a valid length-bearing marker (e.g. FF C0 SOF0) is not directly tested; existing MissingSoi uses FF D9 (a standalone marker) and RandomBytes uses a non-marker leading byte, so the 'first marker is a real segment but not SOI' branch of RequireSoi is unproven. [T.81 B.2.1]

**Required new tests:**
- `Parser_StandaloneMarkerInSegmentPosition_IsRejectedNotMisframed` (decoder, T.81 B.1.1.3 (SOI/EOI/RSTn/TEM are standalone, carry no length)): Craft a header stream (SOI, DQT, then a stray standalone marker FF D0 (RST0) or FF 01 (TEM), then SOF0/SOS) and assert the parser rejects it as a JpegFormatException rather than consuming the next two bytes as a bogus length/payload and desynchronizing. Closes the gap that HasLengthField/IsRestartMarker are never consulted by ParseHeaders. → Jpeg.Decode throws JpegFormatException; the stray standalone marker does not cause its following bytes to be misinterpreted as a segment length.
- `Parser_EoiBeforeScanData_ThrowsWithScanDataMessage` (decoder, T.81 B.2.1 (EOI terminates the image; a scan must precede it)): Feed a stream that is SOI followed immediately by EOI (FF D8 FF D9), or SOI+DQT+EOI, so ParseHeaders reaches EOI before encountering any SOS, and assert it is rejected on the 'Reached EOI before any scan data' path (distinct from RequireSoi). → Jpeg.Decode throws JpegFormatException indicating EOI was reached before any scan data.
- `Reader_TruncatedLengthField_Throws` (decoder, T.81 B.1.1.4 (length field integrity / EOF while reading Lp)): Feed a stream that ends after a marker plus a single length byte (e.g. FF E0 00) and assert ReadUInt16/ReadSegment throws, exercising the hi/lo EOF path of the 2-byte length read directly rather than the payload-fill path. → MarkerReader.ReadSegment (via ReadUInt16) throws JpegFormatException on the incomplete length field.
- `Decode_TruncatedStreamMissingEoi_SurfacesError` (regression, T.81 B.2.1 (a well-formed stream terminates with EOI)): Encode a valid baseline frame, then remove the trailing EOI (and optionally cut entropy short) and assert the decoder surfaces a JpegFormatException for the unterminated stream. Guards the termination-validation behavior the compliance findings recommend adding; currently missing-EOI is silently accepted. → Once termination validation is implemented, Jpeg.Decode throws JpegFormatException for a stream lacking EOI (test may be marked pending until the feature lands).
- `RequireSoi_FirstMarkerIsSofNotSoi_Throws` (decoder, T.81 B.2.1 / B.1.1.3 (SOI must begin the interchange stream)): Feed a stream whose first marker is a valid length-bearing marker other than SOI (e.g. FF C0 ...) and assert RequireSoi rejects it, covering the branch where the first marker is a real segment rather than the EOI/non-marker cases already tested. → Jpeg.Decode throws JpegFormatException because the first marker is not SOI (0xD8).

