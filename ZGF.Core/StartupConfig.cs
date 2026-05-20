namespace ZGF.Core;

public readonly struct StartupConfig
{
    public required int WindowWidth { get; init; }
    public required int WindowHeight { get; init; }
    public required string WindowTitle { get; init; }
    public bool IsUndecorated { get; init; }
}