using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public class WebKeysTests
{
    [Theory]
    [InlineData(KeyboardKey.Escape, 0x09)]
    [InlineData(KeyboardKey.W, 0x19)]
    [InlineData(KeyboardKey.A, 0x26)]
    [InlineData(KeyboardKey.Space, 0x41)]
    [InlineData(KeyboardKey.NumLock, 0x4D)]
    [InlineData(KeyboardKey.NumpadEnter, 0x68)]
    [InlineData(KeyboardKey.RightControl, 0x69)]
    [InlineData(KeyboardKey.PrintScreen, 0x6B)]
    [InlineData(KeyboardKey.Home, 0x6E)]
    [InlineData(KeyboardKey.UpArrow, 0x6F)]
    [InlineData(KeyboardKey.LeftArrow, 0x71)]
    [InlineData(KeyboardKey.Delete, 0x77)]
    [InlineData(KeyboardKey.NumpadEquals, 0x7D)]
    [InlineData(KeyboardKey.Pause, 0x7F)]
    [InlineData(KeyboardKey.LeftSuper, 0x85)]
    [InlineData(KeyboardKey.Menu, 0x87)]
    [InlineData(KeyboardKey.F13, 0xBF)]
    [InlineData(KeyboardKey.F24, 0xCA)]
    public void Linux_key_code_is_the_xkb_keycode_chromium_maps_to_the_dom_code(KeyboardKey key, int xkb)
    {
        Assert.Equal(xkb, WebKeys.LinuxKeyCodeOf(key));
    }

    [Fact]
    public void Every_key_with_a_windows_scan_code_has_a_distinct_linux_key_code()
    {
        var seen = new Dictionary<int, KeyboardKey>();
        foreach (var key in Enum.GetValues<KeyboardKey>())
        {
            if (WebKeys.WindowsNativeKeyCodeOf(key) == 0)
                continue;
            var code = WebKeys.LinuxKeyCodeOf(key);
            Assert.True(code != 0, $"{key} has no Linux key code");
            Assert.True(seen.TryAdd(code, key), $"{key} and {(seen.TryGetValue(code, out var other) ? other : key)} share Linux key code {code}");
        }
    }

    [Fact]
    public void Keys_without_a_position_have_no_linux_key_code()
    {
        Assert.Equal(0, WebKeys.LinuxKeyCodeOf(KeyboardKey.Unknown));
        Assert.Equal(0, WebKeys.LinuxKeyCodeOf(KeyboardKey.F25));
    }
}
