using System.Runtime.CompilerServices;
using System.Security;
// ReSharper disable InconsistentNaming

public readonly struct GLboolean
{
	private readonly byte _value;
	public GLboolean(byte value) => _value = value;
	public static implicit operator GLboolean(bool b) => new GLboolean(b ? (byte)1 : (byte)0);
	public static implicit operator bool(GLboolean b) => b._value != 0;
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "IdentifierTypo")]
[SuppressUnmanagedCodeSecurity]
public static unsafe class GL46
{
	public delegate nint GetProcAddressDelegate(string funcName);

	public const uint GL_DEPTH_BUFFER_BIT = 0x00000100;
	public const uint GL_STENCIL_BUFFER_BIT = 0x00000400;
	public const uint GL_COLOR_BUFFER_BIT = 0x00004000;
	public const uint GL_DYNAMIC_STORAGE_BIT = 0x0100;
	public const uint GL_CLIENT_STORAGE_BIT = 0x0200;
	public const uint GL_CONTEXT_FLAG_FORWARD_COMPATIBLE_BIT = 0x00000001;
	public const uint GL_CONTEXT_FLAG_DEBUG_BIT = 0x00000002;
	public const uint GL_CONTEXT_FLAG_ROBUST_ACCESS_BIT = 0x00000004;
	public const uint GL_CONTEXT_FLAG_NO_ERROR_BIT = 0x00000008;
	public const uint GL_CONTEXT_CORE_PROFILE_BIT = 0x00000001;
	public const uint GL_CONTEXT_COMPATIBILITY_PROFILE_BIT = 0x00000002;
	public const uint GL_MAP_READ_BIT = 0x0001;
	public const uint GL_MAP_WRITE_BIT = 0x0002;
	public const uint GL_MAP_INVALIDATE_RANGE_BIT = 0x0004;
	public const uint GL_MAP_INVALIDATE_BUFFER_BIT = 0x0008;
	public const uint GL_MAP_FLUSH_EXPLICIT_BIT = 0x0010;
	public const uint GL_MAP_UNSYNCHRONIZED_BIT = 0x0020;
	public const uint GL_MAP_PERSISTENT_BIT = 0x0040;
	public const uint GL_MAP_COHERENT_BIT = 0x0080;
	public const uint GL_VERTEX_ATTRIB_ARRAY_BARRIER_BIT = 0x00000001;
	public const uint GL_ELEMENT_ARRAY_BARRIER_BIT = 0x00000002;
	public const uint GL_UNIFORM_BARRIER_BIT = 0x00000004;
	public const uint GL_TEXTURE_FETCH_BARRIER_BIT = 0x00000008;
	public const uint GL_SHADER_IMAGE_ACCESS_BARRIER_BIT = 0x00000020;
	public const uint GL_COMMAND_BARRIER_BIT = 0x00000040;
	public const uint GL_PIXEL_BUFFER_BARRIER_BIT = 0x00000080;
	public const uint GL_TEXTURE_UPDATE_BARRIER_BIT = 0x00000100;
	public const uint GL_BUFFER_UPDATE_BARRIER_BIT = 0x00000200;
	public const uint GL_FRAMEBUFFER_BARRIER_BIT = 0x00000400;
	public const uint GL_TRANSFORM_FEEDBACK_BARRIER_BIT = 0x00000800;
	public const uint GL_ATOMIC_COUNTER_BARRIER_BIT = 0x00001000;
	public const uint GL_SHADER_STORAGE_BARRIER_BIT = 0x00002000;
	public const uint GL_CLIENT_MAPPED_BUFFER_BARRIER_BIT = 0x00004000;
	public const uint GL_QUERY_BUFFER_BARRIER_BIT = 0x00008000;
	public const uint GL_ALL_BARRIER_BITS = 0xFFFFFFFF;
	public const uint GL_SYNC_FLUSH_COMMANDS_BIT = 0x00000001;
	public const uint GL_VERTEX_SHADER_BIT = 0x00000001;
	public const uint GL_FRAGMENT_SHADER_BIT = 0x00000002;
	public const uint GL_GEOMETRY_SHADER_BIT = 0x00000004;
	public const uint GL_TESS_CONTROL_SHADER_BIT = 0x00000008;
	public const uint GL_TESS_EVALUATION_SHADER_BIT = 0x00000010;
	public const uint GL_COMPUTE_SHADER_BIT = 0x00000020;
	public const uint GL_ALL_SHADER_BITS = 0xFFFFFFFF;
	public const uint GL_FALSE = 0;
	public const uint GL_NO_ERROR = 0;
	public const uint GL_ZERO = 0;
	public const uint GL_NONE = 0;
	public const uint GL_TRUE = 1;
	public const uint GL_ONE = 1;
	public const uint GL_INVALID_INDEX = 0xFFFFFFFF;
	public const ulong GL_TIMEOUT_IGNORED = 0xFFFFFFFFFFFFFFFF;
	public const uint GL_POINTS = 0x0000;
	public const uint GL_LINES = 0x0001;
	public const uint GL_LINE_LOOP = 0x0002;
	public const uint GL_LINE_STRIP = 0x0003;
	public const uint GL_TRIANGLES = 0x0004;
	public const uint GL_TRIANGLE_STRIP = 0x0005;
	public const uint GL_TRIANGLE_FAN = 0x0006;
	public const uint GL_LINES_ADJACENCY = 0x000A;
	public const uint GL_LINE_STRIP_ADJACENCY = 0x000B;
	public const uint GL_TRIANGLES_ADJACENCY = 0x000C;
	public const uint GL_TRIANGLE_STRIP_ADJACENCY = 0x000D;
	public const uint GL_PATCHES = 0x000E;
	public const uint GL_NEVER = 0x0200;
	public const uint GL_LESS = 0x0201;
	public const uint GL_EQUAL = 0x0202;
	public const uint GL_LEQUAL = 0x0203;
	public const uint GL_GREATER = 0x0204;
	public const uint GL_NOTEQUAL = 0x0205;
	public const uint GL_GEQUAL = 0x0206;
	public const uint GL_ALWAYS = 0x0207;
	public const uint GL_SRC_COLOR = 0x0300;
	public const uint GL_ONE_MINUS_SRC_COLOR = 0x0301;
	public const uint GL_SRC_ALPHA = 0x0302;
	public const uint GL_ONE_MINUS_SRC_ALPHA = 0x0303;
	public const uint GL_DST_ALPHA = 0x0304;
	public const uint GL_ONE_MINUS_DST_ALPHA = 0x0305;
	public const uint GL_DST_COLOR = 0x0306;
	public const uint GL_ONE_MINUS_DST_COLOR = 0x0307;
	public const uint GL_SRC_ALPHA_SATURATE = 0x0308;
	public const uint GL_FRONT_LEFT = 0x0400;
	public const uint GL_FRONT_RIGHT = 0x0401;
	public const uint GL_BACK_LEFT = 0x0402;
	public const uint GL_BACK_RIGHT = 0x0403;
	public const uint GL_FRONT = 0x0404;
	public const uint GL_BACK = 0x0405;
	public const uint GL_LEFT = 0x0406;
	public const uint GL_RIGHT = 0x0407;
	public const uint GL_FRONT_AND_BACK = 0x0408;
	public const uint GL_INVALID_ENUM = 0x0500;
	public const uint GL_INVALID_VALUE = 0x0501;
	public const uint GL_INVALID_OPERATION = 0x0502;
	public const uint GL_OUT_OF_MEMORY = 0x0505;
	public const uint GL_INVALID_FRAMEBUFFER_OPERATION = 0x0506;
	public const uint GL_CONTEXT_LOST = 0x0507;
	public const uint GL_CW = 0x0900;
	public const uint GL_CCW = 0x0901;
	public const uint GL_POINT_SIZE = 0x0B11;
	public const uint GL_POINT_SIZE_RANGE = 0x0B12;
	public const uint GL_SMOOTH_POINT_SIZE_RANGE = 0x0B12;
	public const uint GL_POINT_SIZE_GRANULARITY = 0x0B13;
	public const uint GL_SMOOTH_POINT_SIZE_GRANULARITY = 0x0B13;
	public const uint GL_LINE_SMOOTH = 0x0B20;
	public const uint GL_LINE_WIDTH = 0x0B21;
	public const uint GL_LINE_WIDTH_RANGE = 0x0B22;
	public const uint GL_SMOOTH_LINE_WIDTH_RANGE = 0x0B22;
	public const uint GL_LINE_WIDTH_GRANULARITY = 0x0B23;
	public const uint GL_SMOOTH_LINE_WIDTH_GRANULARITY = 0x0B23;
	public const uint GL_POLYGON_MODE = 0x0B40;
	public const uint GL_POLYGON_SMOOTH = 0x0B41;
	public const uint GL_CULL_FACE = 0x0B44;
	public const uint GL_CULL_FACE_MODE = 0x0B45;
	public const uint GL_FRONT_FACE = 0x0B46;
	public const uint GL_DEPTH_RANGE = 0x0B70;
	public const uint GL_DEPTH_TEST = 0x0B71;
	public const uint GL_DEPTH_WRITEMASK = 0x0B72;
	public const uint GL_DEPTH_CLEAR_VALUE = 0x0B73;
	public const uint GL_DEPTH_FUNC = 0x0B74;
	public const uint GL_STENCIL_TEST = 0x0B90;
	public const uint GL_STENCIL_CLEAR_VALUE = 0x0B91;
	public const uint GL_STENCIL_FUNC = 0x0B92;
	public const uint GL_STENCIL_VALUE_MASK = 0x0B93;
	public const uint GL_STENCIL_FAIL = 0x0B94;
	public const uint GL_STENCIL_PASS_DEPTH_FAIL = 0x0B95;
	public const uint GL_STENCIL_PASS_DEPTH_PASS = 0x0B96;
	public const uint GL_STENCIL_REF = 0x0B97;
	public const uint GL_STENCIL_WRITEMASK = 0x0B98;
	public const uint GL_VIEWPORT = 0x0BA2;
	public const uint GL_DITHER = 0x0BD0;
	public const uint GL_BLEND_DST = 0x0BE0;
	public const uint GL_BLEND_SRC = 0x0BE1;
	public const uint GL_BLEND = 0x0BE2;
	public const uint GL_LOGIC_OP_MODE = 0x0BF0;
	public const uint GL_COLOR_LOGIC_OP = 0x0BF2;
	public const uint GL_DRAW_BUFFER = 0x0C01;
	public const uint GL_READ_BUFFER = 0x0C02;
	public const uint GL_SCISSOR_BOX = 0x0C10;
	public const uint GL_SCISSOR_TEST = 0x0C11;
	public const uint GL_COLOR_CLEAR_VALUE = 0x0C22;
	public const uint GL_COLOR_WRITEMASK = 0x0C23;
	public const uint GL_DOUBLEBUFFER = 0x0C32;
	public const uint GL_STEREO = 0x0C33;
	public const uint GL_LINE_SMOOTH_HINT = 0x0C52;
	public const uint GL_POLYGON_SMOOTH_HINT = 0x0C53;
	public const uint GL_UNPACK_SWAP_BYTES = 0x0CF0;
	public const uint GL_UNPACK_LSB_FIRST = 0x0CF1;
	public const uint GL_UNPACK_ROW_LENGTH = 0x0CF2;
	public const uint GL_UNPACK_SKIP_ROWS = 0x0CF3;
	public const uint GL_UNPACK_SKIP_PIXELS = 0x0CF4;
	public const uint GL_UNPACK_ALIGNMENT = 0x0CF5;
	public const uint GL_PACK_SWAP_BYTES = 0x0D00;
	public const uint GL_PACK_LSB_FIRST = 0x0D01;
	public const uint GL_PACK_ROW_LENGTH = 0x0D02;
	public const uint GL_PACK_SKIP_ROWS = 0x0D03;
	public const uint GL_PACK_SKIP_PIXELS = 0x0D04;
	public const uint GL_PACK_ALIGNMENT = 0x0D05;
	public const uint GL_MAX_CLIP_DISTANCES = 0x0D32;
	public const uint GL_MAX_TEXTURE_SIZE = 0x0D33;
	public const uint GL_MAX_VIEWPORT_DIMS = 0x0D3A;
	public const uint GL_SUBPIXEL_BITS = 0x0D50;
	public const uint GL_TEXTURE_1D = 0x0DE0;
	public const uint GL_TEXTURE_2D = 0x0DE1;
	public const uint GL_TEXTURE_WIDTH = 0x1000;
	public const uint GL_TEXTURE_HEIGHT = 0x1001;
	public const uint GL_TEXTURE_INTERNAL_FORMAT = 0x1003;
	public const uint GL_TEXTURE_BORDER_COLOR = 0x1004;
	public const uint GL_TEXTURE_TARGET = 0x1006;
	public const uint GL_DONT_CARE = 0x1100;
	public const uint GL_FASTEST = 0x1101;
	public const uint GL_NICEST = 0x1102;
	public const uint GL_BYTE = 0x1400;
	public const uint GL_UNSIGNED_BYTE = 0x1401;
	public const uint GL_SHORT = 0x1402;
	public const uint GL_UNSIGNED_SHORT = 0x1403;
	public const uint GL_INT = 0x1404;
	public const uint GL_UNSIGNED_INT = 0x1405;
	public const uint GL_FLOAT = 0x1406;
	public const uint GL_DOUBLE = 0x140A;
	public const uint GL_HALF_FLOAT = 0x140B;
	public const uint GL_FIXED = 0x140C;
	public const uint GL_CLEAR = 0x1500;
	public const uint GL_AND = 0x1501;
	public const uint GL_AND_REVERSE = 0x1502;
	public const uint GL_COPY = 0x1503;
	public const uint GL_AND_INVERTED = 0x1504;
	public const uint GL_NOOP = 0x1505;
	public const uint GL_XOR = 0x1506;
	public const uint GL_OR = 0x1507;
	public const uint GL_NOR = 0x1508;
	public const uint GL_EQUIV = 0x1509;
	public const uint GL_INVERT = 0x150A;
	public const uint GL_OR_REVERSE = 0x150B;
	public const uint GL_COPY_INVERTED = 0x150C;
	public const uint GL_OR_INVERTED = 0x150D;
	public const uint GL_NAND = 0x150E;
	public const uint GL_SET = 0x150F;
	public const uint GL_TEXTURE = 0x1702;
	public const uint GL_COLOR = 0x1800;
	public const uint GL_DEPTH = 0x1801;
	public const uint GL_STENCIL = 0x1802;
	public const uint GL_STENCIL_INDEX = 0x1901;
	public const uint GL_DEPTH_COMPONENT = 0x1902;
	public const uint GL_RED = 0x1903;
	public const uint GL_GREEN = 0x1904;
	public const uint GL_BLUE = 0x1905;
	public const uint GL_ALPHA = 0x1906;
	public const uint GL_RGB = 0x1907;
	public const uint GL_RGBA = 0x1908;
	public const uint GL_POINT = 0x1B00;
	public const uint GL_LINE = 0x1B01;
	public const uint GL_FILL = 0x1B02;
	public const uint GL_KEEP = 0x1E00;
	public const uint GL_REPLACE = 0x1E01;
	public const uint GL_INCR = 0x1E02;
	public const uint GL_DECR = 0x1E03;
	public const uint GL_VENDOR = 0x1F00;
	public const uint GL_RENDERER = 0x1F01;
	public const uint GL_VERSION = 0x1F02;
	public const uint GL_EXTENSIONS = 0x1F03;
	public const uint GL_NEAREST = 0x2600;
	public const uint GL_LINEAR = 0x2601;
	public const uint GL_NEAREST_MIPMAP_NEAREST = 0x2700;
	public const uint GL_LINEAR_MIPMAP_NEAREST = 0x2701;
	public const uint GL_NEAREST_MIPMAP_LINEAR = 0x2702;
	public const uint GL_LINEAR_MIPMAP_LINEAR = 0x2703;
	public const uint GL_TEXTURE_MAG_FILTER = 0x2800;
	public const uint GL_TEXTURE_MIN_FILTER = 0x2801;
	public const uint GL_TEXTURE_WRAP_S = 0x2802;
	public const uint GL_TEXTURE_WRAP_T = 0x2803;
	public const uint GL_REPEAT = 0x2901;
	public const uint GL_POLYGON_OFFSET_UNITS = 0x2A00;
	public const uint GL_POLYGON_OFFSET_POINT = 0x2A01;
	public const uint GL_POLYGON_OFFSET_LINE = 0x2A02;
	public const uint GL_R3_G3_B2 = 0x2A10;
	public const uint GL_CLIP_DISTANCE0 = 0x3000;
	public const uint GL_CLIP_DISTANCE1 = 0x3001;
	public const uint GL_CLIP_DISTANCE2 = 0x3002;
	public const uint GL_CLIP_DISTANCE3 = 0x3003;
	public const uint GL_CLIP_DISTANCE4 = 0x3004;
	public const uint GL_CLIP_DISTANCE5 = 0x3005;
	public const uint GL_CLIP_DISTANCE6 = 0x3006;
	public const uint GL_CLIP_DISTANCE7 = 0x3007;
	public const uint GL_CONSTANT_COLOR = 0x8001;
	public const uint GL_ONE_MINUS_CONSTANT_COLOR = 0x8002;
	public const uint GL_CONSTANT_ALPHA = 0x8003;
	public const uint GL_ONE_MINUS_CONSTANT_ALPHA = 0x8004;
	public const uint GL_BLEND_COLOR = 0x8005;
	public const uint GL_FUNC_ADD = 0x8006;
	public const uint GL_MIN = 0x8007;
	public const uint GL_MAX = 0x8008;
	public const uint GL_BLEND_EQUATION = 0x8009;
	public const uint GL_BLEND_EQUATION_RGB = 0x8009;
	public const uint GL_FUNC_SUBTRACT = 0x800A;
	public const uint GL_FUNC_REVERSE_SUBTRACT = 0x800B;
	public const uint GL_CONVOLUTION_1D = 0x8010;
	public const uint GL_CONVOLUTION_2D = 0x8011;
	public const uint GL_SEPARABLE_2D = 0x8012;
	public const uint GL_HISTOGRAM = 0x8024;
	public const uint GL_PROXY_HISTOGRAM = 0x8025;
	public const uint GL_MINMAX = 0x802E;
	public const uint GL_UNSIGNED_BYTE_3_3_2 = 0x8032;
	public const uint GL_UNSIGNED_SHORT_4_4_4_4 = 0x8033;
	public const uint GL_UNSIGNED_SHORT_5_5_5_1 = 0x8034;
	public const uint GL_UNSIGNED_INT_8_8_8_8 = 0x8035;
	public const uint GL_UNSIGNED_INT_10_10_10_2 = 0x8036;
	public const uint GL_POLYGON_OFFSET_FILL = 0x8037;
	public const uint GL_POLYGON_OFFSET_FACTOR = 0x8038;
	public const uint GL_RGB4 = 0x804F;
	public const uint GL_RGB5 = 0x8050;
	public const uint GL_RGB8 = 0x8051;
	public const uint GL_RGB10 = 0x8052;
	public const uint GL_RGB12 = 0x8053;
	public const uint GL_RGB16 = 0x8054;
	public const uint GL_RGBA2 = 0x8055;
	public const uint GL_RGBA4 = 0x8056;
	public const uint GL_RGB5_A1 = 0x8057;
	public const uint GL_RGBA8 = 0x8058;
	public const uint GL_RGB10_A2 = 0x8059;
	public const uint GL_RGBA12 = 0x805A;
	public const uint GL_RGBA16 = 0x805B;
	public const uint GL_TEXTURE_RED_SIZE = 0x805C;
	public const uint GL_TEXTURE_GREEN_SIZE = 0x805D;
	public const uint GL_TEXTURE_BLUE_SIZE = 0x805E;
	public const uint GL_TEXTURE_ALPHA_SIZE = 0x805F;
	public const uint GL_PROXY_TEXTURE_1D = 0x8063;
	public const uint GL_PROXY_TEXTURE_2D = 0x8064;
	public const uint GL_TEXTURE_BINDING_1D = 0x8068;
	public const uint GL_TEXTURE_BINDING_2D = 0x8069;
	public const uint GL_TEXTURE_BINDING_3D = 0x806A;
	public const uint GL_PACK_SKIP_IMAGES = 0x806B;
	public const uint GL_PACK_IMAGE_HEIGHT = 0x806C;
	public const uint GL_UNPACK_SKIP_IMAGES = 0x806D;
	public const uint GL_UNPACK_IMAGE_HEIGHT = 0x806E;
	public const uint GL_TEXTURE_3D = 0x806F;
	public const uint GL_PROXY_TEXTURE_3D = 0x8070;
	public const uint GL_TEXTURE_DEPTH = 0x8071;
	public const uint GL_TEXTURE_WRAP_R = 0x8072;
	public const uint GL_MAX_3D_TEXTURE_SIZE = 0x8073;
	public const uint GL_MULTISAMPLE = 0x809D;
	public const uint GL_SAMPLE_ALPHA_TO_COVERAGE = 0x809E;
	public const uint GL_SAMPLE_ALPHA_TO_ONE = 0x809F;
	public const uint GL_SAMPLE_COVERAGE = 0x80A0;
	public const uint GL_SAMPLE_BUFFERS = 0x80A8;
	public const uint GL_SAMPLES = 0x80A9;
	public const uint GL_SAMPLE_COVERAGE_VALUE = 0x80AA;
	public const uint GL_SAMPLE_COVERAGE_INVERT = 0x80AB;
	public const uint GL_BLEND_DST_RGB = 0x80C8;
	public const uint GL_BLEND_SRC_RGB = 0x80C9;
	public const uint GL_BLEND_DST_ALPHA = 0x80CA;
	public const uint GL_BLEND_SRC_ALPHA = 0x80CB;
	public const uint GL_COLOR_TABLE = 0x80D0;
	public const uint GL_POST_CONVOLUTION_COLOR_TABLE = 0x80D1;
	public const uint GL_POST_COLOR_MATRIX_COLOR_TABLE = 0x80D2;
	public const uint GL_PROXY_COLOR_TABLE = 0x80D3;
	public const uint GL_PROXY_POST_CONVOLUTION_COLOR_TABLE = 0x80D4;
	public const uint GL_PROXY_POST_COLOR_MATRIX_COLOR_TABLE = 0x80D5;
	public const uint GL_BGR = 0x80E0;
	public const uint GL_BGRA = 0x80E1;
	public const uint GL_MAX_ELEMENTS_VERTICES = 0x80E8;
	public const uint GL_MAX_ELEMENTS_INDICES = 0x80E9;
	public const uint GL_PARAMETER_BUFFER = 0x80EE;
	public const uint GL_PARAMETER_BUFFER_BINDING = 0x80EF;
	public const uint GL_POINT_FADE_THRESHOLD_SIZE = 0x8128;
	public const uint GL_CLAMP_TO_BORDER = 0x812D;
	public const uint GL_CLAMP_TO_EDGE = 0x812F;
	public const uint GL_TEXTURE_MIN_LOD = 0x813A;
	public const uint GL_TEXTURE_MAX_LOD = 0x813B;
	public const uint GL_TEXTURE_BASE_LEVEL = 0x813C;
	public const uint GL_TEXTURE_MAX_LEVEL = 0x813D;
	public const uint GL_DEPTH_COMPONENT16 = 0x81A5;
	public const uint GL_DEPTH_COMPONENT24 = 0x81A6;
	public const uint GL_DEPTH_COMPONENT32 = 0x81A7;
	public const uint GL_FRAMEBUFFER_ATTACHMENT_COLOR_ENCODING = 0x8210;
	public const uint GL_FRAMEBUFFER_ATTACHMENT_COMPONENT_TYPE = 0x8211;
	public const uint GL_FRAMEBUFFER_ATTACHMENT_RED_SIZE = 0x8212;
	public const uint GL_FRAMEBUFFER_ATTACHMENT_GREEN_SIZE = 0x8213;
	public const uint GL_FRAMEBUFFER_ATTACHMENT_BLUE_SIZE = 0x8214;
	public const uint GL_FRAMEBUFFER_ATTACHMENT_ALPHA_SIZE = 0x8215;
	public const uint GL_FRAMEBUFFER_ATTACHMENT_DEPTH_SIZE = 0x8216;
	public const uint GL_FRAMEBUFFER_ATTACHMENT_STENCIL_SIZE = 0x8217;
	public const uint GL_FRAMEBUFFER_DEFAULT = 0x8218;
	public const uint GL_FRAMEBUFFER_UNDEFINED = 0x8219;
	public const uint GL_DEPTH_STENCIL_ATTACHMENT = 0x821A;
	public const uint GL_MAJOR_VERSION = 0x821B;
	public const uint GL_MINOR_VERSION = 0x821C;
	public const uint GL_NUM_EXTENSIONS = 0x821D;
	public const uint GL_CONTEXT_FLAGS = 0x821E;
	public const uint GL_BUFFER_IMMUTABLE_STORAGE = 0x821F;
	public const uint GL_BUFFER_STORAGE_FLAGS = 0x8220;
	public const uint GL_PRIMITIVE_RESTART_FOR_PATCHES_SUPPORTED = 0x8221;
	public const uint GL_COMPRESSED_RED = 0x8225;
	public const uint GL_COMPRESSED_RG = 0x8226;
	public const uint GL_RG = 0x8227;
	public const uint GL_RG_INTEGER = 0x8228;
	public const uint GL_R8 = 0x8229;
	public const uint GL_R16 = 0x822A;
	public const uint GL_RG8 = 0x822B;
	public const uint GL_RG16 = 0x822C;
	public const uint GL_R16F = 0x822D;
	public const uint GL_R32F = 0x822E;
	public const uint GL_RG16F = 0x822F;
	public const uint GL_RG32F = 0x8230;
	public const uint GL_R8I = 0x8231;
	public const uint GL_R8UI = 0x8232;
	public const uint GL_R16I = 0x8233;
	public const uint GL_R16UI = 0x8234;
	public const uint GL_R32I = 0x8235;
	public const uint GL_R32UI = 0x8236;
	public const uint GL_RG8I = 0x8237;
	public const uint GL_RG8UI = 0x8238;
	public const uint GL_RG16I = 0x8239;
	public const uint GL_RG16UI = 0x823A;
	public const uint GL_RG32I = 0x823B;
	public const uint GL_RG32UI = 0x823C;
	public const uint GL_DEBUG_OUTPUT_SYNCHRONOUS = 0x8242;
	public const uint GL_DEBUG_NEXT_LOGGED_MESSAGE_LENGTH = 0x8243;
	public const uint GL_DEBUG_CALLBACK_FUNCTION = 0x8244;
	public const uint GL_DEBUG_CALLBACK_USER_PARAM = 0x8245;
	public const uint GL_DEBUG_SOURCE_API = 0x8246;
	public const uint GL_DEBUG_SOURCE_WINDOW_SYSTEM = 0x8247;
	public const uint GL_DEBUG_SOURCE_SHADER_COMPILER = 0x8248;
	public const uint GL_DEBUG_SOURCE_THIRD_PARTY = 0x8249;
	public const uint GL_DEBUG_SOURCE_APPLICATION = 0x824A;
	public const uint GL_DEBUG_SOURCE_OTHER = 0x824B;
	public const uint GL_DEBUG_TYPE_ERROR = 0x824C;
	public const uint GL_DEBUG_TYPE_DEPRECATED_BEHAVIOR = 0x824D;
	public const uint GL_DEBUG_TYPE_UNDEFINED_BEHAVIOR = 0x824E;
	public const uint GL_DEBUG_TYPE_PORTABILITY = 0x824F;
	public const uint GL_DEBUG_TYPE_PERFORMANCE = 0x8250;
	public const uint GL_DEBUG_TYPE_OTHER = 0x8251;
	public const uint GL_LOSE_CONTEXT_ON_RESET = 0x8252;
	public const uint GL_GUILTY_CONTEXT_RESET = 0x8253;
	public const uint GL_INNOCENT_CONTEXT_RESET = 0x8254;
	public const uint GL_UNKNOWN_CONTEXT_RESET = 0x8255;
	public const uint GL_RESET_NOTIFICATION_STRATEGY = 0x8256;
	public const uint GL_PROGRAM_BINARY_RETRIEVABLE_HINT = 0x8257;
	public const uint GL_PROGRAM_SEPARABLE = 0x8258;
	public const uint GL_ACTIVE_PROGRAM = 0x8259;
	public const uint GL_PROGRAM_PIPELINE_BINDING = 0x825A;
	public const uint GL_MAX_VIEWPORTS = 0x825B;
	public const uint GL_VIEWPORT_SUBPIXEL_BITS = 0x825C;
	public const uint GL_VIEWPORT_BOUNDS_RANGE = 0x825D;
	public const uint GL_LAYER_PROVOKING_VERTEX = 0x825E;
	public const uint GL_VIEWPORT_INDEX_PROVOKING_VERTEX = 0x825F;
	public const uint GL_UNDEFINED_VERTEX = 0x8260;
	public const uint GL_NO_RESET_NOTIFICATION = 0x8261;
	public const uint GL_MAX_COMPUTE_SHARED_MEMORY_SIZE = 0x8262;
	public const uint GL_MAX_COMPUTE_UNIFORM_COMPONENTS = 0x8263;
	public const uint GL_MAX_COMPUTE_ATOMIC_COUNTER_BUFFERS = 0x8264;
	public const uint GL_MAX_COMPUTE_ATOMIC_COUNTERS = 0x8265;
	public const uint GL_MAX_COMBINED_COMPUTE_UNIFORM_COMPONENTS = 0x8266;
	public const uint GL_COMPUTE_WORK_GROUP_SIZE = 0x8267;
	public const uint GL_DEBUG_TYPE_MARKER = 0x8268;
	public const uint GL_DEBUG_TYPE_PUSH_GROUP = 0x8269;
	public const uint GL_DEBUG_TYPE_POP_GROUP = 0x826A;
	public const uint GL_DEBUG_SEVERITY_NOTIFICATION = 0x826B;
	public const uint GL_MAX_DEBUG_GROUP_STACK_DEPTH = 0x826C;
	public const uint GL_DEBUG_GROUP_STACK_DEPTH = 0x826D;
	public const uint GL_MAX_UNIFORM_LOCATIONS = 0x826E;
	public const uint GL_INTERNALFORMAT_SUPPORTED = 0x826F;
	public const uint GL_INTERNALFORMAT_PREFERRED = 0x8270;
	public const uint GL_INTERNALFORMAT_RED_SIZE = 0x8271;
	public const uint GL_INTERNALFORMAT_GREEN_SIZE = 0x8272;
	public const uint GL_INTERNALFORMAT_BLUE_SIZE = 0x8273;
	public const uint GL_INTERNALFORMAT_ALPHA_SIZE = 0x8274;
	public const uint GL_INTERNALFORMAT_DEPTH_SIZE = 0x8275;
	public const uint GL_INTERNALFORMAT_STENCIL_SIZE = 0x8276;
	public const uint GL_INTERNALFORMAT_SHARED_SIZE = 0x8277;
	public const uint GL_INTERNALFORMAT_RED_TYPE = 0x8278;
	public const uint GL_INTERNALFORMAT_GREEN_TYPE = 0x8279;
	public const uint GL_INTERNALFORMAT_BLUE_TYPE = 0x827A;
	public const uint GL_INTERNALFORMAT_ALPHA_TYPE = 0x827B;
	public const uint GL_INTERNALFORMAT_DEPTH_TYPE = 0x827C;
	public const uint GL_INTERNALFORMAT_STENCIL_TYPE = 0x827D;
	public const uint GL_MAX_WIDTH = 0x827E;
	public const uint GL_MAX_HEIGHT = 0x827F;
	public const uint GL_MAX_DEPTH = 0x8280;
	public const uint GL_MAX_LAYERS = 0x8281;
	public const uint GL_MAX_COMBINED_DIMENSIONS = 0x8282;
	public const uint GL_COLOR_COMPONENTS = 0x8283;
	public const uint GL_DEPTH_COMPONENTS = 0x8284;
	public const uint GL_STENCIL_COMPONENTS = 0x8285;
	public const uint GL_COLOR_RENDERABLE = 0x8286;
	public const uint GL_DEPTH_RENDERABLE = 0x8287;
	public const uint GL_STENCIL_RENDERABLE = 0x8288;
	public const uint GL_FRAMEBUFFER_RENDERABLE = 0x8289;
	public const uint GL_FRAMEBUFFER_RENDERABLE_LAYERED = 0x828A;
	public const uint GL_FRAMEBUFFER_BLEND = 0x828B;
	public const uint GL_READ_PIXELS = 0x828C;
	public const uint GL_READ_PIXELS_FORMAT = 0x828D;
	public const uint GL_READ_PIXELS_TYPE = 0x828E;
	public const uint GL_TEXTURE_IMAGE_FORMAT = 0x828F;
	public const uint GL_TEXTURE_IMAGE_TYPE = 0x8290;
	public const uint GL_GET_TEXTURE_IMAGE_FORMAT = 0x8291;
	public const uint GL_GET_TEXTURE_IMAGE_TYPE = 0x8292;
	public const uint GL_MIPMAP = 0x8293;
	public const uint GL_MANUAL_GENERATE_MIPMAP = 0x8294;
	public const uint GL_AUTO_GENERATE_MIPMAP = 0x8295;
	public const uint GL_COLOR_ENCODING = 0x8296;
	public const uint GL_SRGB_READ = 0x8297;
	public const uint GL_SRGB_WRITE = 0x8298;
	public const uint GL_FILTER = 0x829A;
	public const uint GL_VERTEX_TEXTURE = 0x829B;
	public const uint GL_TESS_CONTROL_TEXTURE = 0x829C;
	public const uint GL_TESS_EVALUATION_TEXTURE = 0x829D;
	public const uint GL_GEOMETRY_TEXTURE = 0x829E;
	public const uint GL_FRAGMENT_TEXTURE = 0x829F;
	public const uint GL_COMPUTE_TEXTURE = 0x82A0;
	public const uint GL_TEXTURE_SHADOW = 0x82A1;
	public const uint GL_TEXTURE_GATHER = 0x82A2;
	public const uint GL_TEXTURE_GATHER_SHADOW = 0x82A3;
	public const uint GL_SHADER_IMAGE_LOAD = 0x82A4;
	public const uint GL_SHADER_IMAGE_STORE = 0x82A5;
	public const uint GL_SHADER_IMAGE_ATOMIC = 0x82A6;
	public const uint GL_IMAGE_TEXEL_SIZE = 0x82A7;
	public const uint GL_IMAGE_COMPATIBILITY_CLASS = 0x82A8;
	public const uint GL_IMAGE_PIXEL_FORMAT = 0x82A9;
	public const uint GL_IMAGE_PIXEL_TYPE = 0x82AA;
	public const uint GL_SIMULTANEOUS_TEXTURE_AND_DEPTH_TEST = 0x82AC;
	public const uint GL_SIMULTANEOUS_TEXTURE_AND_STENCIL_TEST = 0x82AD;
	public const uint GL_SIMULTANEOUS_TEXTURE_AND_DEPTH_WRITE = 0x82AE;
	public const uint GL_SIMULTANEOUS_TEXTURE_AND_STENCIL_WRITE = 0x82AF;
	public const uint GL_TEXTURE_COMPRESSED_BLOCK_WIDTH = 0x82B1;
	public const uint GL_TEXTURE_COMPRESSED_BLOCK_HEIGHT = 0x82B2;
	public const uint GL_TEXTURE_COMPRESSED_BLOCK_SIZE = 0x82B3;
	public const uint GL_CLEAR_BUFFER = 0x82B4;
	public const uint GL_TEXTURE_VIEW = 0x82B5;
	public const uint GL_VIEW_COMPATIBILITY_CLASS = 0x82B6;
	public const uint GL_FULL_SUPPORT = 0x82B7;
	public const uint GL_CAVEAT_SUPPORT = 0x82B8;
	public const uint GL_IMAGE_CLASS_4_X_32 = 0x82B9;
	public const uint GL_IMAGE_CLASS_2_X_32 = 0x82BA;
	public const uint GL_IMAGE_CLASS_1_X_32 = 0x82BB;
	public const uint GL_IMAGE_CLASS_4_X_16 = 0x82BC;
	public const uint GL_IMAGE_CLASS_2_X_16 = 0x82BD;
	public const uint GL_IMAGE_CLASS_1_X_16 = 0x82BE;
	public const uint GL_IMAGE_CLASS_4_X_8 = 0x82BF;
	public const uint GL_IMAGE_CLASS_2_X_8 = 0x82C0;
	public const uint GL_IMAGE_CLASS_1_X_8 = 0x82C1;
	public const uint GL_IMAGE_CLASS_11_11_10 = 0x82C2;
	public const uint GL_IMAGE_CLASS_10_10_10_2 = 0x82C3;
	public const uint GL_VIEW_CLASS_128_BITS = 0x82C4;
	public const uint GL_VIEW_CLASS_96_BITS = 0x82C5;
	public const uint GL_VIEW_CLASS_64_BITS = 0x82C6;
	public const uint GL_VIEW_CLASS_48_BITS = 0x82C7;
	public const uint GL_VIEW_CLASS_32_BITS = 0x82C8;
	public const uint GL_VIEW_CLASS_24_BITS = 0x82C9;
	public const uint GL_VIEW_CLASS_16_BITS = 0x82CA;
	public const uint GL_VIEW_CLASS_8_BITS = 0x82CB;
	public const uint GL_VIEW_CLASS_S3TC_DXT1_RGB = 0x82CC;
	public const uint GL_VIEW_CLASS_S3TC_DXT1_RGBA = 0x82CD;
	public const uint GL_VIEW_CLASS_S3TC_DXT3_RGBA = 0x82CE;
	public const uint GL_VIEW_CLASS_S3TC_DXT5_RGBA = 0x82CF;
	public const uint GL_VIEW_CLASS_RGTC1_RED = 0x82D0;
	public const uint GL_VIEW_CLASS_RGTC2_RG = 0x82D1;
	public const uint GL_VIEW_CLASS_BPTC_UNORM = 0x82D2;
	public const uint GL_VIEW_CLASS_BPTC_FLOAT = 0x82D3;
	public const uint GL_VERTEX_ATTRIB_BINDING = 0x82D4;
	public const uint GL_VERTEX_ATTRIB_RELATIVE_OFFSET = 0x82D5;
	public const uint GL_VERTEX_BINDING_DIVISOR = 0x82D6;
	public const uint GL_VERTEX_BINDING_OFFSET = 0x82D7;
	public const uint GL_VERTEX_BINDING_STRIDE = 0x82D8;
	public const uint GL_MAX_VERTEX_ATTRIB_RELATIVE_OFFSET = 0x82D9;
	public const uint GL_MAX_VERTEX_ATTRIB_BINDINGS = 0x82DA;
	public const uint GL_TEXTURE_VIEW_MIN_LEVEL = 0x82DB;
	public const uint GL_TEXTURE_VIEW_NUM_LEVELS = 0x82DC;
	public const uint GL_TEXTURE_VIEW_MIN_LAYER = 0x82DD;
	public const uint GL_TEXTURE_VIEW_NUM_LAYERS = 0x82DE;
	public const uint GL_TEXTURE_IMMUTABLE_LEVELS = 0x82DF;
	public const uint GL_BUFFER = 0x82E0;
	public const uint GL_SHADER = 0x82E1;
	public const uint GL_PROGRAM = 0x82E2;
	public const uint GL_QUERY = 0x82E3;
	public const uint GL_PROGRAM_PIPELINE = 0x82E4;
	public const uint GL_MAX_VERTEX_ATTRIB_STRIDE = 0x82E5;
	public const uint GL_SAMPLER = 0x82E6;
	public const uint GL_DISPLAY_LIST = 0x82E7;
	public const uint GL_MAX_LABEL_LENGTH = 0x82E8;
	public const uint GL_NUM_SHADING_LANGUAGE_VERSIONS = 0x82E9;
	public const uint GL_QUERY_TARGET = 0x82EA;
	public const uint GL_TRANSFORM_FEEDBACK_OVERFLOW = 0x82EC;
	public const uint GL_TRANSFORM_FEEDBACK_STREAM_OVERFLOW = 0x82ED;
	public const uint GL_VERTICES_SUBMITTED = 0x82EE;
	public const uint GL_PRIMITIVES_SUBMITTED = 0x82EF;
	public const uint GL_VERTEX_SHADER_INVOCATIONS = 0x82F0;
	public const uint GL_TESS_CONTROL_SHADER_PATCHES = 0x82F1;
	public const uint GL_TESS_EVALUATION_SHADER_INVOCATIONS = 0x82F2;
	public const uint GL_GEOMETRY_SHADER_PRIMITIVES_EMITTED = 0x82F3;
	public const uint GL_FRAGMENT_SHADER_INVOCATIONS = 0x82F4;
	public const uint GL_COMPUTE_SHADER_INVOCATIONS = 0x82F5;
	public const uint GL_CLIPPING_INPUT_PRIMITIVES = 0x82F6;
	public const uint GL_CLIPPING_OUTPUT_PRIMITIVES = 0x82F7;
	public const uint GL_MAX_CULL_DISTANCES = 0x82F9;
	public const uint GL_MAX_COMBINED_CLIP_AND_CULL_DISTANCES = 0x82FA;
	public const uint GL_CONTEXT_RELEASE_BEHAVIOR = 0x82FB;
	public const uint GL_CONTEXT_RELEASE_BEHAVIOR_FLUSH = 0x82FC;
	public const uint GL_UNSIGNED_BYTE_2_3_3_REV = 0x8362;
	public const uint GL_UNSIGNED_SHORT_5_6_5 = 0x8363;
	public const uint GL_UNSIGNED_SHORT_5_6_5_REV = 0x8364;
	public const uint GL_UNSIGNED_SHORT_4_4_4_4_REV = 0x8365;
	public const uint GL_UNSIGNED_SHORT_1_5_5_5_REV = 0x8366;
	public const uint GL_UNSIGNED_INT_8_8_8_8_REV = 0x8367;
	public const uint GL_UNSIGNED_INT_2_10_10_10_REV = 0x8368;
	public const uint GL_MIRRORED_REPEAT = 0x8370;
	public const uint GL_ALIASED_LINE_WIDTH_RANGE = 0x846E;
	public const uint GL_TEXTURE0 = 0x84C0;
	public const uint GL_TEXTURE1 = 0x84C1;
	public const uint GL_TEXTURE2 = 0x84C2;
	public const uint GL_TEXTURE3 = 0x84C3;
	public const uint GL_TEXTURE4 = 0x84C4;
	public const uint GL_TEXTURE5 = 0x84C5;
	public const uint GL_TEXTURE6 = 0x84C6;
	public const uint GL_TEXTURE7 = 0x84C7;
	public const uint GL_TEXTURE8 = 0x84C8;
	public const uint GL_TEXTURE9 = 0x84C9;
	public const uint GL_TEXTURE10 = 0x84CA;
	public const uint GL_TEXTURE11 = 0x84CB;
	public const uint GL_TEXTURE12 = 0x84CC;
	public const uint GL_TEXTURE13 = 0x84CD;
	public const uint GL_TEXTURE14 = 0x84CE;
	public const uint GL_TEXTURE15 = 0x84CF;
	public const uint GL_TEXTURE16 = 0x84D0;
	public const uint GL_TEXTURE17 = 0x84D1;
	public const uint GL_TEXTURE18 = 0x84D2;
	public const uint GL_TEXTURE19 = 0x84D3;
	public const uint GL_TEXTURE20 = 0x84D4;
	public const uint GL_TEXTURE21 = 0x84D5;
	public const uint GL_TEXTURE22 = 0x84D6;
	public const uint GL_TEXTURE23 = 0x84D7;
	public const uint GL_TEXTURE24 = 0x84D8;
	public const uint GL_TEXTURE25 = 0x84D9;
	public const uint GL_TEXTURE26 = 0x84DA;
	public const uint GL_TEXTURE27 = 0x84DB;
	public const uint GL_TEXTURE28 = 0x84DC;
	public const uint GL_TEXTURE29 = 0x84DD;
	public const uint GL_TEXTURE30 = 0x84DE;
	public const uint GL_TEXTURE31 = 0x84DF;
	public const uint GL_ACTIVE_TEXTURE = 0x84E0;
	public const uint GL_MAX_RENDERBUFFER_SIZE = 0x84E8;
	public const uint GL_COMPRESSED_RGB = 0x84ED;
	public const uint GL_COMPRESSED_RGBA = 0x84EE;
	public const uint GL_TEXTURE_COMPRESSION_HINT = 0x84EF;
	public const uint GL_UNIFORM_BLOCK_REFERENCED_BY_TESS_CONTROL_SHADER = 0x84F0;
	public const uint GL_UNIFORM_BLOCK_REFERENCED_BY_TESS_EVALUATION_SHADER = 0x84F1;
	public const uint GL_TEXTURE_RECTANGLE = 0x84F5;
	public const uint GL_TEXTURE_BINDING_RECTANGLE = 0x84F6;
	public const uint GL_PROXY_TEXTURE_RECTANGLE = 0x84F7;
	public const uint GL_MAX_RECTANGLE_TEXTURE_SIZE = 0x84F8;
	public const uint GL_DEPTH_STENCIL = 0x84F9;
	public const uint GL_UNSIGNED_INT_24_8 = 0x84FA;
	public const uint GL_MAX_TEXTURE_LOD_BIAS = 0x84FD;
	public const uint GL_TEXTURE_MAX_ANISOTROPY = 0x84FE;
	public const uint GL_MAX_TEXTURE_MAX_ANISOTROPY = 0x84FF;
	public const uint GL_TEXTURE_LOD_BIAS = 0x8501;
	public const uint GL_INCR_WRAP = 0x8507;
	public const uint GL_DECR_WRAP = 0x8508;
	public const uint GL_TEXTURE_CUBE_MAP = 0x8513;
	public const uint GL_TEXTURE_BINDING_CUBE_MAP = 0x8514;
	public const uint GL_TEXTURE_CUBE_MAP_POSITIVE_X = 0x8515;
	public const uint GL_TEXTURE_CUBE_MAP_NEGATIVE_X = 0x8516;
	public const uint GL_TEXTURE_CUBE_MAP_POSITIVE_Y = 0x8517;
	public const uint GL_TEXTURE_CUBE_MAP_NEGATIVE_Y = 0x8518;
	public const uint GL_TEXTURE_CUBE_MAP_POSITIVE_Z = 0x8519;
	public const uint GL_TEXTURE_CUBE_MAP_NEGATIVE_Z = 0x851A;
	public const uint GL_PROXY_TEXTURE_CUBE_MAP = 0x851B;
	public const uint GL_MAX_CUBE_MAP_TEXTURE_SIZE = 0x851C;
	public const uint GL_SRC1_ALPHA = 0x8589;
	public const uint GL_VERTEX_ARRAY_BINDING = 0x85B5;
	public const uint GL_VERTEX_ATTRIB_ARRAY_ENABLED = 0x8622;
	public const uint GL_VERTEX_ATTRIB_ARRAY_SIZE = 0x8623;
	public const uint GL_VERTEX_ATTRIB_ARRAY_STRIDE = 0x8624;
	public const uint GL_VERTEX_ATTRIB_ARRAY_TYPE = 0x8625;
	public const uint GL_CURRENT_VERTEX_ATTRIB = 0x8626;
	public const uint GL_VERTEX_PROGRAM_POINT_SIZE = 0x8642;
	public const uint GL_PROGRAM_POINT_SIZE = 0x8642;
	public const uint GL_VERTEX_ATTRIB_ARRAY_POINTER = 0x8645;
	public const uint GL_DEPTH_CLAMP = 0x864F;
	public const uint GL_TEXTURE_COMPRESSED_IMAGE_SIZE = 0x86A0;
	public const uint GL_TEXTURE_COMPRESSED = 0x86A1;
	public const uint GL_NUM_COMPRESSED_TEXTURE_FORMATS = 0x86A2;
	public const uint GL_COMPRESSED_TEXTURE_FORMATS = 0x86A3;
	public const uint GL_PROGRAM_BINARY_LENGTH = 0x8741;
	public const uint GL_MIRROR_CLAMP_TO_EDGE = 0x8743;
	public const uint GL_VERTEX_ATTRIB_ARRAY_LONG = 0x874E;
	public const uint GL_BUFFER_SIZE = 0x8764;
	public const uint GL_BUFFER_USAGE = 0x8765;
	public const uint GL_NUM_PROGRAM_BINARY_FORMATS = 0x87FE;
	public const uint GL_PROGRAM_BINARY_FORMATS = 0x87FF;
	public const uint GL_STENCIL_BACK_FUNC = 0x8800;
	public const uint GL_STENCIL_BACK_FAIL = 0x8801;
	public const uint GL_STENCIL_BACK_PASS_DEPTH_FAIL = 0x8802;
	public const uint GL_STENCIL_BACK_PASS_DEPTH_PASS = 0x8803;
	public const uint GL_RGBA32F = 0x8814;
	public const uint GL_RGB32F = 0x8815;
	public const uint GL_RGBA16F = 0x881A;
	public const uint GL_RGB16F = 0x881B;
	public const uint GL_MAX_DRAW_BUFFERS = 0x8824;
	public const uint GL_DRAW_BUFFER0 = 0x8825;
	public const uint GL_DRAW_BUFFER1 = 0x8826;
	public const uint GL_DRAW_BUFFER2 = 0x8827;
	public const uint GL_DRAW_BUFFER3 = 0x8828;
	public const uint GL_DRAW_BUFFER4 = 0x8829;
	public const uint GL_DRAW_BUFFER5 = 0x882A;
	public const uint GL_DRAW_BUFFER6 = 0x882B;
	public const uint GL_DRAW_BUFFER7 = 0x882C;
	public const uint GL_DRAW_BUFFER8 = 0x882D;
	public const uint GL_DRAW_BUFFER9 = 0x882E;
	public const uint GL_DRAW_BUFFER10 = 0x882F;
	public const uint GL_DRAW_BUFFER11 = 0x8830;
	public const uint GL_DRAW_BUFFER12 = 0x8831;
	public const uint GL_DRAW_BUFFER13 = 0x8832;
	public const uint GL_DRAW_BUFFER14 = 0x8833;
	public const uint GL_DRAW_BUFFER15 = 0x8834;
	public const uint GL_BLEND_EQUATION_ALPHA = 0x883D;
	public const uint GL_TEXTURE_DEPTH_SIZE = 0x884A;
	public const uint GL_TEXTURE_COMPARE_MODE = 0x884C;
	public const uint GL_TEXTURE_COMPARE_FUNC = 0x884D;
	public const uint GL_COMPARE_REF_TO_TEXTURE = 0x884E;
	public const uint GL_TEXTURE_CUBE_MAP_SEAMLESS = 0x884F;
	public const uint GL_QUERY_COUNTER_BITS = 0x8864;
	public const uint GL_CURRENT_QUERY = 0x8865;
	public const uint GL_QUERY_RESULT = 0x8866;
	public const uint GL_QUERY_RESULT_AVAILABLE = 0x8867;
	public const uint GL_MAX_VERTEX_ATTRIBS = 0x8869;
	public const uint GL_VERTEX_ATTRIB_ARRAY_NORMALIZED = 0x886A;
	public const uint GL_MAX_TESS_CONTROL_INPUT_COMPONENTS = 0x886C;
	public const uint GL_MAX_TESS_EVALUATION_INPUT_COMPONENTS = 0x886D;
	public const uint GL_MAX_TEXTURE_IMAGE_UNITS = 0x8872;
	public const uint GL_GEOMETRY_SHADER_INVOCATIONS = 0x887F;
	public const uint GL_ARRAY_BUFFER = 0x8892;
	public const uint GL_ELEMENT_ARRAY_BUFFER = 0x8893;
	public const uint GL_ARRAY_BUFFER_BINDING = 0x8894;
	public const uint GL_ELEMENT_ARRAY_BUFFER_BINDING = 0x8895;
	public const uint GL_VERTEX_ATTRIB_ARRAY_BUFFER_BINDING = 0x889F;
	public const uint GL_READ_ONLY = 0x88B8;
	public const uint GL_WRITE_ONLY = 0x88B9;
	public const uint GL_READ_WRITE = 0x88BA;
	public const uint GL_BUFFER_ACCESS = 0x88BB;
	public const uint GL_BUFFER_MAPPED = 0x88BC;
	public const uint GL_BUFFER_MAP_POINTER = 0x88BD;
	public const uint GL_TIME_ELAPSED = 0x88BF;
	public const uint GL_STREAM_DRAW = 0x88E0;
	public const uint GL_STREAM_READ = 0x88E1;
	public const uint GL_STREAM_COPY = 0x88E2;
	public const uint GL_STATIC_DRAW = 0x88E4;
	public const uint GL_STATIC_READ = 0x88E5;
	public const uint GL_STATIC_COPY = 0x88E6;
	public const uint GL_DYNAMIC_DRAW = 0x88E8;
	public const uint GL_DYNAMIC_READ = 0x88E9;
	public const uint GL_DYNAMIC_COPY = 0x88EA;
	public const uint GL_PIXEL_PACK_BUFFER = 0x88EB;
	public const uint GL_PIXEL_UNPACK_BUFFER = 0x88EC;
	public const uint GL_PIXEL_PACK_BUFFER_BINDING = 0x88ED;
	public const uint GL_PIXEL_UNPACK_BUFFER_BINDING = 0x88EF;
	public const uint GL_DEPTH24_STENCIL8 = 0x88F0;
	public const uint GL_TEXTURE_STENCIL_SIZE = 0x88F1;
	public const uint GL_SRC1_COLOR = 0x88F9;
	public const uint GL_ONE_MINUS_SRC1_COLOR = 0x88FA;
	public const uint GL_ONE_MINUS_SRC1_ALPHA = 0x88FB;
	public const uint GL_MAX_DUAL_SOURCE_DRAW_BUFFERS = 0x88FC;
	public const uint GL_VERTEX_ATTRIB_ARRAY_INTEGER = 0x88FD;
	public const uint GL_VERTEX_ATTRIB_ARRAY_DIVISOR = 0x88FE;
	public const uint GL_MAX_ARRAY_TEXTURE_LAYERS = 0x88FF;
	public const uint GL_MIN_PROGRAM_TEXEL_OFFSET = 0x8904;
	public const uint GL_MAX_PROGRAM_TEXEL_OFFSET = 0x8905;
	public const uint GL_SAMPLES_PASSED = 0x8914;
	public const uint GL_GEOMETRY_VERTICES_OUT = 0x8916;
	public const uint GL_GEOMETRY_INPUT_TYPE = 0x8917;
	public const uint GL_GEOMETRY_OUTPUT_TYPE = 0x8918;
	public const uint GL_SAMPLER_BINDING = 0x8919;
	public const uint GL_CLAMP_READ_COLOR = 0x891C;
	public const uint GL_FIXED_ONLY = 0x891D;
	public const uint GL_UNIFORM_BUFFER = 0x8A11;
	public const uint GL_UNIFORM_BUFFER_BINDING = 0x8A28;
	public const uint GL_UNIFORM_BUFFER_START = 0x8A29;
	public const uint GL_UNIFORM_BUFFER_SIZE = 0x8A2A;
	public const uint GL_MAX_VERTEX_UNIFORM_BLOCKS = 0x8A2B;
	public const uint GL_MAX_GEOMETRY_UNIFORM_BLOCKS = 0x8A2C;
	public const uint GL_MAX_FRAGMENT_UNIFORM_BLOCKS = 0x8A2D;
	public const uint GL_MAX_COMBINED_UNIFORM_BLOCKS = 0x8A2E;
	public const uint GL_MAX_UNIFORM_BUFFER_BINDINGS = 0x8A2F;
	public const uint GL_MAX_UNIFORM_BLOCK_SIZE = 0x8A30;
	public const uint GL_MAX_COMBINED_VERTEX_UNIFORM_COMPONENTS = 0x8A31;
	public const uint GL_MAX_COMBINED_GEOMETRY_UNIFORM_COMPONENTS = 0x8A32;
	public const uint GL_MAX_COMBINED_FRAGMENT_UNIFORM_COMPONENTS = 0x8A33;
	public const uint GL_UNIFORM_BUFFER_OFFSET_ALIGNMENT = 0x8A34;
	public const uint GL_ACTIVE_UNIFORM_BLOCK_MAX_NAME_LENGTH = 0x8A35;
	public const uint GL_ACTIVE_UNIFORM_BLOCKS = 0x8A36;
	public const uint GL_UNIFORM_TYPE = 0x8A37;
	public const uint GL_UNIFORM_SIZE = 0x8A38;
	public const uint GL_UNIFORM_NAME_LENGTH = 0x8A39;
	public const uint GL_UNIFORM_BLOCK_INDEX = 0x8A3A;
	public const uint GL_UNIFORM_OFFSET = 0x8A3B;
	public const uint GL_UNIFORM_ARRAY_STRIDE = 0x8A3C;
	public const uint GL_UNIFORM_MATRIX_STRIDE = 0x8A3D;
	public const uint GL_UNIFORM_IS_ROW_MAJOR = 0x8A3E;
	public const uint GL_UNIFORM_BLOCK_BINDING = 0x8A3F;
	public const uint GL_UNIFORM_BLOCK_DATA_SIZE = 0x8A40;
	public const uint GL_UNIFORM_BLOCK_NAME_LENGTH = 0x8A41;
	public const uint GL_UNIFORM_BLOCK_ACTIVE_UNIFORMS = 0x8A42;
	public const uint GL_UNIFORM_BLOCK_ACTIVE_UNIFORM_INDICES = 0x8A43;
	public const uint GL_UNIFORM_BLOCK_REFERENCED_BY_VERTEX_SHADER = 0x8A44;
	public const uint GL_UNIFORM_BLOCK_REFERENCED_BY_GEOMETRY_SHADER = 0x8A45;
	public const uint GL_UNIFORM_BLOCK_REFERENCED_BY_FRAGMENT_SHADER = 0x8A46;
	public const uint GL_FRAGMENT_SHADER = 0x8B30;
	public const uint GL_VERTEX_SHADER = 0x8B31;
	public const uint GL_MAX_FRAGMENT_UNIFORM_COMPONENTS = 0x8B49;
	public const uint GL_MAX_VERTEX_UNIFORM_COMPONENTS = 0x8B4A;
	public const uint GL_MAX_VARYING_FLOATS = 0x8B4B;
	public const uint GL_MAX_VARYING_COMPONENTS = 0x8B4B;
	public const uint GL_MAX_VERTEX_TEXTURE_IMAGE_UNITS = 0x8B4C;
	public const uint GL_MAX_COMBINED_TEXTURE_IMAGE_UNITS = 0x8B4D;
	public const uint GL_SHADER_TYPE = 0x8B4F;
	public const uint GL_FLOAT_VEC2 = 0x8B50;
	public const uint GL_FLOAT_VEC3 = 0x8B51;
	public const uint GL_FLOAT_VEC4 = 0x8B52;
	public const uint GL_INT_VEC2 = 0x8B53;
	public const uint GL_INT_VEC3 = 0x8B54;
	public const uint GL_INT_VEC4 = 0x8B55;
	public const uint GL_BOOL = 0x8B56;
	public const uint GL_BOOL_VEC2 = 0x8B57;
	public const uint GL_BOOL_VEC3 = 0x8B58;
	public const uint GL_BOOL_VEC4 = 0x8B59;
	public const uint GL_FLOAT_MAT2 = 0x8B5A;
	public const uint GL_FLOAT_MAT3 = 0x8B5B;
	public const uint GL_FLOAT_MAT4 = 0x8B5C;
	public const uint GL_SAMPLER_1D = 0x8B5D;
	public const uint GL_SAMPLER_2D = 0x8B5E;
	public const uint GL_SAMPLER_3D = 0x8B5F;
	public const uint GL_SAMPLER_CUBE = 0x8B60;
	public const uint GL_SAMPLER_1D_SHADOW = 0x8B61;
	public const uint GL_SAMPLER_2D_SHADOW = 0x8B62;
	public const uint GL_SAMPLER_2D_RECT = 0x8B63;
	public const uint GL_SAMPLER_2D_RECT_SHADOW = 0x8B64;
	public const uint GL_FLOAT_MAT2x3 = 0x8B65;
	public const uint GL_FLOAT_MAT2x4 = 0x8B66;
	public const uint GL_FLOAT_MAT3x2 = 0x8B67;
	public const uint GL_FLOAT_MAT3x4 = 0x8B68;
	public const uint GL_FLOAT_MAT4x2 = 0x8B69;
	public const uint GL_FLOAT_MAT4x3 = 0x8B6A;
	public const uint GL_DELETE_STATUS = 0x8B80;
	public const uint GL_COMPILE_STATUS = 0x8B81;
	public const uint GL_LINK_STATUS = 0x8B82;
	public const uint GL_VALIDATE_STATUS = 0x8B83;
	public const uint GL_INFO_LOG_LENGTH = 0x8B84;
	public const uint GL_ATTACHED_SHADERS = 0x8B85;
	public const uint GL_ACTIVE_UNIFORMS = 0x8B86;
	public const uint GL_ACTIVE_UNIFORM_MAX_LENGTH = 0x8B87;
	public const uint GL_SHADER_SOURCE_LENGTH = 0x8B88;
	public const uint GL_ACTIVE_ATTRIBUTES = 0x8B89;
	public const uint GL_ACTIVE_ATTRIBUTE_MAX_LENGTH = 0x8B8A;
	public const uint GL_FRAGMENT_SHADER_DERIVATIVE_HINT = 0x8B8B;
	public const uint GL_SHADING_LANGUAGE_VERSION = 0x8B8C;
	public const uint GL_CURRENT_PROGRAM = 0x8B8D;
	public const uint GL_IMPLEMENTATION_COLOR_READ_TYPE = 0x8B9A;
	public const uint GL_IMPLEMENTATION_COLOR_READ_FORMAT = 0x8B9B;
	public const uint GL_TEXTURE_RED_TYPE = 0x8C10;
	public const uint GL_TEXTURE_GREEN_TYPE = 0x8C11;
	public const uint GL_TEXTURE_BLUE_TYPE = 0x8C12;
	public const uint GL_TEXTURE_ALPHA_TYPE = 0x8C13;
	public const uint GL_TEXTURE_DEPTH_TYPE = 0x8C16;
	public const uint GL_UNSIGNED_NORMALIZED = 0x8C17;
	public const uint GL_TEXTURE_1D_ARRAY = 0x8C18;
	public const uint GL_PROXY_TEXTURE_1D_ARRAY = 0x8C19;
	public const uint GL_TEXTURE_2D_ARRAY = 0x8C1A;
	public const uint GL_PROXY_TEXTURE_2D_ARRAY = 0x8C1B;
	public const uint GL_TEXTURE_BINDING_1D_ARRAY = 0x8C1C;
	public const uint GL_TEXTURE_BINDING_2D_ARRAY = 0x8C1D;
	public const uint GL_MAX_GEOMETRY_TEXTURE_IMAGE_UNITS = 0x8C29;
	public const uint GL_TEXTURE_BUFFER = 0x8C2A;
	public const uint GL_TEXTURE_BUFFER_BINDING = 0x8C2A;
	public const uint GL_MAX_TEXTURE_BUFFER_SIZE = 0x8C2B;
	public const uint GL_TEXTURE_BINDING_BUFFER = 0x8C2C;
	public const uint GL_TEXTURE_BUFFER_DATA_STORE_BINDING = 0x8C2D;
	public const uint GL_ANY_SAMPLES_PASSED = 0x8C2F;
	public const uint GL_SAMPLE_SHADING = 0x8C36;
	public const uint GL_MIN_SAMPLE_SHADING_VALUE = 0x8C37;
	public const uint GL_R11F_G11F_B10F = 0x8C3A;
	public const uint GL_UNSIGNED_INT_10F_11F_11F_REV = 0x8C3B;
	public const uint GL_RGB9_E5 = 0x8C3D;
	public const uint GL_UNSIGNED_INT_5_9_9_9_REV = 0x8C3E;
	public const uint GL_TEXTURE_SHARED_SIZE = 0x8C3F;
	public const uint GL_SRGB = 0x8C40;
	public const uint GL_SRGB8 = 0x8C41;
	public const uint GL_SRGB_ALPHA = 0x8C42;
	public const uint GL_SRGB8_ALPHA8 = 0x8C43;
	public const uint GL_COMPRESSED_SRGB = 0x8C48;
	public const uint GL_COMPRESSED_SRGB_ALPHA = 0x8C49;
	public const uint GL_TRANSFORM_FEEDBACK_VARYING_MAX_LENGTH = 0x8C76;
	public const uint GL_TRANSFORM_FEEDBACK_BUFFER_MODE = 0x8C7F;
	public const uint GL_MAX_TRANSFORM_FEEDBACK_SEPARATE_COMPONENTS = 0x8C80;
	public const uint GL_TRANSFORM_FEEDBACK_VARYINGS = 0x8C83;
	public const uint GL_TRANSFORM_FEEDBACK_BUFFER_START = 0x8C84;
	public const uint GL_TRANSFORM_FEEDBACK_BUFFER_SIZE = 0x8C85;
	public const uint GL_PRIMITIVES_GENERATED = 0x8C87;
	public const uint GL_TRANSFORM_FEEDBACK_PRIMITIVES_WRITTEN = 0x8C88;
	public const uint GL_RASTERIZER_DISCARD = 0x8C89;
	public const uint GL_MAX_TRANSFORM_FEEDBACK_INTERLEAVED_COMPONENTS = 0x8C8A;
	public const uint GL_MAX_TRANSFORM_FEEDBACK_SEPARATE_ATTRIBS = 0x8C8B;
	public const uint GL_INTERLEAVED_ATTRIBS = 0x8C8C;
	public const uint GL_SEPARATE_ATTRIBS = 0x8C8D;
	public const uint GL_TRANSFORM_FEEDBACK_BUFFER = 0x8C8E;
	public const uint GL_TRANSFORM_FEEDBACK_BUFFER_BINDING = 0x8C8F;
	public const uint GL_POINT_SPRITE_COORD_ORIGIN = 0x8CA0;
	public const uint GL_LOWER_LEFT = 0x8CA1;
	public const uint GL_UPPER_LEFT = 0x8CA2;
	public const uint GL_STENCIL_BACK_REF = 0x8CA3;
	public const uint GL_STENCIL_BACK_VALUE_MASK = 0x8CA4;
	public const uint GL_STENCIL_BACK_WRITEMASK = 0x8CA5;
	public const uint GL_DRAW_FRAMEBUFFER_BINDING = 0x8CA6;
	public const uint GL_FRAMEBUFFER_BINDING = 0x8CA6;
	public const uint GL_RENDERBUFFER_BINDING = 0x8CA7;
	public const uint GL_READ_FRAMEBUFFER = 0x8CA8;
	public const uint GL_DRAW_FRAMEBUFFER = 0x8CA9;
	public const uint GL_READ_FRAMEBUFFER_BINDING = 0x8CAA;
	public const uint GL_RENDERBUFFER_SAMPLES = 0x8CAB;
	public const uint GL_DEPTH_COMPONENT32F = 0x8CAC;
	public const uint GL_DEPTH32F_STENCIL8 = 0x8CAD;
	public const uint GL_FRAMEBUFFER_ATTACHMENT_OBJECT_TYPE = 0x8CD0;
	public const uint GL_FRAMEBUFFER_ATTACHMENT_OBJECT_NAME = 0x8CD1;
	public const uint GL_FRAMEBUFFER_ATTACHMENT_TEXTURE_LEVEL = 0x8CD2;
	public const uint GL_FRAMEBUFFER_ATTACHMENT_TEXTURE_CUBE_MAP_FACE = 0x8CD3;
	public const uint GL_FRAMEBUFFER_ATTACHMENT_TEXTURE_LAYER = 0x8CD4;
	public const uint GL_FRAMEBUFFER_COMPLETE = 0x8CD5;
	public const uint GL_FRAMEBUFFER_INCOMPLETE_ATTACHMENT = 0x8CD6;
	public const uint GL_FRAMEBUFFER_INCOMPLETE_MISSING_ATTACHMENT = 0x8CD7;
	public const uint GL_FRAMEBUFFER_INCOMPLETE_DRAW_BUFFER = 0x8CDB;
	public const uint GL_FRAMEBUFFER_INCOMPLETE_READ_BUFFER = 0x8CDC;
	public const uint GL_FRAMEBUFFER_UNSUPPORTED = 0x8CDD;
	public const uint GL_MAX_COLOR_ATTACHMENTS = 0x8CDF;
	public const uint GL_COLOR_ATTACHMENT0 = 0x8CE0;
	public const uint GL_COLOR_ATTACHMENT1 = 0x8CE1;
	public const uint GL_COLOR_ATTACHMENT2 = 0x8CE2;
	public const uint GL_COLOR_ATTACHMENT3 = 0x8CE3;
	public const uint GL_COLOR_ATTACHMENT4 = 0x8CE4;
	public const uint GL_COLOR_ATTACHMENT5 = 0x8CE5;
	public const uint GL_COLOR_ATTACHMENT6 = 0x8CE6;
	public const uint GL_COLOR_ATTACHMENT7 = 0x8CE7;
	public const uint GL_COLOR_ATTACHMENT8 = 0x8CE8;
	public const uint GL_COLOR_ATTACHMENT9 = 0x8CE9;
	public const uint GL_COLOR_ATTACHMENT10 = 0x8CEA;
	public const uint GL_COLOR_ATTACHMENT11 = 0x8CEB;
	public const uint GL_COLOR_ATTACHMENT12 = 0x8CEC;
	public const uint GL_COLOR_ATTACHMENT13 = 0x8CED;
	public const uint GL_COLOR_ATTACHMENT14 = 0x8CEE;
	public const uint GL_COLOR_ATTACHMENT15 = 0x8CEF;
	public const uint GL_COLOR_ATTACHMENT16 = 0x8CF0;
	public const uint GL_COLOR_ATTACHMENT17 = 0x8CF1;
	public const uint GL_COLOR_ATTACHMENT18 = 0x8CF2;
	public const uint GL_COLOR_ATTACHMENT19 = 0x8CF3;
	public const uint GL_COLOR_ATTACHMENT20 = 0x8CF4;
	public const uint GL_COLOR_ATTACHMENT21 = 0x8CF5;
	public const uint GL_COLOR_ATTACHMENT22 = 0x8CF6;
	public const uint GL_COLOR_ATTACHMENT23 = 0x8CF7;
	public const uint GL_COLOR_ATTACHMENT24 = 0x8CF8;
	public const uint GL_COLOR_ATTACHMENT25 = 0x8CF9;
	public const uint GL_COLOR_ATTACHMENT26 = 0x8CFA;
	public const uint GL_COLOR_ATTACHMENT27 = 0x8CFB;
	public const uint GL_COLOR_ATTACHMENT28 = 0x8CFC;
	public const uint GL_COLOR_ATTACHMENT29 = 0x8CFD;
	public const uint GL_COLOR_ATTACHMENT30 = 0x8CFE;
	public const uint GL_COLOR_ATTACHMENT31 = 0x8CFF;
	public const uint GL_DEPTH_ATTACHMENT = 0x8D00;
	public const uint GL_STENCIL_ATTACHMENT = 0x8D20;
	public const uint GL_FRAMEBUFFER = 0x8D40;
	public const uint GL_RENDERBUFFER = 0x8D41;
	public const uint GL_RENDERBUFFER_WIDTH = 0x8D42;
	public const uint GL_RENDERBUFFER_HEIGHT = 0x8D43;
	public const uint GL_RENDERBUFFER_INTERNAL_FORMAT = 0x8D44;
	public const uint GL_STENCIL_INDEX1 = 0x8D46;
	public const uint GL_STENCIL_INDEX4 = 0x8D47;
	public const uint GL_STENCIL_INDEX8 = 0x8D48;
	public const uint GL_STENCIL_INDEX16 = 0x8D49;
	public const uint GL_RENDERBUFFER_RED_SIZE = 0x8D50;
	public const uint GL_RENDERBUFFER_GREEN_SIZE = 0x8D51;
	public const uint GL_RENDERBUFFER_BLUE_SIZE = 0x8D52;
	public const uint GL_RENDERBUFFER_ALPHA_SIZE = 0x8D53;
	public const uint GL_RENDERBUFFER_DEPTH_SIZE = 0x8D54;
	public const uint GL_RENDERBUFFER_STENCIL_SIZE = 0x8D55;
	public const uint GL_FRAMEBUFFER_INCOMPLETE_MULTISAMPLE = 0x8D56;
	public const uint GL_MAX_SAMPLES = 0x8D57;
	public const uint GL_RGB565 = 0x8D62;
	public const uint GL_PRIMITIVE_RESTART_FIXED_INDEX = 0x8D69;
	public const uint GL_ANY_SAMPLES_PASSED_CONSERVATIVE = 0x8D6A;
	public const uint GL_MAX_ELEMENT_INDEX = 0x8D6B;
	public const uint GL_RGBA32UI = 0x8D70;
	public const uint GL_RGB32UI = 0x8D71;
	public const uint GL_RGBA16UI = 0x8D76;
	public const uint GL_RGB16UI = 0x8D77;
	public const uint GL_RGBA8UI = 0x8D7C;
	public const uint GL_RGB8UI = 0x8D7D;
	public const uint GL_RGBA32I = 0x8D82;
	public const uint GL_RGB32I = 0x8D83;
	public const uint GL_RGBA16I = 0x8D88;
	public const uint GL_RGB16I = 0x8D89;
	public const uint GL_RGBA8I = 0x8D8E;
	public const uint GL_RGB8I = 0x8D8F;
	public const uint GL_RED_INTEGER = 0x8D94;
	public const uint GL_GREEN_INTEGER = 0x8D95;
	public const uint GL_BLUE_INTEGER = 0x8D96;
	public const uint GL_RGB_INTEGER = 0x8D98;
	public const uint GL_RGBA_INTEGER = 0x8D99;
	public const uint GL_BGR_INTEGER = 0x8D9A;
	public const uint GL_BGRA_INTEGER = 0x8D9B;
	public const uint GL_INT_2_10_10_10_REV = 0x8D9F;
	public const uint GL_FRAMEBUFFER_ATTACHMENT_LAYERED = 0x8DA7;
	public const uint GL_FRAMEBUFFER_INCOMPLETE_LAYER_TARGETS = 0x8DA8;
	public const uint GL_FLOAT_32_UNSIGNED_INT_24_8_REV = 0x8DAD;
	public const uint GL_FRAMEBUFFER_SRGB = 0x8DB9;
	public const uint GL_COMPRESSED_RED_RGTC1 = 0x8DBB;
	public const uint GL_COMPRESSED_SIGNED_RED_RGTC1 = 0x8DBC;
	public const uint GL_COMPRESSED_RG_RGTC2 = 0x8DBD;
	public const uint GL_COMPRESSED_SIGNED_RG_RGTC2 = 0x8DBE;
	public const uint GL_SAMPLER_1D_ARRAY = 0x8DC0;
	public const uint GL_SAMPLER_2D_ARRAY = 0x8DC1;
	public const uint GL_SAMPLER_BUFFER = 0x8DC2;
	public const uint GL_SAMPLER_1D_ARRAY_SHADOW = 0x8DC3;
	public const uint GL_SAMPLER_2D_ARRAY_SHADOW = 0x8DC4;
	public const uint GL_SAMPLER_CUBE_SHADOW = 0x8DC5;
	public const uint GL_UNSIGNED_INT_VEC2 = 0x8DC6;
	public const uint GL_UNSIGNED_INT_VEC3 = 0x8DC7;
	public const uint GL_UNSIGNED_INT_VEC4 = 0x8DC8;
	public const uint GL_INT_SAMPLER_1D = 0x8DC9;
	public const uint GL_INT_SAMPLER_2D = 0x8DCA;
	public const uint GL_INT_SAMPLER_3D = 0x8DCB;
	public const uint GL_INT_SAMPLER_CUBE = 0x8DCC;
	public const uint GL_INT_SAMPLER_2D_RECT = 0x8DCD;
	public const uint GL_INT_SAMPLER_1D_ARRAY = 0x8DCE;
	public const uint GL_INT_SAMPLER_2D_ARRAY = 0x8DCF;
	public const uint GL_INT_SAMPLER_BUFFER = 0x8DD0;
	public const uint GL_UNSIGNED_INT_SAMPLER_1D = 0x8DD1;
	public const uint GL_UNSIGNED_INT_SAMPLER_2D = 0x8DD2;
	public const uint GL_UNSIGNED_INT_SAMPLER_3D = 0x8DD3;
	public const uint GL_UNSIGNED_INT_SAMPLER_CUBE = 0x8DD4;
	public const uint GL_UNSIGNED_INT_SAMPLER_2D_RECT = 0x8DD5;
	public const uint GL_UNSIGNED_INT_SAMPLER_1D_ARRAY = 0x8DD6;
	public const uint GL_UNSIGNED_INT_SAMPLER_2D_ARRAY = 0x8DD7;
	public const uint GL_UNSIGNED_INT_SAMPLER_BUFFER = 0x8DD8;
	public const uint GL_GEOMETRY_SHADER = 0x8DD9;
	public const uint GL_MAX_GEOMETRY_UNIFORM_COMPONENTS = 0x8DDF;
	public const uint GL_MAX_GEOMETRY_OUTPUT_VERTICES = 0x8DE0;
	public const uint GL_MAX_GEOMETRY_TOTAL_OUTPUT_COMPONENTS = 0x8DE1;
	public const uint GL_ACTIVE_SUBROUTINES = 0x8DE5;
	public const uint GL_ACTIVE_SUBROUTINE_UNIFORMS = 0x8DE6;
	public const uint GL_MAX_SUBROUTINES = 0x8DE7;
	public const uint GL_MAX_SUBROUTINE_UNIFORM_LOCATIONS = 0x8DE8;
	public const uint GL_LOW_FLOAT = 0x8DF0;
	public const uint GL_MEDIUM_FLOAT = 0x8DF1;
	public const uint GL_HIGH_FLOAT = 0x8DF2;
	public const uint GL_LOW_INT = 0x8DF3;
	public const uint GL_MEDIUM_INT = 0x8DF4;
	public const uint GL_HIGH_INT = 0x8DF5;
	public const uint GL_SHADER_BINARY_FORMATS = 0x8DF8;
	public const uint GL_NUM_SHADER_BINARY_FORMATS = 0x8DF9;
	public const uint GL_SHADER_COMPILER = 0x8DFA;
	public const uint GL_MAX_VERTEX_UNIFORM_VECTORS = 0x8DFB;
	public const uint GL_MAX_VARYING_VECTORS = 0x8DFC;
	public const uint GL_MAX_FRAGMENT_UNIFORM_VECTORS = 0x8DFD;
	public const uint GL_QUERY_WAIT = 0x8E13;
	public const uint GL_QUERY_NO_WAIT = 0x8E14;
	public const uint GL_QUERY_BY_REGION_WAIT = 0x8E15;
	public const uint GL_QUERY_BY_REGION_NO_WAIT = 0x8E16;
	public const uint GL_QUERY_WAIT_INVERTED = 0x8E17;
	public const uint GL_QUERY_NO_WAIT_INVERTED = 0x8E18;
	public const uint GL_QUERY_BY_REGION_WAIT_INVERTED = 0x8E19;
	public const uint GL_QUERY_BY_REGION_NO_WAIT_INVERTED = 0x8E1A;
	public const uint GL_POLYGON_OFFSET_CLAMP = 0x8E1B;
	public const uint GL_MAX_COMBINED_TESS_CONTROL_UNIFORM_COMPONENTS = 0x8E1E;
	public const uint GL_MAX_COMBINED_TESS_EVALUATION_UNIFORM_COMPONENTS = 0x8E1F;
	public const uint GL_TRANSFORM_FEEDBACK = 0x8E22;
	public const uint GL_TRANSFORM_FEEDBACK_BUFFER_PAUSED = 0x8E23;
	public const uint GL_TRANSFORM_FEEDBACK_PAUSED = 0x8E23;
	public const uint GL_TRANSFORM_FEEDBACK_BUFFER_ACTIVE = 0x8E24;
	public const uint GL_TRANSFORM_FEEDBACK_ACTIVE = 0x8E24;
	public const uint GL_TRANSFORM_FEEDBACK_BINDING = 0x8E25;
	public const uint GL_TIMESTAMP = 0x8E28;
	public const uint GL_TEXTURE_SWIZZLE_R = 0x8E42;
	public const uint GL_TEXTURE_SWIZZLE_G = 0x8E43;
	public const uint GL_TEXTURE_SWIZZLE_B = 0x8E44;
	public const uint GL_TEXTURE_SWIZZLE_A = 0x8E45;
	public const uint GL_TEXTURE_SWIZZLE_RGBA = 0x8E46;
	public const uint GL_ACTIVE_SUBROUTINE_UNIFORM_LOCATIONS = 0x8E47;
	public const uint GL_ACTIVE_SUBROUTINE_MAX_LENGTH = 0x8E48;
	public const uint GL_ACTIVE_SUBROUTINE_UNIFORM_MAX_LENGTH = 0x8E49;
	public const uint GL_NUM_COMPATIBLE_SUBROUTINES = 0x8E4A;
	public const uint GL_COMPATIBLE_SUBROUTINES = 0x8E4B;
	public const uint GL_QUADS_FOLLOW_PROVOKING_VERTEX_CONVENTION = 0x8E4C;
	public const uint GL_FIRST_VERTEX_CONVENTION = 0x8E4D;
	public const uint GL_LAST_VERTEX_CONVENTION = 0x8E4E;
	public const uint GL_PROVOKING_VERTEX = 0x8E4F;
	public const uint GL_SAMPLE_POSITION = 0x8E50;
	public const uint GL_SAMPLE_MASK = 0x8E51;
	public const uint GL_SAMPLE_MASK_VALUE = 0x8E52;
	public const uint GL_MAX_SAMPLE_MASK_WORDS = 0x8E59;
	public const uint GL_MAX_GEOMETRY_SHADER_INVOCATIONS = 0x8E5A;
	public const uint GL_MIN_FRAGMENT_INTERPOLATION_OFFSET = 0x8E5B;
	public const uint GL_MAX_FRAGMENT_INTERPOLATION_OFFSET = 0x8E5C;
	public const uint GL_FRAGMENT_INTERPOLATION_OFFSET_BITS = 0x8E5D;
	public const uint GL_MIN_PROGRAM_TEXTURE_GATHER_OFFSET = 0x8E5E;
	public const uint GL_MAX_PROGRAM_TEXTURE_GATHER_OFFSET = 0x8E5F;
	public const uint GL_MAX_TRANSFORM_FEEDBACK_BUFFERS = 0x8E70;
	public const uint GL_MAX_VERTEX_STREAMS = 0x8E71;
	public const uint GL_PATCH_VERTICES = 0x8E72;
	public const uint GL_PATCH_DEFAULT_INNER_LEVEL = 0x8E73;
	public const uint GL_PATCH_DEFAULT_OUTER_LEVEL = 0x8E74;
	public const uint GL_TESS_CONTROL_OUTPUT_VERTICES = 0x8E75;
	public const uint GL_TESS_GEN_MODE = 0x8E76;
	public const uint GL_TESS_GEN_SPACING = 0x8E77;
	public const uint GL_TESS_GEN_VERTEX_ORDER = 0x8E78;
	public const uint GL_TESS_GEN_POINT_MODE = 0x8E79;
	public const uint GL_ISOLINES = 0x8E7A;
	public const uint GL_FRACTIONAL_ODD = 0x8E7B;
	public const uint GL_FRACTIONAL_EVEN = 0x8E7C;
	public const uint GL_MAX_PATCH_VERTICES = 0x8E7D;
	public const uint GL_MAX_TESS_GEN_LEVEL = 0x8E7E;
	public const uint GL_MAX_TESS_CONTROL_UNIFORM_COMPONENTS = 0x8E7F;
	public const uint GL_MAX_TESS_EVALUATION_UNIFORM_COMPONENTS = 0x8E80;
	public const uint GL_MAX_TESS_CONTROL_TEXTURE_IMAGE_UNITS = 0x8E81;
	public const uint GL_MAX_TESS_EVALUATION_TEXTURE_IMAGE_UNITS = 0x8E82;
	public const uint GL_MAX_TESS_CONTROL_OUTPUT_COMPONENTS = 0x8E83;
	public const uint GL_MAX_TESS_PATCH_COMPONENTS = 0x8E84;
	public const uint GL_MAX_TESS_CONTROL_TOTAL_OUTPUT_COMPONENTS = 0x8E85;
	public const uint GL_MAX_TESS_EVALUATION_OUTPUT_COMPONENTS = 0x8E86;
	public const uint GL_TESS_EVALUATION_SHADER = 0x8E87;
	public const uint GL_TESS_CONTROL_SHADER = 0x8E88;
	public const uint GL_MAX_TESS_CONTROL_UNIFORM_BLOCKS = 0x8E89;
	public const uint GL_MAX_TESS_EVALUATION_UNIFORM_BLOCKS = 0x8E8A;
	public const uint GL_COMPRESSED_RGBA_BPTC_UNORM = 0x8E8C;
	public const uint GL_COMPRESSED_SRGB_ALPHA_BPTC_UNORM = 0x8E8D;
	public const uint GL_COMPRESSED_RGB_BPTC_SIGNED_FLOAT = 0x8E8E;
	public const uint GL_COMPRESSED_RGB_BPTC_UNSIGNED_FLOAT = 0x8E8F;
	public const uint GL_COPY_READ_BUFFER = 0x8F36;
	public const uint GL_COPY_READ_BUFFER_BINDING = 0x8F36;
	public const uint GL_COPY_WRITE_BUFFER = 0x8F37;
	public const uint GL_COPY_WRITE_BUFFER_BINDING = 0x8F37;
	public const uint GL_MAX_IMAGE_UNITS = 0x8F38;
	public const uint GL_MAX_COMBINED_IMAGE_UNITS_AND_FRAGMENT_OUTPUTS = 0x8F39;
	public const uint GL_MAX_COMBINED_SHADER_OUTPUT_RESOURCES = 0x8F39;
	public const uint GL_IMAGE_BINDING_NAME = 0x8F3A;
	public const uint GL_IMAGE_BINDING_LEVEL = 0x8F3B;
	public const uint GL_IMAGE_BINDING_LAYERED = 0x8F3C;
	public const uint GL_IMAGE_BINDING_LAYER = 0x8F3D;
	public const uint GL_IMAGE_BINDING_ACCESS = 0x8F3E;
	public const uint GL_DRAW_INDIRECT_BUFFER = 0x8F3F;
	public const uint GL_DRAW_INDIRECT_BUFFER_BINDING = 0x8F43;
	public const uint GL_DOUBLE_MAT2 = 0x8F46;
	public const uint GL_DOUBLE_MAT3 = 0x8F47;
	public const uint GL_DOUBLE_MAT4 = 0x8F48;
	public const uint GL_DOUBLE_MAT2x3 = 0x8F49;
	public const uint GL_DOUBLE_MAT2x4 = 0x8F4A;
	public const uint GL_DOUBLE_MAT3x2 = 0x8F4B;
	public const uint GL_DOUBLE_MAT3x4 = 0x8F4C;
	public const uint GL_DOUBLE_MAT4x2 = 0x8F4D;
	public const uint GL_DOUBLE_MAT4x3 = 0x8F4E;
	public const uint GL_VERTEX_BINDING_BUFFER = 0x8F4F;
	public const uint GL_R8_SNORM = 0x8F94;
	public const uint GL_RG8_SNORM = 0x8F95;
	public const uint GL_RGB8_SNORM = 0x8F96;
	public const uint GL_RGBA8_SNORM = 0x8F97;
	public const uint GL_R16_SNORM = 0x8F98;
	public const uint GL_RG16_SNORM = 0x8F99;
	public const uint GL_RGB16_SNORM = 0x8F9A;
	public const uint GL_RGBA16_SNORM = 0x8F9B;
	public const uint GL_SIGNED_NORMALIZED = 0x8F9C;
	public const uint GL_PRIMITIVE_RESTART = 0x8F9D;
	public const uint GL_PRIMITIVE_RESTART_INDEX = 0x8F9E;
	public const uint GL_DOUBLE_VEC2 = 0x8FFC;
	public const uint GL_DOUBLE_VEC3 = 0x8FFD;
	public const uint GL_DOUBLE_VEC4 = 0x8FFE;
	public const uint GL_TEXTURE_CUBE_MAP_ARRAY = 0x9009;
	public const uint GL_TEXTURE_BINDING_CUBE_MAP_ARRAY = 0x900A;
	public const uint GL_PROXY_TEXTURE_CUBE_MAP_ARRAY = 0x900B;
	public const uint GL_SAMPLER_CUBE_MAP_ARRAY = 0x900C;
	public const uint GL_SAMPLER_CUBE_MAP_ARRAY_SHADOW = 0x900D;
	public const uint GL_INT_SAMPLER_CUBE_MAP_ARRAY = 0x900E;
	public const uint GL_UNSIGNED_INT_SAMPLER_CUBE_MAP_ARRAY = 0x900F;
	public const uint GL_IMAGE_1D = 0x904C;
	public const uint GL_IMAGE_2D = 0x904D;
	public const uint GL_IMAGE_3D = 0x904E;
	public const uint GL_IMAGE_2D_RECT = 0x904F;
	public const uint GL_IMAGE_CUBE = 0x9050;
	public const uint GL_IMAGE_BUFFER = 0x9051;
	public const uint GL_IMAGE_1D_ARRAY = 0x9052;
	public const uint GL_IMAGE_2D_ARRAY = 0x9053;
	public const uint GL_IMAGE_CUBE_MAP_ARRAY = 0x9054;
	public const uint GL_IMAGE_2D_MULTISAMPLE = 0x9055;
	public const uint GL_IMAGE_2D_MULTISAMPLE_ARRAY = 0x9056;
	public const uint GL_INT_IMAGE_1D = 0x9057;
	public const uint GL_INT_IMAGE_2D = 0x9058;
	public const uint GL_INT_IMAGE_3D = 0x9059;
	public const uint GL_INT_IMAGE_2D_RECT = 0x905A;
	public const uint GL_INT_IMAGE_CUBE = 0x905B;
	public const uint GL_INT_IMAGE_BUFFER = 0x905C;
	public const uint GL_INT_IMAGE_1D_ARRAY = 0x905D;
	public const uint GL_INT_IMAGE_2D_ARRAY = 0x905E;
	public const uint GL_INT_IMAGE_CUBE_MAP_ARRAY = 0x905F;
	public const uint GL_INT_IMAGE_2D_MULTISAMPLE = 0x9060;
	public const uint GL_INT_IMAGE_2D_MULTISAMPLE_ARRAY = 0x9061;
	public const uint GL_UNSIGNED_INT_IMAGE_1D = 0x9062;
	public const uint GL_UNSIGNED_INT_IMAGE_2D = 0x9063;
	public const uint GL_UNSIGNED_INT_IMAGE_3D = 0x9064;
	public const uint GL_UNSIGNED_INT_IMAGE_2D_RECT = 0x9065;
	public const uint GL_UNSIGNED_INT_IMAGE_CUBE = 0x9066;
	public const uint GL_UNSIGNED_INT_IMAGE_BUFFER = 0x9067;
	public const uint GL_UNSIGNED_INT_IMAGE_1D_ARRAY = 0x9068;
	public const uint GL_UNSIGNED_INT_IMAGE_2D_ARRAY = 0x9069;
	public const uint GL_UNSIGNED_INT_IMAGE_CUBE_MAP_ARRAY = 0x906A;
	public const uint GL_UNSIGNED_INT_IMAGE_2D_MULTISAMPLE = 0x906B;
	public const uint GL_UNSIGNED_INT_IMAGE_2D_MULTISAMPLE_ARRAY = 0x906C;
	public const uint GL_MAX_IMAGE_SAMPLES = 0x906D;
	public const uint GL_IMAGE_BINDING_FORMAT = 0x906E;
	public const uint GL_RGB10_A2UI = 0x906F;
	public const uint GL_MIN_MAP_BUFFER_ALIGNMENT = 0x90BC;
	public const uint GL_IMAGE_FORMAT_COMPATIBILITY_TYPE = 0x90C7;
	public const uint GL_IMAGE_FORMAT_COMPATIBILITY_BY_SIZE = 0x90C8;
	public const uint GL_IMAGE_FORMAT_COMPATIBILITY_BY_CLASS = 0x90C9;
	public const uint GL_MAX_VERTEX_IMAGE_UNIFORMS = 0x90CA;
	public const uint GL_MAX_TESS_CONTROL_IMAGE_UNIFORMS = 0x90CB;
	public const uint GL_MAX_TESS_EVALUATION_IMAGE_UNIFORMS = 0x90CC;
	public const uint GL_MAX_GEOMETRY_IMAGE_UNIFORMS = 0x90CD;
	public const uint GL_MAX_FRAGMENT_IMAGE_UNIFORMS = 0x90CE;
	public const uint GL_MAX_COMBINED_IMAGE_UNIFORMS = 0x90CF;
	public const uint GL_SHADER_STORAGE_BUFFER = 0x90D2;
	public const uint GL_SHADER_STORAGE_BUFFER_BINDING = 0x90D3;
	public const uint GL_SHADER_STORAGE_BUFFER_START = 0x90D4;
	public const uint GL_SHADER_STORAGE_BUFFER_SIZE = 0x90D5;
	public const uint GL_MAX_VERTEX_SHADER_STORAGE_BLOCKS = 0x90D6;
	public const uint GL_MAX_GEOMETRY_SHADER_STORAGE_BLOCKS = 0x90D7;
	public const uint GL_MAX_TESS_CONTROL_SHADER_STORAGE_BLOCKS = 0x90D8;
	public const uint GL_MAX_TESS_EVALUATION_SHADER_STORAGE_BLOCKS = 0x90D9;
	public const uint GL_MAX_FRAGMENT_SHADER_STORAGE_BLOCKS = 0x90DA;
	public const uint GL_MAX_COMPUTE_SHADER_STORAGE_BLOCKS = 0x90DB;
	public const uint GL_MAX_COMBINED_SHADER_STORAGE_BLOCKS = 0x90DC;
	public const uint GL_MAX_SHADER_STORAGE_BUFFER_BINDINGS = 0x90DD;
	public const uint GL_MAX_SHADER_STORAGE_BLOCK_SIZE = 0x90DE;
	public const uint GL_SHADER_STORAGE_BUFFER_OFFSET_ALIGNMENT = 0x90DF;
	public const uint GL_DEPTH_STENCIL_TEXTURE_MODE = 0x90EA;
	public const uint GL_MAX_COMPUTE_WORK_GROUP_INVOCATIONS = 0x90EB;
	public const uint GL_UNIFORM_BLOCK_REFERENCED_BY_COMPUTE_SHADER = 0x90EC;
	public const uint GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_COMPUTE_SHADER = 0x90ED;
	public const uint GL_DISPATCH_INDIRECT_BUFFER = 0x90EE;
	public const uint GL_DISPATCH_INDIRECT_BUFFER_BINDING = 0x90EF;
	public const uint GL_TEXTURE_2D_MULTISAMPLE = 0x9100;
	public const uint GL_PROXY_TEXTURE_2D_MULTISAMPLE = 0x9101;
	public const uint GL_TEXTURE_2D_MULTISAMPLE_ARRAY = 0x9102;
	public const uint GL_PROXY_TEXTURE_2D_MULTISAMPLE_ARRAY = 0x9103;
	public const uint GL_TEXTURE_BINDING_2D_MULTISAMPLE = 0x9104;
	public const uint GL_TEXTURE_BINDING_2D_MULTISAMPLE_ARRAY = 0x9105;
	public const uint GL_TEXTURE_SAMPLES = 0x9106;
	public const uint GL_TEXTURE_FIXED_SAMPLE_LOCATIONS = 0x9107;
	public const uint GL_SAMPLER_2D_MULTISAMPLE = 0x9108;
	public const uint GL_INT_SAMPLER_2D_MULTISAMPLE = 0x9109;
	public const uint GL_UNSIGNED_INT_SAMPLER_2D_MULTISAMPLE = 0x910A;
	public const uint GL_SAMPLER_2D_MULTISAMPLE_ARRAY = 0x910B;
	public const uint GL_INT_SAMPLER_2D_MULTISAMPLE_ARRAY = 0x910C;
	public const uint GL_UNSIGNED_INT_SAMPLER_2D_MULTISAMPLE_ARRAY = 0x910D;
	public const uint GL_MAX_COLOR_TEXTURE_SAMPLES = 0x910E;
	public const uint GL_MAX_DEPTH_TEXTURE_SAMPLES = 0x910F;
	public const uint GL_MAX_INTEGER_SAMPLES = 0x9110;
	public const uint GL_MAX_SERVER_WAIT_TIMEOUT = 0x9111;
	public const uint GL_OBJECT_TYPE = 0x9112;
	public const uint GL_SYNC_CONDITION = 0x9113;
	public const uint GL_SYNC_STATUS = 0x9114;
	public const uint GL_SYNC_FLAGS = 0x9115;
	public const uint GL_SYNC_FENCE = 0x9116;
	public const uint GL_SYNC_GPU_COMMANDS_COMPLETE = 0x9117;
	public const uint GL_UNSIGNALED = 0x9118;
	public const uint GL_SIGNALED = 0x9119;
	public const uint GL_ALREADY_SIGNALED = 0x911A;
	public const uint GL_TIMEOUT_EXPIRED = 0x911B;
	public const uint GL_CONDITION_SATISFIED = 0x911C;
	public const uint GL_WAIT_FAILED = 0x911D;
	public const uint GL_BUFFER_ACCESS_FLAGS = 0x911F;
	public const uint GL_BUFFER_MAP_LENGTH = 0x9120;
	public const uint GL_BUFFER_MAP_OFFSET = 0x9121;
	public const uint GL_MAX_VERTEX_OUTPUT_COMPONENTS = 0x9122;
	public const uint GL_MAX_GEOMETRY_INPUT_COMPONENTS = 0x9123;
	public const uint GL_MAX_GEOMETRY_OUTPUT_COMPONENTS = 0x9124;
	public const uint GL_MAX_FRAGMENT_INPUT_COMPONENTS = 0x9125;
	public const uint GL_CONTEXT_PROFILE_MASK = 0x9126;
	public const uint GL_UNPACK_COMPRESSED_BLOCK_WIDTH = 0x9127;
	public const uint GL_UNPACK_COMPRESSED_BLOCK_HEIGHT = 0x9128;
	public const uint GL_UNPACK_COMPRESSED_BLOCK_DEPTH = 0x9129;
	public const uint GL_UNPACK_COMPRESSED_BLOCK_SIZE = 0x912A;
	public const uint GL_PACK_COMPRESSED_BLOCK_WIDTH = 0x912B;
	public const uint GL_PACK_COMPRESSED_BLOCK_HEIGHT = 0x912C;
	public const uint GL_PACK_COMPRESSED_BLOCK_DEPTH = 0x912D;
	public const uint GL_PACK_COMPRESSED_BLOCK_SIZE = 0x912E;
	public const uint GL_TEXTURE_IMMUTABLE_FORMAT = 0x912F;
	public const uint GL_MAX_DEBUG_MESSAGE_LENGTH = 0x9143;
	public const uint GL_MAX_DEBUG_LOGGED_MESSAGES = 0x9144;
	public const uint GL_DEBUG_LOGGED_MESSAGES = 0x9145;
	public const uint GL_DEBUG_SEVERITY_HIGH = 0x9146;
	public const uint GL_DEBUG_SEVERITY_MEDIUM = 0x9147;
	public const uint GL_DEBUG_SEVERITY_LOW = 0x9148;
	public const uint GL_QUERY_BUFFER = 0x9192;
	public const uint GL_QUERY_BUFFER_BINDING = 0x9193;
	public const uint GL_QUERY_RESULT_NO_WAIT = 0x9194;
	public const uint GL_TEXTURE_BUFFER_OFFSET = 0x919D;
	public const uint GL_TEXTURE_BUFFER_SIZE = 0x919E;
	public const uint GL_TEXTURE_BUFFER_OFFSET_ALIGNMENT = 0x919F;
	public const uint GL_COMPUTE_SHADER = 0x91B9;
	public const uint GL_MAX_COMPUTE_UNIFORM_BLOCKS = 0x91BB;
	public const uint GL_MAX_COMPUTE_TEXTURE_IMAGE_UNITS = 0x91BC;
	public const uint GL_MAX_COMPUTE_IMAGE_UNIFORMS = 0x91BD;
	public const uint GL_MAX_COMPUTE_WORK_GROUP_COUNT = 0x91BE;
	public const uint GL_MAX_COMPUTE_WORK_GROUP_SIZE = 0x91BF;
	public const uint GL_COMPRESSED_R11_EAC = 0x9270;
	public const uint GL_COMPRESSED_SIGNED_R11_EAC = 0x9271;
	public const uint GL_COMPRESSED_RG11_EAC = 0x9272;
	public const uint GL_COMPRESSED_SIGNED_RG11_EAC = 0x9273;
	public const uint GL_COMPRESSED_RGB8_ETC2 = 0x9274;
	public const uint GL_COMPRESSED_SRGB8_ETC2 = 0x9275;
	public const uint GL_COMPRESSED_RGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9276;
	public const uint GL_COMPRESSED_SRGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9277;
	public const uint GL_COMPRESSED_RGBA8_ETC2_EAC = 0x9278;
	public const uint GL_COMPRESSED_SRGB8_ALPHA8_ETC2_EAC = 0x9279;
	public const uint GL_ATOMIC_COUNTER_BUFFER = 0x92C0;
	public const uint GL_ATOMIC_COUNTER_BUFFER_BINDING = 0x92C1;
	public const uint GL_ATOMIC_COUNTER_BUFFER_START = 0x92C2;
	public const uint GL_ATOMIC_COUNTER_BUFFER_SIZE = 0x92C3;
	public const uint GL_ATOMIC_COUNTER_BUFFER_DATA_SIZE = 0x92C4;
	public const uint GL_ATOMIC_COUNTER_BUFFER_ACTIVE_ATOMIC_COUNTERS = 0x92C5;
	public const uint GL_ATOMIC_COUNTER_BUFFER_ACTIVE_ATOMIC_COUNTER_INDICES = 0x92C6;
	public const uint GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_VERTEX_SHADER = 0x92C7;
	public const uint GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_TESS_CONTROL_SHADER = 0x92C8;
	public const uint GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_TESS_EVALUATION_SHADER = 0x92C9;
	public const uint GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_GEOMETRY_SHADER = 0x92CA;
	public const uint GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_FRAGMENT_SHADER = 0x92CB;
	public const uint GL_MAX_VERTEX_ATOMIC_COUNTER_BUFFERS = 0x92CC;
	public const uint GL_MAX_TESS_CONTROL_ATOMIC_COUNTER_BUFFERS = 0x92CD;
	public const uint GL_MAX_TESS_EVALUATION_ATOMIC_COUNTER_BUFFERS = 0x92CE;
	public const uint GL_MAX_GEOMETRY_ATOMIC_COUNTER_BUFFERS = 0x92CF;
	public const uint GL_MAX_FRAGMENT_ATOMIC_COUNTER_BUFFERS = 0x92D0;
	public const uint GL_MAX_COMBINED_ATOMIC_COUNTER_BUFFERS = 0x92D1;
	public const uint GL_MAX_VERTEX_ATOMIC_COUNTERS = 0x92D2;
	public const uint GL_MAX_TESS_CONTROL_ATOMIC_COUNTERS = 0x92D3;
	public const uint GL_MAX_TESS_EVALUATION_ATOMIC_COUNTERS = 0x92D4;
	public const uint GL_MAX_GEOMETRY_ATOMIC_COUNTERS = 0x92D5;
	public const uint GL_MAX_FRAGMENT_ATOMIC_COUNTERS = 0x92D6;
	public const uint GL_MAX_COMBINED_ATOMIC_COUNTERS = 0x92D7;
	public const uint GL_MAX_ATOMIC_COUNTER_BUFFER_SIZE = 0x92D8;
	public const uint GL_ACTIVE_ATOMIC_COUNTER_BUFFERS = 0x92D9;
	public const uint GL_UNIFORM_ATOMIC_COUNTER_BUFFER_INDEX = 0x92DA;
	public const uint GL_UNSIGNED_INT_ATOMIC_COUNTER = 0x92DB;
	public const uint GL_MAX_ATOMIC_COUNTER_BUFFER_BINDINGS = 0x92DC;
	public const uint GL_DEBUG_OUTPUT = 0x92E0;
	public const uint GL_UNIFORM = 0x92E1;
	public const uint GL_UNIFORM_BLOCK = 0x92E2;
	public const uint GL_PROGRAM_INPUT = 0x92E3;
	public const uint GL_PROGRAM_OUTPUT = 0x92E4;
	public const uint GL_BUFFER_VARIABLE = 0x92E5;
	public const uint GL_SHADER_STORAGE_BLOCK = 0x92E6;
	public const uint GL_IS_PER_PATCH = 0x92E7;
	public const uint GL_VERTEX_SUBROUTINE = 0x92E8;
	public const uint GL_TESS_CONTROL_SUBROUTINE = 0x92E9;
	public const uint GL_TESS_EVALUATION_SUBROUTINE = 0x92EA;
	public const uint GL_GEOMETRY_SUBROUTINE = 0x92EB;
	public const uint GL_FRAGMENT_SUBROUTINE = 0x92EC;
	public const uint GL_COMPUTE_SUBROUTINE = 0x92ED;
	public const uint GL_VERTEX_SUBROUTINE_UNIFORM = 0x92EE;
	public const uint GL_TESS_CONTROL_SUBROUTINE_UNIFORM = 0x92EF;
	public const uint GL_TESS_EVALUATION_SUBROUTINE_UNIFORM = 0x92F0;
	public const uint GL_GEOMETRY_SUBROUTINE_UNIFORM = 0x92F1;
	public const uint GL_FRAGMENT_SUBROUTINE_UNIFORM = 0x92F2;
	public const uint GL_COMPUTE_SUBROUTINE_UNIFORM = 0x92F3;
	public const uint GL_TRANSFORM_FEEDBACK_VARYING = 0x92F4;
	public const uint GL_ACTIVE_RESOURCES = 0x92F5;
	public const uint GL_MAX_NAME_LENGTH = 0x92F6;
	public const uint GL_MAX_NUM_ACTIVE_VARIABLES = 0x92F7;
	public const uint GL_MAX_NUM_COMPATIBLE_SUBROUTINES = 0x92F8;
	public const uint GL_NAME_LENGTH = 0x92F9;
	public const uint GL_TYPE = 0x92FA;
	public const uint GL_ARRAY_SIZE = 0x92FB;
	public const uint GL_OFFSET = 0x92FC;
	public const uint GL_BLOCK_INDEX = 0x92FD;
	public const uint GL_ARRAY_STRIDE = 0x92FE;
	public const uint GL_MATRIX_STRIDE = 0x92FF;
	public const uint GL_IS_ROW_MAJOR = 0x9300;
	public const uint GL_ATOMIC_COUNTER_BUFFER_INDEX = 0x9301;
	public const uint GL_BUFFER_BINDING = 0x9302;
	public const uint GL_BUFFER_DATA_SIZE = 0x9303;
	public const uint GL_NUM_ACTIVE_VARIABLES = 0x9304;
	public const uint GL_ACTIVE_VARIABLES = 0x9305;
	public const uint GL_REFERENCED_BY_VERTEX_SHADER = 0x9306;
	public const uint GL_REFERENCED_BY_TESS_CONTROL_SHADER = 0x9307;
	public const uint GL_REFERENCED_BY_TESS_EVALUATION_SHADER = 0x9308;
	public const uint GL_REFERENCED_BY_GEOMETRY_SHADER = 0x9309;
	public const uint GL_REFERENCED_BY_FRAGMENT_SHADER = 0x930A;
	public const uint GL_REFERENCED_BY_COMPUTE_SHADER = 0x930B;
	public const uint GL_TOP_LEVEL_ARRAY_SIZE = 0x930C;
	public const uint GL_TOP_LEVEL_ARRAY_STRIDE = 0x930D;
	public const uint GL_LOCATION = 0x930E;
	public const uint GL_LOCATION_INDEX = 0x930F;
	public const uint GL_FRAMEBUFFER_DEFAULT_WIDTH = 0x9310;
	public const uint GL_FRAMEBUFFER_DEFAULT_HEIGHT = 0x9311;
	public const uint GL_FRAMEBUFFER_DEFAULT_LAYERS = 0x9312;
	public const uint GL_FRAMEBUFFER_DEFAULT_SAMPLES = 0x9313;
	public const uint GL_FRAMEBUFFER_DEFAULT_FIXED_SAMPLE_LOCATIONS = 0x9314;
	public const uint GL_MAX_FRAMEBUFFER_WIDTH = 0x9315;
	public const uint GL_MAX_FRAMEBUFFER_HEIGHT = 0x9316;
	public const uint GL_MAX_FRAMEBUFFER_LAYERS = 0x9317;
	public const uint GL_MAX_FRAMEBUFFER_SAMPLES = 0x9318;
	public const uint GL_LOCATION_COMPONENT = 0x934A;
	public const uint GL_TRANSFORM_FEEDBACK_BUFFER_INDEX = 0x934B;
	public const uint GL_TRANSFORM_FEEDBACK_BUFFER_STRIDE = 0x934C;
	public const uint GL_CLIP_ORIGIN = 0x935C;
	public const uint GL_CLIP_DEPTH_MODE = 0x935D;
	public const uint GL_NEGATIVE_ONE_TO_ONE = 0x935E;
	public const uint GL_ZERO_TO_ONE = 0x935F;
	public const uint GL_CLEAR_TEXTURE = 0x9365;
	public const uint GL_NUM_SAMPLE_COUNTS = 0x9380;
	public const uint GL_SHADER_BINARY_FORMAT_SPIR_V = 0x9551;
	public const uint GL_SPIR_V_BINARY = 0x9552;
	public const uint GL_SPIR_V_EXTENSIONS = 0x9553;
	public const uint GL_NUM_SPIR_V_EXTENSIONS = 0x9554;

	private static void* s_glActiveShaderProgram;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glActiveShaderProgram(uint pipeline, uint program) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glActiveShaderProgram)(pipeline, program);

	private static void* s_glActiveTexture;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glActiveTexture(uint texture) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glActiveTexture)(texture);

	private static void* s_glAttachShader;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glAttachShader(uint program, uint shader) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glAttachShader)(program, shader);

	private static void* s_glBeginConditionalRender;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBeginConditionalRender(uint id, uint mode) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glBeginConditionalRender)(id, mode);

	private static void* s_glBeginQuery;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBeginQuery(uint target, uint id) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glBeginQuery)(target, id);

	private static void* s_glBeginQueryIndexed;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBeginQueryIndexed(uint target, uint index, uint id) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glBeginQueryIndexed)(target, index, id);

	private static void* s_glBeginTransformFeedback;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBeginTransformFeedback(uint primitiveMode) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glBeginTransformFeedback)(primitiveMode);

	private static void* s_glBindAttribLocation;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindAttribLocation(uint program, uint index, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, byte*, void>)s_glBindAttribLocation)(program, index, name);

	private static void* s_glBindBuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindBuffer(uint target, uint buffer) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glBindBuffer)(target, buffer);

	private static void* s_glBindBufferBase;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindBufferBase(uint target, uint index, uint buffer) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glBindBufferBase)(target, index, buffer);

	private static void* s_glBindBufferRange;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindBufferRange(uint target, uint index, uint buffer, nint offset, nint size) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, nint, nint, void>)s_glBindBufferRange)(target, index, buffer, offset, size);

	private static void* s_glBindBuffersBase;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindBuffersBase(uint target, uint first, int count, uint* buffers) => ((delegate* unmanaged[Cdecl]<uint, uint, int, uint*, void>)s_glBindBuffersBase)(target, first, count, buffers);

	private static void* s_glBindBuffersRange;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindBuffersRange(uint target, uint first, int count, uint* buffers, nint* offsets, nint* sizes) => ((delegate* unmanaged[Cdecl]<uint, uint, int, uint*, nint*, nint*, void>)s_glBindBuffersRange)(target, first, count, buffers, offsets, sizes);

	private static void* s_glBindFragDataLocation;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindFragDataLocation(uint program, uint color, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, byte*, void>)s_glBindFragDataLocation)(program, color, name);

	private static void* s_glBindFragDataLocationIndexed;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindFragDataLocationIndexed(uint program, uint colorNumber, uint index, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, byte*, void>)s_glBindFragDataLocationIndexed)(program, colorNumber, index, name);

	private static void* s_glBindFramebuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindFramebuffer(uint target, uint framebuffer) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glBindFramebuffer)(target, framebuffer);

	private static void* s_glBindImageTexture;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindImageTexture(uint unit, uint texture, int level, GLboolean layered, int layer, uint access, uint format) => ((delegate* unmanaged[Cdecl]<uint, uint, int, GLboolean, int, uint, uint, void>)s_glBindImageTexture)(unit, texture, level, layered, layer, access, format);

	private static void* s_glBindImageTextures;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindImageTextures(uint first, int count, uint* textures) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glBindImageTextures)(first, count, textures);

	private static void* s_glBindProgramPipeline;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindProgramPipeline(uint pipeline) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glBindProgramPipeline)(pipeline);

	private static void* s_glBindRenderbuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindRenderbuffer(uint target, uint renderbuffer) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glBindRenderbuffer)(target, renderbuffer);

	private static void* s_glBindSampler;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindSampler(uint unit, uint sampler) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glBindSampler)(unit, sampler);

	private static void* s_glBindSamplers;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindSamplers(uint first, int count, uint* samplers) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glBindSamplers)(first, count, samplers);

	private static void* s_glBindTexture;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindTexture(uint target, uint texture) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glBindTexture)(target, texture);

	private static void* s_glBindTextureUnit;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindTextureUnit(uint unit, uint texture) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glBindTextureUnit)(unit, texture);

	private static void* s_glBindTextures;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindTextures(uint first, int count, uint* textures) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glBindTextures)(first, count, textures);

	private static void* s_glBindTransformFeedback;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindTransformFeedback(uint target, uint id) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glBindTransformFeedback)(target, id);

	private static void* s_glBindVertexArray;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindVertexArray(uint array) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glBindVertexArray)(array);

	private static void* s_glBindVertexBuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindVertexBuffer(uint bindingindex, uint buffer, nint offset, int stride) => ((delegate* unmanaged[Cdecl]<uint, uint, nint, int, void>)s_glBindVertexBuffer)(bindingindex, buffer, offset, stride);

	private static void* s_glBindVertexBuffers;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBindVertexBuffers(uint first, int count, uint* buffers, nint* offsets, int* strides) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, nint*, int*, void>)s_glBindVertexBuffers)(first, count, buffers, offsets, strides);

	private static void* s_glBlendColor;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBlendColor(float red, float green, float blue, float alpha) => ((delegate* unmanaged[Cdecl]<float, float, float, float, void>)s_glBlendColor)(red, green, blue, alpha);

	private static void* s_glBlendEquation;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBlendEquation(uint mode) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glBlendEquation)(mode);

	private static void* s_glBlendEquationSeparate;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBlendEquationSeparate(uint modeRGB, uint modeAlpha) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glBlendEquationSeparate)(modeRGB, modeAlpha);

	private static void* s_glBlendEquationSeparatei;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBlendEquationSeparatei(uint buf, uint modeRGB, uint modeAlpha) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glBlendEquationSeparatei)(buf, modeRGB, modeAlpha);

	private static void* s_glBlendEquationi;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBlendEquationi(uint buf, uint mode) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glBlendEquationi)(buf, mode);

	private static void* s_glBlendFunc;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBlendFunc(uint sfactor, uint dfactor) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glBlendFunc)(sfactor, dfactor);

	private static void* s_glBlendFuncSeparate;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBlendFuncSeparate(uint sfactorRGB, uint dfactorRGB, uint sfactorAlpha, uint dfactorAlpha) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, uint, void>)s_glBlendFuncSeparate)(sfactorRGB, dfactorRGB, sfactorAlpha, dfactorAlpha);

	private static void* s_glBlendFuncSeparatei;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBlendFuncSeparatei(uint buf, uint srcRGB, uint dstRGB, uint srcAlpha, uint dstAlpha) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, uint, uint, void>)s_glBlendFuncSeparatei)(buf, srcRGB, dstRGB, srcAlpha, dstAlpha);

	private static void* s_glBlendFunci;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBlendFunci(uint buf, uint src, uint dst) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glBlendFunci)(buf, src, dst);

	private static void* s_glBlitFramebuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, uint mask, uint filter) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, int, uint, uint, void>)s_glBlitFramebuffer)(srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter);

	private static void* s_glBlitNamedFramebuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBlitNamedFramebuffer(uint readFramebuffer, uint drawFramebuffer, int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, uint mask, uint filter) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int, int, int, int, int, int, int, uint, uint, void>)s_glBlitNamedFramebuffer)(readFramebuffer, drawFramebuffer, srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter);

	private static void* s_glBufferData;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBufferData(uint target, nint size, void* data, uint usage) => ((delegate* unmanaged[Cdecl]<uint, nint, void*, uint, void>)s_glBufferData)(target, size, data, usage);

	private static void* s_glBufferStorage;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBufferStorage(uint target, nint size, void* data, uint flags) => ((delegate* unmanaged[Cdecl]<uint, nint, void*, uint, void>)s_glBufferStorage)(target, size, data, flags);

	private static void* s_glBufferSubData;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glBufferSubData(uint target, nint offset, nint size, void* data) => ((delegate* unmanaged[Cdecl]<uint, nint, nint, void*, void>)s_glBufferSubData)(target, offset, size, data);

	private static void* s_glCheckFramebufferStatus;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static uint glCheckFramebufferStatus(uint target) => ((delegate* unmanaged[Cdecl]<uint, uint>)s_glCheckFramebufferStatus)(target);

	private static void* s_glCheckNamedFramebufferStatus;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static uint glCheckNamedFramebufferStatus(uint framebuffer, uint target) => ((delegate* unmanaged[Cdecl]<uint, uint, uint>)s_glCheckNamedFramebufferStatus)(framebuffer, target);

	private static void* s_glClampColor;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClampColor(uint target, uint clamp) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glClampColor)(target, clamp);

	private static void* s_glClear;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClear(uint mask) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glClear)(mask);

	private static void* s_glClearBufferData;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearBufferData(uint target, uint internalformat, uint format, uint type, void* data) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, uint, void*, void>)s_glClearBufferData)(target, internalformat, format, type, data);

	private static void* s_glClearBufferSubData;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearBufferSubData(uint target, uint internalformat, nint offset, nint size, uint format, uint type, void* data) => ((delegate* unmanaged[Cdecl]<uint, uint, nint, nint, uint, uint, void*, void>)s_glClearBufferSubData)(target, internalformat, offset, size, format, type, data);

	private static void* s_glClearBufferfi;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearBufferfi(uint buffer, int drawbuffer, float depth, int stencil) => ((delegate* unmanaged[Cdecl]<uint, int, float, int, void>)s_glClearBufferfi)(buffer, drawbuffer, depth, stencil);

	private static void* s_glClearBufferfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearBufferfv(uint buffer, int drawbuffer, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, float*, void>)s_glClearBufferfv)(buffer, drawbuffer, value);

	private static void* s_glClearBufferiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearBufferiv(uint buffer, int drawbuffer, int* value) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glClearBufferiv)(buffer, drawbuffer, value);

	private static void* s_glClearBufferuiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearBufferuiv(uint buffer, int drawbuffer, uint* value) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glClearBufferuiv)(buffer, drawbuffer, value);

	private static void* s_glClearColor;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearColor(float red, float green, float blue, float alpha) => ((delegate* unmanaged[Cdecl]<float, float, float, float, void>)s_glClearColor)(red, green, blue, alpha);

	private static void* s_glClearDepth;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearDepth(double depth) => ((delegate* unmanaged[Cdecl]<double, void>)s_glClearDepth)(depth);

	private static void* s_glClearDepthf;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearDepthf(float d) => ((delegate* unmanaged[Cdecl]<float, void>)s_glClearDepthf)(d);

	private static void* s_glClearNamedBufferData;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearNamedBufferData(uint buffer, uint internalformat, uint format, uint type, void* data) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, uint, void*, void>)s_glClearNamedBufferData)(buffer, internalformat, format, type, data);

	private static void* s_glClearNamedBufferSubData;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearNamedBufferSubData(uint buffer, uint internalformat, nint offset, nint size, uint format, uint type, void* data) => ((delegate* unmanaged[Cdecl]<uint, uint, nint, nint, uint, uint, void*, void>)s_glClearNamedBufferSubData)(buffer, internalformat, offset, size, format, type, data);

	private static void* s_glClearNamedFramebufferfi;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearNamedFramebufferfi(uint framebuffer, uint buffer, int drawbuffer, float depth, int stencil) => ((delegate* unmanaged[Cdecl]<uint, uint, int, float, int, void>)s_glClearNamedFramebufferfi)(framebuffer, buffer, drawbuffer, depth, stencil);

	private static void* s_glClearNamedFramebufferfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearNamedFramebufferfv(uint framebuffer, uint buffer, int drawbuffer, float* value) => ((delegate* unmanaged[Cdecl]<uint, uint, int, float*, void>)s_glClearNamedFramebufferfv)(framebuffer, buffer, drawbuffer, value);

	private static void* s_glClearNamedFramebufferiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearNamedFramebufferiv(uint framebuffer, uint buffer, int drawbuffer, int* value) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int*, void>)s_glClearNamedFramebufferiv)(framebuffer, buffer, drawbuffer, value);

	private static void* s_glClearNamedFramebufferuiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearNamedFramebufferuiv(uint framebuffer, uint buffer, int drawbuffer, uint* value) => ((delegate* unmanaged[Cdecl]<uint, uint, int, uint*, void>)s_glClearNamedFramebufferuiv)(framebuffer, buffer, drawbuffer, value);

	private static void* s_glClearStencil;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearStencil(int s) => ((delegate* unmanaged[Cdecl]<int, void>)s_glClearStencil)(s);

	private static void* s_glClearTexImage;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearTexImage(uint texture, int level, uint format, uint type, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, uint, uint, void*, void>)s_glClearTexImage)(texture, level, format, type, data);

	private static void* s_glClearTexSubImage;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClearTexSubImage(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, uint format, uint type, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, uint, uint, void*, void>)s_glClearTexSubImage)(texture, level, xoffset, yoffset, zoffset, width, height, depth, format, type, data);

	private static void* s_glClientWaitSync;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static uint glClientWaitSync(nint sync, uint flags, ulong timeout) => ((delegate* unmanaged[Cdecl]<nint, uint, ulong, uint>)s_glClientWaitSync)(sync, flags, timeout);

	private static void* s_glClipControl;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glClipControl(uint origin, uint depth) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glClipControl)(origin, depth);

	private static void* s_glColorMask;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glColorMask(GLboolean red, GLboolean green, GLboolean blue, GLboolean alpha) => ((delegate* unmanaged[Cdecl]<GLboolean, GLboolean, GLboolean, GLboolean, void>)s_glColorMask)(red, green, blue, alpha);

	private static void* s_glColorMaski;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glColorMaski(uint index, GLboolean r, GLboolean g, GLboolean b, GLboolean a) => ((delegate* unmanaged[Cdecl]<uint, GLboolean, GLboolean, GLboolean, GLboolean, void>)s_glColorMaski)(index, r, g, b, a);

	private static void* s_glColorP3ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glColorP3ui(uint type, uint color) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glColorP3ui)(type, color);

	private static void* s_glColorP3uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glColorP3uiv(uint type, uint* color) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glColorP3uiv)(type, color);

	private static void* s_glColorP4ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glColorP4ui(uint type, uint color) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glColorP4ui)(type, color);

	private static void* s_glColorP4uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glColorP4uiv(uint type, uint* color) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glColorP4uiv)(type, color);

	private static void* s_glCompileShader;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCompileShader(uint shader) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glCompileShader)(shader);

	private static void* s_glCompressedTexImage1D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCompressedTexImage1D(uint target, int level, uint internalformat, int width, int border, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, int, void*, void>)s_glCompressedTexImage1D)(target, level, internalformat, width, border, imageSize, data);

	private static void* s_glCompressedTexImage2D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCompressedTexImage2D(uint target, int level, uint internalformat, int width, int height, int border, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, int, int, void*, void>)s_glCompressedTexImage2D)(target, level, internalformat, width, height, border, imageSize, data);

	private static void* s_glCompressedTexImage3D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCompressedTexImage3D(uint target, int level, uint internalformat, int width, int height, int depth, int border, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, int, int, int, void*, void>)s_glCompressedTexImage3D)(target, level, internalformat, width, height, depth, border, imageSize, data);

	private static void* s_glCompressedTexSubImage1D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCompressedTexSubImage1D(uint target, int level, int xoffset, int width, uint format, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, uint, int, void*, void>)s_glCompressedTexSubImage1D)(target, level, xoffset, width, format, imageSize, data);

	private static void* s_glCompressedTexSubImage2D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCompressedTexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, uint, int, void*, void>)s_glCompressedTexSubImage2D)(target, level, xoffset, yoffset, width, height, format, imageSize, data);

	private static void* s_glCompressedTexSubImage3D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCompressedTexSubImage3D(uint target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, uint format, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, uint, int, void*, void>)s_glCompressedTexSubImage3D)(target, level, xoffset, yoffset, zoffset, width, height, depth, format, imageSize, data);

	private static void* s_glCompressedTextureSubImage1D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCompressedTextureSubImage1D(uint texture, int level, int xoffset, int width, uint format, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, uint, int, void*, void>)s_glCompressedTextureSubImage1D)(texture, level, xoffset, width, format, imageSize, data);

	private static void* s_glCompressedTextureSubImage2D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCompressedTextureSubImage2D(uint texture, int level, int xoffset, int yoffset, int width, int height, uint format, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, uint, int, void*, void>)s_glCompressedTextureSubImage2D)(texture, level, xoffset, yoffset, width, height, format, imageSize, data);

	private static void* s_glCompressedTextureSubImage3D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCompressedTextureSubImage3D(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, uint format, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, uint, int, void*, void>)s_glCompressedTextureSubImage3D)(texture, level, xoffset, yoffset, zoffset, width, height, depth, format, imageSize, data);

	private static void* s_glCopyBufferSubData;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCopyBufferSubData(uint readTarget, uint writeTarget, nint readOffset, nint writeOffset, nint size) => ((delegate* unmanaged[Cdecl]<uint, uint, nint, nint, nint, void>)s_glCopyBufferSubData)(readTarget, writeTarget, readOffset, writeOffset, size);

	private static void* s_glCopyImageSubData;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCopyImageSubData(uint srcName, uint srcTarget, int srcLevel, int srcX, int srcY, int srcZ, uint dstName, uint dstTarget, int dstLevel, int dstX, int dstY, int dstZ, int srcWidth, int srcHeight, int srcDepth) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int, int, int, uint, uint, int, int, int, int, int, int, int, void>)s_glCopyImageSubData)(srcName, srcTarget, srcLevel, srcX, srcY, srcZ, dstName, dstTarget, dstLevel, dstX, dstY, dstZ, srcWidth, srcHeight, srcDepth);

	private static void* s_glCopyNamedBufferSubData;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCopyNamedBufferSubData(uint readBuffer, uint writeBuffer, nint readOffset, nint writeOffset, nint size) => ((delegate* unmanaged[Cdecl]<uint, uint, nint, nint, nint, void>)s_glCopyNamedBufferSubData)(readBuffer, writeBuffer, readOffset, writeOffset, size);

	private static void* s_glCopyTexImage1D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCopyTexImage1D(uint target, int level, uint internalformat, int x, int y, int width, int border) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, int, int, void>)s_glCopyTexImage1D)(target, level, internalformat, x, y, width, border);

	private static void* s_glCopyTexImage2D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCopyTexImage2D(uint target, int level, uint internalformat, int x, int y, int width, int height, int border) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, int, int, int, void>)s_glCopyTexImage2D)(target, level, internalformat, x, y, width, height, border);

	private static void* s_glCopyTexSubImage1D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCopyTexSubImage1D(uint target, int level, int xoffset, int x, int y, int width) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, void>)s_glCopyTexSubImage1D)(target, level, xoffset, x, y, width);

	private static void* s_glCopyTexSubImage2D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCopyTexSubImage2D(uint target, int level, int xoffset, int yoffset, int x, int y, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, void>)s_glCopyTexSubImage2D)(target, level, xoffset, yoffset, x, y, width, height);

	private static void* s_glCopyTexSubImage3D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCopyTexSubImage3D(uint target, int level, int xoffset, int yoffset, int zoffset, int x, int y, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, int, void>)s_glCopyTexSubImage3D)(target, level, xoffset, yoffset, zoffset, x, y, width, height);

	private static void* s_glCopyTextureSubImage1D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCopyTextureSubImage1D(uint texture, int level, int xoffset, int x, int y, int width) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, void>)s_glCopyTextureSubImage1D)(texture, level, xoffset, x, y, width);

	private static void* s_glCopyTextureSubImage2D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCopyTextureSubImage2D(uint texture, int level, int xoffset, int yoffset, int x, int y, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, void>)s_glCopyTextureSubImage2D)(texture, level, xoffset, yoffset, x, y, width, height);

	private static void* s_glCopyTextureSubImage3D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCopyTextureSubImage3D(uint texture, int level, int xoffset, int yoffset, int zoffset, int x, int y, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, int, void>)s_glCopyTextureSubImage3D)(texture, level, xoffset, yoffset, zoffset, x, y, width, height);

	private static void* s_glCreateBuffers;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCreateBuffers(int n, uint* buffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glCreateBuffers)(n, buffers);

	private static void* s_glCreateFramebuffers;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCreateFramebuffers(int n, uint* framebuffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glCreateFramebuffers)(n, framebuffers);

	private static void* s_glCreateProgram;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static uint glCreateProgram() => ((delegate* unmanaged[Cdecl]<uint>)s_glCreateProgram)();

	private static void* s_glCreateProgramPipelines;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCreateProgramPipelines(int n, uint* pipelines) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glCreateProgramPipelines)(n, pipelines);

	private static void* s_glCreateQueries;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCreateQueries(uint target, int n, uint* ids) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glCreateQueries)(target, n, ids);

	private static void* s_glCreateRenderbuffers;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCreateRenderbuffers(int n, uint* renderbuffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glCreateRenderbuffers)(n, renderbuffers);

	private static void* s_glCreateSamplers;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCreateSamplers(int n, uint* samplers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glCreateSamplers)(n, samplers);

	private static void* s_glCreateShader;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static uint glCreateShader(uint type) => ((delegate* unmanaged[Cdecl]<uint, uint>)s_glCreateShader)(type);

	private static void* s_glCreateShaderProgramv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static uint glCreateShaderProgramv(uint type, int count, byte** strings) => ((delegate* unmanaged[Cdecl]<uint, int, byte**, uint>)s_glCreateShaderProgramv)(type, count, strings);

	private static void* s_glCreateTextures;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCreateTextures(uint target, int n, uint* textures) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glCreateTextures)(target, n, textures);

	private static void* s_glCreateTransformFeedbacks;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCreateTransformFeedbacks(int n, uint* ids) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glCreateTransformFeedbacks)(n, ids);

	private static void* s_glCreateVertexArrays;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCreateVertexArrays(int n, uint* arrays) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glCreateVertexArrays)(n, arrays);

	private static void* s_glCullFace;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glCullFace(uint mode) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glCullFace)(mode);

	private static void* s_glDebugMessageCallback;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDebugMessageCallback(nint callback, void* userParam) => ((delegate* unmanaged[Cdecl]<nint, void*, void>)s_glDebugMessageCallback)(callback, userParam);

	private static void* s_glDebugMessageControl;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDebugMessageControl(uint source, uint type, uint severity, int count, uint* ids, GLboolean enabled) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int, uint*, GLboolean, void>)s_glDebugMessageControl)(source, type, severity, count, ids, enabled);

	private static void* s_glDebugMessageInsert;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDebugMessageInsert(uint source, uint type, uint id, uint severity, int length, byte* buf) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, uint, int, byte*, void>)s_glDebugMessageInsert)(source, type, id, severity, length, buf);

	private static void* s_glDeleteBuffers;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDeleteBuffers(int n, uint* buffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteBuffers)(n, buffers);

	private static void* s_glDeleteFramebuffers;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDeleteFramebuffers(int n, uint* framebuffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteFramebuffers)(n, framebuffers);

	private static void* s_glDeleteProgram;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDeleteProgram(uint program) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glDeleteProgram)(program);

	private static void* s_glDeleteProgramPipelines;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDeleteProgramPipelines(int n, uint* pipelines) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteProgramPipelines)(n, pipelines);

	private static void* s_glDeleteQueries;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDeleteQueries(int n, uint* ids) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteQueries)(n, ids);

	private static void* s_glDeleteRenderbuffers;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDeleteRenderbuffers(int n, uint* renderbuffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteRenderbuffers)(n, renderbuffers);

	private static void* s_glDeleteSamplers;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDeleteSamplers(int count, uint* samplers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteSamplers)(count, samplers);

	private static void* s_glDeleteShader;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDeleteShader(uint shader) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glDeleteShader)(shader);

	private static void* s_glDeleteSync;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDeleteSync(nint sync) => ((delegate* unmanaged[Cdecl]<nint, void>)s_glDeleteSync)(sync);

	private static void* s_glDeleteTextures;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDeleteTextures(int n, uint* textures) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteTextures)(n, textures);

	private static void* s_glDeleteTransformFeedbacks;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDeleteTransformFeedbacks(int n, uint* ids) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteTransformFeedbacks)(n, ids);

	private static void* s_glDeleteVertexArrays;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDeleteVertexArrays(int n, uint* arrays) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteVertexArrays)(n, arrays);

	private static void* s_glDepthFunc;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDepthFunc(uint func) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glDepthFunc)(func);

	private static void* s_glDepthMask;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDepthMask(GLboolean flag) => ((delegate* unmanaged[Cdecl]<GLboolean, void>)s_glDepthMask)(flag);

	private static void* s_glDepthRange;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDepthRange(double n, double f) => ((delegate* unmanaged[Cdecl]<double, double, void>)s_glDepthRange)(n, f);

	private static void* s_glDepthRangeArrayv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDepthRangeArrayv(uint first, int count, double* v) => ((delegate* unmanaged[Cdecl]<uint, int, double*, void>)s_glDepthRangeArrayv)(first, count, v);

	private static void* s_glDepthRangeIndexed;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDepthRangeIndexed(uint index, double n, double f) => ((delegate* unmanaged[Cdecl]<uint, double, double, void>)s_glDepthRangeIndexed)(index, n, f);

	private static void* s_glDepthRangef;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDepthRangef(float n, float f) => ((delegate* unmanaged[Cdecl]<float, float, void>)s_glDepthRangef)(n, f);

	private static void* s_glDetachShader;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDetachShader(uint program, uint shader) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glDetachShader)(program, shader);

	private static void* s_glDisable;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDisable(uint cap) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glDisable)(cap);

	private static void* s_glDisableVertexArrayAttrib;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDisableVertexArrayAttrib(uint vaobj, uint index) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glDisableVertexArrayAttrib)(vaobj, index);

	private static void* s_glDisableVertexAttribArray;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDisableVertexAttribArray(uint index) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glDisableVertexAttribArray)(index);

	private static void* s_glDisablei;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDisablei(uint target, uint index) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glDisablei)(target, index);

	private static void* s_glDispatchCompute;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDispatchCompute(uint num_groups_x, uint num_groups_y, uint num_groups_z) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glDispatchCompute)(num_groups_x, num_groups_y, num_groups_z);

	private static void* s_glDispatchComputeIndirect;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDispatchComputeIndirect(nint indirect) => ((delegate* unmanaged[Cdecl]<nint, void>)s_glDispatchComputeIndirect)(indirect);

	private static void* s_glDrawArrays;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawArrays(uint mode, int first, int count) => ((delegate* unmanaged[Cdecl]<uint, int, int, void>)s_glDrawArrays)(mode, first, count);

	private static void* s_glDrawArraysIndirect;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawArraysIndirect(uint mode, void* indirect) => ((delegate* unmanaged[Cdecl]<uint, void*, void>)s_glDrawArraysIndirect)(mode, indirect);

	private static void* s_glDrawArraysInstanced;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawArraysInstanced(uint mode, int first, int count, int instancecount) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, void>)s_glDrawArraysInstanced)(mode, first, count, instancecount);

	private static void* s_glDrawArraysInstancedBaseInstance;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawArraysInstancedBaseInstance(uint mode, int first, int count, int instancecount, uint baseinstance) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, uint, void>)s_glDrawArraysInstancedBaseInstance)(mode, first, count, instancecount, baseinstance);

	private static void* s_glDrawBuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawBuffer(uint buf) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glDrawBuffer)(buf);

	private static void* s_glDrawBuffers;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawBuffers(int n, uint* bufs) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDrawBuffers)(n, bufs);

	private static void* s_glDrawElements;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawElements(uint mode, int count, uint type, void* indices) => ((delegate* unmanaged[Cdecl]<uint, int, uint, void*, void>)s_glDrawElements)(mode, count, type, indices);

	private static void* s_glDrawElementsBaseVertex;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawElementsBaseVertex(uint mode, int count, uint type, void* indices, int basevertex) => ((delegate* unmanaged[Cdecl]<uint, int, uint, void*, int, void>)s_glDrawElementsBaseVertex)(mode, count, type, indices, basevertex);

	private static void* s_glDrawElementsIndirect;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawElementsIndirect(uint mode, uint type, void* indirect) => ((delegate* unmanaged[Cdecl]<uint, uint, void*, void>)s_glDrawElementsIndirect)(mode, type, indirect);

	private static void* s_glDrawElementsInstanced;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawElementsInstanced(uint mode, int count, uint type, void* indices, int instancecount) => ((delegate* unmanaged[Cdecl]<uint, int, uint, void*, int, void>)s_glDrawElementsInstanced)(mode, count, type, indices, instancecount);

	private static void* s_glDrawElementsInstancedBaseInstance;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawElementsInstancedBaseInstance(uint mode, int count, uint type, void* indices, int instancecount, uint baseinstance) => ((delegate* unmanaged[Cdecl]<uint, int, uint, void*, int, uint, void>)s_glDrawElementsInstancedBaseInstance)(mode, count, type, indices, instancecount, baseinstance);

	private static void* s_glDrawElementsInstancedBaseVertex;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawElementsInstancedBaseVertex(uint mode, int count, uint type, void* indices, int instancecount, int basevertex) => ((delegate* unmanaged[Cdecl]<uint, int, uint, void*, int, int, void>)s_glDrawElementsInstancedBaseVertex)(mode, count, type, indices, instancecount, basevertex);

	private static void* s_glDrawElementsInstancedBaseVertexBaseInstance;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawElementsInstancedBaseVertexBaseInstance(uint mode, int count, uint type, void* indices, int instancecount, int basevertex, uint baseinstance) => ((delegate* unmanaged[Cdecl]<uint, int, uint, void*, int, int, uint, void>)s_glDrawElementsInstancedBaseVertexBaseInstance)(mode, count, type, indices, instancecount, basevertex, baseinstance);

	private static void* s_glDrawRangeElements;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawRangeElements(uint mode, uint start, uint end, int count, uint type, void* indices) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int, uint, void*, void>)s_glDrawRangeElements)(mode, start, end, count, type, indices);

	private static void* s_glDrawRangeElementsBaseVertex;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawRangeElementsBaseVertex(uint mode, uint start, uint end, int count, uint type, void* indices, int basevertex) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int, uint, void*, int, void>)s_glDrawRangeElementsBaseVertex)(mode, start, end, count, type, indices, basevertex);

	private static void* s_glDrawTransformFeedback;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawTransformFeedback(uint mode, uint id) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glDrawTransformFeedback)(mode, id);

	private static void* s_glDrawTransformFeedbackInstanced;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawTransformFeedbackInstanced(uint mode, uint id, int instancecount) => ((delegate* unmanaged[Cdecl]<uint, uint, int, void>)s_glDrawTransformFeedbackInstanced)(mode, id, instancecount);

	private static void* s_glDrawTransformFeedbackStream;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawTransformFeedbackStream(uint mode, uint id, uint stream) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glDrawTransformFeedbackStream)(mode, id, stream);

	private static void* s_glDrawTransformFeedbackStreamInstanced;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glDrawTransformFeedbackStreamInstanced(uint mode, uint id, uint stream, int instancecount) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int, void>)s_glDrawTransformFeedbackStreamInstanced)(mode, id, stream, instancecount);

	private static void* s_glEnable;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glEnable(uint cap) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glEnable)(cap);

	private static void* s_glEnableVertexArrayAttrib;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glEnableVertexArrayAttrib(uint vaobj, uint index) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glEnableVertexArrayAttrib)(vaobj, index);

	private static void* s_glEnableVertexAttribArray;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glEnableVertexAttribArray(uint index) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glEnableVertexAttribArray)(index);

	private static void* s_glEnablei;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glEnablei(uint target, uint index) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glEnablei)(target, index);

	private static void* s_glEndConditionalRender;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glEndConditionalRender() => ((delegate* unmanaged[Cdecl]<void>)s_glEndConditionalRender)();

	private static void* s_glEndQuery;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glEndQuery(uint target) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glEndQuery)(target);

	private static void* s_glEndQueryIndexed;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glEndQueryIndexed(uint target, uint index) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glEndQueryIndexed)(target, index);

	private static void* s_glEndTransformFeedback;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glEndTransformFeedback() => ((delegate* unmanaged[Cdecl]<void>)s_glEndTransformFeedback)();

	private static void* s_glFenceSync;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static nint glFenceSync(uint condition, uint flags) => ((delegate* unmanaged[Cdecl]<uint, uint, nint>)s_glFenceSync)(condition, flags);

	private static void* s_glFinish;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glFinish() => ((delegate* unmanaged[Cdecl]<void>)s_glFinish)();

	private static void* s_glFlush;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glFlush() => ((delegate* unmanaged[Cdecl]<void>)s_glFlush)();

	private static void* s_glFlushMappedBufferRange;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glFlushMappedBufferRange(uint target, nint offset, nint length) => ((delegate* unmanaged[Cdecl]<uint, nint, nint, void>)s_glFlushMappedBufferRange)(target, offset, length);

	private static void* s_glFlushMappedNamedBufferRange;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glFlushMappedNamedBufferRange(uint buffer, nint offset, nint length) => ((delegate* unmanaged[Cdecl]<uint, nint, nint, void>)s_glFlushMappedNamedBufferRange)(buffer, offset, length);

	private static void* s_glFramebufferParameteri;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glFramebufferParameteri(uint target, uint pname, int param) => ((delegate* unmanaged[Cdecl]<uint, uint, int, void>)s_glFramebufferParameteri)(target, pname, param);

	private static void* s_glFramebufferRenderbuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glFramebufferRenderbuffer(uint target, uint attachment, uint renderbuffertarget, uint renderbuffer) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, uint, void>)s_glFramebufferRenderbuffer)(target, attachment, renderbuffertarget, renderbuffer);

	private static void* s_glFramebufferTexture;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glFramebufferTexture(uint target, uint attachment, uint texture, int level) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int, void>)s_glFramebufferTexture)(target, attachment, texture, level);

	private static void* s_glFramebufferTexture1D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glFramebufferTexture1D(uint target, uint attachment, uint textarget, uint texture, int level) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, uint, int, void>)s_glFramebufferTexture1D)(target, attachment, textarget, texture, level);

	private static void* s_glFramebufferTexture2D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glFramebufferTexture2D(uint target, uint attachment, uint textarget, uint texture, int level) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, uint, int, void>)s_glFramebufferTexture2D)(target, attachment, textarget, texture, level);

	private static void* s_glFramebufferTexture3D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glFramebufferTexture3D(uint target, uint attachment, uint textarget, uint texture, int level, int zoffset) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, uint, int, int, void>)s_glFramebufferTexture3D)(target, attachment, textarget, texture, level, zoffset);

	private static void* s_glFramebufferTextureLayer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glFramebufferTextureLayer(uint target, uint attachment, uint texture, int level, int layer) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int, int, void>)s_glFramebufferTextureLayer)(target, attachment, texture, level, layer);

	private static void* s_glFrontFace;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glFrontFace(uint mode) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glFrontFace)(mode);

	private static void* s_glGenBuffers;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGenBuffers(int n, uint* buffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenBuffers)(n, buffers);

	private static void* s_glGenFramebuffers;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGenFramebuffers(int n, uint* framebuffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenFramebuffers)(n, framebuffers);

	private static void* s_glGenProgramPipelines;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGenProgramPipelines(int n, uint* pipelines) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenProgramPipelines)(n, pipelines);

	private static void* s_glGenQueries;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGenQueries(int n, uint* ids) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenQueries)(n, ids);

	private static void* s_glGenRenderbuffers;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGenRenderbuffers(int n, uint* renderbuffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenRenderbuffers)(n, renderbuffers);

	private static void* s_glGenSamplers;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGenSamplers(int count, uint* samplers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenSamplers)(count, samplers);

	private static void* s_glGenTextures;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGenTextures(int n, uint* textures) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenTextures)(n, textures);

	private static void* s_glGenTransformFeedbacks;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGenTransformFeedbacks(int n, uint* ids) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenTransformFeedbacks)(n, ids);

	private static void* s_glGenVertexArrays;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGenVertexArrays(int n, uint* arrays) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenVertexArrays)(n, arrays);

	private static void* s_glGenerateMipmap;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGenerateMipmap(uint target) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glGenerateMipmap)(target);

	private static void* s_glGenerateTextureMipmap;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGenerateTextureMipmap(uint texture) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glGenerateTextureMipmap)(texture);

	private static void* s_glGetActiveAtomicCounterBufferiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetActiveAtomicCounterBufferiv(uint program, uint bufferIndex, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int*, void>)s_glGetActiveAtomicCounterBufferiv)(program, bufferIndex, pname, @params);

	private static void* s_glGetActiveAttrib;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetActiveAttrib(uint program, uint index, int bufSize, int* length, int* size, uint* type, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int*, int*, uint*, byte*, void>)s_glGetActiveAttrib)(program, index, bufSize, length, size, type, name);

	private static void* s_glGetActiveSubroutineName;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetActiveSubroutineName(uint program, uint shadertype, uint index, int bufSize, int* length, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int, int*, byte*, void>)s_glGetActiveSubroutineName)(program, shadertype, index, bufSize, length, name);

	private static void* s_glGetActiveSubroutineUniformName;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetActiveSubroutineUniformName(uint program, uint shadertype, uint index, int bufSize, int* length, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int, int*, byte*, void>)s_glGetActiveSubroutineUniformName)(program, shadertype, index, bufSize, length, name);

	private static void* s_glGetActiveSubroutineUniformiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetActiveSubroutineUniformiv(uint program, uint shadertype, uint index, uint pname, int* values) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, uint, int*, void>)s_glGetActiveSubroutineUniformiv)(program, shadertype, index, pname, values);

	private static void* s_glGetActiveUniform;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetActiveUniform(uint program, uint index, int bufSize, int* length, int* size, uint* type, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int*, int*, uint*, byte*, void>)s_glGetActiveUniform)(program, index, bufSize, length, size, type, name);

	private static void* s_glGetActiveUniformBlockName;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetActiveUniformBlockName(uint program, uint uniformBlockIndex, int bufSize, int* length, byte* uniformBlockName) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int*, byte*, void>)s_glGetActiveUniformBlockName)(program, uniformBlockIndex, bufSize, length, uniformBlockName);

	private static void* s_glGetActiveUniformBlockiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetActiveUniformBlockiv(uint program, uint uniformBlockIndex, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int*, void>)s_glGetActiveUniformBlockiv)(program, uniformBlockIndex, pname, @params);

	private static void* s_glGetActiveUniformName;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetActiveUniformName(uint program, uint uniformIndex, int bufSize, int* length, byte* uniformName) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int*, byte*, void>)s_glGetActiveUniformName)(program, uniformIndex, bufSize, length, uniformName);

	private static void* s_glGetActiveUniformsiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetActiveUniformsiv(uint program, int uniformCount, uint* uniformIndices, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, uint, int*, void>)s_glGetActiveUniformsiv)(program, uniformCount, uniformIndices, pname, @params);

	private static void* s_glGetAttachedShaders;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetAttachedShaders(uint program, int maxCount, int* count, uint* shaders) => ((delegate* unmanaged[Cdecl]<uint, int, int*, uint*, void>)s_glGetAttachedShaders)(program, maxCount, count, shaders);

	private static void* s_glGetAttribLocation;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static int glGetAttribLocation(uint program, byte* name) => ((delegate* unmanaged[Cdecl]<uint, byte*, int>)s_glGetAttribLocation)(program, name);

	private static void* s_glGetBooleani_v;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetBooleani_v(uint target, uint index, GLboolean* data) => ((delegate* unmanaged[Cdecl]<uint, uint, GLboolean*, void>)s_glGetBooleani_v)(target, index, data);

	private static void* s_glGetBooleanv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetBooleanv(uint pname, GLboolean* data) => ((delegate* unmanaged[Cdecl]<uint, GLboolean*, void>)s_glGetBooleanv)(pname, data);

	private static void* s_glGetBufferParameteri64v;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetBufferParameteri64v(uint target, uint pname, long* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, long*, void>)s_glGetBufferParameteri64v)(target, pname, @params);

	private static void* s_glGetBufferParameteriv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetBufferParameteriv(uint target, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetBufferParameteriv)(target, pname, @params);

	private static void* s_glGetBufferPointerv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetBufferPointerv(uint target, uint pname, void** @params) => ((delegate* unmanaged[Cdecl]<uint, uint, void**, void>)s_glGetBufferPointerv)(target, pname, @params);

	private static void* s_glGetBufferSubData;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetBufferSubData(uint target, nint offset, nint size, void* data) => ((delegate* unmanaged[Cdecl]<uint, nint, nint, void*, void>)s_glGetBufferSubData)(target, offset, size, data);

	private static void* s_glGetCompressedTexImage;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetCompressedTexImage(uint target, int level, void* img) => ((delegate* unmanaged[Cdecl]<uint, int, void*, void>)s_glGetCompressedTexImage)(target, level, img);

	private static void* s_glGetCompressedTextureImage;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetCompressedTextureImage(uint texture, int level, int bufSize, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, void*, void>)s_glGetCompressedTextureImage)(texture, level, bufSize, pixels);

	private static void* s_glGetCompressedTextureSubImage;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetCompressedTextureSubImage(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int bufSize, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, int, void*, void>)s_glGetCompressedTextureSubImage)(texture, level, xoffset, yoffset, zoffset, width, height, depth, bufSize, pixels);

	private static void* s_glGetDebugMessageLog;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static uint glGetDebugMessageLog(uint count, int bufSize, uint* sources, uint* types, uint* ids, uint* severities, int* lengths, byte* messageLog) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, uint*, uint*, uint*, int*, byte*, uint>)s_glGetDebugMessageLog)(count, bufSize, sources, types, ids, severities, lengths, messageLog);

	private static void* s_glGetDoublei_v;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetDoublei_v(uint target, uint index, double* data) => ((delegate* unmanaged[Cdecl]<uint, uint, double*, void>)s_glGetDoublei_v)(target, index, data);

	private static void* s_glGetDoublev;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetDoublev(uint pname, double* data) => ((delegate* unmanaged[Cdecl]<uint, double*, void>)s_glGetDoublev)(pname, data);

	private static void* s_glGetError;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static uint glGetError() => ((delegate* unmanaged[Cdecl]<uint>)s_glGetError)();

	private static void* s_glGetFloati_v;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetFloati_v(uint target, uint index, float* data) => ((delegate* unmanaged[Cdecl]<uint, uint, float*, void>)s_glGetFloati_v)(target, index, data);

	private static void* s_glGetFloatv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetFloatv(uint pname, float* data) => ((delegate* unmanaged[Cdecl]<uint, float*, void>)s_glGetFloatv)(pname, data);

	private static void* s_glGetFragDataIndex;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static int glGetFragDataIndex(uint program, byte* name) => ((delegate* unmanaged[Cdecl]<uint, byte*, int>)s_glGetFragDataIndex)(program, name);

	private static void* s_glGetFragDataLocation;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static int glGetFragDataLocation(uint program, byte* name) => ((delegate* unmanaged[Cdecl]<uint, byte*, int>)s_glGetFragDataLocation)(program, name);

	private static void* s_glGetFramebufferAttachmentParameteriv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetFramebufferAttachmentParameteriv(uint target, uint attachment, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int*, void>)s_glGetFramebufferAttachmentParameteriv)(target, attachment, pname, @params);

	private static void* s_glGetFramebufferParameteriv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetFramebufferParameteriv(uint target, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetFramebufferParameteriv)(target, pname, @params);

	private static void* s_glGetGraphicsResetStatus;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static uint glGetGraphicsResetStatus() => ((delegate* unmanaged[Cdecl]<uint>)s_glGetGraphicsResetStatus)();

	private static void* s_glGetInteger64i_v;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetInteger64i_v(uint target, uint index, long* data) => ((delegate* unmanaged[Cdecl]<uint, uint, long*, void>)s_glGetInteger64i_v)(target, index, data);

	private static void* s_glGetInteger64v;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetInteger64v(uint pname, long* data) => ((delegate* unmanaged[Cdecl]<uint, long*, void>)s_glGetInteger64v)(pname, data);

	private static void* s_glGetIntegeri_v;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetIntegeri_v(uint target, uint index, int* data) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetIntegeri_v)(target, index, data);

	private static void* s_glGetIntegerv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetIntegerv(uint pname, int* data) => ((delegate* unmanaged[Cdecl]<uint, int*, void>)s_glGetIntegerv)(pname, data);

	private static void* s_glGetInternalformati64v;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetInternalformati64v(uint target, uint internalformat, uint pname, int count, long* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int, long*, void>)s_glGetInternalformati64v)(target, internalformat, pname, count, @params);

	private static void* s_glGetInternalformativ;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetInternalformativ(uint target, uint internalformat, uint pname, int count, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int, int*, void>)s_glGetInternalformativ)(target, internalformat, pname, count, @params);

	private static void* s_glGetMultisamplefv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetMultisamplefv(uint pname, uint index, float* val) => ((delegate* unmanaged[Cdecl]<uint, uint, float*, void>)s_glGetMultisamplefv)(pname, index, val);

	private static void* s_glGetNamedBufferParameteri64v;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetNamedBufferParameteri64v(uint buffer, uint pname, long* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, long*, void>)s_glGetNamedBufferParameteri64v)(buffer, pname, @params);

	private static void* s_glGetNamedBufferParameteriv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetNamedBufferParameteriv(uint buffer, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetNamedBufferParameteriv)(buffer, pname, @params);

	private static void* s_glGetNamedBufferPointerv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetNamedBufferPointerv(uint buffer, uint pname, void** @params) => ((delegate* unmanaged[Cdecl]<uint, uint, void**, void>)s_glGetNamedBufferPointerv)(buffer, pname, @params);

	private static void* s_glGetNamedBufferSubData;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetNamedBufferSubData(uint buffer, nint offset, nint size, void* data) => ((delegate* unmanaged[Cdecl]<uint, nint, nint, void*, void>)s_glGetNamedBufferSubData)(buffer, offset, size, data);

	private static void* s_glGetNamedFramebufferAttachmentParameteriv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetNamedFramebufferAttachmentParameteriv(uint framebuffer, uint attachment, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int*, void>)s_glGetNamedFramebufferAttachmentParameteriv)(framebuffer, attachment, pname, @params);

	private static void* s_glGetNamedFramebufferParameteriv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetNamedFramebufferParameteriv(uint framebuffer, uint pname, int* param) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetNamedFramebufferParameteriv)(framebuffer, pname, param);

	private static void* s_glGetNamedRenderbufferParameteriv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetNamedRenderbufferParameteriv(uint renderbuffer, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetNamedRenderbufferParameteriv)(renderbuffer, pname, @params);

	private static void* s_glGetObjectLabel;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetObjectLabel(uint identifier, uint name, int bufSize, int* length, byte* label) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int*, byte*, void>)s_glGetObjectLabel)(identifier, name, bufSize, length, label);

	private static void* s_glGetObjectPtrLabel;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetObjectPtrLabel(void* ptr, int bufSize, int* length, byte* label) => ((delegate* unmanaged[Cdecl]<void*, int, int*, byte*, void>)s_glGetObjectPtrLabel)(ptr, bufSize, length, label);

	private static void* s_glGetProgramBinary;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetProgramBinary(uint program, int bufSize, int* length, uint* binaryFormat, void* binary) => ((delegate* unmanaged[Cdecl]<uint, int, int*, uint*, void*, void>)s_glGetProgramBinary)(program, bufSize, length, binaryFormat, binary);

	private static void* s_glGetProgramInfoLog;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetProgramInfoLog(uint program, int bufSize, int* length, byte* infoLog) => ((delegate* unmanaged[Cdecl]<uint, int, int*, byte*, void>)s_glGetProgramInfoLog)(program, bufSize, length, infoLog);

	private static void* s_glGetProgramInterfaceiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetProgramInterfaceiv(uint program, uint programInterface, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int*, void>)s_glGetProgramInterfaceiv)(program, programInterface, pname, @params);

	private static void* s_glGetProgramPipelineInfoLog;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetProgramPipelineInfoLog(uint pipeline, int bufSize, int* length, byte* infoLog) => ((delegate* unmanaged[Cdecl]<uint, int, int*, byte*, void>)s_glGetProgramPipelineInfoLog)(pipeline, bufSize, length, infoLog);

	private static void* s_glGetProgramPipelineiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetProgramPipelineiv(uint pipeline, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetProgramPipelineiv)(pipeline, pname, @params);

	private static void* s_glGetProgramResourceIndex;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static uint glGetProgramResourceIndex(uint program, uint programInterface, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, byte*, uint>)s_glGetProgramResourceIndex)(program, programInterface, name);

	private static void* s_glGetProgramResourceLocation;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static int glGetProgramResourceLocation(uint program, uint programInterface, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, byte*, int>)s_glGetProgramResourceLocation)(program, programInterface, name);

	private static void* s_glGetProgramResourceLocationIndex;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static int glGetProgramResourceLocationIndex(uint program, uint programInterface, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, byte*, int>)s_glGetProgramResourceLocationIndex)(program, programInterface, name);

	private static void* s_glGetProgramResourceName;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetProgramResourceName(uint program, uint programInterface, uint index, int bufSize, int* length, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int, int*, byte*, void>)s_glGetProgramResourceName)(program, programInterface, index, bufSize, length, name);

	private static void* s_glGetProgramResourceiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetProgramResourceiv(uint program, uint programInterface, uint index, int propCount, uint* props, int count, int* length, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int, uint*, int, int*, int*, void>)s_glGetProgramResourceiv)(program, programInterface, index, propCount, props, count, length, @params);

	private static void* s_glGetProgramStageiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetProgramStageiv(uint program, uint shadertype, uint pname, int* values) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int*, void>)s_glGetProgramStageiv)(program, shadertype, pname, values);

	private static void* s_glGetProgramiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetProgramiv(uint program, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetProgramiv)(program, pname, @params);

	private static void* s_glGetQueryBufferObjecti64v;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetQueryBufferObjecti64v(uint id, uint buffer, uint pname, nint offset) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, nint, void>)s_glGetQueryBufferObjecti64v)(id, buffer, pname, offset);

	private static void* s_glGetQueryBufferObjectiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetQueryBufferObjectiv(uint id, uint buffer, uint pname, nint offset) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, nint, void>)s_glGetQueryBufferObjectiv)(id, buffer, pname, offset);

	private static void* s_glGetQueryBufferObjectui64v;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetQueryBufferObjectui64v(uint id, uint buffer, uint pname, nint offset) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, nint, void>)s_glGetQueryBufferObjectui64v)(id, buffer, pname, offset);

	private static void* s_glGetQueryBufferObjectuiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetQueryBufferObjectuiv(uint id, uint buffer, uint pname, nint offset) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, nint, void>)s_glGetQueryBufferObjectuiv)(id, buffer, pname, offset);

	private static void* s_glGetQueryIndexediv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetQueryIndexediv(uint target, uint index, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int*, void>)s_glGetQueryIndexediv)(target, index, pname, @params);

	private static void* s_glGetQueryObjecti64v;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetQueryObjecti64v(uint id, uint pname, long* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, long*, void>)s_glGetQueryObjecti64v)(id, pname, @params);

	private static void* s_glGetQueryObjectiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetQueryObjectiv(uint id, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetQueryObjectiv)(id, pname, @params);

	private static void* s_glGetQueryObjectui64v;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetQueryObjectui64v(uint id, uint pname, ulong* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, ulong*, void>)s_glGetQueryObjectui64v)(id, pname, @params);

	private static void* s_glGetQueryObjectuiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetQueryObjectuiv(uint id, uint pname, uint* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, uint*, void>)s_glGetQueryObjectuiv)(id, pname, @params);

	private static void* s_glGetQueryiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetQueryiv(uint target, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetQueryiv)(target, pname, @params);

	private static void* s_glGetRenderbufferParameteriv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetRenderbufferParameteriv(uint target, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetRenderbufferParameteriv)(target, pname, @params);

	private static void* s_glGetSamplerParameterIiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetSamplerParameterIiv(uint sampler, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetSamplerParameterIiv)(sampler, pname, @params);

	private static void* s_glGetSamplerParameterIuiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetSamplerParameterIuiv(uint sampler, uint pname, uint* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, uint*, void>)s_glGetSamplerParameterIuiv)(sampler, pname, @params);

	private static void* s_glGetSamplerParameterfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetSamplerParameterfv(uint sampler, uint pname, float* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, float*, void>)s_glGetSamplerParameterfv)(sampler, pname, @params);

	private static void* s_glGetSamplerParameteriv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetSamplerParameteriv(uint sampler, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetSamplerParameteriv)(sampler, pname, @params);

	private static void* s_glGetShaderInfoLog;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetShaderInfoLog(uint shader, int bufSize, int* length, byte* infoLog) => ((delegate* unmanaged[Cdecl]<uint, int, int*, byte*, void>)s_glGetShaderInfoLog)(shader, bufSize, length, infoLog);

	private static void* s_glGetShaderPrecisionFormat;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetShaderPrecisionFormat(uint shadertype, uint precisiontype, int* range, int* precision) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, int*, void>)s_glGetShaderPrecisionFormat)(shadertype, precisiontype, range, precision);

	private static void* s_glGetShaderSource;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetShaderSource(uint shader, int bufSize, int* length, byte* source) => ((delegate* unmanaged[Cdecl]<uint, int, int*, byte*, void>)s_glGetShaderSource)(shader, bufSize, length, source);

	private static void* s_glGetShaderiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetShaderiv(uint shader, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetShaderiv)(shader, pname, @params);

	private static void* s_glGetString;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static byte* glGetString(uint name) => ((delegate* unmanaged[Cdecl]<uint, byte*>)s_glGetString)(name);

	private static void* s_glGetStringi;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static byte* glGetStringi(uint name, uint index) => ((delegate* unmanaged[Cdecl]<uint, uint, byte*>)s_glGetStringi)(name, index);

	private static void* s_glGetSubroutineIndex;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static uint glGetSubroutineIndex(uint program, uint shadertype, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, byte*, uint>)s_glGetSubroutineIndex)(program, shadertype, name);

	private static void* s_glGetSubroutineUniformLocation;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static int glGetSubroutineUniformLocation(uint program, uint shadertype, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, byte*, int>)s_glGetSubroutineUniformLocation)(program, shadertype, name);

	private static void* s_glGetSynciv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetSynciv(nint sync, uint pname, int count, int* length, int* values) => ((delegate* unmanaged[Cdecl]<nint, uint, int, int*, int*, void>)s_glGetSynciv)(sync, pname, count, length, values);

	private static void* s_glGetTexImage;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTexImage(uint target, int level, uint format, uint type, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, uint, uint, void*, void>)s_glGetTexImage)(target, level, format, type, pixels);

	private static void* s_glGetTexLevelParameterfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTexLevelParameterfv(uint target, int level, uint pname, float* @params) => ((delegate* unmanaged[Cdecl]<uint, int, uint, float*, void>)s_glGetTexLevelParameterfv)(target, level, pname, @params);

	private static void* s_glGetTexLevelParameteriv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTexLevelParameteriv(uint target, int level, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int*, void>)s_glGetTexLevelParameteriv)(target, level, pname, @params);

	private static void* s_glGetTexParameterIiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTexParameterIiv(uint target, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetTexParameterIiv)(target, pname, @params);

	private static void* s_glGetTexParameterIuiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTexParameterIuiv(uint target, uint pname, uint* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, uint*, void>)s_glGetTexParameterIuiv)(target, pname, @params);

	private static void* s_glGetTexParameterfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTexParameterfv(uint target, uint pname, float* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, float*, void>)s_glGetTexParameterfv)(target, pname, @params);

	private static void* s_glGetTexParameteriv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTexParameteriv(uint target, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetTexParameteriv)(target, pname, @params);

	private static void* s_glGetTextureImage;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTextureImage(uint texture, int level, uint format, uint type, int bufSize, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, uint, uint, int, void*, void>)s_glGetTextureImage)(texture, level, format, type, bufSize, pixels);

	private static void* s_glGetTextureLevelParameterfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTextureLevelParameterfv(uint texture, int level, uint pname, float* @params) => ((delegate* unmanaged[Cdecl]<uint, int, uint, float*, void>)s_glGetTextureLevelParameterfv)(texture, level, pname, @params);

	private static void* s_glGetTextureLevelParameteriv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTextureLevelParameteriv(uint texture, int level, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int*, void>)s_glGetTextureLevelParameteriv)(texture, level, pname, @params);

	private static void* s_glGetTextureParameterIiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTextureParameterIiv(uint texture, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetTextureParameterIiv)(texture, pname, @params);

	private static void* s_glGetTextureParameterIuiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTextureParameterIuiv(uint texture, uint pname, uint* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, uint*, void>)s_glGetTextureParameterIuiv)(texture, pname, @params);

	private static void* s_glGetTextureParameterfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTextureParameterfv(uint texture, uint pname, float* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, float*, void>)s_glGetTextureParameterfv)(texture, pname, @params);

	private static void* s_glGetTextureParameteriv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTextureParameteriv(uint texture, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetTextureParameteriv)(texture, pname, @params);

	private static void* s_glGetTextureSubImage;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTextureSubImage(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, uint format, uint type, int bufSize, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, uint, uint, int, void*, void>)s_glGetTextureSubImage)(texture, level, xoffset, yoffset, zoffset, width, height, depth, format, type, bufSize, pixels);

	private static void* s_glGetTransformFeedbackVarying;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTransformFeedbackVarying(uint program, uint index, int bufSize, int* length, int* size, uint* type, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int*, int*, uint*, byte*, void>)s_glGetTransformFeedbackVarying)(program, index, bufSize, length, size, type, name);

	private static void* s_glGetTransformFeedbacki64_v;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTransformFeedbacki64_v(uint xfb, uint pname, uint index, long* param) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, long*, void>)s_glGetTransformFeedbacki64_v)(xfb, pname, index, param);

	private static void* s_glGetTransformFeedbacki_v;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTransformFeedbacki_v(uint xfb, uint pname, uint index, int* param) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int*, void>)s_glGetTransformFeedbacki_v)(xfb, pname, index, param);

	private static void* s_glGetTransformFeedbackiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetTransformFeedbackiv(uint xfb, uint pname, int* param) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetTransformFeedbackiv)(xfb, pname, param);

	private static void* s_glGetUniformBlockIndex;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static uint glGetUniformBlockIndex(uint program, byte* uniformBlockName) => ((delegate* unmanaged[Cdecl]<uint, byte*, uint>)s_glGetUniformBlockIndex)(program, uniformBlockName);

	private static void* s_glGetUniformIndices;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetUniformIndices(uint program, int uniformCount, byte** uniformNames, uint* uniformIndices) => ((delegate* unmanaged[Cdecl]<uint, int, byte**, uint*, void>)s_glGetUniformIndices)(program, uniformCount, uniformNames, uniformIndices);

	private static void* s_glGetUniformLocation;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static int glGetUniformLocation(uint program, byte* name) => ((delegate* unmanaged[Cdecl]<uint, byte*, int>)s_glGetUniformLocation)(program, name);

	private static void* s_glGetUniformSubroutineuiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetUniformSubroutineuiv(uint shadertype, int location, uint* @params) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glGetUniformSubroutineuiv)(shadertype, location, @params);

	private static void* s_glGetUniformdv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetUniformdv(uint program, int location, double* @params) => ((delegate* unmanaged[Cdecl]<uint, int, double*, void>)s_glGetUniformdv)(program, location, @params);

	private static void* s_glGetUniformfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetUniformfv(uint program, int location, float* @params) => ((delegate* unmanaged[Cdecl]<uint, int, float*, void>)s_glGetUniformfv)(program, location, @params);

	private static void* s_glGetUniformiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetUniformiv(uint program, int location, int* @params) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glGetUniformiv)(program, location, @params);

	private static void* s_glGetUniformuiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetUniformuiv(uint program, int location, uint* @params) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glGetUniformuiv)(program, location, @params);

	private static void* s_glGetVertexArrayIndexed64iv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetVertexArrayIndexed64iv(uint vaobj, uint index, uint pname, long* param) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, long*, void>)s_glGetVertexArrayIndexed64iv)(vaobj, index, pname, param);

	private static void* s_glGetVertexArrayIndexediv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetVertexArrayIndexediv(uint vaobj, uint index, uint pname, int* param) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int*, void>)s_glGetVertexArrayIndexediv)(vaobj, index, pname, param);

	private static void* s_glGetVertexArrayiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetVertexArrayiv(uint vaobj, uint pname, int* param) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetVertexArrayiv)(vaobj, pname, param);

	private static void* s_glGetVertexAttribIiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetVertexAttribIiv(uint index, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetVertexAttribIiv)(index, pname, @params);

	private static void* s_glGetVertexAttribIuiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetVertexAttribIuiv(uint index, uint pname, uint* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, uint*, void>)s_glGetVertexAttribIuiv)(index, pname, @params);

	private static void* s_glGetVertexAttribLdv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetVertexAttribLdv(uint index, uint pname, double* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, double*, void>)s_glGetVertexAttribLdv)(index, pname, @params);

	private static void* s_glGetVertexAttribPointerv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetVertexAttribPointerv(uint index, uint pname, void** pointer) => ((delegate* unmanaged[Cdecl]<uint, uint, void**, void>)s_glGetVertexAttribPointerv)(index, pname, pointer);

	private static void* s_glGetVertexAttribdv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetVertexAttribdv(uint index, uint pname, double* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, double*, void>)s_glGetVertexAttribdv)(index, pname, @params);

	private static void* s_glGetVertexAttribfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetVertexAttribfv(uint index, uint pname, float* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, float*, void>)s_glGetVertexAttribfv)(index, pname, @params);

	private static void* s_glGetVertexAttribiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetVertexAttribiv(uint index, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glGetVertexAttribiv)(index, pname, @params);

	private static void* s_glGetnColorTable;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnColorTable(uint target, uint format, uint type, int bufSize, void* table) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int, void*, void>)s_glGetnColorTable)(target, format, type, bufSize, table);

	private static void* s_glGetnCompressedTexImage;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnCompressedTexImage(uint target, int lod, int bufSize, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, void*, void>)s_glGetnCompressedTexImage)(target, lod, bufSize, pixels);

	private static void* s_glGetnConvolutionFilter;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnConvolutionFilter(uint target, uint format, uint type, int bufSize, void* image) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int, void*, void>)s_glGetnConvolutionFilter)(target, format, type, bufSize, image);

	private static void* s_glGetnHistogram;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnHistogram(uint target, GLboolean reset, uint format, uint type, int bufSize, void* values) => ((delegate* unmanaged[Cdecl]<uint, GLboolean, uint, uint, int, void*, void>)s_glGetnHistogram)(target, reset, format, type, bufSize, values);

	private static void* s_glGetnMapdv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnMapdv(uint target, uint query, int bufSize, double* v) => ((delegate* unmanaged[Cdecl]<uint, uint, int, double*, void>)s_glGetnMapdv)(target, query, bufSize, v);

	private static void* s_glGetnMapfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnMapfv(uint target, uint query, int bufSize, float* v) => ((delegate* unmanaged[Cdecl]<uint, uint, int, float*, void>)s_glGetnMapfv)(target, query, bufSize, v);

	private static void* s_glGetnMapiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnMapiv(uint target, uint query, int bufSize, int* v) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int*, void>)s_glGetnMapiv)(target, query, bufSize, v);

	private static void* s_glGetnMinmax;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnMinmax(uint target, GLboolean reset, uint format, uint type, int bufSize, void* values) => ((delegate* unmanaged[Cdecl]<uint, GLboolean, uint, uint, int, void*, void>)s_glGetnMinmax)(target, reset, format, type, bufSize, values);

	private static void* s_glGetnPixelMapfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnPixelMapfv(uint map, int bufSize, float* values) => ((delegate* unmanaged[Cdecl]<uint, int, float*, void>)s_glGetnPixelMapfv)(map, bufSize, values);

	private static void* s_glGetnPixelMapuiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnPixelMapuiv(uint map, int bufSize, uint* values) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glGetnPixelMapuiv)(map, bufSize, values);

	private static void* s_glGetnPixelMapusv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnPixelMapusv(uint map, int bufSize, ushort* values) => ((delegate* unmanaged[Cdecl]<uint, int, ushort*, void>)s_glGetnPixelMapusv)(map, bufSize, values);

	private static void* s_glGetnPolygonStipple;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnPolygonStipple(int bufSize, byte* pattern) => ((delegate* unmanaged[Cdecl]<int, byte*, void>)s_glGetnPolygonStipple)(bufSize, pattern);

	private static void* s_glGetnSeparableFilter;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnSeparableFilter(uint target, uint format, uint type, int rowBufSize, void* row, int columnBufSize, void* column, void* span) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int, void*, int, void*, void*, void>)s_glGetnSeparableFilter)(target, format, type, rowBufSize, row, columnBufSize, column, span);

	private static void* s_glGetnTexImage;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnTexImage(uint target, int level, uint format, uint type, int bufSize, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, uint, uint, int, void*, void>)s_glGetnTexImage)(target, level, format, type, bufSize, pixels);

	private static void* s_glGetnUniformdv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnUniformdv(uint program, int location, int bufSize, double* @params) => ((delegate* unmanaged[Cdecl]<uint, int, int, double*, void>)s_glGetnUniformdv)(program, location, bufSize, @params);

	private static void* s_glGetnUniformfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnUniformfv(uint program, int location, int bufSize, float* @params) => ((delegate* unmanaged[Cdecl]<uint, int, int, float*, void>)s_glGetnUniformfv)(program, location, bufSize, @params);

	private static void* s_glGetnUniformiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnUniformiv(uint program, int location, int bufSize, int* @params) => ((delegate* unmanaged[Cdecl]<uint, int, int, int*, void>)s_glGetnUniformiv)(program, location, bufSize, @params);

	private static void* s_glGetnUniformuiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glGetnUniformuiv(uint program, int location, int bufSize, uint* @params) => ((delegate* unmanaged[Cdecl]<uint, int, int, uint*, void>)s_glGetnUniformuiv)(program, location, bufSize, @params);

	private static void* s_glHint;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glHint(uint target, uint mode) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glHint)(target, mode);

	private static void* s_glInvalidateBufferData;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glInvalidateBufferData(uint buffer) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glInvalidateBufferData)(buffer);

	private static void* s_glInvalidateBufferSubData;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glInvalidateBufferSubData(uint buffer, nint offset, nint length) => ((delegate* unmanaged[Cdecl]<uint, nint, nint, void>)s_glInvalidateBufferSubData)(buffer, offset, length);

	private static void* s_glInvalidateFramebuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glInvalidateFramebuffer(uint target, int numAttachments, uint* attachments) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glInvalidateFramebuffer)(target, numAttachments, attachments);

	private static void* s_glInvalidateNamedFramebufferData;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glInvalidateNamedFramebufferData(uint framebuffer, int numAttachments, uint* attachments) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glInvalidateNamedFramebufferData)(framebuffer, numAttachments, attachments);

	private static void* s_glInvalidateNamedFramebufferSubData;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glInvalidateNamedFramebufferSubData(uint framebuffer, int numAttachments, uint* attachments, int x, int y, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, int, int, int, int, void>)s_glInvalidateNamedFramebufferSubData)(framebuffer, numAttachments, attachments, x, y, width, height);

	private static void* s_glInvalidateSubFramebuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glInvalidateSubFramebuffer(uint target, int numAttachments, uint* attachments, int x, int y, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, int, int, int, int, void>)s_glInvalidateSubFramebuffer)(target, numAttachments, attachments, x, y, width, height);

	private static void* s_glInvalidateTexImage;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glInvalidateTexImage(uint texture, int level) => ((delegate* unmanaged[Cdecl]<uint, int, void>)s_glInvalidateTexImage)(texture, level);

	private static void* s_glInvalidateTexSubImage;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glInvalidateTexSubImage(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, void>)s_glInvalidateTexSubImage)(texture, level, xoffset, yoffset, zoffset, width, height, depth);

	private static void* s_glIsBuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static GLboolean glIsBuffer(uint buffer) => ((delegate* unmanaged[Cdecl]<uint, GLboolean>)s_glIsBuffer)(buffer);

	private static void* s_glIsEnabled;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static GLboolean glIsEnabled(uint cap) => ((delegate* unmanaged[Cdecl]<uint, GLboolean>)s_glIsEnabled)(cap);

	private static void* s_glIsEnabledi;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static GLboolean glIsEnabledi(uint target, uint index) => ((delegate* unmanaged[Cdecl]<uint, uint, GLboolean>)s_glIsEnabledi)(target, index);

	private static void* s_glIsFramebuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static GLboolean glIsFramebuffer(uint framebuffer) => ((delegate* unmanaged[Cdecl]<uint, GLboolean>)s_glIsFramebuffer)(framebuffer);

	private static void* s_glIsProgram;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static GLboolean glIsProgram(uint program) => ((delegate* unmanaged[Cdecl]<uint, GLboolean>)s_glIsProgram)(program);

	private static void* s_glIsProgramPipeline;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static GLboolean glIsProgramPipeline(uint pipeline) => ((delegate* unmanaged[Cdecl]<uint, GLboolean>)s_glIsProgramPipeline)(pipeline);

	private static void* s_glIsQuery;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static GLboolean glIsQuery(uint id) => ((delegate* unmanaged[Cdecl]<uint, GLboolean>)s_glIsQuery)(id);

	private static void* s_glIsRenderbuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static GLboolean glIsRenderbuffer(uint renderbuffer) => ((delegate* unmanaged[Cdecl]<uint, GLboolean>)s_glIsRenderbuffer)(renderbuffer);

	private static void* s_glIsSampler;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static GLboolean glIsSampler(uint sampler) => ((delegate* unmanaged[Cdecl]<uint, GLboolean>)s_glIsSampler)(sampler);

	private static void* s_glIsShader;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static GLboolean glIsShader(uint shader) => ((delegate* unmanaged[Cdecl]<uint, GLboolean>)s_glIsShader)(shader);

	private static void* s_glIsSync;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static GLboolean glIsSync(nint sync) => ((delegate* unmanaged[Cdecl]<nint, GLboolean>)s_glIsSync)(sync);

	private static void* s_glIsTexture;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static GLboolean glIsTexture(uint texture) => ((delegate* unmanaged[Cdecl]<uint, GLboolean>)s_glIsTexture)(texture);

	private static void* s_glIsTransformFeedback;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static GLboolean glIsTransformFeedback(uint id) => ((delegate* unmanaged[Cdecl]<uint, GLboolean>)s_glIsTransformFeedback)(id);

	private static void* s_glIsVertexArray;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static GLboolean glIsVertexArray(uint array) => ((delegate* unmanaged[Cdecl]<uint, GLboolean>)s_glIsVertexArray)(array);

	private static void* s_glLineWidth;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glLineWidth(float width) => ((delegate* unmanaged[Cdecl]<float, void>)s_glLineWidth)(width);

	private static void* s_glLinkProgram;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glLinkProgram(uint program) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glLinkProgram)(program);

	private static void* s_glLogicOp;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glLogicOp(uint opcode) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glLogicOp)(opcode);

	private static void* s_glMapBuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void * glMapBuffer(uint target, uint access) => ((delegate* unmanaged[Cdecl]<uint, uint, void *>)s_glMapBuffer)(target, access);

	private static void* s_glMapBufferRange;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void * glMapBufferRange(uint target, nint offset, nint length, uint access) => ((delegate* unmanaged[Cdecl]<uint, nint, nint, uint, void *>)s_glMapBufferRange)(target, offset, length, access);

	private static void* s_glMapNamedBuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void * glMapNamedBuffer(uint buffer, uint access) => ((delegate* unmanaged[Cdecl]<uint, uint, void *>)s_glMapNamedBuffer)(buffer, access);

	private static void* s_glMapNamedBufferRange;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void * glMapNamedBufferRange(uint buffer, nint offset, nint length, uint access) => ((delegate* unmanaged[Cdecl]<uint, nint, nint, uint, void *>)s_glMapNamedBufferRange)(buffer, offset, length, access);

	private static void* s_glMemoryBarrier;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMemoryBarrier(uint barriers) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glMemoryBarrier)(barriers);

	private static void* s_glMemoryBarrierByRegion;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMemoryBarrierByRegion(uint barriers) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glMemoryBarrierByRegion)(barriers);

	private static void* s_glMinSampleShading;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMinSampleShading(float value) => ((delegate* unmanaged[Cdecl]<float, void>)s_glMinSampleShading)(value);

	private static void* s_glMultiDrawArrays;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMultiDrawArrays(uint mode, int* first, int* count, int drawcount) => ((delegate* unmanaged[Cdecl]<uint, int*, int*, int, void>)s_glMultiDrawArrays)(mode, first, count, drawcount);

	private static void* s_glMultiDrawArraysIndirect;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMultiDrawArraysIndirect(uint mode, void* indirect, int drawcount, int stride) => ((delegate* unmanaged[Cdecl]<uint, void*, int, int, void>)s_glMultiDrawArraysIndirect)(mode, indirect, drawcount, stride);

	private static void* s_glMultiDrawArraysIndirectCount;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMultiDrawArraysIndirectCount(uint mode, void* indirect, nint drawcount, int maxdrawcount, int stride) => ((delegate* unmanaged[Cdecl]<uint, void*, nint, int, int, void>)s_glMultiDrawArraysIndirectCount)(mode, indirect, drawcount, maxdrawcount, stride);

	private static void* s_glMultiDrawElements;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMultiDrawElements(uint mode, int* count, uint type, void** indices, int drawcount) => ((delegate* unmanaged[Cdecl]<uint, int*, uint, void**, int, void>)s_glMultiDrawElements)(mode, count, type, indices, drawcount);

	private static void* s_glMultiDrawElementsBaseVertex;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMultiDrawElementsBaseVertex(uint mode, int* count, uint type, void** indices, int drawcount, int* basevertex) => ((delegate* unmanaged[Cdecl]<uint, int*, uint, void**, int, int*, void>)s_glMultiDrawElementsBaseVertex)(mode, count, type, indices, drawcount, basevertex);

	private static void* s_glMultiDrawElementsIndirect;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMultiDrawElementsIndirect(uint mode, uint type, void* indirect, int drawcount, int stride) => ((delegate* unmanaged[Cdecl]<uint, uint, void*, int, int, void>)s_glMultiDrawElementsIndirect)(mode, type, indirect, drawcount, stride);

	private static void* s_glMultiDrawElementsIndirectCount;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMultiDrawElementsIndirectCount(uint mode, uint type, void* indirect, nint drawcount, int maxdrawcount, int stride) => ((delegate* unmanaged[Cdecl]<uint, uint, void*, nint, int, int, void>)s_glMultiDrawElementsIndirectCount)(mode, type, indirect, drawcount, maxdrawcount, stride);

	private static void* s_glMultiTexCoordP1ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMultiTexCoordP1ui(uint texture, uint type, uint coords) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glMultiTexCoordP1ui)(texture, type, coords);

	private static void* s_glMultiTexCoordP1uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMultiTexCoordP1uiv(uint texture, uint type, uint* coords) => ((delegate* unmanaged[Cdecl]<uint, uint, uint*, void>)s_glMultiTexCoordP1uiv)(texture, type, coords);

	private static void* s_glMultiTexCoordP2ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMultiTexCoordP2ui(uint texture, uint type, uint coords) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glMultiTexCoordP2ui)(texture, type, coords);

	private static void* s_glMultiTexCoordP2uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMultiTexCoordP2uiv(uint texture, uint type, uint* coords) => ((delegate* unmanaged[Cdecl]<uint, uint, uint*, void>)s_glMultiTexCoordP2uiv)(texture, type, coords);

	private static void* s_glMultiTexCoordP3ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMultiTexCoordP3ui(uint texture, uint type, uint coords) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glMultiTexCoordP3ui)(texture, type, coords);

	private static void* s_glMultiTexCoordP3uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMultiTexCoordP3uiv(uint texture, uint type, uint* coords) => ((delegate* unmanaged[Cdecl]<uint, uint, uint*, void>)s_glMultiTexCoordP3uiv)(texture, type, coords);

	private static void* s_glMultiTexCoordP4ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMultiTexCoordP4ui(uint texture, uint type, uint coords) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glMultiTexCoordP4ui)(texture, type, coords);

	private static void* s_glMultiTexCoordP4uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glMultiTexCoordP4uiv(uint texture, uint type, uint* coords) => ((delegate* unmanaged[Cdecl]<uint, uint, uint*, void>)s_glMultiTexCoordP4uiv)(texture, type, coords);

	private static void* s_glNamedBufferData;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glNamedBufferData(uint buffer, nint size, void* data, uint usage) => ((delegate* unmanaged[Cdecl]<uint, nint, void*, uint, void>)s_glNamedBufferData)(buffer, size, data, usage);

	private static void* s_glNamedBufferStorage;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glNamedBufferStorage(uint buffer, nint size, void* data, uint flags) => ((delegate* unmanaged[Cdecl]<uint, nint, void*, uint, void>)s_glNamedBufferStorage)(buffer, size, data, flags);

	private static void* s_glNamedBufferSubData;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glNamedBufferSubData(uint buffer, nint offset, nint size, void* data) => ((delegate* unmanaged[Cdecl]<uint, nint, nint, void*, void>)s_glNamedBufferSubData)(buffer, offset, size, data);

	private static void* s_glNamedFramebufferDrawBuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glNamedFramebufferDrawBuffer(uint framebuffer, uint buf) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glNamedFramebufferDrawBuffer)(framebuffer, buf);

	private static void* s_glNamedFramebufferDrawBuffers;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glNamedFramebufferDrawBuffers(uint framebuffer, int n, uint* bufs) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glNamedFramebufferDrawBuffers)(framebuffer, n, bufs);

	private static void* s_glNamedFramebufferParameteri;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glNamedFramebufferParameteri(uint framebuffer, uint pname, int param) => ((delegate* unmanaged[Cdecl]<uint, uint, int, void>)s_glNamedFramebufferParameteri)(framebuffer, pname, param);

	private static void* s_glNamedFramebufferReadBuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glNamedFramebufferReadBuffer(uint framebuffer, uint src) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glNamedFramebufferReadBuffer)(framebuffer, src);

	private static void* s_glNamedFramebufferRenderbuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glNamedFramebufferRenderbuffer(uint framebuffer, uint attachment, uint renderbuffertarget, uint renderbuffer) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, uint, void>)s_glNamedFramebufferRenderbuffer)(framebuffer, attachment, renderbuffertarget, renderbuffer);

	private static void* s_glNamedFramebufferTexture;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glNamedFramebufferTexture(uint framebuffer, uint attachment, uint texture, int level) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int, void>)s_glNamedFramebufferTexture)(framebuffer, attachment, texture, level);

	private static void* s_glNamedFramebufferTextureLayer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glNamedFramebufferTextureLayer(uint framebuffer, uint attachment, uint texture, int level, int layer) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, int, int, void>)s_glNamedFramebufferTextureLayer)(framebuffer, attachment, texture, level, layer);

	private static void* s_glNamedRenderbufferStorage;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glNamedRenderbufferStorage(uint renderbuffer, uint internalformat, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int, void>)s_glNamedRenderbufferStorage)(renderbuffer, internalformat, width, height);

	private static void* s_glNamedRenderbufferStorageMultisample;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glNamedRenderbufferStorageMultisample(uint renderbuffer, int samples, uint internalformat, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, void>)s_glNamedRenderbufferStorageMultisample)(renderbuffer, samples, internalformat, width, height);

	private static void* s_glNormalP3ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glNormalP3ui(uint type, uint coords) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glNormalP3ui)(type, coords);

	private static void* s_glNormalP3uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glNormalP3uiv(uint type, uint* coords) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glNormalP3uiv)(type, coords);

	private static void* s_glObjectLabel;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glObjectLabel(uint identifier, uint name, int length, byte* label) => ((delegate* unmanaged[Cdecl]<uint, uint, int, byte*, void>)s_glObjectLabel)(identifier, name, length, label);

	private static void* s_glObjectPtrLabel;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glObjectPtrLabel(void* ptr, int length, byte* label) => ((delegate* unmanaged[Cdecl]<void*, int, byte*, void>)s_glObjectPtrLabel)(ptr, length, label);

	private static void* s_glPatchParameterfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glPatchParameterfv(uint pname, float* values) => ((delegate* unmanaged[Cdecl]<uint, float*, void>)s_glPatchParameterfv)(pname, values);

	private static void* s_glPatchParameteri;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glPatchParameteri(uint pname, int value) => ((delegate* unmanaged[Cdecl]<uint, int, void>)s_glPatchParameteri)(pname, value);

	private static void* s_glPauseTransformFeedback;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glPauseTransformFeedback() => ((delegate* unmanaged[Cdecl]<void>)s_glPauseTransformFeedback)();

	private static void* s_glPixelStoref;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glPixelStoref(uint pname, float param) => ((delegate* unmanaged[Cdecl]<uint, float, void>)s_glPixelStoref)(pname, param);

	private static void* s_glPixelStorei;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glPixelStorei(uint pname, int param) => ((delegate* unmanaged[Cdecl]<uint, int, void>)s_glPixelStorei)(pname, param);

	private static void* s_glPointParameterf;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glPointParameterf(uint pname, float param) => ((delegate* unmanaged[Cdecl]<uint, float, void>)s_glPointParameterf)(pname, param);

	private static void* s_glPointParameterfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glPointParameterfv(uint pname, float* @params) => ((delegate* unmanaged[Cdecl]<uint, float*, void>)s_glPointParameterfv)(pname, @params);

	private static void* s_glPointParameteri;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glPointParameteri(uint pname, int param) => ((delegate* unmanaged[Cdecl]<uint, int, void>)s_glPointParameteri)(pname, param);

	private static void* s_glPointParameteriv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glPointParameteriv(uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, int*, void>)s_glPointParameteriv)(pname, @params);

	private static void* s_glPointSize;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glPointSize(float size) => ((delegate* unmanaged[Cdecl]<float, void>)s_glPointSize)(size);

	private static void* s_glPolygonMode;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glPolygonMode(uint face, uint mode) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glPolygonMode)(face, mode);

	private static void* s_glPolygonOffset;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glPolygonOffset(float factor, float units) => ((delegate* unmanaged[Cdecl]<float, float, void>)s_glPolygonOffset)(factor, units);

	private static void* s_glPolygonOffsetClamp;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glPolygonOffsetClamp(float factor, float units, float clamp) => ((delegate* unmanaged[Cdecl]<float, float, float, void>)s_glPolygonOffsetClamp)(factor, units, clamp);

	private static void* s_glPopDebugGroup;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glPopDebugGroup() => ((delegate* unmanaged[Cdecl]<void>)s_glPopDebugGroup)();

	private static void* s_glPrimitiveRestartIndex;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glPrimitiveRestartIndex(uint index) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glPrimitiveRestartIndex)(index);

	private static void* s_glProgramBinary;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramBinary(uint program, uint binaryFormat, void* binary, int length) => ((delegate* unmanaged[Cdecl]<uint, uint, void*, int, void>)s_glProgramBinary)(program, binaryFormat, binary, length);

	private static void* s_glProgramParameteri;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramParameteri(uint program, uint pname, int value) => ((delegate* unmanaged[Cdecl]<uint, uint, int, void>)s_glProgramParameteri)(program, pname, value);

	private static void* s_glProgramUniform1d;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform1d(uint program, int location, double v0) => ((delegate* unmanaged[Cdecl]<uint, int, double, void>)s_glProgramUniform1d)(program, location, v0);

	private static void* s_glProgramUniform1dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform1dv(uint program, int location, int count, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, double*, void>)s_glProgramUniform1dv)(program, location, count, value);

	private static void* s_glProgramUniform1f;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform1f(uint program, int location, float v0) => ((delegate* unmanaged[Cdecl]<uint, int, float, void>)s_glProgramUniform1f)(program, location, v0);

	private static void* s_glProgramUniform1fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform1fv(uint program, int location, int count, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, float*, void>)s_glProgramUniform1fv)(program, location, count, value);

	private static void* s_glProgramUniform1i;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform1i(uint program, int location, int v0) => ((delegate* unmanaged[Cdecl]<uint, int, int, void>)s_glProgramUniform1i)(program, location, v0);

	private static void* s_glProgramUniform1iv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform1iv(uint program, int location, int count, int* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, int*, void>)s_glProgramUniform1iv)(program, location, count, value);

	private static void* s_glProgramUniform1ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform1ui(uint program, int location, uint v0) => ((delegate* unmanaged[Cdecl]<uint, int, uint, void>)s_glProgramUniform1ui)(program, location, v0);

	private static void* s_glProgramUniform1uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform1uiv(uint program, int location, int count, uint* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, uint*, void>)s_glProgramUniform1uiv)(program, location, count, value);

	private static void* s_glProgramUniform2d;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform2d(uint program, int location, double v0, double v1) => ((delegate* unmanaged[Cdecl]<uint, int, double, double, void>)s_glProgramUniform2d)(program, location, v0, v1);

	private static void* s_glProgramUniform2dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform2dv(uint program, int location, int count, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, double*, void>)s_glProgramUniform2dv)(program, location, count, value);

	private static void* s_glProgramUniform2f;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform2f(uint program, int location, float v0, float v1) => ((delegate* unmanaged[Cdecl]<uint, int, float, float, void>)s_glProgramUniform2f)(program, location, v0, v1);

	private static void* s_glProgramUniform2fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform2fv(uint program, int location, int count, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, float*, void>)s_glProgramUniform2fv)(program, location, count, value);

	private static void* s_glProgramUniform2i;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform2i(uint program, int location, int v0, int v1) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, void>)s_glProgramUniform2i)(program, location, v0, v1);

	private static void* s_glProgramUniform2iv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform2iv(uint program, int location, int count, int* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, int*, void>)s_glProgramUniform2iv)(program, location, count, value);

	private static void* s_glProgramUniform2ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform2ui(uint program, int location, uint v0, uint v1) => ((delegate* unmanaged[Cdecl]<uint, int, uint, uint, void>)s_glProgramUniform2ui)(program, location, v0, v1);

	private static void* s_glProgramUniform2uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform2uiv(uint program, int location, int count, uint* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, uint*, void>)s_glProgramUniform2uiv)(program, location, count, value);

	private static void* s_glProgramUniform3d;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform3d(uint program, int location, double v0, double v1, double v2) => ((delegate* unmanaged[Cdecl]<uint, int, double, double, double, void>)s_glProgramUniform3d)(program, location, v0, v1, v2);

	private static void* s_glProgramUniform3dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform3dv(uint program, int location, int count, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, double*, void>)s_glProgramUniform3dv)(program, location, count, value);

	private static void* s_glProgramUniform3f;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform3f(uint program, int location, float v0, float v1, float v2) => ((delegate* unmanaged[Cdecl]<uint, int, float, float, float, void>)s_glProgramUniform3f)(program, location, v0, v1, v2);

	private static void* s_glProgramUniform3fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform3fv(uint program, int location, int count, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, float*, void>)s_glProgramUniform3fv)(program, location, count, value);

	private static void* s_glProgramUniform3i;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform3i(uint program, int location, int v0, int v1, int v2) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, void>)s_glProgramUniform3i)(program, location, v0, v1, v2);

	private static void* s_glProgramUniform3iv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform3iv(uint program, int location, int count, int* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, int*, void>)s_glProgramUniform3iv)(program, location, count, value);

	private static void* s_glProgramUniform3ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform3ui(uint program, int location, uint v0, uint v1, uint v2) => ((delegate* unmanaged[Cdecl]<uint, int, uint, uint, uint, void>)s_glProgramUniform3ui)(program, location, v0, v1, v2);

	private static void* s_glProgramUniform3uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform3uiv(uint program, int location, int count, uint* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, uint*, void>)s_glProgramUniform3uiv)(program, location, count, value);

	private static void* s_glProgramUniform4d;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform4d(uint program, int location, double v0, double v1, double v2, double v3) => ((delegate* unmanaged[Cdecl]<uint, int, double, double, double, double, void>)s_glProgramUniform4d)(program, location, v0, v1, v2, v3);

	private static void* s_glProgramUniform4dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform4dv(uint program, int location, int count, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, double*, void>)s_glProgramUniform4dv)(program, location, count, value);

	private static void* s_glProgramUniform4f;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform4f(uint program, int location, float v0, float v1, float v2, float v3) => ((delegate* unmanaged[Cdecl]<uint, int, float, float, float, float, void>)s_glProgramUniform4f)(program, location, v0, v1, v2, v3);

	private static void* s_glProgramUniform4fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform4fv(uint program, int location, int count, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, float*, void>)s_glProgramUniform4fv)(program, location, count, value);

	private static void* s_glProgramUniform4i;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform4i(uint program, int location, int v0, int v1, int v2, int v3) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, void>)s_glProgramUniform4i)(program, location, v0, v1, v2, v3);

	private static void* s_glProgramUniform4iv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform4iv(uint program, int location, int count, int* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, int*, void>)s_glProgramUniform4iv)(program, location, count, value);

	private static void* s_glProgramUniform4ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform4ui(uint program, int location, uint v0, uint v1, uint v2, uint v3) => ((delegate* unmanaged[Cdecl]<uint, int, uint, uint, uint, uint, void>)s_glProgramUniform4ui)(program, location, v0, v1, v2, v3);

	private static void* s_glProgramUniform4uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniform4uiv(uint program, int location, int count, uint* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, uint*, void>)s_glProgramUniform4uiv)(program, location, count, value);

	private static void* s_glProgramUniformMatrix2dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix2dv(uint program, int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, double*, void>)s_glProgramUniformMatrix2dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix2fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix2fv(uint program, int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, float*, void>)s_glProgramUniformMatrix2fv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix2x3dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix2x3dv(uint program, int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, double*, void>)s_glProgramUniformMatrix2x3dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix2x3fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix2x3fv(uint program, int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, float*, void>)s_glProgramUniformMatrix2x3fv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix2x4dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix2x4dv(uint program, int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, double*, void>)s_glProgramUniformMatrix2x4dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix2x4fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix2x4fv(uint program, int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, float*, void>)s_glProgramUniformMatrix2x4fv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix3dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix3dv(uint program, int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, double*, void>)s_glProgramUniformMatrix3dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix3fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix3fv(uint program, int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, float*, void>)s_glProgramUniformMatrix3fv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix3x2dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix3x2dv(uint program, int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, double*, void>)s_glProgramUniformMatrix3x2dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix3x2fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix3x2fv(uint program, int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, float*, void>)s_glProgramUniformMatrix3x2fv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix3x4dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix3x4dv(uint program, int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, double*, void>)s_glProgramUniformMatrix3x4dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix3x4fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix3x4fv(uint program, int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, float*, void>)s_glProgramUniformMatrix3x4fv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix4dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix4dv(uint program, int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, double*, void>)s_glProgramUniformMatrix4dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix4fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix4fv(uint program, int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, float*, void>)s_glProgramUniformMatrix4fv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix4x2dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix4x2dv(uint program, int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, double*, void>)s_glProgramUniformMatrix4x2dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix4x2fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix4x2fv(uint program, int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, float*, void>)s_glProgramUniformMatrix4x2fv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix4x3dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix4x3dv(uint program, int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, double*, void>)s_glProgramUniformMatrix4x3dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix4x3fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProgramUniformMatrix4x3fv(uint program, int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, GLboolean, float*, void>)s_glProgramUniformMatrix4x3fv)(program, location, count, transpose, value);

	private static void* s_glProvokingVertex;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glProvokingVertex(uint mode) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glProvokingVertex)(mode);

	private static void* s_glPushDebugGroup;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glPushDebugGroup(uint source, uint id, int length, byte* message) => ((delegate* unmanaged[Cdecl]<uint, uint, int, byte*, void>)s_glPushDebugGroup)(source, id, length, message);

	private static void* s_glQueryCounter;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glQueryCounter(uint id, uint target) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glQueryCounter)(id, target);

	private static void* s_glReadBuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glReadBuffer(uint src) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glReadBuffer)(src);

	private static void* s_glReadPixels;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glReadPixels(int x, int y, int width, int height, uint format, uint type, void* pixels) => ((delegate* unmanaged[Cdecl]<int, int, int, int, uint, uint, void*, void>)s_glReadPixels)(x, y, width, height, format, type, pixels);

	private static void* s_glReadnPixels;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glReadnPixels(int x, int y, int width, int height, uint format, uint type, int bufSize, void* data) => ((delegate* unmanaged[Cdecl]<int, int, int, int, uint, uint, int, void*, void>)s_glReadnPixels)(x, y, width, height, format, type, bufSize, data);

	private static void* s_glReleaseShaderCompiler;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glReleaseShaderCompiler() => ((delegate* unmanaged[Cdecl]<void>)s_glReleaseShaderCompiler)();

	private static void* s_glRenderbufferStorage;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glRenderbufferStorage(uint target, uint internalformat, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int, void>)s_glRenderbufferStorage)(target, internalformat, width, height);

	private static void* s_glRenderbufferStorageMultisample;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glRenderbufferStorageMultisample(uint target, int samples, uint internalformat, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, void>)s_glRenderbufferStorageMultisample)(target, samples, internalformat, width, height);

	private static void* s_glResumeTransformFeedback;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glResumeTransformFeedback() => ((delegate* unmanaged[Cdecl]<void>)s_glResumeTransformFeedback)();

	private static void* s_glSampleCoverage;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glSampleCoverage(float value, GLboolean invert) => ((delegate* unmanaged[Cdecl]<float, GLboolean, void>)s_glSampleCoverage)(value, invert);

	private static void* s_glSampleMaski;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glSampleMaski(uint maskNumber, uint mask) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glSampleMaski)(maskNumber, mask);

	private static void* s_glSamplerParameterIiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glSamplerParameterIiv(uint sampler, uint pname, int* param) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glSamplerParameterIiv)(sampler, pname, param);

	private static void* s_glSamplerParameterIuiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glSamplerParameterIuiv(uint sampler, uint pname, uint* param) => ((delegate* unmanaged[Cdecl]<uint, uint, uint*, void>)s_glSamplerParameterIuiv)(sampler, pname, param);

	private static void* s_glSamplerParameterf;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glSamplerParameterf(uint sampler, uint pname, float param) => ((delegate* unmanaged[Cdecl]<uint, uint, float, void>)s_glSamplerParameterf)(sampler, pname, param);

	private static void* s_glSamplerParameterfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glSamplerParameterfv(uint sampler, uint pname, float* param) => ((delegate* unmanaged[Cdecl]<uint, uint, float*, void>)s_glSamplerParameterfv)(sampler, pname, param);

	private static void* s_glSamplerParameteri;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glSamplerParameteri(uint sampler, uint pname, int param) => ((delegate* unmanaged[Cdecl]<uint, uint, int, void>)s_glSamplerParameteri)(sampler, pname, param);

	private static void* s_glSamplerParameteriv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glSamplerParameteriv(uint sampler, uint pname, int* param) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glSamplerParameteriv)(sampler, pname, param);

	private static void* s_glScissor;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glScissor(int x, int y, int width, int height) => ((delegate* unmanaged[Cdecl]<int, int, int, int, void>)s_glScissor)(x, y, width, height);

	private static void* s_glScissorArrayv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glScissorArrayv(uint first, int count, int* v) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glScissorArrayv)(first, count, v);

	private static void* s_glScissorIndexed;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glScissorIndexed(uint index, int left, int bottom, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, void>)s_glScissorIndexed)(index, left, bottom, width, height);

	private static void* s_glScissorIndexedv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glScissorIndexedv(uint index, int* v) => ((delegate* unmanaged[Cdecl]<uint, int*, void>)s_glScissorIndexedv)(index, v);

	private static void* s_glSecondaryColorP3ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glSecondaryColorP3ui(uint type, uint color) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glSecondaryColorP3ui)(type, color);

	private static void* s_glSecondaryColorP3uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glSecondaryColorP3uiv(uint type, uint* color) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glSecondaryColorP3uiv)(type, color);

	private static void* s_glShaderBinary;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glShaderBinary(int count, uint* shaders, uint binaryFormat, void* binary, int length) => ((delegate* unmanaged[Cdecl]<int, uint*, uint, void*, int, void>)s_glShaderBinary)(count, shaders, binaryFormat, binary, length);

	private static void* s_glShaderSource;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glShaderSource(uint shader, int count, byte** @string, int* length) => ((delegate* unmanaged[Cdecl]<uint, int, byte**, int*, void>)s_glShaderSource)(shader, count, @string, length);

	private static void* s_glShaderStorageBlockBinding;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glShaderStorageBlockBinding(uint program, uint storageBlockIndex, uint storageBlockBinding) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glShaderStorageBlockBinding)(program, storageBlockIndex, storageBlockBinding);

	private static void* s_glSpecializeShader;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glSpecializeShader(uint shader, byte* pEntryPoint, uint numSpecializationConstants, uint* pConstantIndex, uint* pConstantValue) => ((delegate* unmanaged[Cdecl]<uint, byte*, uint, uint*, uint*, void>)s_glSpecializeShader)(shader, pEntryPoint, numSpecializationConstants, pConstantIndex, pConstantValue);

	private static void* s_glStencilFunc;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glStencilFunc(uint func, int @ref, uint mask) => ((delegate* unmanaged[Cdecl]<uint, int, uint, void>)s_glStencilFunc)(func, @ref, mask);

	private static void* s_glStencilFuncSeparate;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glStencilFuncSeparate(uint face, uint func, int @ref, uint mask) => ((delegate* unmanaged[Cdecl]<uint, uint, int, uint, void>)s_glStencilFuncSeparate)(face, func, @ref, mask);

	private static void* s_glStencilMask;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glStencilMask(uint mask) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glStencilMask)(mask);

	private static void* s_glStencilMaskSeparate;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glStencilMaskSeparate(uint face, uint mask) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glStencilMaskSeparate)(face, mask);

	private static void* s_glStencilOp;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glStencilOp(uint fail, uint zfail, uint zpass) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glStencilOp)(fail, zfail, zpass);

	private static void* s_glStencilOpSeparate;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glStencilOpSeparate(uint face, uint sfail, uint dpfail, uint dppass) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, uint, void>)s_glStencilOpSeparate)(face, sfail, dpfail, dppass);

	private static void* s_glTexBuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexBuffer(uint target, uint internalformat, uint buffer) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glTexBuffer)(target, internalformat, buffer);

	private static void* s_glTexBufferRange;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexBufferRange(uint target, uint internalformat, uint buffer, nint offset, nint size) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, nint, nint, void>)s_glTexBufferRange)(target, internalformat, buffer, offset, size);

	private static void* s_glTexCoordP1ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexCoordP1ui(uint type, uint coords) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glTexCoordP1ui)(type, coords);

	private static void* s_glTexCoordP1uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexCoordP1uiv(uint type, uint* coords) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glTexCoordP1uiv)(type, coords);

	private static void* s_glTexCoordP2ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexCoordP2ui(uint type, uint coords) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glTexCoordP2ui)(type, coords);

	private static void* s_glTexCoordP2uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexCoordP2uiv(uint type, uint* coords) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glTexCoordP2uiv)(type, coords);

	private static void* s_glTexCoordP3ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexCoordP3ui(uint type, uint coords) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glTexCoordP3ui)(type, coords);

	private static void* s_glTexCoordP3uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexCoordP3uiv(uint type, uint* coords) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glTexCoordP3uiv)(type, coords);

	private static void* s_glTexCoordP4ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexCoordP4ui(uint type, uint coords) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glTexCoordP4ui)(type, coords);

	private static void* s_glTexCoordP4uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexCoordP4uiv(uint type, uint* coords) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glTexCoordP4uiv)(type, coords);

	private static void* s_glTexImage1D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexImage1D(uint target, int level, int internalformat, int width, int border, uint format, uint type, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, uint, uint, void*, void>)s_glTexImage1D)(target, level, internalformat, width, border, format, type, pixels);

	private static void* s_glTexImage2D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexImage2D(uint target, int level, int internalformat, int width, int height, int border, uint format, uint type, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, uint, uint, void*, void>)s_glTexImage2D)(target, level, internalformat, width, height, border, format, type, pixels);

	private static void* s_glTexImage2DMultisample;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexImage2DMultisample(uint target, int samples, uint internalformat, int width, int height, GLboolean fixedsamplelocations) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, GLboolean, void>)s_glTexImage2DMultisample)(target, samples, internalformat, width, height, fixedsamplelocations);

	private static void* s_glTexImage3D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexImage3D(uint target, int level, int internalformat, int width, int height, int depth, int border, uint format, uint type, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, uint, uint, void*, void>)s_glTexImage3D)(target, level, internalformat, width, height, depth, border, format, type, pixels);

	private static void* s_glTexImage3DMultisample;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexImage3DMultisample(uint target, int samples, uint internalformat, int width, int height, int depth, GLboolean fixedsamplelocations) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, int, GLboolean, void>)s_glTexImage3DMultisample)(target, samples, internalformat, width, height, depth, fixedsamplelocations);

	private static void* s_glTexParameterIiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexParameterIiv(uint target, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glTexParameterIiv)(target, pname, @params);

	private static void* s_glTexParameterIuiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexParameterIuiv(uint target, uint pname, uint* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, uint*, void>)s_glTexParameterIuiv)(target, pname, @params);

	private static void* s_glTexParameterf;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexParameterf(uint target, uint pname, float param) => ((delegate* unmanaged[Cdecl]<uint, uint, float, void>)s_glTexParameterf)(target, pname, param);

	private static void* s_glTexParameterfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexParameterfv(uint target, uint pname, float* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, float*, void>)s_glTexParameterfv)(target, pname, @params);

	private static void* s_glTexParameteri;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexParameteri(uint target, uint pname, int param) => ((delegate* unmanaged[Cdecl]<uint, uint, int, void>)s_glTexParameteri)(target, pname, param);

	private static void* s_glTexParameteriv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexParameteriv(uint target, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glTexParameteriv)(target, pname, @params);

	private static void* s_glTexStorage1D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexStorage1D(uint target, int levels, uint internalformat, int width) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, void>)s_glTexStorage1D)(target, levels, internalformat, width);

	private static void* s_glTexStorage2D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexStorage2D(uint target, int levels, uint internalformat, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, void>)s_glTexStorage2D)(target, levels, internalformat, width, height);

	private static void* s_glTexStorage2DMultisample;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexStorage2DMultisample(uint target, int samples, uint internalformat, int width, int height, GLboolean fixedsamplelocations) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, GLboolean, void>)s_glTexStorage2DMultisample)(target, samples, internalformat, width, height, fixedsamplelocations);

	private static void* s_glTexStorage3D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexStorage3D(uint target, int levels, uint internalformat, int width, int height, int depth) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, int, void>)s_glTexStorage3D)(target, levels, internalformat, width, height, depth);

	private static void* s_glTexStorage3DMultisample;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexStorage3DMultisample(uint target, int samples, uint internalformat, int width, int height, int depth, GLboolean fixedsamplelocations) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, int, GLboolean, void>)s_glTexStorage3DMultisample)(target, samples, internalformat, width, height, depth, fixedsamplelocations);

	private static void* s_glTexSubImage1D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexSubImage1D(uint target, int level, int xoffset, int width, uint format, uint type, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, uint, uint, void*, void>)s_glTexSubImage1D)(target, level, xoffset, width, format, type, pixels);

	private static void* s_glTexSubImage2D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, uint type, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, uint, uint, void*, void>)s_glTexSubImage2D)(target, level, xoffset, yoffset, width, height, format, type, pixels);

	private static void* s_glTexSubImage3D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTexSubImage3D(uint target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, uint format, uint type, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, uint, uint, void*, void>)s_glTexSubImage3D)(target, level, xoffset, yoffset, zoffset, width, height, depth, format, type, pixels);

	private static void* s_glTextureBarrier;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureBarrier() => ((delegate* unmanaged[Cdecl]<void>)s_glTextureBarrier)();

	private static void* s_glTextureBuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureBuffer(uint texture, uint internalformat, uint buffer) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glTextureBuffer)(texture, internalformat, buffer);

	private static void* s_glTextureBufferRange;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureBufferRange(uint texture, uint internalformat, uint buffer, nint offset, nint size) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, nint, nint, void>)s_glTextureBufferRange)(texture, internalformat, buffer, offset, size);

	private static void* s_glTextureParameterIiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureParameterIiv(uint texture, uint pname, int* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glTextureParameterIiv)(texture, pname, @params);

	private static void* s_glTextureParameterIuiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureParameterIuiv(uint texture, uint pname, uint* @params) => ((delegate* unmanaged[Cdecl]<uint, uint, uint*, void>)s_glTextureParameterIuiv)(texture, pname, @params);

	private static void* s_glTextureParameterf;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureParameterf(uint texture, uint pname, float param) => ((delegate* unmanaged[Cdecl]<uint, uint, float, void>)s_glTextureParameterf)(texture, pname, param);

	private static void* s_glTextureParameterfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureParameterfv(uint texture, uint pname, float* param) => ((delegate* unmanaged[Cdecl]<uint, uint, float*, void>)s_glTextureParameterfv)(texture, pname, param);

	private static void* s_glTextureParameteri;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureParameteri(uint texture, uint pname, int param) => ((delegate* unmanaged[Cdecl]<uint, uint, int, void>)s_glTextureParameteri)(texture, pname, param);

	private static void* s_glTextureParameteriv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureParameteriv(uint texture, uint pname, int* param) => ((delegate* unmanaged[Cdecl]<uint, uint, int*, void>)s_glTextureParameteriv)(texture, pname, param);

	private static void* s_glTextureStorage1D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureStorage1D(uint texture, int levels, uint internalformat, int width) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, void>)s_glTextureStorage1D)(texture, levels, internalformat, width);

	private static void* s_glTextureStorage2D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureStorage2D(uint texture, int levels, uint internalformat, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, void>)s_glTextureStorage2D)(texture, levels, internalformat, width, height);

	private static void* s_glTextureStorage2DMultisample;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureStorage2DMultisample(uint texture, int samples, uint internalformat, int width, int height, GLboolean fixedsamplelocations) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, GLboolean, void>)s_glTextureStorage2DMultisample)(texture, samples, internalformat, width, height, fixedsamplelocations);

	private static void* s_glTextureStorage3D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureStorage3D(uint texture, int levels, uint internalformat, int width, int height, int depth) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, int, void>)s_glTextureStorage3D)(texture, levels, internalformat, width, height, depth);

	private static void* s_glTextureStorage3DMultisample;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureStorage3DMultisample(uint texture, int samples, uint internalformat, int width, int height, int depth, GLboolean fixedsamplelocations) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, int, GLboolean, void>)s_glTextureStorage3DMultisample)(texture, samples, internalformat, width, height, depth, fixedsamplelocations);

	private static void* s_glTextureSubImage1D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureSubImage1D(uint texture, int level, int xoffset, int width, uint format, uint type, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, uint, uint, void*, void>)s_glTextureSubImage1D)(texture, level, xoffset, width, format, type, pixels);

	private static void* s_glTextureSubImage2D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureSubImage2D(uint texture, int level, int xoffset, int yoffset, int width, int height, uint format, uint type, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, uint, uint, void*, void>)s_glTextureSubImage2D)(texture, level, xoffset, yoffset, width, height, format, type, pixels);

	private static void* s_glTextureSubImage3D;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureSubImage3D(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, uint format, uint type, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, uint, uint, void*, void>)s_glTextureSubImage3D)(texture, level, xoffset, yoffset, zoffset, width, height, depth, format, type, pixels);

	private static void* s_glTextureView;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTextureView(uint texture, uint target, uint origtexture, uint internalformat, uint minlevel, uint numlevels, uint minlayer, uint numlayers) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, uint, uint, uint, uint, uint, void>)s_glTextureView)(texture, target, origtexture, internalformat, minlevel, numlevels, minlayer, numlayers);

	private static void* s_glTransformFeedbackBufferBase;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTransformFeedbackBufferBase(uint xfb, uint index, uint buffer) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glTransformFeedbackBufferBase)(xfb, index, buffer);

	private static void* s_glTransformFeedbackBufferRange;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTransformFeedbackBufferRange(uint xfb, uint index, uint buffer, nint offset, nint size) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, nint, nint, void>)s_glTransformFeedbackBufferRange)(xfb, index, buffer, offset, size);

	private static void* s_glTransformFeedbackVaryings;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glTransformFeedbackVaryings(uint program, int count, byte** varyings, uint bufferMode) => ((delegate* unmanaged[Cdecl]<uint, int, byte**, uint, void>)s_glTransformFeedbackVaryings)(program, count, varyings, bufferMode);

	private static void* s_glUniform1d;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform1d(int location, double x) => ((delegate* unmanaged[Cdecl]<int, double, void>)s_glUniform1d)(location, x);

	private static void* s_glUniform1dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform1dv(int location, int count, double* value) => ((delegate* unmanaged[Cdecl]<int, int, double*, void>)s_glUniform1dv)(location, count, value);

	private static void* s_glUniform1f;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform1f(int location, float v0) => ((delegate* unmanaged[Cdecl]<int, float, void>)s_glUniform1f)(location, v0);

	private static void* s_glUniform1fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform1fv(int location, int count, float* value) => ((delegate* unmanaged[Cdecl]<int, int, float*, void>)s_glUniform1fv)(location, count, value);

	private static void* s_glUniform1i;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform1i(int location, int v0) => ((delegate* unmanaged[Cdecl]<int, int, void>)s_glUniform1i)(location, v0);

	private static void* s_glUniform1iv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform1iv(int location, int count, int* value) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glUniform1iv)(location, count, value);

	private static void* s_glUniform1ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform1ui(int location, uint v0) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glUniform1ui)(location, v0);

	private static void* s_glUniform1uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform1uiv(int location, int count, uint* value) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glUniform1uiv)(location, count, value);

	private static void* s_glUniform2d;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform2d(int location, double x, double y) => ((delegate* unmanaged[Cdecl]<int, double, double, void>)s_glUniform2d)(location, x, y);

	private static void* s_glUniform2dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform2dv(int location, int count, double* value) => ((delegate* unmanaged[Cdecl]<int, int, double*, void>)s_glUniform2dv)(location, count, value);

	private static void* s_glUniform2f;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform2f(int location, float v0, float v1) => ((delegate* unmanaged[Cdecl]<int, float, float, void>)s_glUniform2f)(location, v0, v1);

	private static void* s_glUniform2fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform2fv(int location, int count, float* value) => ((delegate* unmanaged[Cdecl]<int, int, float*, void>)s_glUniform2fv)(location, count, value);

	private static void* s_glUniform2i;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform2i(int location, int v0, int v1) => ((delegate* unmanaged[Cdecl]<int, int, int, void>)s_glUniform2i)(location, v0, v1);

	private static void* s_glUniform2iv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform2iv(int location, int count, int* value) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glUniform2iv)(location, count, value);

	private static void* s_glUniform2ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform2ui(int location, uint v0, uint v1) => ((delegate* unmanaged[Cdecl]<int, uint, uint, void>)s_glUniform2ui)(location, v0, v1);

	private static void* s_glUniform2uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform2uiv(int location, int count, uint* value) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glUniform2uiv)(location, count, value);

	private static void* s_glUniform3d;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform3d(int location, double x, double y, double z) => ((delegate* unmanaged[Cdecl]<int, double, double, double, void>)s_glUniform3d)(location, x, y, z);

	private static void* s_glUniform3dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform3dv(int location, int count, double* value) => ((delegate* unmanaged[Cdecl]<int, int, double*, void>)s_glUniform3dv)(location, count, value);

	private static void* s_glUniform3f;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform3f(int location, float v0, float v1, float v2) => ((delegate* unmanaged[Cdecl]<int, float, float, float, void>)s_glUniform3f)(location, v0, v1, v2);

	private static void* s_glUniform3fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform3fv(int location, int count, float* value) => ((delegate* unmanaged[Cdecl]<int, int, float*, void>)s_glUniform3fv)(location, count, value);

	private static void* s_glUniform3i;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform3i(int location, int v0, int v1, int v2) => ((delegate* unmanaged[Cdecl]<int, int, int, int, void>)s_glUniform3i)(location, v0, v1, v2);

	private static void* s_glUniform3iv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform3iv(int location, int count, int* value) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glUniform3iv)(location, count, value);

	private static void* s_glUniform3ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform3ui(int location, uint v0, uint v1, uint v2) => ((delegate* unmanaged[Cdecl]<int, uint, uint, uint, void>)s_glUniform3ui)(location, v0, v1, v2);

	private static void* s_glUniform3uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform3uiv(int location, int count, uint* value) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glUniform3uiv)(location, count, value);

	private static void* s_glUniform4d;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform4d(int location, double x, double y, double z, double w) => ((delegate* unmanaged[Cdecl]<int, double, double, double, double, void>)s_glUniform4d)(location, x, y, z, w);

	private static void* s_glUniform4dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform4dv(int location, int count, double* value) => ((delegate* unmanaged[Cdecl]<int, int, double*, void>)s_glUniform4dv)(location, count, value);

	private static void* s_glUniform4f;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform4f(int location, float v0, float v1, float v2, float v3) => ((delegate* unmanaged[Cdecl]<int, float, float, float, float, void>)s_glUniform4f)(location, v0, v1, v2, v3);

	private static void* s_glUniform4fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform4fv(int location, int count, float* value) => ((delegate* unmanaged[Cdecl]<int, int, float*, void>)s_glUniform4fv)(location, count, value);

	private static void* s_glUniform4i;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform4i(int location, int v0, int v1, int v2, int v3) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, void>)s_glUniform4i)(location, v0, v1, v2, v3);

	private static void* s_glUniform4iv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform4iv(int location, int count, int* value) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glUniform4iv)(location, count, value);

	private static void* s_glUniform4ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform4ui(int location, uint v0, uint v1, uint v2, uint v3) => ((delegate* unmanaged[Cdecl]<int, uint, uint, uint, uint, void>)s_glUniform4ui)(location, v0, v1, v2, v3);

	private static void* s_glUniform4uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniform4uiv(int location, int count, uint* value) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glUniform4uiv)(location, count, value);

	private static void* s_glUniformBlockBinding;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformBlockBinding(uint program, uint uniformBlockIndex, uint uniformBlockBinding) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glUniformBlockBinding)(program, uniformBlockIndex, uniformBlockBinding);

	private static void* s_glUniformMatrix2dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix2dv(int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, double*, void>)s_glUniformMatrix2dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix2fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix2fv(int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, float*, void>)s_glUniformMatrix2fv)(location, count, transpose, value);

	private static void* s_glUniformMatrix2x3dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix2x3dv(int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, double*, void>)s_glUniformMatrix2x3dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix2x3fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix2x3fv(int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, float*, void>)s_glUniformMatrix2x3fv)(location, count, transpose, value);

	private static void* s_glUniformMatrix2x4dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix2x4dv(int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, double*, void>)s_glUniformMatrix2x4dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix2x4fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix2x4fv(int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, float*, void>)s_glUniformMatrix2x4fv)(location, count, transpose, value);

	private static void* s_glUniformMatrix3dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix3dv(int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, double*, void>)s_glUniformMatrix3dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix3fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix3fv(int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, float*, void>)s_glUniformMatrix3fv)(location, count, transpose, value);

	private static void* s_glUniformMatrix3x2dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix3x2dv(int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, double*, void>)s_glUniformMatrix3x2dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix3x2fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix3x2fv(int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, float*, void>)s_glUniformMatrix3x2fv)(location, count, transpose, value);

	private static void* s_glUniformMatrix3x4dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix3x4dv(int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, double*, void>)s_glUniformMatrix3x4dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix3x4fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix3x4fv(int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, float*, void>)s_glUniformMatrix3x4fv)(location, count, transpose, value);

	private static void* s_glUniformMatrix4dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix4dv(int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, double*, void>)s_glUniformMatrix4dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix4fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix4fv(int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, float*, void>)s_glUniformMatrix4fv)(location, count, transpose, value);

	private static void* s_glUniformMatrix4x2dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix4x2dv(int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, double*, void>)s_glUniformMatrix4x2dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix4x2fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix4x2fv(int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, float*, void>)s_glUniformMatrix4x2fv)(location, count, transpose, value);

	private static void* s_glUniformMatrix4x3dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix4x3dv(int location, int count, GLboolean transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, double*, void>)s_glUniformMatrix4x3dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix4x3fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformMatrix4x3fv(int location, int count, GLboolean transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, GLboolean, float*, void>)s_glUniformMatrix4x3fv)(location, count, transpose, value);

	private static void* s_glUniformSubroutinesuiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUniformSubroutinesuiv(uint shadertype, int count, uint* indices) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glUniformSubroutinesuiv)(shadertype, count, indices);

	private static void* s_glUnmapBuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static GLboolean glUnmapBuffer(uint target) => ((delegate* unmanaged[Cdecl]<uint, GLboolean>)s_glUnmapBuffer)(target);

	private static void* s_glUnmapNamedBuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static GLboolean glUnmapNamedBuffer(uint buffer) => ((delegate* unmanaged[Cdecl]<uint, GLboolean>)s_glUnmapNamedBuffer)(buffer);

	private static void* s_glUseProgram;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUseProgram(uint program) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glUseProgram)(program);

	private static void* s_glUseProgramStages;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glUseProgramStages(uint pipeline, uint stages, uint program) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glUseProgramStages)(pipeline, stages, program);

	private static void* s_glValidateProgram;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glValidateProgram(uint program) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glValidateProgram)(program);

	private static void* s_glValidateProgramPipeline;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glValidateProgramPipeline(uint pipeline) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glValidateProgramPipeline)(pipeline);

	private static void* s_glVertexArrayAttribBinding;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexArrayAttribBinding(uint vaobj, uint attribindex, uint bindingindex) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glVertexArrayAttribBinding)(vaobj, attribindex, bindingindex);

	private static void* s_glVertexArrayAttribFormat;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexArrayAttribFormat(uint vaobj, uint attribindex, int size, uint type, GLboolean normalized, uint relativeoffset) => ((delegate* unmanaged[Cdecl]<uint, uint, int, uint, GLboolean, uint, void>)s_glVertexArrayAttribFormat)(vaobj, attribindex, size, type, normalized, relativeoffset);

	private static void* s_glVertexArrayAttribIFormat;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexArrayAttribIFormat(uint vaobj, uint attribindex, int size, uint type, uint relativeoffset) => ((delegate* unmanaged[Cdecl]<uint, uint, int, uint, uint, void>)s_glVertexArrayAttribIFormat)(vaobj, attribindex, size, type, relativeoffset);

	private static void* s_glVertexArrayAttribLFormat;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexArrayAttribLFormat(uint vaobj, uint attribindex, int size, uint type, uint relativeoffset) => ((delegate* unmanaged[Cdecl]<uint, uint, int, uint, uint, void>)s_glVertexArrayAttribLFormat)(vaobj, attribindex, size, type, relativeoffset);

	private static void* s_glVertexArrayBindingDivisor;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexArrayBindingDivisor(uint vaobj, uint bindingindex, uint divisor) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glVertexArrayBindingDivisor)(vaobj, bindingindex, divisor);

	private static void* s_glVertexArrayElementBuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexArrayElementBuffer(uint vaobj, uint buffer) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glVertexArrayElementBuffer)(vaobj, buffer);

	private static void* s_glVertexArrayVertexBuffer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexArrayVertexBuffer(uint vaobj, uint bindingindex, uint buffer, nint offset, int stride) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, nint, int, void>)s_glVertexArrayVertexBuffer)(vaobj, bindingindex, buffer, offset, stride);

	private static void* s_glVertexArrayVertexBuffers;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexArrayVertexBuffers(uint vaobj, uint first, int count, uint* buffers, nint* offsets, int* strides) => ((delegate* unmanaged[Cdecl]<uint, uint, int, uint*, nint*, int*, void>)s_glVertexArrayVertexBuffers)(vaobj, first, count, buffers, offsets, strides);

	private static void* s_glVertexAttrib1d;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib1d(uint index, double x) => ((delegate* unmanaged[Cdecl]<uint, double, void>)s_glVertexAttrib1d)(index, x);

	private static void* s_glVertexAttrib1dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib1dv(uint index, double* v) => ((delegate* unmanaged[Cdecl]<uint, double*, void>)s_glVertexAttrib1dv)(index, v);

	private static void* s_glVertexAttrib1f;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib1f(uint index, float x) => ((delegate* unmanaged[Cdecl]<uint, float, void>)s_glVertexAttrib1f)(index, x);

	private static void* s_glVertexAttrib1fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib1fv(uint index, float* v) => ((delegate* unmanaged[Cdecl]<uint, float*, void>)s_glVertexAttrib1fv)(index, v);

	private static void* s_glVertexAttrib1s;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib1s(uint index, short x) => ((delegate* unmanaged[Cdecl]<uint, short, void>)s_glVertexAttrib1s)(index, x);

	private static void* s_glVertexAttrib1sv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib1sv(uint index, short* v) => ((delegate* unmanaged[Cdecl]<uint, short*, void>)s_glVertexAttrib1sv)(index, v);

	private static void* s_glVertexAttrib2d;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib2d(uint index, double x, double y) => ((delegate* unmanaged[Cdecl]<uint, double, double, void>)s_glVertexAttrib2d)(index, x, y);

	private static void* s_glVertexAttrib2dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib2dv(uint index, double* v) => ((delegate* unmanaged[Cdecl]<uint, double*, void>)s_glVertexAttrib2dv)(index, v);

	private static void* s_glVertexAttrib2f;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib2f(uint index, float x, float y) => ((delegate* unmanaged[Cdecl]<uint, float, float, void>)s_glVertexAttrib2f)(index, x, y);

	private static void* s_glVertexAttrib2fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib2fv(uint index, float* v) => ((delegate* unmanaged[Cdecl]<uint, float*, void>)s_glVertexAttrib2fv)(index, v);

	private static void* s_glVertexAttrib2s;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib2s(uint index, short x, short y) => ((delegate* unmanaged[Cdecl]<uint, short, short, void>)s_glVertexAttrib2s)(index, x, y);

	private static void* s_glVertexAttrib2sv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib2sv(uint index, short* v) => ((delegate* unmanaged[Cdecl]<uint, short*, void>)s_glVertexAttrib2sv)(index, v);

	private static void* s_glVertexAttrib3d;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib3d(uint index, double x, double y, double z) => ((delegate* unmanaged[Cdecl]<uint, double, double, double, void>)s_glVertexAttrib3d)(index, x, y, z);

	private static void* s_glVertexAttrib3dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib3dv(uint index, double* v) => ((delegate* unmanaged[Cdecl]<uint, double*, void>)s_glVertexAttrib3dv)(index, v);

	private static void* s_glVertexAttrib3f;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib3f(uint index, float x, float y, float z) => ((delegate* unmanaged[Cdecl]<uint, float, float, float, void>)s_glVertexAttrib3f)(index, x, y, z);

	private static void* s_glVertexAttrib3fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib3fv(uint index, float* v) => ((delegate* unmanaged[Cdecl]<uint, float*, void>)s_glVertexAttrib3fv)(index, v);

	private static void* s_glVertexAttrib3s;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib3s(uint index, short x, short y, short z) => ((delegate* unmanaged[Cdecl]<uint, short, short, short, void>)s_glVertexAttrib3s)(index, x, y, z);

	private static void* s_glVertexAttrib3sv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib3sv(uint index, short* v) => ((delegate* unmanaged[Cdecl]<uint, short*, void>)s_glVertexAttrib3sv)(index, v);

	private static void* s_glVertexAttrib4Nbv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4Nbv(uint index, sbyte* v) => ((delegate* unmanaged[Cdecl]<uint, sbyte*, void>)s_glVertexAttrib4Nbv)(index, v);

	private static void* s_glVertexAttrib4Niv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4Niv(uint index, int* v) => ((delegate* unmanaged[Cdecl]<uint, int*, void>)s_glVertexAttrib4Niv)(index, v);

	private static void* s_glVertexAttrib4Nsv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4Nsv(uint index, short* v) => ((delegate* unmanaged[Cdecl]<uint, short*, void>)s_glVertexAttrib4Nsv)(index, v);

	private static void* s_glVertexAttrib4Nub;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4Nub(uint index, byte x, byte y, byte z, byte w) => ((delegate* unmanaged[Cdecl]<uint, byte, byte, byte, byte, void>)s_glVertexAttrib4Nub)(index, x, y, z, w);

	private static void* s_glVertexAttrib4Nubv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4Nubv(uint index, byte* v) => ((delegate* unmanaged[Cdecl]<uint, byte*, void>)s_glVertexAttrib4Nubv)(index, v);

	private static void* s_glVertexAttrib4Nuiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4Nuiv(uint index, uint* v) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glVertexAttrib4Nuiv)(index, v);

	private static void* s_glVertexAttrib4Nusv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4Nusv(uint index, ushort* v) => ((delegate* unmanaged[Cdecl]<uint, ushort*, void>)s_glVertexAttrib4Nusv)(index, v);

	private static void* s_glVertexAttrib4bv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4bv(uint index, sbyte* v) => ((delegate* unmanaged[Cdecl]<uint, sbyte*, void>)s_glVertexAttrib4bv)(index, v);

	private static void* s_glVertexAttrib4d;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4d(uint index, double x, double y, double z, double w) => ((delegate* unmanaged[Cdecl]<uint, double, double, double, double, void>)s_glVertexAttrib4d)(index, x, y, z, w);

	private static void* s_glVertexAttrib4dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4dv(uint index, double* v) => ((delegate* unmanaged[Cdecl]<uint, double*, void>)s_glVertexAttrib4dv)(index, v);

	private static void* s_glVertexAttrib4f;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4f(uint index, float x, float y, float z, float w) => ((delegate* unmanaged[Cdecl]<uint, float, float, float, float, void>)s_glVertexAttrib4f)(index, x, y, z, w);

	private static void* s_glVertexAttrib4fv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4fv(uint index, float* v) => ((delegate* unmanaged[Cdecl]<uint, float*, void>)s_glVertexAttrib4fv)(index, v);

	private static void* s_glVertexAttrib4iv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4iv(uint index, int* v) => ((delegate* unmanaged[Cdecl]<uint, int*, void>)s_glVertexAttrib4iv)(index, v);

	private static void* s_glVertexAttrib4s;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4s(uint index, short x, short y, short z, short w) => ((delegate* unmanaged[Cdecl]<uint, short, short, short, short, void>)s_glVertexAttrib4s)(index, x, y, z, w);

	private static void* s_glVertexAttrib4sv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4sv(uint index, short* v) => ((delegate* unmanaged[Cdecl]<uint, short*, void>)s_glVertexAttrib4sv)(index, v);

	private static void* s_glVertexAttrib4ubv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4ubv(uint index, byte* v) => ((delegate* unmanaged[Cdecl]<uint, byte*, void>)s_glVertexAttrib4ubv)(index, v);

	private static void* s_glVertexAttrib4uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4uiv(uint index, uint* v) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glVertexAttrib4uiv)(index, v);

	private static void* s_glVertexAttrib4usv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttrib4usv(uint index, ushort* v) => ((delegate* unmanaged[Cdecl]<uint, ushort*, void>)s_glVertexAttrib4usv)(index, v);

	private static void* s_glVertexAttribBinding;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribBinding(uint attribindex, uint bindingindex) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glVertexAttribBinding)(attribindex, bindingindex);

	private static void* s_glVertexAttribDivisor;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribDivisor(uint index, uint divisor) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glVertexAttribDivisor)(index, divisor);

	private static void* s_glVertexAttribFormat;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribFormat(uint attribindex, int size, uint type, GLboolean normalized, uint relativeoffset) => ((delegate* unmanaged[Cdecl]<uint, int, uint, GLboolean, uint, void>)s_glVertexAttribFormat)(attribindex, size, type, normalized, relativeoffset);

	private static void* s_glVertexAttribI1i;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI1i(uint index, int x) => ((delegate* unmanaged[Cdecl]<uint, int, void>)s_glVertexAttribI1i)(index, x);

	private static void* s_glVertexAttribI1iv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI1iv(uint index, int* v) => ((delegate* unmanaged[Cdecl]<uint, int*, void>)s_glVertexAttribI1iv)(index, v);

	private static void* s_glVertexAttribI1ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI1ui(uint index, uint x) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glVertexAttribI1ui)(index, x);

	private static void* s_glVertexAttribI1uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI1uiv(uint index, uint* v) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glVertexAttribI1uiv)(index, v);

	private static void* s_glVertexAttribI2i;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI2i(uint index, int x, int y) => ((delegate* unmanaged[Cdecl]<uint, int, int, void>)s_glVertexAttribI2i)(index, x, y);

	private static void* s_glVertexAttribI2iv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI2iv(uint index, int* v) => ((delegate* unmanaged[Cdecl]<uint, int*, void>)s_glVertexAttribI2iv)(index, v);

	private static void* s_glVertexAttribI2ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI2ui(uint index, uint x, uint y) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glVertexAttribI2ui)(index, x, y);

	private static void* s_glVertexAttribI2uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI2uiv(uint index, uint* v) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glVertexAttribI2uiv)(index, v);

	private static void* s_glVertexAttribI3i;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI3i(uint index, int x, int y, int z) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, void>)s_glVertexAttribI3i)(index, x, y, z);

	private static void* s_glVertexAttribI3iv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI3iv(uint index, int* v) => ((delegate* unmanaged[Cdecl]<uint, int*, void>)s_glVertexAttribI3iv)(index, v);

	private static void* s_glVertexAttribI3ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI3ui(uint index, uint x, uint y, uint z) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, uint, void>)s_glVertexAttribI3ui)(index, x, y, z);

	private static void* s_glVertexAttribI3uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI3uiv(uint index, uint* v) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glVertexAttribI3uiv)(index, v);

	private static void* s_glVertexAttribI4bv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI4bv(uint index, sbyte* v) => ((delegate* unmanaged[Cdecl]<uint, sbyte*, void>)s_glVertexAttribI4bv)(index, v);

	private static void* s_glVertexAttribI4i;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI4i(uint index, int x, int y, int z, int w) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, void>)s_glVertexAttribI4i)(index, x, y, z, w);

	private static void* s_glVertexAttribI4iv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI4iv(uint index, int* v) => ((delegate* unmanaged[Cdecl]<uint, int*, void>)s_glVertexAttribI4iv)(index, v);

	private static void* s_glVertexAttribI4sv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI4sv(uint index, short* v) => ((delegate* unmanaged[Cdecl]<uint, short*, void>)s_glVertexAttribI4sv)(index, v);

	private static void* s_glVertexAttribI4ubv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI4ubv(uint index, byte* v) => ((delegate* unmanaged[Cdecl]<uint, byte*, void>)s_glVertexAttribI4ubv)(index, v);

	private static void* s_glVertexAttribI4ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI4ui(uint index, uint x, uint y, uint z, uint w) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, uint, uint, void>)s_glVertexAttribI4ui)(index, x, y, z, w);

	private static void* s_glVertexAttribI4uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI4uiv(uint index, uint* v) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glVertexAttribI4uiv)(index, v);

	private static void* s_glVertexAttribI4usv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribI4usv(uint index, ushort* v) => ((delegate* unmanaged[Cdecl]<uint, ushort*, void>)s_glVertexAttribI4usv)(index, v);

	private static void* s_glVertexAttribIFormat;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribIFormat(uint attribindex, int size, uint type, uint relativeoffset) => ((delegate* unmanaged[Cdecl]<uint, int, uint, uint, void>)s_glVertexAttribIFormat)(attribindex, size, type, relativeoffset);

	private static void* s_glVertexAttribIPointer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribIPointer(uint index, int size, uint type, int stride, void* pointer) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, void*, void>)s_glVertexAttribIPointer)(index, size, type, stride, pointer);

	private static void* s_glVertexAttribL1d;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribL1d(uint index, double x) => ((delegate* unmanaged[Cdecl]<uint, double, void>)s_glVertexAttribL1d)(index, x);

	private static void* s_glVertexAttribL1dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribL1dv(uint index, double* v) => ((delegate* unmanaged[Cdecl]<uint, double*, void>)s_glVertexAttribL1dv)(index, v);

	private static void* s_glVertexAttribL2d;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribL2d(uint index, double x, double y) => ((delegate* unmanaged[Cdecl]<uint, double, double, void>)s_glVertexAttribL2d)(index, x, y);

	private static void* s_glVertexAttribL2dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribL2dv(uint index, double* v) => ((delegate* unmanaged[Cdecl]<uint, double*, void>)s_glVertexAttribL2dv)(index, v);

	private static void* s_glVertexAttribL3d;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribL3d(uint index, double x, double y, double z) => ((delegate* unmanaged[Cdecl]<uint, double, double, double, void>)s_glVertexAttribL3d)(index, x, y, z);

	private static void* s_glVertexAttribL3dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribL3dv(uint index, double* v) => ((delegate* unmanaged[Cdecl]<uint, double*, void>)s_glVertexAttribL3dv)(index, v);

	private static void* s_glVertexAttribL4d;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribL4d(uint index, double x, double y, double z, double w) => ((delegate* unmanaged[Cdecl]<uint, double, double, double, double, void>)s_glVertexAttribL4d)(index, x, y, z, w);

	private static void* s_glVertexAttribL4dv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribL4dv(uint index, double* v) => ((delegate* unmanaged[Cdecl]<uint, double*, void>)s_glVertexAttribL4dv)(index, v);

	private static void* s_glVertexAttribLFormat;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribLFormat(uint attribindex, int size, uint type, uint relativeoffset) => ((delegate* unmanaged[Cdecl]<uint, int, uint, uint, void>)s_glVertexAttribLFormat)(attribindex, size, type, relativeoffset);

	private static void* s_glVertexAttribLPointer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribLPointer(uint index, int size, uint type, int stride, void* pointer) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, void*, void>)s_glVertexAttribLPointer)(index, size, type, stride, pointer);

	private static void* s_glVertexAttribP1ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribP1ui(uint index, uint type, GLboolean normalized, uint value) => ((delegate* unmanaged[Cdecl]<uint, uint, GLboolean, uint, void>)s_glVertexAttribP1ui)(index, type, normalized, value);

	private static void* s_glVertexAttribP1uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribP1uiv(uint index, uint type, GLboolean normalized, uint* value) => ((delegate* unmanaged[Cdecl]<uint, uint, GLboolean, uint*, void>)s_glVertexAttribP1uiv)(index, type, normalized, value);

	private static void* s_glVertexAttribP2ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribP2ui(uint index, uint type, GLboolean normalized, uint value) => ((delegate* unmanaged[Cdecl]<uint, uint, GLboolean, uint, void>)s_glVertexAttribP2ui)(index, type, normalized, value);

	private static void* s_glVertexAttribP2uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribP2uiv(uint index, uint type, GLboolean normalized, uint* value) => ((delegate* unmanaged[Cdecl]<uint, uint, GLboolean, uint*, void>)s_glVertexAttribP2uiv)(index, type, normalized, value);

	private static void* s_glVertexAttribP3ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribP3ui(uint index, uint type, GLboolean normalized, uint value) => ((delegate* unmanaged[Cdecl]<uint, uint, GLboolean, uint, void>)s_glVertexAttribP3ui)(index, type, normalized, value);

	private static void* s_glVertexAttribP3uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribP3uiv(uint index, uint type, GLboolean normalized, uint* value) => ((delegate* unmanaged[Cdecl]<uint, uint, GLboolean, uint*, void>)s_glVertexAttribP3uiv)(index, type, normalized, value);

	private static void* s_glVertexAttribP4ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribP4ui(uint index, uint type, GLboolean normalized, uint value) => ((delegate* unmanaged[Cdecl]<uint, uint, GLboolean, uint, void>)s_glVertexAttribP4ui)(index, type, normalized, value);

	private static void* s_glVertexAttribP4uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribP4uiv(uint index, uint type, GLboolean normalized, uint* value) => ((delegate* unmanaged[Cdecl]<uint, uint, GLboolean, uint*, void>)s_glVertexAttribP4uiv)(index, type, normalized, value);

	private static void* s_glVertexAttribPointer;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexAttribPointer(uint index, int size, uint type, GLboolean normalized, int stride, void* pointer) => ((delegate* unmanaged[Cdecl]<uint, int, uint, GLboolean, int, void*, void>)s_glVertexAttribPointer)(index, size, type, normalized, stride, pointer);

	private static void* s_glVertexBindingDivisor;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexBindingDivisor(uint bindingindex, uint divisor) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glVertexBindingDivisor)(bindingindex, divisor);

	private static void* s_glVertexP2ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexP2ui(uint type, uint value) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glVertexP2ui)(type, value);

	private static void* s_glVertexP2uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexP2uiv(uint type, uint* value) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glVertexP2uiv)(type, value);

	private static void* s_glVertexP3ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexP3ui(uint type, uint value) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glVertexP3ui)(type, value);

	private static void* s_glVertexP3uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexP3uiv(uint type, uint* value) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glVertexP3uiv)(type, value);

	private static void* s_glVertexP4ui;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexP4ui(uint type, uint value) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glVertexP4ui)(type, value);

	private static void* s_glVertexP4uiv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glVertexP4uiv(uint type, uint* value) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glVertexP4uiv)(type, value);

	private static void* s_glViewport;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glViewport(int x, int y, int width, int height) => ((delegate* unmanaged[Cdecl]<int, int, int, int, void>)s_glViewport)(x, y, width, height);

	private static void* s_glViewportArrayv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glViewportArrayv(uint first, int count, float* v) => ((delegate* unmanaged[Cdecl]<uint, int, float*, void>)s_glViewportArrayv)(first, count, v);

	private static void* s_glViewportIndexedf;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glViewportIndexedf(uint index, float x, float y, float w, float h) => ((delegate* unmanaged[Cdecl]<uint, float, float, float, float, void>)s_glViewportIndexedf)(index, x, y, w, h);

	private static void* s_glViewportIndexedfv;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glViewportIndexedfv(uint index, float* v) => ((delegate* unmanaged[Cdecl]<uint, float*, void>)s_glViewportIndexedfv)(index, v);

	private static void* s_glWaitSync;
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void glWaitSync(nint sync, uint flags, ulong timeout) => ((delegate* unmanaged[Cdecl]<nint, uint, ulong, void>)s_glWaitSync)(sync, flags, timeout);

	public static void Import(GetProcAddressDelegate getProcAddress)
	{
		var missing = new System.Collections.Generic.List<string>();
		s_glActiveShaderProgram = (void*)getProcAddress("glActiveShaderProgram");
		if (s_glActiveShaderProgram == null) missing.Add("glActiveShaderProgram");
		s_glActiveTexture = (void*)getProcAddress("glActiveTexture");
		if (s_glActiveTexture == null) missing.Add("glActiveTexture");
		s_glAttachShader = (void*)getProcAddress("glAttachShader");
		if (s_glAttachShader == null) missing.Add("glAttachShader");
		s_glBeginConditionalRender = (void*)getProcAddress("glBeginConditionalRender");
		if (s_glBeginConditionalRender == null) missing.Add("glBeginConditionalRender");
		s_glBeginQuery = (void*)getProcAddress("glBeginQuery");
		if (s_glBeginQuery == null) missing.Add("glBeginQuery");
		s_glBeginQueryIndexed = (void*)getProcAddress("glBeginQueryIndexed");
		if (s_glBeginQueryIndexed == null) missing.Add("glBeginQueryIndexed");
		s_glBeginTransformFeedback = (void*)getProcAddress("glBeginTransformFeedback");
		if (s_glBeginTransformFeedback == null) missing.Add("glBeginTransformFeedback");
		s_glBindAttribLocation = (void*)getProcAddress("glBindAttribLocation");
		if (s_glBindAttribLocation == null) missing.Add("glBindAttribLocation");
		s_glBindBuffer = (void*)getProcAddress("glBindBuffer");
		if (s_glBindBuffer == null) missing.Add("glBindBuffer");
		s_glBindBufferBase = (void*)getProcAddress("glBindBufferBase");
		if (s_glBindBufferBase == null) missing.Add("glBindBufferBase");
		s_glBindBufferRange = (void*)getProcAddress("glBindBufferRange");
		if (s_glBindBufferRange == null) missing.Add("glBindBufferRange");
		s_glBindBuffersBase = (void*)getProcAddress("glBindBuffersBase");
		if (s_glBindBuffersBase == null) missing.Add("glBindBuffersBase");
		s_glBindBuffersRange = (void*)getProcAddress("glBindBuffersRange");
		if (s_glBindBuffersRange == null) missing.Add("glBindBuffersRange");
		s_glBindFragDataLocation = (void*)getProcAddress("glBindFragDataLocation");
		if (s_glBindFragDataLocation == null) missing.Add("glBindFragDataLocation");
		s_glBindFragDataLocationIndexed = (void*)getProcAddress("glBindFragDataLocationIndexed");
		if (s_glBindFragDataLocationIndexed == null) missing.Add("glBindFragDataLocationIndexed");
		s_glBindFramebuffer = (void*)getProcAddress("glBindFramebuffer");
		if (s_glBindFramebuffer == null) missing.Add("glBindFramebuffer");
		s_glBindImageTexture = (void*)getProcAddress("glBindImageTexture");
		if (s_glBindImageTexture == null) missing.Add("glBindImageTexture");
		s_glBindImageTextures = (void*)getProcAddress("glBindImageTextures");
		if (s_glBindImageTextures == null) missing.Add("glBindImageTextures");
		s_glBindProgramPipeline = (void*)getProcAddress("glBindProgramPipeline");
		if (s_glBindProgramPipeline == null) missing.Add("glBindProgramPipeline");
		s_glBindRenderbuffer = (void*)getProcAddress("glBindRenderbuffer");
		if (s_glBindRenderbuffer == null) missing.Add("glBindRenderbuffer");
		s_glBindSampler = (void*)getProcAddress("glBindSampler");
		if (s_glBindSampler == null) missing.Add("glBindSampler");
		s_glBindSamplers = (void*)getProcAddress("glBindSamplers");
		if (s_glBindSamplers == null) missing.Add("glBindSamplers");
		s_glBindTexture = (void*)getProcAddress("glBindTexture");
		if (s_glBindTexture == null) missing.Add("glBindTexture");
		s_glBindTextureUnit = (void*)getProcAddress("glBindTextureUnit");
		if (s_glBindTextureUnit == null) missing.Add("glBindTextureUnit");
		s_glBindTextures = (void*)getProcAddress("glBindTextures");
		if (s_glBindTextures == null) missing.Add("glBindTextures");
		s_glBindTransformFeedback = (void*)getProcAddress("glBindTransformFeedback");
		if (s_glBindTransformFeedback == null) missing.Add("glBindTransformFeedback");
		s_glBindVertexArray = (void*)getProcAddress("glBindVertexArray");
		if (s_glBindVertexArray == null) missing.Add("glBindVertexArray");
		s_glBindVertexBuffer = (void*)getProcAddress("glBindVertexBuffer");
		if (s_glBindVertexBuffer == null) missing.Add("glBindVertexBuffer");
		s_glBindVertexBuffers = (void*)getProcAddress("glBindVertexBuffers");
		if (s_glBindVertexBuffers == null) missing.Add("glBindVertexBuffers");
		s_glBlendColor = (void*)getProcAddress("glBlendColor");
		if (s_glBlendColor == null) missing.Add("glBlendColor");
		s_glBlendEquation = (void*)getProcAddress("glBlendEquation");
		if (s_glBlendEquation == null) missing.Add("glBlendEquation");
		s_glBlendEquationSeparate = (void*)getProcAddress("glBlendEquationSeparate");
		if (s_glBlendEquationSeparate == null) missing.Add("glBlendEquationSeparate");
		s_glBlendEquationSeparatei = (void*)getProcAddress("glBlendEquationSeparatei");
		if (s_glBlendEquationSeparatei == null) missing.Add("glBlendEquationSeparatei");
		s_glBlendEquationi = (void*)getProcAddress("glBlendEquationi");
		if (s_glBlendEquationi == null) missing.Add("glBlendEquationi");
		s_glBlendFunc = (void*)getProcAddress("glBlendFunc");
		if (s_glBlendFunc == null) missing.Add("glBlendFunc");
		s_glBlendFuncSeparate = (void*)getProcAddress("glBlendFuncSeparate");
		if (s_glBlendFuncSeparate == null) missing.Add("glBlendFuncSeparate");
		s_glBlendFuncSeparatei = (void*)getProcAddress("glBlendFuncSeparatei");
		if (s_glBlendFuncSeparatei == null) missing.Add("glBlendFuncSeparatei");
		s_glBlendFunci = (void*)getProcAddress("glBlendFunci");
		if (s_glBlendFunci == null) missing.Add("glBlendFunci");
		s_glBlitFramebuffer = (void*)getProcAddress("glBlitFramebuffer");
		if (s_glBlitFramebuffer == null) missing.Add("glBlitFramebuffer");
		s_glBlitNamedFramebuffer = (void*)getProcAddress("glBlitNamedFramebuffer");
		if (s_glBlitNamedFramebuffer == null) missing.Add("glBlitNamedFramebuffer");
		s_glBufferData = (void*)getProcAddress("glBufferData");
		if (s_glBufferData == null) missing.Add("glBufferData");
		s_glBufferStorage = (void*)getProcAddress("glBufferStorage");
		if (s_glBufferStorage == null) missing.Add("glBufferStorage");
		s_glBufferSubData = (void*)getProcAddress("glBufferSubData");
		if (s_glBufferSubData == null) missing.Add("glBufferSubData");
		s_glCheckFramebufferStatus = (void*)getProcAddress("glCheckFramebufferStatus");
		if (s_glCheckFramebufferStatus == null) missing.Add("glCheckFramebufferStatus");
		s_glCheckNamedFramebufferStatus = (void*)getProcAddress("glCheckNamedFramebufferStatus");
		if (s_glCheckNamedFramebufferStatus == null) missing.Add("glCheckNamedFramebufferStatus");
		s_glClampColor = (void*)getProcAddress("glClampColor");
		if (s_glClampColor == null) missing.Add("glClampColor");
		s_glClear = (void*)getProcAddress("glClear");
		if (s_glClear == null) missing.Add("glClear");
		s_glClearBufferData = (void*)getProcAddress("glClearBufferData");
		if (s_glClearBufferData == null) missing.Add("glClearBufferData");
		s_glClearBufferSubData = (void*)getProcAddress("glClearBufferSubData");
		if (s_glClearBufferSubData == null) missing.Add("glClearBufferSubData");
		s_glClearBufferfi = (void*)getProcAddress("glClearBufferfi");
		if (s_glClearBufferfi == null) missing.Add("glClearBufferfi");
		s_glClearBufferfv = (void*)getProcAddress("glClearBufferfv");
		if (s_glClearBufferfv == null) missing.Add("glClearBufferfv");
		s_glClearBufferiv = (void*)getProcAddress("glClearBufferiv");
		if (s_glClearBufferiv == null) missing.Add("glClearBufferiv");
		s_glClearBufferuiv = (void*)getProcAddress("glClearBufferuiv");
		if (s_glClearBufferuiv == null) missing.Add("glClearBufferuiv");
		s_glClearColor = (void*)getProcAddress("glClearColor");
		if (s_glClearColor == null) missing.Add("glClearColor");
		s_glClearDepth = (void*)getProcAddress("glClearDepth");
		if (s_glClearDepth == null) missing.Add("glClearDepth");
		s_glClearDepthf = (void*)getProcAddress("glClearDepthf");
		if (s_glClearDepthf == null) missing.Add("glClearDepthf");
		s_glClearNamedBufferData = (void*)getProcAddress("glClearNamedBufferData");
		if (s_glClearNamedBufferData == null) missing.Add("glClearNamedBufferData");
		s_glClearNamedBufferSubData = (void*)getProcAddress("glClearNamedBufferSubData");
		if (s_glClearNamedBufferSubData == null) missing.Add("glClearNamedBufferSubData");
		s_glClearNamedFramebufferfi = (void*)getProcAddress("glClearNamedFramebufferfi");
		if (s_glClearNamedFramebufferfi == null) missing.Add("glClearNamedFramebufferfi");
		s_glClearNamedFramebufferfv = (void*)getProcAddress("glClearNamedFramebufferfv");
		if (s_glClearNamedFramebufferfv == null) missing.Add("glClearNamedFramebufferfv");
		s_glClearNamedFramebufferiv = (void*)getProcAddress("glClearNamedFramebufferiv");
		if (s_glClearNamedFramebufferiv == null) missing.Add("glClearNamedFramebufferiv");
		s_glClearNamedFramebufferuiv = (void*)getProcAddress("glClearNamedFramebufferuiv");
		if (s_glClearNamedFramebufferuiv == null) missing.Add("glClearNamedFramebufferuiv");
		s_glClearStencil = (void*)getProcAddress("glClearStencil");
		if (s_glClearStencil == null) missing.Add("glClearStencil");
		s_glClearTexImage = (void*)getProcAddress("glClearTexImage");
		if (s_glClearTexImage == null) missing.Add("glClearTexImage");
		s_glClearTexSubImage = (void*)getProcAddress("glClearTexSubImage");
		if (s_glClearTexSubImage == null) missing.Add("glClearTexSubImage");
		s_glClientWaitSync = (void*)getProcAddress("glClientWaitSync");
		if (s_glClientWaitSync == null) missing.Add("glClientWaitSync");
		s_glClipControl = (void*)getProcAddress("glClipControl");
		if (s_glClipControl == null) missing.Add("glClipControl");
		s_glColorMask = (void*)getProcAddress("glColorMask");
		if (s_glColorMask == null) missing.Add("glColorMask");
		s_glColorMaski = (void*)getProcAddress("glColorMaski");
		if (s_glColorMaski == null) missing.Add("glColorMaski");
		s_glColorP3ui = (void*)getProcAddress("glColorP3ui");
		if (s_glColorP3ui == null) missing.Add("glColorP3ui");
		s_glColorP3uiv = (void*)getProcAddress("glColorP3uiv");
		if (s_glColorP3uiv == null) missing.Add("glColorP3uiv");
		s_glColorP4ui = (void*)getProcAddress("glColorP4ui");
		if (s_glColorP4ui == null) missing.Add("glColorP4ui");
		s_glColorP4uiv = (void*)getProcAddress("glColorP4uiv");
		if (s_glColorP4uiv == null) missing.Add("glColorP4uiv");
		s_glCompileShader = (void*)getProcAddress("glCompileShader");
		if (s_glCompileShader == null) missing.Add("glCompileShader");
		s_glCompressedTexImage1D = (void*)getProcAddress("glCompressedTexImage1D");
		if (s_glCompressedTexImage1D == null) missing.Add("glCompressedTexImage1D");
		s_glCompressedTexImage2D = (void*)getProcAddress("glCompressedTexImage2D");
		if (s_glCompressedTexImage2D == null) missing.Add("glCompressedTexImage2D");
		s_glCompressedTexImage3D = (void*)getProcAddress("glCompressedTexImage3D");
		if (s_glCompressedTexImage3D == null) missing.Add("glCompressedTexImage3D");
		s_glCompressedTexSubImage1D = (void*)getProcAddress("glCompressedTexSubImage1D");
		if (s_glCompressedTexSubImage1D == null) missing.Add("glCompressedTexSubImage1D");
		s_glCompressedTexSubImage2D = (void*)getProcAddress("glCompressedTexSubImage2D");
		if (s_glCompressedTexSubImage2D == null) missing.Add("glCompressedTexSubImage2D");
		s_glCompressedTexSubImage3D = (void*)getProcAddress("glCompressedTexSubImage3D");
		if (s_glCompressedTexSubImage3D == null) missing.Add("glCompressedTexSubImage3D");
		s_glCompressedTextureSubImage1D = (void*)getProcAddress("glCompressedTextureSubImage1D");
		if (s_glCompressedTextureSubImage1D == null) missing.Add("glCompressedTextureSubImage1D");
		s_glCompressedTextureSubImage2D = (void*)getProcAddress("glCompressedTextureSubImage2D");
		if (s_glCompressedTextureSubImage2D == null) missing.Add("glCompressedTextureSubImage2D");
		s_glCompressedTextureSubImage3D = (void*)getProcAddress("glCompressedTextureSubImage3D");
		if (s_glCompressedTextureSubImage3D == null) missing.Add("glCompressedTextureSubImage3D");
		s_glCopyBufferSubData = (void*)getProcAddress("glCopyBufferSubData");
		if (s_glCopyBufferSubData == null) missing.Add("glCopyBufferSubData");
		s_glCopyImageSubData = (void*)getProcAddress("glCopyImageSubData");
		if (s_glCopyImageSubData == null) missing.Add("glCopyImageSubData");
		s_glCopyNamedBufferSubData = (void*)getProcAddress("glCopyNamedBufferSubData");
		if (s_glCopyNamedBufferSubData == null) missing.Add("glCopyNamedBufferSubData");
		s_glCopyTexImage1D = (void*)getProcAddress("glCopyTexImage1D");
		if (s_glCopyTexImage1D == null) missing.Add("glCopyTexImage1D");
		s_glCopyTexImage2D = (void*)getProcAddress("glCopyTexImage2D");
		if (s_glCopyTexImage2D == null) missing.Add("glCopyTexImage2D");
		s_glCopyTexSubImage1D = (void*)getProcAddress("glCopyTexSubImage1D");
		if (s_glCopyTexSubImage1D == null) missing.Add("glCopyTexSubImage1D");
		s_glCopyTexSubImage2D = (void*)getProcAddress("glCopyTexSubImage2D");
		if (s_glCopyTexSubImage2D == null) missing.Add("glCopyTexSubImage2D");
		s_glCopyTexSubImage3D = (void*)getProcAddress("glCopyTexSubImage3D");
		if (s_glCopyTexSubImage3D == null) missing.Add("glCopyTexSubImage3D");
		s_glCopyTextureSubImage1D = (void*)getProcAddress("glCopyTextureSubImage1D");
		if (s_glCopyTextureSubImage1D == null) missing.Add("glCopyTextureSubImage1D");
		s_glCopyTextureSubImage2D = (void*)getProcAddress("glCopyTextureSubImage2D");
		if (s_glCopyTextureSubImage2D == null) missing.Add("glCopyTextureSubImage2D");
		s_glCopyTextureSubImage3D = (void*)getProcAddress("glCopyTextureSubImage3D");
		if (s_glCopyTextureSubImage3D == null) missing.Add("glCopyTextureSubImage3D");
		s_glCreateBuffers = (void*)getProcAddress("glCreateBuffers");
		if (s_glCreateBuffers == null) missing.Add("glCreateBuffers");
		s_glCreateFramebuffers = (void*)getProcAddress("glCreateFramebuffers");
		if (s_glCreateFramebuffers == null) missing.Add("glCreateFramebuffers");
		s_glCreateProgram = (void*)getProcAddress("glCreateProgram");
		if (s_glCreateProgram == null) missing.Add("glCreateProgram");
		s_glCreateProgramPipelines = (void*)getProcAddress("glCreateProgramPipelines");
		if (s_glCreateProgramPipelines == null) missing.Add("glCreateProgramPipelines");
		s_glCreateQueries = (void*)getProcAddress("glCreateQueries");
		if (s_glCreateQueries == null) missing.Add("glCreateQueries");
		s_glCreateRenderbuffers = (void*)getProcAddress("glCreateRenderbuffers");
		if (s_glCreateRenderbuffers == null) missing.Add("glCreateRenderbuffers");
		s_glCreateSamplers = (void*)getProcAddress("glCreateSamplers");
		if (s_glCreateSamplers == null) missing.Add("glCreateSamplers");
		s_glCreateShader = (void*)getProcAddress("glCreateShader");
		if (s_glCreateShader == null) missing.Add("glCreateShader");
		s_glCreateShaderProgramv = (void*)getProcAddress("glCreateShaderProgramv");
		if (s_glCreateShaderProgramv == null) missing.Add("glCreateShaderProgramv");
		s_glCreateTextures = (void*)getProcAddress("glCreateTextures");
		if (s_glCreateTextures == null) missing.Add("glCreateTextures");
		s_glCreateTransformFeedbacks = (void*)getProcAddress("glCreateTransformFeedbacks");
		if (s_glCreateTransformFeedbacks == null) missing.Add("glCreateTransformFeedbacks");
		s_glCreateVertexArrays = (void*)getProcAddress("glCreateVertexArrays");
		if (s_glCreateVertexArrays == null) missing.Add("glCreateVertexArrays");
		s_glCullFace = (void*)getProcAddress("glCullFace");
		if (s_glCullFace == null) missing.Add("glCullFace");
		s_glDebugMessageCallback = (void*)getProcAddress("glDebugMessageCallback");
		if (s_glDebugMessageCallback == null) missing.Add("glDebugMessageCallback");
		s_glDebugMessageControl = (void*)getProcAddress("glDebugMessageControl");
		if (s_glDebugMessageControl == null) missing.Add("glDebugMessageControl");
		s_glDebugMessageInsert = (void*)getProcAddress("glDebugMessageInsert");
		if (s_glDebugMessageInsert == null) missing.Add("glDebugMessageInsert");
		s_glDeleteBuffers = (void*)getProcAddress("glDeleteBuffers");
		if (s_glDeleteBuffers == null) missing.Add("glDeleteBuffers");
		s_glDeleteFramebuffers = (void*)getProcAddress("glDeleteFramebuffers");
		if (s_glDeleteFramebuffers == null) missing.Add("glDeleteFramebuffers");
		s_glDeleteProgram = (void*)getProcAddress("glDeleteProgram");
		if (s_glDeleteProgram == null) missing.Add("glDeleteProgram");
		s_glDeleteProgramPipelines = (void*)getProcAddress("glDeleteProgramPipelines");
		if (s_glDeleteProgramPipelines == null) missing.Add("glDeleteProgramPipelines");
		s_glDeleteQueries = (void*)getProcAddress("glDeleteQueries");
		if (s_glDeleteQueries == null) missing.Add("glDeleteQueries");
		s_glDeleteRenderbuffers = (void*)getProcAddress("glDeleteRenderbuffers");
		if (s_glDeleteRenderbuffers == null) missing.Add("glDeleteRenderbuffers");
		s_glDeleteSamplers = (void*)getProcAddress("glDeleteSamplers");
		if (s_glDeleteSamplers == null) missing.Add("glDeleteSamplers");
		s_glDeleteShader = (void*)getProcAddress("glDeleteShader");
		if (s_glDeleteShader == null) missing.Add("glDeleteShader");
		s_glDeleteSync = (void*)getProcAddress("glDeleteSync");
		if (s_glDeleteSync == null) missing.Add("glDeleteSync");
		s_glDeleteTextures = (void*)getProcAddress("glDeleteTextures");
		if (s_glDeleteTextures == null) missing.Add("glDeleteTextures");
		s_glDeleteTransformFeedbacks = (void*)getProcAddress("glDeleteTransformFeedbacks");
		if (s_glDeleteTransformFeedbacks == null) missing.Add("glDeleteTransformFeedbacks");
		s_glDeleteVertexArrays = (void*)getProcAddress("glDeleteVertexArrays");
		if (s_glDeleteVertexArrays == null) missing.Add("glDeleteVertexArrays");
		s_glDepthFunc = (void*)getProcAddress("glDepthFunc");
		if (s_glDepthFunc == null) missing.Add("glDepthFunc");
		s_glDepthMask = (void*)getProcAddress("glDepthMask");
		if (s_glDepthMask == null) missing.Add("glDepthMask");
		s_glDepthRange = (void*)getProcAddress("glDepthRange");
		if (s_glDepthRange == null) missing.Add("glDepthRange");
		s_glDepthRangeArrayv = (void*)getProcAddress("glDepthRangeArrayv");
		if (s_glDepthRangeArrayv == null) missing.Add("glDepthRangeArrayv");
		s_glDepthRangeIndexed = (void*)getProcAddress("glDepthRangeIndexed");
		if (s_glDepthRangeIndexed == null) missing.Add("glDepthRangeIndexed");
		s_glDepthRangef = (void*)getProcAddress("glDepthRangef");
		if (s_glDepthRangef == null) missing.Add("glDepthRangef");
		s_glDetachShader = (void*)getProcAddress("glDetachShader");
		if (s_glDetachShader == null) missing.Add("glDetachShader");
		s_glDisable = (void*)getProcAddress("glDisable");
		if (s_glDisable == null) missing.Add("glDisable");
		s_glDisableVertexArrayAttrib = (void*)getProcAddress("glDisableVertexArrayAttrib");
		if (s_glDisableVertexArrayAttrib == null) missing.Add("glDisableVertexArrayAttrib");
		s_glDisableVertexAttribArray = (void*)getProcAddress("glDisableVertexAttribArray");
		if (s_glDisableVertexAttribArray == null) missing.Add("glDisableVertexAttribArray");
		s_glDisablei = (void*)getProcAddress("glDisablei");
		if (s_glDisablei == null) missing.Add("glDisablei");
		s_glDispatchCompute = (void*)getProcAddress("glDispatchCompute");
		if (s_glDispatchCompute == null) missing.Add("glDispatchCompute");
		s_glDispatchComputeIndirect = (void*)getProcAddress("glDispatchComputeIndirect");
		if (s_glDispatchComputeIndirect == null) missing.Add("glDispatchComputeIndirect");
		s_glDrawArrays = (void*)getProcAddress("glDrawArrays");
		if (s_glDrawArrays == null) missing.Add("glDrawArrays");
		s_glDrawArraysIndirect = (void*)getProcAddress("glDrawArraysIndirect");
		if (s_glDrawArraysIndirect == null) missing.Add("glDrawArraysIndirect");
		s_glDrawArraysInstanced = (void*)getProcAddress("glDrawArraysInstanced");
		if (s_glDrawArraysInstanced == null) missing.Add("glDrawArraysInstanced");
		s_glDrawArraysInstancedBaseInstance = (void*)getProcAddress("glDrawArraysInstancedBaseInstance");
		if (s_glDrawArraysInstancedBaseInstance == null) missing.Add("glDrawArraysInstancedBaseInstance");
		s_glDrawBuffer = (void*)getProcAddress("glDrawBuffer");
		if (s_glDrawBuffer == null) missing.Add("glDrawBuffer");
		s_glDrawBuffers = (void*)getProcAddress("glDrawBuffers");
		if (s_glDrawBuffers == null) missing.Add("glDrawBuffers");
		s_glDrawElements = (void*)getProcAddress("glDrawElements");
		if (s_glDrawElements == null) missing.Add("glDrawElements");
		s_glDrawElementsBaseVertex = (void*)getProcAddress("glDrawElementsBaseVertex");
		if (s_glDrawElementsBaseVertex == null) missing.Add("glDrawElementsBaseVertex");
		s_glDrawElementsIndirect = (void*)getProcAddress("glDrawElementsIndirect");
		if (s_glDrawElementsIndirect == null) missing.Add("glDrawElementsIndirect");
		s_glDrawElementsInstanced = (void*)getProcAddress("glDrawElementsInstanced");
		if (s_glDrawElementsInstanced == null) missing.Add("glDrawElementsInstanced");
		s_glDrawElementsInstancedBaseInstance = (void*)getProcAddress("glDrawElementsInstancedBaseInstance");
		if (s_glDrawElementsInstancedBaseInstance == null) missing.Add("glDrawElementsInstancedBaseInstance");
		s_glDrawElementsInstancedBaseVertex = (void*)getProcAddress("glDrawElementsInstancedBaseVertex");
		if (s_glDrawElementsInstancedBaseVertex == null) missing.Add("glDrawElementsInstancedBaseVertex");
		s_glDrawElementsInstancedBaseVertexBaseInstance = (void*)getProcAddress("glDrawElementsInstancedBaseVertexBaseInstance");
		if (s_glDrawElementsInstancedBaseVertexBaseInstance == null) missing.Add("glDrawElementsInstancedBaseVertexBaseInstance");
		s_glDrawRangeElements = (void*)getProcAddress("glDrawRangeElements");
		if (s_glDrawRangeElements == null) missing.Add("glDrawRangeElements");
		s_glDrawRangeElementsBaseVertex = (void*)getProcAddress("glDrawRangeElementsBaseVertex");
		if (s_glDrawRangeElementsBaseVertex == null) missing.Add("glDrawRangeElementsBaseVertex");
		s_glDrawTransformFeedback = (void*)getProcAddress("glDrawTransformFeedback");
		if (s_glDrawTransformFeedback == null) missing.Add("glDrawTransformFeedback");
		s_glDrawTransformFeedbackInstanced = (void*)getProcAddress("glDrawTransformFeedbackInstanced");
		if (s_glDrawTransformFeedbackInstanced == null) missing.Add("glDrawTransformFeedbackInstanced");
		s_glDrawTransformFeedbackStream = (void*)getProcAddress("glDrawTransformFeedbackStream");
		if (s_glDrawTransformFeedbackStream == null) missing.Add("glDrawTransformFeedbackStream");
		s_glDrawTransformFeedbackStreamInstanced = (void*)getProcAddress("glDrawTransformFeedbackStreamInstanced");
		if (s_glDrawTransformFeedbackStreamInstanced == null) missing.Add("glDrawTransformFeedbackStreamInstanced");
		s_glEnable = (void*)getProcAddress("glEnable");
		if (s_glEnable == null) missing.Add("glEnable");
		s_glEnableVertexArrayAttrib = (void*)getProcAddress("glEnableVertexArrayAttrib");
		if (s_glEnableVertexArrayAttrib == null) missing.Add("glEnableVertexArrayAttrib");
		s_glEnableVertexAttribArray = (void*)getProcAddress("glEnableVertexAttribArray");
		if (s_glEnableVertexAttribArray == null) missing.Add("glEnableVertexAttribArray");
		s_glEnablei = (void*)getProcAddress("glEnablei");
		if (s_glEnablei == null) missing.Add("glEnablei");
		s_glEndConditionalRender = (void*)getProcAddress("glEndConditionalRender");
		if (s_glEndConditionalRender == null) missing.Add("glEndConditionalRender");
		s_glEndQuery = (void*)getProcAddress("glEndQuery");
		if (s_glEndQuery == null) missing.Add("glEndQuery");
		s_glEndQueryIndexed = (void*)getProcAddress("glEndQueryIndexed");
		if (s_glEndQueryIndexed == null) missing.Add("glEndQueryIndexed");
		s_glEndTransformFeedback = (void*)getProcAddress("glEndTransformFeedback");
		if (s_glEndTransformFeedback == null) missing.Add("glEndTransformFeedback");
		s_glFenceSync = (void*)getProcAddress("glFenceSync");
		if (s_glFenceSync == null) missing.Add("glFenceSync");
		s_glFinish = (void*)getProcAddress("glFinish");
		if (s_glFinish == null) missing.Add("glFinish");
		s_glFlush = (void*)getProcAddress("glFlush");
		if (s_glFlush == null) missing.Add("glFlush");
		s_glFlushMappedBufferRange = (void*)getProcAddress("glFlushMappedBufferRange");
		if (s_glFlushMappedBufferRange == null) missing.Add("glFlushMappedBufferRange");
		s_glFlushMappedNamedBufferRange = (void*)getProcAddress("glFlushMappedNamedBufferRange");
		if (s_glFlushMappedNamedBufferRange == null) missing.Add("glFlushMappedNamedBufferRange");
		s_glFramebufferParameteri = (void*)getProcAddress("glFramebufferParameteri");
		if (s_glFramebufferParameteri == null) missing.Add("glFramebufferParameteri");
		s_glFramebufferRenderbuffer = (void*)getProcAddress("glFramebufferRenderbuffer");
		if (s_glFramebufferRenderbuffer == null) missing.Add("glFramebufferRenderbuffer");
		s_glFramebufferTexture = (void*)getProcAddress("glFramebufferTexture");
		if (s_glFramebufferTexture == null) missing.Add("glFramebufferTexture");
		s_glFramebufferTexture1D = (void*)getProcAddress("glFramebufferTexture1D");
		if (s_glFramebufferTexture1D == null) missing.Add("glFramebufferTexture1D");
		s_glFramebufferTexture2D = (void*)getProcAddress("glFramebufferTexture2D");
		if (s_glFramebufferTexture2D == null) missing.Add("glFramebufferTexture2D");
		s_glFramebufferTexture3D = (void*)getProcAddress("glFramebufferTexture3D");
		if (s_glFramebufferTexture3D == null) missing.Add("glFramebufferTexture3D");
		s_glFramebufferTextureLayer = (void*)getProcAddress("glFramebufferTextureLayer");
		if (s_glFramebufferTextureLayer == null) missing.Add("glFramebufferTextureLayer");
		s_glFrontFace = (void*)getProcAddress("glFrontFace");
		if (s_glFrontFace == null) missing.Add("glFrontFace");
		s_glGenBuffers = (void*)getProcAddress("glGenBuffers");
		if (s_glGenBuffers == null) missing.Add("glGenBuffers");
		s_glGenFramebuffers = (void*)getProcAddress("glGenFramebuffers");
		if (s_glGenFramebuffers == null) missing.Add("glGenFramebuffers");
		s_glGenProgramPipelines = (void*)getProcAddress("glGenProgramPipelines");
		if (s_glGenProgramPipelines == null) missing.Add("glGenProgramPipelines");
		s_glGenQueries = (void*)getProcAddress("glGenQueries");
		if (s_glGenQueries == null) missing.Add("glGenQueries");
		s_glGenRenderbuffers = (void*)getProcAddress("glGenRenderbuffers");
		if (s_glGenRenderbuffers == null) missing.Add("glGenRenderbuffers");
		s_glGenSamplers = (void*)getProcAddress("glGenSamplers");
		if (s_glGenSamplers == null) missing.Add("glGenSamplers");
		s_glGenTextures = (void*)getProcAddress("glGenTextures");
		if (s_glGenTextures == null) missing.Add("glGenTextures");
		s_glGenTransformFeedbacks = (void*)getProcAddress("glGenTransformFeedbacks");
		if (s_glGenTransformFeedbacks == null) missing.Add("glGenTransformFeedbacks");
		s_glGenVertexArrays = (void*)getProcAddress("glGenVertexArrays");
		if (s_glGenVertexArrays == null) missing.Add("glGenVertexArrays");
		s_glGenerateMipmap = (void*)getProcAddress("glGenerateMipmap");
		if (s_glGenerateMipmap == null) missing.Add("glGenerateMipmap");
		s_glGenerateTextureMipmap = (void*)getProcAddress("glGenerateTextureMipmap");
		if (s_glGenerateTextureMipmap == null) missing.Add("glGenerateTextureMipmap");
		s_glGetActiveAtomicCounterBufferiv = (void*)getProcAddress("glGetActiveAtomicCounterBufferiv");
		if (s_glGetActiveAtomicCounterBufferiv == null) missing.Add("glGetActiveAtomicCounterBufferiv");
		s_glGetActiveAttrib = (void*)getProcAddress("glGetActiveAttrib");
		if (s_glGetActiveAttrib == null) missing.Add("glGetActiveAttrib");
		s_glGetActiveSubroutineName = (void*)getProcAddress("glGetActiveSubroutineName");
		if (s_glGetActiveSubroutineName == null) missing.Add("glGetActiveSubroutineName");
		s_glGetActiveSubroutineUniformName = (void*)getProcAddress("glGetActiveSubroutineUniformName");
		if (s_glGetActiveSubroutineUniformName == null) missing.Add("glGetActiveSubroutineUniformName");
		s_glGetActiveSubroutineUniformiv = (void*)getProcAddress("glGetActiveSubroutineUniformiv");
		if (s_glGetActiveSubroutineUniformiv == null) missing.Add("glGetActiveSubroutineUniformiv");
		s_glGetActiveUniform = (void*)getProcAddress("glGetActiveUniform");
		if (s_glGetActiveUniform == null) missing.Add("glGetActiveUniform");
		s_glGetActiveUniformBlockName = (void*)getProcAddress("glGetActiveUniformBlockName");
		if (s_glGetActiveUniformBlockName == null) missing.Add("glGetActiveUniformBlockName");
		s_glGetActiveUniformBlockiv = (void*)getProcAddress("glGetActiveUniformBlockiv");
		if (s_glGetActiveUniformBlockiv == null) missing.Add("glGetActiveUniformBlockiv");
		s_glGetActiveUniformName = (void*)getProcAddress("glGetActiveUniformName");
		if (s_glGetActiveUniformName == null) missing.Add("glGetActiveUniformName");
		s_glGetActiveUniformsiv = (void*)getProcAddress("glGetActiveUniformsiv");
		if (s_glGetActiveUniformsiv == null) missing.Add("glGetActiveUniformsiv");
		s_glGetAttachedShaders = (void*)getProcAddress("glGetAttachedShaders");
		if (s_glGetAttachedShaders == null) missing.Add("glGetAttachedShaders");
		s_glGetAttribLocation = (void*)getProcAddress("glGetAttribLocation");
		if (s_glGetAttribLocation == null) missing.Add("glGetAttribLocation");
		s_glGetBooleani_v = (void*)getProcAddress("glGetBooleani_v");
		if (s_glGetBooleani_v == null) missing.Add("glGetBooleani_v");
		s_glGetBooleanv = (void*)getProcAddress("glGetBooleanv");
		if (s_glGetBooleanv == null) missing.Add("glGetBooleanv");
		s_glGetBufferParameteri64v = (void*)getProcAddress("glGetBufferParameteri64v");
		if (s_glGetBufferParameteri64v == null) missing.Add("glGetBufferParameteri64v");
		s_glGetBufferParameteriv = (void*)getProcAddress("glGetBufferParameteriv");
		if (s_glGetBufferParameteriv == null) missing.Add("glGetBufferParameteriv");
		s_glGetBufferPointerv = (void*)getProcAddress("glGetBufferPointerv");
		if (s_glGetBufferPointerv == null) missing.Add("glGetBufferPointerv");
		s_glGetBufferSubData = (void*)getProcAddress("glGetBufferSubData");
		if (s_glGetBufferSubData == null) missing.Add("glGetBufferSubData");
		s_glGetCompressedTexImage = (void*)getProcAddress("glGetCompressedTexImage");
		if (s_glGetCompressedTexImage == null) missing.Add("glGetCompressedTexImage");
		s_glGetCompressedTextureImage = (void*)getProcAddress("glGetCompressedTextureImage");
		if (s_glGetCompressedTextureImage == null) missing.Add("glGetCompressedTextureImage");
		s_glGetCompressedTextureSubImage = (void*)getProcAddress("glGetCompressedTextureSubImage");
		if (s_glGetCompressedTextureSubImage == null) missing.Add("glGetCompressedTextureSubImage");
		s_glGetDebugMessageLog = (void*)getProcAddress("glGetDebugMessageLog");
		if (s_glGetDebugMessageLog == null) missing.Add("glGetDebugMessageLog");
		s_glGetDoublei_v = (void*)getProcAddress("glGetDoublei_v");
		if (s_glGetDoublei_v == null) missing.Add("glGetDoublei_v");
		s_glGetDoublev = (void*)getProcAddress("glGetDoublev");
		if (s_glGetDoublev == null) missing.Add("glGetDoublev");
		s_glGetError = (void*)getProcAddress("glGetError");
		if (s_glGetError == null) missing.Add("glGetError");
		s_glGetFloati_v = (void*)getProcAddress("glGetFloati_v");
		if (s_glGetFloati_v == null) missing.Add("glGetFloati_v");
		s_glGetFloatv = (void*)getProcAddress("glGetFloatv");
		if (s_glGetFloatv == null) missing.Add("glGetFloatv");
		s_glGetFragDataIndex = (void*)getProcAddress("glGetFragDataIndex");
		if (s_glGetFragDataIndex == null) missing.Add("glGetFragDataIndex");
		s_glGetFragDataLocation = (void*)getProcAddress("glGetFragDataLocation");
		if (s_glGetFragDataLocation == null) missing.Add("glGetFragDataLocation");
		s_glGetFramebufferAttachmentParameteriv = (void*)getProcAddress("glGetFramebufferAttachmentParameteriv");
		if (s_glGetFramebufferAttachmentParameteriv == null) missing.Add("glGetFramebufferAttachmentParameteriv");
		s_glGetFramebufferParameteriv = (void*)getProcAddress("glGetFramebufferParameteriv");
		if (s_glGetFramebufferParameteriv == null) missing.Add("glGetFramebufferParameteriv");
		s_glGetGraphicsResetStatus = (void*)getProcAddress("glGetGraphicsResetStatus");
		if (s_glGetGraphicsResetStatus == null) missing.Add("glGetGraphicsResetStatus");
		s_glGetInteger64i_v = (void*)getProcAddress("glGetInteger64i_v");
		if (s_glGetInteger64i_v == null) missing.Add("glGetInteger64i_v");
		s_glGetInteger64v = (void*)getProcAddress("glGetInteger64v");
		if (s_glGetInteger64v == null) missing.Add("glGetInteger64v");
		s_glGetIntegeri_v = (void*)getProcAddress("glGetIntegeri_v");
		if (s_glGetIntegeri_v == null) missing.Add("glGetIntegeri_v");
		s_glGetIntegerv = (void*)getProcAddress("glGetIntegerv");
		if (s_glGetIntegerv == null) missing.Add("glGetIntegerv");
		s_glGetInternalformati64v = (void*)getProcAddress("glGetInternalformati64v");
		if (s_glGetInternalformati64v == null) missing.Add("glGetInternalformati64v");
		s_glGetInternalformativ = (void*)getProcAddress("glGetInternalformativ");
		if (s_glGetInternalformativ == null) missing.Add("glGetInternalformativ");
		s_glGetMultisamplefv = (void*)getProcAddress("glGetMultisamplefv");
		if (s_glGetMultisamplefv == null) missing.Add("glGetMultisamplefv");
		s_glGetNamedBufferParameteri64v = (void*)getProcAddress("glGetNamedBufferParameteri64v");
		if (s_glGetNamedBufferParameteri64v == null) missing.Add("glGetNamedBufferParameteri64v");
		s_glGetNamedBufferParameteriv = (void*)getProcAddress("glGetNamedBufferParameteriv");
		if (s_glGetNamedBufferParameteriv == null) missing.Add("glGetNamedBufferParameteriv");
		s_glGetNamedBufferPointerv = (void*)getProcAddress("glGetNamedBufferPointerv");
		if (s_glGetNamedBufferPointerv == null) missing.Add("glGetNamedBufferPointerv");
		s_glGetNamedBufferSubData = (void*)getProcAddress("glGetNamedBufferSubData");
		if (s_glGetNamedBufferSubData == null) missing.Add("glGetNamedBufferSubData");
		s_glGetNamedFramebufferAttachmentParameteriv = (void*)getProcAddress("glGetNamedFramebufferAttachmentParameteriv");
		if (s_glGetNamedFramebufferAttachmentParameteriv == null) missing.Add("glGetNamedFramebufferAttachmentParameteriv");
		s_glGetNamedFramebufferParameteriv = (void*)getProcAddress("glGetNamedFramebufferParameteriv");
		if (s_glGetNamedFramebufferParameteriv == null) missing.Add("glGetNamedFramebufferParameteriv");
		s_glGetNamedRenderbufferParameteriv = (void*)getProcAddress("glGetNamedRenderbufferParameteriv");
		if (s_glGetNamedRenderbufferParameteriv == null) missing.Add("glGetNamedRenderbufferParameteriv");
		s_glGetObjectLabel = (void*)getProcAddress("glGetObjectLabel");
		if (s_glGetObjectLabel == null) missing.Add("glGetObjectLabel");
		s_glGetObjectPtrLabel = (void*)getProcAddress("glGetObjectPtrLabel");
		if (s_glGetObjectPtrLabel == null) missing.Add("glGetObjectPtrLabel");
		s_glGetProgramBinary = (void*)getProcAddress("glGetProgramBinary");
		if (s_glGetProgramBinary == null) missing.Add("glGetProgramBinary");
		s_glGetProgramInfoLog = (void*)getProcAddress("glGetProgramInfoLog");
		if (s_glGetProgramInfoLog == null) missing.Add("glGetProgramInfoLog");
		s_glGetProgramInterfaceiv = (void*)getProcAddress("glGetProgramInterfaceiv");
		if (s_glGetProgramInterfaceiv == null) missing.Add("glGetProgramInterfaceiv");
		s_glGetProgramPipelineInfoLog = (void*)getProcAddress("glGetProgramPipelineInfoLog");
		if (s_glGetProgramPipelineInfoLog == null) missing.Add("glGetProgramPipelineInfoLog");
		s_glGetProgramPipelineiv = (void*)getProcAddress("glGetProgramPipelineiv");
		if (s_glGetProgramPipelineiv == null) missing.Add("glGetProgramPipelineiv");
		s_glGetProgramResourceIndex = (void*)getProcAddress("glGetProgramResourceIndex");
		if (s_glGetProgramResourceIndex == null) missing.Add("glGetProgramResourceIndex");
		s_glGetProgramResourceLocation = (void*)getProcAddress("glGetProgramResourceLocation");
		if (s_glGetProgramResourceLocation == null) missing.Add("glGetProgramResourceLocation");
		s_glGetProgramResourceLocationIndex = (void*)getProcAddress("glGetProgramResourceLocationIndex");
		if (s_glGetProgramResourceLocationIndex == null) missing.Add("glGetProgramResourceLocationIndex");
		s_glGetProgramResourceName = (void*)getProcAddress("glGetProgramResourceName");
		if (s_glGetProgramResourceName == null) missing.Add("glGetProgramResourceName");
		s_glGetProgramResourceiv = (void*)getProcAddress("glGetProgramResourceiv");
		if (s_glGetProgramResourceiv == null) missing.Add("glGetProgramResourceiv");
		s_glGetProgramStageiv = (void*)getProcAddress("glGetProgramStageiv");
		if (s_glGetProgramStageiv == null) missing.Add("glGetProgramStageiv");
		s_glGetProgramiv = (void*)getProcAddress("glGetProgramiv");
		if (s_glGetProgramiv == null) missing.Add("glGetProgramiv");
		s_glGetQueryBufferObjecti64v = (void*)getProcAddress("glGetQueryBufferObjecti64v");
		if (s_glGetQueryBufferObjecti64v == null) missing.Add("glGetQueryBufferObjecti64v");
		s_glGetQueryBufferObjectiv = (void*)getProcAddress("glGetQueryBufferObjectiv");
		if (s_glGetQueryBufferObjectiv == null) missing.Add("glGetQueryBufferObjectiv");
		s_glGetQueryBufferObjectui64v = (void*)getProcAddress("glGetQueryBufferObjectui64v");
		if (s_glGetQueryBufferObjectui64v == null) missing.Add("glGetQueryBufferObjectui64v");
		s_glGetQueryBufferObjectuiv = (void*)getProcAddress("glGetQueryBufferObjectuiv");
		if (s_glGetQueryBufferObjectuiv == null) missing.Add("glGetQueryBufferObjectuiv");
		s_glGetQueryIndexediv = (void*)getProcAddress("glGetQueryIndexediv");
		if (s_glGetQueryIndexediv == null) missing.Add("glGetQueryIndexediv");
		s_glGetQueryObjecti64v = (void*)getProcAddress("glGetQueryObjecti64v");
		if (s_glGetQueryObjecti64v == null) missing.Add("glGetQueryObjecti64v");
		s_glGetQueryObjectiv = (void*)getProcAddress("glGetQueryObjectiv");
		if (s_glGetQueryObjectiv == null) missing.Add("glGetQueryObjectiv");
		s_glGetQueryObjectui64v = (void*)getProcAddress("glGetQueryObjectui64v");
		if (s_glGetQueryObjectui64v == null) missing.Add("glGetQueryObjectui64v");
		s_glGetQueryObjectuiv = (void*)getProcAddress("glGetQueryObjectuiv");
		if (s_glGetQueryObjectuiv == null) missing.Add("glGetQueryObjectuiv");
		s_glGetQueryiv = (void*)getProcAddress("glGetQueryiv");
		if (s_glGetQueryiv == null) missing.Add("glGetQueryiv");
		s_glGetRenderbufferParameteriv = (void*)getProcAddress("glGetRenderbufferParameteriv");
		if (s_glGetRenderbufferParameteriv == null) missing.Add("glGetRenderbufferParameteriv");
		s_glGetSamplerParameterIiv = (void*)getProcAddress("glGetSamplerParameterIiv");
		if (s_glGetSamplerParameterIiv == null) missing.Add("glGetSamplerParameterIiv");
		s_glGetSamplerParameterIuiv = (void*)getProcAddress("glGetSamplerParameterIuiv");
		if (s_glGetSamplerParameterIuiv == null) missing.Add("glGetSamplerParameterIuiv");
		s_glGetSamplerParameterfv = (void*)getProcAddress("glGetSamplerParameterfv");
		if (s_glGetSamplerParameterfv == null) missing.Add("glGetSamplerParameterfv");
		s_glGetSamplerParameteriv = (void*)getProcAddress("glGetSamplerParameteriv");
		if (s_glGetSamplerParameteriv == null) missing.Add("glGetSamplerParameteriv");
		s_glGetShaderInfoLog = (void*)getProcAddress("glGetShaderInfoLog");
		if (s_glGetShaderInfoLog == null) missing.Add("glGetShaderInfoLog");
		s_glGetShaderPrecisionFormat = (void*)getProcAddress("glGetShaderPrecisionFormat");
		if (s_glGetShaderPrecisionFormat == null) missing.Add("glGetShaderPrecisionFormat");
		s_glGetShaderSource = (void*)getProcAddress("glGetShaderSource");
		if (s_glGetShaderSource == null) missing.Add("glGetShaderSource");
		s_glGetShaderiv = (void*)getProcAddress("glGetShaderiv");
		if (s_glGetShaderiv == null) missing.Add("glGetShaderiv");
		s_glGetString = (void*)getProcAddress("glGetString");
		if (s_glGetString == null) missing.Add("glGetString");
		s_glGetStringi = (void*)getProcAddress("glGetStringi");
		if (s_glGetStringi == null) missing.Add("glGetStringi");
		s_glGetSubroutineIndex = (void*)getProcAddress("glGetSubroutineIndex");
		if (s_glGetSubroutineIndex == null) missing.Add("glGetSubroutineIndex");
		s_glGetSubroutineUniformLocation = (void*)getProcAddress("glGetSubroutineUniformLocation");
		if (s_glGetSubroutineUniformLocation == null) missing.Add("glGetSubroutineUniformLocation");
		s_glGetSynciv = (void*)getProcAddress("glGetSynciv");
		if (s_glGetSynciv == null) missing.Add("glGetSynciv");
		s_glGetTexImage = (void*)getProcAddress("glGetTexImage");
		if (s_glGetTexImage == null) missing.Add("glGetTexImage");
		s_glGetTexLevelParameterfv = (void*)getProcAddress("glGetTexLevelParameterfv");
		if (s_glGetTexLevelParameterfv == null) missing.Add("glGetTexLevelParameterfv");
		s_glGetTexLevelParameteriv = (void*)getProcAddress("glGetTexLevelParameteriv");
		if (s_glGetTexLevelParameteriv == null) missing.Add("glGetTexLevelParameteriv");
		s_glGetTexParameterIiv = (void*)getProcAddress("glGetTexParameterIiv");
		if (s_glGetTexParameterIiv == null) missing.Add("glGetTexParameterIiv");
		s_glGetTexParameterIuiv = (void*)getProcAddress("glGetTexParameterIuiv");
		if (s_glGetTexParameterIuiv == null) missing.Add("glGetTexParameterIuiv");
		s_glGetTexParameterfv = (void*)getProcAddress("glGetTexParameterfv");
		if (s_glGetTexParameterfv == null) missing.Add("glGetTexParameterfv");
		s_glGetTexParameteriv = (void*)getProcAddress("glGetTexParameteriv");
		if (s_glGetTexParameteriv == null) missing.Add("glGetTexParameteriv");
		s_glGetTextureImage = (void*)getProcAddress("glGetTextureImage");
		if (s_glGetTextureImage == null) missing.Add("glGetTextureImage");
		s_glGetTextureLevelParameterfv = (void*)getProcAddress("glGetTextureLevelParameterfv");
		if (s_glGetTextureLevelParameterfv == null) missing.Add("glGetTextureLevelParameterfv");
		s_glGetTextureLevelParameteriv = (void*)getProcAddress("glGetTextureLevelParameteriv");
		if (s_glGetTextureLevelParameteriv == null) missing.Add("glGetTextureLevelParameteriv");
		s_glGetTextureParameterIiv = (void*)getProcAddress("glGetTextureParameterIiv");
		if (s_glGetTextureParameterIiv == null) missing.Add("glGetTextureParameterIiv");
		s_glGetTextureParameterIuiv = (void*)getProcAddress("glGetTextureParameterIuiv");
		if (s_glGetTextureParameterIuiv == null) missing.Add("glGetTextureParameterIuiv");
		s_glGetTextureParameterfv = (void*)getProcAddress("glGetTextureParameterfv");
		if (s_glGetTextureParameterfv == null) missing.Add("glGetTextureParameterfv");
		s_glGetTextureParameteriv = (void*)getProcAddress("glGetTextureParameteriv");
		if (s_glGetTextureParameteriv == null) missing.Add("glGetTextureParameteriv");
		s_glGetTextureSubImage = (void*)getProcAddress("glGetTextureSubImage");
		if (s_glGetTextureSubImage == null) missing.Add("glGetTextureSubImage");
		s_glGetTransformFeedbackVarying = (void*)getProcAddress("glGetTransformFeedbackVarying");
		if (s_glGetTransformFeedbackVarying == null) missing.Add("glGetTransformFeedbackVarying");
		s_glGetTransformFeedbacki64_v = (void*)getProcAddress("glGetTransformFeedbacki64_v");
		if (s_glGetTransformFeedbacki64_v == null) missing.Add("glGetTransformFeedbacki64_v");
		s_glGetTransformFeedbacki_v = (void*)getProcAddress("glGetTransformFeedbacki_v");
		if (s_glGetTransformFeedbacki_v == null) missing.Add("glGetTransformFeedbacki_v");
		s_glGetTransformFeedbackiv = (void*)getProcAddress("glGetTransformFeedbackiv");
		if (s_glGetTransformFeedbackiv == null) missing.Add("glGetTransformFeedbackiv");
		s_glGetUniformBlockIndex = (void*)getProcAddress("glGetUniformBlockIndex");
		if (s_glGetUniformBlockIndex == null) missing.Add("glGetUniformBlockIndex");
		s_glGetUniformIndices = (void*)getProcAddress("glGetUniformIndices");
		if (s_glGetUniformIndices == null) missing.Add("glGetUniformIndices");
		s_glGetUniformLocation = (void*)getProcAddress("glGetUniformLocation");
		if (s_glGetUniformLocation == null) missing.Add("glGetUniformLocation");
		s_glGetUniformSubroutineuiv = (void*)getProcAddress("glGetUniformSubroutineuiv");
		if (s_glGetUniformSubroutineuiv == null) missing.Add("glGetUniformSubroutineuiv");
		s_glGetUniformdv = (void*)getProcAddress("glGetUniformdv");
		if (s_glGetUniformdv == null) missing.Add("glGetUniformdv");
		s_glGetUniformfv = (void*)getProcAddress("glGetUniformfv");
		if (s_glGetUniformfv == null) missing.Add("glGetUniformfv");
		s_glGetUniformiv = (void*)getProcAddress("glGetUniformiv");
		if (s_glGetUniformiv == null) missing.Add("glGetUniformiv");
		s_glGetUniformuiv = (void*)getProcAddress("glGetUniformuiv");
		if (s_glGetUniformuiv == null) missing.Add("glGetUniformuiv");
		s_glGetVertexArrayIndexed64iv = (void*)getProcAddress("glGetVertexArrayIndexed64iv");
		if (s_glGetVertexArrayIndexed64iv == null) missing.Add("glGetVertexArrayIndexed64iv");
		s_glGetVertexArrayIndexediv = (void*)getProcAddress("glGetVertexArrayIndexediv");
		if (s_glGetVertexArrayIndexediv == null) missing.Add("glGetVertexArrayIndexediv");
		s_glGetVertexArrayiv = (void*)getProcAddress("glGetVertexArrayiv");
		if (s_glGetVertexArrayiv == null) missing.Add("glGetVertexArrayiv");
		s_glGetVertexAttribIiv = (void*)getProcAddress("glGetVertexAttribIiv");
		if (s_glGetVertexAttribIiv == null) missing.Add("glGetVertexAttribIiv");
		s_glGetVertexAttribIuiv = (void*)getProcAddress("glGetVertexAttribIuiv");
		if (s_glGetVertexAttribIuiv == null) missing.Add("glGetVertexAttribIuiv");
		s_glGetVertexAttribLdv = (void*)getProcAddress("glGetVertexAttribLdv");
		if (s_glGetVertexAttribLdv == null) missing.Add("glGetVertexAttribLdv");
		s_glGetVertexAttribPointerv = (void*)getProcAddress("glGetVertexAttribPointerv");
		if (s_glGetVertexAttribPointerv == null) missing.Add("glGetVertexAttribPointerv");
		s_glGetVertexAttribdv = (void*)getProcAddress("glGetVertexAttribdv");
		if (s_glGetVertexAttribdv == null) missing.Add("glGetVertexAttribdv");
		s_glGetVertexAttribfv = (void*)getProcAddress("glGetVertexAttribfv");
		if (s_glGetVertexAttribfv == null) missing.Add("glGetVertexAttribfv");
		s_glGetVertexAttribiv = (void*)getProcAddress("glGetVertexAttribiv");
		if (s_glGetVertexAttribiv == null) missing.Add("glGetVertexAttribiv");
		s_glGetnColorTable = (void*)getProcAddress("glGetnColorTable");
		if (s_glGetnColorTable == null) missing.Add("glGetnColorTable");
		s_glGetnCompressedTexImage = (void*)getProcAddress("glGetnCompressedTexImage");
		if (s_glGetnCompressedTexImage == null) missing.Add("glGetnCompressedTexImage");
		s_glGetnConvolutionFilter = (void*)getProcAddress("glGetnConvolutionFilter");
		if (s_glGetnConvolutionFilter == null) missing.Add("glGetnConvolutionFilter");
		s_glGetnHistogram = (void*)getProcAddress("glGetnHistogram");
		if (s_glGetnHistogram == null) missing.Add("glGetnHistogram");
		s_glGetnMapdv = (void*)getProcAddress("glGetnMapdv");
		if (s_glGetnMapdv == null) missing.Add("glGetnMapdv");
		s_glGetnMapfv = (void*)getProcAddress("glGetnMapfv");
		if (s_glGetnMapfv == null) missing.Add("glGetnMapfv");
		s_glGetnMapiv = (void*)getProcAddress("glGetnMapiv");
		if (s_glGetnMapiv == null) missing.Add("glGetnMapiv");
		s_glGetnMinmax = (void*)getProcAddress("glGetnMinmax");
		if (s_glGetnMinmax == null) missing.Add("glGetnMinmax");
		s_glGetnPixelMapfv = (void*)getProcAddress("glGetnPixelMapfv");
		if (s_glGetnPixelMapfv == null) missing.Add("glGetnPixelMapfv");
		s_glGetnPixelMapuiv = (void*)getProcAddress("glGetnPixelMapuiv");
		if (s_glGetnPixelMapuiv == null) missing.Add("glGetnPixelMapuiv");
		s_glGetnPixelMapusv = (void*)getProcAddress("glGetnPixelMapusv");
		if (s_glGetnPixelMapusv == null) missing.Add("glGetnPixelMapusv");
		s_glGetnPolygonStipple = (void*)getProcAddress("glGetnPolygonStipple");
		if (s_glGetnPolygonStipple == null) missing.Add("glGetnPolygonStipple");
		s_glGetnSeparableFilter = (void*)getProcAddress("glGetnSeparableFilter");
		if (s_glGetnSeparableFilter == null) missing.Add("glGetnSeparableFilter");
		s_glGetnTexImage = (void*)getProcAddress("glGetnTexImage");
		if (s_glGetnTexImage == null) missing.Add("glGetnTexImage");
		s_glGetnUniformdv = (void*)getProcAddress("glGetnUniformdv");
		if (s_glGetnUniformdv == null) missing.Add("glGetnUniformdv");
		s_glGetnUniformfv = (void*)getProcAddress("glGetnUniformfv");
		if (s_glGetnUniformfv == null) missing.Add("glGetnUniformfv");
		s_glGetnUniformiv = (void*)getProcAddress("glGetnUniformiv");
		if (s_glGetnUniformiv == null) missing.Add("glGetnUniformiv");
		s_glGetnUniformuiv = (void*)getProcAddress("glGetnUniformuiv");
		if (s_glGetnUniformuiv == null) missing.Add("glGetnUniformuiv");
		s_glHint = (void*)getProcAddress("glHint");
		if (s_glHint == null) missing.Add("glHint");
		s_glInvalidateBufferData = (void*)getProcAddress("glInvalidateBufferData");
		if (s_glInvalidateBufferData == null) missing.Add("glInvalidateBufferData");
		s_glInvalidateBufferSubData = (void*)getProcAddress("glInvalidateBufferSubData");
		if (s_glInvalidateBufferSubData == null) missing.Add("glInvalidateBufferSubData");
		s_glInvalidateFramebuffer = (void*)getProcAddress("glInvalidateFramebuffer");
		if (s_glInvalidateFramebuffer == null) missing.Add("glInvalidateFramebuffer");
		s_glInvalidateNamedFramebufferData = (void*)getProcAddress("glInvalidateNamedFramebufferData");
		if (s_glInvalidateNamedFramebufferData == null) missing.Add("glInvalidateNamedFramebufferData");
		s_glInvalidateNamedFramebufferSubData = (void*)getProcAddress("glInvalidateNamedFramebufferSubData");
		if (s_glInvalidateNamedFramebufferSubData == null) missing.Add("glInvalidateNamedFramebufferSubData");
		s_glInvalidateSubFramebuffer = (void*)getProcAddress("glInvalidateSubFramebuffer");
		if (s_glInvalidateSubFramebuffer == null) missing.Add("glInvalidateSubFramebuffer");
		s_glInvalidateTexImage = (void*)getProcAddress("glInvalidateTexImage");
		if (s_glInvalidateTexImage == null) missing.Add("glInvalidateTexImage");
		s_glInvalidateTexSubImage = (void*)getProcAddress("glInvalidateTexSubImage");
		if (s_glInvalidateTexSubImage == null) missing.Add("glInvalidateTexSubImage");
		s_glIsBuffer = (void*)getProcAddress("glIsBuffer");
		if (s_glIsBuffer == null) missing.Add("glIsBuffer");
		s_glIsEnabled = (void*)getProcAddress("glIsEnabled");
		if (s_glIsEnabled == null) missing.Add("glIsEnabled");
		s_glIsEnabledi = (void*)getProcAddress("glIsEnabledi");
		if (s_glIsEnabledi == null) missing.Add("glIsEnabledi");
		s_glIsFramebuffer = (void*)getProcAddress("glIsFramebuffer");
		if (s_glIsFramebuffer == null) missing.Add("glIsFramebuffer");
		s_glIsProgram = (void*)getProcAddress("glIsProgram");
		if (s_glIsProgram == null) missing.Add("glIsProgram");
		s_glIsProgramPipeline = (void*)getProcAddress("glIsProgramPipeline");
		if (s_glIsProgramPipeline == null) missing.Add("glIsProgramPipeline");
		s_glIsQuery = (void*)getProcAddress("glIsQuery");
		if (s_glIsQuery == null) missing.Add("glIsQuery");
		s_glIsRenderbuffer = (void*)getProcAddress("glIsRenderbuffer");
		if (s_glIsRenderbuffer == null) missing.Add("glIsRenderbuffer");
		s_glIsSampler = (void*)getProcAddress("glIsSampler");
		if (s_glIsSampler == null) missing.Add("glIsSampler");
		s_glIsShader = (void*)getProcAddress("glIsShader");
		if (s_glIsShader == null) missing.Add("glIsShader");
		s_glIsSync = (void*)getProcAddress("glIsSync");
		if (s_glIsSync == null) missing.Add("glIsSync");
		s_glIsTexture = (void*)getProcAddress("glIsTexture");
		if (s_glIsTexture == null) missing.Add("glIsTexture");
		s_glIsTransformFeedback = (void*)getProcAddress("glIsTransformFeedback");
		if (s_glIsTransformFeedback == null) missing.Add("glIsTransformFeedback");
		s_glIsVertexArray = (void*)getProcAddress("glIsVertexArray");
		if (s_glIsVertexArray == null) missing.Add("glIsVertexArray");
		s_glLineWidth = (void*)getProcAddress("glLineWidth");
		if (s_glLineWidth == null) missing.Add("glLineWidth");
		s_glLinkProgram = (void*)getProcAddress("glLinkProgram");
		if (s_glLinkProgram == null) missing.Add("glLinkProgram");
		s_glLogicOp = (void*)getProcAddress("glLogicOp");
		if (s_glLogicOp == null) missing.Add("glLogicOp");
		s_glMapBuffer = (void*)getProcAddress("glMapBuffer");
		if (s_glMapBuffer == null) missing.Add("glMapBuffer");
		s_glMapBufferRange = (void*)getProcAddress("glMapBufferRange");
		if (s_glMapBufferRange == null) missing.Add("glMapBufferRange");
		s_glMapNamedBuffer = (void*)getProcAddress("glMapNamedBuffer");
		if (s_glMapNamedBuffer == null) missing.Add("glMapNamedBuffer");
		s_glMapNamedBufferRange = (void*)getProcAddress("glMapNamedBufferRange");
		if (s_glMapNamedBufferRange == null) missing.Add("glMapNamedBufferRange");
		s_glMemoryBarrier = (void*)getProcAddress("glMemoryBarrier");
		if (s_glMemoryBarrier == null) missing.Add("glMemoryBarrier");
		s_glMemoryBarrierByRegion = (void*)getProcAddress("glMemoryBarrierByRegion");
		if (s_glMemoryBarrierByRegion == null) missing.Add("glMemoryBarrierByRegion");
		s_glMinSampleShading = (void*)getProcAddress("glMinSampleShading");
		if (s_glMinSampleShading == null) missing.Add("glMinSampleShading");
		s_glMultiDrawArrays = (void*)getProcAddress("glMultiDrawArrays");
		if (s_glMultiDrawArrays == null) missing.Add("glMultiDrawArrays");
		s_glMultiDrawArraysIndirect = (void*)getProcAddress("glMultiDrawArraysIndirect");
		if (s_glMultiDrawArraysIndirect == null) missing.Add("glMultiDrawArraysIndirect");
		s_glMultiDrawArraysIndirectCount = (void*)getProcAddress("glMultiDrawArraysIndirectCount");
		if (s_glMultiDrawArraysIndirectCount == null) missing.Add("glMultiDrawArraysIndirectCount");
		s_glMultiDrawElements = (void*)getProcAddress("glMultiDrawElements");
		if (s_glMultiDrawElements == null) missing.Add("glMultiDrawElements");
		s_glMultiDrawElementsBaseVertex = (void*)getProcAddress("glMultiDrawElementsBaseVertex");
		if (s_glMultiDrawElementsBaseVertex == null) missing.Add("glMultiDrawElementsBaseVertex");
		s_glMultiDrawElementsIndirect = (void*)getProcAddress("glMultiDrawElementsIndirect");
		if (s_glMultiDrawElementsIndirect == null) missing.Add("glMultiDrawElementsIndirect");
		s_glMultiDrawElementsIndirectCount = (void*)getProcAddress("glMultiDrawElementsIndirectCount");
		if (s_glMultiDrawElementsIndirectCount == null) missing.Add("glMultiDrawElementsIndirectCount");
		s_glMultiTexCoordP1ui = (void*)getProcAddress("glMultiTexCoordP1ui");
		if (s_glMultiTexCoordP1ui == null) missing.Add("glMultiTexCoordP1ui");
		s_glMultiTexCoordP1uiv = (void*)getProcAddress("glMultiTexCoordP1uiv");
		if (s_glMultiTexCoordP1uiv == null) missing.Add("glMultiTexCoordP1uiv");
		s_glMultiTexCoordP2ui = (void*)getProcAddress("glMultiTexCoordP2ui");
		if (s_glMultiTexCoordP2ui == null) missing.Add("glMultiTexCoordP2ui");
		s_glMultiTexCoordP2uiv = (void*)getProcAddress("glMultiTexCoordP2uiv");
		if (s_glMultiTexCoordP2uiv == null) missing.Add("glMultiTexCoordP2uiv");
		s_glMultiTexCoordP3ui = (void*)getProcAddress("glMultiTexCoordP3ui");
		if (s_glMultiTexCoordP3ui == null) missing.Add("glMultiTexCoordP3ui");
		s_glMultiTexCoordP3uiv = (void*)getProcAddress("glMultiTexCoordP3uiv");
		if (s_glMultiTexCoordP3uiv == null) missing.Add("glMultiTexCoordP3uiv");
		s_glMultiTexCoordP4ui = (void*)getProcAddress("glMultiTexCoordP4ui");
		if (s_glMultiTexCoordP4ui == null) missing.Add("glMultiTexCoordP4ui");
		s_glMultiTexCoordP4uiv = (void*)getProcAddress("glMultiTexCoordP4uiv");
		if (s_glMultiTexCoordP4uiv == null) missing.Add("glMultiTexCoordP4uiv");
		s_glNamedBufferData = (void*)getProcAddress("glNamedBufferData");
		if (s_glNamedBufferData == null) missing.Add("glNamedBufferData");
		s_glNamedBufferStorage = (void*)getProcAddress("glNamedBufferStorage");
		if (s_glNamedBufferStorage == null) missing.Add("glNamedBufferStorage");
		s_glNamedBufferSubData = (void*)getProcAddress("glNamedBufferSubData");
		if (s_glNamedBufferSubData == null) missing.Add("glNamedBufferSubData");
		s_glNamedFramebufferDrawBuffer = (void*)getProcAddress("glNamedFramebufferDrawBuffer");
		if (s_glNamedFramebufferDrawBuffer == null) missing.Add("glNamedFramebufferDrawBuffer");
		s_glNamedFramebufferDrawBuffers = (void*)getProcAddress("glNamedFramebufferDrawBuffers");
		if (s_glNamedFramebufferDrawBuffers == null) missing.Add("glNamedFramebufferDrawBuffers");
		s_glNamedFramebufferParameteri = (void*)getProcAddress("glNamedFramebufferParameteri");
		if (s_glNamedFramebufferParameteri == null) missing.Add("glNamedFramebufferParameteri");
		s_glNamedFramebufferReadBuffer = (void*)getProcAddress("glNamedFramebufferReadBuffer");
		if (s_glNamedFramebufferReadBuffer == null) missing.Add("glNamedFramebufferReadBuffer");
		s_glNamedFramebufferRenderbuffer = (void*)getProcAddress("glNamedFramebufferRenderbuffer");
		if (s_glNamedFramebufferRenderbuffer == null) missing.Add("glNamedFramebufferRenderbuffer");
		s_glNamedFramebufferTexture = (void*)getProcAddress("glNamedFramebufferTexture");
		if (s_glNamedFramebufferTexture == null) missing.Add("glNamedFramebufferTexture");
		s_glNamedFramebufferTextureLayer = (void*)getProcAddress("glNamedFramebufferTextureLayer");
		if (s_glNamedFramebufferTextureLayer == null) missing.Add("glNamedFramebufferTextureLayer");
		s_glNamedRenderbufferStorage = (void*)getProcAddress("glNamedRenderbufferStorage");
		if (s_glNamedRenderbufferStorage == null) missing.Add("glNamedRenderbufferStorage");
		s_glNamedRenderbufferStorageMultisample = (void*)getProcAddress("glNamedRenderbufferStorageMultisample");
		if (s_glNamedRenderbufferStorageMultisample == null) missing.Add("glNamedRenderbufferStorageMultisample");
		s_glNormalP3ui = (void*)getProcAddress("glNormalP3ui");
		if (s_glNormalP3ui == null) missing.Add("glNormalP3ui");
		s_glNormalP3uiv = (void*)getProcAddress("glNormalP3uiv");
		if (s_glNormalP3uiv == null) missing.Add("glNormalP3uiv");
		s_glObjectLabel = (void*)getProcAddress("glObjectLabel");
		if (s_glObjectLabel == null) missing.Add("glObjectLabel");
		s_glObjectPtrLabel = (void*)getProcAddress("glObjectPtrLabel");
		if (s_glObjectPtrLabel == null) missing.Add("glObjectPtrLabel");
		s_glPatchParameterfv = (void*)getProcAddress("glPatchParameterfv");
		if (s_glPatchParameterfv == null) missing.Add("glPatchParameterfv");
		s_glPatchParameteri = (void*)getProcAddress("glPatchParameteri");
		if (s_glPatchParameteri == null) missing.Add("glPatchParameteri");
		s_glPauseTransformFeedback = (void*)getProcAddress("glPauseTransformFeedback");
		if (s_glPauseTransformFeedback == null) missing.Add("glPauseTransformFeedback");
		s_glPixelStoref = (void*)getProcAddress("glPixelStoref");
		if (s_glPixelStoref == null) missing.Add("glPixelStoref");
		s_glPixelStorei = (void*)getProcAddress("glPixelStorei");
		if (s_glPixelStorei == null) missing.Add("glPixelStorei");
		s_glPointParameterf = (void*)getProcAddress("glPointParameterf");
		if (s_glPointParameterf == null) missing.Add("glPointParameterf");
		s_glPointParameterfv = (void*)getProcAddress("glPointParameterfv");
		if (s_glPointParameterfv == null) missing.Add("glPointParameterfv");
		s_glPointParameteri = (void*)getProcAddress("glPointParameteri");
		if (s_glPointParameteri == null) missing.Add("glPointParameteri");
		s_glPointParameteriv = (void*)getProcAddress("glPointParameteriv");
		if (s_glPointParameteriv == null) missing.Add("glPointParameteriv");
		s_glPointSize = (void*)getProcAddress("glPointSize");
		if (s_glPointSize == null) missing.Add("glPointSize");
		s_glPolygonMode = (void*)getProcAddress("glPolygonMode");
		if (s_glPolygonMode == null) missing.Add("glPolygonMode");
		s_glPolygonOffset = (void*)getProcAddress("glPolygonOffset");
		if (s_glPolygonOffset == null) missing.Add("glPolygonOffset");
		s_glPolygonOffsetClamp = (void*)getProcAddress("glPolygonOffsetClamp");
		if (s_glPolygonOffsetClamp == null) missing.Add("glPolygonOffsetClamp");
		s_glPopDebugGroup = (void*)getProcAddress("glPopDebugGroup");
		if (s_glPopDebugGroup == null) missing.Add("glPopDebugGroup");
		s_glPrimitiveRestartIndex = (void*)getProcAddress("glPrimitiveRestartIndex");
		if (s_glPrimitiveRestartIndex == null) missing.Add("glPrimitiveRestartIndex");
		s_glProgramBinary = (void*)getProcAddress("glProgramBinary");
		if (s_glProgramBinary == null) missing.Add("glProgramBinary");
		s_glProgramParameteri = (void*)getProcAddress("glProgramParameteri");
		if (s_glProgramParameteri == null) missing.Add("glProgramParameteri");
		s_glProgramUniform1d = (void*)getProcAddress("glProgramUniform1d");
		if (s_glProgramUniform1d == null) missing.Add("glProgramUniform1d");
		s_glProgramUniform1dv = (void*)getProcAddress("glProgramUniform1dv");
		if (s_glProgramUniform1dv == null) missing.Add("glProgramUniform1dv");
		s_glProgramUniform1f = (void*)getProcAddress("glProgramUniform1f");
		if (s_glProgramUniform1f == null) missing.Add("glProgramUniform1f");
		s_glProgramUniform1fv = (void*)getProcAddress("glProgramUniform1fv");
		if (s_glProgramUniform1fv == null) missing.Add("glProgramUniform1fv");
		s_glProgramUniform1i = (void*)getProcAddress("glProgramUniform1i");
		if (s_glProgramUniform1i == null) missing.Add("glProgramUniform1i");
		s_glProgramUniform1iv = (void*)getProcAddress("glProgramUniform1iv");
		if (s_glProgramUniform1iv == null) missing.Add("glProgramUniform1iv");
		s_glProgramUniform1ui = (void*)getProcAddress("glProgramUniform1ui");
		if (s_glProgramUniform1ui == null) missing.Add("glProgramUniform1ui");
		s_glProgramUniform1uiv = (void*)getProcAddress("glProgramUniform1uiv");
		if (s_glProgramUniform1uiv == null) missing.Add("glProgramUniform1uiv");
		s_glProgramUniform2d = (void*)getProcAddress("glProgramUniform2d");
		if (s_glProgramUniform2d == null) missing.Add("glProgramUniform2d");
		s_glProgramUniform2dv = (void*)getProcAddress("glProgramUniform2dv");
		if (s_glProgramUniform2dv == null) missing.Add("glProgramUniform2dv");
		s_glProgramUniform2f = (void*)getProcAddress("glProgramUniform2f");
		if (s_glProgramUniform2f == null) missing.Add("glProgramUniform2f");
		s_glProgramUniform2fv = (void*)getProcAddress("glProgramUniform2fv");
		if (s_glProgramUniform2fv == null) missing.Add("glProgramUniform2fv");
		s_glProgramUniform2i = (void*)getProcAddress("glProgramUniform2i");
		if (s_glProgramUniform2i == null) missing.Add("glProgramUniform2i");
		s_glProgramUniform2iv = (void*)getProcAddress("glProgramUniform2iv");
		if (s_glProgramUniform2iv == null) missing.Add("glProgramUniform2iv");
		s_glProgramUniform2ui = (void*)getProcAddress("glProgramUniform2ui");
		if (s_glProgramUniform2ui == null) missing.Add("glProgramUniform2ui");
		s_glProgramUniform2uiv = (void*)getProcAddress("glProgramUniform2uiv");
		if (s_glProgramUniform2uiv == null) missing.Add("glProgramUniform2uiv");
		s_glProgramUniform3d = (void*)getProcAddress("glProgramUniform3d");
		if (s_glProgramUniform3d == null) missing.Add("glProgramUniform3d");
		s_glProgramUniform3dv = (void*)getProcAddress("glProgramUniform3dv");
		if (s_glProgramUniform3dv == null) missing.Add("glProgramUniform3dv");
		s_glProgramUniform3f = (void*)getProcAddress("glProgramUniform3f");
		if (s_glProgramUniform3f == null) missing.Add("glProgramUniform3f");
		s_glProgramUniform3fv = (void*)getProcAddress("glProgramUniform3fv");
		if (s_glProgramUniform3fv == null) missing.Add("glProgramUniform3fv");
		s_glProgramUniform3i = (void*)getProcAddress("glProgramUniform3i");
		if (s_glProgramUniform3i == null) missing.Add("glProgramUniform3i");
		s_glProgramUniform3iv = (void*)getProcAddress("glProgramUniform3iv");
		if (s_glProgramUniform3iv == null) missing.Add("glProgramUniform3iv");
		s_glProgramUniform3ui = (void*)getProcAddress("glProgramUniform3ui");
		if (s_glProgramUniform3ui == null) missing.Add("glProgramUniform3ui");
		s_glProgramUniform3uiv = (void*)getProcAddress("glProgramUniform3uiv");
		if (s_glProgramUniform3uiv == null) missing.Add("glProgramUniform3uiv");
		s_glProgramUniform4d = (void*)getProcAddress("glProgramUniform4d");
		if (s_glProgramUniform4d == null) missing.Add("glProgramUniform4d");
		s_glProgramUniform4dv = (void*)getProcAddress("glProgramUniform4dv");
		if (s_glProgramUniform4dv == null) missing.Add("glProgramUniform4dv");
		s_glProgramUniform4f = (void*)getProcAddress("glProgramUniform4f");
		if (s_glProgramUniform4f == null) missing.Add("glProgramUniform4f");
		s_glProgramUniform4fv = (void*)getProcAddress("glProgramUniform4fv");
		if (s_glProgramUniform4fv == null) missing.Add("glProgramUniform4fv");
		s_glProgramUniform4i = (void*)getProcAddress("glProgramUniform4i");
		if (s_glProgramUniform4i == null) missing.Add("glProgramUniform4i");
		s_glProgramUniform4iv = (void*)getProcAddress("glProgramUniform4iv");
		if (s_glProgramUniform4iv == null) missing.Add("glProgramUniform4iv");
		s_glProgramUniform4ui = (void*)getProcAddress("glProgramUniform4ui");
		if (s_glProgramUniform4ui == null) missing.Add("glProgramUniform4ui");
		s_glProgramUniform4uiv = (void*)getProcAddress("glProgramUniform4uiv");
		if (s_glProgramUniform4uiv == null) missing.Add("glProgramUniform4uiv");
		s_glProgramUniformMatrix2dv = (void*)getProcAddress("glProgramUniformMatrix2dv");
		if (s_glProgramUniformMatrix2dv == null) missing.Add("glProgramUniformMatrix2dv");
		s_glProgramUniformMatrix2fv = (void*)getProcAddress("glProgramUniformMatrix2fv");
		if (s_glProgramUniformMatrix2fv == null) missing.Add("glProgramUniformMatrix2fv");
		s_glProgramUniformMatrix2x3dv = (void*)getProcAddress("glProgramUniformMatrix2x3dv");
		if (s_glProgramUniformMatrix2x3dv == null) missing.Add("glProgramUniformMatrix2x3dv");
		s_glProgramUniformMatrix2x3fv = (void*)getProcAddress("glProgramUniformMatrix2x3fv");
		if (s_glProgramUniformMatrix2x3fv == null) missing.Add("glProgramUniformMatrix2x3fv");
		s_glProgramUniformMatrix2x4dv = (void*)getProcAddress("glProgramUniformMatrix2x4dv");
		if (s_glProgramUniformMatrix2x4dv == null) missing.Add("glProgramUniformMatrix2x4dv");
		s_glProgramUniformMatrix2x4fv = (void*)getProcAddress("glProgramUniformMatrix2x4fv");
		if (s_glProgramUniformMatrix2x4fv == null) missing.Add("glProgramUniformMatrix2x4fv");
		s_glProgramUniformMatrix3dv = (void*)getProcAddress("glProgramUniformMatrix3dv");
		if (s_glProgramUniformMatrix3dv == null) missing.Add("glProgramUniformMatrix3dv");
		s_glProgramUniformMatrix3fv = (void*)getProcAddress("glProgramUniformMatrix3fv");
		if (s_glProgramUniformMatrix3fv == null) missing.Add("glProgramUniformMatrix3fv");
		s_glProgramUniformMatrix3x2dv = (void*)getProcAddress("glProgramUniformMatrix3x2dv");
		if (s_glProgramUniformMatrix3x2dv == null) missing.Add("glProgramUniformMatrix3x2dv");
		s_glProgramUniformMatrix3x2fv = (void*)getProcAddress("glProgramUniformMatrix3x2fv");
		if (s_glProgramUniformMatrix3x2fv == null) missing.Add("glProgramUniformMatrix3x2fv");
		s_glProgramUniformMatrix3x4dv = (void*)getProcAddress("glProgramUniformMatrix3x4dv");
		if (s_glProgramUniformMatrix3x4dv == null) missing.Add("glProgramUniformMatrix3x4dv");
		s_glProgramUniformMatrix3x4fv = (void*)getProcAddress("glProgramUniformMatrix3x4fv");
		if (s_glProgramUniformMatrix3x4fv == null) missing.Add("glProgramUniformMatrix3x4fv");
		s_glProgramUniformMatrix4dv = (void*)getProcAddress("glProgramUniformMatrix4dv");
		if (s_glProgramUniformMatrix4dv == null) missing.Add("glProgramUniformMatrix4dv");
		s_glProgramUniformMatrix4fv = (void*)getProcAddress("glProgramUniformMatrix4fv");
		if (s_glProgramUniformMatrix4fv == null) missing.Add("glProgramUniformMatrix4fv");
		s_glProgramUniformMatrix4x2dv = (void*)getProcAddress("glProgramUniformMatrix4x2dv");
		if (s_glProgramUniformMatrix4x2dv == null) missing.Add("glProgramUniformMatrix4x2dv");
		s_glProgramUniformMatrix4x2fv = (void*)getProcAddress("glProgramUniformMatrix4x2fv");
		if (s_glProgramUniformMatrix4x2fv == null) missing.Add("glProgramUniformMatrix4x2fv");
		s_glProgramUniformMatrix4x3dv = (void*)getProcAddress("glProgramUniformMatrix4x3dv");
		if (s_glProgramUniformMatrix4x3dv == null) missing.Add("glProgramUniformMatrix4x3dv");
		s_glProgramUniformMatrix4x3fv = (void*)getProcAddress("glProgramUniformMatrix4x3fv");
		if (s_glProgramUniformMatrix4x3fv == null) missing.Add("glProgramUniformMatrix4x3fv");
		s_glProvokingVertex = (void*)getProcAddress("glProvokingVertex");
		if (s_glProvokingVertex == null) missing.Add("glProvokingVertex");
		s_glPushDebugGroup = (void*)getProcAddress("glPushDebugGroup");
		if (s_glPushDebugGroup == null) missing.Add("glPushDebugGroup");
		s_glQueryCounter = (void*)getProcAddress("glQueryCounter");
		if (s_glQueryCounter == null) missing.Add("glQueryCounter");
		s_glReadBuffer = (void*)getProcAddress("glReadBuffer");
		if (s_glReadBuffer == null) missing.Add("glReadBuffer");
		s_glReadPixels = (void*)getProcAddress("glReadPixels");
		if (s_glReadPixels == null) missing.Add("glReadPixels");
		s_glReadnPixels = (void*)getProcAddress("glReadnPixels");
		if (s_glReadnPixels == null) missing.Add("glReadnPixels");
		s_glReleaseShaderCompiler = (void*)getProcAddress("glReleaseShaderCompiler");
		if (s_glReleaseShaderCompiler == null) missing.Add("glReleaseShaderCompiler");
		s_glRenderbufferStorage = (void*)getProcAddress("glRenderbufferStorage");
		if (s_glRenderbufferStorage == null) missing.Add("glRenderbufferStorage");
		s_glRenderbufferStorageMultisample = (void*)getProcAddress("glRenderbufferStorageMultisample");
		if (s_glRenderbufferStorageMultisample == null) missing.Add("glRenderbufferStorageMultisample");
		s_glResumeTransformFeedback = (void*)getProcAddress("glResumeTransformFeedback");
		if (s_glResumeTransformFeedback == null) missing.Add("glResumeTransformFeedback");
		s_glSampleCoverage = (void*)getProcAddress("glSampleCoverage");
		if (s_glSampleCoverage == null) missing.Add("glSampleCoverage");
		s_glSampleMaski = (void*)getProcAddress("glSampleMaski");
		if (s_glSampleMaski == null) missing.Add("glSampleMaski");
		s_glSamplerParameterIiv = (void*)getProcAddress("glSamplerParameterIiv");
		if (s_glSamplerParameterIiv == null) missing.Add("glSamplerParameterIiv");
		s_glSamplerParameterIuiv = (void*)getProcAddress("glSamplerParameterIuiv");
		if (s_glSamplerParameterIuiv == null) missing.Add("glSamplerParameterIuiv");
		s_glSamplerParameterf = (void*)getProcAddress("glSamplerParameterf");
		if (s_glSamplerParameterf == null) missing.Add("glSamplerParameterf");
		s_glSamplerParameterfv = (void*)getProcAddress("glSamplerParameterfv");
		if (s_glSamplerParameterfv == null) missing.Add("glSamplerParameterfv");
		s_glSamplerParameteri = (void*)getProcAddress("glSamplerParameteri");
		if (s_glSamplerParameteri == null) missing.Add("glSamplerParameteri");
		s_glSamplerParameteriv = (void*)getProcAddress("glSamplerParameteriv");
		if (s_glSamplerParameteriv == null) missing.Add("glSamplerParameteriv");
		s_glScissor = (void*)getProcAddress("glScissor");
		if (s_glScissor == null) missing.Add("glScissor");
		s_glScissorArrayv = (void*)getProcAddress("glScissorArrayv");
		if (s_glScissorArrayv == null) missing.Add("glScissorArrayv");
		s_glScissorIndexed = (void*)getProcAddress("glScissorIndexed");
		if (s_glScissorIndexed == null) missing.Add("glScissorIndexed");
		s_glScissorIndexedv = (void*)getProcAddress("glScissorIndexedv");
		if (s_glScissorIndexedv == null) missing.Add("glScissorIndexedv");
		s_glSecondaryColorP3ui = (void*)getProcAddress("glSecondaryColorP3ui");
		if (s_glSecondaryColorP3ui == null) missing.Add("glSecondaryColorP3ui");
		s_glSecondaryColorP3uiv = (void*)getProcAddress("glSecondaryColorP3uiv");
		if (s_glSecondaryColorP3uiv == null) missing.Add("glSecondaryColorP3uiv");
		s_glShaderBinary = (void*)getProcAddress("glShaderBinary");
		if (s_glShaderBinary == null) missing.Add("glShaderBinary");
		s_glShaderSource = (void*)getProcAddress("glShaderSource");
		if (s_glShaderSource == null) missing.Add("glShaderSource");
		s_glShaderStorageBlockBinding = (void*)getProcAddress("glShaderStorageBlockBinding");
		if (s_glShaderStorageBlockBinding == null) missing.Add("glShaderStorageBlockBinding");
		s_glSpecializeShader = (void*)getProcAddress("glSpecializeShader");
		if (s_glSpecializeShader == null) missing.Add("glSpecializeShader");
		s_glStencilFunc = (void*)getProcAddress("glStencilFunc");
		if (s_glStencilFunc == null) missing.Add("glStencilFunc");
		s_glStencilFuncSeparate = (void*)getProcAddress("glStencilFuncSeparate");
		if (s_glStencilFuncSeparate == null) missing.Add("glStencilFuncSeparate");
		s_glStencilMask = (void*)getProcAddress("glStencilMask");
		if (s_glStencilMask == null) missing.Add("glStencilMask");
		s_glStencilMaskSeparate = (void*)getProcAddress("glStencilMaskSeparate");
		if (s_glStencilMaskSeparate == null) missing.Add("glStencilMaskSeparate");
		s_glStencilOp = (void*)getProcAddress("glStencilOp");
		if (s_glStencilOp == null) missing.Add("glStencilOp");
		s_glStencilOpSeparate = (void*)getProcAddress("glStencilOpSeparate");
		if (s_glStencilOpSeparate == null) missing.Add("glStencilOpSeparate");
		s_glTexBuffer = (void*)getProcAddress("glTexBuffer");
		if (s_glTexBuffer == null) missing.Add("glTexBuffer");
		s_glTexBufferRange = (void*)getProcAddress("glTexBufferRange");
		if (s_glTexBufferRange == null) missing.Add("glTexBufferRange");
		s_glTexCoordP1ui = (void*)getProcAddress("glTexCoordP1ui");
		if (s_glTexCoordP1ui == null) missing.Add("glTexCoordP1ui");
		s_glTexCoordP1uiv = (void*)getProcAddress("glTexCoordP1uiv");
		if (s_glTexCoordP1uiv == null) missing.Add("glTexCoordP1uiv");
		s_glTexCoordP2ui = (void*)getProcAddress("glTexCoordP2ui");
		if (s_glTexCoordP2ui == null) missing.Add("glTexCoordP2ui");
		s_glTexCoordP2uiv = (void*)getProcAddress("glTexCoordP2uiv");
		if (s_glTexCoordP2uiv == null) missing.Add("glTexCoordP2uiv");
		s_glTexCoordP3ui = (void*)getProcAddress("glTexCoordP3ui");
		if (s_glTexCoordP3ui == null) missing.Add("glTexCoordP3ui");
		s_glTexCoordP3uiv = (void*)getProcAddress("glTexCoordP3uiv");
		if (s_glTexCoordP3uiv == null) missing.Add("glTexCoordP3uiv");
		s_glTexCoordP4ui = (void*)getProcAddress("glTexCoordP4ui");
		if (s_glTexCoordP4ui == null) missing.Add("glTexCoordP4ui");
		s_glTexCoordP4uiv = (void*)getProcAddress("glTexCoordP4uiv");
		if (s_glTexCoordP4uiv == null) missing.Add("glTexCoordP4uiv");
		s_glTexImage1D = (void*)getProcAddress("glTexImage1D");
		if (s_glTexImage1D == null) missing.Add("glTexImage1D");
		s_glTexImage2D = (void*)getProcAddress("glTexImage2D");
		if (s_glTexImage2D == null) missing.Add("glTexImage2D");
		s_glTexImage2DMultisample = (void*)getProcAddress("glTexImage2DMultisample");
		if (s_glTexImage2DMultisample == null) missing.Add("glTexImage2DMultisample");
		s_glTexImage3D = (void*)getProcAddress("glTexImage3D");
		if (s_glTexImage3D == null) missing.Add("glTexImage3D");
		s_glTexImage3DMultisample = (void*)getProcAddress("glTexImage3DMultisample");
		if (s_glTexImage3DMultisample == null) missing.Add("glTexImage3DMultisample");
		s_glTexParameterIiv = (void*)getProcAddress("glTexParameterIiv");
		if (s_glTexParameterIiv == null) missing.Add("glTexParameterIiv");
		s_glTexParameterIuiv = (void*)getProcAddress("glTexParameterIuiv");
		if (s_glTexParameterIuiv == null) missing.Add("glTexParameterIuiv");
		s_glTexParameterf = (void*)getProcAddress("glTexParameterf");
		if (s_glTexParameterf == null) missing.Add("glTexParameterf");
		s_glTexParameterfv = (void*)getProcAddress("glTexParameterfv");
		if (s_glTexParameterfv == null) missing.Add("glTexParameterfv");
		s_glTexParameteri = (void*)getProcAddress("glTexParameteri");
		if (s_glTexParameteri == null) missing.Add("glTexParameteri");
		s_glTexParameteriv = (void*)getProcAddress("glTexParameteriv");
		if (s_glTexParameteriv == null) missing.Add("glTexParameteriv");
		s_glTexStorage1D = (void*)getProcAddress("glTexStorage1D");
		if (s_glTexStorage1D == null) missing.Add("glTexStorage1D");
		s_glTexStorage2D = (void*)getProcAddress("glTexStorage2D");
		if (s_glTexStorage2D == null) missing.Add("glTexStorage2D");
		s_glTexStorage2DMultisample = (void*)getProcAddress("glTexStorage2DMultisample");
		if (s_glTexStorage2DMultisample == null) missing.Add("glTexStorage2DMultisample");
		s_glTexStorage3D = (void*)getProcAddress("glTexStorage3D");
		if (s_glTexStorage3D == null) missing.Add("glTexStorage3D");
		s_glTexStorage3DMultisample = (void*)getProcAddress("glTexStorage3DMultisample");
		if (s_glTexStorage3DMultisample == null) missing.Add("glTexStorage3DMultisample");
		s_glTexSubImage1D = (void*)getProcAddress("glTexSubImage1D");
		if (s_glTexSubImage1D == null) missing.Add("glTexSubImage1D");
		s_glTexSubImage2D = (void*)getProcAddress("glTexSubImage2D");
		if (s_glTexSubImage2D == null) missing.Add("glTexSubImage2D");
		s_glTexSubImage3D = (void*)getProcAddress("glTexSubImage3D");
		if (s_glTexSubImage3D == null) missing.Add("glTexSubImage3D");
		s_glTextureBarrier = (void*)getProcAddress("glTextureBarrier");
		if (s_glTextureBarrier == null) missing.Add("glTextureBarrier");
		s_glTextureBuffer = (void*)getProcAddress("glTextureBuffer");
		if (s_glTextureBuffer == null) missing.Add("glTextureBuffer");
		s_glTextureBufferRange = (void*)getProcAddress("glTextureBufferRange");
		if (s_glTextureBufferRange == null) missing.Add("glTextureBufferRange");
		s_glTextureParameterIiv = (void*)getProcAddress("glTextureParameterIiv");
		if (s_glTextureParameterIiv == null) missing.Add("glTextureParameterIiv");
		s_glTextureParameterIuiv = (void*)getProcAddress("glTextureParameterIuiv");
		if (s_glTextureParameterIuiv == null) missing.Add("glTextureParameterIuiv");
		s_glTextureParameterf = (void*)getProcAddress("glTextureParameterf");
		if (s_glTextureParameterf == null) missing.Add("glTextureParameterf");
		s_glTextureParameterfv = (void*)getProcAddress("glTextureParameterfv");
		if (s_glTextureParameterfv == null) missing.Add("glTextureParameterfv");
		s_glTextureParameteri = (void*)getProcAddress("glTextureParameteri");
		if (s_glTextureParameteri == null) missing.Add("glTextureParameteri");
		s_glTextureParameteriv = (void*)getProcAddress("glTextureParameteriv");
		if (s_glTextureParameteriv == null) missing.Add("glTextureParameteriv");
		s_glTextureStorage1D = (void*)getProcAddress("glTextureStorage1D");
		if (s_glTextureStorage1D == null) missing.Add("glTextureStorage1D");
		s_glTextureStorage2D = (void*)getProcAddress("glTextureStorage2D");
		if (s_glTextureStorage2D == null) missing.Add("glTextureStorage2D");
		s_glTextureStorage2DMultisample = (void*)getProcAddress("glTextureStorage2DMultisample");
		if (s_glTextureStorage2DMultisample == null) missing.Add("glTextureStorage2DMultisample");
		s_glTextureStorage3D = (void*)getProcAddress("glTextureStorage3D");
		if (s_glTextureStorage3D == null) missing.Add("glTextureStorage3D");
		s_glTextureStorage3DMultisample = (void*)getProcAddress("glTextureStorage3DMultisample");
		if (s_glTextureStorage3DMultisample == null) missing.Add("glTextureStorage3DMultisample");
		s_glTextureSubImage1D = (void*)getProcAddress("glTextureSubImage1D");
		if (s_glTextureSubImage1D == null) missing.Add("glTextureSubImage1D");
		s_glTextureSubImage2D = (void*)getProcAddress("glTextureSubImage2D");
		if (s_glTextureSubImage2D == null) missing.Add("glTextureSubImage2D");
		s_glTextureSubImage3D = (void*)getProcAddress("glTextureSubImage3D");
		if (s_glTextureSubImage3D == null) missing.Add("glTextureSubImage3D");
		s_glTextureView = (void*)getProcAddress("glTextureView");
		if (s_glTextureView == null) missing.Add("glTextureView");
		s_glTransformFeedbackBufferBase = (void*)getProcAddress("glTransformFeedbackBufferBase");
		if (s_glTransformFeedbackBufferBase == null) missing.Add("glTransformFeedbackBufferBase");
		s_glTransformFeedbackBufferRange = (void*)getProcAddress("glTransformFeedbackBufferRange");
		if (s_glTransformFeedbackBufferRange == null) missing.Add("glTransformFeedbackBufferRange");
		s_glTransformFeedbackVaryings = (void*)getProcAddress("glTransformFeedbackVaryings");
		if (s_glTransformFeedbackVaryings == null) missing.Add("glTransformFeedbackVaryings");
		s_glUniform1d = (void*)getProcAddress("glUniform1d");
		if (s_glUniform1d == null) missing.Add("glUniform1d");
		s_glUniform1dv = (void*)getProcAddress("glUniform1dv");
		if (s_glUniform1dv == null) missing.Add("glUniform1dv");
		s_glUniform1f = (void*)getProcAddress("glUniform1f");
		if (s_glUniform1f == null) missing.Add("glUniform1f");
		s_glUniform1fv = (void*)getProcAddress("glUniform1fv");
		if (s_glUniform1fv == null) missing.Add("glUniform1fv");
		s_glUniform1i = (void*)getProcAddress("glUniform1i");
		if (s_glUniform1i == null) missing.Add("glUniform1i");
		s_glUniform1iv = (void*)getProcAddress("glUniform1iv");
		if (s_glUniform1iv == null) missing.Add("glUniform1iv");
		s_glUniform1ui = (void*)getProcAddress("glUniform1ui");
		if (s_glUniform1ui == null) missing.Add("glUniform1ui");
		s_glUniform1uiv = (void*)getProcAddress("glUniform1uiv");
		if (s_glUniform1uiv == null) missing.Add("glUniform1uiv");
		s_glUniform2d = (void*)getProcAddress("glUniform2d");
		if (s_glUniform2d == null) missing.Add("glUniform2d");
		s_glUniform2dv = (void*)getProcAddress("glUniform2dv");
		if (s_glUniform2dv == null) missing.Add("glUniform2dv");
		s_glUniform2f = (void*)getProcAddress("glUniform2f");
		if (s_glUniform2f == null) missing.Add("glUniform2f");
		s_glUniform2fv = (void*)getProcAddress("glUniform2fv");
		if (s_glUniform2fv == null) missing.Add("glUniform2fv");
		s_glUniform2i = (void*)getProcAddress("glUniform2i");
		if (s_glUniform2i == null) missing.Add("glUniform2i");
		s_glUniform2iv = (void*)getProcAddress("glUniform2iv");
		if (s_glUniform2iv == null) missing.Add("glUniform2iv");
		s_glUniform2ui = (void*)getProcAddress("glUniform2ui");
		if (s_glUniform2ui == null) missing.Add("glUniform2ui");
		s_glUniform2uiv = (void*)getProcAddress("glUniform2uiv");
		if (s_glUniform2uiv == null) missing.Add("glUniform2uiv");
		s_glUniform3d = (void*)getProcAddress("glUniform3d");
		if (s_glUniform3d == null) missing.Add("glUniform3d");
		s_glUniform3dv = (void*)getProcAddress("glUniform3dv");
		if (s_glUniform3dv == null) missing.Add("glUniform3dv");
		s_glUniform3f = (void*)getProcAddress("glUniform3f");
		if (s_glUniform3f == null) missing.Add("glUniform3f");
		s_glUniform3fv = (void*)getProcAddress("glUniform3fv");
		if (s_glUniform3fv == null) missing.Add("glUniform3fv");
		s_glUniform3i = (void*)getProcAddress("glUniform3i");
		if (s_glUniform3i == null) missing.Add("glUniform3i");
		s_glUniform3iv = (void*)getProcAddress("glUniform3iv");
		if (s_glUniform3iv == null) missing.Add("glUniform3iv");
		s_glUniform3ui = (void*)getProcAddress("glUniform3ui");
		if (s_glUniform3ui == null) missing.Add("glUniform3ui");
		s_glUniform3uiv = (void*)getProcAddress("glUniform3uiv");
		if (s_glUniform3uiv == null) missing.Add("glUniform3uiv");
		s_glUniform4d = (void*)getProcAddress("glUniform4d");
		if (s_glUniform4d == null) missing.Add("glUniform4d");
		s_glUniform4dv = (void*)getProcAddress("glUniform4dv");
		if (s_glUniform4dv == null) missing.Add("glUniform4dv");
		s_glUniform4f = (void*)getProcAddress("glUniform4f");
		if (s_glUniform4f == null) missing.Add("glUniform4f");
		s_glUniform4fv = (void*)getProcAddress("glUniform4fv");
		if (s_glUniform4fv == null) missing.Add("glUniform4fv");
		s_glUniform4i = (void*)getProcAddress("glUniform4i");
		if (s_glUniform4i == null) missing.Add("glUniform4i");
		s_glUniform4iv = (void*)getProcAddress("glUniform4iv");
		if (s_glUniform4iv == null) missing.Add("glUniform4iv");
		s_glUniform4ui = (void*)getProcAddress("glUniform4ui");
		if (s_glUniform4ui == null) missing.Add("glUniform4ui");
		s_glUniform4uiv = (void*)getProcAddress("glUniform4uiv");
		if (s_glUniform4uiv == null) missing.Add("glUniform4uiv");
		s_glUniformBlockBinding = (void*)getProcAddress("glUniformBlockBinding");
		if (s_glUniformBlockBinding == null) missing.Add("glUniformBlockBinding");
		s_glUniformMatrix2dv = (void*)getProcAddress("glUniformMatrix2dv");
		if (s_glUniformMatrix2dv == null) missing.Add("glUniformMatrix2dv");
		s_glUniformMatrix2fv = (void*)getProcAddress("glUniformMatrix2fv");
		if (s_glUniformMatrix2fv == null) missing.Add("glUniformMatrix2fv");
		s_glUniformMatrix2x3dv = (void*)getProcAddress("glUniformMatrix2x3dv");
		if (s_glUniformMatrix2x3dv == null) missing.Add("glUniformMatrix2x3dv");
		s_glUniformMatrix2x3fv = (void*)getProcAddress("glUniformMatrix2x3fv");
		if (s_glUniformMatrix2x3fv == null) missing.Add("glUniformMatrix2x3fv");
		s_glUniformMatrix2x4dv = (void*)getProcAddress("glUniformMatrix2x4dv");
		if (s_glUniformMatrix2x4dv == null) missing.Add("glUniformMatrix2x4dv");
		s_glUniformMatrix2x4fv = (void*)getProcAddress("glUniformMatrix2x4fv");
		if (s_glUniformMatrix2x4fv == null) missing.Add("glUniformMatrix2x4fv");
		s_glUniformMatrix3dv = (void*)getProcAddress("glUniformMatrix3dv");
		if (s_glUniformMatrix3dv == null) missing.Add("glUniformMatrix3dv");
		s_glUniformMatrix3fv = (void*)getProcAddress("glUniformMatrix3fv");
		if (s_glUniformMatrix3fv == null) missing.Add("glUniformMatrix3fv");
		s_glUniformMatrix3x2dv = (void*)getProcAddress("glUniformMatrix3x2dv");
		if (s_glUniformMatrix3x2dv == null) missing.Add("glUniformMatrix3x2dv");
		s_glUniformMatrix3x2fv = (void*)getProcAddress("glUniformMatrix3x2fv");
		if (s_glUniformMatrix3x2fv == null) missing.Add("glUniformMatrix3x2fv");
		s_glUniformMatrix3x4dv = (void*)getProcAddress("glUniformMatrix3x4dv");
		if (s_glUniformMatrix3x4dv == null) missing.Add("glUniformMatrix3x4dv");
		s_glUniformMatrix3x4fv = (void*)getProcAddress("glUniformMatrix3x4fv");
		if (s_glUniformMatrix3x4fv == null) missing.Add("glUniformMatrix3x4fv");
		s_glUniformMatrix4dv = (void*)getProcAddress("glUniformMatrix4dv");
		if (s_glUniformMatrix4dv == null) missing.Add("glUniformMatrix4dv");
		s_glUniformMatrix4fv = (void*)getProcAddress("glUniformMatrix4fv");
		if (s_glUniformMatrix4fv == null) missing.Add("glUniformMatrix4fv");
		s_glUniformMatrix4x2dv = (void*)getProcAddress("glUniformMatrix4x2dv");
		if (s_glUniformMatrix4x2dv == null) missing.Add("glUniformMatrix4x2dv");
		s_glUniformMatrix4x2fv = (void*)getProcAddress("glUniformMatrix4x2fv");
		if (s_glUniformMatrix4x2fv == null) missing.Add("glUniformMatrix4x2fv");
		s_glUniformMatrix4x3dv = (void*)getProcAddress("glUniformMatrix4x3dv");
		if (s_glUniformMatrix4x3dv == null) missing.Add("glUniformMatrix4x3dv");
		s_glUniformMatrix4x3fv = (void*)getProcAddress("glUniformMatrix4x3fv");
		if (s_glUniformMatrix4x3fv == null) missing.Add("glUniformMatrix4x3fv");
		s_glUniformSubroutinesuiv = (void*)getProcAddress("glUniformSubroutinesuiv");
		if (s_glUniformSubroutinesuiv == null) missing.Add("glUniformSubroutinesuiv");
		s_glUnmapBuffer = (void*)getProcAddress("glUnmapBuffer");
		if (s_glUnmapBuffer == null) missing.Add("glUnmapBuffer");
		s_glUnmapNamedBuffer = (void*)getProcAddress("glUnmapNamedBuffer");
		if (s_glUnmapNamedBuffer == null) missing.Add("glUnmapNamedBuffer");
		s_glUseProgram = (void*)getProcAddress("glUseProgram");
		if (s_glUseProgram == null) missing.Add("glUseProgram");
		s_glUseProgramStages = (void*)getProcAddress("glUseProgramStages");
		if (s_glUseProgramStages == null) missing.Add("glUseProgramStages");
		s_glValidateProgram = (void*)getProcAddress("glValidateProgram");
		if (s_glValidateProgram == null) missing.Add("glValidateProgram");
		s_glValidateProgramPipeline = (void*)getProcAddress("glValidateProgramPipeline");
		if (s_glValidateProgramPipeline == null) missing.Add("glValidateProgramPipeline");
		s_glVertexArrayAttribBinding = (void*)getProcAddress("glVertexArrayAttribBinding");
		if (s_glVertexArrayAttribBinding == null) missing.Add("glVertexArrayAttribBinding");
		s_glVertexArrayAttribFormat = (void*)getProcAddress("glVertexArrayAttribFormat");
		if (s_glVertexArrayAttribFormat == null) missing.Add("glVertexArrayAttribFormat");
		s_glVertexArrayAttribIFormat = (void*)getProcAddress("glVertexArrayAttribIFormat");
		if (s_glVertexArrayAttribIFormat == null) missing.Add("glVertexArrayAttribIFormat");
		s_glVertexArrayAttribLFormat = (void*)getProcAddress("glVertexArrayAttribLFormat");
		if (s_glVertexArrayAttribLFormat == null) missing.Add("glVertexArrayAttribLFormat");
		s_glVertexArrayBindingDivisor = (void*)getProcAddress("glVertexArrayBindingDivisor");
		if (s_glVertexArrayBindingDivisor == null) missing.Add("glVertexArrayBindingDivisor");
		s_glVertexArrayElementBuffer = (void*)getProcAddress("glVertexArrayElementBuffer");
		if (s_glVertexArrayElementBuffer == null) missing.Add("glVertexArrayElementBuffer");
		s_glVertexArrayVertexBuffer = (void*)getProcAddress("glVertexArrayVertexBuffer");
		if (s_glVertexArrayVertexBuffer == null) missing.Add("glVertexArrayVertexBuffer");
		s_glVertexArrayVertexBuffers = (void*)getProcAddress("glVertexArrayVertexBuffers");
		if (s_glVertexArrayVertexBuffers == null) missing.Add("glVertexArrayVertexBuffers");
		s_glVertexAttrib1d = (void*)getProcAddress("glVertexAttrib1d");
		if (s_glVertexAttrib1d == null) missing.Add("glVertexAttrib1d");
		s_glVertexAttrib1dv = (void*)getProcAddress("glVertexAttrib1dv");
		if (s_glVertexAttrib1dv == null) missing.Add("glVertexAttrib1dv");
		s_glVertexAttrib1f = (void*)getProcAddress("glVertexAttrib1f");
		if (s_glVertexAttrib1f == null) missing.Add("glVertexAttrib1f");
		s_glVertexAttrib1fv = (void*)getProcAddress("glVertexAttrib1fv");
		if (s_glVertexAttrib1fv == null) missing.Add("glVertexAttrib1fv");
		s_glVertexAttrib1s = (void*)getProcAddress("glVertexAttrib1s");
		if (s_glVertexAttrib1s == null) missing.Add("glVertexAttrib1s");
		s_glVertexAttrib1sv = (void*)getProcAddress("glVertexAttrib1sv");
		if (s_glVertexAttrib1sv == null) missing.Add("glVertexAttrib1sv");
		s_glVertexAttrib2d = (void*)getProcAddress("glVertexAttrib2d");
		if (s_glVertexAttrib2d == null) missing.Add("glVertexAttrib2d");
		s_glVertexAttrib2dv = (void*)getProcAddress("glVertexAttrib2dv");
		if (s_glVertexAttrib2dv == null) missing.Add("glVertexAttrib2dv");
		s_glVertexAttrib2f = (void*)getProcAddress("glVertexAttrib2f");
		if (s_glVertexAttrib2f == null) missing.Add("glVertexAttrib2f");
		s_glVertexAttrib2fv = (void*)getProcAddress("glVertexAttrib2fv");
		if (s_glVertexAttrib2fv == null) missing.Add("glVertexAttrib2fv");
		s_glVertexAttrib2s = (void*)getProcAddress("glVertexAttrib2s");
		if (s_glVertexAttrib2s == null) missing.Add("glVertexAttrib2s");
		s_glVertexAttrib2sv = (void*)getProcAddress("glVertexAttrib2sv");
		if (s_glVertexAttrib2sv == null) missing.Add("glVertexAttrib2sv");
		s_glVertexAttrib3d = (void*)getProcAddress("glVertexAttrib3d");
		if (s_glVertexAttrib3d == null) missing.Add("glVertexAttrib3d");
		s_glVertexAttrib3dv = (void*)getProcAddress("glVertexAttrib3dv");
		if (s_glVertexAttrib3dv == null) missing.Add("glVertexAttrib3dv");
		s_glVertexAttrib3f = (void*)getProcAddress("glVertexAttrib3f");
		if (s_glVertexAttrib3f == null) missing.Add("glVertexAttrib3f");
		s_glVertexAttrib3fv = (void*)getProcAddress("glVertexAttrib3fv");
		if (s_glVertexAttrib3fv == null) missing.Add("glVertexAttrib3fv");
		s_glVertexAttrib3s = (void*)getProcAddress("glVertexAttrib3s");
		if (s_glVertexAttrib3s == null) missing.Add("glVertexAttrib3s");
		s_glVertexAttrib3sv = (void*)getProcAddress("glVertexAttrib3sv");
		if (s_glVertexAttrib3sv == null) missing.Add("glVertexAttrib3sv");
		s_glVertexAttrib4Nbv = (void*)getProcAddress("glVertexAttrib4Nbv");
		if (s_glVertexAttrib4Nbv == null) missing.Add("glVertexAttrib4Nbv");
		s_glVertexAttrib4Niv = (void*)getProcAddress("glVertexAttrib4Niv");
		if (s_glVertexAttrib4Niv == null) missing.Add("glVertexAttrib4Niv");
		s_glVertexAttrib4Nsv = (void*)getProcAddress("glVertexAttrib4Nsv");
		if (s_glVertexAttrib4Nsv == null) missing.Add("glVertexAttrib4Nsv");
		s_glVertexAttrib4Nub = (void*)getProcAddress("glVertexAttrib4Nub");
		if (s_glVertexAttrib4Nub == null) missing.Add("glVertexAttrib4Nub");
		s_glVertexAttrib4Nubv = (void*)getProcAddress("glVertexAttrib4Nubv");
		if (s_glVertexAttrib4Nubv == null) missing.Add("glVertexAttrib4Nubv");
		s_glVertexAttrib4Nuiv = (void*)getProcAddress("glVertexAttrib4Nuiv");
		if (s_glVertexAttrib4Nuiv == null) missing.Add("glVertexAttrib4Nuiv");
		s_glVertexAttrib4Nusv = (void*)getProcAddress("glVertexAttrib4Nusv");
		if (s_glVertexAttrib4Nusv == null) missing.Add("glVertexAttrib4Nusv");
		s_glVertexAttrib4bv = (void*)getProcAddress("glVertexAttrib4bv");
		if (s_glVertexAttrib4bv == null) missing.Add("glVertexAttrib4bv");
		s_glVertexAttrib4d = (void*)getProcAddress("glVertexAttrib4d");
		if (s_glVertexAttrib4d == null) missing.Add("glVertexAttrib4d");
		s_glVertexAttrib4dv = (void*)getProcAddress("glVertexAttrib4dv");
		if (s_glVertexAttrib4dv == null) missing.Add("glVertexAttrib4dv");
		s_glVertexAttrib4f = (void*)getProcAddress("glVertexAttrib4f");
		if (s_glVertexAttrib4f == null) missing.Add("glVertexAttrib4f");
		s_glVertexAttrib4fv = (void*)getProcAddress("glVertexAttrib4fv");
		if (s_glVertexAttrib4fv == null) missing.Add("glVertexAttrib4fv");
		s_glVertexAttrib4iv = (void*)getProcAddress("glVertexAttrib4iv");
		if (s_glVertexAttrib4iv == null) missing.Add("glVertexAttrib4iv");
		s_glVertexAttrib4s = (void*)getProcAddress("glVertexAttrib4s");
		if (s_glVertexAttrib4s == null) missing.Add("glVertexAttrib4s");
		s_glVertexAttrib4sv = (void*)getProcAddress("glVertexAttrib4sv");
		if (s_glVertexAttrib4sv == null) missing.Add("glVertexAttrib4sv");
		s_glVertexAttrib4ubv = (void*)getProcAddress("glVertexAttrib4ubv");
		if (s_glVertexAttrib4ubv == null) missing.Add("glVertexAttrib4ubv");
		s_glVertexAttrib4uiv = (void*)getProcAddress("glVertexAttrib4uiv");
		if (s_glVertexAttrib4uiv == null) missing.Add("glVertexAttrib4uiv");
		s_glVertexAttrib4usv = (void*)getProcAddress("glVertexAttrib4usv");
		if (s_glVertexAttrib4usv == null) missing.Add("glVertexAttrib4usv");
		s_glVertexAttribBinding = (void*)getProcAddress("glVertexAttribBinding");
		if (s_glVertexAttribBinding == null) missing.Add("glVertexAttribBinding");
		s_glVertexAttribDivisor = (void*)getProcAddress("glVertexAttribDivisor");
		if (s_glVertexAttribDivisor == null) missing.Add("glVertexAttribDivisor");
		s_glVertexAttribFormat = (void*)getProcAddress("glVertexAttribFormat");
		if (s_glVertexAttribFormat == null) missing.Add("glVertexAttribFormat");
		s_glVertexAttribI1i = (void*)getProcAddress("glVertexAttribI1i");
		if (s_glVertexAttribI1i == null) missing.Add("glVertexAttribI1i");
		s_glVertexAttribI1iv = (void*)getProcAddress("glVertexAttribI1iv");
		if (s_glVertexAttribI1iv == null) missing.Add("glVertexAttribI1iv");
		s_glVertexAttribI1ui = (void*)getProcAddress("glVertexAttribI1ui");
		if (s_glVertexAttribI1ui == null) missing.Add("glVertexAttribI1ui");
		s_glVertexAttribI1uiv = (void*)getProcAddress("glVertexAttribI1uiv");
		if (s_glVertexAttribI1uiv == null) missing.Add("glVertexAttribI1uiv");
		s_glVertexAttribI2i = (void*)getProcAddress("glVertexAttribI2i");
		if (s_glVertexAttribI2i == null) missing.Add("glVertexAttribI2i");
		s_glVertexAttribI2iv = (void*)getProcAddress("glVertexAttribI2iv");
		if (s_glVertexAttribI2iv == null) missing.Add("glVertexAttribI2iv");
		s_glVertexAttribI2ui = (void*)getProcAddress("glVertexAttribI2ui");
		if (s_glVertexAttribI2ui == null) missing.Add("glVertexAttribI2ui");
		s_glVertexAttribI2uiv = (void*)getProcAddress("glVertexAttribI2uiv");
		if (s_glVertexAttribI2uiv == null) missing.Add("glVertexAttribI2uiv");
		s_glVertexAttribI3i = (void*)getProcAddress("glVertexAttribI3i");
		if (s_glVertexAttribI3i == null) missing.Add("glVertexAttribI3i");
		s_glVertexAttribI3iv = (void*)getProcAddress("glVertexAttribI3iv");
		if (s_glVertexAttribI3iv == null) missing.Add("glVertexAttribI3iv");
		s_glVertexAttribI3ui = (void*)getProcAddress("glVertexAttribI3ui");
		if (s_glVertexAttribI3ui == null) missing.Add("glVertexAttribI3ui");
		s_glVertexAttribI3uiv = (void*)getProcAddress("glVertexAttribI3uiv");
		if (s_glVertexAttribI3uiv == null) missing.Add("glVertexAttribI3uiv");
		s_glVertexAttribI4bv = (void*)getProcAddress("glVertexAttribI4bv");
		if (s_glVertexAttribI4bv == null) missing.Add("glVertexAttribI4bv");
		s_glVertexAttribI4i = (void*)getProcAddress("glVertexAttribI4i");
		if (s_glVertexAttribI4i == null) missing.Add("glVertexAttribI4i");
		s_glVertexAttribI4iv = (void*)getProcAddress("glVertexAttribI4iv");
		if (s_glVertexAttribI4iv == null) missing.Add("glVertexAttribI4iv");
		s_glVertexAttribI4sv = (void*)getProcAddress("glVertexAttribI4sv");
		if (s_glVertexAttribI4sv == null) missing.Add("glVertexAttribI4sv");
		s_glVertexAttribI4ubv = (void*)getProcAddress("glVertexAttribI4ubv");
		if (s_glVertexAttribI4ubv == null) missing.Add("glVertexAttribI4ubv");
		s_glVertexAttribI4ui = (void*)getProcAddress("glVertexAttribI4ui");
		if (s_glVertexAttribI4ui == null) missing.Add("glVertexAttribI4ui");
		s_glVertexAttribI4uiv = (void*)getProcAddress("glVertexAttribI4uiv");
		if (s_glVertexAttribI4uiv == null) missing.Add("glVertexAttribI4uiv");
		s_glVertexAttribI4usv = (void*)getProcAddress("glVertexAttribI4usv");
		if (s_glVertexAttribI4usv == null) missing.Add("glVertexAttribI4usv");
		s_glVertexAttribIFormat = (void*)getProcAddress("glVertexAttribIFormat");
		if (s_glVertexAttribIFormat == null) missing.Add("glVertexAttribIFormat");
		s_glVertexAttribIPointer = (void*)getProcAddress("glVertexAttribIPointer");
		if (s_glVertexAttribIPointer == null) missing.Add("glVertexAttribIPointer");
		s_glVertexAttribL1d = (void*)getProcAddress("glVertexAttribL1d");
		if (s_glVertexAttribL1d == null) missing.Add("glVertexAttribL1d");
		s_glVertexAttribL1dv = (void*)getProcAddress("glVertexAttribL1dv");
		if (s_glVertexAttribL1dv == null) missing.Add("glVertexAttribL1dv");
		s_glVertexAttribL2d = (void*)getProcAddress("glVertexAttribL2d");
		if (s_glVertexAttribL2d == null) missing.Add("glVertexAttribL2d");
		s_glVertexAttribL2dv = (void*)getProcAddress("glVertexAttribL2dv");
		if (s_glVertexAttribL2dv == null) missing.Add("glVertexAttribL2dv");
		s_glVertexAttribL3d = (void*)getProcAddress("glVertexAttribL3d");
		if (s_glVertexAttribL3d == null) missing.Add("glVertexAttribL3d");
		s_glVertexAttribL3dv = (void*)getProcAddress("glVertexAttribL3dv");
		if (s_glVertexAttribL3dv == null) missing.Add("glVertexAttribL3dv");
		s_glVertexAttribL4d = (void*)getProcAddress("glVertexAttribL4d");
		if (s_glVertexAttribL4d == null) missing.Add("glVertexAttribL4d");
		s_glVertexAttribL4dv = (void*)getProcAddress("glVertexAttribL4dv");
		if (s_glVertexAttribL4dv == null) missing.Add("glVertexAttribL4dv");
		s_glVertexAttribLFormat = (void*)getProcAddress("glVertexAttribLFormat");
		if (s_glVertexAttribLFormat == null) missing.Add("glVertexAttribLFormat");
		s_glVertexAttribLPointer = (void*)getProcAddress("glVertexAttribLPointer");
		if (s_glVertexAttribLPointer == null) missing.Add("glVertexAttribLPointer");
		s_glVertexAttribP1ui = (void*)getProcAddress("glVertexAttribP1ui");
		if (s_glVertexAttribP1ui == null) missing.Add("glVertexAttribP1ui");
		s_glVertexAttribP1uiv = (void*)getProcAddress("glVertexAttribP1uiv");
		if (s_glVertexAttribP1uiv == null) missing.Add("glVertexAttribP1uiv");
		s_glVertexAttribP2ui = (void*)getProcAddress("glVertexAttribP2ui");
		if (s_glVertexAttribP2ui == null) missing.Add("glVertexAttribP2ui");
		s_glVertexAttribP2uiv = (void*)getProcAddress("glVertexAttribP2uiv");
		if (s_glVertexAttribP2uiv == null) missing.Add("glVertexAttribP2uiv");
		s_glVertexAttribP3ui = (void*)getProcAddress("glVertexAttribP3ui");
		if (s_glVertexAttribP3ui == null) missing.Add("glVertexAttribP3ui");
		s_glVertexAttribP3uiv = (void*)getProcAddress("glVertexAttribP3uiv");
		if (s_glVertexAttribP3uiv == null) missing.Add("glVertexAttribP3uiv");
		s_glVertexAttribP4ui = (void*)getProcAddress("glVertexAttribP4ui");
		if (s_glVertexAttribP4ui == null) missing.Add("glVertexAttribP4ui");
		s_glVertexAttribP4uiv = (void*)getProcAddress("glVertexAttribP4uiv");
		if (s_glVertexAttribP4uiv == null) missing.Add("glVertexAttribP4uiv");
		s_glVertexAttribPointer = (void*)getProcAddress("glVertexAttribPointer");
		if (s_glVertexAttribPointer == null) missing.Add("glVertexAttribPointer");
		s_glVertexBindingDivisor = (void*)getProcAddress("glVertexBindingDivisor");
		if (s_glVertexBindingDivisor == null) missing.Add("glVertexBindingDivisor");
		s_glVertexP2ui = (void*)getProcAddress("glVertexP2ui");
		if (s_glVertexP2ui == null) missing.Add("glVertexP2ui");
		s_glVertexP2uiv = (void*)getProcAddress("glVertexP2uiv");
		if (s_glVertexP2uiv == null) missing.Add("glVertexP2uiv");
		s_glVertexP3ui = (void*)getProcAddress("glVertexP3ui");
		if (s_glVertexP3ui == null) missing.Add("glVertexP3ui");
		s_glVertexP3uiv = (void*)getProcAddress("glVertexP3uiv");
		if (s_glVertexP3uiv == null) missing.Add("glVertexP3uiv");
		s_glVertexP4ui = (void*)getProcAddress("glVertexP4ui");
		if (s_glVertexP4ui == null) missing.Add("glVertexP4ui");
		s_glVertexP4uiv = (void*)getProcAddress("glVertexP4uiv");
		if (s_glVertexP4uiv == null) missing.Add("glVertexP4uiv");
		s_glViewport = (void*)getProcAddress("glViewport");
		if (s_glViewport == null) missing.Add("glViewport");
		s_glViewportArrayv = (void*)getProcAddress("glViewportArrayv");
		if (s_glViewportArrayv == null) missing.Add("glViewportArrayv");
		s_glViewportIndexedf = (void*)getProcAddress("glViewportIndexedf");
		if (s_glViewportIndexedf == null) missing.Add("glViewportIndexedf");
		s_glViewportIndexedfv = (void*)getProcAddress("glViewportIndexedfv");
		if (s_glViewportIndexedfv == null) missing.Add("glViewportIndexedfv");
		s_glWaitSync = (void*)getProcAddress("glWaitSync");
		if (s_glWaitSync == null) missing.Add("glWaitSync");
		if (missing.Count > 0)
			throw new System.InvalidOperationException(
				"OpenGL: failed to load " + missing.Count + " function(s): " + string.Join(", ", missing));
	}
}
