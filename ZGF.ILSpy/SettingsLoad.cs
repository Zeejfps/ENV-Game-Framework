namespace ZGF.ILSpy;

/// <summary>What reading the settings file gave: the settings, with defaults for what the file could not
/// give, and what was wrong with it.</summary>
public sealed record SettingsLoad(AppSettings Settings, SettingsProblem Problem);

/// <summary>What was wrong with the settings file, for the screen to tell in the chosen language. A
/// <c>Detail</c> is as the system or the JSON reader said it.</summary>
public abstract record SettingsProblem
{
    private SettingsProblem()
    {
    }

    public sealed record None : SettingsProblem;

    /// <summary>The file could not be read, so the settings are the defaults.</summary>
    public sealed record NotRead(string Detail, SettingsKept Kept) : SettingsProblem;

    /// <summary>The file is not JSON of the settings, so the settings are the defaults.</summary>
    public sealed record NotJson(string Detail, SettingsKept Kept) : SettingsProblem;

    /// <summary>The file holds <c>null</c>, so the settings are the defaults.</summary>
    public sealed record Empty : SettingsProblem;

    /// <summary>The saved layout lacks a node, a window or a list of panels, so the panels are where
    /// they start out.</summary>
    public sealed record LayoutPartsMissing : SettingsProblem;

    /// <summary>Two parts of the saved layout have one id, so the panels are where they start out.</summary>
    public sealed record LayoutPartsRepeated : SettingsProblem;
}

/// <summary>How a settings file that could not be read is kept from being saved over.</summary>
public abstract record SettingsKept
{
    private SettingsKept()
    {
    }

    /// <summary>The file was copied to <c>Path</c>, and the settings are saved in its place.</summary>
    public sealed record CopiedTo(string Path) : SettingsKept;

    /// <summary>The file could not be copied either, so the settings are not saved this time.</summary>
    public sealed record NotSaved : SettingsKept;
}
