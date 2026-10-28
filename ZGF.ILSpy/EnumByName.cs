using System.Text.Json.Serialization;

namespace ZGF.ILSpy;

/// <summary>Reads and writes <typeparamref name="T"/> by its names alone, so that a number in a file
/// is refused instead of read as a value no name stands for.</summary>
internal sealed class EnumByName<T>() : JsonStringEnumConverter<T>(allowIntegerValues: false) where T : struct, Enum;
