# SOF frame-header parsing (SOF0/1/2, reject SOF3/9-11/DAC)

**Section key:** `sof`  
**Compliance:** PARTIAL · **Adversarial justified:** n/a · **Final:** **PARTIAL**

## Findings

- **[major]** Sum of Hi*Vi <= 10 for interleaved scans (T.81 A.2.2 / B.2.2 max 10 data units per MCU)
  - Observed: No check of sum(Hi*Vi) anywhere. ParseFrameHeader (BaselineDecoder.cs:322-337) only bounds each Hi,Vi to 1..4; SetupGeometry (381-400) computes geometry without validating the per-MCU data-unit total. A frame with e.g. one 4x4 component (16) or multiple components summing >10 is accepted, producing an oversized/invalid interleaved MCU.
  - Spec: ITU-T T.81 A.2.2 / B.2.2
- **[major]** Reject unsupported precisions; valid DCT precision is 8 (baseline) or 8/12 (extended/progressive) only
  - Observed: Decode16 (BaselineDecoder.cs:105) accepts precision 9,10,11,12 via `if (_precision is < 9 or > 12) throw`, so P=9/10/11 (invalid per T.81, which permits only 8 or 12) are decoded rather than rejected — despite the adjacent comment stating 'JPEG DCT precision is 8 or 12'. ParseFrameHeader (308) reads P without any validation. Additionally baseline SOF0 with P=12 is accepted (routed to Decode16) even though SOF0 mandates P=8.
  - Spec: ITU-T T.81 B.2.2 (P: baseline 8; extended/progressive 8 or 12)
- **[major]** Must reject DAC (Define Arithmetic Conditioning marker)
  - Observed: In ParseHeaders (BaselineDecoder.cs:169-217) there is no case for JpegMarkers.DefineArithmeticConditioning (0xCC). IsStartOfFrame excludes 0xCC (JpegMarkers.cs:86 ranges skip it), so a DAC segment falls through the default branch and is silently consumed/ignored instead of being rejected.
  - Spec: ITU-T T.81 B.2.4.3 (DAC); requirement to reject DAC
- **[minor]** Tqi (quantization-table selector) must be 0..3
  - Observed: ParseFrameHeader (BaselineDecoder.cs:329) stores QuantId = segment[p+2] (full byte, 0..255) with no range check. It is only validated lazily at decode time in GetQuantTable (740-742) when the component is actually used, so ReadInfo and any not-yet-decoded component escape validation, and the error surfaces late rather than at frame-header parse.
  - Spec: ITU-T T.81 B.2.2 (Tqi 0..3)
- **[minor]** Y (number of lines) may be 0 when DNL is used
  - Observed: ParseFrameHeader (BaselineDecoder.cs:313) rejects height==0 outright (`_width <= 0 || _height <= 0`). Spec permits Y=0 with a subsequent DNL marker. DNL (0xDC) is also unhandled in ParseHeaders (silently skipped), so Y=0 streams cannot be decoded at all.
  - Spec: ITU-T T.81 B.2.2 (Y may be 0, defined later via DNL)
- **[info]** Component identifiers Ci should be unique within a frame
  - Observed: ParseFrameHeader (BaselineDecoder.cs:322-337) does not verify Ci uniqueness; FindComponentIndex (373-379) returns the first match, so duplicate component ids are not rejected.
  - Spec: ITU-T T.81 B.2.2 (Ci)
- **[info]** A single frame header per image
  - Observed: ParseHeaders (BaselineDecoder.cs:177-181) calls ParseFrameHeader on every SOF0/1/2 encountered; a second SOF silently overwrites _precision/_width/_height/_components rather than being flagged as malformed.
  - Spec: ITU-T T.81 B.2.1

## Required fixes

- In ParseFrameHeader (or SetupGeometry), compute sum of Hi*Vi over all frame components and reject when it exceeds 10 for interleaved decoding (T.81 A.2.2).
- Validate P at frame-header parse time and reject any precision other than 8 or 12; tie it to the frame type (SOF0 must be P=8). Change Decode16's `_precision is < 9 or > 12` to reject 9/10/11 (only allow 12), matching the code's own comment.
- Add an explicit case for DefineArithmeticConditioning (0xCC) in ParseHeaders that throws JpegFormatException (arithmetic coding unsupported), rather than silently skipping it.
- Validate Tqi (0..3) for every component inside ParseFrameHeader instead of relying on lazy GetQuantTable checks, so malformed frames are rejected up front and ReadInfo is covered.
- Decide DNL policy: either support Y=0 with a trailing DNL marker, or document that Y must be non-zero; if unsupported, keep rejecting Y=0 but do so with a message that names the DNL limitation rather than a generic 'Invalid image dimensions'.
- Reject duplicate component identifiers Ci within a frame and reject a second SOF marker in the same stream.

## Code locations

- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:303-338 ParseFrameHeader (P/Y/X/Nf and per-component Ci/Hi/Vi/Tqi parsing)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:308 precision read (unvalidated)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:313-316 dimension and Nf=0 rejection`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:332-333 Hi/Vi 1..4 rejection`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:83-84 Decode precision gate (==8)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:101-106 Decode16 precision gate (accepts 9..12)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:169-217 ParseHeaders marker dispatch (SOF routing + unsupported-SOF rejection at 209-215; no DAC/DNL case)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:381-400 SetupGeometry (no sum Hi*Vi<=10 check)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:738-757 GetQuantTable/GetDcTable/GetAcTable (lazy Tqi/table-id bounds)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Markers/JpegMarkers.cs:16-25 SOF0/1/2/3 constants`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Markers/JpegMarkers.cs:85-86 IsStartOfFrame (excludes 0xCC DAC, 0xC4 DHT, 0xC8 JPG)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Markers/JpegMarkers.cs:31 DefineArithmeticConditioning (0xCC) constant, unreferenced in ParseHeaders`

## Test coverage

**Existing:**
- SamplingFactorValidationTests.cs::SamplingFactorAboveFour_Throws — proves per-component Hi/Vi > 4 (bytes 0x55,0x51,0x15,0xF1) is rejected with JpegFormatException (T.81 B.2.2 Hi,Vi in 1..4 upper bound).
- CorruptionTests.cs::InvalidComponentSamplingFactor_ThrowsFormatException — proves Hi=0/Vi=0 (sampling byte 0x00) is rejected (T.81 B.2.2 lower bound Hi,Vi>=1).
- SamplingFactorValidationTests.cs::ValidSamplingFactors_AreAcceptedByRealEncoderOutput — proves valid factors (1,2,4) for 444/422/420/411 still decode (guards the [1,4] bound from over-rejecting).
- UnsupportedFrameTypeTests.cs::UnsupportedFrameType_ThrowsClearFormatException — proves SOF3 (0xC3 lossless), SOF9/10/11 (0xC9/CA/CB arithmetic), SOF5/7/13 differential are rejected with a JpegFormatException mentioning 'SOF'.
- UnsupportedFrameTypeTests.cs::ArithmeticFrame_MessageMentionsUnsupported — proves the arithmetic SOF9 rejection message says 'not supported'.
- UnsupportedFrameTypeTests.cs::SupportedFrameType_DoesNotThrowFrameTypeError — proves SOF0/SOF1/SOF2 pass frame-type parsing (no 'Unsupported frame type').
- CorruptionTests.cs::ZeroDimensions_ThrowsFormatException — proves Y=0 (height field 0x0000) is rejected (note: this asserts the CURRENT non-spec behavior; T.81 permits Y=0 with DNL).
- TableIdValidationTests.cs::FrameQuantTableIdOutOfRange_ThrowsCleanly — proves Tqi=5 on a component that is actually decoded is rejected as JpegFormatException (only the lazy decode-time path, on a full Jpeg.Decode).
- HighPrecisionCodecTests.cs::Encode16_RejectsNon12BitPrecision — proves the ENCODER rejects precisions other than 8/12 (NotSupportedException); not a decoder-side SOF precision check.
- HighPrecisionCodecTests.cs::Grayscale12_RoundTrips / RgbYCbCr12_RoundTrips / Cmyk12_RoundTrips (and progressive variants) — implicitly prove a valid P=12 extended/progressive (SOF1/SOF2) frame is accepted and decoded via Decode16.
- HighPrecisionCodecTests.cs::Decode_RejectsTwelveBitSource / Decode16_RejectsEightBitSource — prove the precision-vs-API-entrypoint routing (8-bit via Decode, 12-bit via Decode16) is enforced.
- StructureComplianceTests.cs::Sof0_EncodesDimensionsAndComponentCount — proves encoder emits P=8, correct Y/X/Nf and the 0x22 luma sampling byte in SOF0 (encoder-side well-formedness, not a decoder reject).
- MarkerTests.cs::IsStartOfFrame_Classifies — proves 0xC0/C1/C2 classify as SOF and 0xC4 (DHT)/0xC8 (JPG) do not; does NOT exercise 0xCC (DAC).
- CorruptionTests.cs::TruncatedSegment_ThrowsFormatException / TruncatedStream_NeverThrowsNonJpegException; HeaderFuzzTests.cs::CorruptingAnyHeaderByte_NeverThrowsNonJpegException — prove general truncated/mutated header bytes never escape as non-Jpeg exceptions (broad robustness, not the specific 6+Nf*3 length check).

**Gaps:**
- Sum of Hi*Vi <= 10 per interleaved MCU (T.81 A.2.2/B.2.2): NO test. Neither an oversized single component (4x4=16) nor multiple components summing >10 is rejected or proven; and no test proves a legal sum (<=10) is still accepted.
- Reject unsupported DCT precisions on DECODE: NO test that a stream with P=9/10/11 is rejected (only encoder-side rejection is tested via Encode16).
- Reject baseline SOF0 with P=12: NO test (SOF0 mandates P=8; currently P=12 is routed to Decode16 and accepted).
- Reject DAC (0xFFCC) segment: NO test. MarkerTests never classifies 0xCC and no decode test feeds a DAC segment; it is currently silently skipped.
- Component count Nf=0: NO test (boundary reject not exercised).
- Samples-per-line X=0: NO explicit test (only Y/height=0 is tested; width=0 path is untested).
- Tqi validation at frame-header-parse time / via Identify / for a component that is never decoded: GAP — only the lazy decode-time path with Tqi=5 is proven; Tqi=4 boundary, Tqi=15, and the Identify/ReadInfo path are unproven.
- Component-identifier Ci uniqueness within a frame: NO test (duplicate Ci not rejected).
- Single frame header per image: NO test (a second SOF silently overwriting frame state is not flagged).
- Y=0 defined later via DNL (T.81 B.2.2): NO test for the spec-permitted path; DNL (0xDC) handling is also untested and the existing ZeroDimensions test locks in the opposite (reject) behavior.
- Truncated frame header where segment length claims fewer than 6+Nf*3 bytes: NO targeted regression test for the ParseFrameHeader bounds (lines ~305-316).

**Required new tests:**
- `Sof0_SingleComponent_H4V4_ExceedsMcuDataUnitLimit_Throws` (decoder, ITU-T T.81 A.2.2 / B.2.2 (max 10 data units per MCU)): A single component with H=4,V=4 yields 16 data units per MCU (>10); ParseFrameHeader/SetupGeometry must reject it instead of building an oversized interleaved MCU. → Jpeg.Decode throws JpegFormatException (currently accepted — test exposes the missing sum(Hi*Vi) check).
- `Sof0_ThreeComponents_SumEight_AcceptedByFrameParse` (decoder, ITU-T T.81 A.2.2 / B.2.2): Three components sampled 2x2,2x1,2x1 sum to 8 (<=10) and must pass frame-header/geometry validation (any later failure is for missing tables, never an MCU-limit error). → Frame header is accepted; a full decode of the minimal stream fails only for missing scan/DHT, and the exception message never mentions the MCU/data-unit limit.
- `Sof0_ThreeComponents_SumTwelve_Throws` (decoder, ITU-T T.81 A.2.2 / B.2.2): Three components whose Hi*Vi sum to 12 (>10) must be rejected as an over-large interleaved MCU. → Jpeg.Decode throws JpegFormatException (currently accepted).
- `Sof0_Precision12_Rejected` (decoder, ITU-T T.81 B.2.2 (baseline P=8)): Baseline SOF0 mandates P=8; a SOF0 header declaring P=12 must be rejected rather than silently routed to the 16-bit path. → Jpeg.Decode (and Jpeg.Decode16) throws JpegFormatException (currently P=12 on SOF0 is accepted via Decode16).
- `Sof1_UnsupportedPrecision_Rejected` (decoder, ITU-T T.81 B.2.2 (P = 8 or 12 only)): Only P=8 or P=12 are valid DCT precisions; a SOF1/SOF2 declaring P=9, 10, or 11 must be rejected at decode. → Theory over P in {9,10,11}: Jpeg.Decode16 throws JpegFormatException (currently `_precision is <9 or >12` lets 9/10/11 through).
- `Sof1_Precision12_DecodesViaDecode16` (round-trip, ITU-T T.81 B.2.2 (extended/progressive P=8 or 12)): Explicitly assert the one valid extended/progressive high-precision case (SOF1 P=12) is accepted, guarding the precision check from over-rejecting. → Encode16 -> Decode16 of a 12-bit image round-trips with decoded.Precision == 12 (reinforces HighPrecisionCodecTests).
- `DacSegment_Rejected` (decoder, ITU-T T.81 B.2.4.3 (DAC)): A DAC (0xFFCC) arithmetic-conditioning segment is unsupported and must be rejected, not silently consumed by the default marker branch. → A hand-built stream containing 0xFF 0xCC throws JpegFormatException (currently silently skipped).
- `Sof0_ComponentCountZero_Throws` (decoder, ITU-T T.81 B.2.2 (Nf >= 1)): Nf=0 is invalid; a frame header declaring zero components must be rejected. → Jpeg.Decode throws JpegFormatException.
- `Sof0_SamplesPerLineZero_Throws` (decoder, ITU-T T.81 B.2.2 (X in 1..65535)): X=0 (samples/line) is invalid; complements the existing Y=0 test by exercising the width=0 path explicitly. → Jpeg.Decode throws JpegFormatException.
- `FrameQuantTableId_OutOfRange_RejectedAtHeaderParseAndIdentify` (decoder, ITU-T T.81 B.2.2 (Tqi 0..3)): Tqi must be 0..3 and must be validated at frame-header parse for every component (including via Identify/ReadInfo and components that are never entropy-decoded), not lazily only when a component is decoded. → Theory over Tqi in {4,15}: both Jpeg.Decode and Jpeg.Identify throw JpegFormatException (currently the Identify/undecoded-component path escapes validation).
- `DuplicateComponentId_Rejected` (decoder, ITU-T T.81 B.2.2 (Ci)): Component identifiers Ci must be unique within a frame; two components sharing a Ci must be rejected rather than resolved to the first match. → Jpeg.Decode throws JpegFormatException (currently duplicate Ci is accepted).
- `MultipleFrameHeaders_Rejected` (decoder, ITU-T T.81 B.2.1): Exactly one frame header per (non-hierarchical) image; a second SOF marker must be flagged as malformed rather than silently overwriting frame state. → A stream with two SOF0 markers throws JpegFormatException (currently the second SOF silently overwrites).
- `TruncatedFrameHeader_LengthTooSmall_Throws` (regression, ITU-T T.81 B.2.2 (Lf = 8 + 3*Nf)): Regression guard for ParseFrameHeader bounds: a SOF whose declared length is less than 6 + Nf*3 must be rejected without an out-of-range read. → Jpeg.Decode throws JpegFormatException (a JpegException subtype), never an IndexOutOfRange/other CLR exception.
- `YZero_WithDnl_Decodes` (decoder, ITU-T T.81 B.2.2 (Y=0 with DNL) / B.2.5): Y may be 0 in the frame header when a subsequent DNL (0xDC) marker defines the number of lines; such a stream must decode to the DNL-defined height rather than being rejected outright. → A stream with SOF Y=0 plus a valid DNL decodes to the DNL height (currently rejected by the `_height <= 0` guard and DNL is unhandled — test documents the compliance gap).

