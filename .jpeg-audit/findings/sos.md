# SOS scan-header parsing

**Section key:** `sos`  
**Compliance:** PARTIAL · **Adversarial justified:** n/a · **Final:** **PARTIAL**

## Findings

- **[major]** Reject Ns=0 or Ns>4 (T.81 B.2.3: 1 <= Ns <= 4)
  - Observed: ParseScanHeader (BaselineDecoder.cs:344-346) reads count=segment[0] and rejects only count==0 (and enforces a length floor). There is NO upper-bound check, so Ns can be 5..255. FindComponentIndex resolves each Csj against frame ids, so a scan declaring Ns=5 with duplicate valid component ids is accepted rather than rejected. Frame count itself is also unbounded in ParseFrameHeader (segment[5]), but is caught later by SetupGeometry's {1,3,4} guard; the scan-side Ns upper bound is not caught anywhere.
  - Spec: T.81 B.2.3 (Ns range 1..4)
- **[major]** Baseline: Ss=0, Se=63, Ah=Al=0
  - Observed: The baseline path never validates spectral-selection / successive-approximation fields. ParseScanHeader (BaselineDecoder.cs:360-370) stores Ss/Se/Ah/Al into _scan, and DecodeScan (BaselineDecoder.cs:402-451) ignores them entirely (it always decodes a full 0..63 block via BlockScanCoder.DecodeBlock). A non-conformant baseline/extended-sequential stream with e.g. Ss=5, Se=10, Ah=2, Al=3 is silently accepted and mis-decoded instead of being rejected. Progressive validation exists (DecodeProgressiveScan, BaselineDecoder.Progressive.cs:66-70) but is unreachable for SOF0/SOF1.
  - Spec: T.81 B.2.3 (baseline Ss=0,Se=63,Ah=Al=0)
- **[minor]** Td/Ta 0..3 (baseline 0..1)
  - Observed: ParseScanHeader (BaselineDecoder.cs:355-356) assigns DcTableId=tables>>4 and AcTableId=tables&0x0F with no validation at parse time. Values 4..15 are caught lazily when GetDcTable/GetAcTable (BaselineDecoder.cs:745-757) bounds-check against the size-4 table arrays, so the 0..3 ceiling is effectively enforced for any decoded component. However the baseline-specific restriction Td/Ta in 0..1 is never enforced: a baseline scan selecting DC/AC table id 2 or 3 is accepted. Validation is also deferred to decode time rather than reported at header parse.
  - Spec: T.81 B.2.3 / Table B.3 (baseline Td,Ta in {0,1})
- **[info]** Ah/Al field range (progressive point transform)
  - Observed: Al (ahAl & 0x0F, BaselineDecoder.cs:369) can be 0..15 and is used directly as a shift amount (e.g. predictors[si] << scan.Al in DecodeDcFirst, BaselineDecoder.Progressive.cs:126; 1 << scan.Al in refine). T.81 constrains Al to 0..13 for 8-bit data; values 14..15 are not rejected. Not one of the explicitly listed requirements, noted for completeness.
  - Spec: T.81 B.2.3 (Ah/Al range)
- **[info]** Csj must match a frame component id
  - Observed: CORRECT. FindComponentIndex (BaselineDecoder.cs:373-379) linearly matches Csj against _components[].Id and throws JpegFormatException for an unknown id. Note it does not detect duplicate Csj within one scan, but that is outside the listed requirements.
  - Spec: T.81 B.2.3 (Csj matches frame component)
- **[info]** Progressive DC scan Ss=Se=0; AC scan Ns=1, 1<=Ss<=Se<=63
  - Observed: CORRECT for progressive. DecodeProgressiveScan (BaselineDecoder.Progressive.cs:66-82) enforces Ss<=63, Se<=63, Ss<=Se; DC scans (Ss==0) require Se==0; AC scans (Ss!=0, hence Ss>=1) require exactly one component. These checks are sound but apply only to the progressive path.
  - Spec: T.81 G.1.1.1 / B.2.3

## Required fixes

- In ParseScanHeader (BaselineDecoder.cs:345), reject Ns>4: change the guard to `if (count == 0 || count > 4 || segment.Length < 1 + count*2 + 3)`.
- Add baseline/extended-sequential scan validation: when !_isProgressive, after parsing Ss/Se/Ah/Al, require Ss==0, Se==63, Ah==0, Al==0 and throw JpegFormatException otherwise (either in ParseScanHeader gated on !_isProgressive, or at the top of DecodeScan).
- Validate Td/Ta at parse time in ParseScanHeader: require 0..3 for all modes, and 0..1 for baseline (SOF0) / (per T.81 Table B.3) rather than relying on the deferred GetDcTable/GetAcTable bounds check; this yields an accurate error at the header instead of mid-scan.
- Optionally bound Al (and Ah) to 0..13 for progressive scans in DecodeProgressiveScan to prevent oversized point-transform shifts.

## Code locations

- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:340-371 ParseScanHeader (SOS parse: Ns, Csj, Td/Ta, Ss, Se, Ah, Al)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:344-346 Ns/length validation (rejects Ns=0 only, no Ns>4)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:355-356 Td/Ta assignment (no parse-time validation)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:373-379 FindComponentIndex (Csj -> frame component match)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:402-451 DecodeScan (baseline path; ignores Ss/Se/Ah/Al)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:745-757 GetDcTable/GetAcTable (lazy Td/Ta 0..3 bounds enforcement)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.Progressive.cs:64-90 DecodeProgressiveScan (progressive Ss/Se/Ah + Ns=1 validation)`

## Test coverage

**Existing:**
- ProgressiveScanHeaderTests.cs::DcScanWithNonZeroSe_ThrowsCleanly — proves progressive DC scan (Ss=0) with Se!=0 is rejected (T.81 G.1.1.1)
- ProgressiveScanHeaderTests.cs::SpectralSelectionEndAboveMax_ThrowsCleanly — proves progressive AC scan with Se>63 is rejected
- ProgressiveScanHeaderTests.cs::SpectralSelectionStartAboveEnd_ThrowsCleanly — proves progressive AC scan with Ss>Se is rejected
- ProgressiveRobustnessTests.cs::ProgressiveAcScanWithMultipleComponents_Throws — proves progressive AC scan with Ns=2 is rejected (AC must be non-interleaved, Ns=1)
- TableIdValidationTests.cs::ScanHuffmanTableIdOutOfRange_ThrowsCleanly — proves DC table id 4 (Td>3) in SOS is rejected, enforcing the 0..3 ceiling
- TableIdValidationTests.cs::ScanAcTableIdOutOfRange_ThrowsCleanly — proves AC table id 5 (Ta>3) in SOS is rejected, enforcing the 0..3 ceiling
- HeaderFuzzTests.cs::CorruptingAnyHeaderByte_NeverThrowsNonJpegException — mutates every SOS header byte (incl. Ns, Csj, TdTa, Ss/Se/AhAl) and proves only JpegException (never a non-Jpeg crash) escapes; a robustness guard, NOT a proof that any specific illegal field value is rejected

**Gaps:**
- Reject Ns>4 (1<=Ns<=4): no test drives an SOS with Ns=5..255; the fuzz test tolerates a successful decode so does not prove rejection. Per compliance, ParseScanHeader has no upper-bound check — this behavior is currently absent AND untested.
- Reject Ns=0: code rejects count==0 but there is no dedicated test asserting the rejection as a regression guard (fuzz only proves no crash).
- Baseline (SOF0/SOF1) must have Ss=0, Se=63, Ah=0, Al=0: no test corrupts a baseline SOS spectral-selection / successive-approximation fields. Progressive-only validation exists; baseline path ignores these fields. Currently unenforced and untested.
- Baseline-specific Td/Ta in {0,1}: the 0..3 ceiling is tested, but no test asserts a baseline scan selecting DC/AC table id 2 or 3 is rejected. Currently unenforced and untested.
- Csj must match a frame component id (unknown-id rejection): FindComponentIndex throws for an unknown Csj, but no test drives an SOS whose Csj is absent from the frame. Behavior exists but is untested.
- Truncated SOS (segment length shorter than 1+2*Ns+3): no test drives an SOS whose declared length cannot hold Ns component entries plus the 3 spectral bytes.
- Al point-transform range 0..13 for 8-bit data (info): Al 14..15 used directly as a shift; no test and not rejected.

**Required new tests:**
- `BaselineScanWithNonZeroSpectralSelection_Throws` (decoder, T.81 B.2.3 (baseline Ss=0, Se=63)): Encode a baseline SOF0 stream, patch the SOS Ss byte to 5 (also cover Se=10 via a second case); a baseline scan must have Ss=0,Se=63. Locate SOS, offset payload+1+2*Ns is Ss. Expect rejection; currently silently mis-decoded. → Jpeg.Decode throws JpegFormatException (baseline spectral selection must be Ss=0/Se=63). Currently FAILS: stream is accepted and mis-decoded.
- `BaselineScanWithNonZeroSuccessiveApproximation_Throws` (decoder, T.81 B.2.3 (baseline Ah=Al=0)): Encode a baseline SOF0 stream, patch the SOS Ah<<4|Al byte (payload+1+2*Ns+2) to e.g. 0x23 (Ah=2,Al=3); baseline requires Ah=Al=0. Expect rejection; currently ignored. → Jpeg.Decode throws JpegFormatException. Currently FAILS: Ah/Al ignored on the baseline path and stream is accepted.
- `ScanWithComponentCountAboveFour_Throws` (decoder, T.81 B.2.3 (Ns range 1..4)): Build/patch an SOS declaring Ns=5 referencing duplicate valid component ids (and enough component-entry bytes / length to pass the length floor). T.81 requires 1<=Ns<=4. Expect rejection on the Ns upper bound. → Jpeg.Decode throws JpegFormatException (Ns must be 1..4). Currently FAILS: no upper-bound check, scan accepted.
- `ScanWithZeroComponents_Throws` (regression, T.81 B.2.3 (Ns range 1..4)): Patch the SOS Ns byte to 0 and adjust length so parsing reaches the count check. Regression guard confirming the existing Ns==0 rejection stays in place. → Jpeg.Decode throws JpegFormatException. Currently PASSES (already handled); locks in the behavior.
- `BaselineScanSelectingHuffmanTableId2_Throws` (decoder, T.81 B.2.3 / Table B.3 (baseline Td,Ta in {0,1})): Encode a baseline SOF0 stream, patch the SOS TdTa byte (payload+2) to 0x20 (Td=2) or 0x02 (Ta=2) with that table present, verifying the baseline Td/Ta in {0,1} restriction. Ceiling 0..3 is already covered; this targets the baseline 0..1 rule. → Jpeg.Decode throws JpegFormatException (baseline may only use table ids 0 or 1). Currently FAILS: id 2/3 accepted.
- `ScanReferencingUnknownComponentId_Throws` (decoder, T.81 B.2.3 (Csj matches a frame component id)): Encode a grayscale (or RGB) baseline stream and patch the SOS Csj byte (payload+1) to an id not present in the SOF frame. Confirms FindComponentIndex rejects an unmatched Csj. → Jpeg.Decode throws JpegFormatException ('Scan references unknown component id'). Currently PASSES (behavior exists); adds the missing coverage.
- `TruncatedScanComponentList_Throws` (decoder, T.81 B.2.3 (SOS length must hold Ns entries + Ss/Se/AhAl)): Patch an SOS segment length to a value smaller than 1+2*Ns+3 (component list plus spectral bytes cannot fit). Confirms truncated-scan-header rejection. → Jpeg.Decode throws JpegFormatException ('Truncated or invalid scan component list').
- `ProgressiveAcScanWithStartAboveEnd_Throws` (decoder, T.81 G.1.1.1 / B.2.3 (1<=Ss<=Se<=63)): Encode a progressive stream, patch a first AC scan so Ss=10 and Se=5 (Ss>Se) explicitly rather than only via Se=0. Strengthens the Ss<=Se check with a non-degenerate case. → Jpeg.Decode throws JpegFormatException. Currently PASSES (validated in DecodeProgressiveScan); explicit-case coverage.
- `ProgressiveScanWithAlAbove13_Throws` (decoder, T.81 B.2.3 (Al 0..13 for 8-bit)): Info-level: encode a progressive stream and patch a scan's Al (AhAl & 0x0F) to 14 or 15, which exceeds the T.81 8-bit point-transform limit and is used directly as a shift. Documents desired rejection. → Jpeg.Decode throws JpegFormatException. Currently FAILS: Al 14..15 accepted and used as shift amount.

