using System.Numerics;
using System.Runtime.CompilerServices;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using EasyGameFramework.Core;

namespace EasyGameFramework.Experimental;

public sealed class PixelCanvas : IPixelCanvas
{
    private IWindow Window { get; }
    private ILogger Logger { get; }
    private IGpu Gpu { get; }
    private CpuTexture Texture { get; }
    private IGpuRenderbufferHandle Framebuffer { get; }
    private IHandle<IGpuMesh> QuadMesh { get; }
    private IHandle<IGpuShader> FullScreenQuadShader { get; }

    public PixelCanvas(ILogger logger, IWindow window, int resolutionX, int resolutionY)
    {
        ResolutionX = resolutionX;
        ResolutionY = resolutionY;
        Logger = logger;
        Window = window;
        Gpu = window.Gpu;
        Texture = new CpuTexture
        {
            Width = resolutionX,
            Height = resolutionY,
            Pixels = new byte[resolutionX * resolutionY * 4]
        };
        Framebuffer = Gpu.CreateRenderbuffer(1, false, resolutionX, resolutionY);

        var quadMesh = CpuMesh.CreateQuad();
        QuadMesh = Gpu.MeshController.CreateAndBind(quadMesh);
        FullScreenQuadShader = Gpu.ShaderController.Load("Assets/fullScreenQuad");
    }

    public void Clear()
    {
        Array.Fill(Texture.Pixels, (byte)0);
    }

    public void DrawLine(int x0, int y0, int x1, int y1)
    {
        if (x0 < 0)
            x0 = 0;

        if (y0 < 0)
            y0 = 0;

        if (x1 < 0)
            x1 = 0;
        else if (x1 >= Texture.Width)
            x1 = Texture.Width - 1;

        if (y1 < 0)
            y1 = 0;
        else if (y1 >= Texture.Height)
            y1 = Texture.Height - 1;

        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = (x0 < x1) ? 1 : -1;
        var sy = (y0 < y1) ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            DrawPixel(x0, y0, 0xFF00FF);

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    public void DrawRect(int x, int y, int width, int height)
    {
        uint color = 0xff00ffff;
        
        // Draw top and bottom edges
        for (int col = x; col < x + width; col++)
        {
            DrawPixel(col, y, color);
            DrawPixel(col, y + height - 1, color);
        }

        // Draw left and right edges
        for (int row = y + 1; row < y + height - 1; row++)
        {
            DrawPixel(x, row, color);
            DrawPixel( x + width - 1, row, color);
        }
    }

    public void Render()
    {
        var gpu = Gpu;
        var meshController = gpu.MeshController;
        var textureController = gpu.TextureController;
        var shaderController = gpu.ShaderController;
        
        gpu.EnableBlending = true;
        
        textureController.SaveState();

        textureController.Bind(Framebuffer.ColorBuffers[0]);
        textureController.Upload(Texture.Pixels);
        
        shaderController.Bind(FullScreenQuadShader);
        
        //meshController.Bind(QuadMesh);
        meshController.Render();
        
        textureController.RestoreState();
    }

    public Vector2 ScreenToCanvasPoint(Vector2 screenPoint)
    {
        var screenWidth = Window.ScreenWidth;
        var screenHeight = Window.ScreenHeight;
        
        var x = (screenPoint.X / screenWidth) * Texture.Width;
        var y = Texture.Height - (screenPoint.Y / screenHeight) * Texture.Height;
        
        return new Vector2(x, y);
    }

    public int ResolutionX { get; }
    public int ResolutionY { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawPixel(int x, int y, uint rgba)
    {
        var pixelIndex = x * 4 + y * Texture.Width * 4;
        Texture.Pixels[pixelIndex + 0] = (byte)((rgba >> 24) & 0xFF);
        Texture.Pixels[pixelIndex + 1] = (byte)((rgba >> 16) & 0xFF);
        Texture.Pixels[pixelIndex + 2] = (byte)((rgba >>  8) & 0xFF);
        Texture.Pixels[pixelIndex + 3] = (byte)((rgba >>  0) & 0xFF);
    }
}