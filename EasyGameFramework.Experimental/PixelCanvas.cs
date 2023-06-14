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
        var width = ResolutionX;
        var height = ResolutionY;
        var color = 0xff00ffff;
        bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
        if (steep)
        {
            (x0, y0) = (y0, x0);
            (x1, y1) = (y1, x1);
        }
        if (x0 > x1)
        {
            (x0, x1) = (x1, x0);
            (y0, y1) = (y1, y0);
        }

        int dx = x1 - x0;
        int dy = Math.Abs(y1 - y0);
        int err = dx / 2;
        int ystep = (y0 < y1) ? 1 : -1;
        int y = y0;

        for (int x = x0; x <= x1; x++)
        {
            if (steep)
            {
                if (x >= 0 && x < height && y >= 0 && y < width)
                    DrawPixel(y, x, color);
            }
            else
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                    DrawPixel(x, y, color);
            }

            err -= dy;
            if (err < 0)
            {
                y += ystep;
                err += dx;
            }
        }
    }

    public void DrawRect(int x, int y, int width, int height)
    {
        uint color = 0xff00ffff;

        var sx = x;
        if (sx >= ResolutionX)
            return;
        
        var ex = x + width;
        if (ex < 0)
            return;

        var sy = y;
        if (sy >= ResolutionY)
            return;
        
        var ey = y + height;
        if (ey < 0)
            return;

        if (sx < 0)
            sx = 0;
        
        if (ex >= ResolutionX)
            ex = ResolutionX - 1;

        if (sy < 0)
            sy = 0;

        if (ey >= ResolutionY)
            ey = ResolutionY - 1;
        
        //Logger.Trace($"X: {sx}, Y: {sy}, EX: {ex}, EY: {ey}");
        
        // Draw top and bottom edges
        for (int col = sx; col < ex; col++)
        {
            DrawPixel(col, sy, color);
            DrawPixel(col, ey, color);
        }

        // Draw left and right edges
        for (int row = sy; row < ey; row++)
        {
            DrawPixel(sx, row, color);
            DrawPixel( ex, row, color);
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