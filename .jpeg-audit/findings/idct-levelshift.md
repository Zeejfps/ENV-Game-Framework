# Inverse DCT, level shift, clamp/round

**Section key:** `idct-levelshift`  
**Compliance:** PASS · **Adversarial justified:** false · **Final:** **FAIL**

## Findings

- **[info]** T.81 A.3.3: IDCT per equation with normalization 1/4·C(u)C(v) and orthonormal factor verification
  - Observed: Correct. Both the reference Dct and hot-path FastDct use the orthonormal basis c(0)=sqrt(1/8), c(u!=0)=sqrt(2/8)=1/2 (Dct.cs:114-120, FastDct.cs:127-131). This exactly reproduces the spec's 1/4·C(u)C(v)·cos·cos separable IDCT: verified for all three cases (u,v both !=0 => 1/4; u=0,v!=0 => 1/(4·sqrt2); u=v=0 => 1/8). FastDct.Inverse's even/odd decomposition (FastDct.cs:112-125) correctly applies the (-1)^u column symmetry to yield output[x]=E+O and output[7-x]=E-O.
  - Spec: ITU-T T.81 A.3.3
- **[info]** T.81 A.3.1: level shift adds 2^(P-1) (128 for 8-bit, 2048 for 12-bit)
  - Observed: Correct. 8-bit path adds literal 128 (BaselineDecoder.cs:464). High-precision path computes center = 1 << (_precision - 1) (BaselineDecoder.cs:471,478), giving 2048 for P=12. Level shift is applied AFTER the IDCT and rounding, as required on the decode side.
  - Spec: ITU-T T.81 A.3.1
- **[info]** Output clamped to [0, 2^P-1]
  - Observed: Correct. 8-bit clamps to [0,255] via Math.Clamp then casts to byte (BaselineDecoder.cs:465). High-precision clamps to [0, (1<<_precision)-1] then casts to ushort (BaselineDecoder.cs:472,479). Clamp is correctly performed after level shift.
  - Spec: ITU-T T.81 A.3.1
- **[minor]** Result rounded to nearest integer
  - Observed: Rounding uses Math.Round(spatial) (BaselineDecoder.cs:464,478) which defaults to MidpointRounding.ToEven (banker's rounding) rather than round-half-away-from-zero used by reference decoders (e.g. libjpeg's effective +0.5). At exact half-way IDCT outputs the two implementations can differ by 1 LSB. This is still a valid 'nearest integer' choice and half-way outputs from a double-precision IDCT are essentially measure-zero, so it stays within any T.83 statistical tolerance; flagged only as a deviation from the conventional rounding convention.
  - Spec: ITU-T T.81 A.3.3 (round to nearest integer)
- **[info]** FDCT/IDCT accuracy should meet T.83 if claimed
  - Observed: FastDct is documented as 'numerically exact' and validated against the double-precision reference Dct. Because both operate entirely in IEEE double precision, error versus an ideal IDCT is far below T.83's peak/mean/rms bounds; conformance is effectively guaranteed. However, no explicit T.83 accuracy conformance test (the 10000-block pseudo-random test vectors) is present in these files to formally substantiate the 'exact' claim.
  - Spec: ITU-T T.83

## Adversarial — overlooked violations

- **[major]** StoreBlock (BaselineDecoder.cs:464 8-bit, :478 high-precision) computes `value = (int)Math.Round(spatial) + center` in unchecked int arithmetic (net10.0, no CheckForOverflowUnderflow). Dequantized coefficients reach ~2.147e9 each (short coeff up to 32767 times ushort quant step up to 65535, Quantizer.cs:45), so an IDCT output pixel can exceed int.MaxValue. The double->int conversion saturates to int.MaxValue, then `+ center` overflows and wraps to a large NEGATIVE int, so Math.Clamp(negative,0,max) returns 0 instead of the required 2^P-1. The explicit clamp-to-max requirement is inverted for large-positive IDCT results: a should-be-saturated-white pixel decodes as black (0). The reviewer's 'Correct. clamp performed after level shift' finding overlooked that the level-shift addition itself overflows before the clamp sees a sane value. (ITU-T T.81 A.3.1 (output clamped to [0, 2^P-1]))
- **[minor]** Decode16 accepts precision 9,10,11 (BaselineDecoder.cs:105) and StoreBlock uses center=1<<(precision-1), max=(1<<precision)-1 for them. T.81 only defines DCT sample precision 8 and 12; decoding 9-11 bit as if valid produces output for a stream a strict conformant decoder should reject (or at least is untested against any conformance vector). Combined with the overflow above, the clamp guarantees are unverified for these precisions. (ITU-T T.81 A.3.1 / clamp range validity for non-standard precisions)
- **[info]** Confirming the reviewer's own minor finding: Math.Round defaults to MidpointRounding.ToEven (banker's rounding) at :464/:478, whereas Quantizer.Quantize deliberately uses MidpointRounding.AwayFromZero (Quantizer.cs:26). The forward/inverse rounding conventions are inconsistent with each other and with libjpeg's round-half-up; at exact-half IDCT outputs this yields a 1-LSB difference from reference decoders. Measure-zero for a double IDCT, so within T.83 tolerance, but a real convention deviation. (ITU-T T.81 A.3.3 (round to nearest integer))

## Counterexamples

- 12-bit clamp inversion: A valid SOF1/SOF2 12-bit stream (P=12, center=2048, max=4095) with a DQT of 16-bit precision whose steps are 65535, and a block whose entropy-decoded coefficients (each short, e.g. +32767) are sign-aligned so the IDCT output at pixel (0,0) reaches, say, 8e9. spatial=8e9 -> (int)Math.Round=2147483647 (saturated) -> +2048 -> unchecked wrap to -2147481601 -> Math.Clamp(-2147481601,0,4095)=0. Correct output is 4095. Pixel decodes black instead of white.
- 8-bit clamp inversion via oversized quant table: Decode() path (P=8) with a 16-bit DQT (Pq!=0 is accepted by ParseQuantTables regardless of sample precision) with steps 65535 and AC categories up to 15 (DecodeBlock permits category up to 15, dcCategory up to 16, magnitude up to 32767). A few aligned coefficients push one IDCT pixel past 2.147e9 -> (int) saturates to int.MaxValue -> +128 wraps negative -> Clamp -> 0 instead of 255.
- Minimal reinforcement: only ~4-8 coefficients each ~2.1e9 (32767*65535) aligned in sign at a pixel exceed int.MaxValue*(1/0.25 basis)~2.15e9, so the overflow does not require a fully dense block; it is easily produced by a hand-crafted but structurally valid entropy segment.
- Banker's-rounding divergence: an IDCT output of exactly x.5 (e.g. spatial=2.5) yields Math.Round=2 (to-even) whereas libjpeg/round-half-up yields 3; after +128 the stored sample differs by 1 from the conventional reference at that sample.

## Code locations

- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Transforms/Dct.cs:78-123 (Dct.Inverse + BuildBasis: reference orthonormal IDCT and normalization factors)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Transforms/FastDct.cs:71-131 (FastDct.Inverse, Inverse1D even/odd decomposition, Basis: hot-path IDCT)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:442 (FastDct.Inverse invocation per block)`
- `/Users/zee-seriesai/src/cs/ENV-Game-Framework/JpegSharp/Decoder/BaselineDecoder.cs:455-483 (StoreBlock: round + level shift + clamp for 8-bit and 9-12 bit)`

## Test coverage

**Existing:**
- DctTests.cs::Inverse_OfConstantDc_ProducesFlatBlock — proves reference IDCT normalization for the DC-only case (coeff 8*30 reconstructs a flat 30 block), confirming the orthonormal 1/8 DC factor of T.81 A.3.3
- DctTests.cs::Forward_ConstantBlock_ProducesOnlyDc — proves the DC normalization factor (flat block => DC = 8*sample, all AC = 0)
- DctTests.cs::ForwardThenInverse_RoundTrips — proves reference FDCT/IDCT are exact inverses to 6 decimals over a deterministic pattern
- DctTests.cs::Transform_IsEnergyPreserving — proves orthonormality via Parseval (energy in == energy out), substantiating the 1/4·C(u)C(v) normalization
- DctTests.cs::Forward_IsLinear — proves DCT linearity
- DctTests.cs::Forward_SingleVerticalRamp_MatchesReference — proves basis-function directionality/normalization for a single AC coefficient
- FastDctTests.cs::FastInverse_MatchesReference — proves the hot-path FastDct.Inverse matches the double-precision reference Dct.Inverse to 6 decimals (50 random blocks, coeffs in [-500,500]); partial substantiation of the 'numerically exact' claim
- FastDctTests.cs::FastForward_MatchesReference — proves FastDct.Forward matches reference
- FastDctTests.cs::FastForwardInverse_RoundTrips — proves FastDct forward+inverse round-trips exactly
- FastDctTests.cs::FastForward_FlatBlock_ProducesOnlyDc — proves FastDct DC normalization
- HighPrecisionCodecTests.cs::Grayscale12_RoundTrips — end-to-end 12-bit decode implicitly exercises StoreBlock center=2048/max=4095 and asserts decoded precision==12; tolerance-based, so it proves the 12-bit level-shift/clamp path works for in-range smooth data
- HighPrecisionCodecTests.cs::RgbDirect12_RoundTrips / RgbYCbCr12_RoundTrips / Cmyk12_RoundTrips (and progressive/subsampled variants) — further exercise the 12-bit StoreBlock path end-to-end within a tolerance
- HighPrecisionCodecTests.cs::Encode16_RejectsNon12BitPrecision — proves the ENCODE side rejects precisions other than 12 (NotSupportedException); does not cover the DECODE side accepting 9/10/11
- QuantizationTests.cs::Quantize_DividesAndRoundsToNearest — proves the FORWARD (encode) rounding convention is MidpointRounding.AwayFromZero (10.5=>11, -10.5=>-11); documents the convention that StoreBlock's IDCT rounding is inconsistent with

**Gaps:**
- Level-shift center on decode: no test feeds an all-zero coefficient block through the decode IDCT+StoreBlock and asserts every reconstructed sample equals the center (128 for P=8, 2048 for P=12). Existing DctTests only cover the raw transform, not the level shift added in StoreBlock (BaselineDecoder.cs:464,478).
- Clamp saturation on decode: no test drives IDCT output well below 0 and well above 2^P-1 and asserts samples saturate exactly at 0 and 2^P-1, for both P=8 and P=12. Round-trip tests only use in-range smooth gradients that never exercise the clamp.
- Level-shift overflow (adversarial major bug): no test constructs a block whose dequantized coefficients push the IDCT pixel beyond int.MaxValue so that (int)Math.Round(spatial)+center overflows/wraps negative and Math.Clamp returns 0 instead of 2^P-1. The clamp-to-max requirement (T.81 A.3.1) is unverified against overflow (BaselineDecoder.cs:464,478).
- IDCT rounding convention on decode: no test pins StoreBlock's Math.Round behavior (MidpointRounding.ToEven / banker's rounding) at exact half-integer IDCT outputs, nor documents the deliberate divergence from the encoder's AwayFromZero and from libjpeg's round-half-up.
- T.83 accuracy conformance: no formal T.83 pseudo-random 10000-block IDCT accuracy test (peak<=1, mean<=0.015, rms<=0.06 per-pixel). FastInverse_MatchesReference only compares two double-precision implementations over 50 blocks and does not substantiate T.83 conformance against an ideal/high-precision reference.
- Output-range property invariant: no property test asserting every stored decode sample lies in [0, 2^P-1] across random valid blocks for P=8 and P=12, and that monotonic scaling of a dominant positive coefficient never causes a stored sample to DECREASE (guards the wrap-to-0 inversion).
- Non-standard precision handling: no test that Decode16 either rejects or has explicit level-shift/clamp coverage for P in {9,10,11}, which BaselineDecoder.cs:105 accepts though T.81 defines only P=8 and P=12.
- Direct StoreBlock unit coverage: StoreBlock is only exercised indirectly via full-image round-trips; its level-shift/round/clamp arithmetic is never asserted in isolation for either precision path.

**Required new tests:**
- `Decode_AllZeroCoefficientBlock_ReconstructsLevelShiftCenter_8bit` (decoder, ITU-T T.81 A.3.1): Verify that an all-zero DCT coefficient block decodes to a flat plane equal to the 8-bit level-shift center (128), confirming the level shift is applied after IDCT. → Every reconstructed 8-bit sample in the block equals exactly 128.
- `Decode_AllZeroCoefficientBlock_ReconstructsLevelShiftCenter_12bit` (decoder, ITU-T T.81 A.3.1): Verify an all-zero coefficient block decodes to the 12-bit level-shift center (2048) via Decode16, confirming center = 1<<(P-1) for P=12. → Every reconstructed 12-bit sample equals exactly 2048.
- `Decode_LargePositiveDcOnly_SaturatesToMax_8bit_and_12bit` (decoder, ITU-T T.81 A.3.1): Feed a DC-only block whose dequantized DC drives IDCT output far above 2^P-1 and assert samples clamp exactly at the max (255 for P=8, 4095 for P=12), not wrap to a smaller value. → All samples equal 255 (P=8) / 4095 (P=12); none decode as 0 or a mid value.
- `Decode_LargeNegativeDcOnly_SaturatesToZero_8bit_and_12bit` (decoder, ITU-T T.81 A.3.1): Feed a DC-only block whose dequantized DC drives IDCT output far below 0 and assert samples clamp exactly at 0 for both precisions. → All samples equal 0 for both P=8 and P=12.
- `Decode_ExtremeCoefficientOverflow_SaturatesToMaxNotZero` (regression, ITU-T T.81 A.3.1): Pin the adversarial overflow bug: construct a block (via crafted quantized coeffs + max quant table) whose dequantized IDCT pixel exceeds int.MaxValue so that (int)Math.Round(spatial)+center overflows to a negative int and Math.Clamp yields 0. Assert the stored should-be-white sample equals 2^P-1. Fix requires clamping in double/long domain before the int cast. → Stored sample == 2^P-1 (saturated). Currently EXPECTED TO FAIL until StoreBlock clamps before the narrowing conversion (BaselineDecoder.cs:464,478).
- `StoreBlock_RoundsHalfToEven_AndDocumentsConvention` (regression, ITU-T T.81 A.3.3): Construct IDCT outputs at exact +x.5 and -x.5 midpoints and assert the stored sample follows MidpointRounding.ToEven (banker's rounding), locking the chosen convention and flagging its divergence from the encoder's AwayFromZero (Quantizer) and libjpeg's round-half-up. → Half-integer outputs round to the nearest even integer (e.g. 0.5+128 => 128, 1.5+128 => 130); test documents this as the intentional, locked convention.
- `FastDct_Inverse_T83AccuracyConformance` (decoder, ITU-T T.83): Run the ITU-T T.83 pseudo-random 10000-block IDCT accuracy procedure comparing FastDct.Inverse against a high-precision (extended/ideal) reference IDCT, computing peak, mean and RMS per-pixel error, to formally substantiate the 'numerically exact' claim. → Peak error <= 1, overall mean error <= 0.015, and RMS error <= 0.06 per pixel for every input class.
- `Decode_StoredSamplesAlwaysInRange_And_MonotonicInDc_Property` (decoder, ITU-T T.81 A.3.1): Property test over random valid coefficient blocks (P=8 and P=12): assert every reconstructed sample lies in [0, 2^P-1], and that monotonically increasing a single dominant positive coefficient never causes a stored sample to decrease (guards against the overflow wrap-to-0 inversion). → All samples within [0,2^P-1]; sample values are non-decreasing as the dominant positive coefficient increases toward saturation.
- `Decode16_NonStandardPrecision_9_10_11_RejectedOrCovered` (decoder, ITU-T T.81 A.3.1): Assert Decode16 either rejects a stream declaring precision 9/10/11 (T.81 defines only 8 and 12) or, if intentionally supported, produces correctly level-shifted (center=1<<(P-1)) and clamped ([0,2^P-1]) output for those precisions. → Either a JpegFormatException/NotSupportedException is thrown, or reconstructed samples honor center and max for P in {9,10,11}; behavior is explicitly asserted rather than untested (BaselineDecoder.cs:105).
- `Encode16ThenDecode16_TwelveBit_NoOutOfRangeSamples` (round-trip, ITU-T T.81 A.3.1): Round-trip a 12-bit image containing saturated (0 and 4095) regions and assert StoreBlock produces no out-of-range ushort and preserves the saturated extremes, validating center=2048/max=4095 under real saturation rather than only smooth gradients. → All decoded samples in [0,4095]; pure-black (0) and pure-white (4095) input regions decode at or adjacent to 0 and 4095 within tolerance, never wrapped.

