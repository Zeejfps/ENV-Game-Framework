﻿using static GL46;
using GLFW;
using ZGF.Core;
using ZGF.Geometry;
using ZnvQuadTree;

namespace SoftwareRendererOpenGlBackend;

public sealed class QuadTreeRendererApp : OpenGlApp
{
    private readonly QuadTreeRenderer _renderer;
    private readonly QuadTreePointF<Item> _quadTree;
    
    private readonly SizeCallback _framebufferSizeCallback;
    private readonly MouseButtonCallback _mouseButtonCallback;
    private readonly MouseCallback _cursorPositonCallback;
    private readonly KeyCallback _keyCallback;
    
    public QuadTreeRendererApp(StartupConfig startupConfig) : base(startupConfig)
    {
        var framebufferWidth = startupConfig.WindowWidth / 2;
        var framebufferHeight = startupConfig.WindowHeight / 2;
        _quadTree = new QuadTreePointF<Item>(new RectF
        {
            Bottom = 0,
            Left = 0,
            Width = framebufferWidth,
            Height = framebufferHeight
        }, 20, maxDepth: 6);
        
        _renderer = new QuadTreeRenderer(
            framebufferWidth,  
            framebufferHeight,
            _quadTree
        );

        _framebufferSizeCallback = HandleFrameBufferSizeEvent;
        _mouseButtonCallback = HandleMouseButtonEvent;
        _cursorPositonCallback = HandleMouseMoveEvent;
        _keyCallback = HandleKeyEvent;

        Glfw.SetFramebufferSizeCallback(WindowHandle, _framebufferSizeCallback);
        Glfw.SetMouseButtonCallback(WindowHandle, _mouseButtonCallback);
        Glfw.SetCursorPositionCallback(WindowHandle, _cursorPositonCallback);
        Glfw.SetKeyCallback(WindowHandle, _keyCallback);
        
        glClearColor(0.2f, 0.3f, 0.3f, 1.0f);
    }

    private void HandleKeyEvent(Window window, Keys key, int scanCode, InputState state, ModifierKeys mods)
    {
        if (key != Keys.Space)
            return;
            
        if (state != InputState.Press)
            return;

        for (var i = 0; i < 100000; i++)
        {
            var x = Random.Shared.Next(0, _renderer.FramebufferWidth);
            var y = Random.Shared.Next(0, _renderer.FramebufferHeight);
            AddItemAt(x, y);
        }
    }
    
    private void AddItemAt(float x, float y)
    {
        var item = new Item
        {
            Position = new PointF
            {
                X = x,
                Y = y
            }
        };
        _quadTree.Insert(item, item.Position);
    }
    
    private void WindowToWorldPoint(Window window, double windowX, double windowY, out int worldX, out int worldY)
    {
        var renderer = _renderer;
        Glfw.GetWindowSize(window, out var windowWidth, out var windowHeight);
        var wFactor = (float)renderer.FramebufferWidth / windowWidth;
        var hFactor = (float)renderer.FramebufferHeight / windowHeight;
        worldX = (int)(windowX * wFactor);
        worldY = (int)((windowHeight - windowY) * hFactor);
    }

    private void HandleFrameBufferSizeEvent(Window window, int width, int height)
    {
        glViewport(0, 0, width, height);
        Render();
        Glfw.SwapBuffers(window);
    }

    private void HandleMouseButtonEvent(Window window, MouseButton button, InputState state, ModifierKeys modifiers)
    {
        if (button != MouseButton.Left)
            return;

        if (state != InputState.Press)
            return;

        Glfw.GetCursorPosition(window, out var windowX, out var windowY);
        WindowToWorldPoint(window, windowX, windowY, out var worldX, out var worldY);
        AddItemAt(worldX, worldY);
    }

    private void HandleMouseMoveEvent(Window window, double windowX, double windowY)
    {
        WindowToWorldPoint(window, windowX, windowY, out var worldX, out var worldY);
        _renderer.SetMousePosition(worldX, worldY);
    }

    private void Render()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        _renderer.Render();
    }
    
    protected override void OnUpdate()
    {
        Render();
    }

    protected override void DisposeManagedResources()
    {
        _renderer.Dispose();
    }

    protected override void DisposeUnmanagedResources()
    {
        
    }
}