﻿namespace EasyGameFramework.Api;

public interface ILogger
{
    void Trace(string message);
    void Trace(object obj);
    
    void Warn(string message);
}