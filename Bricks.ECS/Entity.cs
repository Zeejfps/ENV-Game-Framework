namespace Bricks.ECS;

public readonly record struct Entity(uint Index, uint Generation)
{
    public ulong Id => ((ulong)Generation << 32) | Index;
}