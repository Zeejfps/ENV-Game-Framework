namespace SlangIntegrationTest;

[Flags]
public enum SlangResourceShape : uint
{
    None = 0x00,

    Texture1D = 0x01,
    Texture2D = 0x02,
    Texture3D = 0x03,
    TextureCube = 0x04,
    TextureBuffer = 0x05,
    StructuredBuffer = 0x06,
    ByteAddressBuffer = 0x07,
    ResourceUnknown = 0x08,
    AccelerationStructure = 0x09,
    TextureSubpass = 0x0A,

    BaseShapeMask = 0x0F,
    ExtShapeMask = 0x1F0,

    FeedbackFlag = 0x10,
    ShadowFlag = 0x20,
    ArrayFlag = 0x40,
    MultisampleFlag = 0x80,
    CombinedFlag = 0x100,

    Texture1DArray = Texture1D | ArrayFlag,
    Texture2DArray = Texture2D | ArrayFlag,
    TextureCubeArray = TextureCube | ArrayFlag,
    Texture2DMultisample = Texture2D | MultisampleFlag,
    Texture2DMultisampleArray = Texture2D | MultisampleFlag | ArrayFlag,
    TextureSubpassMultisample = TextureSubpass | MultisampleFlag,
}

public static class SlangResourceShapeExtensions
{
    public static SlangResourceShape BaseShape(this SlangResourceShape shape) =>
        shape & SlangResourceShape.BaseShapeMask;

    public static bool IsArray(this SlangResourceShape shape) =>
        (shape & SlangResourceShape.ArrayFlag) != 0;

    public static bool IsMultisample(this SlangResourceShape shape) =>
        (shape & SlangResourceShape.MultisampleFlag) != 0;

    public static bool IsCombined(this SlangResourceShape shape) =>
        (shape & SlangResourceShape.CombinedFlag) != 0;

    public static bool IsFeedback(this SlangResourceShape shape) =>
        (shape & SlangResourceShape.FeedbackFlag) != 0;

    public static bool IsShadow(this SlangResourceShape shape) =>
        (shape & SlangResourceShape.ShadowFlag) != 0;
}
