﻿using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace SampleGames;

public class GamepadButtonBinding : IButtonBinding
{
    public event Action? Pressed;

    public event Action? Released;

    private readonly int m_Slot;
    private readonly GamepadButton m_Button;

    public GamepadButtonBinding(int slot, GamepadButton button)
    {
        m_Slot = slot;
        m_Button = button;
    }

    public void Bind(IInput input)
    {
        input.GamepadConnected += OnGamepadConnected;
        if (input.TryGetGamepadInSlot(m_Slot, out var gamepad))
            gamepad!.ButtonPressed += OnButtonPressed;
    }

    private void OnGamepadConnected(GamepadConnectedEvent evt)
    {
        if (evt.Slot != m_Slot)
            return;

        var gamepad = evt.Gamepad;
        gamepad.ButtonPressed += OnButtonPressed;
    }

    private void OnButtonPressed(GamepadButtonStateChangedEvent evt)
    {
        if (evt.Button != m_Button)
            return;
        
        Pressed?.Invoke();
    }

    public void Unbind(IInput input)
    {
        input.GamepadConnected -= OnGamepadConnected;
    }
}