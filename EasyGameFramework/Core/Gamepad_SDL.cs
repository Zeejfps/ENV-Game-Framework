using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class Gamepad_SDL : IGamepad
{
    private string Name { get; }
    private string Guid { get; }

    public Gamepad_SDL(string guid, string name)
    {
        Guid = guid;
        Name = name;
    }

    public override string ToString()
    {
        return Name;
    }
}