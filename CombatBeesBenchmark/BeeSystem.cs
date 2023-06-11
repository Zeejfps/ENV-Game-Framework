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
    public float Size;
    public int TeamIndex;
    public bool IsDead;
    public EnemyBee Enemy;
}

public struct EnemyBee
{
    public int Index;
    public int TeamIndex;

    public static EnemyBee Null = new()
    {
        Index = -1,
        TeamIndex = -1
    };
}

public readonly struct BeeSystemConfig
{
    public float MinBeeSize { get; init; }
    public float MaxBeeSize { get; init; }
    public int MaxBeeCount { get; init; }
    public float FlightJitter { get; init; }
    public float Damping { get; init; }
    public float TeamAttraction { get; init; }
    public float TeamRepulsion { get; init; }
    public float AttackDistance { get; init; }
    public float ChaseForce { get; init; }
    public float AttackForce { get; init; }
    public float HitDistance { get; init; }
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
        
        //Context.Logger.Trace($"Spawning Bees: {numberOfBeesToSpawn} for team: {teamIndex}");
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
        UpdateBees(dt, 0);
        UpdateBees(dt, 1);
    }

    private void UpdateBees(float dt, int teamIndex)
    {
        var flightJitter = Config.FlightJitter * dt;
        var damping = 1f - Config.Damping * dt;
        var teamAttraction = Config.TeamAttraction * dt;
        var teamRepulsion = Config.TeamRepulsion * dt;
        var chaseForce = Config.ChaseForce * dt;
        var attackForce = Config.AttackForce * dt;
        var attackDistance = Config.AttackDistance;
        var hitDistance = Config.HitDistance;
        
        var beeTeam = BeeTeams[teamIndex];
        var bees = CollectionsMarshal.AsSpan(beeTeam);

        var field = Field;
        //var gravity = field.Gravity * dt;
        var fieldHalfWidth = field.Size.X * 0.5f;
        var fieldHalfDepth = field.Size.Y * 0.5f;
        var fieldHalfHeight = field.Size.Z * 0.5f;
        for (var i = 0; i < bees.Length; i++)
        {
            ref var bee = ref bees[i];
            
            bee.Velocity += RandomInsideUnitSphere() * flightJitter;
            bee.Velocity *= damping;
           
            ref var attractiveFriend = ref GetRandomBee(bees);
            Vector3 delta = attractiveFriend.Position - bee.Position;
            var dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
            if (dist > 0f)
                bee.Velocity += delta * (teamAttraction / dist);

            ref var repellentFriend = ref GetRandomBee(bees);
            delta = repellentFriend.Position - bee.Position;
            dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
            if (dist > 0f)
                bee.Velocity -= delta * (teamRepulsion / dist);

            if (bee.Enemy.Equals(EnemyBee.Null))
            {
                bee.Enemy = GetRandomEnemyBee(teamIndex);
            }
            else
            {
                var enemyTeam = BeeTeams[bee.Enemy.TeamIndex];
                ref var enemyBee = ref CollectionsMarshal.AsSpan(enemyTeam)[bee.Enemy.Index];
                delta = enemyBee.Position - bee.Position;
                var sqrDist = delta.LengthSquared();
                if (sqrDist > attackDistance * attackDistance)
                {
                    bee.Velocity += delta * (chaseForce / MathF.Sqrt(sqrDist));
                }
                else
                {
                    bee.Velocity += delta * (attackForce / MathF.Sqrt(sqrDist));
                    if (sqrDist < hitDistance * hitDistance)
                    {
                        enemyBee.IsDead = true;
                        enemyBee.Velocity *= .5f;
                        bee.Enemy = EnemyBee.Null;
                    }
                }
            }    
            
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

    private ref Bee GetRandomBee(Span<Bee> bees)
    {
        var random = Random;
        return ref bees[random.Next(0, bees.Length)];
    }

    private EnemyBee GetRandomEnemyBee(int myTeamIndex)
    {
        var numberOfTeams = NumberOfBeeTeams;
        if (numberOfTeams == 1)
            return EnemyBee.Null;

        var random = Random;
        var randomTeamIndex = random.Next(0, numberOfTeams);
        while (randomTeamIndex == myTeamIndex)
            randomTeamIndex = random.Next(0, numberOfTeams);

        var bees = BeeTeams[randomTeamIndex];
        if (bees.Count == 0)
            return EnemyBee.Null;
        
        var index = random.Next(0, bees.Count);
        return new EnemyBee
        {
            Index = index,
            TeamIndex = randomTeamIndex
        };
    }
  
    private readonly List<Batch> m_Batches = new();

    private int DrawBees(int teamIndex, Vector3 beeColor, int startBatchIndex)
    {
        var bees = CollectionsMarshal.AsSpan(BeeTeams[teamIndex]);
        var beeCount = bees.Length;
        if (beeCount == 0)
            return 0;
        
        var maxBatchSize = Batch.MAX_BATCH_SIZE;
        var requiredBatchCount = (int)MathF.Ceiling(beeCount / (float)maxBatchSize);
        //Context.Logger.Trace($"Required Batches: {requiredBatchCount} for BeeCount: {beeCount} for Team: {teamIndex}");
        var endBatchIndex = startBatchIndex + requiredBatchCount;

        //Context.Logger.Trace($"StartBatchIndex: {startBatchIndex} EndBatchIndex: {endBatchIndex}");

        var batchIndex = startBatchIndex;
        var beeIndex = 0;
        for (; batchIndex < endBatchIndex; batchIndex++)
        {
            //Context.Logger.Trace($"BatchIndex: {batchIndex}, BatchCount: {m_Batches.Count}, EndBatchIndex: {endBatchIndex}");
            var batch = m_Batches[batchIndex];
            for (; beeIndex < beeCount && batch.Size < maxBatchSize; beeIndex++)
            {
                ref var bee = ref bees[beeIndex];
                batch.Add(bee.Position, bee.Direction, bee.Size, beeColor);
            }
        }

        return batchIndex - 1;
    } 
    
    public void Render(ICamera camera)
    {
        foreach (var batch in m_Batches)
            batch.Clear();
        
        var batchCount = m_Batches.Count;
        var totalBeeCount = BeeTeams.Sum(beeTeam => beeTeam.Count);

        var requiredBatchCount = (int)MathF.Ceiling(totalBeeCount / (float)Batch.MAX_BATCH_SIZE);
        //Context.Logger.Trace($"Required Batch Count: {requiredBatchCount} for TotalBeeCount: {totalBeeCount}");
        if (batchCount < requiredBatchCount)
        {
            var numberOfNeededBatches = requiredBatchCount - batchCount;
            for (var i = 0; i < numberOfNeededBatches; i++)
                m_Batches.Add(new Batch());
        }
        
        var beeColor = new Vector3(0.31f, 0.43f, 1f);
        var lastUsedBatchIndex = DrawBees(0, beeColor, 0);
        
        beeColor = new Vector3(0.95f, 0.95f, 0.59f);
        DrawBees(1, beeColor, lastUsedBatchIndex);
        
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
            //Context.Logger.Trace($"BatchSize: {batch.Size}");
        }

        gpu.RestoreState();
    }

    private void SpawnBeen(int teamIndex)
    {
        var spawnPosition = Vector3.UnitX * (-Field.Size.X * .4f + Field.Size.X * .8f * teamIndex);
        var bee = new Bee
        {
            Position = spawnPosition,
            Size = RandomFloatInRange(Config.MinBeeSize, Config.MaxBeeSize),
            TeamIndex = teamIndex,
            Enemy =
            {
                Index = -1,
                TeamIndex = -1
            }
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

    public void Add(Vector3 position, Vector3 direction, float size, Vector3 color)
    {
        var modelMatrix = Matrix4x4.CreateScale(size, size, size)
                        * Matrix4x4.CreateLookAt(Vector3.Zero, direction, Vector3.UnitY)
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