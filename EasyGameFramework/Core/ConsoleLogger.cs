﻿using EasyGameFramework.Api;

namespace EasyGameFramework.Core;

internal sealed class ConsoleLogger : ILogger
{
    public void Trace(string message)
    {
        Console.WriteLine($@"[{DateTime.Now}] Trace: {message}");
    }

    public void Trace(object obj)
    {
        Trace(obj.ToString());
    }
}