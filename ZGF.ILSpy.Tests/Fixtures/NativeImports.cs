using System.Runtime.InteropServices;

namespace ZGF.ILSpy.Tests.Fixtures;

internal static class NativeImports
{
    [DllImport("fixture-native", EntryPoint = "fixture_answer")]
    internal static extern int Answer();
}
