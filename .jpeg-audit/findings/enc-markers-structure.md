# Encoder marker emission + overall file-structure compliance

**Section key:** `enc-markers-structure`  
**Compliance:** PARTIAL · **Adversarial justified:** n/a · **Final:** **PARTIAL**

## Findings

- **[info]** T.81 B.2.1 segment ordering: SOI, [tables/misc], SOF, [tables/misc], SOS, entropy(+RSTn), EOI
  - Observed: WriteHeader (BaselineEncoder.cs:236-252) emits SOI, then APP14/APP0, APP1(Exif), APP2(ICC), COM, preserved APPn, DQT, SOF, DHT, DRI; then WriteScanHeader emits SOS, WriteEntropyData emits entropy+RSTn, then EOI (Encode:224-228). DQT precedes SOF; DHT/DRI sit between SOF and SOS. This is a valid tables/misc placement. Requirement met.
  - Spec: ITU-T T.81 B.2.1 / B.2.4
- **[info]** Segment length field correct and inclusive (Lp counts the two length bytes, not the marker)
  - Observed: MarkerWriter.WriteSegment (MarkerWriter.cs:42-50) writes length = payload.Length + 2 and guards payload > 65533 with JpegFormatException. Correct and inclusive. DHT payload buffer sized 1+16+256 (BaselineEncoder.cs:522) cannot overflow. Requirement met.
  - Spec: ITU-T T.81 B.1.1.4
- **[info]** SOF precision/component fields consistent with data; DQT/DHT emitted before referenced
  - Observed: WriteFrameHeader (BaselineEncoder.cs:486-505) writes precision=_precision (8 or 12), 16-bit height/width, n components, per-component Id/(H<<4|V)/QuantId. Frame marker SOF0 for 8-bit, SOF1 for 12-bit (Encode:221-223). SOS selectors (WriteScanHeader:541-542) use TableClass as both DC/AC id, matching DHT ids written in WriteHuffmanTables (510-516). DQT written before SOF, DHT before SOS. YCCK/CMYK/RGB-direct table classes are internally consistent. Requirement met.
  - Spec: ITU-T T.81 B.2.2 / B.2.3
- **[minor]** DRI (restart interval) segment must carry the correct 16-bit interval value
  - Observed: The 8-bit constructor validates RestartInterval <= 65535 (BaselineEncoder.cs:62-63), but the 12-bit constructor (JpegImage16, lines 128-189) performs NO such validation. WriteDri (BaselineEncoder.cs:441-445) masks the interval to 16 bits (>>8 and &0xFF). A 12-bit encode with RestartInterval > 65535 would silently emit a truncated/incorrect DRI segment rather than throwing, producing a stream whose restart cadence disagrees with the emitted RSTn markers.
  - Spec: ITU-T T.81 B.2.4.4
- **[minor]** Exif (T.872) requires the APP1 Exif segment immediately after SOI; JFIF (T.871) requires APP0 JFIF immediately after SOI — the two are mutually exclusive as the first marker
  - Observed: For the YCbCr path WriteHeader emits WriteJfif (APP0) first, then WriteExif (APP1) (BaselineEncoder.cs:242-243). For Adobe paths it emits APP14 first, then APP1 Exif (241,243). When Exif metadata is present the Exif APP1 is therefore never the first marker after SOI, violating strict T.872 ordering (and pairing JFIF+Exif, which each spec disallows). Widely tolerated by libjpeg-family decoders, so not a decode blocker.
  - Spec: ITU-T T.872 (Exif) 4.5.4 / T.871 (JFIF) 6
- **[info]** Entropy segment: 1-bit padding of final partial byte and 0x00 stuffing after 0xFF; RSTn between intervals only, none trailing
  - Observed: BitWriter.Flush pads remaining bits with 1s (BitWriter.cs:47-56) and WriteBits inserts a 0x00 after each 0xFF data byte (BitWriter.cs:39-40). WriteEntropyData (BaselineEncoder.cs:415-422) flushes then writes 0xFF/RSTn only before the block that opens a new interval (mcuIndex>0 && mcuIndex%interval==0) and resets predictors; no restart marker is emitted after the final MCU. Requirement met.
  - Spec: ITU-T T.81 B.1.1.5 / F.1.2.3
- **[info]** Single-segment metadata (Exif) size limit handling
  - Observed: WriteExif builds a 6+exif.Length payload and relies on WriteSegment to throw when it exceeds 65533 (BaselineEncoder.Metadata.cs:33-41). ICC is correctly chunked across APP2 segments with sequence/count bytes and a >255-chunk guard (44-67). Exif has no multi-segment fallback, but a single APP1 is inherently 64KB-limited, so throwing is acceptable.
  - Spec: ITU-T T.81 B.1.1.4 / T.872

## Required fixes

- Validate RestartInterval <= MaxDimension (65535) in the JpegImage16/12-bit constructor (BaselineEncoder.cs:128-189), mirroring the 8-bit constructor check at lines 62-63, so WriteDri (441-445) cannot silently truncate the DRI value.
- (Optional, strict metadata compliance) When Exif metadata is present, emit the Exif APP1 segment as the first marker after SOI and suppress the JFIF APP0 (or document the JFIF+Exif co-emission as an intentional deviation), per T.872/T.871 first-marker requirements.

## Code locations

- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.cs:203-229 (Encode: overall marker sequence + EOI)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.cs:236-252 (WriteHeader: SOI..DRI ordering)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.cs:447-484 (WriteQuantTables: DQT 8/16-bit)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.cs:486-505 (WriteFrameHeader: SOF)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.cs:507-530 (WriteHuffmanTables/WriteDht: DHT)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.cs:532-549 (WriteScanHeader: SOS)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.cs:399-439 (WriteEntropyData: RSTn + predictor reset)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.cs:441-445 (WriteDri) + 62-63/128-189 (RestartInterval validation gap in 12-bit ctor)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Markers/MarkerWriter.cs:42-50 (WriteSegment length field)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Bitstream/BitWriter.cs:33-56 (byte stuffing + 1-bit pad)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.Metadata.cs:10-99 (JFIF/Exif/ICC/COM/Adobe segments)`

## Test coverage

**Existing:**
- StructureComplianceTests.cs::Baseline_HasExpectedMarkerOrderAndSegments — proves SOI first, EOI last, DQT precedes SOF0, SOF0 precedes SOS, APP0(JFIF) and DHT present (T.81 B.2.1 ordering, partial)
- StructureComplianceTests.cs::Sof0_EncodesDimensionsAndComponentCount — proves SOF precision=8, 16-bit height/width, component count, and per-component sampling factor consistent with data (B.2.2)
- StructureComplianceTests.cs::EverySegmentLength_MatchesItsPayload — walks all length-bearing segments and asserts each length >=2 and does not overrun the stream (B.1.1.4 inclusive length, in-bounds)
- StructureComplianceTests.cs::Progressive_UsesSof2AndMultipleScans — proves progressive uses SOF2 (no SOF0) and emits multiple SOS scans
- MarkerTests.cs::Writer_WritesSegmentWithLengthPrefix — proves MarkerWriter emits length = payload+2 (inclusive Lp) for a known payload (B.1.1.4)
- MarkerTests.cs::Constants_HaveExpectedValues / IsStartOfFrame_Classifies / HasLengthField_Classifies / IsRestartMarker_Classifies — proves marker-code constants and classification helpers used throughout the writer
- MarkerTests.cs::ReaderWriter_RoundTrip — proves SOI/APPn/COM/EOI segment write+read round-trip with correct length framing
- RestartIntervalTests.cs::Output_ContainsDriAndRestartMarkers — proves a DRI (0xDD) segment and at least one RSTn (0xD0-0xD7) marker are emitted when RestartInterval is set (B.2.4.4, presence only)
- RestartIntervalTests.cs::Grayscale_WithRestartInterval_RoundTrips / Rgb420_WithRestartInterval_RoundTrips — prove restart-interval streams decode correctly (predictor reset implied by matching pixels), self-decoder only
- RestartIntervalTests.cs::RestartInterval_LargerThanMcuCount_StillDecodes — proves an interval exceeding MCU count still decodes (no trailing RSTn breakage), self-decoder
- RestartIntervalTests.cs::RestartEncoding_IsDeterministic — proves restart encoding is byte-deterministic
- BoundaryFieldTests.cs::RestartInterval_Above16Bit_Throws — proves 8-bit encode with RestartInterval=70000 throws ArgumentException (DRI value bound, 8-bit path only)
- BoundaryFieldTests.cs::RestartInterval_AtMax_EncodesAndDecodes — proves RestartInterval=65535 (DRI max) encodes and round-trips without truncation (regression on the DRI masking bug)
- HighPrecisionQuantTests.cs::QuantValueAbove255_IsWrittenAs16BitAndRoundTrips — proves DQT is emitted at 16-bit precision (Pq=1, 0x10|Tq) with true zig-zag values when any quant value exceeds 255, and reconstructs consistently (B.2.4.1)
- HighPrecisionQuantTests.cs::EightBitQuantTable_StillUses8BitPrecision — proves DQT uses Pq=0 8-bit form (1+64 bytes) for standard tables
- DqtVariantTests.cs::SixteenBitQuantTable_DecodesIdentically — proves a 16-bit-precision DQT decodes identically (decoder-side; encoder emits 8-bit here)
- DqtVariantTests.cs::MultipleQuantTablesInOneSegment_Decode — proves multiple quant tables merged into one DQT segment decode correctly
- DqtVariantTests.cs::TruncatedSixteenBitDqt_ThrowsFormatException — proves a DQT declaring 16-bit but missing bytes throws (segment-length integrity)
- MetadataTests.cs::Exif_SurvivesRoundTrip / SmallIccProfile_SurvivesRoundTrip / LargeIccProfile_IsChunkedAndReassembled / Comments_SurviveRoundTrip / JfifDensity_SurvivesRoundTrip — prove Exif/ICC/COM/JFIF metadata segments emit and round-trip; large ICC chunked across APP2 and reassembled (T.872 / ICC APP2 chunking)
- IccReassemblyTests.cs::SingleChunkIcc_HasCorrectSequenceHeader — proves APP2 payload carries 'ICC_PROFILE\0' + seq=1 + count=1 header bytes (ICC chunk framing)
- IccReassemblyTests.cs::IccChunks_OutOfOrderInStream_ReassembleBySequenceNumber — proves multi-chunk ICC reassembles by sequence byte independent of file order
- MetadataEdgeTests.cs::ExifContainingFfAndZeroBytes_SurvivesIntact / IccContainingMarkerLikeBytes_SurvivesIntact — prove length-prefixed metadata payloads are not byte-stuffed or mistaken for markers
- MetadataEdgeTests.cs::MaximumSizeSegment_IsAccepted — proves an application segment at the 65533-byte limit encodes and round-trips (B.1.1.4 upper bound)
- MetadataEdgeTests.cs::UnicodeComment_RoundTrips / EmptyComment_RoundTrips — prove COM segment content edge cases
- CombinationTests.cs::Cmyk_WithMetadata_RoundTrips — proves Exif + ICC + COM together on a CMYK stream all round-trip with correct Adobe transform (combined tables-misc emission)
- CmykRoundTripTests.cs::Cmyk_Output_ContainsAdobeMarker — proves APP14 (0xEE) Adobe marker with 'Adobe' identifier is emitted for CMYK
- CmykRoundTripTests.cs::Cmyk_RoundTrips_AtHighQuality / FlatCmyk_RoundTripsNearlyExact / Cmyk_OddDimensions_Decode — prove 4-component CMYK structural round-trip (self-decoder)
- YcckTests.cs::Ycck_WritesAdobeTransform2 / Cmyk_StillWritesAdobeTransform0 / Ycck_RoundTrips_AtHighQuality — prove YCCK path emits Adobe transform=2 and round-trips; direct CMYK emits transform=0
- RgbColorTransformTests.cs::RgbDirect_WritesAdobeTransform0 / RgbDirect_RoundTripsNearLossless / YCbCrEncoding_RemainsDefault — prove RGB-direct path emits APP14 transform=0 and round-trips; default YCbCr emits no Adobe marker
- HighPrecisionCodecTests.cs::Grayscale12_RoundTrips / RgbDirect12_RoundTrips / RgbYCbCr12_RoundTrips / Cmyk12_RoundTrips / Cmyk12_AsYcck_RoundTrips — prove 12-bit SOF1 grayscale/RGB/YCbCr/CMYK/YCCK structural round-trip (self-decoder)
- HighPrecisionCodecTests.cs::Encode16_RejectsNon12BitPrecision — proves the codec rejects non-8/12-bit sample precision (SOF precision constraint, T.81)
- TableIdValidationTests.cs::(QuantId>3 rejected) — proves an out-of-range QuantId selector in SOF is rejected on decode (selector-range validation, decoder side)

**Gaps:**
- NO independent-decoder (libjpeg/djpeg) interop test exists anywhere in the suite. Every round-trip asserts against JpegSharp's own decoder, so 'Output must be decodable by independent decoders (libjpeg)' — the core interop requirement of this section — is entirely unproven. A structural bug that both encoder and decoder share would pass all current tests.
- 12-bit encode with RestartInterval > 65535 does NOT throw and would emit a truncated DRI (BaselineEncoder.cs 12-bit ctor at 128-189 omits the RestartInterval>MaxDimension guard that the 8-bit ctor has at 62-63). No test exercises the 12-bit DRI-overflow path (minor finding is untested).
- RSTn cadence is not verified: no test asserts RSTn markers appear at exactly the DRI interval boundaries, cycle D0->D7->D0, and that NO RSTn follows the final MCU. RestartIntervalTests only checks that >=1 RSTn exists (F.1.2.3 restart cadence).
- No test proves every QuantId and Huffman DC/AC selector referenced in SOF/SOS was previously defined by a DQT/DHT segment. StructureComplianceTests checks DQT<SOF and 'DHT present' but never cross-checks that each referenced table id is actually defined (B.2.2/B.2.3 'defined before referenced').
- Exif+JFIF/APP14 first-marker ordering (T.872 4.5.4 / T.871 6) is untested: no test asserts the observed marker order when Exif metadata is present alongside JFIF or Adobe, so the documented deviation is neither pinned nor regression-guarded.
- 12-bit DRI happy path is untested: no HighPrecision test sets RestartInterval, so RSTn emission and predictor reset at 12-bit precision are unproven.
- No test asserts all DHT segments precede SOS across a multi-table stream (color/CMYK). StructureComplianceTests only checks a single DHT is 'present' for grayscale-ish RGB; ordering of every DHT relative to SOS is not verified for 2- and 4-table cases.
- No test verifies RSTn-restart output decodes byte/pixel-identically to the same image encoded without restart markers (proving predictors truly reset rather than coincidentally decoding); current tests only assert closeness to source pixels.

**Required new tests:**
- `EncodedStreams_DecodeCleanlyWithLibjpeg_ForAllColorSpaces` (interop, ITU-T T.81 B.2 (decodable by independent decoders)): Encode 8-bit grayscale, YCbCr-RGB, RGB-direct, CMYK, and YCCK, then decode each with an independent reference decoder (libjpeg djpeg via Process, or SkiaSharp/System.Drawing if a native djpeg is unavailable) and assert successful decode with matching dimensions and no warnings — the one requirement no self-decoder test can prove. → djpeg/reference decoder exits 0 with no warnings and produces an image of the encoded dimensions/component count for every color space.
- `TwelveBitEncode_WithLibjpeg12_DecodesCleanly` (interop, ITU-T T.81 B.2.2 (SOF1 12-bit precision)): Encode a 12-bit SOF1 grayscale and RGB stream and decode with a 12-bit-capable reference decoder to prove SOF1 extended-sequential output is externally decodable, not just self-consistent. → Reference 12-bit decoder decodes the stream to the correct dimensions without error.
- `TwelveBitEncode_RestartIntervalAbove16Bit_Throws` (regression, ITU-T T.81 B.2.4.4 (DRI 16-bit interval field)): Encode a 12-bit image with RestartInterval > 65535 and assert it throws (the 8-bit path already throws; the 12-bit constructor currently masks the value into a truncated DRI). Pins the minor finding. → Jpeg.Encode16 throws ArgumentException for RestartInterval=70000; currently FAILS (no guard in the 12-bit ctor, emits a truncated DRI).
- `RestartMarkers_CadenceMatchesDriAndCycleD0ThroughD7_NoTrailingRst` (encoder, ITU-T T.81 F.1.2.3 (restart marker cadence and cycling)): Encode with a known RestartInterval on an image with many MCUs and parse the entropy stream: assert the count of RSTn equals floor((mcuCount-1)/interval), that codes cycle D0->D7->D0 in order, and that no RSTn appears immediately before EOI. → RSTn count matches the DRI-derived expectation, markers cycle 0xD0..0xD7 in sequence, and the final MCU is followed by EOI with no trailing RSTn.
- `EveryReferencedQuantAndHuffmanSelector_IsDefinedBeforeSos` (encoder, ITU-T T.81 B.2.2 / B.2.3 (tables defined before referenced)): Parse SOF component QuantId selectors and SOS component DC/AC Huffman selectors, then confirm each referenced id was defined by a preceding DQT/DHT segment; run across grayscale, YCbCr (2 tables), CMYK, and YCCK (4 components). → For every color space, all SOF QuantIds and all SOS DC/AC selector ids appear among the ids defined in earlier DQT/DHT segments.
- `AllDhtSegments_PrecedeSos_ForMultiTableStreams` (encoder, ITU-T T.81 B.2.1 (all tables-misc precede SOS)): For YCbCr-RGB (2 Huffman table pairs) and CMYK (up to 4 components) encodes, assert every DHT marker offset is less than the SOS marker offset — extending the single-DHT grayscale check to multi-table streams. → The maximum DHT offset is strictly less than the SOS offset in every multi-table stream.
- `RestartEncoded_DecodesIdenticalToNonRestartEncoded` (round-trip, ITU-T T.81 F.1.2.3 (predictor reset at restart)): Encode the same image with and without RestartInterval and assert both decode to identical pixel data, proving DC predictors truly reset at each interval rather than the stream merely decoding close to the source. → Decoded pixel arrays from the restart and non-restart encodes are byte-identical.
- `ExifPresent_FirstMarkerOrderIsPinned_ForJfifAndAdobePaths` (encoder, ITU-T T.872 4.5.4 / T.871 6 (first-marker placement)): Encode with Exif metadata on the YCbCr(JFIF) path and on the CMYK(Adobe) path; parse the marker sequence after SOI and assert the exact observed order (JFIF/APP14 then Exif APP1), pinning the documented T.872 deviation so any future reordering is caught. → Marker order after SOI matches the documented emission (APP0-JFIF then APP1-Exif for YCbCr; APP14-Adobe then APP1-Exif for CMYK); segments all parse with correct inclusive lengths.
- `CombinedMetadata_AllSegmentLengthsInclusiveAndParse` (round-trip, ITU-T T.81 B.1.1.4 / T.872 (segment length + ICC chunk reassembly)): Encode with Exif + JFIF density + multi-chunk ICC (>65533 bytes) + COM + a preserved raw APPn together, then walk the header asserting every length-bearing segment has length == payload+2, all segments parse to SOS, and the reassembled ICC equals the original. → All segment lengths equal payload+2, the header parses cleanly through to SOS, and decoded Exif/ICC/COM/APPn match the originals byte-for-byte.
- `TwelveBitRestartInterval_RoundTripsAndEmitsRstn` (round-trip, ITU-T T.81 B.2.4.4 / F.1.2.3): Encode a 12-bit image with a valid RestartInterval, assert DRI and RSTn markers are emitted and the stream round-trips, covering the currently-untested 12-bit restart happy path. → 12-bit stream contains a DRI and >=1 RSTn and decodes back within tolerance of the source samples.

