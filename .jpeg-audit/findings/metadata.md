# Metadata: JFIF (APP0), Exif (APP1), ICC (APP2 multi-seg), Adobe (APP14), COM

**Section key:** `metadata`  
**Compliance:** PARTIAL · **Adversarial justified:** n/a · **Final:** **PARTIAL**

## Findings

- **[major]** COM arbitrary bytes must round-trip losslessly
  - Observed: Decoder stores COM as a UTF-8 string (BaselineDecoder.cs:204 System.Text.Encoding.UTF8.GetString(segment)) and the encoder re-emits via UTF8.GetBytes (BaselineEncoder.Metadata.cs:75). COM is defined as arbitrary bytes; any non-UTF-8 / binary comment is decoded with U+FFFD replacement and cannot round-trip, silently corrupting the payload. Invalid UTF-8 -> lossy on read, and even valid text is forced through a UTF-8 codec rather than preserved verbatim.
  - Spec: T.81 B.2.4.5 (COM = arbitrary bytes); task requirement 'COM arbitrary bytes'
- **[major]** ICC APP2 chunks must reassemble correctly; reader must tolerate duplicate/malformed chunk sets without producing corrupt output
  - Observed: BuildMetadata (BaselineDecoder.Metadata.cs:73-88) sorts _iccChunks by seq via non-stable List.Sort and blindly concatenates every chunk, ignoring the count byte (segment[13], never read in ParseIccChunk:36-45). Duplicate sequence numbers are both concatenated, missing sequence numbers leave silent gaps, and the declared count is never validated. The result is a silently corrupt ICC profile rather than a tolerated/rejected one. seq=0 (illegal per ICC, which is 1-based) is also accepted.
  - Spec: ICC.1 Annex B / T.872 (ICC_PROFILE\0 + seqno + count, reassemble in order); task requirement 'reassemble in order' and 'tolerate duplicate'
- **[minor]** JFIF Xdensity/Ydensity must be >=1; units must be 0/1/2
  - Observed: ParseJfif (BaselineDecoder.Metadata.cs:17-19) casts segment[7] straight to JpegDensityUnit without validating it is 0/1/2 (a value of 3+ yields an out-of-range enum stored in metadata), and reads Xdensity/Ydensity without checking they are >=1 (a stored density of 0 is accepted). WriteJfif then silently clamps X/Y to [1,MaxDimension] (BaselineEncoder.Metadata.cs:13-14), so a decoded density of 0 is altered on re-encode without notice.
  - Spec: T.871 6.2 (Xdensity/Ydensity nonzero; units field values 0,1,2)
- **[minor]** Exif files require APP1 as the first application segment with no JFIF APP0; JFIF requires APP0 first
  - Observed: WriteHeader (BaselineEncoder.cs:239-243) always writes a synthetic JFIF APP0 first (unless Adobe/CMYK) and then writes Exif APP1 immediately after. Producing both APP0 (JFIF) and APP1 (Exif) in the same stream is non-conformant to the Exif spec, which forbids JFIF APP0 in an Exif file and mandates APP1 immediately after SOI. Round-tripping an Exif-only source therefore injects a JFIF segment that was not present.
  - Spec: T.872 4.5.4 / 4.7 (Exif APP1 first, no JFIF APP0)
- **[minor]** Adobe APP14 transform flag values are 0/1/2
  - Observed: ParseAdobe (BaselineDecoder.Metadata.cs:55) stores segment[11] verbatim with no range check. Downstream color logic (BaselineDecoder.cs:560-561, 647-648) treats any value >=0 as 'Adobe present -> invert' and only ==2 as YCCK, so an out-of-range transform (e.g. 3) is silently treated as inverted non-YCCK rather than flagged.
  - Spec: T.872 / Adobe APP14 (transform in {0,1,2})
- **[minor]** Reader must tolerate duplicate JFIF/Exif/Adobe segments
  - Observed: Duplicate APP0/APP1(Exif)/APP14 segments are silently last-wins: _density (line 20), _exif (line 32) and _adobeTransform (line 55) are overwritten by later occurrences with no detection or preference for the first. This is tolerant (no crash) but undocumented and may pick a spurious later segment.
  - Spec: Task requirement 'tolerate ... duplicate ... segments'
- **[info]** Encoder must handle oversized Exif payloads
  - Observed: WriteExif (BaselineEncoder.Metadata.cs:27-42) builds a single APP1 payload of 6+exif.Length bytes; if Exif exceeds ~65527 bytes MarkerWriter.WriteSegment throws JpegFormatException (MarkerWriter.cs:44-45). Exif is spec-capped at 64KB so this is acceptable, but the failure is a hard throw with no APP1 chaining, unlike ICC which chunks.
  - Spec: T.872 (Exif APP1 <= 64KB)
- **[info]** Round-trip must preserve original APPn/COM ordering
  - Observed: On re-encode the original inter-segment order is not preserved: WriteHeader emits a fixed order (Adobe/JFIF, Exif, ICC, comments, then preserved app segments — BaselineEncoder.cs:239-246). A source that interleaved unrecognized APPn before Exif/ICC is reordered. Not a spec violation but a fidelity gap.
  - Spec: Task requirement 'round-trip through decode and encode'

## Required fixes

- Store COM segments as raw byte[] (not string) to preserve arbitrary bytes; if a string API is kept, add a byte[] round-trip path so binary/non-UTF-8 comments are not corrupted. Update JpegMetadata.Comments, BaselineDecoder.cs:204, and WriteComments (BaselineEncoder.Metadata.cs:75).
- Harden ICC reassembly in BuildMetadata (BaselineDecoder.Metadata.cs:73-88): read and honor the count byte, deduplicate/validate sequence numbers (1..count, reject seq=0), detect gaps/duplicates, and use a stable ordering. Decide and document behavior for malformed chunk sets (drop vs. best-effort) instead of silently concatenating.
- In ParseJfif, validate/normalize the units byte (0/1/2) and treat Xdensity/Ydensity of 0 explicitly; avoid the silent WriteJfif clamp altering decoded density, or document the normalization.
- Avoid emitting a synthetic JFIF APP0 when Exif metadata is present (or make JFIF emission opt-in) so Exif output stays spec-conformant; ensure APP1 Exif ordering matches T.872.
- Validate the Adobe transform flag range (0/1/2) in ParseAdobe and handle out-of-range values explicitly rather than defaulting unknown values to inverted-non-YCCK.
- Consider chaining/rejecting oversized Exif in WriteExif with a clear error tied to the 64KB Exif limit rather than the generic MarkerWriter throw.

## Code locations

- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.Metadata.cs:9 ParseJfif`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.Metadata.cs:24 ParseExif`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.Metadata.cs:36 ParseIccChunk`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.Metadata.cs:47 ParseAdobe`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.Metadata.cs:62 BuildMetadata (ICC reassembly 73-88)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:187-205 marker dispatch (COM at 204)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.Metadata.cs:10 WriteJfif`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.Metadata.cs:27 WriteExif`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.Metadata.cs:44 WriteIcc`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.Metadata.cs:69 WriteComments`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.Metadata.cs:87 WriteAdobe`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.cs:236-246 WriteHeader ordering`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Api/JpegMetadata.cs:39 JpegMetadata`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Markers/MarkerWriter.cs:42 WriteSegment (65533 cap)`

## Test coverage

**Existing:**
- MetadataTests.cs::Exif_SurvivesRoundTrip — Exif APP1 raw payload round-trips through decode+encode
- MetadataTests.cs::SmallIccProfile_SurvivesRoundTrip — single-chunk ICC APP2 round-trip
- MetadataTests.cs::LargeIccProfile_IsChunkedAndReassembled — ICC split across many APP2 segments reassembles correctly (in-order)
- MetadataTests.cs::Comments_SurviveRoundTrip — multiple COM segments round-trip (ASCII text only)
- MetadataTests.cs::JfifDensity_SurvivesRoundTrip — JFIF APP0 density (DPI 300x300) round-trips
- MetadataTests.cs::AdobeTransform_IsCapturedForCmyk — Adobe APP14 transform=0 captured for CMYK
- MetadataTests.cs::Decode_WithoutMetadata_HasEmptyMetadata — missing metadata segments tolerated (null/empty, no crash)
- MetadataEdgeTests.cs::UnicodeComment_RoundTrips — COM with multibyte UTF-8 (accents/CJK/emoji) round-trips (still forced through UTF-8 codec)
- MetadataEdgeTests.cs::ExifContainingFfAndZeroBytes_SurvivesIntact — Exif payload with 0xFF/0x00 bytes not marker-corrupted
- MetadataEdgeTests.cs::IccContainingMarkerLikeBytes_SurvivesIntact — ICC with embedded FF D8 marker-like bytes across segments survives
- MetadataEdgeTests.cs::EmptyComment_RoundTrips — empty COM segment round-trips
- MetadataEdgeTests.cs::MaximumSizeSegment_IsAccepted — APPn payload at 65533-byte segment limit encodes+round-trips
- IccReassemblyTests.cs::IccChunks_OutOfOrderInStream_ReassembleBySequenceNumber — APP2 chunks physically reversed in stream reassemble by seq byte
- IccReassemblyTests.cs::SingleChunkIcc_HasCorrectSequenceHeader — verifies ICC_PROFILE\0 + seq=1 + count=1 header layout on encode
- BoundaryFieldTests.cs::JfifDensity_AboveMax_IsClampedNotTruncated — density >65535 clamped to 65535 (not bit-truncated)
- BoundaryFieldTests.cs::NormalDensity_RoundTripsExactly — DPCM 300x300 density exact round-trip
- UnknownMarkerTests.cs::UnknownAppSegment_SurvivesRoundTrip — unrecognized APP3/APP11 preserved verbatim in ApplicationSegments (via options metadata)
- UnknownMarkerTests.cs::RecognizedSegments_AreNotDuplicatedAsUnknown — JFIF/Exif parsed into typed fields, not also preserved raw
- UnknownMarkerTests.cs::UnknownAppSegment_InHandBuiltStream_IsPreserved — decoder preserves an APP5 injected into a real stream
- UnknownMarkerTests.cs::NonMatchingApp0_IsPreservedRatherThanTreatedAsJfif — an APP0 that is not 'JFIF\0' (JFXX-like) is preserved not misparsed
- ColorTransformDetectionTests.cs::AdobeTransform1_TriggersYCbCrDecode — injected Adobe APP14 transform=1 captured and drives decode
- ApiContractTests.cs::BareJpeg_WithoutJfif_DecodesAsYCbCr — stream with APP0 removed still decodes, Density null (missing-segment tolerance)
- ApiCoverageTests.cs::Metadata_SurvivesProgressiveEncoding — Exif+Density+COM survive progressive encode path
- ApiCoverageTests.cs::ImageMetadata_IsUsedWhenOptionsMetadataNull — image-attached metadata (COM) used when options.Metadata null
- StructureComplianceTests.cs::Baseline_HasExpectedMarkerOrderAndSegments — JFIF APP0 present and marker ordering (APP0 before DQT/SOF)

**Gaps:**
- MAJOR: COM arbitrary/binary bytes (invalid UTF-8, e.g. 0x00/0xFF/0x80 sequences) must round-trip byte-identical. No test exercises non-UTF-8 COM; existing COM tests are all valid UTF-8 text, masking the UTF8.GetString/GetBytes lossy path (U+FFFD replacement).
- MAJOR: ICC APP2 reassembly with a DUPLICATE sequence number — no test; current code concatenates both duplicates producing silent corruption.
- MAJOR: ICC APP2 reassembly with a MISSING sequence number (gap in seq set) — no test; current code silently concatenates around the gap.
- MAJOR: ICC declared count byte (segment[13]) is never validated on decode — no test for count/actual-chunk mismatch.
- ICC chunk with seq=0 (illegal per ICC 1-based numbering) — no test asserting defined tolerant/reject behavior.
- MINOR: JFIF APP0 units field out of range (units=3+) — no test; ParseJfif casts straight to enum with no validation/normalization.
- MINOR: JFIF Xdensity=0 / Ydensity=0 (spec requires >=1) — no test; density 0 accepted on decode then silently clamped to 1 on re-encode.
- MINOR: Duplicate JFIF APP0 segments — no test asserting documented first-wins/last-wins behavior (currently silent last-wins).
- MINOR: Duplicate Exif APP1 segments — no test asserting documented first-wins/last-wins (currently silent last-wins).
- MINOR: Duplicate Adobe APP14 segments — no test asserting documented behavior (currently silent last-wins).
- MINOR: Exif-only source round-trip must NOT inject a spurious JFIF APP0 and must order APP1 conformantly (Exif spec forbids JFIF APP0). No test; encoder always emits synthetic JFIF APP0.
- MINOR: Adobe APP14 transform out of range (transform=3) — no test asserting defined color handling; currently treated as inverted non-YCCK silently.
- Oversized ICC profile requiring >255 chunks must produce a clear, specific error — no test (encoder throws JpegException, unverified).
- Oversized Exif payload (>~65527 bytes / >64KB) must produce a clear, specific error — no test (encoder path throws JpegFormatException from MarkerWriter, unverified).
- Unrecognized APP1 that is not Exif (e.g. XMP 'http://ns.adobe.com/xap/1.0/\0') must be preserved verbatim in ApplicationSegments and round-tripped — no test (only non-JFIF APP0 covered).
- Unrecognized APP0 JFXX specifically (thumbnail extension) preserved verbatim — only a generic non-JFIF APP0 is covered; JFXX identifier not exercised.
- INFO: Round-trip preservation of original inter-segment APPn/COM ordering (unrecognized APPn interleaved before Exif/ICC) — no test; encoder reorders to a fixed sequence.

**Required new tests:**
- `ComWithRawBinaryBytes_RoundTripsByteIdentical` (round-trip, T.81 B.2.4.5 (COM = arbitrary bytes)): Prove a COM segment containing arbitrary non-UTF-8 bytes (e.g. {0x00,0xFF,0x80,0xFE,0x41}) survives decode+encode byte-for-byte. Primary MAJOR finding: UTF8.GetString/GetBytes corrupts binary comments with U+FFFD replacement. → Round-tripped COM payload equals the original bytes exactly (no U+FFFD, no re-encoding loss). If the string-based API cannot represent this, the test pins the corruption as a failing/xfail regression.
- `IccChunks_DuplicateSequenceNumber_DefinedBehavior` (decoder, ICC.1 Annex B / T.872 (reassemble in order; tolerate duplicate)): Hand-build a stream with two APP2 chunks sharing the same seq (seq=1 twice) plus remaining chunks; assert correct de-duplicated reassembly or defined rejection, never silent double-concatenation. → Either the reassembled ICC equals the intended profile (duplicate ignored) or a specific JpegFormatException is thrown; NOT a longer, silently-corrupted profile.
- `IccChunks_MissingSequenceNumber_DefinedBehavior` (decoder, ICC.1 Annex B / T.872 (reassemble in order)): Hand-build an APP2 chunk set with a gap in the sequence (chunks 1,2,4 with count=4, seq=3 missing) and assert defined behavior rather than silent gap concatenation yielding a misaligned profile. → Decoder rejects with a specific error OR surfaces the incomplete profile in a documented way; it must not silently emit a shorter profile as if complete.
- `IccChunk_CountMismatchAndSeqZero_Tolerated` (decoder, ICC.1 Annex B (seqno 1-based, count field)): Feed APP2 chunks whose declared count byte (segment[13]) disagrees with the actual chunk count, and a chunk with seq=0 (illegal, ICC is 1-based). Assert validation/tolerance rather than ignoring the count entirely (segment[13] currently never read). → Defined outcome: count validated (specific error on mismatch) or seq=0 rejected/handled deterministically; no silent corruption.
- `JfifUnitsOutOfRange_IsNormalizedNotStoredAsInvalidEnum` (decoder, T.871 6.2 (units field values 0,1,2)): Decode a hand-built JFIF APP0 with units=3 and assert the decoder normalizes to a valid JpegDensityUnit (0/1/2) or rejects, rather than storing an out-of-range enum and re-emitting it. No exception expected. → Density.Unit is a valid enum member (or segment rejected/normalized); no crash; re-encode does not propagate an invalid units byte.
- `JfifZeroDensity_IsNormalizedToOne` (decoder, T.871 6.2 (Xdensity/Ydensity nonzero)): Decode a hand-built JFIF APP0 with Xdensity=0/Ydensity=0 (spec requires >=1). Assert defined normalization (documented clamp to 1) and no exception, making the round-trip alteration intentional. → Decoded density normalized to >=1 per documented rule (e.g. 1,1) with no exception; behavior asserted explicitly.
- `DuplicateJfifApp0_WinnerIsDocumented` (decoder, Task requirement 'tolerate duplicate segments'): Hand-build a stream with two JFIF APP0 segments carrying different densities and pin the documented tolerance rule (currently last-wins on _density). → No crash; Density equals the documented winner (first or last per spec decision); test pins the chosen contract.
- `DuplicateExifApp1_WinnerIsDocumented` (decoder, Task requirement 'tolerate duplicate segments'): Hand-build a stream with two Exif APP1 segments with different payloads and assert the documented duplicate-tolerance rule (currently last-wins on _exif). → No crash; Exif equals the documented winner; behavior asserted explicitly.
- `DuplicateAdobeApp14_WinnerIsDocumented` (decoder, Task requirement 'tolerate duplicate segments'): Hand-build a stream with two Adobe APP14 segments with different transform bytes and assert documented duplicate-tolerance (currently last-wins on _adobeTransform). → No crash; AdobeColorTransform equals the documented winner; behavior asserted explicitly.
- `ExifOnlySource_DoesNotInjectJfifApp0` (encoder, T.872 4.5.4 / 4.7 (Exif APP1 first, no JFIF APP0)): Encode an image with Exif metadata (no JFIF density intent) and assert the output does not contain a synthetic JFIF APP0 alongside Exif APP1, and that APP1 ordering is Exif-conformant. Targets the always-JFIF WriteHeader behavior. → No JFIF APP0 when producing an Exif file; APP1(Exif) immediately after SOI. If current behavior injects JFIF, the test pins the non-conformance as a documented gap.
- `AdobeTransformOutOfRange_HasDefinedColorHandling` (decoder, T.872 / Adobe APP14 (transform in {0,1,2})): Decode a stream with Adobe APP14 transform=3 (out of {0,1,2}) and assert defined, deterministic color handling rather than the silent 'present -> invert, non-YCCK' fall-through. → transform=3 is rejected/normalized with a documented rule, or the color path is explicitly asserted; not an untested silent behavior.
- `OversizedIccProfile_ThrowsSpecificError` (encoder, ICC.1 / T.872 (APP2 seqno one byte, <=255 chunks)): Encode an ICC profile large enough to require >255 APP2 chunks and assert a clear, specific exception (JpegException with the 255-chunk message), confirming the encoder guard is reachable and messaged. → A specific JpegException identifying the ICC/255-chunk limit; not an ambiguous overflow or truncated output.
- `OversizedExifPayload_ThrowsSpecificError` (encoder, T.872 (Exif APP1 <= 64KB)): Encode an Exif payload exceeding the ~65527-byte APP1 limit and assert a clear, specific error (Exif exceeds 64KB) rather than an opaque MarkerWriter format exception, since Exif is not chunked like ICC. → A specific, descriptive exception identifying the Exif/64KB limit.
- `UnrecognizedApp1Xmp_IsPreservedVerbatim` (round-trip, Task requirement 'unrecognized APP1 preserved verbatim and round-tripped'): Round-trip an APP1 segment that is not Exif (XMP identifier 'http://ns.adobe.com/xap/1.0/\0' + payload). Assert it is preserved in ApplicationSegments verbatim, not misparsed as Exif nor dropped. Complements the existing non-JFIF-APP0 test which does not cover APP1. → The XMP APP1 appears in decoded ApplicationSegments with identical marker code and bytes; not captured as Exif.
- `UnrecognizedApp0Jfxx_IsPreservedVerbatim` (round-trip, T.871 (JFXX extension) / task 'unrecognized APP0 (JFXX) preserved'): Round-trip a JFXX APP0 segment (identifier 'JFXX\0' + thumbnail extension bytes) and assert it is preserved verbatim rather than misinterpreted as JFIF. Sharpens the existing generic non-JFIF-APP0 coverage with the real JFXX identifier. → The JFXX APP0 survives in ApplicationSegments with identical bytes; not parsed into Density.
- `InterleavedAppnOrdering_RoundTripFidelity` (regression, Task requirement 'round-trip through decode and encode' (segment ordering)): Decode a hand-built stream that interleaves an unrecognized APPn before Exif/ICC, re-encode, and document the ordering behavior (encoder emits a fixed order). Pins the info-level fidelity gap so future ordering changes are intentional. → Test documents the current fixed re-emit order (Adobe/JFIF, Exif, ICC, COM, preserved APPn) so the fidelity gap is explicit and change-controlled; no data loss of the preserved segment.

