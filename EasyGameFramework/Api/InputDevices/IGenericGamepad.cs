namespace EasyGameFramework.Api.InputDevices;

public interface IGenericGamepad
{
    InputButton NorthButton { get; }
    InputButton EastButton { get; }
    InputButton SouthButton { get; }
    InputButton WestButton { get; }

    InputButton DPadUpButton { get; }
    InputButton DPadRightButton { get; }
    InputButton DPadDownButton { get; }
    InputButton DPadLeftButton { get; }
}