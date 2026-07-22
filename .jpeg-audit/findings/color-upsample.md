# Color conversion (YCbCr/RGB/CMYK/YCCK) + chroma upsampling

**Section key:** `color-upsample`  
**Compliance:** FAIL · **Adversarial justified:** n/a · **Final:** **FAIL**

## Findings

- **[blocker]** T.871 YCbCr<->RGB centered at 2^(P-1); high-precision conversion must be correct across supported precisions (9-16 bit)
  - Observed: The int-based high-precision YCbCrToRgb (ColorConverter.cs:71-79) and RgbToYCbCr (ColorConverter.cs:90-96) do all arithmetic in 32-bit int and silently overflow at 16-bit precision (unchecked arithmetic wraps). Forward: with r=g=b=65535 the single term YG*g = 38470*65535 = 2,521,131,450 > int.MaxValue (2,147,483,647), so the luma sum wraps to a negative/garbage value. Inverse: with cr=65535, center=32768, c=32767 the term CrToR*c = 91881*32767 = 3,010,548,927 overflows, and CbToB*d = 116130*32767 = 3,805,352,910 overflows. JpegImage16 explicitly supports precision 9-16 (JpegImage16.cs:30-31,62) with first-class 16-bit RGB APIs (CreateRgb / CreateFromRgba16161616), and both encode (BaselineEncoder.Color.cs:118,168) and decode (BaselineDecoder.cs:665,713) route 16-bit YCbCr through these overflowing methods. Notably CmykToRgb(int) at ColorConverter.cs:155-162 uses `long` intermediates specifically to avoid this exact 16-bit overflow (comment line 157), but the YCbCr paths were not given the same fix. Result: 16-bit YCbCr images produce corrupt pixels. Overflow begins around ~15-16 bit; <=12-bit is safe.
  - Spec: T.871 YCbCr/RGB conversion; internal support of P=9..16
- **[minor]** Chroma upsampling/downsampling must map subsampled<->full-res planes; robust edge handling
  - Observed: ChromaSampler.Downsample (ChromaSampler.cs:66 and :203) computes (sum + (count>>1))/count without guarding count==0. ValidateDimensions only checks that buffer length == width*height; it does NOT verify dstWidth/dstHeight equal SubsampledSize(src,factor) as the doc requires. If a caller passes an over-sized dst, an output cell whose source origin (dx*hFactor, dy*vFactor) lies entirely outside the source yields count==0 and throws DivideByZeroException instead of a meaningful validation error.
  - Spec: Chroma subsampling mapping; defensive dimension validation
- **[minor]** CMYK<->RGB conversion for Adobe CMYK/YCCK
  - Observed: CmykToRgb (ColorConverter.cs:134-139, 155-162) uses the naive device-independent multiplicative model R=(255-C)(255-K)/255. This is not JPEG/Adobe-specified (real Adobe CMYK is device/ICC-profile dependent and often stored inverted). It also does not itself account for Adobe inversion (that is handled separately in BaselineDecoder.AssembleCmyk:583-596). Acceptable as an approximation but will not round-trip true Adobe CMYK exactly; treat as an approximation, not a spec-exact transform.
  - Spec: Adobe APP14 CMYK/YCCK; no spec-mandated CMYK->RGB (ICC dependent)
- **[info]** T.871 forward RGB->YCbCr rounding of Cb/Cr chroma offset
  - Observed: Forward Cb/Cr (ColorConverter.cs:37-38,94-95) add (128<<16)+Half where Half=1<<15. libjpeg uses (CBCR_OFFSET + ONE_HALF - 1) i.e. Half-1 for the rounding bias. This produces an off-by-one at exact .5 tie boundaries versus libjpeg; visually and spec-wise negligible, but not bit-identical to the reference implementation.
  - Spec: T.871 rounding convention
- **[info]** Adobe APP14 transform 0=RGB/CMYK,1=YCbCr,2=YCCK
  - Observed: The transform selection is not in the reviewed ColorConverter/ChromaSampler files but is correctly implemented in the decoder: ShouldApplyYCbCr (BaselineDecoder.cs:536-548) maps transform 0->RGB, 1->YCbCr, and falls back to component-id/JFIF heuristics when no Adobe marker; AssembleCmyk (BaselineDecoder.cs:550-606) handles transform 2 (YCCK) plus Adobe CMYK inversion. Requirement is met, just outside the two named files.
  - Spec: Adobe APP14 transform semantics

## Required fixes

- Widen the high-precision YCbCr arithmetic to 64-bit (long) so it cannot overflow at 15-16 bit: in RgbToYCbCr(int,int,int,int,...) (ColorConverter.cs:90-96) compute YR*(long)r + YG*g + YB*b (and likewise for Cb/Cr), and in YCbCrToRgb(int,...) (ColorConverter.cs:71-79) compute CrToR*(long)c, CbToB*(long)d, CbToG*(long)d + CrToG*(long)c in long before the >>ScaleBits shift. Mirror the long-based approach already used in CmykToRgb(int) at line 155-162.
- Guard ChromaSampler.Downsample against count==0 (ChromaSampler.cs:66 and :203) OR validate that dstWidth==SubsampledSize(srcWidth,hFactor) and dstHeight==SubsampledSize(srcHeight,vFactor) and throw ArgumentException with a clear message instead of DivideByZeroException.
- Optionally align forward Cb/Cr rounding bias with libjpeg (use +Half-1 instead of +Half at ColorConverter.cs:37-38 and 94-95) if bit-exact parity with libjpeg reference output is a goal.
- Document that CmykToRgb is a naive multiplicative approximation, not an ICC/Adobe-accurate transform, so callers do not treat it as spec-exact.

## Code locations

- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Color/ColorConverter.cs:16-24 (YCbCr/RGB fixed-point coefficients; YR/YG/YB, CrToR=91881=1.402, CbToG=22554=0.344136, CrToG=46802=0.714136, CbToB=116130=1.772 all correct)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Color/ColorConverter.cs:34-39 (RgbToYCbCr byte)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Color/ColorConverter.cs:49-56 (YCbCrToRgb byte, correct JFIF inverse + Clamp[0,255])`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Color/ColorConverter.cs:71-79 (YCbCrToRgb high-precision; center=(maxValue+1)>>1=2^(P-1); OVERFLOWS int at 16-bit)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Color/ColorConverter.cs:90-96 (RgbToYCbCr high-precision; OVERFLOWS int at 16-bit)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Color/ColorConverter.cs:99 (ClampTo high-precision clamp to [0,maxValue])`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Color/ColorConverter.cs:134-162 (CmykToRgb byte + high-precision; long intermediates only in the high-precision variant)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Color/ColorConverter.cs:196-221 (YcckToCmyk/CmykToYcck)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Color/ColorConverter.cs:223-231 (Clamp byte [0,255])`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Color/ChromaSampler.cs:20 (SubsampledSize ceil)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Color/ChromaSampler.cs:33-69 and 170-206 (Downsample byte/ushort; DivideByZero risk at :66/:203)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Color/ChromaSampler.cs:82-106 (Upsample nearest-neighbour, unused by decoder)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Color/ChromaSampler.cs:119-156 and 219-256 (UpsampleLinear centered bilinear; used by decoder)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:504-548 (AssembleThreeComponent + ShouldApplyYCbCr, APP14 0/1 dispatch)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:550-617 (AssembleCmyk, YCCK/Adobe-invert; UpsampleToFull)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:639-734 (16-bit assembly paths using the overflowing high-precision YCbCr conversion)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Encoder/BaselineEncoder.Color.cs:118,168 (16-bit encode via RgbToYCbCr high-precision)`

## Test coverage

**Existing:**
- ColorConverterTests.cs::RgbToYCbCr_MatchesKnownValues — T.871 8-bit forward: white/black/gray map to expected Y/Cb/Cr (128 center)
- ColorConverterTests.cs::RgbToYCbCr_PureRed_ProducesExpectedLumaAndChroma — 8-bit pure-red luma ~76, Cr clamps at 255
- ColorConverterTests.cs::GrayLevels_HaveNeutralChroma — every 8-bit gray v yields Y=v, Cb=Cr=128 (achromatic center)
- ColorConverterTests.cs::RgbYCbCr_RoundTrips_WithinTolerance — 8-bit RgbToYCbCr->YCbCrToRgb round-trip within +-2 over 2000 random pixels
- ColorConverterTests.cs::YCbCrToRgb_ClampsOutOfGamut — 8-bit YCbCrToRgb clamps out-of-gamut input to [0,255]
- ColorConverterTests.cs::PlanarYCbCrToRgb_MatchesPerPixel — planar 8-bit YCbCr->RGB matches per-pixel path
- ColorConverterTests.cs::PlanarRgbToYCbCr_MatchesPerPixel — planar 8-bit RGB->YCbCr matches per-pixel path
- ColorConverterTests.cs::CmykRgb_RoundTrips_WithinTolerance — 8-bit CMYK<->RGB multiplicative model round-trip within +-1
- ColorConverterTests.cs::YcckCmyk_RoundTrips_WithinTolerance — 8-bit YCCK<->CMYK round-trip within +-2, K passes through
- HighPrecisionColorTests.cs::YCbCr12_RoundTripsWithinTolerance — 12-bit (maxValue=4095) RGB<->YCbCr round-trip within +-3 (only 12-bit, NOT 13-16)
- HighPrecisionColorTests.cs::YCbCr12_GrayIsAchromatic — 12-bit gray maps to Y=gray, Cb=Cr=2048 (center=2^(P-1) for P=12 only)
- HighPrecisionColorTests.cs::Upsample16_IsExactWhenDimensionsMatch — ushort UpsampleLinear is identity when dims match
- HighPrecisionColorTests.cs::Upsample16_DoublesResolutionAndStaysInRange — ushort UpsampleLinear preserves endpoints and clamps within [0,4095]
- ChromaSamplerTests.cs::SubsampledSize_RoundsUp — SubsampledSize ceil semantics for 4:2:0/4:1:1/identity
- ChromaSamplerTests.cs::Downsample_444_IsIdentity / Downsample_420_AveragesTwoByTwoBlocks / Downsample_411_AveragesFourWide / Downsample_OddDimensions_ClampsBlocksAtEdges — box-average downsample mapping incl. edge clamping
- ChromaSamplerTests.cs::Upsample_420_ReplicatesPixels / Upsample_422_ReplicatesHorizontallyOnly / Upsample_OddDimensions_ClampsToSourceBounds — nearest-neighbour upsample mapping and bounds clamp
- ChromaSamplerTests.cs::ConstantPlane_DownsampleThenUpsample_IsExact — constant plane survives down+up-sample exactly
- ChromaSamplerTests.cs::InvalidFactor_Throws — factor<1 throws ArgumentOutOfRangeException
- ChromaUpsampleLinearTests.cs::Identity_WhenDimensionsMatch / ConstantPlane_UpsamplesToConstant / LinearRamp_InterpolatesSmoothly_NotBlocky / MidpointOfTwoValues_IsAveraged / OutputStaysInByteRange — byte UpsampleLinear interpolation correctness and [0,255] clamp
- ColorTransformDetectionTests.cs::AdobeTransform1_TriggersYCbCrDecode — APP14 transform=1 selects YCbCr decode path (ShouldApplyYCbCr)
- ColorTransformDetectionTests.cs::RgbDirect_Progressive_MatchesRgbDirectBaseline / RgbDirect_HalfResIsNotWorseThanYCbCr420_OnColorEdges — RGB-direct (no YCbCr) decode integration
- RgbColorTransformTests.cs::RgbDirect_WritesAdobeTransform0 / RgbDirect_DoesNotApplyYCbCrTransform / RgbDirect_RoundTripsNearLossless / YCbCrEncoding_RemainsDefault — APP14 transform=0 RGB path selection and correctness
- YcckTests.cs::Ycck_WritesAdobeTransform2 / Ycck_RoundTrips_AtHighQuality / Cmyk_StillWritesAdobeTransform0 / FlatYcck_RoundTripsNearlyExact — APP14 transform=2 (YCCK) round-trip and marker write
- CmykRoundTripTests.cs::Cmyk_RoundTrips_AtHighQuality / FlatCmyk_RoundTripsNearlyExact / Cmyk_OddDimensions_Decode / Cmyk_Output_ContainsAdobeMarker — 8-bit Adobe CMYK codec round-trip incl. inversion
- HighPrecisionCodecTests.cs::RgbYCbCr12_Subsampled_RoundTrips — 12-bit 4:2:0/4:2:2/4:1:1 subsampled encode/decode round-trip tolerance
- HighPrecisionCodecTests.cs::Cmyk12_RoundTrips / Cmyk12_AsYcck_RoundTrips — 12-bit CMYK and YCCK codec round-trip

**Gaps:**
- BLOCKER — 16-bit (P=13..16) YCbCr<->RGB correctness is completely untested. HighPrecisionColorTests only exercises P=12 (maxValue=4095), well below the int-overflow threshold (~15-16 bit). The int-based YCbCrToRgb(int) and RgbToYCbCr(int) at ColorConverter.cs:71-96 overflow 32-bit int at 16-bit precision (e.g. YG*65535=2.52e9, CbToB*32767=3.8e9 both exceed int.MaxValue) yet no test drives maxValue=65535. The exact bug the CmykToRgb(int) long-intermediate fix (line 157) was made to avoid is unguarded for the YCbCr paths.
- No parameterized precision sweep. Only P=12 is proven; P=9,10,13,14,15,16 have zero coverage for center==2^(P-1) and gray->achromatic invariants.
- No golden/extreme-value test of YCbCrToRgb(int) at 16-bit saturation (cr=65535, cb=65535, y=65535) to assert outputs are valid, non-negative, non-garbage (overflow detection).
- 8-bit exact T.871 values are only proven for pure red; pure green (0,255,0) and pure blue (0,0,255) forward values, and inverse of a gray ramp, are not exact-checked.
- Chroma Downsample validation gap: ValidateDimensions (ChromaSampler.cs:266-272) only checks length==width*height, NOT that dstWidth/dstHeight equal SubsampledSize(src,factor). An over-sized dst yields an output cell with count==0 at ChromaSampler.cs:66 / :203 and throws DivideByZeroException instead of a meaningful validation error. Untested for both byte and ushort overloads.
- No unit-level assertion that a subsampled plane upsamples back to the EXPECTED full-res plane values (existing 4:2:0/4:2:2/4:1:1 coverage is only end-to-end codec round-trip tolerance, not plane-exact).
- Decoder color-space selection for non-Adobe streams (component-id 'R''G''B' heuristic and JFIF fallback in ShouldApplyYCbCr) is not directly exercised; only the Adobe-marker paths (transform 0/1/2) are tested.
- CMYK->RGB is documented as an approximation (not spec-exact / ICC-independent); no test pins this as an intentional approximation rather than a correctness guarantee.

**Required new tests:**
- `YCbCr16_RoundTrip_SaturatedPixels` (round-trip, T.871 YCbCr<->RGB centered at 2^(P-1); correct across P=9..16): Drive RgbToYCbCr(int)->YCbCrToRgb(int) at maxValue=65535 for saturated inputs (65535,65535,65535), (65535,0,0), (0,65535,0), (0,0,65535); assert each channel returns within +-1 of the original. Directly exposes the 32-bit int overflow blocker. → FAILS on current code (overflow produces wrapped/garbage values); after fixing the int paths to long/wide arithmetic, all channels within +-1.
- `YCbCrToRgb16_Extremes_ProduceValidSamples` (regression, internal support of P=16; T.871 inverse conversion): Golden/overflow-detection unit test: call YCbCrToRgb(y,cb,cr,65535,...) for extreme chroma (cr=65535,cb=65535,y=65535 and y=0) and assert outputs are in [0,65535] with no negative/garbage results; assert the CrToR*c and CbToB*d terms are computed without int overflow. → FAILS now (intermediate terms overflow int.MaxValue → nonsensical clamped output); passes once intermediates widen to long.
- `YCbCr_PrecisionSweep_CenterAndGrayAchromatic` (round-trip, T.871 centered at 2^(P-1); precision-independent coefficients): Parameterized over P=9,10,12,13,14,15,16: verify chroma center equals 2^(P-1) by checking a mid-gray (r=g=b=center) yields Y=center and Cb=Cr=center exactly, and that RgbToYCbCr/YCbCrToRgb round-trips a mid-gray back to itself. → Passes for P<=12; FAILS for P=15,16 on current code due to overflow; passes for all P after fix.
- `RgbToYCbCr_8bit_PureColors_ExactT871` (encoder, T.871 8-bit forward coefficients and [0,255] clamp): Exact-value checks against T.871 for the untested primaries: RgbToYCbCr(0,255,0) and (0,0,255) forward Y/Cb/Cr, plus YCbCrToRgb of a neutral gray ramp (Cb=Cr=128 → R=G=B=Y), and confirm out-of-range results clamp to [0,255]. → Passes on current 8-bit code; locks in exact T.871 primary values (green Y~150, blue Y~29) and gray-ramp inverse identity.
- `Downsample_OversizedDst_ThrowsValidationNotDivideByZero` (regression, Chroma subsampling mapping; defensive dimension validation): Call Downsample (byte and ushort overloads) with dstWidth/dstHeight larger than SubsampledSize(src,factor) so an output cell's source origin lies outside src (count==0); assert a meaningful ArgumentException is thrown, not DivideByZeroException. → FAILS now (throws DivideByZeroException at (sum+(count>>1))/count); passes after ValidateDimensions verifies dst == SubsampledSize(src,factor).
- `Downsample_ThenUpsampleLinear_RampMatchesExpectedPlane` (round-trip, Chroma subsampled<->full-res plane mapping): Plane-exact test: downsample a known 4:2:0/4:2:2/4:1:1 ramp plane then UpsampleLinear back to full res; assert a constant plane is reproduced exactly and a ramp stays within [0,maxValue] with no clamping artifacts, comparing against precomputed expected samples (not just codec tolerance). → Passes on current sampler code; establishes plane-level (not end-to-end) mapping coverage for byte and ushort paths.
- `Decoder_SelectsColorSpace_ForAllAdobeAndNonAdobeStreams` (decoder, Adobe APP14 transform 0=RGB/CMYK,1=YCbCr,2=YCCK; JFIF/component-id fallback): Integration: decode streams with APP14 transform=0 (RGB), transform=1 (YCbCr), transform=2 (YCCK), and a non-Adobe stream whose SOF component ids are 'R','G','B'; assert ShouldApplyYCbCr / AssembleCmyk select the correct color space for each, covering the currently-untested non-Adobe component-id heuristic. → transform 0→RGB, 1→YCbCr, 2→YCCK all decode to correct color space; non-Adobe 'RGB'-id stream decodes as RGB without applying YCbCr.

