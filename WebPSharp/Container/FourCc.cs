using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace WebPSharp.Container;

/// <summary>
/// A four-character code (FourCC) identifying a RIFF chunk. Stored as the four raw bytes packed
/// little-endian, matching the on-disk layout, so it can be compared against a value read
/// directly from the stream with a single integer comparison.
/// </summary>
public readonly struct FourCc : IEquatable<FourCc>
{
    private readonly uint _packed;

    /// <summary>Creates a code from its packed little-endian representation.</summary>
    /// <param name="packed">The four bytes packed with the first character in the low byte.</param>
    public FourCc(uint packed) => _packed = packed;

    /// <summary>Creates a code from four bytes.</summary>
    /// <param name="a">First byte.</param>
    /// <param name="b">Second byte.</param>
    /// <param name="c">Third byte.</param>
    /// <param name="d">Fourth byte.</param>
    public FourCc(byte a, byte b, byte c, byte d)
        => _packed = (uint)(a | (b << 8) | (c << 16) | (d << 24));

    /// <summary>Creates a code from a four-character ASCII string.</summary>
    /// <param name="tag">A string of exactly four ASCII characters.</param>
    /// <exception cref="ArgumentException"><paramref name="tag"/> is not exactly four ASCII characters.</exception>
    public FourCc(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (tag.Length != 4)
            throw new ArgumentException("A FourCC must be exactly four characters.", nameof(tag));
        uint packed = 0;
        for (var i = 0; i < 4; i++)
        {
            var ch = tag[i];
            if (ch > 0x7F)
                throw new ArgumentException("A FourCC must contain only ASCII characters.", nameof(tag));
            packed |= (uint)ch << (i * 8);
        }
        _packed = packed;
    }

    /// <summary>The packed little-endian representation.</summary>
    public uint Packed => _packed;

    /// <summary>Reads a code from the first four bytes of a span.</summary>
    /// <param name="source">A span of at least four bytes.</param>
    /// <returns>The parsed code.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FourCc Read(ReadOnlySpan<byte> source)
        => new(BinaryPrimitives.ReadUInt32LittleEndian(source));

    /// <summary>Writes the four bytes of this code into a span.</summary>
    /// <param name="destination">A span of at least four bytes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Span<byte> destination)
        => BinaryPrimitives.WriteUInt32LittleEndian(destination, _packed);

    /// <inheritdoc/>
    public bool Equals(FourCc other) => _packed == other._packed;

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is FourCc other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => (int)_packed;

    /// <summary>Compares two codes for equality.</summary>
    public static bool operator ==(FourCc left, FourCc right) => left.Equals(right);

    /// <summary>Compares two codes for inequality.</summary>
    public static bool operator !=(FourCc left, FourCc right) => !left.Equals(right);

    /// <summary>Returns the four characters of the code as a string, with non-printable bytes shown as '?'.</summary>
    /// <returns>The four-character representation.</returns>
    public override string ToString()
    {
        Span<char> chars = stackalloc char[4];
        for (var i = 0; i < 4; i++)
        {
            var b = (byte)(_packed >> (i * 8));
            chars[i] = b is >= 0x20 and < 0x7F ? (char)b : '?';
        }
        return new string(chars);
    }
}
