namespace Module.GridStorage
{
    public readonly record struct Point
    {
        public required uint X { get; init; }
        public required uint Y { get; init; }

        public static Point Of(uint x, uint y)
        {
            return new Point { X = x, Y = y };
        }
    }
}