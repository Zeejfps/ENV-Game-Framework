namespace Module.GridStorage
{
    public readonly record struct Slot<TItem>
    {
        public required Point Origin { get; init; }
        public required Size Size { get; init; }
        public required TItem Item { get; init; }
    }
}