namespace ZGF.ILSpy;

/// <summary>Which types and members the browse tree shows.</summary>
public enum ApiVisibility
{
    /// <summary>Only what other assemblies can use.</summary>
    PublicOnly,

    /// <summary>Everything the source declared, public or not.</summary>
    AllButCompilerGenerated,

    /// <summary>Also what the compiler made up, such as closure classes and backing fields.</summary>
    All,
}
