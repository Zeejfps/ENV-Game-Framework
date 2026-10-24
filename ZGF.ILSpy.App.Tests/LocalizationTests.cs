using System.Globalization;
using System.Reflection;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Preferences;

namespace ZGF.ILSpy.App.Tests;

public sealed class LocalizationTests
{
    public static TheoryData<Locale> AllLocales() => new(Enum.GetValues<Locale>());

    [Fact]
    public void Every_locale_is_offered_in_the_language_menu()
    {
        Assert.Equal(Enum.GetValues<Locale>().Order(), Locales.All.Select(static l => l.Locale).Order());
    }

    [Theory]
    [MemberData(nameof(AllLocales))]
    public void Every_string_of_a_locale_formats(Locale locale)
    {
        var strings = Strings.For(locale);
        foreach (var property in typeof(Strings).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.PropertyType == typeof(string))
                Assert.False(string.IsNullOrWhiteSpace((string?)property.GetValue(strings)), property.Name);
        }
        foreach (var method in typeof(Strings).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (method.ReturnType != typeof(string) || method.IsSpecialName) continue;
            foreach (var count in new[] { 0, 1, 2, 5, 21, 1234 })
            {
                var args = method.GetParameters().Select(p => p.ParameterType == typeof(int) ? (object)count : "x").ToArray();
                var text = (string?)method.Invoke(strings, args);
                Assert.False(string.IsNullOrWhiteSpace(text), method.Name);
                Assert.DoesNotContain("{", text);
            }
        }
    }

    [Theory]
    [InlineData("de-DE", Locale.De)]
    [InlineData("fr-CA", Locale.Fr)]
    [InlineData("pt-PT", Locale.PtBr)]
    [InlineData("zh-CN", Locale.ZhHans)]
    [InlineData("ja-JP", Locale.Ja)]
    [InlineData("ko-KR", Locale.Ko)]
    [InlineData("nl-NL", Locale.En)]
    public void The_system_language_picks_the_closest_locale(string culture, Locale expected)
    {
        Assert.Equal(expected, Locales.For(new CultureInfo(culture)));
    }

    [Fact]
    public void Switching_the_language_relabels_the_window_and_is_saved()
    {
        var app = new AppDriver();
        app.Run(2);
        Assert.True(app.Frames.Drew("File"));

        SetLocale(app.AppState, Locale.De);
        app.Run(2);

        Assert.True(app.Frames.Drew("Datei"));
        Assert.False(app.Frames.Drew("File"));
    }

    [Fact]
    public void The_chosen_language_is_restored_with_the_settings()
    {
        using var temp = new TempDir();
        var d = new AppStateDriver(temp.Settings());
        SetLocale(d.AppState, Locale.Ja);
        d.Pump();

        Assert.Equal(Locale.Ja, new AppState(temp.Settings()).Locale);
    }
}
