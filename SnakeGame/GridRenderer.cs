using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using SampleGames;

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
        GridShader = Gpu.ShaderController.Load("Assets/grid");
        QuadMesh = Gpu.MeshController.Load("Assets/quad");
    }

    public void Render()
    {
        var gpu = Gpu;
        
        var activeFramebuffer = Gpu.FramebufferController;
        var cellWidth = activeFramebuffer.Width / GridSize.Width;
        var cellHeight = activeFramebuffer.Height / GridSize.Height;

        gpu.ShaderController.Bind(GridShader);
        gpu.ShaderController.SetVector2("u_Pitch", new Vector2(cellWidth, cellHeight));
        gpu.MeshController.Bind(QuadMesh);
        gpu.MeshController.Render();
    }
}