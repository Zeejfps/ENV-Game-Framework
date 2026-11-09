namespace ZGF.Gui.Immediate;

/// <summary>The two files a <c>Look</c> wrote, as absolute paths.</summary>
public sealed record LookFiles(string Text, string Png)
{
    public override string ToString() => $"{Text}\n{Png}";
}

/// <summary>What every harness's <c>Look</c> accepts.</summary>
public static class LookArguments
{
    public const int MaxMagnify = 16;

    /// <summary>Requires <paramref name="name"/> to name a file in the scratch folder itself: no folder, no root, no
    /// <c>.</c> or <c>..</c>, on any platform.</summary>
    /// <exception cref="ArgumentException">It does not.</exception>
    public static void RequireFileName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.Length == 0 || name is "." or ".." || name.IndexOfAny(['/', '\\', ':']) >= 0
            || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || Path.IsPathRooted(name))
            throw new ArgumentException($"A Look's name is a plain file name, not \"{name}\".", nameof(name));
    }

    /// <summary>Requires a crop around a node of a finite <paramref name="margin"/> of 0 points or more, each pixel
    /// <paramref name="magnify"/> times as wide and high, from 1 to <see cref="MaxMagnify"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">One is out of range.</exception>
    public static void RequireCrop(float margin, int magnify)
    {
        if (!float.IsFinite(margin) || margin < 0f)
            throw new ArgumentOutOfRangeException(nameof(margin), margin, "A margin is a finite number of points, 0 or more.");
        if (magnify is < 1 or > MaxMagnify)
            throw new ArgumentOutOfRangeException(nameof(magnify), magnify, $"Magnify is from 1 to {MaxMagnify}.");
    }
}
