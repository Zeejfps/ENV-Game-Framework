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
    /// <summary>How exported projects are laid out; does not change decompiled code.</summary>
    ProjectExport,
}

/// <summary>One of the decompiler's on/off choices, named as the decompiler names it.</summary>
public sealed record DecompilerToggle(string Name, DecompilerToggleGroup Group, CSharpVersion? IntroducedIn, bool DefaultValue)
{
    private static readonly Dictionary<string, PropertyInfo> Properties = typeof(DecompilerSettings).GetProperties()
        .Where(static p => p.PropertyType == typeof(bool) && p.CanWrite).ToDictionary(static p => p.Name);

    /// <summary>Every toggle that changes decompiled code, in the decompiler's order.</summary>
    public static IReadOnlyList<DecompilerToggle> All { get; } = Discover();

    private static readonly Dictionary<string, DecompilerToggle> ByName = All.ToDictionary(static t => t.Name);

    public static DecompilerToggle? Find(string name) => ByName.GetValueOrDefault(name);

    /// <summary>Whether locals take their names from the assembly's PDB; the one toggle IL reads.</summary>
    public static DecompilerToggle UseDebugSymbols => ByName[nameof(DecompilerSettings.UseDebugSymbols)];

    /// <summary>Whether resolving a reference reads its assembly from a file, rather than only finding
    /// one already listed; applies as assemblies are read.</summary>
    public static DecompilerToggle AutoLoadAssemblyReferences => ByName[nameof(DecompilerSettings.AutoLoadAssemblyReferences)];

    /// <summary>Whether Windows Runtime metadata reads as the .NET types it projects to; applies as
    /// assemblies are read.</summary>
    public static DecompilerToggle ApplyWindowsRuntimeProjections => ByName[nameof(DecompilerSettings.ApplyWindowsRuntimeProjections)];

    internal void Apply(DecompilerSettings settings, bool value) => Properties[Name].SetValue(settings, value);

    private static List<DecompilerToggle> Discover()
    {
        var defaults = new DecompilerSettings();
        var toggles = new List<DecompilerToggle>();
        foreach (var property in typeof(DecompilerSettings).GetProperties())
        {
            if (property.PropertyType != typeof(bool) || !property.CanWrite) continue;
            if (property.GetCustomAttribute<BrowsableAttribute>() is { Browsable: false }) continue;
            var category = property.GetCustomAttribute<CategoryAttribute>()?.Category;
            if (category == null) continue;
            var value = (bool)property.GetValue(defaults)!;
            var version = Introduced(category);
            if (version != null)
                toggles.Add(new DecompilerToggle(property.Name, DecompilerToggleGroup.Language, version, value));
            else if (category == "DecompilerSettings.Other")
                toggles.Add(new DecompilerToggle(property.Name, DecompilerToggleGroup.Other, null, value));
            else if (category == "DecompilerSettings.FSpecificOptions")
                toggles.Add(new DecompilerToggle(property.Name, DecompilerToggleGroup.FSharp, null, value));
            else if (category == "DecompilerSettings.ProjectExport")
                toggles.Add(new DecompilerToggle(property.Name, DecompilerToggleGroup.ProjectExport, null, value));
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

    private readonly CSharpVersion _languageVersion = CSharpVersion.CSharp14;
    private readonly ImmutableSortedDictionary<string, bool> _toggles = ImmutableSortedDictionary<string, bool>.Empty;

    /// <summary>The C# version written; a value that names none, as an older or damaged settings
    /// file can hold, means the newest.</summary>
    public CSharpVersion LanguageVersion
    {
        get => _languageVersion;
        init => _languageVersion = Enum.IsDefined(value) ? value : CSharpVersion.CSharp14;
    }

    /// <summary>The toggles set away from their defaults; null, as an older settings file reads,
    /// means none.</summary>
    public ImmutableSortedDictionary<string, bool> Toggles
    {
        get => _toggles;
        init => _toggles = value ?? ImmutableSortedDictionary<string, bool>.Empty;
    }

    /// <summary>Whether <paramref name="toggle"/> is on as chosen, whether or not the version allows it.</summary>
    public bool IsOn(DecompilerToggle toggle) => Toggles.TryGetValue(toggle.Name, out var value) ? value : toggle.DefaultValue;

    /// <summary>Whether <see cref="LanguageVersion"/> is new enough for <paramref name="toggle"/>.</summary>
    public bool Allows(DecompilerToggle toggle) => toggle.IntroducedIn == null || toggle.IntroducedIn <= LanguageVersion;

    /// <summary>Whether code in <paramref name="language"/> comes out the same with these options as
    /// with <paramref name="other"/>.</summary>
    public bool ProducesSameCode(DecompilerOptions other, DecompilationLanguage language)
    {
        if (DecompilationLanguages.ShowsCSharp(language))
            return WithoutProjectExport().Equals(other.WithoutProjectExport());
        var symbols = DecompilerToggle.UseDebugSymbols;
        return IsOn(symbols) == other.IsOn(symbols);
    }

    /// <summary>These options with every toggle that shapes code back at its default; the project
    /// export toggles, which the export dialog sets, stay as they are.</summary>
    public DecompilerOptions WithCodeTogglesReset()
    {
        var toggles = Toggles.ToBuilder();
        foreach (var toggle in DecompilerToggle.All)
        {
            if (toggle.Group != DecompilerToggleGroup.ProjectExport)
                toggles.Remove(toggle.Name);
        }
        return this with { Toggles = toggles.ToImmutable() };
    }

    /// <summary>These options with the project export toggles at their defaults, as they only shape
    /// exported projects, not code.</summary>
    private DecompilerOptions WithoutProjectExport()
    {
        var toggles = Toggles.ToBuilder();
        foreach (var toggle in DecompilerToggle.All)
        {
            if (toggle.Group == DecompilerToggleGroup.ProjectExport)
                toggles.Remove(toggle.Name);
        }
        return this with { Toggles = toggles.ToImmutable() };
    }

    public DecompilerOptions With(DecompilerToggle toggle, bool value) => With([toggle], value);

    public DecompilerOptions With(IEnumerable<DecompilerToggle> toggles, bool value)
    {
        var changed = Toggles.ToBuilder();
        foreach (var toggle in toggles)
        {
            if (value == toggle.DefaultValue)
                changed.Remove(toggle.Name);
            else
                changed[toggle.Name] = value;
        }
        return this with { Toggles = changed.ToImmutable() };
    }

    /// <summary>Whether <paramref name="other"/> makes the same choices; the same options are equal at once,
    /// without walking the toggles.</summary>
    public bool Equals(DecompilerOptions? other)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (other is null)
            return false;
        if (LanguageVersion != other.LanguageVersion)
            return false;
        if (Toggles.Count != other.Toggles.Count)
            return false;
        return HasSameToggles(other);
    }

    private bool HasSameToggles(DecompilerOptions other) =>
        Toggles.All(t => other.Toggles.TryGetValue(t.Key, out var value) && value == t.Value);

    public override int GetHashCode() => HashCode.Combine(LanguageVersion, Toggles.Count);

    internal void Apply(DecompilerSettings settings)
    {
        foreach (var (name, value) in Toggles)
        {
            var toggle = DecompilerToggle.Find(name);
            if (toggle != null && Allows(toggle))
                toggle.Apply(settings, value);
        }
    }
}
