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
    IEntity<AttractRepelArchetype>
{
    public bool IsAlive { get; set; }
    public int TeamIndex { get; }

    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public float Size { get; set; }
    public Vector3 LookDirection { get; set; }
    public float DeathTimer { get; set; }
    public Vector4 Color { get; set; }
    private World World { get; }
    private Bee? Target { get; set; }
    private BeePool<Bee> AliveBees { get; }
    private Vector3 AttractPoint { get; set; }
    private Vector3 RepelPoint { get; set; }
    private Vector3 MoveDirection { get; set; }


    public Bee(int teamIndex, World world, BeePool<Bee> aliveBees)
    {
        TeamIndex = teamIndex;
        World = world;
        AliveBees = aliveBees;
        Color = teamIndex == 0 ? new Vector4(1f, 0f, 0f, 1f) : new Vector4(0f, 0f, 1f, 1f);

        World.Add<BeeRenderArchetype>(this);
        World.Add<CollisionArchetype>(this);
    }

    public void Spawn()
    {
        World.Spawn(this);
    }
    
    public void Into(out CollisionArchetype archetype)
    {
        archetype.MovementState.Position = Position;
        archetype.MovementState.Velocity = Velocity;
    }

    public void From(ref CollisionArchetype archetype)
    {
        Position = archetype.MovementState.Position;
        Velocity = archetype.MovementState.Velocity;
    }

    public void Into(out BeeRenderArchetype archetype)
    {
        archetype.Color = Color;

        var size = Size;
        archetype.ModelMatrix = Matrix4x4.CreateScale(size, size, size)
                                * Matrix4x4.CreateLookAt(Vector3.Zero, LookDirection, Vector3.UnitY)
                                * Matrix4x4.CreateTranslation(Position);
    }

    public void From(ref BeeRenderArchetype archetype)
    {
        
    }

    public void Into(out AliveBeeArchetype archetype)
    {
        if (Target == null || !Target.IsAlive)
        {
            Target = World.GetRandomEnemy(TeamIndex);
        }
        
        Into(out archetype.Movement);
        archetype.TargetPosition = Target.Position;
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
            World.Kill(target);
            Target = World.GetRandomEnemy(TeamIndex);
        }
    }

    public void Into(out DeadBeeArchetype archetype)
    {
        Into(out archetype.Movement);
        archetype.DeathTimer = DeathTimer;
    }

    public void From(ref DeadBeeArchetype archetype)
    {
        From(ref archetype.Movement);
        DeathTimer = archetype.DeathTimer;
        if (DeathTimer <= 0f)
            World.Spawn(this);
    }

    public void Into(out MovementArchetype archetype)
    {
        archetype.Position = Position;
        archetype.Velocity = Velocity;
    }

    public void From(ref MovementArchetype archetype)
    {
        Position = archetype.Position;
        Velocity = archetype.Velocity;
    }

    public void Into(out AttractRepelArchetype archetype)
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
}