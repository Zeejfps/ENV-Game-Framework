using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Components;

public struct NeedTargetComponent
{
    public IEntity<TargetComponent> Target;
    public int TeamIndex;
}