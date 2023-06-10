using System.Numerics;
using System.Runtime.InteropServices;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;

namespace CombatBeesBenchmark;

public struct Bee
{
    public Vector3 Position;
    public Vector3 Velocity;
    public Vector3 Direction;
    public float DeathTimer;
    public float Size;
}

public struct BeeSystemConfig
{
    public float MinBeeSize { get; init; }
    public float MaxBeeSize { get; init; }
    public int MaxBeeCount { get; init; }
    public float FlightJitter { get; set; }
    public float Damping { get; set; }
}

public sealed class BeeSystem
{
    public int NumberOfBeeTeams { get; }
    
    private IContext Context { get; }
    private Field Field { get; }
    private List<Bee>[] BeeTeams { get; }
    private Random Random { get; }
    private BeeSystemConfig Config { get; }

    public BeeSystem(IContext context, Field field, BeeSystemConfig config)
    {
        Context = context;
        Config = config;
        Field = field;
        Random = new Random();
        
        var numberOfBeeTeams = 2;
        NumberOfBeeTeams = numberOfBeeTeams;
        BeeTeams = new List<Bee>[numberOfBeeTeams];
        var maxBeeCountPerTeam = config.MaxBeeCount / BeeTeams.Length;
        for (var i = 0; i < numberOfBeeTeams; i++)
            BeeTeams[i] = new List<Bee>(maxBeeCountPerTeam);
    }

    public int GetBeeCountForTeam(int teamIndex)
    {
        return BeeTeams[teamIndex].Count;
    }

    public void SpawnBees(int teamIndex, int numberOfBeesToSpawn)
    {
        if (numberOfBeesToSpawn < 1)
            return;
        
        for (var i = 0; i < numberOfBeesToSpawn; i++)
            SpawnBeen(teamIndex);
    }

    private IHandle<IGpuMesh> QuadMeshHandle { get; set; }
    private IHandle<IGpuShader> BeeShaderHandle { get; set; }

    public void LoadResources()
    {
        var gpu = Context.Window.Gpu;
        QuadMeshHandle = gpu.Mesh.Load("Assets/quad");
        BeeShaderHandle = gpu.Shader.Load("Assets/bee");
    }

    public void Update(float dt)
    {
        UpdateBees(dt, BeeTeams[0]);
        UpdateBees(dt, BeeTeams[1]);
    }

    private void UpdateBees(float dt, List<Bee> beeTeam)
    {
        var flightJitter = Config.FlightJitter * dt;
        var damping = 1f - Config.Damping * dt;
        
        var field = Field;
        //var gravity = field.Gravity * dt;
        var fieldHalfWidth = field.Size.X * 0.5f;
        var fieldHalfDepth = field.Size.Y * 0.5f;
        var fieldHalfHeight = field.Size.Z * 0.5f;
        var bees = CollectionsMarshal.AsSpan(beeTeam);
        for (var i = 0; i < bees.Length; i++)
        {
            ref var bee = ref bees[i];
            
            bee.Velocity += RandomInsideUnitSphere() * flightJitter;
            bee.Velocity *= damping;
            bee.Direction = Vector3.Lerp(bee.Direction, Vector3.Normalize(bee.Velocity), dt * 4f);
            bee.Position += bee.Velocity * dt;
            
            if (MathF.Abs(bee.Position.X) > fieldHalfWidth)
            {
                bee.Position.X = fieldHalfWidth * MathF.Sign(bee.Position.X);
                bee.Velocity.X *= -.5f;
                bee.Velocity.Y *= .8f;
                bee.Velocity.Z *= .8f;
            }
            if (MathF.Abs(bee.Position.Z) > fieldHalfHeight)
            {
                bee.Position.Z = fieldHalfHeight * MathF.Sign(bee.Position.Z);
                bee.Velocity.Z *= -.5f;
                bee.Velocity.X *= .8f;
                bee.Velocity.Y *= .8f;
            }
            if (MathF.Abs(bee.Position.Y) > fieldHalfDepth)
            {
                bee.Position.Y = fieldHalfDepth * MathF.Sign(bee.Position.Y);
                bee.Velocity.Y *= -.5f;
                bee.Velocity.Z *= .8f;
                bee.Velocity.X *= .8f;
            }
        }
    }
    

    private readonly List<Batch> m_Batches = new();

    private void RenderBees(ICamera camera, List<Bee> beeTeam, Vector3 beeColor)
    {
        foreach (var batch in m_Batches)
            batch.Clear();

        var bees = CollectionsMarshal.AsSpan(beeTeam);
        
        var batchCount = m_Batches.Count;
        var beeCount = bees.Length;
        var requiredBatchCount = (int)MathF.Ceiling(beeCount / (float)Batch.MAX_BATCH_SIZE);
        if (batchCount < requiredBatchCount)
        {
            var numberOfNeededBatches = requiredBatchCount - batchCount;
            for (var i = 0; i < numberOfNeededBatches; i++)
                m_Batches.Add(new Batch());
        }

        for (var batchIndex = 0; batchIndex < requiredBatchCount; batchIndex++)
        {
            var batch = m_Batches[batchIndex];
            var startBeeIndex = batchIndex * Batch.MAX_BATCH_SIZE;
            var endBeeIndex = startBeeIndex + Batch.MAX_BATCH_SIZE;
            for (var beeIndex = startBeeIndex; beeIndex < endBeeIndex && beeIndex < beeCount; beeIndex++)
            {
                ref var bee = ref bees[beeIndex];
                batch.Add(bee.Position, bee.Size, beeColor);
            }
        }

        var gpu = Context.Window.Gpu;
        gpu.SaveState();

        var activeShader = gpu.Shader;
        var activeMesh = gpu.Mesh;
        
        activeShader.Bind(BeeShaderHandle);
        activeMesh.Bind(QuadMeshHandle);

        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        activeShader.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        activeShader.SetMatrix4x4("matrix_view", viewMatrix);
        
        foreach (var batch in m_Batches)
        {
            activeShader.SetVector3Array("colors", batch.Colors);
            activeShader.SetMatrix4x4Array("model_matrices", batch.ModelMatrices);
            activeMesh.RenderInstanced(batch.Size);
        }

        gpu.RestoreState();
    } 
    
    public void Render(ICamera camera)
    {
        var beeColor = new Vector3(0.31f, 0.43f, 1f);
        RenderBees(camera, BeeTeams[0], beeColor);

        beeColor = new Vector3(1f, 0.5f, 0.5f);
        RenderBees(camera, BeeTeams[1], beeColor);
    }

    private void SpawnBeen(int teamIndex)
    {
        var spawnPosition = Vector3.UnitX * (-Field.Size.X * .4f + Field.Size.X * .8f * teamIndex);
        var bee = new Bee
        {
            Position = spawnPosition,
            Size = RandomFloatInRange(Config.MinBeeSize, Config.MaxBeeSize)
        };
        
        var beeTeam = BeeTeams[teamIndex];
        beeTeam.Add(bee);
    }

    private float RandomFloatInRange(float min, float max)
    {
        return (float)(Random.NextDouble() * (max - min) + min);
    }

    private Vector3 RandomInsideUnitSphere()
    {
        float theta = RandomFloatInRange(0f, 2f * MathF.PI);
        float phi = RandomFloatInRange(0f, MathF.PI);

        float x = MathF.Sin(phi) * MathF.Cos(theta);
        float y = MathF.Sin(phi) * MathF.Sin(theta);
        float z = MathF.Cos(phi);

        return new Vector3(x, y, z);
    }
}

class Batch
{
    public const int MAX_BATCH_SIZE = 128;

    public ReadOnlySpan<Vector3> Colors => m_Colors;
    public ReadOnlySpan<Matrix4x4> ModelMatrices => m_ModelMatrices;
    
    private int m_Size;
    public int Size => m_Size;

    private readonly Vector3[] m_Colors = new Vector3[MAX_BATCH_SIZE];
    private readonly Matrix4x4[] m_ModelMatrices = new Matrix4x4[MAX_BATCH_SIZE];

    public void Add(Vector3 position, float size, Vector3 color)
    {
        var modelMatrix = Matrix4x4.CreateScale(size, size, size)
                          * Matrix4x4.CreateTranslation(position);
        
        m_Colors[m_Size] = color;
        m_ModelMatrices[m_Size] = modelMatrix;
        m_Size++;
    }
    
    public void Clear()
    {
        m_Size = 0;
    }
}