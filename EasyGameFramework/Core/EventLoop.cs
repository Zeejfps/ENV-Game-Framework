﻿using EasyGameFramework.Api;

namespace EasyGameFramework.Core;

internal sealed class EventLoop : IEventLoop
{
    public event Action? OnEarlyUpdate;
    public event Action? OnUpdate;
    public event Action? OnLateUpdate;

    public bool IsRunning { get; private set; }

    public void Start()
    {
        if (IsRunning)
            return;

        IsRunning = true;
        while (IsRunning)
        {
            OnEarlyUpdate?.Invoke();
            OnUpdate?.Invoke();
            OnLateUpdate?.Invoke();
        }
    }

    public void Stop()
    {
        IsRunning = false;
    }
}