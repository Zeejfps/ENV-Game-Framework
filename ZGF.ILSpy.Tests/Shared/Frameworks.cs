using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

internal static class Frameworks
{
    public static TargetFrameworkId Net48 { get; } = Id(".NETFramework,Version=v4.8");

    /// <summary>The framework id <paramref name="text"/> writes; throws when it is not one.</summary>
    public static TargetFrameworkId Id(string text) =>
        TargetFrameworkId.Parse(text) ?? throw new ArgumentException($"Not a framework id: '{text}'", nameof(text));
}
