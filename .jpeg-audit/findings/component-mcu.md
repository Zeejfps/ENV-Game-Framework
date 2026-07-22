# Component handling, sampling factors, MCU structure & block order

**Section key:** `component-mcu`  
**Compliance:** PARTIAL · **Adversarial justified:** n/a · **Final:** **PARTIAL**

## Findings

- **[major]** Non-interleaved (single-component) scans must use per-component dims: blocksPerLine=ceil(xi/8), xi=ceil(X*Hi/Hmax); blocksPerCol=ceil(yi/8), yi=ceil(Y*Vi/Vmax); MCU = 1 data unit (T.81 A.2.2, A.2.4).
  - Observed: BaselineDecoder.DecodeScan (lines 417-450) ALWAYS iterates the interleaved MCU grid (_mcusPerRow x _mcusPerCol) and decodes c.V*c.H blocks per component per MCU, with no Ns==1 branch. For a genuinely non-interleaved scan (scan.Components.Length==1) this yields the wrong data-unit count whenever the scan's component has Hi>1 or Vi>1 (or is the luma component with Hi=Hmax>1). Example: single-component scan with X=8, Hi=Hmax=2 => correct blocksPerLine = ceil(ceil(8*2/2)/8)=1, but DecodeScan computes BlocksWide=_mcusPerRow*c.H = ceil(8/16)*2 = 2, so it decodes twice the blocks per line and desynchronizes the entropy stream. Note the progressive path DOES do this correctly via ComponentActualWidth/Height + CeilDiv(...,8) at Progressive.cs:142-143,240-241,346-348; the baseline path lacks the equivalent.
  - Spec: ITU-T T.81 A.2.2, A.2.4
- **[major]** A baseline sequential frame may be coded as multiple (typically non-interleaved) scans; a decoder must process every scan (T.81 A.2, B.2.3).
  - Observed: ParseHeaders (lines 154-166) returns entropyStart at the FIRST StartOfScan and never returns to the marker loop for the non-progressive path. FillPlanes -> DecodeScan (lines 124-130, 402-451) decodes exactly one scan over data.AsSpan(entropyStart) and stops; subsequent DHT/DRI/SOS segments and their entropy data are ignored. Multi-scan (non-interleaved) baseline JPEGs therefore decode only the first component/scan and produce corrupt output for the rest. Only the progressive path (DecodeProgressive, Progressive.cs:15-30) has a multi-scan marker loop.
  - Spec: ITU-T T.81 A.2, B.2.3
- **[minor]** Interleaved MCU: components in SOS order, each contributing Hi*Vi data units in raster (row-major) order; MCUs = ceil(X/(8*Hmax)) x ceil(Y/(8*Vmax)) (T.81 A.2.3).
  - Observed: For the interleaved case this is implemented CORRECTLY: SetupGeometry (386-387) computes _mcusPerRow=CeilDiv(X,8*Hmax), _mcusPerCol=CeilDiv(Y,8*Vmax); DecodeScan iterates SOS order via 'foreach ci in _scan.Components' (429) then by=0..V-1 (outer), bx=0..H-1 (inner) raster order (435-437), storing at ((mx*H+bx)*8,(my*V+by)*8) (443). Hmax/Vmax are taken over all frame components (ParseFrameHeader 334-335). No defect in the interleaved path; noted for completeness.
  - Spec: ITU-T T.81 A.2.3
- **[minor]** Component count / sampling-factor validity: T.81 permits Ns and Nf from 1..255 and Hi,Vi in 1..4, with sum(Hi*Vi) <= 10 for an interleaved scan (T.81 B.2.2, B.2.3).
  - Observed: SetupGeometry (383) rejects any frame whose component count is not 1, 3, or 4 (JpegFormatException 'Unsupported component count'); a valid 2-component frame is rejected. ParseFrameHeader (332-333) correctly bounds Hi,Vi to 1..4, but there is no enforcement of sum(Hi*Vi) <= 10 for interleaved scans, so an over-large/malformed MCU is accepted rather than diagnosed.
  - Spec: ITU-T T.81 B.2.2, B.2.3
- **[info]** Edge MCUs at right/bottom borders where X or Y is not a multiple of 8*Hmax / 8*Vmax must be handled (padding on encode, cropping on decode) (T.81 A.2.4).
  - Observed: Handled acceptably: SetupGeometry allocates full padded planes (PlaneWidth=BlocksWide*8, PlaneHeight=BlocksHigh*8, lines 391-398); StoreBlock (455-483) always writes complete 8x8 blocks into the padded plane; AssembleImage* crop to _width x _height (492, 512-516, 627-628). Upsampling clamps at borders (ChromaSampler.UpsampleLinear 136-137,147-148). No defect; documented for the record.
  - Spec: ITU-T T.81 A.2.4
- **[info]** Block entropy layout: DC differential + AC run/size, zig-zag order (T.81 F.1.2).
  - Observed: BlockScanCoder.DecodeBlock (91-133) correctly decodes DC diff (predictor+Extend), AC run/size with ZRL (run==15,size==0) and EOB (size==0), producing zig-zag order later converted by ZigZag.ToNatural (DecodeScan line 440). Correct; noted only to confirm block-internal ordering is spec-compliant.
  - Spec: ITU-T T.81 F.1.2

## Required fixes

- In DecodeScan, branch on _scan.Components.Length: when Ns==1 (non-interleaved), iterate one data unit per MCU over a per-component grid of CeilDiv(ComponentActualWidth(c),8) x CeilDiv(ComponentActualHeight(c),8) blocks (reuse the ComponentActualWidth/Height + CeilDiv helpers already in BaselineDecoder.Progressive.cs), storing at (bx*8,by*8); do NOT use the interleaved _mcusPerRow/_mcusPerCol*Hi/Vi geometry for single-component scans.
- Support multiple baseline scans: after DecodeScan consumes one scan, resume the marker loop (as DecodeProgressive does) to parse subsequent DHT/DRI/SOS segments and decode each scan into its component plane, rather than ParseHeaders returning permanently at the first SOS.
- For non-interleaved scans, count restart intervals in single data units (one block per MCU) rather than interleaved MCUs, matching the DecodeAcFirst blockIndex logic in the progressive path.
- Relax SetupGeometry's component-count guard to also accept 2-component frames (or justify the restriction), and add enforcement of sum(Hi*Vi) <= 10 for interleaved scans per T.81 B.2.3.

## Code locations

- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:381-400 SetupGeometry (MCU count, per-component BlocksWide/High, plane allocation)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:402-451 DecodeScan (MCU iteration, SOS-order component loop, raster block order, restart handling) — no non-interleaved/Ns==1 branch`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:154-166 ParseHeaders (returns after first SOS; no multi-scan loop for baseline)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:303-338 ParseFrameHeader (Hi,Vi bounds 1..4; Hmax/Vmax)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:340-371 ParseScanHeader (SOS component list -> _scan.Components in scan order)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:455-483 StoreBlock (level shift/clamp into padded plane)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:485-617 AssembleImage/UpsampleToFull (crop to X x Y)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.Progressive.cs:136-166 DecodeAcFirst + 346-348 ComponentActualWidth/Height (correct non-interleaved geometry the baseline path lacks)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Coding/BlockScanCoder.cs:91-133 DecodeBlock (DC diff + AC run/size, zig-zag)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Color/ChromaSampler.cs:119-156 UpsampleLinear (edge clamp)`

## Test coverage

**Existing:**
- BaselineRoundTripTests.cs::Rgb_RoundTrips_ForEachSubsampling — round-trips 4:4:4/4:2:2/4:2:0/4:1:1 at 40x40, exercising the INTERLEAVED MCU path (SOS-order components, Hi*Vi blocks in raster order); correctness of interleaved data-unit ordering is proven indirectly by pixel fidelity (T.81 A.2.3, minor finding — interleaved path only).
- EdgeCaseTests.cs::Rgb420_AllDimensions_PreserveSizeAndDecode — 4:2:0 at 1x1,1x17,17x1,23x41,33x33,64x1, etc.; proves interleaved MCU count = ceil(X/16) x ceil(Y/16) and correct edge cropping to WxH indirectly via size + successful decode (T.81 A.2.4 edge handling, interleaved).
- EdgeCaseTests.cs::Grayscale_AllDimensions_PreserveSizeAndDecode — single-component (Ns=1) grayscale with H=V=1 across many odd dims; exercises the 1-block-per-MCU path, but ONLY for H=V=1 where the interleaved grid coincides with correct non-interleaved geometry (does not exercise Hi>1).
- BaselineRoundTripTests.cs::OddDimensions_PreserveSizeAndDecode — grayscale 1x1,7x3,17x9,9x16; confirms non-multiple-of-8 edge cropping for H=V=1 (T.81 A.2.4).
- StructureComplianceTests.cs::Sof0_EncodesDimensionsAndComponentCount — asserts SOF0 encodes precision/H/W/component-count and the luma 0x22 (2x2) sampling byte for 4:2:0 (encoder-side sampling-factor emission).
- SamplingFactorValidationTests.cs::SamplingFactorAboveFour_Throws — rejects Hi or Vi > 4 (0x55,0x51,0x15,0xF1) (T.81 B.2.2 sampling-factor 1..4 bound).
- SamplingFactorValidationTests.cs::ValidSamplingFactors_AreAcceptedByRealEncoderOutput — confirms encoder factors 1/2/4 for all subsampling modes still decode after the [1,4] validation.
- RestartIntervalTests.cs::Rgb420_WithRestartInterval_RoundTrips — RSTn consumed on interleaved MCU boundaries for 4:2:0 (T.81 restart on interleaved scan).
- RestartIntervalTests.cs::Grayscale_WithRestartInterval_RoundTrips — RSTn consumed on single-block (H=V=1) MCU boundaries; partial coverage of restart-on-single-block, but not for a genuinely non-interleaved scan with Hi>1.
- RealImageIntegrationTests.cs::RealImage_Progressive_MatchesBaseline — baseline vs progressive planes byte-identical for 4:4:4 and 4:2:0 on real images; proves baseline/progressive parity for the INTERLEAVED geometry only.
- EdgeCaseTests.cs::Progressive_TinyImages_AllSubsampling — baseline vs progressive byte-identical across subsampling on tiny images (interleaved geometry parity).
- BlockCoderTests.cs::SingleBlock_RoundTrips / DcPrediction_ChainsAcrossBlocks / DcOnlyBlock_EmitsEob / LongZeroRun_UsesZrlAndRoundTrips / FullNonZeroBlock_RoundTrips — verify block-internal entropy layout: DC differential + AC run/size with ZRL and EOB in zig-zag order (T.81 F.1.2, info finding).
- CombinationTests.cs::Cmyk_WithMetadata_RoundTrips + EdgeCaseTests.cs::SinglePixel_EachColorSpace — implicitly confirm component counts 1 (grayscale), 3 (RGB/YCbCr) and 4 (CMYK) are accepted by SetupGeometry.

**Gaps:**
- MAJOR (unproven): Non-interleaved single-component scan where the scanned component has Hi=Hmax>1 or Vi=Vmax>1. DecodeScan (BaselineDecoder.cs:417-450) always iterates the interleaved grid and decodes c.V*c.H blocks per component, so per-component dims (blocksPerLine=ceil(ceil(X*Hi/Hmax)/8)) are never used. No test decodes such a stream — the encoder cannot emit one (always single interleaved SOS), and there are no hand-crafted/interop JPEG assets. Completely uncovered (T.81 A.2.2, A.2.4).
- MAJOR (unproven): Multi-scan baseline sequential frame (one component per SOS, 2+ scans). ParseHeaders returns at the FIRST SOS and the non-progressive path decodes exactly one scan, ignoring subsequent DHT/DRI/SOS. No test feeds a multi-scan baseline stream; all decode inputs come from the encoder (single interleaved scan) or golden fixtures (T.81 A.2, B.2.3).
- MINOR (unproven): 2-component (Ns=2/Nf=2) frame handling. SetupGeometry rejects component counts not in {1,3,4}, but no test asserts that a 2-component frame is explicitly and correctly rejected (or supported) (T.81 B.2.2, B.2.3).
- MINOR (unproven): sum(Hi*Vi) <= 10 constraint for interleaved scans is neither enforced in code nor tested; an over-large MCU is silently accepted (T.81 B.2.2).
- PARTIAL: Restart intervals on a genuinely non-interleaved scan (single data-unit boundaries with Hi>1). Only interleaved and H=V=1 grayscale restart cases exist; the non-interleaved-with-Hi>1 boundary path is untested.
- PARTIAL: MCU count = ceil(X/(8*Hmax)) x ceil(Y/(8*Vmax)) is only asserted indirectly (via output size + fidelity) for 4:2:0; 4:2:2 (2x1) and 4:1:1 (4x1) at non-multiple dimensions have no dimension-specific MCU/crop assertion, and no test asserts the exact MCU/block count directly.

**Required new tests:**
- `NonInterleavedSingleComponentScan_WithLumaHi2_DecodesCorrectBlockCount` (decoder, ITU-T T.81 A.2.2, A.2.4): Decode a hand-crafted baseline stream whose single scanned component has Hi=Hmax=2 (e.g. X=8,Y=8, SOF luma factor 0x21 or 0x22) so the correct per-component geometry (blocksPerLine=ceil(ceil(X*Hi/Hmax)/8)=1) differs from the interleaved-grid count (mcusPerRow*Hi=2). Confirms the entropy stream stays synchronized and pixels are correct. Directly targets the primary major finding. → Decoder uses per-component dims and decodes the correct data-unit count; output pixels match. Under current code it over-decodes and desynchronizes — test FAILS until the Ns==1 branch is added.
- `MultiScanBaseline_ThreeNonInterleavedScans_RoundTrips` (interop, ITU-T T.81 A.2, B.2.3): Feed a baseline SOF0 frame coded as 3 separate single-component SOS segments (one per component) and assert an exact/near-exact round-trip of all three planes. Targets the second major finding (only the first scan is decoded). → All three scans are decoded and all components reconstruct correctly. Under current code only the first scan is processed and components 2/3 are garbage — test FAILS until the non-progressive multi-scan marker loop is added.
- `TwoComponentFrame_IsRejectedWithClearError` (decoder, ITU-T T.81 B.2.2, B.2.3): Decode a frame declaring Nf=2 components and assert it is diagnosed (JpegFormatException) rather than silently mis-decoded, pinning the documented behavior of SetupGeometry's {1,3,4} restriction. → JpegFormatException ('Unsupported component count 2') is thrown. Currently passes (documents intended rejection); guards against silent acceptance regressions.
- `InterleavedMcuCount_422And420And411_AtNonMultipleDims_MatchesCeilFormula` (round-trip, ITU-T T.81 A.2.3, A.2.4): Round-trip 4:2:2 (e.g. 25x9), 4:2:0 (17x17) and 4:1:1 (9x9) and assert output is exactly WxH with correct edge content, exercising MCU count = ceil(X/(8*Hmax)) x ceil(Y/(8*Vmax)) and edge cropping for Hmax=2 and Hmax=4. → Decoded dimensions equal WxH and edge pixels are within lossy tolerance for every subsampling; passes on current interleaved path (regression guard for MCU/crop geometry).
- `NonInterleavedScan_WithRestartInterval_ConsumesRstOnDataUnitBoundaries` (decoder, ITU-T T.81 A.2.4, B.2.3): Decode a hand-crafted non-interleaved single-component scan (Hi=2) with a DRI so RSTn markers fall on single data-unit boundaries; verify markers are consumed and predictors reset correctly. → RSTn markers are consumed on the correct data-unit boundaries and the plane decodes correctly. FAILS under current code because the non-interleaved data-unit count (and thus restart boundary) is wrong.
- `BaselineVsProgressive_NonInterleavedGeometryParity` (regression, ITU-T T.81 A.2.2, A.2.4): For a source that yields Hi=Hmax>1 in a non-interleaved baseline coding, assert the baseline-decoded planes equal the progressive-decoded planes of the same source, cross-checking the baseline non-interleaved geometry against the already-correct progressive ComponentActualWidth/Height path. → Baseline and progressive planes are byte-identical (within lossy encode equivalence). FAILS while the baseline path lacks per-component geometry; passes once fixed, then guards against divergence.
- `InterleavedScan_SumHiVi_ExceedingTen_IsDiagnosed` (decoder, ITU-T T.81 B.2.2): Decode an interleaved scan whose sum(Hi*Vi) > 10 and assert it is rejected with a clear error rather than accepted, covering the unenforced B.2.2 MCU-size constraint. → JpegFormatException is thrown. Currently FAILS/absent because the sum(Hi*Vi)<=10 check is not implemented; documents the gap and drives the fix.

