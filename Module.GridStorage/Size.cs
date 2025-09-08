namespace Module.GridStorage
{
    public readonly record struct Size
    {
        public required uint Width { get; init; }
        public required uint Height { get; init; }

        public static Size Of(uint width, uint height)
        {
            return new Size { Width = width, Height = height };
        }
    }
}