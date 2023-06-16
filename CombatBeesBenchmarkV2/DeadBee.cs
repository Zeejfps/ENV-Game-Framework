using System.Numerics;

namespace CombatBeesBenchmark;

public sealed class DeadBee : IDeadBee
{
    public int TeamIndex { get; }
    public Vector3 Position { get; set; }
    public Vector3 LookDirection { get; set; }
    
    public DeadBee(int teamIndex)
    {
        TeamIndex = teamIndex;
    }

    public DeadBeeState Save()
    {
        return new DeadBeeState
        {
            
        };
    }

    public void Load(DeadBeeState state)
    {
        
    }
    
    public BeeRenderState SaveRenderState()
    {
        return new BeeRenderState
        {
            Color = new Vector3(1f, 0f, 1f),
            ModelMatrix = Matrix4x4.CreateScale(1f, 1f, 1f)
                          * Matrix4x4.CreateLookAt(Vector3.Zero, LookDirection, Vector3.UnitY)
                          * Matrix4x4.CreateTranslation(Position),
        };
    }
}