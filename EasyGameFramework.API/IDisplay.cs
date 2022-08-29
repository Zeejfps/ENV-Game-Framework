namespace EasyGameFramework.API;

public interface IDisplay
{
    int ResolutionX { get; }
    int ResolutionY { get; }
    int RefreshRate { get; }
}