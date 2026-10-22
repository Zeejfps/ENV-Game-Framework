using System.Collections.Immutable;
using System.ComponentModel;
using System.Reflection;
using ICSharpCode.Decompiler;

namespace ZGF.ILSpy.Core;

/// <summary>The C# version decompiled code is written in: syntax newer than it is not used.</summary>
public enum CSharpVersion
{
    CSharp1 = 1,
    CSharp2 = 2,
    CSharp3 = 3,
    CSharp4 = 4,
    CSharp5 = 5,
    CSharp6 = 6,
    CSharp7 = 7,
    CSharp7_1 = 701,
    CSharp7_2 = 702,
    CSharp7_3 = 703,
    CSharp8 = 800,
    CSharp9 = 900,
    CSharp10 = 1000,
    CSharp11 = 1100,
    CSharp12 = 1200,
    CSharp13 = 1300,
    CSharp14 = 1400,
}

/// <summary>What a <see cref="DecompilerToggle"/> is about.</summary>
public enum DecompilerToggleGroup
{
    /// <summary>A C# feature, used only from <see cref="DecompilerToggle.IntroducedIn"/> on.</summary>
    Language,
    Other,
    /// <summary>Clean-ups for code the F# compiler emits.</summary>
    FSharp,
}

/// <summary>One of the decompiler's on/off choices, named as the decompiler names it.</summary>
public sealed record DecompilerToggle(string Name, DecompilerToggleGroup Group, CSharpVersion? IntroducedIn, bool DefaultValue)
{
    private static readonly Dictionary<string, PropertyInfo> Properties = typeof(DecompilerSettings).GetProperties()
        .Where(static p => p.PropertyType == typeof(bool) && p.CanWrite).ToDictionary(static p => p.Name);

    /// <summary>Settings that do nothing here: we pass the decompiler no debug symbols or XML
    /// documentation, the next two only apply when assemblies load, and we export no projects.</summary>
    private static readonly HashSet<string> Excluded =
    [
        nameof(DecompilerSettings.UseDebugSymbols),
        nameof(DecompilerSettings.ShowXmlDocumentation),
        nameof(DecompilerSettings.AutoLoadAssemblyReferences),
        nameof(DecompilerSettings.ApplyWindowsRuntimeProjections),
        nameof(DecompilerSettings.UseSdkStyleProjectFormat),
        nameof(DecompilerSettings.UseNestedDirectoriesForNamespaces),
    ];

    /// <summary>Every toggle that changes decompiled code, in the decompiler's order.</summary>
    public static IReadOnlyList<DecompilerToggle> All { get; } = Discover();

    private static readonly Dictionary<string, DecompilerToggle> ByName = All.ToDictionary(static t => t.Name);

    public static DecompilerToggle? Find(string name) => ByName.GetValueOrDefault(name);

    internal void Apply(DecompilerSettings settings, bool value) => Properties[Name].SetValue(settings, value);

    private static List<DecompilerToggle> Discover()
    {
        var defaults = new DecompilerSettings();
        var toggles = new List<DecompilerToggle>();
        foreach (var property in typeof(DecompilerSettings).GetProperties())
        {
            if (property.PropertyType != typeof(bool) || !property.CanWrite || Excluded.Contains(property.Name)) continue;
            if (property.GetCustomAttribute<BrowsableAttribute>() is { Browsable: false }) continue;
            if (property.GetCustomAttribute<CategoryAttribute>()?.Category is not { } category) continue;
            var value = (bool)property.GetValue(defaults)!;
            if (Introduced(category) is { } version)
                toggles.Add(new DecompilerToggle(property.Name, DecompilerToggleGroup.Language, version, value));
            else if (category == "DecompilerSettings.Other")
                toggles.Add(new DecompilerToggle(property.Name, DecompilerToggleGroup.Other, null, value));
            else if (category == "DecompilerSettings.FSpecificOptions")
                toggles.Add(new DecompilerToggle(property.Name, DecompilerToggleGroup.FSharp, null, value));
        }
        return toggles;
    }

    /// <summary>The version a category such as <c>C# 7.3 / VS 2017.7</c> names.</summary>
    private static CSharpVersion? Introduced(string category)
    {
        if (!category.StartsWith("C# ", StringComparison.Ordinal)) return null;
        var end = category.IndexOf(' ', 3);
        var parts = category[3..(end < 0 ? category.Length : end)].Split('.');
        if (!int.TryParse(parts[0], out var major)) return null;
        var minor = parts.Length > 1 && int.TryParse(parts[1], out var m) ? m : 0;
        var value = major >= 8 ? major * 100 + minor : minor == 0 ? major : major * 100 + minor;
        return Enum.IsDefined(typeof(CSharpVersion), value) ? (CSharpVersion)value : null;
    }
}

/// <summary>The choices that change how C# is decompiled. <see cref="Toggles"/> holds only the
/// toggles set away from their defaults, and keeps them while <see cref="LanguageVersion"/> is too
/// old for them, so they come back with a newer version.</summary>
public sealed record DecompilerOptions
{
    public static DecompilerOptions Default { get; } = new();

    public CSharpVersion LanguageVersion { get; init; } = CSharpVersion.CSharp14;

    public ImmutableSortedDictionary<string, bool> Toggles { get; init; } = ImmutableSortedDictionary<string, bool>.Empty;

    /// <summary>Whether <paramref name="toggle"/> is on as chosen, whether or not the version allows it.</summary>
    public bool IsOn(DecompilerToggle toggle) => Toggles.TryGetValue(toggle.Name, out var value) ? value : toggle.DefaultValue;

    /// <summary>Whether <see cref="LanguageVersion"/> is new enough for <paramref name="toggle"/>.</summary>
    public bool Allows(DecompilerToggle toggle) => toggle.IntroducedIn is not { } version || version <= LanguageVersion;

    public DecompilerOptions With(DecompilerToggle toggle, bool value) => this with
    {
        Toggles = value == toggle.DefaultValue ? Toggles.Remove(toggle.Name) : Toggles.SetItem(toggle.Name, value),
    };

    public bool Equals(DecompilerOptions? other) =>
        other is not null && LanguageVersion == other.LanguageVersion && Toggles.Count == other.Toggles.Count &&
        Toggles.All(t => other.Toggles.TryGetValue(t.Key, out var value) && value == t.Value);

    public override int GetHashCode() => HashCode.Combine(LanguageVersion, Toggles.Count);

    internal void Apply(DecompilerSettings settings)
    {
        foreach (var (name, value) in Toggles)
        {
            if (DecompilerToggle.Find(name) is { } toggle && Allows(toggle))
                toggle.Apply(settings, value);
        }
    }
}
