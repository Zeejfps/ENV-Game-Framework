using System.Numerics;
using CombatBeesBenchmarkV2.Archetype;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmark;

public sealed class Bee : IBee,
    IEntity<MovementArchetype>,
    IEntity<CollisionArchetype>,
    IEntity<BeeRenderArchetype>,
    IEntity<AliveBeeArchetype>,
    IEntity<DeadBeeArchetype>,
    IEntity<AttractRepelArchetype>,
    IEntity<SpawnableBeeArchetype>,
    IEntity<NeedTargetArchetype>,
    IEntity<KilledArchetype>,
    IEntity<BeeSpawnedEvent>
{
    private bool IsAlive { get; set; }
    public int TeamIndex { get; }

    public Vector3 Position { get; set; }
    private Vector3 Velocity { get; set; }
    private float Size { get; set; }
    private Vector3 LookDirection { get; set; }
    private float DeathTimer { get; set; }
    private Vector4 Color { get; set; }
    private World<Bee> World { get; }
    private Bee? Target { get; set; }
    private Vector3 AttractPoint { get; set; }
    private Vector3 RepelPoint { get; set; }
    private Vector3 MoveDirection { get; set; }
    
    public Bee(int teamIndex, World<Bee> world)
    {
        TeamIndex = teamIndex;
        World = world;
        Color = teamIndex == 0 ? new Vector4(1f, 0f, 0f, 1f) : new Vector4(0f, 0f, 1f, 1f);
        
        Velocity = Vector3.UnitX;
        LookDirection = Vector3.UnitX;
        
        World.Add<BeeRenderArchetype>(this);
        World.Add<CollisionArchetype>(this);
    }

    public void Spawn()
    {
        World.Add<SpawnableBeeArchetype>(this);
        World.Add<NeedTargetArchetype>(this);
    }

    private void Kill()
    {
        World.Remove<AliveBeeArchetype>(this);
        World.Remove<AttractRepelArchetype>(this);
        World.Add<KilledArchetype>(this);
    }
    
    public void Into(ref CollisionArchetype archetype)
    {
        archetype.MovementState.Position = Position;
        archetype.MovementState.Velocity = Velocity;
    }

    public void From(ref CollisionArchetype archetype)
    {
        Position = archetype.MovementState.Position;
        Velocity = archetype.MovementState.Velocity;
    }

    public void Into(ref BeeRenderArchetype archetype)
    {
        archetype.Color = Color;

        var size = Size;
        archetype.ModelMatrix = Matrix4x4.CreateScale(size, size, size)
                                * Matrix4x4.CreateLookAt(Vector3.Zero, LookDirection, Vector3.UnitY)
                                * Matrix4x4.CreateTranslation(Position);

        // Console.WriteLine($"Size: {size}");
        // Console.WriteLine($"Position: {Position}");
        // Console.WriteLine($"LookDir: {LookDirection}");
        // Console.WriteLine(archetype.ModelMatrix);
    }

    public void From(ref BeeRenderArchetype archetype)
    {
        
    }

    public void Into(ref AliveBeeArchetype archetype)
    {
        if (Target == null || !Target.IsAlive)
            World.Add<NeedTargetArchetype>(this);
        
        Into(ref archetype.Movement);
        archetype.TargetPosition = Target?.Position ?? Vector3.Zero;
        archetype.LookDirection = LookDirection;
        archetype.MoveDirection = MoveDirection;
        archetype.AttractionPoint = AttractPoint;
        archetype.RepellentPoint = RepelPoint;
        archetype.IsTargetKilled = false;
    }

    public void From(ref AliveBeeArchetype archetype)
    {
        var target = Target;
        From(ref archetype.Movement);
        LookDirection = archetype.LookDirection;
        if (archetype.IsTargetKilled && target != null && target.IsAlive)
        {
            target.Kill();
            World.Add<NeedTargetArchetype>(this);
        }
    }

    public void Into(ref DeadBeeArchetype archetype)
    {
        Into(ref archetype.Movement);
        archetype.DeathTimer = DeathTimer;
    }

    public void From(ref DeadBeeArchetype archetype)
    {
        From(ref archetype.Movement);
        DeathTimer = archetype.DeathTimer;
        if (DeathTimer <= 0f)
            Spawn();
    }

    public void Into(ref MovementArchetype archetype)
    {
        archetype.Position = Position;
        archetype.Velocity = Velocity;
    }

    public void From(ref MovementArchetype archetype)
    {
        Position = archetype.Position;
        Velocity = archetype.Velocity;
    }

    public void Into(ref AttractRepelArchetype archetype)
    {
        archetype.AttractionPoint = AttractPoint;
        archetype.RepellentPoint = RepelPoint;
        archetype.TeamIndex = TeamIndex;
        archetype.MoveDirection = MoveDirection;
    }

    public void From(ref AttractRepelArchetype archetype)
    {
        AttractPoint = archetype.AttractionPoint;
        RepelPoint = archetype.RepellentPoint;
        MoveDirection = archetype.MoveDirection;
    }

    public void Into(ref SpawnableBeeArchetype component)
    {
        component.In.TeamIndex = TeamIndex;
        component.In.Bee = this;
    }

    public void From(ref SpawnableBeeArchetype component)
    {
        Position = component.Out.SpawnPosition;
        Size = component.Out.Size;
        
        IsAlive = true;
        
        World.Remove<SpawnableBeeArchetype>(this);
        World.Remove<DeadBeeArchetype>(this);
        World.Add<AliveBeeArchetype>(this);
        World.Add<AttractRepelArchetype>(this);
        World.Add<BeeSpawnedEvent>(this);
    }

    public void Into(ref NeedTargetArchetype component)
    {
        component.In.TeamIndex = TeamIndex;
    }

    public void From(ref NeedTargetArchetype component)
    {
        Target = component.Out.Target;
        if (Target != null && Target.IsAlive)
            World.Remove<NeedTargetArchetype>(this);
    }

    public void Into(ref KilledArchetype component)
    {
        component.In.Bee = this;
    }

    public void From(ref KilledArchetype component)
    {
        Velocity *= 0.5f;
        DeathTimer = 10f;
        IsAlive = false;
        
        World.Remove<KilledArchetype>(this);
        World.Add<DeadBeeArchetype>(this);
    }

    public void Into(ref BeeSpawnedEvent component)
    {
        component.Bee = this;
    }

    public void From(ref BeeSpawnedEvent component)
    {
    }
}