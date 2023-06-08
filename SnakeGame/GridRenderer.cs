using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using SampleGames;

namespace SnakeGame;

public sealed class GridRenderer
{
    public GridRenderer(IGpu gpu, GridSize gridSize)
    {
        Gpu = gpu;
        GridSize = gridSize;
    }

    private IGpu Gpu { get; }
    private GridSize GridSize { get; }
    private IHandle<IGpuShader> GridShader { get; set; }
    private IHandle<IGpuMesh> QuadMesh { get; set; }

    public void LoadResources()
    {
        GridShader = Gpu.Shader.Load("Assets/grid");
        QuadMesh = Gpu.Mesh.Load("Assets/quad");
    }

    public void Render()
    {
        var gpu = Gpu;
        
        var renderbuffer = Gpu.Renderbuffer;
        var cellWidth = renderbuffer.Width / GridSize.Width;
        var cellHeight = renderbuffer.Height / GridSize.Height;

        gpu.Shader.Bind(GridShader);
        gpu.Shader.SetVector2("u_Pitch", new Vector2(cellWidth, cellHeight));
        gpu.Mesh.Bind(QuadMesh);
        gpu.Mesh.Render();
    }
}