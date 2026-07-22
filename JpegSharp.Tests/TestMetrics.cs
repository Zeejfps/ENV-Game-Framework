namespace JpegSharp.Tests;

/// <summary>
/// Shared fidelity metrics used across the test suite.
/// </summary>
internal static class TestMetrics
{
    /// <summary>Mean absolute per-sample error between two equal-length buffers.</summary>
    public static double MeanError(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Buffers must have equal length.");
        long total = 0;
        for (var i = 0; i < a.Length; i++)
            total += Math.Abs(a[i] - b[i]);
        return (double)total / a.Length;
    }

    /// <summary>Peak signal-to-noise ratio in dB (positive infinity when identical).</summary>
    public static double Psnr(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Buffers must have equal length.");
        double mse = 0;
        for (var i = 0; i < a.Length; i++)
        {
            double d = a[i] - b[i];
            mse += d * d;
        }

        mse /= a.Length;
        return mse <= 0 ? double.PositiveInfinity : 10.0 * Math.Log10(255.0 * 255.0 / mse);
    }
}
