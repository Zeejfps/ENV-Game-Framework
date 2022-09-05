using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class GenericGamepad_SDL : IGenericGamepad
{
    public InputButton NorthButton { get; } = new();
    public InputButton EastButton { get; } = new();
    public InputButton WestButton { get; } = new();
    public InputButton DPadUpButton { get; } = new();
    public InputButton DPadRightButton { get; } = new();
    public InputButton DPadDownButton { get; } = new();
    public InputButton DPadLeftButton { get; } = new();
    public InputButton SouthButton { get; } = new();
    
    private string Name { get; }
    private string Guid { get; }

    public GenericGamepad_SDL(string guid, string name)
    {
        Guid = guid;
        Name = name;
    }

    public override string ToString()
    {
        return Name;
    }
}