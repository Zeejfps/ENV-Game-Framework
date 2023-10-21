using CombatBeesBenchmarkV3;
using CombatBeesBenchmarkV3.Archetypes;
using CombatBeesBenchmarkV3.EcsPrototype;
using CombatBeesBenchmarkV3.Systems;

Console.WriteLine("Hello, World!");

var random = new Random();
var world = new World<Entity>();

world.AddSystem(new BeeSpawningSystem(world, 100, random));

for (var i = 0; i < 50; i++)
{
    var entity = new Entity
    {
        TeamIndex = 0
    };
    world.AddEntity<SpawnableBee>(entity);
}

for (var i = 0; i < 50; i++)
{
    var entity = new Entity
    {
        TeamIndex = 1
    };
    world.AddEntity<SpawnableBee>(entity);
}