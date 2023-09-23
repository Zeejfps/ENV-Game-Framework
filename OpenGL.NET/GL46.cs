using System.Runtime.InteropServices;
using System.Security;

[System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "IdentifierTypo")]
[SuppressUnmanagedCodeSecurity]
public static unsafe class GL46
{
	public delegate IntPtr GetProcAddressDelegate(string funcName);

	public const int GL_DEPTH_BUFFER_BIT = 0x00000100;
	public const int GL_STENCIL_BUFFER_BIT = 0x00000400;
	public const int GL_COLOR_BUFFER_BIT = 0x00004000;
	public const int GL_DYNAMIC_STORAGE_BIT = 0x0100;
	public const int GL_CLIENT_STORAGE_BIT = 0x0200;
	public const int GL_CONTEXT_FLAG_FORWARD_COMPATIBLE_BIT = 0x00000001;
	public const int GL_CONTEXT_FLAG_DEBUG_BIT = 0x00000002;
	public const int GL_CONTEXT_FLAG_ROBUST_ACCESS_BIT = 0x00000004;
	public const int GL_CONTEXT_FLAG_NO_ERROR_BIT = 0x00000008;
	public const int GL_CONTEXT_CORE_PROFILE_BIT = 0x00000001;
	public const int GL_CONTEXT_COMPATIBILITY_PROFILE_BIT = 0x00000002;
	public const int GL_MAP_READ_BIT = 0x0001;
	public const int GL_MAP_WRITE_BIT = 0x0002;
	public const int GL_MAP_INVALIDATE_RANGE_BIT = 0x0004;
	public const int GL_MAP_INVALIDATE_BUFFER_BIT = 0x0008;
	public const int GL_MAP_FLUSH_EXPLICIT_BIT = 0x0010;
	public const int GL_MAP_UNSYNCHRONIZED_BIT = 0x0020;
	public const int GL_MAP_PERSISTENT_BIT = 0x0040;
	public const int GL_MAP_COHERENT_BIT = 0x0080;
	public const int GL_VERTEX_ATTRIB_ARRAY_BARRIER_BIT = 0x00000001;
	public const int GL_ELEMENT_ARRAY_BARRIER_BIT = 0x00000002;
	public const int GL_UNIFORM_BARRIER_BIT = 0x00000004;
	public const int GL_TEXTURE_FETCH_BARRIER_BIT = 0x00000008;
	public const int GL_SHADER_IMAGE_ACCESS_BARRIER_BIT = 0x00000020;
	public const int GL_COMMAND_BARRIER_BIT = 0x00000040;
	public const int GL_PIXEL_BUFFER_BARRIER_BIT = 0x00000080;
	public const int GL_TEXTURE_UPDATE_BARRIER_BIT = 0x00000100;
	public const int GL_BUFFER_UPDATE_BARRIER_BIT = 0x00000200;
	public const int GL_FRAMEBUFFER_BARRIER_BIT = 0x00000400;
	public const int GL_TRANSFORM_FEEDBACK_BARRIER_BIT = 0x00000800;
	public const int GL_ATOMIC_COUNTER_BARRIER_BIT = 0x00001000;
	public const int GL_SHADER_STORAGE_BARRIER_BIT = 0x00002000;
	public const int GL_CLIENT_MAPPED_BUFFER_BARRIER_BIT = 0x00004000;
	public const int GL_QUERY_BUFFER_BARRIER_BIT = 0x00008000;
	public const uint GL_ALL_BARRIER_BITS = 0xFFFFFFFF;
	public const int GL_SYNC_FLUSH_COMMANDS_BIT = 0x00000001;
	public const int GL_VERTEX_SHADER_BIT = 0x00000001;
	public const int GL_FRAGMENT_SHADER_BIT = 0x00000002;
	public const int GL_GEOMETRY_SHADER_BIT = 0x00000004;
	public const int GL_TESS_CONTROL_SHADER_BIT = 0x00000008;
	public const int GL_TESS_EVALUATION_SHADER_BIT = 0x00000010;
	public const int GL_COMPUTE_SHADER_BIT = 0x00000020;
	public const uint GL_ALL_SHADER_BITS = 0xFFFFFFFF;
	public const int GL_FALSE = 0;
	public const int GL_NO_ERROR = 0;
	public const int GL_ZERO = 0;
	public const int GL_NONE = 0;
	public const int GL_TRUE = 1;
	public const int GL_ONE = 1;
	public const uint GL_INVALID_INDEX = 0xFFFFFFFF;
	public const ulong GL_TIMEOUT_IGNORED = 0xFFFFFFFFFFFFFFFF;
	public const int GL_POINTS = 0x0000;
	public const int GL_LINES = 0x0001;
	public const int GL_LINE_LOOP = 0x0002;
	public const int GL_LINE_STRIP = 0x0003;
	public const int GL_TRIANGLES = 0x0004;
	public const int GL_TRIANGLE_STRIP = 0x0005;
	public const int GL_TRIANGLE_FAN = 0x0006;
	public const int GL_LINES_ADJACENCY = 0x000A;
	public const int GL_LINE_STRIP_ADJACENCY = 0x000B;
	public const int GL_TRIANGLES_ADJACENCY = 0x000C;
	public const int GL_TRIANGLE_STRIP_ADJACENCY = 0x000D;
	public const int GL_PATCHES = 0x000E;
	public const int GL_NEVER = 0x0200;
	public const int GL_LESS = 0x0201;
	public const int GL_EQUAL = 0x0202;
	public const int GL_LEQUAL = 0x0203;
	public const int GL_GREATER = 0x0204;
	public const int GL_NOTEQUAL = 0x0205;
	public const int GL_GEQUAL = 0x0206;
	public const int GL_ALWAYS = 0x0207;
	public const int GL_SRC_COLOR = 0x0300;
	public const int GL_ONE_MINUS_SRC_COLOR = 0x0301;
	public const int GL_SRC_ALPHA = 0x0302;
	public const int GL_ONE_MINUS_SRC_ALPHA = 0x0303;
	public const int GL_DST_ALPHA = 0x0304;
	public const int GL_ONE_MINUS_DST_ALPHA = 0x0305;
	public const int GL_DST_COLOR = 0x0306;
	public const int GL_ONE_MINUS_DST_COLOR = 0x0307;
	public const int GL_SRC_ALPHA_SATURATE = 0x0308;
	public const int GL_FRONT_LEFT = 0x0400;
	public const int GL_FRONT_RIGHT = 0x0401;
	public const int GL_BACK_LEFT = 0x0402;
	public const int GL_BACK_RIGHT = 0x0403;
	public const int GL_FRONT = 0x0404;
	public const int GL_BACK = 0x0405;
	public const int GL_LEFT = 0x0406;
	public const int GL_RIGHT = 0x0407;
	public const int GL_FRONT_AND_BACK = 0x0408;
	public const int GL_INVALID_ENUM = 0x0500;
	public const int GL_INVALID_VALUE = 0x0501;
	public const int GL_INVALID_OPERATION = 0x0502;
	public const int GL_OUT_OF_MEMORY = 0x0505;
	public const int GL_INVALID_FRAMEBUFFER_OPERATION = 0x0506;
	public const int GL_CONTEXT_LOST = 0x0507;
	public const int GL_CW = 0x0900;
	public const int GL_CCW = 0x0901;
	public const int GL_POINT_SIZE = 0x0B11;
	public const int GL_POINT_SIZE_RANGE = 0x0B12;
	public const int GL_SMOOTH_POINT_SIZE_RANGE = 0x0B12;
	public const int GL_POINT_SIZE_GRANULARITY = 0x0B13;
	public const int GL_SMOOTH_POINT_SIZE_GRANULARITY = 0x0B13;
	public const int GL_LINE_SMOOTH = 0x0B20;
	public const int GL_LINE_WIDTH = 0x0B21;
	public const int GL_LINE_WIDTH_RANGE = 0x0B22;
	public const int GL_SMOOTH_LINE_WIDTH_RANGE = 0x0B22;
	public const int GL_LINE_WIDTH_GRANULARITY = 0x0B23;
	public const int GL_SMOOTH_LINE_WIDTH_GRANULARITY = 0x0B23;
	public const int GL_POLYGON_MODE = 0x0B40;
	public const int GL_POLYGON_SMOOTH = 0x0B41;
	public const int GL_CULL_FACE = 0x0B44;
	public const int GL_CULL_FACE_MODE = 0x0B45;
	public const int GL_FRONT_FACE = 0x0B46;
	public const int GL_DEPTH_RANGE = 0x0B70;
	public const int GL_DEPTH_TEST = 0x0B71;
	public const int GL_DEPTH_WRITEMASK = 0x0B72;
	public const int GL_DEPTH_CLEAR_VALUE = 0x0B73;
	public const int GL_DEPTH_FUNC = 0x0B74;
	public const int GL_STENCIL_TEST = 0x0B90;
	public const int GL_STENCIL_CLEAR_VALUE = 0x0B91;
	public const int GL_STENCIL_FUNC = 0x0B92;
	public const int GL_STENCIL_VALUE_MASK = 0x0B93;
	public const int GL_STENCIL_FAIL = 0x0B94;
	public const int GL_STENCIL_PASS_DEPTH_FAIL = 0x0B95;
	public const int GL_STENCIL_PASS_DEPTH_PASS = 0x0B96;
	public const int GL_STENCIL_REF = 0x0B97;
	public const int GL_STENCIL_WRITEMASK = 0x0B98;
	public const int GL_VIEWPORT = 0x0BA2;
	public const int GL_DITHER = 0x0BD0;
	public const int GL_BLEND_DST = 0x0BE0;
	public const int GL_BLEND_SRC = 0x0BE1;
	public const int GL_BLEND = 0x0BE2;
	public const int GL_LOGIC_OP_MODE = 0x0BF0;
	public const int GL_COLOR_LOGIC_OP = 0x0BF2;
	public const int GL_DRAW_BUFFER = 0x0C01;
	public const int GL_READ_BUFFER = 0x0C02;
	public const int GL_SCISSOR_BOX = 0x0C10;
	public const int GL_SCISSOR_TEST = 0x0C11;
	public const int GL_COLOR_CLEAR_VALUE = 0x0C22;
	public const int GL_COLOR_WRITEMASK = 0x0C23;
	public const int GL_DOUBLEBUFFER = 0x0C32;
	public const int GL_STEREO = 0x0C33;
	public const int GL_LINE_SMOOTH_HINT = 0x0C52;
	public const int GL_POLYGON_SMOOTH_HINT = 0x0C53;
	public const int GL_UNPACK_SWAP_BYTES = 0x0CF0;
	public const int GL_UNPACK_LSB_FIRST = 0x0CF1;
	public const int GL_UNPACK_ROW_LENGTH = 0x0CF2;
	public const int GL_UNPACK_SKIP_ROWS = 0x0CF3;
	public const int GL_UNPACK_SKIP_PIXELS = 0x0CF4;
	public const int GL_UNPACK_ALIGNMENT = 0x0CF5;
	public const int GL_PACK_SWAP_BYTES = 0x0D00;
	public const int GL_PACK_LSB_FIRST = 0x0D01;
	public const int GL_PACK_ROW_LENGTH = 0x0D02;
	public const int GL_PACK_SKIP_ROWS = 0x0D03;
	public const int GL_PACK_SKIP_PIXELS = 0x0D04;
	public const int GL_PACK_ALIGNMENT = 0x0D05;
	public const int GL_MAX_CLIP_DISTANCES = 0x0D32;
	public const int GL_MAX_TEXTURE_SIZE = 0x0D33;
	public const int GL_MAX_VIEWPORT_DIMS = 0x0D3A;
	public const int GL_SUBPIXEL_BITS = 0x0D50;
	public const int GL_TEXTURE_1D = 0x0DE0;
	public const int GL_TEXTURE_2D = 0x0DE1;
	public const int GL_TEXTURE_WIDTH = 0x1000;
	public const int GL_TEXTURE_HEIGHT = 0x1001;
	public const int GL_TEXTURE_INTERNAL_FORMAT = 0x1003;
	public const int GL_TEXTURE_BORDER_COLOR = 0x1004;
	public const int GL_TEXTURE_TARGET = 0x1006;
	public const int GL_DONT_CARE = 0x1100;
	public const int GL_FASTEST = 0x1101;
	public const int GL_NICEST = 0x1102;
	public const int GL_BYTE = 0x1400;
	public const int GL_UNSIGNED_BYTE = 0x1401;
	public const int GL_SHORT = 0x1402;
	public const int GL_UNSIGNED_SHORT = 0x1403;
	public const int GL_INT = 0x1404;
	public const int GL_UNSIGNED_INT = 0x1405;
	public const int GL_FLOAT = 0x1406;
	public const int GL_DOUBLE = 0x140A;
	public const int GL_HALF_FLOAT = 0x140B;
	public const int GL_FIXED = 0x140C;
	public const int GL_CLEAR = 0x1500;
	public const int GL_AND = 0x1501;
	public const int GL_AND_REVERSE = 0x1502;
	public const int GL_COPY = 0x1503;
	public const int GL_AND_INVERTED = 0x1504;
	public const int GL_NOOP = 0x1505;
	public const int GL_XOR = 0x1506;
	public const int GL_OR = 0x1507;
	public const int GL_NOR = 0x1508;
	public const int GL_EQUIV = 0x1509;
	public const int GL_INVERT = 0x150A;
	public const int GL_OR_REVERSE = 0x150B;
	public const int GL_COPY_INVERTED = 0x150C;
	public const int GL_OR_INVERTED = 0x150D;
	public const int GL_NAND = 0x150E;
	public const int GL_SET = 0x150F;
	public const int GL_TEXTURE = 0x1702;
	public const int GL_COLOR = 0x1800;
	public const int GL_DEPTH = 0x1801;
	public const int GL_STENCIL = 0x1802;
	public const int GL_STENCIL_INDEX = 0x1901;
	public const int GL_DEPTH_COMPONENT = 0x1902;
	public const int GL_RED = 0x1903;
	public const int GL_GREEN = 0x1904;
	public const int GL_BLUE = 0x1905;
	public const int GL_ALPHA = 0x1906;
	public const int GL_RGB = 0x1907;
	public const int GL_RGBA = 0x1908;
	public const int GL_POINT = 0x1B00;
	public const int GL_LINE = 0x1B01;
	public const int GL_FILL = 0x1B02;
	public const int GL_KEEP = 0x1E00;
	public const int GL_REPLACE = 0x1E01;
	public const int GL_INCR = 0x1E02;
	public const int GL_DECR = 0x1E03;
	public const int GL_VENDOR = 0x1F00;
	public const int GL_RENDERER = 0x1F01;
	public const int GL_VERSION = 0x1F02;
	public const int GL_EXTENSIONS = 0x1F03;
	public const int GL_NEAREST = 0x2600;
	public const int GL_LINEAR = 0x2601;
	public const int GL_NEAREST_MIPMAP_NEAREST = 0x2700;
	public const int GL_LINEAR_MIPMAP_NEAREST = 0x2701;
	public const int GL_NEAREST_MIPMAP_LINEAR = 0x2702;
	public const int GL_LINEAR_MIPMAP_LINEAR = 0x2703;
	public const int GL_TEXTURE_MAG_FILTER = 0x2800;
	public const int GL_TEXTURE_MIN_FILTER = 0x2801;
	public const int GL_TEXTURE_WRAP_S = 0x2802;
	public const int GL_TEXTURE_WRAP_T = 0x2803;
	public const int GL_REPEAT = 0x2901;
	public const int GL_POLYGON_OFFSET_UNITS = 0x2A00;
	public const int GL_POLYGON_OFFSET_POINT = 0x2A01;
	public const int GL_POLYGON_OFFSET_LINE = 0x2A02;
	public const int GL_R3_G3_B2 = 0x2A10;
	public const int GL_CLIP_DISTANCE0 = 0x3000;
	public const int GL_CLIP_DISTANCE1 = 0x3001;
	public const int GL_CLIP_DISTANCE2 = 0x3002;
	public const int GL_CLIP_DISTANCE3 = 0x3003;
	public const int GL_CLIP_DISTANCE4 = 0x3004;
	public const int GL_CLIP_DISTANCE5 = 0x3005;
	public const int GL_CLIP_DISTANCE6 = 0x3006;
	public const int GL_CLIP_DISTANCE7 = 0x3007;
	public const int GL_CONSTANT_COLOR = 0x8001;
	public const int GL_ONE_MINUS_CONSTANT_COLOR = 0x8002;
	public const int GL_CONSTANT_ALPHA = 0x8003;
	public const int GL_ONE_MINUS_CONSTANT_ALPHA = 0x8004;
	public const int GL_BLEND_COLOR = 0x8005;
	public const int GL_FUNC_ADD = 0x8006;
	public const int GL_MIN = 0x8007;
	public const int GL_MAX = 0x8008;
	public const int GL_BLEND_EQUATION = 0x8009;
	public const int GL_BLEND_EQUATION_RGB = 0x8009;
	public const int GL_FUNC_SUBTRACT = 0x800A;
	public const int GL_FUNC_REVERSE_SUBTRACT = 0x800B;
	public const int GL_CONVOLUTION_1D = 0x8010;
	public const int GL_CONVOLUTION_2D = 0x8011;
	public const int GL_SEPARABLE_2D = 0x8012;
	public const int GL_HISTOGRAM = 0x8024;
	public const int GL_PROXY_HISTOGRAM = 0x8025;
	public const int GL_MINMAX = 0x802E;
	public const int GL_UNSIGNED_BYTE_3_3_2 = 0x8032;
	public const int GL_UNSIGNED_SHORT_4_4_4_4 = 0x8033;
	public const int GL_UNSIGNED_SHORT_5_5_5_1 = 0x8034;
	public const int GL_UNSIGNED_INT_8_8_8_8 = 0x8035;
	public const int GL_UNSIGNED_INT_10_10_10_2 = 0x8036;
	public const int GL_POLYGON_OFFSET_FILL = 0x8037;
	public const int GL_POLYGON_OFFSET_FACTOR = 0x8038;
	public const int GL_RGB4 = 0x804F;
	public const int GL_RGB5 = 0x8050;
	public const int GL_RGB8 = 0x8051;
	public const int GL_RGB10 = 0x8052;
	public const int GL_RGB12 = 0x8053;
	public const int GL_RGB16 = 0x8054;
	public const int GL_RGBA2 = 0x8055;
	public const int GL_RGBA4 = 0x8056;
	public const int GL_RGB5_A1 = 0x8057;
	public const int GL_RGBA8 = 0x8058;
	public const int GL_RGB10_A2 = 0x8059;
	public const int GL_RGBA12 = 0x805A;
	public const int GL_RGBA16 = 0x805B;
	public const int GL_TEXTURE_RED_SIZE = 0x805C;
	public const int GL_TEXTURE_GREEN_SIZE = 0x805D;
	public const int GL_TEXTURE_BLUE_SIZE = 0x805E;
	public const int GL_TEXTURE_ALPHA_SIZE = 0x805F;
	public const int GL_PROXY_TEXTURE_1D = 0x8063;
	public const int GL_PROXY_TEXTURE_2D = 0x8064;
	public const int GL_TEXTURE_BINDING_1D = 0x8068;
	public const int GL_TEXTURE_BINDING_2D = 0x8069;
	public const int GL_TEXTURE_BINDING_3D = 0x806A;
	public const int GL_PACK_SKIP_IMAGES = 0x806B;
	public const int GL_PACK_IMAGE_HEIGHT = 0x806C;
	public const int GL_UNPACK_SKIP_IMAGES = 0x806D;
	public const int GL_UNPACK_IMAGE_HEIGHT = 0x806E;
	public const int GL_TEXTURE_3D = 0x806F;
	public const int GL_PROXY_TEXTURE_3D = 0x8070;
	public const int GL_TEXTURE_DEPTH = 0x8071;
	public const int GL_TEXTURE_WRAP_R = 0x8072;
	public const int GL_MAX_3D_TEXTURE_SIZE = 0x8073;
	public const int GL_MULTISAMPLE = 0x809D;
	public const int GL_SAMPLE_ALPHA_TO_COVERAGE = 0x809E;
	public const int GL_SAMPLE_ALPHA_TO_ONE = 0x809F;
	public const int GL_SAMPLE_COVERAGE = 0x80A0;
	public const int GL_SAMPLE_BUFFERS = 0x80A8;
	public const int GL_SAMPLES = 0x80A9;
	public const int GL_SAMPLE_COVERAGE_VALUE = 0x80AA;
	public const int GL_SAMPLE_COVERAGE_INVERT = 0x80AB;
	public const int GL_BLEND_DST_RGB = 0x80C8;
	public const int GL_BLEND_SRC_RGB = 0x80C9;
	public const int GL_BLEND_DST_ALPHA = 0x80CA;
	public const int GL_BLEND_SRC_ALPHA = 0x80CB;
	public const int GL_COLOR_TABLE = 0x80D0;
	public const int GL_POST_CONVOLUTION_COLOR_TABLE = 0x80D1;
	public const int GL_POST_COLOR_MATRIX_COLOR_TABLE = 0x80D2;
	public const int GL_PROXY_COLOR_TABLE = 0x80D3;
	public const int GL_PROXY_POST_CONVOLUTION_COLOR_TABLE = 0x80D4;
	public const int GL_PROXY_POST_COLOR_MATRIX_COLOR_TABLE = 0x80D5;
	public const int GL_BGR = 0x80E0;
	public const int GL_BGRA = 0x80E1;
	public const int GL_MAX_ELEMENTS_VERTICES = 0x80E8;
	public const int GL_MAX_ELEMENTS_INDICES = 0x80E9;
	public const int GL_PARAMETER_BUFFER = 0x80EE;
	public const int GL_PARAMETER_BUFFER_BINDING = 0x80EF;
	public const int GL_POINT_FADE_THRESHOLD_SIZE = 0x8128;
	public const int GL_CLAMP_TO_BORDER = 0x812D;
	public const int GL_CLAMP_TO_EDGE = 0x812F;
	public const int GL_TEXTURE_MIN_LOD = 0x813A;
	public const int GL_TEXTURE_MAX_LOD = 0x813B;
	public const int GL_TEXTURE_BASE_LEVEL = 0x813C;
	public const int GL_TEXTURE_MAX_LEVEL = 0x813D;
	public const int GL_DEPTH_COMPONENT16 = 0x81A5;
	public const int GL_DEPTH_COMPONENT24 = 0x81A6;
	public const int GL_DEPTH_COMPONENT32 = 0x81A7;
	public const int GL_FRAMEBUFFER_ATTACHMENT_COLOR_ENCODING = 0x8210;
	public const int GL_FRAMEBUFFER_ATTACHMENT_COMPONENT_TYPE = 0x8211;
	public const int GL_FRAMEBUFFER_ATTACHMENT_RED_SIZE = 0x8212;
	public const int GL_FRAMEBUFFER_ATTACHMENT_GREEN_SIZE = 0x8213;
	public const int GL_FRAMEBUFFER_ATTACHMENT_BLUE_SIZE = 0x8214;
	public const int GL_FRAMEBUFFER_ATTACHMENT_ALPHA_SIZE = 0x8215;
	public const int GL_FRAMEBUFFER_ATTACHMENT_DEPTH_SIZE = 0x8216;
	public const int GL_FRAMEBUFFER_ATTACHMENT_STENCIL_SIZE = 0x8217;
	public const int GL_FRAMEBUFFER_DEFAULT = 0x8218;
	public const int GL_FRAMEBUFFER_UNDEFINED = 0x8219;
	public const int GL_DEPTH_STENCIL_ATTACHMENT = 0x821A;
	public const int GL_MAJOR_VERSION = 0x821B;
	public const int GL_MINOR_VERSION = 0x821C;
	public const int GL_NUM_EXTENSIONS = 0x821D;
	public const int GL_CONTEXT_FLAGS = 0x821E;
	public const int GL_BUFFER_IMMUTABLE_STORAGE = 0x821F;
	public const int GL_BUFFER_STORAGE_FLAGS = 0x8220;
	public const int GL_PRIMITIVE_RESTART_FOR_PATCHES_SUPPORTED = 0x8221;
	public const int GL_COMPRESSED_RED = 0x8225;
	public const int GL_COMPRESSED_RG = 0x8226;
	public const int GL_RG = 0x8227;
	public const int GL_RG_INTEGER = 0x8228;
	public const int GL_R8 = 0x8229;
	public const int GL_R16 = 0x822A;
	public const int GL_RG8 = 0x822B;
	public const int GL_RG16 = 0x822C;
	public const int GL_R16F = 0x822D;
	public const int GL_R32F = 0x822E;
	public const int GL_RG16F = 0x822F;
	public const int GL_RG32F = 0x8230;
	public const int GL_R8I = 0x8231;
	public const int GL_R8UI = 0x8232;
	public const int GL_R16I = 0x8233;
	public const int GL_R16UI = 0x8234;
	public const int GL_R32I = 0x8235;
	public const int GL_R32UI = 0x8236;
	public const int GL_RG8I = 0x8237;
	public const int GL_RG8UI = 0x8238;
	public const int GL_RG16I = 0x8239;
	public const int GL_RG16UI = 0x823A;
	public const int GL_RG32I = 0x823B;
	public const int GL_RG32UI = 0x823C;
	public const int GL_DEBUG_OUTPUT_SYNCHRONOUS = 0x8242;
	public const int GL_DEBUG_NEXT_LOGGED_MESSAGE_LENGTH = 0x8243;
	public const int GL_DEBUG_CALLBACK_FUNCTION = 0x8244;
	public const int GL_DEBUG_CALLBACK_USER_PARAM = 0x8245;
	public const int GL_DEBUG_SOURCE_API = 0x8246;
	public const int GL_DEBUG_SOURCE_WINDOW_SYSTEM = 0x8247;
	public const int GL_DEBUG_SOURCE_SHADER_COMPILER = 0x8248;
	public const int GL_DEBUG_SOURCE_THIRD_PARTY = 0x8249;
	public const int GL_DEBUG_SOURCE_APPLICATION = 0x824A;
	public const int GL_DEBUG_SOURCE_OTHER = 0x824B;
	public const int GL_DEBUG_TYPE_ERROR = 0x824C;
	public const int GL_DEBUG_TYPE_DEPRECATED_BEHAVIOR = 0x824D;
	public const int GL_DEBUG_TYPE_UNDEFINED_BEHAVIOR = 0x824E;
	public const int GL_DEBUG_TYPE_PORTABILITY = 0x824F;
	public const int GL_DEBUG_TYPE_PERFORMANCE = 0x8250;
	public const int GL_DEBUG_TYPE_OTHER = 0x8251;
	public const int GL_LOSE_CONTEXT_ON_RESET = 0x8252;
	public const int GL_GUILTY_CONTEXT_RESET = 0x8253;
	public const int GL_INNOCENT_CONTEXT_RESET = 0x8254;
	public const int GL_UNKNOWN_CONTEXT_RESET = 0x8255;
	public const int GL_RESET_NOTIFICATION_STRATEGY = 0x8256;
	public const int GL_PROGRAM_BINARY_RETRIEVABLE_HINT = 0x8257;
	public const int GL_PROGRAM_SEPARABLE = 0x8258;
	public const int GL_ACTIVE_PROGRAM = 0x8259;
	public const int GL_PROGRAM_PIPELINE_BINDING = 0x825A;
	public const int GL_MAX_VIEWPORTS = 0x825B;
	public const int GL_VIEWPORT_SUBPIXEL_BITS = 0x825C;
	public const int GL_VIEWPORT_BOUNDS_RANGE = 0x825D;
	public const int GL_LAYER_PROVOKING_VERTEX = 0x825E;
	public const int GL_VIEWPORT_INDEX_PROVOKING_VERTEX = 0x825F;
	public const int GL_UNDEFINED_VERTEX = 0x8260;
	public const int GL_NO_RESET_NOTIFICATION = 0x8261;
	public const int GL_MAX_COMPUTE_SHARED_MEMORY_SIZE = 0x8262;
	public const int GL_MAX_COMPUTE_UNIFORM_COMPONENTS = 0x8263;
	public const int GL_MAX_COMPUTE_ATOMIC_COUNTER_BUFFERS = 0x8264;
	public const int GL_MAX_COMPUTE_ATOMIC_COUNTERS = 0x8265;
	public const int GL_MAX_COMBINED_COMPUTE_UNIFORM_COMPONENTS = 0x8266;
	public const int GL_COMPUTE_WORK_GROUP_SIZE = 0x8267;
	public const int GL_DEBUG_TYPE_MARKER = 0x8268;
	public const int GL_DEBUG_TYPE_PUSH_GROUP = 0x8269;
	public const int GL_DEBUG_TYPE_POP_GROUP = 0x826A;
	public const int GL_DEBUG_SEVERITY_NOTIFICATION = 0x826B;
	public const int GL_MAX_DEBUG_GROUP_STACK_DEPTH = 0x826C;
	public const int GL_DEBUG_GROUP_STACK_DEPTH = 0x826D;
	public const int GL_MAX_UNIFORM_LOCATIONS = 0x826E;
	public const int GL_INTERNALFORMAT_SUPPORTED = 0x826F;
	public const int GL_INTERNALFORMAT_PREFERRED = 0x8270;
	public const int GL_INTERNALFORMAT_RED_SIZE = 0x8271;
	public const int GL_INTERNALFORMAT_GREEN_SIZE = 0x8272;
	public const int GL_INTERNALFORMAT_BLUE_SIZE = 0x8273;
	public const int GL_INTERNALFORMAT_ALPHA_SIZE = 0x8274;
	public const int GL_INTERNALFORMAT_DEPTH_SIZE = 0x8275;
	public const int GL_INTERNALFORMAT_STENCIL_SIZE = 0x8276;
	public const int GL_INTERNALFORMAT_SHARED_SIZE = 0x8277;
	public const int GL_INTERNALFORMAT_RED_TYPE = 0x8278;
	public const int GL_INTERNALFORMAT_GREEN_TYPE = 0x8279;
	public const int GL_INTERNALFORMAT_BLUE_TYPE = 0x827A;
	public const int GL_INTERNALFORMAT_ALPHA_TYPE = 0x827B;
	public const int GL_INTERNALFORMAT_DEPTH_TYPE = 0x827C;
	public const int GL_INTERNALFORMAT_STENCIL_TYPE = 0x827D;
	public const int GL_MAX_WIDTH = 0x827E;
	public const int GL_MAX_HEIGHT = 0x827F;
	public const int GL_MAX_DEPTH = 0x8280;
	public const int GL_MAX_LAYERS = 0x8281;
	public const int GL_MAX_COMBINED_DIMENSIONS = 0x8282;
	public const int GL_COLOR_COMPONENTS = 0x8283;
	public const int GL_DEPTH_COMPONENTS = 0x8284;
	public const int GL_STENCIL_COMPONENTS = 0x8285;
	public const int GL_COLOR_RENDERABLE = 0x8286;
	public const int GL_DEPTH_RENDERABLE = 0x8287;
	public const int GL_STENCIL_RENDERABLE = 0x8288;
	public const int GL_FRAMEBUFFER_RENDERABLE = 0x8289;
	public const int GL_FRAMEBUFFER_RENDERABLE_LAYERED = 0x828A;
	public const int GL_FRAMEBUFFER_BLEND = 0x828B;
	public const int GL_READ_PIXELS = 0x828C;
	public const int GL_READ_PIXELS_FORMAT = 0x828D;
	public const int GL_READ_PIXELS_TYPE = 0x828E;
	public const int GL_TEXTURE_IMAGE_FORMAT = 0x828F;
	public const int GL_TEXTURE_IMAGE_TYPE = 0x8290;
	public const int GL_GET_TEXTURE_IMAGE_FORMAT = 0x8291;
	public const int GL_GET_TEXTURE_IMAGE_TYPE = 0x8292;
	public const int GL_MIPMAP = 0x8293;
	public const int GL_MANUAL_GENERATE_MIPMAP = 0x8294;
	public const int GL_AUTO_GENERATE_MIPMAP = 0x8295;
	public const int GL_COLOR_ENCODING = 0x8296;
	public const int GL_SRGB_READ = 0x8297;
	public const int GL_SRGB_WRITE = 0x8298;
	public const int GL_FILTER = 0x829A;
	public const int GL_VERTEX_TEXTURE = 0x829B;
	public const int GL_TESS_CONTROL_TEXTURE = 0x829C;
	public const int GL_TESS_EVALUATION_TEXTURE = 0x829D;
	public const int GL_GEOMETRY_TEXTURE = 0x829E;
	public const int GL_FRAGMENT_TEXTURE = 0x829F;
	public const int GL_COMPUTE_TEXTURE = 0x82A0;
	public const int GL_TEXTURE_SHADOW = 0x82A1;
	public const int GL_TEXTURE_GATHER = 0x82A2;
	public const int GL_TEXTURE_GATHER_SHADOW = 0x82A3;
	public const int GL_SHADER_IMAGE_LOAD = 0x82A4;
	public const int GL_SHADER_IMAGE_STORE = 0x82A5;
	public const int GL_SHADER_IMAGE_ATOMIC = 0x82A6;
	public const int GL_IMAGE_TEXEL_SIZE = 0x82A7;
	public const int GL_IMAGE_COMPATIBILITY_CLASS = 0x82A8;
	public const int GL_IMAGE_PIXEL_FORMAT = 0x82A9;
	public const int GL_IMAGE_PIXEL_TYPE = 0x82AA;
	public const int GL_SIMULTANEOUS_TEXTURE_AND_DEPTH_TEST = 0x82AC;
	public const int GL_SIMULTANEOUS_TEXTURE_AND_STENCIL_TEST = 0x82AD;
	public const int GL_SIMULTANEOUS_TEXTURE_AND_DEPTH_WRITE = 0x82AE;
	public const int GL_SIMULTANEOUS_TEXTURE_AND_STENCIL_WRITE = 0x82AF;
	public const int GL_TEXTURE_COMPRESSED_BLOCK_WIDTH = 0x82B1;
	public const int GL_TEXTURE_COMPRESSED_BLOCK_HEIGHT = 0x82B2;
	public const int GL_TEXTURE_COMPRESSED_BLOCK_SIZE = 0x82B3;
	public const int GL_CLEAR_BUFFER = 0x82B4;
	public const int GL_TEXTURE_VIEW = 0x82B5;
	public const int GL_VIEW_COMPATIBILITY_CLASS = 0x82B6;
	public const int GL_FULL_SUPPORT = 0x82B7;
	public const int GL_CAVEAT_SUPPORT = 0x82B8;
	public const int GL_IMAGE_CLASS_4_X_32 = 0x82B9;
	public const int GL_IMAGE_CLASS_2_X_32 = 0x82BA;
	public const int GL_IMAGE_CLASS_1_X_32 = 0x82BB;
	public const int GL_IMAGE_CLASS_4_X_16 = 0x82BC;
	public const int GL_IMAGE_CLASS_2_X_16 = 0x82BD;
	public const int GL_IMAGE_CLASS_1_X_16 = 0x82BE;
	public const int GL_IMAGE_CLASS_4_X_8 = 0x82BF;
	public const int GL_IMAGE_CLASS_2_X_8 = 0x82C0;
	public const int GL_IMAGE_CLASS_1_X_8 = 0x82C1;
	public const int GL_IMAGE_CLASS_11_11_10 = 0x82C2;
	public const int GL_IMAGE_CLASS_10_10_10_2 = 0x82C3;
	public const int GL_VIEW_CLASS_128_BITS = 0x82C4;
	public const int GL_VIEW_CLASS_96_BITS = 0x82C5;
	public const int GL_VIEW_CLASS_64_BITS = 0x82C6;
	public const int GL_VIEW_CLASS_48_BITS = 0x82C7;
	public const int GL_VIEW_CLASS_32_BITS = 0x82C8;
	public const int GL_VIEW_CLASS_24_BITS = 0x82C9;
	public const int GL_VIEW_CLASS_16_BITS = 0x82CA;
	public const int GL_VIEW_CLASS_8_BITS = 0x82CB;
	public const int GL_VIEW_CLASS_S3TC_DXT1_RGB = 0x82CC;
	public const int GL_VIEW_CLASS_S3TC_DXT1_RGBA = 0x82CD;
	public const int GL_VIEW_CLASS_S3TC_DXT3_RGBA = 0x82CE;
	public const int GL_VIEW_CLASS_S3TC_DXT5_RGBA = 0x82CF;
	public const int GL_VIEW_CLASS_RGTC1_RED = 0x82D0;
	public const int GL_VIEW_CLASS_RGTC2_RG = 0x82D1;
	public const int GL_VIEW_CLASS_BPTC_UNORM = 0x82D2;
	public const int GL_VIEW_CLASS_BPTC_FLOAT = 0x82D3;
	public const int GL_VERTEX_ATTRIB_BINDING = 0x82D4;
	public const int GL_VERTEX_ATTRIB_RELATIVE_OFFSET = 0x82D5;
	public const int GL_VERTEX_BINDING_DIVISOR = 0x82D6;
	public const int GL_VERTEX_BINDING_OFFSET = 0x82D7;
	public const int GL_VERTEX_BINDING_STRIDE = 0x82D8;
	public const int GL_MAX_VERTEX_ATTRIB_RELATIVE_OFFSET = 0x82D9;
	public const int GL_MAX_VERTEX_ATTRIB_BINDINGS = 0x82DA;
	public const int GL_TEXTURE_VIEW_MIN_LEVEL = 0x82DB;
	public const int GL_TEXTURE_VIEW_NUM_LEVELS = 0x82DC;
	public const int GL_TEXTURE_VIEW_MIN_LAYER = 0x82DD;
	public const int GL_TEXTURE_VIEW_NUM_LAYERS = 0x82DE;
	public const int GL_TEXTURE_IMMUTABLE_LEVELS = 0x82DF;
	public const int GL_BUFFER = 0x82E0;
	public const int GL_SHADER = 0x82E1;
	public const int GL_PROGRAM = 0x82E2;
	public const int GL_QUERY = 0x82E3;
	public const int GL_PROGRAM_PIPELINE = 0x82E4;
	public const int GL_MAX_VERTEX_ATTRIB_STRIDE = 0x82E5;
	public const int GL_SAMPLER = 0x82E6;
	public const int GL_DISPLAY_LIST = 0x82E7;
	public const int GL_MAX_LABEL_LENGTH = 0x82E8;
	public const int GL_NUM_SHADING_LANGUAGE_VERSIONS = 0x82E9;
	public const int GL_QUERY_TARGET = 0x82EA;
	public const int GL_TRANSFORM_FEEDBACK_OVERFLOW = 0x82EC;
	public const int GL_TRANSFORM_FEEDBACK_STREAM_OVERFLOW = 0x82ED;
	public const int GL_VERTICES_SUBMITTED = 0x82EE;
	public const int GL_PRIMITIVES_SUBMITTED = 0x82EF;
	public const int GL_VERTEX_SHADER_INVOCATIONS = 0x82F0;
	public const int GL_TESS_CONTROL_SHADER_PATCHES = 0x82F1;
	public const int GL_TESS_EVALUATION_SHADER_INVOCATIONS = 0x82F2;
	public const int GL_GEOMETRY_SHADER_PRIMITIVES_EMITTED = 0x82F3;
	public const int GL_FRAGMENT_SHADER_INVOCATIONS = 0x82F4;
	public const int GL_COMPUTE_SHADER_INVOCATIONS = 0x82F5;
	public const int GL_CLIPPING_INPUT_PRIMITIVES = 0x82F6;
	public const int GL_CLIPPING_OUTPUT_PRIMITIVES = 0x82F7;
	public const int GL_MAX_CULL_DISTANCES = 0x82F9;
	public const int GL_MAX_COMBINED_CLIP_AND_CULL_DISTANCES = 0x82FA;
	public const int GL_CONTEXT_RELEASE_BEHAVIOR = 0x82FB;
	public const int GL_CONTEXT_RELEASE_BEHAVIOR_FLUSH = 0x82FC;
	public const int GL_UNSIGNED_BYTE_2_3_3_REV = 0x8362;
	public const int GL_UNSIGNED_SHORT_5_6_5 = 0x8363;
	public const int GL_UNSIGNED_SHORT_5_6_5_REV = 0x8364;
	public const int GL_UNSIGNED_SHORT_4_4_4_4_REV = 0x8365;
	public const int GL_UNSIGNED_SHORT_1_5_5_5_REV = 0x8366;
	public const int GL_UNSIGNED_INT_8_8_8_8_REV = 0x8367;
	public const int GL_UNSIGNED_INT_2_10_10_10_REV = 0x8368;
	public const int GL_MIRRORED_REPEAT = 0x8370;
	public const int GL_ALIASED_LINE_WIDTH_RANGE = 0x846E;
	public const int GL_TEXTURE0 = 0x84C0;
	public const int GL_TEXTURE1 = 0x84C1;
	public const int GL_TEXTURE2 = 0x84C2;
	public const int GL_TEXTURE3 = 0x84C3;
	public const int GL_TEXTURE4 = 0x84C4;
	public const int GL_TEXTURE5 = 0x84C5;
	public const int GL_TEXTURE6 = 0x84C6;
	public const int GL_TEXTURE7 = 0x84C7;
	public const int GL_TEXTURE8 = 0x84C8;
	public const int GL_TEXTURE9 = 0x84C9;
	public const int GL_TEXTURE10 = 0x84CA;
	public const int GL_TEXTURE11 = 0x84CB;
	public const int GL_TEXTURE12 = 0x84CC;
	public const int GL_TEXTURE13 = 0x84CD;
	public const int GL_TEXTURE14 = 0x84CE;
	public const int GL_TEXTURE15 = 0x84CF;
	public const int GL_TEXTURE16 = 0x84D0;
	public const int GL_TEXTURE17 = 0x84D1;
	public const int GL_TEXTURE18 = 0x84D2;
	public const int GL_TEXTURE19 = 0x84D3;
	public const int GL_TEXTURE20 = 0x84D4;
	public const int GL_TEXTURE21 = 0x84D5;
	public const int GL_TEXTURE22 = 0x84D6;
	public const int GL_TEXTURE23 = 0x84D7;
	public const int GL_TEXTURE24 = 0x84D8;
	public const int GL_TEXTURE25 = 0x84D9;
	public const int GL_TEXTURE26 = 0x84DA;
	public const int GL_TEXTURE27 = 0x84DB;
	public const int GL_TEXTURE28 = 0x84DC;
	public const int GL_TEXTURE29 = 0x84DD;
	public const int GL_TEXTURE30 = 0x84DE;
	public const int GL_TEXTURE31 = 0x84DF;
	public const int GL_ACTIVE_TEXTURE = 0x84E0;
	public const int GL_MAX_RENDERBUFFER_SIZE = 0x84E8;
	public const int GL_COMPRESSED_RGB = 0x84ED;
	public const int GL_COMPRESSED_RGBA = 0x84EE;
	public const int GL_TEXTURE_COMPRESSION_HINT = 0x84EF;
	public const int GL_UNIFORM_BLOCK_REFERENCED_BY_TESS_CONTROL_SHADER = 0x84F0;
	public const int GL_UNIFORM_BLOCK_REFERENCED_BY_TESS_EVALUATION_SHADER = 0x84F1;
	public const int GL_TEXTURE_RECTANGLE = 0x84F5;
	public const int GL_TEXTURE_BINDING_RECTANGLE = 0x84F6;
	public const int GL_PROXY_TEXTURE_RECTANGLE = 0x84F7;
	public const int GL_MAX_RECTANGLE_TEXTURE_SIZE = 0x84F8;
	public const int GL_DEPTH_STENCIL = 0x84F9;
	public const int GL_UNSIGNED_INT_24_8 = 0x84FA;
	public const int GL_MAX_TEXTURE_LOD_BIAS = 0x84FD;
	public const int GL_TEXTURE_MAX_ANISOTROPY = 0x84FE;
	public const int GL_MAX_TEXTURE_MAX_ANISOTROPY = 0x84FF;
	public const int GL_TEXTURE_LOD_BIAS = 0x8501;
	public const int GL_INCR_WRAP = 0x8507;
	public const int GL_DECR_WRAP = 0x8508;
	public const int GL_TEXTURE_CUBE_MAP = 0x8513;
	public const int GL_TEXTURE_BINDING_CUBE_MAP = 0x8514;
	public const int GL_TEXTURE_CUBE_MAP_POSITIVE_X = 0x8515;
	public const int GL_TEXTURE_CUBE_MAP_NEGATIVE_X = 0x8516;
	public const int GL_TEXTURE_CUBE_MAP_POSITIVE_Y = 0x8517;
	public const int GL_TEXTURE_CUBE_MAP_NEGATIVE_Y = 0x8518;
	public const int GL_TEXTURE_CUBE_MAP_POSITIVE_Z = 0x8519;
	public const int GL_TEXTURE_CUBE_MAP_NEGATIVE_Z = 0x851A;
	public const int GL_PROXY_TEXTURE_CUBE_MAP = 0x851B;
	public const int GL_MAX_CUBE_MAP_TEXTURE_SIZE = 0x851C;
	public const int GL_SRC1_ALPHA = 0x8589;
	public const int GL_VERTEX_ARRAY_BINDING = 0x85B5;
	public const int GL_VERTEX_ATTRIB_ARRAY_ENABLED = 0x8622;
	public const int GL_VERTEX_ATTRIB_ARRAY_SIZE = 0x8623;
	public const int GL_VERTEX_ATTRIB_ARRAY_STRIDE = 0x8624;
	public const int GL_VERTEX_ATTRIB_ARRAY_TYPE = 0x8625;
	public const int GL_CURRENT_VERTEX_ATTRIB = 0x8626;
	public const int GL_VERTEX_PROGRAM_POINT_SIZE = 0x8642;
	public const int GL_PROGRAM_POINT_SIZE = 0x8642;
	public const int GL_VERTEX_ATTRIB_ARRAY_POINTER = 0x8645;
	public const int GL_DEPTH_CLAMP = 0x864F;
	public const int GL_TEXTURE_COMPRESSED_IMAGE_SIZE = 0x86A0;
	public const int GL_TEXTURE_COMPRESSED = 0x86A1;
	public const int GL_NUM_COMPRESSED_TEXTURE_FORMATS = 0x86A2;
	public const int GL_COMPRESSED_TEXTURE_FORMATS = 0x86A3;
	public const int GL_PROGRAM_BINARY_LENGTH = 0x8741;
	public const int GL_MIRROR_CLAMP_TO_EDGE = 0x8743;
	public const int GL_VERTEX_ATTRIB_ARRAY_LONG = 0x874E;
	public const int GL_BUFFER_SIZE = 0x8764;
	public const int GL_BUFFER_USAGE = 0x8765;
	public const int GL_NUM_PROGRAM_BINARY_FORMATS = 0x87FE;
	public const int GL_PROGRAM_BINARY_FORMATS = 0x87FF;
	public const int GL_STENCIL_BACK_FUNC = 0x8800;
	public const int GL_STENCIL_BACK_FAIL = 0x8801;
	public const int GL_STENCIL_BACK_PASS_DEPTH_FAIL = 0x8802;
	public const int GL_STENCIL_BACK_PASS_DEPTH_PASS = 0x8803;
	public const int GL_RGBA32F = 0x8814;
	public const int GL_RGB32F = 0x8815;
	public const int GL_RGBA16F = 0x881A;
	public const int GL_RGB16F = 0x881B;
	public const int GL_MAX_DRAW_BUFFERS = 0x8824;
	public const int GL_DRAW_BUFFER0 = 0x8825;
	public const int GL_DRAW_BUFFER1 = 0x8826;
	public const int GL_DRAW_BUFFER2 = 0x8827;
	public const int GL_DRAW_BUFFER3 = 0x8828;
	public const int GL_DRAW_BUFFER4 = 0x8829;
	public const int GL_DRAW_BUFFER5 = 0x882A;
	public const int GL_DRAW_BUFFER6 = 0x882B;
	public const int GL_DRAW_BUFFER7 = 0x882C;
	public const int GL_DRAW_BUFFER8 = 0x882D;
	public const int GL_DRAW_BUFFER9 = 0x882E;
	public const int GL_DRAW_BUFFER10 = 0x882F;
	public const int GL_DRAW_BUFFER11 = 0x8830;
	public const int GL_DRAW_BUFFER12 = 0x8831;
	public const int GL_DRAW_BUFFER13 = 0x8832;
	public const int GL_DRAW_BUFFER14 = 0x8833;
	public const int GL_DRAW_BUFFER15 = 0x8834;
	public const int GL_BLEND_EQUATION_ALPHA = 0x883D;
	public const int GL_TEXTURE_DEPTH_SIZE = 0x884A;
	public const int GL_TEXTURE_COMPARE_MODE = 0x884C;
	public const int GL_TEXTURE_COMPARE_FUNC = 0x884D;
	public const int GL_COMPARE_REF_TO_TEXTURE = 0x884E;
	public const int GL_TEXTURE_CUBE_MAP_SEAMLESS = 0x884F;
	public const int GL_QUERY_COUNTER_BITS = 0x8864;
	public const int GL_CURRENT_QUERY = 0x8865;
	public const int GL_QUERY_RESULT = 0x8866;
	public const int GL_QUERY_RESULT_AVAILABLE = 0x8867;
	public const int GL_MAX_VERTEX_ATTRIBS = 0x8869;
	public const int GL_VERTEX_ATTRIB_ARRAY_NORMALIZED = 0x886A;
	public const int GL_MAX_TESS_CONTROL_INPUT_COMPONENTS = 0x886C;
	public const int GL_MAX_TESS_EVALUATION_INPUT_COMPONENTS = 0x886D;
	public const int GL_MAX_TEXTURE_IMAGE_UNITS = 0x8872;
	public const int GL_GEOMETRY_SHADER_INVOCATIONS = 0x887F;
	public const int GL_ARRAY_BUFFER = 0x8892;
	public const int GL_ELEMENT_ARRAY_BUFFER = 0x8893;
	public const int GL_ARRAY_BUFFER_BINDING = 0x8894;
	public const int GL_ELEMENT_ARRAY_BUFFER_BINDING = 0x8895;
	public const int GL_VERTEX_ATTRIB_ARRAY_BUFFER_BINDING = 0x889F;
	public const int GL_READ_ONLY = 0x88B8;
	public const int GL_WRITE_ONLY = 0x88B9;
	public const int GL_READ_WRITE = 0x88BA;
	public const int GL_BUFFER_ACCESS = 0x88BB;
	public const int GL_BUFFER_MAPPED = 0x88BC;
	public const int GL_BUFFER_MAP_POINTER = 0x88BD;
	public const int GL_TIME_ELAPSED = 0x88BF;
	public const int GL_STREAM_DRAW = 0x88E0;
	public const int GL_STREAM_READ = 0x88E1;
	public const int GL_STREAM_COPY = 0x88E2;
	public const int GL_STATIC_DRAW = 0x88E4;
	public const int GL_STATIC_READ = 0x88E5;
	public const int GL_STATIC_COPY = 0x88E6;
	public const int GL_DYNAMIC_DRAW = 0x88E8;
	public const int GL_DYNAMIC_READ = 0x88E9;
	public const int GL_DYNAMIC_COPY = 0x88EA;
	public const int GL_PIXEL_PACK_BUFFER = 0x88EB;
	public const int GL_PIXEL_UNPACK_BUFFER = 0x88EC;
	public const int GL_PIXEL_PACK_BUFFER_BINDING = 0x88ED;
	public const int GL_PIXEL_UNPACK_BUFFER_BINDING = 0x88EF;
	public const int GL_DEPTH24_STENCIL8 = 0x88F0;
	public const int GL_TEXTURE_STENCIL_SIZE = 0x88F1;
	public const int GL_SRC1_COLOR = 0x88F9;
	public const int GL_ONE_MINUS_SRC1_COLOR = 0x88FA;
	public const int GL_ONE_MINUS_SRC1_ALPHA = 0x88FB;
	public const int GL_MAX_DUAL_SOURCE_DRAW_BUFFERS = 0x88FC;
	public const int GL_VERTEX_ATTRIB_ARRAY_INTEGER = 0x88FD;
	public const int GL_VERTEX_ATTRIB_ARRAY_DIVISOR = 0x88FE;
	public const int GL_MAX_ARRAY_TEXTURE_LAYERS = 0x88FF;
	public const int GL_MIN_PROGRAM_TEXEL_OFFSET = 0x8904;
	public const int GL_MAX_PROGRAM_TEXEL_OFFSET = 0x8905;
	public const int GL_SAMPLES_PASSED = 0x8914;
	public const int GL_GEOMETRY_VERTICES_OUT = 0x8916;
	public const int GL_GEOMETRY_INPUT_TYPE = 0x8917;
	public const int GL_GEOMETRY_OUTPUT_TYPE = 0x8918;
	public const int GL_SAMPLER_BINDING = 0x8919;
	public const int GL_CLAMP_READ_COLOR = 0x891C;
	public const int GL_FIXED_ONLY = 0x891D;
	public const int GL_UNIFORM_BUFFER = 0x8A11;
	public const int GL_UNIFORM_BUFFER_BINDING = 0x8A28;
	public const int GL_UNIFORM_BUFFER_START = 0x8A29;
	public const int GL_UNIFORM_BUFFER_SIZE = 0x8A2A;
	public const int GL_MAX_VERTEX_UNIFORM_BLOCKS = 0x8A2B;
	public const int GL_MAX_GEOMETRY_UNIFORM_BLOCKS = 0x8A2C;
	public const int GL_MAX_FRAGMENT_UNIFORM_BLOCKS = 0x8A2D;
	public const int GL_MAX_COMBINED_UNIFORM_BLOCKS = 0x8A2E;
	public const int GL_MAX_UNIFORM_BUFFER_BINDINGS = 0x8A2F;
	public const int GL_MAX_UNIFORM_BLOCK_SIZE = 0x8A30;
	public const int GL_MAX_COMBINED_VERTEX_UNIFORM_COMPONENTS = 0x8A31;
	public const int GL_MAX_COMBINED_GEOMETRY_UNIFORM_COMPONENTS = 0x8A32;
	public const int GL_MAX_COMBINED_FRAGMENT_UNIFORM_COMPONENTS = 0x8A33;
	public const int GL_UNIFORM_BUFFER_OFFSET_ALIGNMENT = 0x8A34;
	public const int GL_ACTIVE_UNIFORM_BLOCK_MAX_NAME_LENGTH = 0x8A35;
	public const int GL_ACTIVE_UNIFORM_BLOCKS = 0x8A36;
	public const int GL_UNIFORM_TYPE = 0x8A37;
	public const int GL_UNIFORM_SIZE = 0x8A38;
	public const int GL_UNIFORM_NAME_LENGTH = 0x8A39;
	public const int GL_UNIFORM_BLOCK_INDEX = 0x8A3A;
	public const int GL_UNIFORM_OFFSET = 0x8A3B;
	public const int GL_UNIFORM_ARRAY_STRIDE = 0x8A3C;
	public const int GL_UNIFORM_MATRIX_STRIDE = 0x8A3D;
	public const int GL_UNIFORM_IS_ROW_MAJOR = 0x8A3E;
	public const int GL_UNIFORM_BLOCK_BINDING = 0x8A3F;
	public const int GL_UNIFORM_BLOCK_DATA_SIZE = 0x8A40;
	public const int GL_UNIFORM_BLOCK_NAME_LENGTH = 0x8A41;
	public const int GL_UNIFORM_BLOCK_ACTIVE_UNIFORMS = 0x8A42;
	public const int GL_UNIFORM_BLOCK_ACTIVE_UNIFORM_INDICES = 0x8A43;
	public const int GL_UNIFORM_BLOCK_REFERENCED_BY_VERTEX_SHADER = 0x8A44;
	public const int GL_UNIFORM_BLOCK_REFERENCED_BY_GEOMETRY_SHADER = 0x8A45;
	public const int GL_UNIFORM_BLOCK_REFERENCED_BY_FRAGMENT_SHADER = 0x8A46;
	public const int GL_FRAGMENT_SHADER = 0x8B30;
	public const int GL_VERTEX_SHADER = 0x8B31;
	public const int GL_MAX_FRAGMENT_UNIFORM_COMPONENTS = 0x8B49;
	public const int GL_MAX_VERTEX_UNIFORM_COMPONENTS = 0x8B4A;
	public const int GL_MAX_VARYING_FLOATS = 0x8B4B;
	public const int GL_MAX_VARYING_COMPONENTS = 0x8B4B;
	public const int GL_MAX_VERTEX_TEXTURE_IMAGE_UNITS = 0x8B4C;
	public const int GL_MAX_COMBINED_TEXTURE_IMAGE_UNITS = 0x8B4D;
	public const int GL_SHADER_TYPE = 0x8B4F;
	public const int GL_FLOAT_VEC2 = 0x8B50;
	public const int GL_FLOAT_VEC3 = 0x8B51;
	public const int GL_FLOAT_VEC4 = 0x8B52;
	public const int GL_INT_VEC2 = 0x8B53;
	public const int GL_INT_VEC3 = 0x8B54;
	public const int GL_INT_VEC4 = 0x8B55;
	public const int GL_BOOL = 0x8B56;
	public const int GL_BOOL_VEC2 = 0x8B57;
	public const int GL_BOOL_VEC3 = 0x8B58;
	public const int GL_BOOL_VEC4 = 0x8B59;
	public const int GL_FLOAT_MAT2 = 0x8B5A;
	public const int GL_FLOAT_MAT3 = 0x8B5B;
	public const int GL_FLOAT_MAT4 = 0x8B5C;
	public const int GL_SAMPLER_1D = 0x8B5D;
	public const int GL_SAMPLER_2D = 0x8B5E;
	public const int GL_SAMPLER_3D = 0x8B5F;
	public const int GL_SAMPLER_CUBE = 0x8B60;
	public const int GL_SAMPLER_1D_SHADOW = 0x8B61;
	public const int GL_SAMPLER_2D_SHADOW = 0x8B62;
	public const int GL_SAMPLER_2D_RECT = 0x8B63;
	public const int GL_SAMPLER_2D_RECT_SHADOW = 0x8B64;
	public const int GL_FLOAT_MAT2x3 = 0x8B65;
	public const int GL_FLOAT_MAT2x4 = 0x8B66;
	public const int GL_FLOAT_MAT3x2 = 0x8B67;
	public const int GL_FLOAT_MAT3x4 = 0x8B68;
	public const int GL_FLOAT_MAT4x2 = 0x8B69;
	public const int GL_FLOAT_MAT4x3 = 0x8B6A;
	public const int GL_DELETE_STATUS = 0x8B80;
	public const int GL_COMPILE_STATUS = 0x8B81;
	public const int GL_LINK_STATUS = 0x8B82;
	public const int GL_VALIDATE_STATUS = 0x8B83;
	public const int GL_INFO_LOG_LENGTH = 0x8B84;
	public const int GL_ATTACHED_SHADERS = 0x8B85;
	public const int GL_ACTIVE_UNIFORMS = 0x8B86;
	public const int GL_ACTIVE_UNIFORM_MAX_LENGTH = 0x8B87;
	public const int GL_SHADER_SOURCE_LENGTH = 0x8B88;
	public const int GL_ACTIVE_ATTRIBUTES = 0x8B89;
	public const int GL_ACTIVE_ATTRIBUTE_MAX_LENGTH = 0x8B8A;
	public const int GL_FRAGMENT_SHADER_DERIVATIVE_HINT = 0x8B8B;
	public const int GL_SHADING_LANGUAGE_VERSION = 0x8B8C;
	public const int GL_CURRENT_PROGRAM = 0x8B8D;
	public const int GL_IMPLEMENTATION_COLOR_READ_TYPE = 0x8B9A;
	public const int GL_IMPLEMENTATION_COLOR_READ_FORMAT = 0x8B9B;
	public const int GL_TEXTURE_RED_TYPE = 0x8C10;
	public const int GL_TEXTURE_GREEN_TYPE = 0x8C11;
	public const int GL_TEXTURE_BLUE_TYPE = 0x8C12;
	public const int GL_TEXTURE_ALPHA_TYPE = 0x8C13;
	public const int GL_TEXTURE_DEPTH_TYPE = 0x8C16;
	public const int GL_UNSIGNED_NORMALIZED = 0x8C17;
	public const int GL_TEXTURE_1D_ARRAY = 0x8C18;
	public const int GL_PROXY_TEXTURE_1D_ARRAY = 0x8C19;
	public const int GL_TEXTURE_2D_ARRAY = 0x8C1A;
	public const int GL_PROXY_TEXTURE_2D_ARRAY = 0x8C1B;
	public const int GL_TEXTURE_BINDING_1D_ARRAY = 0x8C1C;
	public const int GL_TEXTURE_BINDING_2D_ARRAY = 0x8C1D;
	public const int GL_MAX_GEOMETRY_TEXTURE_IMAGE_UNITS = 0x8C29;
	public const int GL_TEXTURE_BUFFER = 0x8C2A;
	public const int GL_TEXTURE_BUFFER_BINDING = 0x8C2A;
	public const int GL_MAX_TEXTURE_BUFFER_SIZE = 0x8C2B;
	public const int GL_TEXTURE_BINDING_BUFFER = 0x8C2C;
	public const int GL_TEXTURE_BUFFER_DATA_STORE_BINDING = 0x8C2D;
	public const int GL_ANY_SAMPLES_PASSED = 0x8C2F;
	public const int GL_SAMPLE_SHADING = 0x8C36;
	public const int GL_MIN_SAMPLE_SHADING_VALUE = 0x8C37;
	public const int GL_R11F_G11F_B10F = 0x8C3A;
	public const int GL_UNSIGNED_INT_10F_11F_11F_REV = 0x8C3B;
	public const int GL_RGB9_E5 = 0x8C3D;
	public const int GL_UNSIGNED_INT_5_9_9_9_REV = 0x8C3E;
	public const int GL_TEXTURE_SHARED_SIZE = 0x8C3F;
	public const int GL_SRGB = 0x8C40;
	public const int GL_SRGB8 = 0x8C41;
	public const int GL_SRGB_ALPHA = 0x8C42;
	public const int GL_SRGB8_ALPHA8 = 0x8C43;
	public const int GL_COMPRESSED_SRGB = 0x8C48;
	public const int GL_COMPRESSED_SRGB_ALPHA = 0x8C49;
	public const int GL_TRANSFORM_FEEDBACK_VARYING_MAX_LENGTH = 0x8C76;
	public const int GL_TRANSFORM_FEEDBACK_BUFFER_MODE = 0x8C7F;
	public const int GL_MAX_TRANSFORM_FEEDBACK_SEPARATE_COMPONENTS = 0x8C80;
	public const int GL_TRANSFORM_FEEDBACK_VARYINGS = 0x8C83;
	public const int GL_TRANSFORM_FEEDBACK_BUFFER_START = 0x8C84;
	public const int GL_TRANSFORM_FEEDBACK_BUFFER_SIZE = 0x8C85;
	public const int GL_PRIMITIVES_GENERATED = 0x8C87;
	public const int GL_TRANSFORM_FEEDBACK_PRIMITIVES_WRITTEN = 0x8C88;
	public const int GL_RASTERIZER_DISCARD = 0x8C89;
	public const int GL_MAX_TRANSFORM_FEEDBACK_INTERLEAVED_COMPONENTS = 0x8C8A;
	public const int GL_MAX_TRANSFORM_FEEDBACK_SEPARATE_ATTRIBS = 0x8C8B;
	public const int GL_INTERLEAVED_ATTRIBS = 0x8C8C;
	public const int GL_SEPARATE_ATTRIBS = 0x8C8D;
	public const int GL_TRANSFORM_FEEDBACK_BUFFER = 0x8C8E;
	public const int GL_TRANSFORM_FEEDBACK_BUFFER_BINDING = 0x8C8F;
	public const int GL_POINT_SPRITE_COORD_ORIGIN = 0x8CA0;
	public const int GL_LOWER_LEFT = 0x8CA1;
	public const int GL_UPPER_LEFT = 0x8CA2;
	public const int GL_STENCIL_BACK_REF = 0x8CA3;
	public const int GL_STENCIL_BACK_VALUE_MASK = 0x8CA4;
	public const int GL_STENCIL_BACK_WRITEMASK = 0x8CA5;
	public const int GL_DRAW_FRAMEBUFFER_BINDING = 0x8CA6;
	public const int GL_FRAMEBUFFER_BINDING = 0x8CA6;
	public const int GL_RENDERBUFFER_BINDING = 0x8CA7;
	public const int GL_READ_FRAMEBUFFER = 0x8CA8;
	public const int GL_DRAW_FRAMEBUFFER = 0x8CA9;
	public const int GL_READ_FRAMEBUFFER_BINDING = 0x8CAA;
	public const int GL_RENDERBUFFER_SAMPLES = 0x8CAB;
	public const int GL_DEPTH_COMPONENT32F = 0x8CAC;
	public const int GL_DEPTH32F_STENCIL8 = 0x8CAD;
	public const int GL_FRAMEBUFFER_ATTACHMENT_OBJECT_TYPE = 0x8CD0;
	public const int GL_FRAMEBUFFER_ATTACHMENT_OBJECT_NAME = 0x8CD1;
	public const int GL_FRAMEBUFFER_ATTACHMENT_TEXTURE_LEVEL = 0x8CD2;
	public const int GL_FRAMEBUFFER_ATTACHMENT_TEXTURE_CUBE_MAP_FACE = 0x8CD3;
	public const int GL_FRAMEBUFFER_ATTACHMENT_TEXTURE_LAYER = 0x8CD4;
	public const int GL_FRAMEBUFFER_COMPLETE = 0x8CD5;
	public const int GL_FRAMEBUFFER_INCOMPLETE_ATTACHMENT = 0x8CD6;
	public const int GL_FRAMEBUFFER_INCOMPLETE_MISSING_ATTACHMENT = 0x8CD7;
	public const int GL_FRAMEBUFFER_INCOMPLETE_DRAW_BUFFER = 0x8CDB;
	public const int GL_FRAMEBUFFER_INCOMPLETE_READ_BUFFER = 0x8CDC;
	public const int GL_FRAMEBUFFER_UNSUPPORTED = 0x8CDD;
	public const int GL_MAX_COLOR_ATTACHMENTS = 0x8CDF;
	public const int GL_COLOR_ATTACHMENT0 = 0x8CE0;
	public const int GL_COLOR_ATTACHMENT1 = 0x8CE1;
	public const int GL_COLOR_ATTACHMENT2 = 0x8CE2;
	public const int GL_COLOR_ATTACHMENT3 = 0x8CE3;
	public const int GL_COLOR_ATTACHMENT4 = 0x8CE4;
	public const int GL_COLOR_ATTACHMENT5 = 0x8CE5;
	public const int GL_COLOR_ATTACHMENT6 = 0x8CE6;
	public const int GL_COLOR_ATTACHMENT7 = 0x8CE7;
	public const int GL_COLOR_ATTACHMENT8 = 0x8CE8;
	public const int GL_COLOR_ATTACHMENT9 = 0x8CE9;
	public const int GL_COLOR_ATTACHMENT10 = 0x8CEA;
	public const int GL_COLOR_ATTACHMENT11 = 0x8CEB;
	public const int GL_COLOR_ATTACHMENT12 = 0x8CEC;
	public const int GL_COLOR_ATTACHMENT13 = 0x8CED;
	public const int GL_COLOR_ATTACHMENT14 = 0x8CEE;
	public const int GL_COLOR_ATTACHMENT15 = 0x8CEF;
	public const int GL_COLOR_ATTACHMENT16 = 0x8CF0;
	public const int GL_COLOR_ATTACHMENT17 = 0x8CF1;
	public const int GL_COLOR_ATTACHMENT18 = 0x8CF2;
	public const int GL_COLOR_ATTACHMENT19 = 0x8CF3;
	public const int GL_COLOR_ATTACHMENT20 = 0x8CF4;
	public const int GL_COLOR_ATTACHMENT21 = 0x8CF5;
	public const int GL_COLOR_ATTACHMENT22 = 0x8CF6;
	public const int GL_COLOR_ATTACHMENT23 = 0x8CF7;
	public const int GL_COLOR_ATTACHMENT24 = 0x8CF8;
	public const int GL_COLOR_ATTACHMENT25 = 0x8CF9;
	public const int GL_COLOR_ATTACHMENT26 = 0x8CFA;
	public const int GL_COLOR_ATTACHMENT27 = 0x8CFB;
	public const int GL_COLOR_ATTACHMENT28 = 0x8CFC;
	public const int GL_COLOR_ATTACHMENT29 = 0x8CFD;
	public const int GL_COLOR_ATTACHMENT30 = 0x8CFE;
	public const int GL_COLOR_ATTACHMENT31 = 0x8CFF;
	public const int GL_DEPTH_ATTACHMENT = 0x8D00;
	public const int GL_STENCIL_ATTACHMENT = 0x8D20;
	public const int GL_FRAMEBUFFER = 0x8D40;
	public const int GL_RENDERBUFFER = 0x8D41;
	public const int GL_RENDERBUFFER_WIDTH = 0x8D42;
	public const int GL_RENDERBUFFER_HEIGHT = 0x8D43;
	public const int GL_RENDERBUFFER_INTERNAL_FORMAT = 0x8D44;
	public const int GL_STENCIL_INDEX1 = 0x8D46;
	public const int GL_STENCIL_INDEX4 = 0x8D47;
	public const int GL_STENCIL_INDEX8 = 0x8D48;
	public const int GL_STENCIL_INDEX16 = 0x8D49;
	public const int GL_RENDERBUFFER_RED_SIZE = 0x8D50;
	public const int GL_RENDERBUFFER_GREEN_SIZE = 0x8D51;
	public const int GL_RENDERBUFFER_BLUE_SIZE = 0x8D52;
	public const int GL_RENDERBUFFER_ALPHA_SIZE = 0x8D53;
	public const int GL_RENDERBUFFER_DEPTH_SIZE = 0x8D54;
	public const int GL_RENDERBUFFER_STENCIL_SIZE = 0x8D55;
	public const int GL_FRAMEBUFFER_INCOMPLETE_MULTISAMPLE = 0x8D56;
	public const int GL_MAX_SAMPLES = 0x8D57;
	public const int GL_RGB565 = 0x8D62;
	public const int GL_PRIMITIVE_RESTART_FIXED_INDEX = 0x8D69;
	public const int GL_ANY_SAMPLES_PASSED_CONSERVATIVE = 0x8D6A;
	public const int GL_MAX_ELEMENT_INDEX = 0x8D6B;
	public const int GL_RGBA32UI = 0x8D70;
	public const int GL_RGB32UI = 0x8D71;
	public const int GL_RGBA16UI = 0x8D76;
	public const int GL_RGB16UI = 0x8D77;
	public const int GL_RGBA8UI = 0x8D7C;
	public const int GL_RGB8UI = 0x8D7D;
	public const int GL_RGBA32I = 0x8D82;
	public const int GL_RGB32I = 0x8D83;
	public const int GL_RGBA16I = 0x8D88;
	public const int GL_RGB16I = 0x8D89;
	public const int GL_RGBA8I = 0x8D8E;
	public const int GL_RGB8I = 0x8D8F;
	public const int GL_RED_INTEGER = 0x8D94;
	public const int GL_GREEN_INTEGER = 0x8D95;
	public const int GL_BLUE_INTEGER = 0x8D96;
	public const int GL_RGB_INTEGER = 0x8D98;
	public const int GL_RGBA_INTEGER = 0x8D99;
	public const int GL_BGR_INTEGER = 0x8D9A;
	public const int GL_BGRA_INTEGER = 0x8D9B;
	public const int GL_INT_2_10_10_10_REV = 0x8D9F;
	public const int GL_FRAMEBUFFER_ATTACHMENT_LAYERED = 0x8DA7;
	public const int GL_FRAMEBUFFER_INCOMPLETE_LAYER_TARGETS = 0x8DA8;
	public const int GL_FLOAT_32_UNSIGNED_INT_24_8_REV = 0x8DAD;
	public const int GL_FRAMEBUFFER_SRGB = 0x8DB9;
	public const int GL_COMPRESSED_RED_RGTC1 = 0x8DBB;
	public const int GL_COMPRESSED_SIGNED_RED_RGTC1 = 0x8DBC;
	public const int GL_COMPRESSED_RG_RGTC2 = 0x8DBD;
	public const int GL_COMPRESSED_SIGNED_RG_RGTC2 = 0x8DBE;
	public const int GL_SAMPLER_1D_ARRAY = 0x8DC0;
	public const int GL_SAMPLER_2D_ARRAY = 0x8DC1;
	public const int GL_SAMPLER_BUFFER = 0x8DC2;
	public const int GL_SAMPLER_1D_ARRAY_SHADOW = 0x8DC3;
	public const int GL_SAMPLER_2D_ARRAY_SHADOW = 0x8DC4;
	public const int GL_SAMPLER_CUBE_SHADOW = 0x8DC5;
	public const int GL_UNSIGNED_INT_VEC2 = 0x8DC6;
	public const int GL_UNSIGNED_INT_VEC3 = 0x8DC7;
	public const int GL_UNSIGNED_INT_VEC4 = 0x8DC8;
	public const int GL_INT_SAMPLER_1D = 0x8DC9;
	public const int GL_INT_SAMPLER_2D = 0x8DCA;
	public const int GL_INT_SAMPLER_3D = 0x8DCB;
	public const int GL_INT_SAMPLER_CUBE = 0x8DCC;
	public const int GL_INT_SAMPLER_2D_RECT = 0x8DCD;
	public const int GL_INT_SAMPLER_1D_ARRAY = 0x8DCE;
	public const int GL_INT_SAMPLER_2D_ARRAY = 0x8DCF;
	public const int GL_INT_SAMPLER_BUFFER = 0x8DD0;
	public const int GL_UNSIGNED_INT_SAMPLER_1D = 0x8DD1;
	public const int GL_UNSIGNED_INT_SAMPLER_2D = 0x8DD2;
	public const int GL_UNSIGNED_INT_SAMPLER_3D = 0x8DD3;
	public const int GL_UNSIGNED_INT_SAMPLER_CUBE = 0x8DD4;
	public const int GL_UNSIGNED_INT_SAMPLER_2D_RECT = 0x8DD5;
	public const int GL_UNSIGNED_INT_SAMPLER_1D_ARRAY = 0x8DD6;
	public const int GL_UNSIGNED_INT_SAMPLER_2D_ARRAY = 0x8DD7;
	public const int GL_UNSIGNED_INT_SAMPLER_BUFFER = 0x8DD8;
	public const int GL_GEOMETRY_SHADER = 0x8DD9;
	public const int GL_MAX_GEOMETRY_UNIFORM_COMPONENTS = 0x8DDF;
	public const int GL_MAX_GEOMETRY_OUTPUT_VERTICES = 0x8DE0;
	public const int GL_MAX_GEOMETRY_TOTAL_OUTPUT_COMPONENTS = 0x8DE1;
	public const int GL_ACTIVE_SUBROUTINES = 0x8DE5;
	public const int GL_ACTIVE_SUBROUTINE_UNIFORMS = 0x8DE6;
	public const int GL_MAX_SUBROUTINES = 0x8DE7;
	public const int GL_MAX_SUBROUTINE_UNIFORM_LOCATIONS = 0x8DE8;
	public const int GL_LOW_FLOAT = 0x8DF0;
	public const int GL_MEDIUM_FLOAT = 0x8DF1;
	public const int GL_HIGH_FLOAT = 0x8DF2;
	public const int GL_LOW_INT = 0x8DF3;
	public const int GL_MEDIUM_INT = 0x8DF4;
	public const int GL_HIGH_INT = 0x8DF5;
	public const int GL_SHADER_BINARY_FORMATS = 0x8DF8;
	public const int GL_NUM_SHADER_BINARY_FORMATS = 0x8DF9;
	public const int GL_SHADER_COMPILER = 0x8DFA;
	public const int GL_MAX_VERTEX_UNIFORM_VECTORS = 0x8DFB;
	public const int GL_MAX_VARYING_VECTORS = 0x8DFC;
	public const int GL_MAX_FRAGMENT_UNIFORM_VECTORS = 0x8DFD;
	public const int GL_QUERY_WAIT = 0x8E13;
	public const int GL_QUERY_NO_WAIT = 0x8E14;
	public const int GL_QUERY_BY_REGION_WAIT = 0x8E15;
	public const int GL_QUERY_BY_REGION_NO_WAIT = 0x8E16;
	public const int GL_QUERY_WAIT_INVERTED = 0x8E17;
	public const int GL_QUERY_NO_WAIT_INVERTED = 0x8E18;
	public const int GL_QUERY_BY_REGION_WAIT_INVERTED = 0x8E19;
	public const int GL_QUERY_BY_REGION_NO_WAIT_INVERTED = 0x8E1A;
	public const int GL_POLYGON_OFFSET_CLAMP = 0x8E1B;
	public const int GL_MAX_COMBINED_TESS_CONTROL_UNIFORM_COMPONENTS = 0x8E1E;
	public const int GL_MAX_COMBINED_TESS_EVALUATION_UNIFORM_COMPONENTS = 0x8E1F;
	public const int GL_TRANSFORM_FEEDBACK = 0x8E22;
	public const int GL_TRANSFORM_FEEDBACK_BUFFER_PAUSED = 0x8E23;
	public const int GL_TRANSFORM_FEEDBACK_PAUSED = 0x8E23;
	public const int GL_TRANSFORM_FEEDBACK_BUFFER_ACTIVE = 0x8E24;
	public const int GL_TRANSFORM_FEEDBACK_ACTIVE = 0x8E24;
	public const int GL_TRANSFORM_FEEDBACK_BINDING = 0x8E25;
	public const int GL_TIMESTAMP = 0x8E28;
	public const int GL_TEXTURE_SWIZZLE_R = 0x8E42;
	public const int GL_TEXTURE_SWIZZLE_G = 0x8E43;
	public const int GL_TEXTURE_SWIZZLE_B = 0x8E44;
	public const int GL_TEXTURE_SWIZZLE_A = 0x8E45;
	public const int GL_TEXTURE_SWIZZLE_RGBA = 0x8E46;
	public const int GL_ACTIVE_SUBROUTINE_UNIFORM_LOCATIONS = 0x8E47;
	public const int GL_ACTIVE_SUBROUTINE_MAX_LENGTH = 0x8E48;
	public const int GL_ACTIVE_SUBROUTINE_UNIFORM_MAX_LENGTH = 0x8E49;
	public const int GL_NUM_COMPATIBLE_SUBROUTINES = 0x8E4A;
	public const int GL_COMPATIBLE_SUBROUTINES = 0x8E4B;
	public const int GL_QUADS_FOLLOW_PROVOKING_VERTEX_CONVENTION = 0x8E4C;
	public const int GL_FIRST_VERTEX_CONVENTION = 0x8E4D;
	public const int GL_LAST_VERTEX_CONVENTION = 0x8E4E;
	public const int GL_PROVOKING_VERTEX = 0x8E4F;
	public const int GL_SAMPLE_POSITION = 0x8E50;
	public const int GL_SAMPLE_MASK = 0x8E51;
	public const int GL_SAMPLE_MASK_VALUE = 0x8E52;
	public const int GL_MAX_SAMPLE_MASK_WORDS = 0x8E59;
	public const int GL_MAX_GEOMETRY_SHADER_INVOCATIONS = 0x8E5A;
	public const int GL_MIN_FRAGMENT_INTERPOLATION_OFFSET = 0x8E5B;
	public const int GL_MAX_FRAGMENT_INTERPOLATION_OFFSET = 0x8E5C;
	public const int GL_FRAGMENT_INTERPOLATION_OFFSET_BITS = 0x8E5D;
	public const int GL_MIN_PROGRAM_TEXTURE_GATHER_OFFSET = 0x8E5E;
	public const int GL_MAX_PROGRAM_TEXTURE_GATHER_OFFSET = 0x8E5F;
	public const int GL_MAX_TRANSFORM_FEEDBACK_BUFFERS = 0x8E70;
	public const int GL_MAX_VERTEX_STREAMS = 0x8E71;
	public const int GL_PATCH_VERTICES = 0x8E72;
	public const int GL_PATCH_DEFAULT_INNER_LEVEL = 0x8E73;
	public const int GL_PATCH_DEFAULT_OUTER_LEVEL = 0x8E74;
	public const int GL_TESS_CONTROL_OUTPUT_VERTICES = 0x8E75;
	public const int GL_TESS_GEN_MODE = 0x8E76;
	public const int GL_TESS_GEN_SPACING = 0x8E77;
	public const int GL_TESS_GEN_VERTEX_ORDER = 0x8E78;
	public const int GL_TESS_GEN_POINT_MODE = 0x8E79;
	public const int GL_ISOLINES = 0x8E7A;
	public const int GL_FRACTIONAL_ODD = 0x8E7B;
	public const int GL_FRACTIONAL_EVEN = 0x8E7C;
	public const int GL_MAX_PATCH_VERTICES = 0x8E7D;
	public const int GL_MAX_TESS_GEN_LEVEL = 0x8E7E;
	public const int GL_MAX_TESS_CONTROL_UNIFORM_COMPONENTS = 0x8E7F;
	public const int GL_MAX_TESS_EVALUATION_UNIFORM_COMPONENTS = 0x8E80;
	public const int GL_MAX_TESS_CONTROL_TEXTURE_IMAGE_UNITS = 0x8E81;
	public const int GL_MAX_TESS_EVALUATION_TEXTURE_IMAGE_UNITS = 0x8E82;
	public const int GL_MAX_TESS_CONTROL_OUTPUT_COMPONENTS = 0x8E83;
	public const int GL_MAX_TESS_PATCH_COMPONENTS = 0x8E84;
	public const int GL_MAX_TESS_CONTROL_TOTAL_OUTPUT_COMPONENTS = 0x8E85;
	public const int GL_MAX_TESS_EVALUATION_OUTPUT_COMPONENTS = 0x8E86;
	public const int GL_TESS_EVALUATION_SHADER = 0x8E87;
	public const int GL_TESS_CONTROL_SHADER = 0x8E88;
	public const int GL_MAX_TESS_CONTROL_UNIFORM_BLOCKS = 0x8E89;
	public const int GL_MAX_TESS_EVALUATION_UNIFORM_BLOCKS = 0x8E8A;
	public const int GL_COMPRESSED_RGBA_BPTC_UNORM = 0x8E8C;
	public const int GL_COMPRESSED_SRGB_ALPHA_BPTC_UNORM = 0x8E8D;
	public const int GL_COMPRESSED_RGB_BPTC_SIGNED_FLOAT = 0x8E8E;
	public const int GL_COMPRESSED_RGB_BPTC_UNSIGNED_FLOAT = 0x8E8F;
	public const int GL_COPY_READ_BUFFER = 0x8F36;
	public const int GL_COPY_READ_BUFFER_BINDING = 0x8F36;
	public const int GL_COPY_WRITE_BUFFER = 0x8F37;
	public const int GL_COPY_WRITE_BUFFER_BINDING = 0x8F37;
	public const int GL_MAX_IMAGE_UNITS = 0x8F38;
	public const int GL_MAX_COMBINED_IMAGE_UNITS_AND_FRAGMENT_OUTPUTS = 0x8F39;
	public const int GL_MAX_COMBINED_SHADER_OUTPUT_RESOURCES = 0x8F39;
	public const int GL_IMAGE_BINDING_NAME = 0x8F3A;
	public const int GL_IMAGE_BINDING_LEVEL = 0x8F3B;
	public const int GL_IMAGE_BINDING_LAYERED = 0x8F3C;
	public const int GL_IMAGE_BINDING_LAYER = 0x8F3D;
	public const int GL_IMAGE_BINDING_ACCESS = 0x8F3E;
	public const int GL_DRAW_INDIRECT_BUFFER = 0x8F3F;
	public const int GL_DRAW_INDIRECT_BUFFER_BINDING = 0x8F43;
	public const int GL_DOUBLE_MAT2 = 0x8F46;
	public const int GL_DOUBLE_MAT3 = 0x8F47;
	public const int GL_DOUBLE_MAT4 = 0x8F48;
	public const int GL_DOUBLE_MAT2x3 = 0x8F49;
	public const int GL_DOUBLE_MAT2x4 = 0x8F4A;
	public const int GL_DOUBLE_MAT3x2 = 0x8F4B;
	public const int GL_DOUBLE_MAT3x4 = 0x8F4C;
	public const int GL_DOUBLE_MAT4x2 = 0x8F4D;
	public const int GL_DOUBLE_MAT4x3 = 0x8F4E;
	public const int GL_VERTEX_BINDING_BUFFER = 0x8F4F;
	public const int GL_R8_SNORM = 0x8F94;
	public const int GL_RG8_SNORM = 0x8F95;
	public const int GL_RGB8_SNORM = 0x8F96;
	public const int GL_RGBA8_SNORM = 0x8F97;
	public const int GL_R16_SNORM = 0x8F98;
	public const int GL_RG16_SNORM = 0x8F99;
	public const int GL_RGB16_SNORM = 0x8F9A;
	public const int GL_RGBA16_SNORM = 0x8F9B;
	public const int GL_SIGNED_NORMALIZED = 0x8F9C;
	public const int GL_PRIMITIVE_RESTART = 0x8F9D;
	public const int GL_PRIMITIVE_RESTART_INDEX = 0x8F9E;
	public const int GL_DOUBLE_VEC2 = 0x8FFC;
	public const int GL_DOUBLE_VEC3 = 0x8FFD;
	public const int GL_DOUBLE_VEC4 = 0x8FFE;
	public const int GL_TEXTURE_CUBE_MAP_ARRAY = 0x9009;
	public const int GL_TEXTURE_BINDING_CUBE_MAP_ARRAY = 0x900A;
	public const int GL_PROXY_TEXTURE_CUBE_MAP_ARRAY = 0x900B;
	public const int GL_SAMPLER_CUBE_MAP_ARRAY = 0x900C;
	public const int GL_SAMPLER_CUBE_MAP_ARRAY_SHADOW = 0x900D;
	public const int GL_INT_SAMPLER_CUBE_MAP_ARRAY = 0x900E;
	public const int GL_UNSIGNED_INT_SAMPLER_CUBE_MAP_ARRAY = 0x900F;
	public const int GL_IMAGE_1D = 0x904C;
	public const int GL_IMAGE_2D = 0x904D;
	public const int GL_IMAGE_3D = 0x904E;
	public const int GL_IMAGE_2D_RECT = 0x904F;
	public const int GL_IMAGE_CUBE = 0x9050;
	public const int GL_IMAGE_BUFFER = 0x9051;
	public const int GL_IMAGE_1D_ARRAY = 0x9052;
	public const int GL_IMAGE_2D_ARRAY = 0x9053;
	public const int GL_IMAGE_CUBE_MAP_ARRAY = 0x9054;
	public const int GL_IMAGE_2D_MULTISAMPLE = 0x9055;
	public const int GL_IMAGE_2D_MULTISAMPLE_ARRAY = 0x9056;
	public const int GL_INT_IMAGE_1D = 0x9057;
	public const int GL_INT_IMAGE_2D = 0x9058;
	public const int GL_INT_IMAGE_3D = 0x9059;
	public const int GL_INT_IMAGE_2D_RECT = 0x905A;
	public const int GL_INT_IMAGE_CUBE = 0x905B;
	public const int GL_INT_IMAGE_BUFFER = 0x905C;
	public const int GL_INT_IMAGE_1D_ARRAY = 0x905D;
	public const int GL_INT_IMAGE_2D_ARRAY = 0x905E;
	public const int GL_INT_IMAGE_CUBE_MAP_ARRAY = 0x905F;
	public const int GL_INT_IMAGE_2D_MULTISAMPLE = 0x9060;
	public const int GL_INT_IMAGE_2D_MULTISAMPLE_ARRAY = 0x9061;
	public const int GL_UNSIGNED_INT_IMAGE_1D = 0x9062;
	public const int GL_UNSIGNED_INT_IMAGE_2D = 0x9063;
	public const int GL_UNSIGNED_INT_IMAGE_3D = 0x9064;
	public const int GL_UNSIGNED_INT_IMAGE_2D_RECT = 0x9065;
	public const int GL_UNSIGNED_INT_IMAGE_CUBE = 0x9066;
	public const int GL_UNSIGNED_INT_IMAGE_BUFFER = 0x9067;
	public const int GL_UNSIGNED_INT_IMAGE_1D_ARRAY = 0x9068;
	public const int GL_UNSIGNED_INT_IMAGE_2D_ARRAY = 0x9069;
	public const int GL_UNSIGNED_INT_IMAGE_CUBE_MAP_ARRAY = 0x906A;
	public const int GL_UNSIGNED_INT_IMAGE_2D_MULTISAMPLE = 0x906B;
	public const int GL_UNSIGNED_INT_IMAGE_2D_MULTISAMPLE_ARRAY = 0x906C;
	public const int GL_MAX_IMAGE_SAMPLES = 0x906D;
	public const int GL_IMAGE_BINDING_FORMAT = 0x906E;
	public const int GL_RGB10_A2UI = 0x906F;
	public const int GL_MIN_MAP_BUFFER_ALIGNMENT = 0x90BC;
	public const int GL_IMAGE_FORMAT_COMPATIBILITY_TYPE = 0x90C7;
	public const int GL_IMAGE_FORMAT_COMPATIBILITY_BY_SIZE = 0x90C8;
	public const int GL_IMAGE_FORMAT_COMPATIBILITY_BY_CLASS = 0x90C9;
	public const int GL_MAX_VERTEX_IMAGE_UNIFORMS = 0x90CA;
	public const int GL_MAX_TESS_CONTROL_IMAGE_UNIFORMS = 0x90CB;
	public const int GL_MAX_TESS_EVALUATION_IMAGE_UNIFORMS = 0x90CC;
	public const int GL_MAX_GEOMETRY_IMAGE_UNIFORMS = 0x90CD;
	public const int GL_MAX_FRAGMENT_IMAGE_UNIFORMS = 0x90CE;
	public const int GL_MAX_COMBINED_IMAGE_UNIFORMS = 0x90CF;
	public const int GL_SHADER_STORAGE_BUFFER = 0x90D2;
	public const int GL_SHADER_STORAGE_BUFFER_BINDING = 0x90D3;
	public const int GL_SHADER_STORAGE_BUFFER_START = 0x90D4;
	public const int GL_SHADER_STORAGE_BUFFER_SIZE = 0x90D5;
	public const int GL_MAX_VERTEX_SHADER_STORAGE_BLOCKS = 0x90D6;
	public const int GL_MAX_GEOMETRY_SHADER_STORAGE_BLOCKS = 0x90D7;
	public const int GL_MAX_TESS_CONTROL_SHADER_STORAGE_BLOCKS = 0x90D8;
	public const int GL_MAX_TESS_EVALUATION_SHADER_STORAGE_BLOCKS = 0x90D9;
	public const int GL_MAX_FRAGMENT_SHADER_STORAGE_BLOCKS = 0x90DA;
	public const int GL_MAX_COMPUTE_SHADER_STORAGE_BLOCKS = 0x90DB;
	public const int GL_MAX_COMBINED_SHADER_STORAGE_BLOCKS = 0x90DC;
	public const int GL_MAX_SHADER_STORAGE_BUFFER_BINDINGS = 0x90DD;
	public const int GL_MAX_SHADER_STORAGE_BLOCK_SIZE = 0x90DE;
	public const int GL_SHADER_STORAGE_BUFFER_OFFSET_ALIGNMENT = 0x90DF;
	public const int GL_DEPTH_STENCIL_TEXTURE_MODE = 0x90EA;
	public const int GL_MAX_COMPUTE_WORK_GROUP_INVOCATIONS = 0x90EB;
	public const int GL_UNIFORM_BLOCK_REFERENCED_BY_COMPUTE_SHADER = 0x90EC;
	public const int GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_COMPUTE_SHADER = 0x90ED;
	public const int GL_DISPATCH_INDIRECT_BUFFER = 0x90EE;
	public const int GL_DISPATCH_INDIRECT_BUFFER_BINDING = 0x90EF;
	public const int GL_TEXTURE_2D_MULTISAMPLE = 0x9100;
	public const int GL_PROXY_TEXTURE_2D_MULTISAMPLE = 0x9101;
	public const int GL_TEXTURE_2D_MULTISAMPLE_ARRAY = 0x9102;
	public const int GL_PROXY_TEXTURE_2D_MULTISAMPLE_ARRAY = 0x9103;
	public const int GL_TEXTURE_BINDING_2D_MULTISAMPLE = 0x9104;
	public const int GL_TEXTURE_BINDING_2D_MULTISAMPLE_ARRAY = 0x9105;
	public const int GL_TEXTURE_SAMPLES = 0x9106;
	public const int GL_TEXTURE_FIXED_SAMPLE_LOCATIONS = 0x9107;
	public const int GL_SAMPLER_2D_MULTISAMPLE = 0x9108;
	public const int GL_INT_SAMPLER_2D_MULTISAMPLE = 0x9109;
	public const int GL_UNSIGNED_INT_SAMPLER_2D_MULTISAMPLE = 0x910A;
	public const int GL_SAMPLER_2D_MULTISAMPLE_ARRAY = 0x910B;
	public const int GL_INT_SAMPLER_2D_MULTISAMPLE_ARRAY = 0x910C;
	public const int GL_UNSIGNED_INT_SAMPLER_2D_MULTISAMPLE_ARRAY = 0x910D;
	public const int GL_MAX_COLOR_TEXTURE_SAMPLES = 0x910E;
	public const int GL_MAX_DEPTH_TEXTURE_SAMPLES = 0x910F;
	public const int GL_MAX_INTEGER_SAMPLES = 0x9110;
	public const int GL_MAX_SERVER_WAIT_TIMEOUT = 0x9111;
	public const int GL_OBJECT_TYPE = 0x9112;
	public const int GL_SYNC_CONDITION = 0x9113;
	public const int GL_SYNC_STATUS = 0x9114;
	public const int GL_SYNC_FLAGS = 0x9115;
	public const int GL_SYNC_FENCE = 0x9116;
	public const int GL_SYNC_GPU_COMMANDS_COMPLETE = 0x9117;
	public const int GL_UNSIGNALED = 0x9118;
	public const int GL_SIGNALED = 0x9119;
	public const int GL_ALREADY_SIGNALED = 0x911A;
	public const int GL_TIMEOUT_EXPIRED = 0x911B;
	public const int GL_CONDITION_SATISFIED = 0x911C;
	public const int GL_WAIT_FAILED = 0x911D;
	public const int GL_BUFFER_ACCESS_FLAGS = 0x911F;
	public const int GL_BUFFER_MAP_LENGTH = 0x9120;
	public const int GL_BUFFER_MAP_OFFSET = 0x9121;
	public const int GL_MAX_VERTEX_OUTPUT_COMPONENTS = 0x9122;
	public const int GL_MAX_GEOMETRY_INPUT_COMPONENTS = 0x9123;
	public const int GL_MAX_GEOMETRY_OUTPUT_COMPONENTS = 0x9124;
	public const int GL_MAX_FRAGMENT_INPUT_COMPONENTS = 0x9125;
	public const int GL_CONTEXT_PROFILE_MASK = 0x9126;
	public const int GL_UNPACK_COMPRESSED_BLOCK_WIDTH = 0x9127;
	public const int GL_UNPACK_COMPRESSED_BLOCK_HEIGHT = 0x9128;
	public const int GL_UNPACK_COMPRESSED_BLOCK_DEPTH = 0x9129;
	public const int GL_UNPACK_COMPRESSED_BLOCK_SIZE = 0x912A;
	public const int GL_PACK_COMPRESSED_BLOCK_WIDTH = 0x912B;
	public const int GL_PACK_COMPRESSED_BLOCK_HEIGHT = 0x912C;
	public const int GL_PACK_COMPRESSED_BLOCK_DEPTH = 0x912D;
	public const int GL_PACK_COMPRESSED_BLOCK_SIZE = 0x912E;
	public const int GL_TEXTURE_IMMUTABLE_FORMAT = 0x912F;
	public const int GL_MAX_DEBUG_MESSAGE_LENGTH = 0x9143;
	public const int GL_MAX_DEBUG_LOGGED_MESSAGES = 0x9144;
	public const int GL_DEBUG_LOGGED_MESSAGES = 0x9145;
	public const int GL_DEBUG_SEVERITY_HIGH = 0x9146;
	public const int GL_DEBUG_SEVERITY_MEDIUM = 0x9147;
	public const int GL_DEBUG_SEVERITY_LOW = 0x9148;
	public const int GL_QUERY_BUFFER = 0x9192;
	public const int GL_QUERY_BUFFER_BINDING = 0x9193;
	public const int GL_QUERY_RESULT_NO_WAIT = 0x9194;
	public const int GL_TEXTURE_BUFFER_OFFSET = 0x919D;
	public const int GL_TEXTURE_BUFFER_SIZE = 0x919E;
	public const int GL_TEXTURE_BUFFER_OFFSET_ALIGNMENT = 0x919F;
	public const int GL_COMPUTE_SHADER = 0x91B9;
	public const int GL_MAX_COMPUTE_UNIFORM_BLOCKS = 0x91BB;
	public const int GL_MAX_COMPUTE_TEXTURE_IMAGE_UNITS = 0x91BC;
	public const int GL_MAX_COMPUTE_IMAGE_UNIFORMS = 0x91BD;
	public const int GL_MAX_COMPUTE_WORK_GROUP_COUNT = 0x91BE;
	public const int GL_MAX_COMPUTE_WORK_GROUP_SIZE = 0x91BF;
	public const int GL_COMPRESSED_R11_EAC = 0x9270;
	public const int GL_COMPRESSED_SIGNED_R11_EAC = 0x9271;
	public const int GL_COMPRESSED_RG11_EAC = 0x9272;
	public const int GL_COMPRESSED_SIGNED_RG11_EAC = 0x9273;
	public const int GL_COMPRESSED_RGB8_ETC2 = 0x9274;
	public const int GL_COMPRESSED_SRGB8_ETC2 = 0x9275;
	public const int GL_COMPRESSED_RGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9276;
	public const int GL_COMPRESSED_SRGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9277;
	public const int GL_COMPRESSED_RGBA8_ETC2_EAC = 0x9278;
	public const int GL_COMPRESSED_SRGB8_ALPHA8_ETC2_EAC = 0x9279;
	public const int GL_ATOMIC_COUNTER_BUFFER = 0x92C0;
	public const int GL_ATOMIC_COUNTER_BUFFER_BINDING = 0x92C1;
	public const int GL_ATOMIC_COUNTER_BUFFER_START = 0x92C2;
	public const int GL_ATOMIC_COUNTER_BUFFER_SIZE = 0x92C3;
	public const int GL_ATOMIC_COUNTER_BUFFER_DATA_SIZE = 0x92C4;
	public const int GL_ATOMIC_COUNTER_BUFFER_ACTIVE_ATOMIC_COUNTERS = 0x92C5;
	public const int GL_ATOMIC_COUNTER_BUFFER_ACTIVE_ATOMIC_COUNTER_INDICES = 0x92C6;
	public const int GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_VERTEX_SHADER = 0x92C7;
	public const int GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_TESS_CONTROL_SHADER = 0x92C8;
	public const int GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_TESS_EVALUATION_SHADER = 0x92C9;
	public const int GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_GEOMETRY_SHADER = 0x92CA;
	public const int GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_FRAGMENT_SHADER = 0x92CB;
	public const int GL_MAX_VERTEX_ATOMIC_COUNTER_BUFFERS = 0x92CC;
	public const int GL_MAX_TESS_CONTROL_ATOMIC_COUNTER_BUFFERS = 0x92CD;
	public const int GL_MAX_TESS_EVALUATION_ATOMIC_COUNTER_BUFFERS = 0x92CE;
	public const int GL_MAX_GEOMETRY_ATOMIC_COUNTER_BUFFERS = 0x92CF;
	public const int GL_MAX_FRAGMENT_ATOMIC_COUNTER_BUFFERS = 0x92D0;
	public const int GL_MAX_COMBINED_ATOMIC_COUNTER_BUFFERS = 0x92D1;
	public const int GL_MAX_VERTEX_ATOMIC_COUNTERS = 0x92D2;
	public const int GL_MAX_TESS_CONTROL_ATOMIC_COUNTERS = 0x92D3;
	public const int GL_MAX_TESS_EVALUATION_ATOMIC_COUNTERS = 0x92D4;
	public const int GL_MAX_GEOMETRY_ATOMIC_COUNTERS = 0x92D5;
	public const int GL_MAX_FRAGMENT_ATOMIC_COUNTERS = 0x92D6;
	public const int GL_MAX_COMBINED_ATOMIC_COUNTERS = 0x92D7;
	public const int GL_MAX_ATOMIC_COUNTER_BUFFER_SIZE = 0x92D8;
	public const int GL_ACTIVE_ATOMIC_COUNTER_BUFFERS = 0x92D9;
	public const int GL_UNIFORM_ATOMIC_COUNTER_BUFFER_INDEX = 0x92DA;
	public const int GL_UNSIGNED_INT_ATOMIC_COUNTER = 0x92DB;
	public const int GL_MAX_ATOMIC_COUNTER_BUFFER_BINDINGS = 0x92DC;
	public const int GL_DEBUG_OUTPUT = 0x92E0;
	public const int GL_UNIFORM = 0x92E1;
	public const int GL_UNIFORM_BLOCK = 0x92E2;
	public const int GL_PROGRAM_INPUT = 0x92E3;
	public const int GL_PROGRAM_OUTPUT = 0x92E4;
	public const int GL_BUFFER_VARIABLE = 0x92E5;
	public const int GL_SHADER_STORAGE_BLOCK = 0x92E6;
	public const int GL_IS_PER_PATCH = 0x92E7;
	public const int GL_VERTEX_SUBROUTINE = 0x92E8;
	public const int GL_TESS_CONTROL_SUBROUTINE = 0x92E9;
	public const int GL_TESS_EVALUATION_SUBROUTINE = 0x92EA;
	public const int GL_GEOMETRY_SUBROUTINE = 0x92EB;
	public const int GL_FRAGMENT_SUBROUTINE = 0x92EC;
	public const int GL_COMPUTE_SUBROUTINE = 0x92ED;
	public const int GL_VERTEX_SUBROUTINE_UNIFORM = 0x92EE;
	public const int GL_TESS_CONTROL_SUBROUTINE_UNIFORM = 0x92EF;
	public const int GL_TESS_EVALUATION_SUBROUTINE_UNIFORM = 0x92F0;
	public const int GL_GEOMETRY_SUBROUTINE_UNIFORM = 0x92F1;
	public const int GL_FRAGMENT_SUBROUTINE_UNIFORM = 0x92F2;
	public const int GL_COMPUTE_SUBROUTINE_UNIFORM = 0x92F3;
	public const int GL_TRANSFORM_FEEDBACK_VARYING = 0x92F4;
	public const int GL_ACTIVE_RESOURCES = 0x92F5;
	public const int GL_MAX_NAME_LENGTH = 0x92F6;
	public const int GL_MAX_NUM_ACTIVE_VARIABLES = 0x92F7;
	public const int GL_MAX_NUM_COMPATIBLE_SUBROUTINES = 0x92F8;
	public const int GL_NAME_LENGTH = 0x92F9;
	public const int GL_TYPE = 0x92FA;
	public const int GL_ARRAY_SIZE = 0x92FB;
	public const int GL_OFFSET = 0x92FC;
	public const int GL_BLOCK_INDEX = 0x92FD;
	public const int GL_ARRAY_STRIDE = 0x92FE;
	public const int GL_MATRIX_STRIDE = 0x92FF;
	public const int GL_IS_ROW_MAJOR = 0x9300;
	public const int GL_ATOMIC_COUNTER_BUFFER_INDEX = 0x9301;
	public const int GL_BUFFER_BINDING = 0x9302;
	public const int GL_BUFFER_DATA_SIZE = 0x9303;
	public const int GL_NUM_ACTIVE_VARIABLES = 0x9304;
	public const int GL_ACTIVE_VARIABLES = 0x9305;
	public const int GL_REFERENCED_BY_VERTEX_SHADER = 0x9306;
	public const int GL_REFERENCED_BY_TESS_CONTROL_SHADER = 0x9307;
	public const int GL_REFERENCED_BY_TESS_EVALUATION_SHADER = 0x9308;
	public const int GL_REFERENCED_BY_GEOMETRY_SHADER = 0x9309;
	public const int GL_REFERENCED_BY_FRAGMENT_SHADER = 0x930A;
	public const int GL_REFERENCED_BY_COMPUTE_SHADER = 0x930B;
	public const int GL_TOP_LEVEL_ARRAY_SIZE = 0x930C;
	public const int GL_TOP_LEVEL_ARRAY_STRIDE = 0x930D;
	public const int GL_LOCATION = 0x930E;
	public const int GL_LOCATION_INDEX = 0x930F;
	public const int GL_FRAMEBUFFER_DEFAULT_WIDTH = 0x9310;
	public const int GL_FRAMEBUFFER_DEFAULT_HEIGHT = 0x9311;
	public const int GL_FRAMEBUFFER_DEFAULT_LAYERS = 0x9312;
	public const int GL_FRAMEBUFFER_DEFAULT_SAMPLES = 0x9313;
	public const int GL_FRAMEBUFFER_DEFAULT_FIXED_SAMPLE_LOCATIONS = 0x9314;
	public const int GL_MAX_FRAMEBUFFER_WIDTH = 0x9315;
	public const int GL_MAX_FRAMEBUFFER_HEIGHT = 0x9316;
	public const int GL_MAX_FRAMEBUFFER_LAYERS = 0x9317;
	public const int GL_MAX_FRAMEBUFFER_SAMPLES = 0x9318;
	public const int GL_LOCATION_COMPONENT = 0x934A;
	public const int GL_TRANSFORM_FEEDBACK_BUFFER_INDEX = 0x934B;
	public const int GL_TRANSFORM_FEEDBACK_BUFFER_STRIDE = 0x934C;
	public const int GL_CLIP_ORIGIN = 0x935C;
	public const int GL_CLIP_DEPTH_MODE = 0x935D;
	public const int GL_NEGATIVE_ONE_TO_ONE = 0x935E;
	public const int GL_ZERO_TO_ONE = 0x935F;
	public const int GL_CLEAR_TEXTURE = 0x9365;
	public const int GL_NUM_SAMPLE_COUNTS = 0x9380;
	public const int GL_SHADER_BINARY_FORMAT_SPIR_V = 0x9551;
	public const int GL_SPIR_V_BINARY = 0x9552;
	public const int GL_SPIR_V_EXTENSIONS = 0x9553;
	public const int GL_NUM_SPIR_V_EXTENSIONS = 0x9554;

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glActiveShaderProgramDelegate(uint pipeline, uint program);
	private static glActiveShaderProgramDelegate s_glActiveShaderProgram;
	public static void glActiveShaderProgram(uint pipeline, uint program) => s_glActiveShaderProgram(pipeline, program);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glActiveTextureDelegate(int texture);
	private static glActiveTextureDelegate s_glActiveTexture;
	public static void glActiveTexture(int texture) => s_glActiveTexture(texture);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glAttachShaderDelegate(uint program, uint shader);
	private static glAttachShaderDelegate s_glAttachShader;
	public static void glAttachShader(uint program, uint shader) => s_glAttachShader(program, shader);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBeginConditionalRenderDelegate(uint id, int mode);
	private static glBeginConditionalRenderDelegate s_glBeginConditionalRender;
	public static void glBeginConditionalRender(uint id, int mode) => s_glBeginConditionalRender(id, mode);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBeginQueryDelegate(int target, uint id);
	private static glBeginQueryDelegate s_glBeginQuery;
	public static void glBeginQuery(int target, uint id) => s_glBeginQuery(target, id);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBeginQueryIndexedDelegate(int target, uint index, uint id);
	private static glBeginQueryIndexedDelegate s_glBeginQueryIndexed;
	public static void glBeginQueryIndexed(int target, uint index, uint id) => s_glBeginQueryIndexed(target, index, id);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBeginTransformFeedbackDelegate(int primitiveMode);
	private static glBeginTransformFeedbackDelegate s_glBeginTransformFeedback;
	public static void glBeginTransformFeedback(int primitiveMode) => s_glBeginTransformFeedback(primitiveMode);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindAttribLocationDelegate(uint program, uint index, char *name);
	private static glBindAttribLocationDelegate s_glBindAttribLocation;
	public static void glBindAttribLocation(uint program, uint index, char *name) => s_glBindAttribLocation(program, index, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindBufferDelegate(int target, uint buffer);
	private static glBindBufferDelegate s_glBindBuffer;
	public static void glBindBuffer(int target, uint buffer) => s_glBindBuffer(target, buffer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindBufferBaseDelegate(int target, uint index, uint buffer);
	private static glBindBufferBaseDelegate s_glBindBufferBase;
	public static void glBindBufferBase(int target, uint index, uint buffer) => s_glBindBufferBase(target, index, buffer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindBufferRangeDelegate(int target, uint index, uint buffer, IntPtr offset, IntPtr size);
	private static glBindBufferRangeDelegate s_glBindBufferRange;
	public static void glBindBufferRange(int target, uint index, uint buffer, IntPtr offset, IntPtr size) => s_glBindBufferRange(target, index, buffer, offset, size);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindBuffersBaseDelegate(int target, uint first, int count, uint *buffers);
	private static glBindBuffersBaseDelegate s_glBindBuffersBase;
	public static void glBindBuffersBase(int target, uint first, int count, uint *buffers) => s_glBindBuffersBase(target, first, count, buffers);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindBuffersRangeDelegate(int target, uint first, int count, uint *buffers, IntPtr *offsets, IntPtr *sizes);
	private static glBindBuffersRangeDelegate s_glBindBuffersRange;
	public static void glBindBuffersRange(int target, uint first, int count, uint *buffers, IntPtr *offsets, IntPtr *sizes) => s_glBindBuffersRange(target, first, count, buffers, offsets, sizes);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindFragDataLocationDelegate(uint program, uint color, char *name);
	private static glBindFragDataLocationDelegate s_glBindFragDataLocation;
	public static void glBindFragDataLocation(uint program, uint color, char *name) => s_glBindFragDataLocation(program, color, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindFragDataLocationIndexedDelegate(uint program, uint colorNumber, uint index, char *name);
	private static glBindFragDataLocationIndexedDelegate s_glBindFragDataLocationIndexed;
	public static void glBindFragDataLocationIndexed(uint program, uint colorNumber, uint index, char *name) => s_glBindFragDataLocationIndexed(program, colorNumber, index, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindFramebufferDelegate(int target, uint framebuffer);
	private static glBindFramebufferDelegate s_glBindFramebuffer;
	public static void glBindFramebuffer(int target, uint framebuffer) => s_glBindFramebuffer(target, framebuffer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindImageTextureDelegate(uint unit, uint texture, int level, bool layered, int layer, int access, int format);
	private static glBindImageTextureDelegate s_glBindImageTexture;
	public static void glBindImageTexture(uint unit, uint texture, int level, bool layered, int layer, int access, int format) => s_glBindImageTexture(unit, texture, level, layered, layer, access, format);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindImageTexturesDelegate(uint first, int count, uint *textures);
	private static glBindImageTexturesDelegate s_glBindImageTextures;
	public static void glBindImageTextures(uint first, int count, uint *textures) => s_glBindImageTextures(first, count, textures);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindProgramPipelineDelegate(uint pipeline);
	private static glBindProgramPipelineDelegate s_glBindProgramPipeline;
	public static void glBindProgramPipeline(uint pipeline) => s_glBindProgramPipeline(pipeline);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindRenderbufferDelegate(int target, uint renderbuffer);
	private static glBindRenderbufferDelegate s_glBindRenderbuffer;
	public static void glBindRenderbuffer(int target, uint renderbuffer) => s_glBindRenderbuffer(target, renderbuffer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindSamplerDelegate(uint unit, uint sampler);
	private static glBindSamplerDelegate s_glBindSampler;
	public static void glBindSampler(uint unit, uint sampler) => s_glBindSampler(unit, sampler);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindSamplersDelegate(uint first, int count, uint *samplers);
	private static glBindSamplersDelegate s_glBindSamplers;
	public static void glBindSamplers(uint first, int count, uint *samplers) => s_glBindSamplers(first, count, samplers);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindTextureDelegate(int target, uint texture);
	private static glBindTextureDelegate s_glBindTexture;
	public static void glBindTexture(int target, uint texture) => s_glBindTexture(target, texture);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindTextureUnitDelegate(uint unit, uint texture);
	private static glBindTextureUnitDelegate s_glBindTextureUnit;
	public static void glBindTextureUnit(uint unit, uint texture) => s_glBindTextureUnit(unit, texture);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindTexturesDelegate(uint first, int count, uint *textures);
	private static glBindTexturesDelegate s_glBindTextures;
	public static void glBindTextures(uint first, int count, uint *textures) => s_glBindTextures(first, count, textures);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindTransformFeedbackDelegate(int target, uint id);
	private static glBindTransformFeedbackDelegate s_glBindTransformFeedback;
	public static void glBindTransformFeedback(int target, uint id) => s_glBindTransformFeedback(target, id);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindVertexArrayDelegate(uint array);
	private static glBindVertexArrayDelegate s_glBindVertexArray;
	public static void glBindVertexArray(uint array) => s_glBindVertexArray(array);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindVertexBufferDelegate(uint bindingindex, uint buffer, IntPtr offset, int stride);
	private static glBindVertexBufferDelegate s_glBindVertexBuffer;
	public static void glBindVertexBuffer(uint bindingindex, uint buffer, IntPtr offset, int stride) => s_glBindVertexBuffer(bindingindex, buffer, offset, stride);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBindVertexBuffersDelegate(uint first, int count, uint *buffers, IntPtr *offsets, int *strides);
	private static glBindVertexBuffersDelegate s_glBindVertexBuffers;
	public static void glBindVertexBuffers(uint first, int count, uint *buffers, IntPtr *offsets, int *strides) => s_glBindVertexBuffers(first, count, buffers, offsets, strides);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBlendColorDelegate(float red, float green, float blue, float alpha);
	private static glBlendColorDelegate s_glBlendColor;
	public static void glBlendColor(float red, float green, float blue, float alpha) => s_glBlendColor(red, green, blue, alpha);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBlendEquationDelegate(int mode);
	private static glBlendEquationDelegate s_glBlendEquation;
	public static void glBlendEquation(int mode) => s_glBlendEquation(mode);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBlendEquationSeparateDelegate(int modeRGB, int modeAlpha);
	private static glBlendEquationSeparateDelegate s_glBlendEquationSeparate;
	public static void glBlendEquationSeparate(int modeRGB, int modeAlpha) => s_glBlendEquationSeparate(modeRGB, modeAlpha);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBlendEquationSeparateiDelegate(uint buf, int modeRGB, int modeAlpha);
	private static glBlendEquationSeparateiDelegate s_glBlendEquationSeparatei;
	public static void glBlendEquationSeparatei(uint buf, int modeRGB, int modeAlpha) => s_glBlendEquationSeparatei(buf, modeRGB, modeAlpha);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBlendEquationiDelegate(uint buf, int mode);
	private static glBlendEquationiDelegate s_glBlendEquationi;
	public static void glBlendEquationi(uint buf, int mode) => s_glBlendEquationi(buf, mode);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBlendFuncDelegate(int sfactor, int dfactor);
	private static glBlendFuncDelegate s_glBlendFunc;
	public static void glBlendFunc(int sfactor, int dfactor) => s_glBlendFunc(sfactor, dfactor);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBlendFuncSeparateDelegate(int sfactorRGB, int dfactorRGB, int sfactorAlpha, int dfactorAlpha);
	private static glBlendFuncSeparateDelegate s_glBlendFuncSeparate;
	public static void glBlendFuncSeparate(int sfactorRGB, int dfactorRGB, int sfactorAlpha, int dfactorAlpha) => s_glBlendFuncSeparate(sfactorRGB, dfactorRGB, sfactorAlpha, dfactorAlpha);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBlendFuncSeparateiDelegate(uint buf, int srcRGB, int dstRGB, int srcAlpha, int dstAlpha);
	private static glBlendFuncSeparateiDelegate s_glBlendFuncSeparatei;
	public static void glBlendFuncSeparatei(uint buf, int srcRGB, int dstRGB, int srcAlpha, int dstAlpha) => s_glBlendFuncSeparatei(buf, srcRGB, dstRGB, srcAlpha, dstAlpha);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBlendFunciDelegate(uint buf, int src, int dst);
	private static glBlendFunciDelegate s_glBlendFunci;
	public static void glBlendFunci(uint buf, int src, int dst) => s_glBlendFunci(buf, src, dst);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBlitFramebufferDelegate(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, int mask, int filter);
	private static glBlitFramebufferDelegate s_glBlitFramebuffer;
	public static void glBlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, int mask, int filter) => s_glBlitFramebuffer(srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBlitNamedFramebufferDelegate(uint readFramebuffer, uint drawFramebuffer, int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, int mask, int filter);
	private static glBlitNamedFramebufferDelegate s_glBlitNamedFramebuffer;
	public static void glBlitNamedFramebuffer(uint readFramebuffer, uint drawFramebuffer, int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, int mask, int filter) => s_glBlitNamedFramebuffer(readFramebuffer, drawFramebuffer, srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBufferDataDelegate(int target, IntPtr size, void *data, int usage);
	private static glBufferDataDelegate s_glBufferData;
	public static void glBufferData(int target, IntPtr size, void *data, int usage) => s_glBufferData(target, size, data, usage);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBufferStorageDelegate(int target, IntPtr size, void *data, int flags);
	private static glBufferStorageDelegate s_glBufferStorage;
	public static void glBufferStorage(int target, IntPtr size, void *data, int flags) => s_glBufferStorage(target, size, data, flags);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glBufferSubDataDelegate(int target, IntPtr offset, IntPtr size, void *data);
	private static glBufferSubDataDelegate s_glBufferSubData;
	public static void glBufferSubData(int target, IntPtr offset, IntPtr size, void *data) => s_glBufferSubData(target, offset, size, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int glCheckFramebufferStatusDelegate(int target);
	private static glCheckFramebufferStatusDelegate s_glCheckFramebufferStatus;
	public static int glCheckFramebufferStatus(int target) => s_glCheckFramebufferStatus(target);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int glCheckNamedFramebufferStatusDelegate(uint framebuffer, int target);
	private static glCheckNamedFramebufferStatusDelegate s_glCheckNamedFramebufferStatus;
	public static int glCheckNamedFramebufferStatus(uint framebuffer, int target) => s_glCheckNamedFramebufferStatus(framebuffer, target);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClampColorDelegate(int target, int clamp);
	private static glClampColorDelegate s_glClampColor;
	public static void glClampColor(int target, int clamp) => s_glClampColor(target, clamp);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearDelegate(int mask);
	private static glClearDelegate s_glClear;
	public static void glClear(int mask) => s_glClear(mask);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearBufferDataDelegate(int target, int internalformat, int format, int type, void *data);
	private static glClearBufferDataDelegate s_glClearBufferData;
	public static void glClearBufferData(int target, int internalformat, int format, int type, void *data) => s_glClearBufferData(target, internalformat, format, type, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearBufferSubDataDelegate(int target, int internalformat, IntPtr offset, IntPtr size, int format, int type, void *data);
	private static glClearBufferSubDataDelegate s_glClearBufferSubData;
	public static void glClearBufferSubData(int target, int internalformat, IntPtr offset, IntPtr size, int format, int type, void *data) => s_glClearBufferSubData(target, internalformat, offset, size, format, type, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearBufferfiDelegate(int buffer, int drawbuffer, float depth, int stencil);
	private static glClearBufferfiDelegate s_glClearBufferfi;
	public static void glClearBufferfi(int buffer, int drawbuffer, float depth, int stencil) => s_glClearBufferfi(buffer, drawbuffer, depth, stencil);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearBufferfvDelegate(int buffer, int drawbuffer, float *value);
	private static glClearBufferfvDelegate s_glClearBufferfv;
	public static void glClearBufferfv(int buffer, int drawbuffer, float *value) => s_glClearBufferfv(buffer, drawbuffer, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearBufferivDelegate(int buffer, int drawbuffer, int *value);
	private static glClearBufferivDelegate s_glClearBufferiv;
	public static void glClearBufferiv(int buffer, int drawbuffer, int *value) => s_glClearBufferiv(buffer, drawbuffer, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearBufferuivDelegate(int buffer, int drawbuffer, uint *value);
	private static glClearBufferuivDelegate s_glClearBufferuiv;
	public static void glClearBufferuiv(int buffer, int drawbuffer, uint *value) => s_glClearBufferuiv(buffer, drawbuffer, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearColorDelegate(float red, float green, float blue, float alpha);
	private static glClearColorDelegate s_glClearColor;
	public static void glClearColor(float red, float green, float blue, float alpha) => s_glClearColor(red, green, blue, alpha);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearDepthDelegate(double depth);
	private static glClearDepthDelegate s_glClearDepth;
	public static void glClearDepth(double depth) => s_glClearDepth(depth);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearDepthfDelegate(float d);
	private static glClearDepthfDelegate s_glClearDepthf;
	public static void glClearDepthf(float d) => s_glClearDepthf(d);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearNamedBufferDataDelegate(uint buffer, int internalformat, int format, int type, void *data);
	private static glClearNamedBufferDataDelegate s_glClearNamedBufferData;
	public static void glClearNamedBufferData(uint buffer, int internalformat, int format, int type, void *data) => s_glClearNamedBufferData(buffer, internalformat, format, type, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearNamedBufferSubDataDelegate(uint buffer, int internalformat, IntPtr offset, IntPtr size, int format, int type, void *data);
	private static glClearNamedBufferSubDataDelegate s_glClearNamedBufferSubData;
	public static void glClearNamedBufferSubData(uint buffer, int internalformat, IntPtr offset, IntPtr size, int format, int type, void *data) => s_glClearNamedBufferSubData(buffer, internalformat, offset, size, format, type, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearNamedFramebufferfiDelegate(uint framebuffer, int buffer, int drawbuffer, float depth, int stencil);
	private static glClearNamedFramebufferfiDelegate s_glClearNamedFramebufferfi;
	public static void glClearNamedFramebufferfi(uint framebuffer, int buffer, int drawbuffer, float depth, int stencil) => s_glClearNamedFramebufferfi(framebuffer, buffer, drawbuffer, depth, stencil);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearNamedFramebufferfvDelegate(uint framebuffer, int buffer, int drawbuffer, float *value);
	private static glClearNamedFramebufferfvDelegate s_glClearNamedFramebufferfv;
	public static void glClearNamedFramebufferfv(uint framebuffer, int buffer, int drawbuffer, float *value) => s_glClearNamedFramebufferfv(framebuffer, buffer, drawbuffer, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearNamedFramebufferivDelegate(uint framebuffer, int buffer, int drawbuffer, int *value);
	private static glClearNamedFramebufferivDelegate s_glClearNamedFramebufferiv;
	public static void glClearNamedFramebufferiv(uint framebuffer, int buffer, int drawbuffer, int *value) => s_glClearNamedFramebufferiv(framebuffer, buffer, drawbuffer, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearNamedFramebufferuivDelegate(uint framebuffer, int buffer, int drawbuffer, uint *value);
	private static glClearNamedFramebufferuivDelegate s_glClearNamedFramebufferuiv;
	public static void glClearNamedFramebufferuiv(uint framebuffer, int buffer, int drawbuffer, uint *value) => s_glClearNamedFramebufferuiv(framebuffer, buffer, drawbuffer, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearStencilDelegate(int s);
	private static glClearStencilDelegate s_glClearStencil;
	public static void glClearStencil(int s) => s_glClearStencil(s);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearTexImageDelegate(uint texture, int level, int format, int type, void *data);
	private static glClearTexImageDelegate s_glClearTexImage;
	public static void glClearTexImage(uint texture, int level, int format, int type, void *data) => s_glClearTexImage(texture, level, format, type, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClearTexSubImageDelegate(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, void *data);
	private static glClearTexSubImageDelegate s_glClearTexSubImage;
	public static void glClearTexSubImage(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, void *data) => s_glClearTexSubImage(texture, level, xoffset, yoffset, zoffset, width, height, depth, format, type, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int glClientWaitSyncDelegate(IntPtr sync, int flags, ulong timeout);
	private static glClientWaitSyncDelegate s_glClientWaitSync;
	public static int glClientWaitSync(IntPtr sync, int flags, ulong timeout) => s_glClientWaitSync(sync, flags, timeout);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glClipControlDelegate(int origin, int depth);
	private static glClipControlDelegate s_glClipControl;
	public static void glClipControl(int origin, int depth) => s_glClipControl(origin, depth);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glColorMaskDelegate(bool red, bool green, bool blue, bool alpha);
	private static glColorMaskDelegate s_glColorMask;
	public static void glColorMask(bool red, bool green, bool blue, bool alpha) => s_glColorMask(red, green, blue, alpha);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glColorMaskiDelegate(uint index, bool r, bool g, bool b, bool a);
	private static glColorMaskiDelegate s_glColorMaski;
	public static void glColorMaski(uint index, bool r, bool g, bool b, bool a) => s_glColorMaski(index, r, g, b, a);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glColorP3uiDelegate(int type, uint color);
	private static glColorP3uiDelegate s_glColorP3ui;
	public static void glColorP3ui(int type, uint color) => s_glColorP3ui(type, color);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glColorP3uivDelegate(int type, uint *color);
	private static glColorP3uivDelegate s_glColorP3uiv;
	public static void glColorP3uiv(int type, uint *color) => s_glColorP3uiv(type, color);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glColorP4uiDelegate(int type, uint color);
	private static glColorP4uiDelegate s_glColorP4ui;
	public static void glColorP4ui(int type, uint color) => s_glColorP4ui(type, color);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glColorP4uivDelegate(int type, uint *color);
	private static glColorP4uivDelegate s_glColorP4uiv;
	public static void glColorP4uiv(int type, uint *color) => s_glColorP4uiv(type, color);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCompileShaderDelegate(uint shader);
	private static glCompileShaderDelegate s_glCompileShader;
	public static void glCompileShader(uint shader) => s_glCompileShader(shader);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCompressedTexImage1DDelegate(int target, int level, int internalformat, int width, int border, int imageSize, void *data);
	private static glCompressedTexImage1DDelegate s_glCompressedTexImage1D;
	public static void glCompressedTexImage1D(int target, int level, int internalformat, int width, int border, int imageSize, void *data) => s_glCompressedTexImage1D(target, level, internalformat, width, border, imageSize, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCompressedTexImage2DDelegate(int target, int level, int internalformat, int width, int height, int border, int imageSize, void *data);
	private static glCompressedTexImage2DDelegate s_glCompressedTexImage2D;
	public static void glCompressedTexImage2D(int target, int level, int internalformat, int width, int height, int border, int imageSize, void *data) => s_glCompressedTexImage2D(target, level, internalformat, width, height, border, imageSize, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCompressedTexImage3DDelegate(int target, int level, int internalformat, int width, int height, int depth, int border, int imageSize, void *data);
	private static glCompressedTexImage3DDelegate s_glCompressedTexImage3D;
	public static void glCompressedTexImage3D(int target, int level, int internalformat, int width, int height, int depth, int border, int imageSize, void *data) => s_glCompressedTexImage3D(target, level, internalformat, width, height, depth, border, imageSize, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCompressedTexSubImage1DDelegate(int target, int level, int xoffset, int width, int format, int imageSize, void *data);
	private static glCompressedTexSubImage1DDelegate s_glCompressedTexSubImage1D;
	public static void glCompressedTexSubImage1D(int target, int level, int xoffset, int width, int format, int imageSize, void *data) => s_glCompressedTexSubImage1D(target, level, xoffset, width, format, imageSize, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCompressedTexSubImage2DDelegate(int target, int level, int xoffset, int yoffset, int width, int height, int format, int imageSize, void *data);
	private static glCompressedTexSubImage2DDelegate s_glCompressedTexSubImage2D;
	public static void glCompressedTexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int imageSize, void *data) => s_glCompressedTexSubImage2D(target, level, xoffset, yoffset, width, height, format, imageSize, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCompressedTexSubImage3DDelegate(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int imageSize, void *data);
	private static glCompressedTexSubImage3DDelegate s_glCompressedTexSubImage3D;
	public static void glCompressedTexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int imageSize, void *data) => s_glCompressedTexSubImage3D(target, level, xoffset, yoffset, zoffset, width, height, depth, format, imageSize, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCompressedTextureSubImage1DDelegate(uint texture, int level, int xoffset, int width, int format, int imageSize, void *data);
	private static glCompressedTextureSubImage1DDelegate s_glCompressedTextureSubImage1D;
	public static void glCompressedTextureSubImage1D(uint texture, int level, int xoffset, int width, int format, int imageSize, void *data) => s_glCompressedTextureSubImage1D(texture, level, xoffset, width, format, imageSize, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCompressedTextureSubImage2DDelegate(uint texture, int level, int xoffset, int yoffset, int width, int height, int format, int imageSize, void *data);
	private static glCompressedTextureSubImage2DDelegate s_glCompressedTextureSubImage2D;
	public static void glCompressedTextureSubImage2D(uint texture, int level, int xoffset, int yoffset, int width, int height, int format, int imageSize, void *data) => s_glCompressedTextureSubImage2D(texture, level, xoffset, yoffset, width, height, format, imageSize, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCompressedTextureSubImage3DDelegate(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int imageSize, void *data);
	private static glCompressedTextureSubImage3DDelegate s_glCompressedTextureSubImage3D;
	public static void glCompressedTextureSubImage3D(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int imageSize, void *data) => s_glCompressedTextureSubImage3D(texture, level, xoffset, yoffset, zoffset, width, height, depth, format, imageSize, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCopyBufferSubDataDelegate(int readTarget, int writeTarget, IntPtr readOffset, IntPtr writeOffset, IntPtr size);
	private static glCopyBufferSubDataDelegate s_glCopyBufferSubData;
	public static void glCopyBufferSubData(int readTarget, int writeTarget, IntPtr readOffset, IntPtr writeOffset, IntPtr size) => s_glCopyBufferSubData(readTarget, writeTarget, readOffset, writeOffset, size);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCopyImageSubDataDelegate(uint srcName, int srcTarget, int srcLevel, int srcX, int srcY, int srcZ, uint dstName, int dstTarget, int dstLevel, int dstX, int dstY, int dstZ, int srcWidth, int srcHeight, int srcDepth);
	private static glCopyImageSubDataDelegate s_glCopyImageSubData;
	public static void glCopyImageSubData(uint srcName, int srcTarget, int srcLevel, int srcX, int srcY, int srcZ, uint dstName, int dstTarget, int dstLevel, int dstX, int dstY, int dstZ, int srcWidth, int srcHeight, int srcDepth) => s_glCopyImageSubData(srcName, srcTarget, srcLevel, srcX, srcY, srcZ, dstName, dstTarget, dstLevel, dstX, dstY, dstZ, srcWidth, srcHeight, srcDepth);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCopyNamedBufferSubDataDelegate(uint readBuffer, uint writeBuffer, IntPtr readOffset, IntPtr writeOffset, IntPtr size);
	private static glCopyNamedBufferSubDataDelegate s_glCopyNamedBufferSubData;
	public static void glCopyNamedBufferSubData(uint readBuffer, uint writeBuffer, IntPtr readOffset, IntPtr writeOffset, IntPtr size) => s_glCopyNamedBufferSubData(readBuffer, writeBuffer, readOffset, writeOffset, size);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCopyTexImage1DDelegate(int target, int level, int internalformat, int x, int y, int width, int border);
	private static glCopyTexImage1DDelegate s_glCopyTexImage1D;
	public static void glCopyTexImage1D(int target, int level, int internalformat, int x, int y, int width, int border) => s_glCopyTexImage1D(target, level, internalformat, x, y, width, border);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCopyTexImage2DDelegate(int target, int level, int internalformat, int x, int y, int width, int height, int border);
	private static glCopyTexImage2DDelegate s_glCopyTexImage2D;
	public static void glCopyTexImage2D(int target, int level, int internalformat, int x, int y, int width, int height, int border) => s_glCopyTexImage2D(target, level, internalformat, x, y, width, height, border);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCopyTexSubImage1DDelegate(int target, int level, int xoffset, int x, int y, int width);
	private static glCopyTexSubImage1DDelegate s_glCopyTexSubImage1D;
	public static void glCopyTexSubImage1D(int target, int level, int xoffset, int x, int y, int width) => s_glCopyTexSubImage1D(target, level, xoffset, x, y, width);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCopyTexSubImage2DDelegate(int target, int level, int xoffset, int yoffset, int x, int y, int width, int height);
	private static glCopyTexSubImage2DDelegate s_glCopyTexSubImage2D;
	public static void glCopyTexSubImage2D(int target, int level, int xoffset, int yoffset, int x, int y, int width, int height) => s_glCopyTexSubImage2D(target, level, xoffset, yoffset, x, y, width, height);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCopyTexSubImage3DDelegate(int target, int level, int xoffset, int yoffset, int zoffset, int x, int y, int width, int height);
	private static glCopyTexSubImage3DDelegate s_glCopyTexSubImage3D;
	public static void glCopyTexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int x, int y, int width, int height) => s_glCopyTexSubImage3D(target, level, xoffset, yoffset, zoffset, x, y, width, height);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCopyTextureSubImage1DDelegate(uint texture, int level, int xoffset, int x, int y, int width);
	private static glCopyTextureSubImage1DDelegate s_glCopyTextureSubImage1D;
	public static void glCopyTextureSubImage1D(uint texture, int level, int xoffset, int x, int y, int width) => s_glCopyTextureSubImage1D(texture, level, xoffset, x, y, width);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCopyTextureSubImage2DDelegate(uint texture, int level, int xoffset, int yoffset, int x, int y, int width, int height);
	private static glCopyTextureSubImage2DDelegate s_glCopyTextureSubImage2D;
	public static void glCopyTextureSubImage2D(uint texture, int level, int xoffset, int yoffset, int x, int y, int width, int height) => s_glCopyTextureSubImage2D(texture, level, xoffset, yoffset, x, y, width, height);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCopyTextureSubImage3DDelegate(uint texture, int level, int xoffset, int yoffset, int zoffset, int x, int y, int width, int height);
	private static glCopyTextureSubImage3DDelegate s_glCopyTextureSubImage3D;
	public static void glCopyTextureSubImage3D(uint texture, int level, int xoffset, int yoffset, int zoffset, int x, int y, int width, int height) => s_glCopyTextureSubImage3D(texture, level, xoffset, yoffset, zoffset, x, y, width, height);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCreateBuffersDelegate(int n, uint *buffers);
	private static glCreateBuffersDelegate s_glCreateBuffers;
	public static void glCreateBuffers(int n, uint *buffers) => s_glCreateBuffers(n, buffers);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCreateFramebuffersDelegate(int n, uint *framebuffers);
	private static glCreateFramebuffersDelegate s_glCreateFramebuffers;
	public static void glCreateFramebuffers(int n, uint *framebuffers) => s_glCreateFramebuffers(n, framebuffers);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate uint glCreateProgramDelegate();
	private static glCreateProgramDelegate s_glCreateProgram;
	public static uint glCreateProgram() => s_glCreateProgram();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCreateProgramPipelinesDelegate(int n, uint *pipelines);
	private static glCreateProgramPipelinesDelegate s_glCreateProgramPipelines;
	public static void glCreateProgramPipelines(int n, uint *pipelines) => s_glCreateProgramPipelines(n, pipelines);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCreateQueriesDelegate(int target, int n, uint *ids);
	private static glCreateQueriesDelegate s_glCreateQueries;
	public static void glCreateQueries(int target, int n, uint *ids) => s_glCreateQueries(target, n, ids);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCreateRenderbuffersDelegate(int n, uint *renderbuffers);
	private static glCreateRenderbuffersDelegate s_glCreateRenderbuffers;
	public static void glCreateRenderbuffers(int n, uint *renderbuffers) => s_glCreateRenderbuffers(n, renderbuffers);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCreateSamplersDelegate(int n, uint *samplers);
	private static glCreateSamplersDelegate s_glCreateSamplers;
	public static void glCreateSamplers(int n, uint *samplers) => s_glCreateSamplers(n, samplers);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate uint glCreateShaderDelegate(int type);
	private static glCreateShaderDelegate s_glCreateShader;
	public static uint glCreateShader(int type) => s_glCreateShader(type);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate uint glCreateShaderProgramvDelegate(int type, int count, char **strs);
	private static glCreateShaderProgramvDelegate s_glCreateShaderProgramv;
	public static uint glCreateShaderProgramv(int type, int count, char **strs) => s_glCreateShaderProgramv(type, count, strs);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCreateTexturesDelegate(int target, int n, uint *textures);
	private static glCreateTexturesDelegate s_glCreateTextures;
	public static void glCreateTextures(int target, int n, uint *textures) => s_glCreateTextures(target, n, textures);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCreateTransformFeedbacksDelegate(int n, uint *ids);
	private static glCreateTransformFeedbacksDelegate s_glCreateTransformFeedbacks;
	public static void glCreateTransformFeedbacks(int n, uint *ids) => s_glCreateTransformFeedbacks(n, ids);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCreateVertexArraysDelegate(int n, uint *arrays);
	private static glCreateVertexArraysDelegate s_glCreateVertexArrays;
	public static void glCreateVertexArrays(int n, uint *arrays) => s_glCreateVertexArrays(n, arrays);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glCullFaceDelegate(int mode);
	private static glCullFaceDelegate s_glCullFace;
	public static void glCullFace(int mode) => s_glCullFace(mode);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDebugMessageCallbackDelegate(IntPtr callback, void *userParam);
	private static glDebugMessageCallbackDelegate s_glDebugMessageCallback;
	public static void glDebugMessageCallback(IntPtr callback, void *userParam) => s_glDebugMessageCallback(callback, userParam);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDebugMessageControlDelegate(int source, int type, int severity, int count, uint *ids, bool enabled);
	private static glDebugMessageControlDelegate s_glDebugMessageControl;
	public static void glDebugMessageControl(int source, int type, int severity, int count, uint *ids, bool enabled) => s_glDebugMessageControl(source, type, severity, count, ids, enabled);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDebugMessageInsertDelegate(int source, int type, uint id, int severity, int length, char *buf);
	private static glDebugMessageInsertDelegate s_glDebugMessageInsert;
	public static void glDebugMessageInsert(int source, int type, uint id, int severity, int length, char *buf) => s_glDebugMessageInsert(source, type, id, severity, length, buf);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDeleteBuffersDelegate(int n, uint *buffers);
	private static glDeleteBuffersDelegate s_glDeleteBuffers;
	public static void glDeleteBuffers(int n, uint *buffers) => s_glDeleteBuffers(n, buffers);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDeleteFramebuffersDelegate(int n, uint *framebuffers);
	private static glDeleteFramebuffersDelegate s_glDeleteFramebuffers;
	public static void glDeleteFramebuffers(int n, uint *framebuffers) => s_glDeleteFramebuffers(n, framebuffers);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDeleteProgramDelegate(uint program);
	private static glDeleteProgramDelegate s_glDeleteProgram;
	public static void glDeleteProgram(uint program) => s_glDeleteProgram(program);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDeleteProgramPipelinesDelegate(int n, uint *pipelines);
	private static glDeleteProgramPipelinesDelegate s_glDeleteProgramPipelines;
	public static void glDeleteProgramPipelines(int n, uint *pipelines) => s_glDeleteProgramPipelines(n, pipelines);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDeleteQueriesDelegate(int n, uint *ids);
	private static glDeleteQueriesDelegate s_glDeleteQueries;
	public static void glDeleteQueries(int n, uint *ids) => s_glDeleteQueries(n, ids);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDeleteRenderbuffersDelegate(int n, uint *renderbuffers);
	private static glDeleteRenderbuffersDelegate s_glDeleteRenderbuffers;
	public static void glDeleteRenderbuffers(int n, uint *renderbuffers) => s_glDeleteRenderbuffers(n, renderbuffers);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDeleteSamplersDelegate(int count, uint *samplers);
	private static glDeleteSamplersDelegate s_glDeleteSamplers;
	public static void glDeleteSamplers(int count, uint *samplers) => s_glDeleteSamplers(count, samplers);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDeleteShaderDelegate(uint shader);
	private static glDeleteShaderDelegate s_glDeleteShader;
	public static void glDeleteShader(uint shader) => s_glDeleteShader(shader);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDeleteSyncDelegate(IntPtr sync);
	private static glDeleteSyncDelegate s_glDeleteSync;
	public static void glDeleteSync(IntPtr sync) => s_glDeleteSync(sync);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDeleteTexturesDelegate(int n, uint *textures);
	private static glDeleteTexturesDelegate s_glDeleteTextures;
	public static void glDeleteTextures(int n, uint *textures) => s_glDeleteTextures(n, textures);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDeleteTransformFeedbacksDelegate(int n, uint *ids);
	private static glDeleteTransformFeedbacksDelegate s_glDeleteTransformFeedbacks;
	public static void glDeleteTransformFeedbacks(int n, uint *ids) => s_glDeleteTransformFeedbacks(n, ids);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDeleteVertexArraysDelegate(int n, uint *arrays);
	private static glDeleteVertexArraysDelegate s_glDeleteVertexArrays;
	public static void glDeleteVertexArrays(int n, uint *arrays) => s_glDeleteVertexArrays(n, arrays);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDepthFuncDelegate(int func);
	private static glDepthFuncDelegate s_glDepthFunc;
	public static void glDepthFunc(int func) => s_glDepthFunc(func);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDepthMaskDelegate(bool flag);
	private static glDepthMaskDelegate s_glDepthMask;
	public static void glDepthMask(bool flag) => s_glDepthMask(flag);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDepthRangeDelegate(double n, double f);
	private static glDepthRangeDelegate s_glDepthRange;
	public static void glDepthRange(double n, double f) => s_glDepthRange(n, f);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDepthRangeArrayvDelegate(uint first, int count, double *v);
	private static glDepthRangeArrayvDelegate s_glDepthRangeArrayv;
	public static void glDepthRangeArrayv(uint first, int count, double *v) => s_glDepthRangeArrayv(first, count, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDepthRangeIndexedDelegate(uint index, double n, double f);
	private static glDepthRangeIndexedDelegate s_glDepthRangeIndexed;
	public static void glDepthRangeIndexed(uint index, double n, double f) => s_glDepthRangeIndexed(index, n, f);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDepthRangefDelegate(float n, float f);
	private static glDepthRangefDelegate s_glDepthRangef;
	public static void glDepthRangef(float n, float f) => s_glDepthRangef(n, f);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDetachShaderDelegate(uint program, uint shader);
	private static glDetachShaderDelegate s_glDetachShader;
	public static void glDetachShader(uint program, uint shader) => s_glDetachShader(program, shader);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDisableDelegate(int cap);
	private static glDisableDelegate s_glDisable;
	public static void glDisable(int cap) => s_glDisable(cap);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDisableVertexArrayAttribDelegate(uint vaobj, uint index);
	private static glDisableVertexArrayAttribDelegate s_glDisableVertexArrayAttrib;
	public static void glDisableVertexArrayAttrib(uint vaobj, uint index) => s_glDisableVertexArrayAttrib(vaobj, index);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDisableVertexAttribArrayDelegate(uint index);
	private static glDisableVertexAttribArrayDelegate s_glDisableVertexAttribArray;
	public static void glDisableVertexAttribArray(uint index) => s_glDisableVertexAttribArray(index);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDisableiDelegate(int target, uint index);
	private static glDisableiDelegate s_glDisablei;
	public static void glDisablei(int target, uint index) => s_glDisablei(target, index);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDispatchComputeDelegate(uint num_groups_x, uint num_groups_y, uint num_groups_z);
	private static glDispatchComputeDelegate s_glDispatchCompute;
	public static void glDispatchCompute(uint num_groups_x, uint num_groups_y, uint num_groups_z) => s_glDispatchCompute(num_groups_x, num_groups_y, num_groups_z);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDispatchComputeIndirectDelegate(IntPtr indirect);
	private static glDispatchComputeIndirectDelegate s_glDispatchComputeIndirect;
	public static void glDispatchComputeIndirect(IntPtr indirect) => s_glDispatchComputeIndirect(indirect);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawArraysDelegate(int mode, int first, int count);
	private static glDrawArraysDelegate s_glDrawArrays;
	public static void glDrawArrays(int mode, int first, int count) => s_glDrawArrays(mode, first, count);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawArraysIndirectDelegate(int mode, void *indirect);
	private static glDrawArraysIndirectDelegate s_glDrawArraysIndirect;
	public static void glDrawArraysIndirect(int mode, void *indirect) => s_glDrawArraysIndirect(mode, indirect);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawArraysInstancedDelegate(int mode, int first, int count, int instancecount);
	private static glDrawArraysInstancedDelegate s_glDrawArraysInstanced;
	public static void glDrawArraysInstanced(int mode, int first, int count, int instancecount) => s_glDrawArraysInstanced(mode, first, count, instancecount);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawArraysInstancedBaseInstanceDelegate(int mode, int first, int count, int instancecount, uint baseinstance);
	private static glDrawArraysInstancedBaseInstanceDelegate s_glDrawArraysInstancedBaseInstance;
	public static void glDrawArraysInstancedBaseInstance(int mode, int first, int count, int instancecount, uint baseinstance) => s_glDrawArraysInstancedBaseInstance(mode, first, count, instancecount, baseinstance);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawBufferDelegate(int buf);
	private static glDrawBufferDelegate s_glDrawBuffer;
	public static void glDrawBuffer(int buf) => s_glDrawBuffer(buf);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawBuffersDelegate(int n, int *bufs);
	private static glDrawBuffersDelegate s_glDrawBuffers;
	public static void glDrawBuffers(int n, int *bufs) => s_glDrawBuffers(n, bufs);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawElementsDelegate(int mode, int count, int type, void *indices);
	private static glDrawElementsDelegate s_glDrawElements;
	public static void glDrawElements(int mode, int count, int type, void *indices) => s_glDrawElements(mode, count, type, indices);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawElementsBaseVertexDelegate(int mode, int count, int type, void *indices, int basevertex);
	private static glDrawElementsBaseVertexDelegate s_glDrawElementsBaseVertex;
	public static void glDrawElementsBaseVertex(int mode, int count, int type, void *indices, int basevertex) => s_glDrawElementsBaseVertex(mode, count, type, indices, basevertex);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawElementsIndirectDelegate(int mode, int type, void *indirect);
	private static glDrawElementsIndirectDelegate s_glDrawElementsIndirect;
	public static void glDrawElementsIndirect(int mode, int type, void *indirect) => s_glDrawElementsIndirect(mode, type, indirect);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawElementsInstancedDelegate(int mode, int count, int type, void *indices, int instancecount);
	private static glDrawElementsInstancedDelegate s_glDrawElementsInstanced;
	public static void glDrawElementsInstanced(int mode, int count, int type, void *indices, int instancecount) => s_glDrawElementsInstanced(mode, count, type, indices, instancecount);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawElementsInstancedBaseInstanceDelegate(int mode, int count, int type, void *indices, int instancecount, uint baseinstance);
	private static glDrawElementsInstancedBaseInstanceDelegate s_glDrawElementsInstancedBaseInstance;
	public static void glDrawElementsInstancedBaseInstance(int mode, int count, int type, void *indices, int instancecount, uint baseinstance) => s_glDrawElementsInstancedBaseInstance(mode, count, type, indices, instancecount, baseinstance);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawElementsInstancedBaseVertexDelegate(int mode, int count, int type, void *indices, int instancecount, int basevertex);
	private static glDrawElementsInstancedBaseVertexDelegate s_glDrawElementsInstancedBaseVertex;
	public static void glDrawElementsInstancedBaseVertex(int mode, int count, int type, void *indices, int instancecount, int basevertex) => s_glDrawElementsInstancedBaseVertex(mode, count, type, indices, instancecount, basevertex);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawElementsInstancedBaseVertexBaseInstanceDelegate(int mode, int count, int type, void *indices, int instancecount, int basevertex, uint baseinstance);
	private static glDrawElementsInstancedBaseVertexBaseInstanceDelegate s_glDrawElementsInstancedBaseVertexBaseInstance;
	public static void glDrawElementsInstancedBaseVertexBaseInstance(int mode, int count, int type, void *indices, int instancecount, int basevertex, uint baseinstance) => s_glDrawElementsInstancedBaseVertexBaseInstance(mode, count, type, indices, instancecount, basevertex, baseinstance);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawRangeElementsDelegate(int mode, uint start, uint end, int count, int type, void *indices);
	private static glDrawRangeElementsDelegate s_glDrawRangeElements;
	public static void glDrawRangeElements(int mode, uint start, uint end, int count, int type, void *indices) => s_glDrawRangeElements(mode, start, end, count, type, indices);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawRangeElementsBaseVertexDelegate(int mode, uint start, uint end, int count, int type, void *indices, int basevertex);
	private static glDrawRangeElementsBaseVertexDelegate s_glDrawRangeElementsBaseVertex;
	public static void glDrawRangeElementsBaseVertex(int mode, uint start, uint end, int count, int type, void *indices, int basevertex) => s_glDrawRangeElementsBaseVertex(mode, start, end, count, type, indices, basevertex);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawTransformFeedbackDelegate(int mode, uint id);
	private static glDrawTransformFeedbackDelegate s_glDrawTransformFeedback;
	public static void glDrawTransformFeedback(int mode, uint id) => s_glDrawTransformFeedback(mode, id);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawTransformFeedbackInstancedDelegate(int mode, uint id, int instancecount);
	private static glDrawTransformFeedbackInstancedDelegate s_glDrawTransformFeedbackInstanced;
	public static void glDrawTransformFeedbackInstanced(int mode, uint id, int instancecount) => s_glDrawTransformFeedbackInstanced(mode, id, instancecount);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawTransformFeedbackStreamDelegate(int mode, uint id, uint stream);
	private static glDrawTransformFeedbackStreamDelegate s_glDrawTransformFeedbackStream;
	public static void glDrawTransformFeedbackStream(int mode, uint id, uint stream) => s_glDrawTransformFeedbackStream(mode, id, stream);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glDrawTransformFeedbackStreamInstancedDelegate(int mode, uint id, uint stream, int instancecount);
	private static glDrawTransformFeedbackStreamInstancedDelegate s_glDrawTransformFeedbackStreamInstanced;
	public static void glDrawTransformFeedbackStreamInstanced(int mode, uint id, uint stream, int instancecount) => s_glDrawTransformFeedbackStreamInstanced(mode, id, stream, instancecount);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glEnableDelegate(int cap);
	private static glEnableDelegate s_glEnable;
	public static void glEnable(int cap) => s_glEnable(cap);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glEnableVertexArrayAttribDelegate(uint vaobj, uint index);
	private static glEnableVertexArrayAttribDelegate s_glEnableVertexArrayAttrib;
	public static void glEnableVertexArrayAttrib(uint vaobj, uint index) => s_glEnableVertexArrayAttrib(vaobj, index);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glEnableVertexAttribArrayDelegate(uint index);
	private static glEnableVertexAttribArrayDelegate s_glEnableVertexAttribArray;
	public static void glEnableVertexAttribArray(uint index) => s_glEnableVertexAttribArray(index);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glEnableiDelegate(int target, uint index);
	private static glEnableiDelegate s_glEnablei;
	public static void glEnablei(int target, uint index) => s_glEnablei(target, index);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glEndConditionalRenderDelegate();
	private static glEndConditionalRenderDelegate s_glEndConditionalRender;
	public static void glEndConditionalRender() => s_glEndConditionalRender();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glEndQueryDelegate(int target);
	private static glEndQueryDelegate s_glEndQuery;
	public static void glEndQuery(int target) => s_glEndQuery(target);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glEndQueryIndexedDelegate(int target, uint index);
	private static glEndQueryIndexedDelegate s_glEndQueryIndexed;
	public static void glEndQueryIndexed(int target, uint index) => s_glEndQueryIndexed(target, index);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glEndTransformFeedbackDelegate();
	private static glEndTransformFeedbackDelegate s_glEndTransformFeedback;
	public static void glEndTransformFeedback() => s_glEndTransformFeedback();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate IntPtr glFenceSyncDelegate(int condition, int flags);
	private static glFenceSyncDelegate s_glFenceSync;
	public static IntPtr glFenceSync(int condition, int flags) => s_glFenceSync(condition, flags);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glFinishDelegate();
	private static glFinishDelegate s_glFinish;
	public static void glFinish() => s_glFinish();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glFlushDelegate();
	private static glFlushDelegate s_glFlush;
	public static void glFlush() => s_glFlush();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glFlushMappedBufferRangeDelegate(int target, IntPtr offset, IntPtr length);
	private static glFlushMappedBufferRangeDelegate s_glFlushMappedBufferRange;
	public static void glFlushMappedBufferRange(int target, IntPtr offset, IntPtr length) => s_glFlushMappedBufferRange(target, offset, length);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glFlushMappedNamedBufferRangeDelegate(uint buffer, IntPtr offset, IntPtr length);
	private static glFlushMappedNamedBufferRangeDelegate s_glFlushMappedNamedBufferRange;
	public static void glFlushMappedNamedBufferRange(uint buffer, IntPtr offset, IntPtr length) => s_glFlushMappedNamedBufferRange(buffer, offset, length);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glFramebufferParameteriDelegate(int target, int pname, int param);
	private static glFramebufferParameteriDelegate s_glFramebufferParameteri;
	public static void glFramebufferParameteri(int target, int pname, int param) => s_glFramebufferParameteri(target, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glFramebufferRenderbufferDelegate(int target, int attachment, int renderbuffertarget, uint renderbuffer);
	private static glFramebufferRenderbufferDelegate s_glFramebufferRenderbuffer;
	public static void glFramebufferRenderbuffer(int target, int attachment, int renderbuffertarget, uint renderbuffer) => s_glFramebufferRenderbuffer(target, attachment, renderbuffertarget, renderbuffer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glFramebufferTextureDelegate(int target, int attachment, uint texture, int level);
	private static glFramebufferTextureDelegate s_glFramebufferTexture;
	public static void glFramebufferTexture(int target, int attachment, uint texture, int level) => s_glFramebufferTexture(target, attachment, texture, level);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glFramebufferTexture1DDelegate(int target, int attachment, int textarget, uint texture, int level);
	private static glFramebufferTexture1DDelegate s_glFramebufferTexture1D;
	public static void glFramebufferTexture1D(int target, int attachment, int textarget, uint texture, int level) => s_glFramebufferTexture1D(target, attachment, textarget, texture, level);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glFramebufferTexture2DDelegate(int target, int attachment, int textarget, uint texture, int level);
	private static glFramebufferTexture2DDelegate s_glFramebufferTexture2D;
	public static void glFramebufferTexture2D(int target, int attachment, int textarget, uint texture, int level) => s_glFramebufferTexture2D(target, attachment, textarget, texture, level);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glFramebufferTexture3DDelegate(int target, int attachment, int textarget, uint texture, int level, int zoffset);
	private static glFramebufferTexture3DDelegate s_glFramebufferTexture3D;
	public static void glFramebufferTexture3D(int target, int attachment, int textarget, uint texture, int level, int zoffset) => s_glFramebufferTexture3D(target, attachment, textarget, texture, level, zoffset);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glFramebufferTextureLayerDelegate(int target, int attachment, uint texture, int level, int layer);
	private static glFramebufferTextureLayerDelegate s_glFramebufferTextureLayer;
	public static void glFramebufferTextureLayer(int target, int attachment, uint texture, int level, int layer) => s_glFramebufferTextureLayer(target, attachment, texture, level, layer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glFrontFaceDelegate(int mode);
	private static glFrontFaceDelegate s_glFrontFace;
	public static void glFrontFace(int mode) => s_glFrontFace(mode);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGenBuffersDelegate(int n, uint *buffers);
	private static glGenBuffersDelegate s_glGenBuffers;
	public static void glGenBuffers(int n, uint *buffers) => s_glGenBuffers(n, buffers);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGenFramebuffersDelegate(int n, uint *framebuffers);
	private static glGenFramebuffersDelegate s_glGenFramebuffers;
	public static void glGenFramebuffers(int n, uint *framebuffers) => s_glGenFramebuffers(n, framebuffers);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGenProgramPipelinesDelegate(int n, uint *pipelines);
	private static glGenProgramPipelinesDelegate s_glGenProgramPipelines;
	public static void glGenProgramPipelines(int n, uint *pipelines) => s_glGenProgramPipelines(n, pipelines);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGenQueriesDelegate(int n, uint *ids);
	private static glGenQueriesDelegate s_glGenQueries;
	public static void glGenQueries(int n, uint *ids) => s_glGenQueries(n, ids);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGenRenderbuffersDelegate(int n, uint *renderbuffers);
	private static glGenRenderbuffersDelegate s_glGenRenderbuffers;
	public static void glGenRenderbuffers(int n, uint *renderbuffers) => s_glGenRenderbuffers(n, renderbuffers);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGenSamplersDelegate(int count, uint *samplers);
	private static glGenSamplersDelegate s_glGenSamplers;
	public static void glGenSamplers(int count, uint *samplers) => s_glGenSamplers(count, samplers);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGenTexturesDelegate(int n, uint *textures);
	private static glGenTexturesDelegate s_glGenTextures;
	public static void glGenTextures(int n, uint *textures) => s_glGenTextures(n, textures);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGenTransformFeedbacksDelegate(int n, uint *ids);
	private static glGenTransformFeedbacksDelegate s_glGenTransformFeedbacks;
	public static void glGenTransformFeedbacks(int n, uint *ids) => s_glGenTransformFeedbacks(n, ids);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGenVertexArraysDelegate(int n, uint *arrays);
	private static glGenVertexArraysDelegate s_glGenVertexArrays;
	public static void glGenVertexArrays(int n, uint *arrays) => s_glGenVertexArrays(n, arrays);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGenerateMipmapDelegate(int target);
	private static glGenerateMipmapDelegate s_glGenerateMipmap;
	public static void glGenerateMipmap(int target) => s_glGenerateMipmap(target);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGenerateTextureMipmapDelegate(uint texture);
	private static glGenerateTextureMipmapDelegate s_glGenerateTextureMipmap;
	public static void glGenerateTextureMipmap(uint texture) => s_glGenerateTextureMipmap(texture);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetActiveAtomicCounterBufferivDelegate(uint program, uint bufferIndex, int pname, int *args);
	private static glGetActiveAtomicCounterBufferivDelegate s_glGetActiveAtomicCounterBufferiv;
	public static void glGetActiveAtomicCounterBufferiv(uint program, uint bufferIndex, int pname, int *args) => s_glGetActiveAtomicCounterBufferiv(program, bufferIndex, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetActiveAttribDelegate(uint program, uint index, int bufSize, int *length, int *size, int *type, char *name);
	private static glGetActiveAttribDelegate s_glGetActiveAttrib;
	public static void glGetActiveAttrib(uint program, uint index, int bufSize, int *length, int *size, int *type, char *name) => s_glGetActiveAttrib(program, index, bufSize, length, size, type, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetActiveSubroutineNameDelegate(uint program, int shadertype, uint index, int bufSize, int *length, char *name);
	private static glGetActiveSubroutineNameDelegate s_glGetActiveSubroutineName;
	public static void glGetActiveSubroutineName(uint program, int shadertype, uint index, int bufSize, int *length, char *name) => s_glGetActiveSubroutineName(program, shadertype, index, bufSize, length, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetActiveSubroutineUniformNameDelegate(uint program, int shadertype, uint index, int bufSize, int *length, char *name);
	private static glGetActiveSubroutineUniformNameDelegate s_glGetActiveSubroutineUniformName;
	public static void glGetActiveSubroutineUniformName(uint program, int shadertype, uint index, int bufSize, int *length, char *name) => s_glGetActiveSubroutineUniformName(program, shadertype, index, bufSize, length, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetActiveSubroutineUniformivDelegate(uint program, int shadertype, uint index, int pname, int *values);
	private static glGetActiveSubroutineUniformivDelegate s_glGetActiveSubroutineUniformiv;
	public static void glGetActiveSubroutineUniformiv(uint program, int shadertype, uint index, int pname, int *values) => s_glGetActiveSubroutineUniformiv(program, shadertype, index, pname, values);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetActiveUniformDelegate(uint program, uint index, int bufSize, int *length, int *size, int *type, char *name);
	private static glGetActiveUniformDelegate s_glGetActiveUniform;
	public static void glGetActiveUniform(uint program, uint index, int bufSize, int *length, int *size, int *type, char *name) => s_glGetActiveUniform(program, index, bufSize, length, size, type, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetActiveUniformBlockNameDelegate(uint program, uint uniformBlockIndex, int bufSize, int *length, char *uniformBlockName);
	private static glGetActiveUniformBlockNameDelegate s_glGetActiveUniformBlockName;
	public static void glGetActiveUniformBlockName(uint program, uint uniformBlockIndex, int bufSize, int *length, char *uniformBlockName) => s_glGetActiveUniformBlockName(program, uniformBlockIndex, bufSize, length, uniformBlockName);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetActiveUniformBlockivDelegate(uint program, uint uniformBlockIndex, int pname, int *args);
	private static glGetActiveUniformBlockivDelegate s_glGetActiveUniformBlockiv;
	public static void glGetActiveUniformBlockiv(uint program, uint uniformBlockIndex, int pname, int *args) => s_glGetActiveUniformBlockiv(program, uniformBlockIndex, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetActiveUniformNameDelegate(uint program, uint uniformIndex, int bufSize, int *length, char *uniformName);
	private static glGetActiveUniformNameDelegate s_glGetActiveUniformName;
	public static void glGetActiveUniformName(uint program, uint uniformIndex, int bufSize, int *length, char *uniformName) => s_glGetActiveUniformName(program, uniformIndex, bufSize, length, uniformName);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetActiveUniformsivDelegate(uint program, int uniformCount, uint *uniformIndices, int pname, int *args);
	private static glGetActiveUniformsivDelegate s_glGetActiveUniformsiv;
	public static void glGetActiveUniformsiv(uint program, int uniformCount, uint *uniformIndices, int pname, int *args) => s_glGetActiveUniformsiv(program, uniformCount, uniformIndices, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetAttachedShadersDelegate(uint program, int maxCount, int *count, uint *shaders);
	private static glGetAttachedShadersDelegate s_glGetAttachedShaders;
	public static void glGetAttachedShaders(uint program, int maxCount, int *count, uint *shaders) => s_glGetAttachedShaders(program, maxCount, count, shaders);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int glGetAttribLocationDelegate(uint program, char *name);
	private static glGetAttribLocationDelegate s_glGetAttribLocation;
	public static int glGetAttribLocation(uint program, char *name) => s_glGetAttribLocation(program, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetBooleani_vDelegate(int target, uint index, bool *data);
	private static glGetBooleani_vDelegate s_glGetBooleani_v;
	public static void glGetBooleani_v(int target, uint index, bool *data) => s_glGetBooleani_v(target, index, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetBooleanvDelegate(int pname, bool *data);
	private static glGetBooleanvDelegate s_glGetBooleanv;
	public static void glGetBooleanv(int pname, bool *data) => s_glGetBooleanv(pname, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetBufferParameteri64vDelegate(int target, int pname, long *args);
	private static glGetBufferParameteri64vDelegate s_glGetBufferParameteri64v;
	public static void glGetBufferParameteri64v(int target, int pname, long *args) => s_glGetBufferParameteri64v(target, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetBufferParameterivDelegate(int target, int pname, int *args);
	private static glGetBufferParameterivDelegate s_glGetBufferParameteriv;
	public static void glGetBufferParameteriv(int target, int pname, int *args) => s_glGetBufferParameteriv(target, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetBufferPointervDelegate(int target, int pname, void **args);
	private static glGetBufferPointervDelegate s_glGetBufferPointerv;
	public static void glGetBufferPointerv(int target, int pname, void **args) => s_glGetBufferPointerv(target, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetBufferSubDataDelegate(int target, IntPtr offset, IntPtr size, void *data);
	private static glGetBufferSubDataDelegate s_glGetBufferSubData;
	public static void glGetBufferSubData(int target, IntPtr offset, IntPtr size, void *data) => s_glGetBufferSubData(target, offset, size, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetCompressedTexImageDelegate(int target, int level, void *img);
	private static glGetCompressedTexImageDelegate s_glGetCompressedTexImage;
	public static void glGetCompressedTexImage(int target, int level, void *img) => s_glGetCompressedTexImage(target, level, img);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetCompressedTextureImageDelegate(uint texture, int level, int bufSize, void *pixels);
	private static glGetCompressedTextureImageDelegate s_glGetCompressedTextureImage;
	public static void glGetCompressedTextureImage(uint texture, int level, int bufSize, void *pixels) => s_glGetCompressedTextureImage(texture, level, bufSize, pixels);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetCompressedTextureSubImageDelegate(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int bufSize, void *pixels);
	private static glGetCompressedTextureSubImageDelegate s_glGetCompressedTextureSubImage;
	public static void glGetCompressedTextureSubImage(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int bufSize, void *pixels) => s_glGetCompressedTextureSubImage(texture, level, xoffset, yoffset, zoffset, width, height, depth, bufSize, pixels);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate uint glGetDebugMessageLogDelegate(uint count, int bufSize, int *sources, int *types, uint *ids, int *severities, int *lengths, char *messageLog);
	private static glGetDebugMessageLogDelegate s_glGetDebugMessageLog;
	public static uint glGetDebugMessageLog(uint count, int bufSize, int *sources, int *types, uint *ids, int *severities, int *lengths, char *messageLog) => s_glGetDebugMessageLog(count, bufSize, sources, types, ids, severities, lengths, messageLog);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetDoublei_vDelegate(int target, uint index, double *data);
	private static glGetDoublei_vDelegate s_glGetDoublei_v;
	public static void glGetDoublei_v(int target, uint index, double *data) => s_glGetDoublei_v(target, index, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetDoublevDelegate(int pname, double *data);
	private static glGetDoublevDelegate s_glGetDoublev;
	public static void glGetDoublev(int pname, double *data) => s_glGetDoublev(pname, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int glGetErrorDelegate();
	private static glGetErrorDelegate s_glGetError;
	public static int glGetError() => s_glGetError();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetFloati_vDelegate(int target, uint index, float *data);
	private static glGetFloati_vDelegate s_glGetFloati_v;
	public static void glGetFloati_v(int target, uint index, float *data) => s_glGetFloati_v(target, index, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetFloatvDelegate(int pname, float *data);
	private static glGetFloatvDelegate s_glGetFloatv;
	public static void glGetFloatv(int pname, float *data) => s_glGetFloatv(pname, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int glGetFragDataIndexDelegate(uint program, char *name);
	private static glGetFragDataIndexDelegate s_glGetFragDataIndex;
	public static int glGetFragDataIndex(uint program, char *name) => s_glGetFragDataIndex(program, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int glGetFragDataLocationDelegate(uint program, char *name);
	private static glGetFragDataLocationDelegate s_glGetFragDataLocation;
	public static int glGetFragDataLocation(uint program, char *name) => s_glGetFragDataLocation(program, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetFramebufferAttachmentParameterivDelegate(int target, int attachment, int pname, int *args);
	private static glGetFramebufferAttachmentParameterivDelegate s_glGetFramebufferAttachmentParameteriv;
	public static void glGetFramebufferAttachmentParameteriv(int target, int attachment, int pname, int *args) => s_glGetFramebufferAttachmentParameteriv(target, attachment, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetFramebufferParameterivDelegate(int target, int pname, int *args);
	private static glGetFramebufferParameterivDelegate s_glGetFramebufferParameteriv;
	public static void glGetFramebufferParameteriv(int target, int pname, int *args) => s_glGetFramebufferParameteriv(target, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int glGetGraphicsResetStatusDelegate();
	private static glGetGraphicsResetStatusDelegate s_glGetGraphicsResetStatus;
	public static int glGetGraphicsResetStatus() => s_glGetGraphicsResetStatus();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetInteger64i_vDelegate(int target, uint index, long *data);
	private static glGetInteger64i_vDelegate s_glGetInteger64i_v;
	public static void glGetInteger64i_v(int target, uint index, long *data) => s_glGetInteger64i_v(target, index, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetInteger64vDelegate(int pname, long *data);
	private static glGetInteger64vDelegate s_glGetInteger64v;
	public static void glGetInteger64v(int pname, long *data) => s_glGetInteger64v(pname, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetIntegeri_vDelegate(int target, uint index, int *data);
	private static glGetIntegeri_vDelegate s_glGetIntegeri_v;
	public static void glGetIntegeri_v(int target, uint index, int *data) => s_glGetIntegeri_v(target, index, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetIntegervDelegate(int pname, int *data);
	private static glGetIntegervDelegate s_glGetIntegerv;
	public static void glGetIntegerv(int pname, int *data) => s_glGetIntegerv(pname, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetInternalformati64vDelegate(int target, int internalformat, int pname, int count, long *args);
	private static glGetInternalformati64vDelegate s_glGetInternalformati64v;
	public static void glGetInternalformati64v(int target, int internalformat, int pname, int count, long *args) => s_glGetInternalformati64v(target, internalformat, pname, count, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetInternalformativDelegate(int target, int internalformat, int pname, int count, int *args);
	private static glGetInternalformativDelegate s_glGetInternalformativ;
	public static void glGetInternalformativ(int target, int internalformat, int pname, int count, int *args) => s_glGetInternalformativ(target, internalformat, pname, count, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetMultisamplefvDelegate(int pname, uint index, float *val);
	private static glGetMultisamplefvDelegate s_glGetMultisamplefv;
	public static void glGetMultisamplefv(int pname, uint index, float *val) => s_glGetMultisamplefv(pname, index, val);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetNamedBufferParameteri64vDelegate(uint buffer, int pname, long *args);
	private static glGetNamedBufferParameteri64vDelegate s_glGetNamedBufferParameteri64v;
	public static void glGetNamedBufferParameteri64v(uint buffer, int pname, long *args) => s_glGetNamedBufferParameteri64v(buffer, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetNamedBufferParameterivDelegate(uint buffer, int pname, int *args);
	private static glGetNamedBufferParameterivDelegate s_glGetNamedBufferParameteriv;
	public static void glGetNamedBufferParameteriv(uint buffer, int pname, int *args) => s_glGetNamedBufferParameteriv(buffer, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetNamedBufferPointervDelegate(uint buffer, int pname, void **args);
	private static glGetNamedBufferPointervDelegate s_glGetNamedBufferPointerv;
	public static void glGetNamedBufferPointerv(uint buffer, int pname, void **args) => s_glGetNamedBufferPointerv(buffer, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetNamedBufferSubDataDelegate(uint buffer, IntPtr offset, IntPtr size, void *data);
	private static glGetNamedBufferSubDataDelegate s_glGetNamedBufferSubData;
	public static void glGetNamedBufferSubData(uint buffer, IntPtr offset, IntPtr size, void *data) => s_glGetNamedBufferSubData(buffer, offset, size, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetNamedFramebufferAttachmentParameterivDelegate(uint framebuffer, int attachment, int pname, int *args);
	private static glGetNamedFramebufferAttachmentParameterivDelegate s_glGetNamedFramebufferAttachmentParameteriv;
	public static void glGetNamedFramebufferAttachmentParameteriv(uint framebuffer, int attachment, int pname, int *args) => s_glGetNamedFramebufferAttachmentParameteriv(framebuffer, attachment, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetNamedFramebufferParameterivDelegate(uint framebuffer, int pname, int *param);
	private static glGetNamedFramebufferParameterivDelegate s_glGetNamedFramebufferParameteriv;
	public static void glGetNamedFramebufferParameteriv(uint framebuffer, int pname, int *param) => s_glGetNamedFramebufferParameteriv(framebuffer, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetNamedRenderbufferParameterivDelegate(uint renderbuffer, int pname, int *args);
	private static glGetNamedRenderbufferParameterivDelegate s_glGetNamedRenderbufferParameteriv;
	public static void glGetNamedRenderbufferParameteriv(uint renderbuffer, int pname, int *args) => s_glGetNamedRenderbufferParameteriv(renderbuffer, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetObjectLabelDelegate(int identifier, uint name, int bufSize, int *length, char *label);
	private static glGetObjectLabelDelegate s_glGetObjectLabel;
	public static void glGetObjectLabel(int identifier, uint name, int bufSize, int *length, char *label) => s_glGetObjectLabel(identifier, name, bufSize, length, label);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetObjectPtrLabelDelegate(void *ptr, int bufSize, int *length, char *label);
	private static glGetObjectPtrLabelDelegate s_glGetObjectPtrLabel;
	public static void glGetObjectPtrLabel(void *ptr, int bufSize, int *length, char *label) => s_glGetObjectPtrLabel(ptr, bufSize, length, label);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetProgramBinaryDelegate(uint program, int bufSize, int *length, int *binaryFormat, void *binary);
	private static glGetProgramBinaryDelegate s_glGetProgramBinary;
	public static void glGetProgramBinary(uint program, int bufSize, int *length, int *binaryFormat, void *binary) => s_glGetProgramBinary(program, bufSize, length, binaryFormat, binary);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetProgramInfoLogDelegate(uint program, int bufSize, int *length, char *infoLog);
	private static glGetProgramInfoLogDelegate s_glGetProgramInfoLog;
	public static void glGetProgramInfoLog(uint program, int bufSize, int *length, char *infoLog) => s_glGetProgramInfoLog(program, bufSize, length, infoLog);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetProgramInterfaceivDelegate(uint program, int programInterface, int pname, int *args);
	private static glGetProgramInterfaceivDelegate s_glGetProgramInterfaceiv;
	public static void glGetProgramInterfaceiv(uint program, int programInterface, int pname, int *args) => s_glGetProgramInterfaceiv(program, programInterface, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetProgramPipelineInfoLogDelegate(uint pipeline, int bufSize, int *length, char *infoLog);
	private static glGetProgramPipelineInfoLogDelegate s_glGetProgramPipelineInfoLog;
	public static void glGetProgramPipelineInfoLog(uint pipeline, int bufSize, int *length, char *infoLog) => s_glGetProgramPipelineInfoLog(pipeline, bufSize, length, infoLog);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetProgramPipelineivDelegate(uint pipeline, int pname, int *args);
	private static glGetProgramPipelineivDelegate s_glGetProgramPipelineiv;
	public static void glGetProgramPipelineiv(uint pipeline, int pname, int *args) => s_glGetProgramPipelineiv(pipeline, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate uint glGetProgramResourceIndexDelegate(uint program, int programInterface, char *name);
	private static glGetProgramResourceIndexDelegate s_glGetProgramResourceIndex;
	public static uint glGetProgramResourceIndex(uint program, int programInterface, char *name) => s_glGetProgramResourceIndex(program, programInterface, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int glGetProgramResourceLocationDelegate(uint program, int programInterface, char *name);
	private static glGetProgramResourceLocationDelegate s_glGetProgramResourceLocation;
	public static int glGetProgramResourceLocation(uint program, int programInterface, char *name) => s_glGetProgramResourceLocation(program, programInterface, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int glGetProgramResourceLocationIndexDelegate(uint program, int programInterface, char *name);
	private static glGetProgramResourceLocationIndexDelegate s_glGetProgramResourceLocationIndex;
	public static int glGetProgramResourceLocationIndex(uint program, int programInterface, char *name) => s_glGetProgramResourceLocationIndex(program, programInterface, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetProgramResourceNameDelegate(uint program, int programInterface, uint index, int bufSize, int *length, char *name);
	private static glGetProgramResourceNameDelegate s_glGetProgramResourceName;
	public static void glGetProgramResourceName(uint program, int programInterface, uint index, int bufSize, int *length, char *name) => s_glGetProgramResourceName(program, programInterface, index, bufSize, length, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetProgramResourceivDelegate(uint program, int programInterface, uint index, int propCount, int *props, int count, int *length, int *args);
	private static glGetProgramResourceivDelegate s_glGetProgramResourceiv;
	public static void glGetProgramResourceiv(uint program, int programInterface, uint index, int propCount, int *props, int count, int *length, int *args) => s_glGetProgramResourceiv(program, programInterface, index, propCount, props, count, length, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetProgramStageivDelegate(uint program, int shadertype, int pname, int *values);
	private static glGetProgramStageivDelegate s_glGetProgramStageiv;
	public static void glGetProgramStageiv(uint program, int shadertype, int pname, int *values) => s_glGetProgramStageiv(program, shadertype, pname, values);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetProgramivDelegate(uint program, int pname, int *args);
	private static glGetProgramivDelegate s_glGetProgramiv;
	public static void glGetProgramiv(uint program, int pname, int *args) => s_glGetProgramiv(program, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetQueryBufferObjecti64vDelegate(uint id, uint buffer, int pname, IntPtr offset);
	private static glGetQueryBufferObjecti64vDelegate s_glGetQueryBufferObjecti64v;
	public static void glGetQueryBufferObjecti64v(uint id, uint buffer, int pname, IntPtr offset) => s_glGetQueryBufferObjecti64v(id, buffer, pname, offset);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetQueryBufferObjectivDelegate(uint id, uint buffer, int pname, IntPtr offset);
	private static glGetQueryBufferObjectivDelegate s_glGetQueryBufferObjectiv;
	public static void glGetQueryBufferObjectiv(uint id, uint buffer, int pname, IntPtr offset) => s_glGetQueryBufferObjectiv(id, buffer, pname, offset);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetQueryBufferObjectui64vDelegate(uint id, uint buffer, int pname, IntPtr offset);
	private static glGetQueryBufferObjectui64vDelegate s_glGetQueryBufferObjectui64v;
	public static void glGetQueryBufferObjectui64v(uint id, uint buffer, int pname, IntPtr offset) => s_glGetQueryBufferObjectui64v(id, buffer, pname, offset);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetQueryBufferObjectuivDelegate(uint id, uint buffer, int pname, IntPtr offset);
	private static glGetQueryBufferObjectuivDelegate s_glGetQueryBufferObjectuiv;
	public static void glGetQueryBufferObjectuiv(uint id, uint buffer, int pname, IntPtr offset) => s_glGetQueryBufferObjectuiv(id, buffer, pname, offset);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetQueryIndexedivDelegate(int target, uint index, int pname, int *args);
	private static glGetQueryIndexedivDelegate s_glGetQueryIndexediv;
	public static void glGetQueryIndexediv(int target, uint index, int pname, int *args) => s_glGetQueryIndexediv(target, index, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetQueryObjecti64vDelegate(uint id, int pname, long *args);
	private static glGetQueryObjecti64vDelegate s_glGetQueryObjecti64v;
	public static void glGetQueryObjecti64v(uint id, int pname, long *args) => s_glGetQueryObjecti64v(id, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetQueryObjectivDelegate(uint id, int pname, int *args);
	private static glGetQueryObjectivDelegate s_glGetQueryObjectiv;
	public static void glGetQueryObjectiv(uint id, int pname, int *args) => s_glGetQueryObjectiv(id, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetQueryObjectui64vDelegate(uint id, int pname, ulong *args);
	private static glGetQueryObjectui64vDelegate s_glGetQueryObjectui64v;
	public static void glGetQueryObjectui64v(uint id, int pname, ulong *args) => s_glGetQueryObjectui64v(id, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetQueryObjectuivDelegate(uint id, int pname, uint *args);
	private static glGetQueryObjectuivDelegate s_glGetQueryObjectuiv;
	public static void glGetQueryObjectuiv(uint id, int pname, uint *args) => s_glGetQueryObjectuiv(id, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetQueryivDelegate(int target, int pname, int *args);
	private static glGetQueryivDelegate s_glGetQueryiv;
	public static void glGetQueryiv(int target, int pname, int *args) => s_glGetQueryiv(target, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetRenderbufferParameterivDelegate(int target, int pname, int *args);
	private static glGetRenderbufferParameterivDelegate s_glGetRenderbufferParameteriv;
	public static void glGetRenderbufferParameteriv(int target, int pname, int *args) => s_glGetRenderbufferParameteriv(target, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetSamplerParameterIivDelegate(uint sampler, int pname, int *args);
	private static glGetSamplerParameterIivDelegate s_glGetSamplerParameterIiv;
	public static void glGetSamplerParameterIiv(uint sampler, int pname, int *args) => s_glGetSamplerParameterIiv(sampler, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetSamplerParameterIuivDelegate(uint sampler, int pname, uint *args);
	private static glGetSamplerParameterIuivDelegate s_glGetSamplerParameterIuiv;
	public static void glGetSamplerParameterIuiv(uint sampler, int pname, uint *args) => s_glGetSamplerParameterIuiv(sampler, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetSamplerParameterfvDelegate(uint sampler, int pname, float *args);
	private static glGetSamplerParameterfvDelegate s_glGetSamplerParameterfv;
	public static void glGetSamplerParameterfv(uint sampler, int pname, float *args) => s_glGetSamplerParameterfv(sampler, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetSamplerParameterivDelegate(uint sampler, int pname, int *args);
	private static glGetSamplerParameterivDelegate s_glGetSamplerParameteriv;
	public static void glGetSamplerParameteriv(uint sampler, int pname, int *args) => s_glGetSamplerParameteriv(sampler, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetShaderInfoLogDelegate(uint shader, int bufSize, int *length, char *infoLog);
	private static glGetShaderInfoLogDelegate s_glGetShaderInfoLog;
	public static void glGetShaderInfoLog(uint shader, int bufSize, int *length, char *infoLog) => s_glGetShaderInfoLog(shader, bufSize, length, infoLog);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetShaderPrecisionFormatDelegate(int shadertype, int precisiontype, int *range, int *precision);
	private static glGetShaderPrecisionFormatDelegate s_glGetShaderPrecisionFormat;
	public static void glGetShaderPrecisionFormat(int shadertype, int precisiontype, int *range, int *precision) => s_glGetShaderPrecisionFormat(shadertype, precisiontype, range, precision);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetShaderSourceDelegate(uint shader, int bufSize, int *length, char *source);
	private static glGetShaderSourceDelegate s_glGetShaderSource;
	public static void glGetShaderSource(uint shader, int bufSize, int *length, char *source) => s_glGetShaderSource(shader, bufSize, length, source);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetShaderivDelegate(uint shader, int pname, int *args);
	private static glGetShaderivDelegate s_glGetShaderiv;
	public static void glGetShaderiv(uint shader, int pname, int *args) => s_glGetShaderiv(shader, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate  byte *glGetStringDelegate(int name);
	private static glGetStringDelegate s_glGetString;
	public static  byte *glGetString(int name) => s_glGetString(name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate  byte *glGetStringiDelegate(int name, uint index);
	private static glGetStringiDelegate s_glGetStringi;
	public static  byte *glGetStringi(int name, uint index) => s_glGetStringi(name, index);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate uint glGetSubroutineIndexDelegate(uint program, int shadertype, char *name);
	private static glGetSubroutineIndexDelegate s_glGetSubroutineIndex;
	public static uint glGetSubroutineIndex(uint program, int shadertype, char *name) => s_glGetSubroutineIndex(program, shadertype, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int glGetSubroutineUniformLocationDelegate(uint program, int shadertype, char *name);
	private static glGetSubroutineUniformLocationDelegate s_glGetSubroutineUniformLocation;
	public static int glGetSubroutineUniformLocation(uint program, int shadertype, char *name) => s_glGetSubroutineUniformLocation(program, shadertype, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetSyncivDelegate(IntPtr sync, int pname, int count, int *length, int *values);
	private static glGetSyncivDelegate s_glGetSynciv;
	public static void glGetSynciv(IntPtr sync, int pname, int count, int *length, int *values) => s_glGetSynciv(sync, pname, count, length, values);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTexImageDelegate(int target, int level, int format, int type, void *pixels);
	private static glGetTexImageDelegate s_glGetTexImage;
	public static void glGetTexImage(int target, int level, int format, int type, void *pixels) => s_glGetTexImage(target, level, format, type, pixels);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTexLevelParameterfvDelegate(int target, int level, int pname, float *args);
	private static glGetTexLevelParameterfvDelegate s_glGetTexLevelParameterfv;
	public static void glGetTexLevelParameterfv(int target, int level, int pname, float *args) => s_glGetTexLevelParameterfv(target, level, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTexLevelParameterivDelegate(int target, int level, int pname, int *args);
	private static glGetTexLevelParameterivDelegate s_glGetTexLevelParameteriv;
	public static void glGetTexLevelParameteriv(int target, int level, int pname, int *args) => s_glGetTexLevelParameteriv(target, level, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTexParameterIivDelegate(int target, int pname, int *args);
	private static glGetTexParameterIivDelegate s_glGetTexParameterIiv;
	public static void glGetTexParameterIiv(int target, int pname, int *args) => s_glGetTexParameterIiv(target, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTexParameterIuivDelegate(int target, int pname, uint *args);
	private static glGetTexParameterIuivDelegate s_glGetTexParameterIuiv;
	public static void glGetTexParameterIuiv(int target, int pname, uint *args) => s_glGetTexParameterIuiv(target, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTexParameterfvDelegate(int target, int pname, float *args);
	private static glGetTexParameterfvDelegate s_glGetTexParameterfv;
	public static void glGetTexParameterfv(int target, int pname, float *args) => s_glGetTexParameterfv(target, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTexParameterivDelegate(int target, int pname, int *args);
	private static glGetTexParameterivDelegate s_glGetTexParameteriv;
	public static void glGetTexParameteriv(int target, int pname, int *args) => s_glGetTexParameteriv(target, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTextureImageDelegate(uint texture, int level, int format, int type, int bufSize, void *pixels);
	private static glGetTextureImageDelegate s_glGetTextureImage;
	public static void glGetTextureImage(uint texture, int level, int format, int type, int bufSize, void *pixels) => s_glGetTextureImage(texture, level, format, type, bufSize, pixels);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTextureLevelParameterfvDelegate(uint texture, int level, int pname, float *args);
	private static glGetTextureLevelParameterfvDelegate s_glGetTextureLevelParameterfv;
	public static void glGetTextureLevelParameterfv(uint texture, int level, int pname, float *args) => s_glGetTextureLevelParameterfv(texture, level, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTextureLevelParameterivDelegate(uint texture, int level, int pname, int *args);
	private static glGetTextureLevelParameterivDelegate s_glGetTextureLevelParameteriv;
	public static void glGetTextureLevelParameteriv(uint texture, int level, int pname, int *args) => s_glGetTextureLevelParameteriv(texture, level, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTextureParameterIivDelegate(uint texture, int pname, int *args);
	private static glGetTextureParameterIivDelegate s_glGetTextureParameterIiv;
	public static void glGetTextureParameterIiv(uint texture, int pname, int *args) => s_glGetTextureParameterIiv(texture, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTextureParameterIuivDelegate(uint texture, int pname, uint *args);
	private static glGetTextureParameterIuivDelegate s_glGetTextureParameterIuiv;
	public static void glGetTextureParameterIuiv(uint texture, int pname, uint *args) => s_glGetTextureParameterIuiv(texture, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTextureParameterfvDelegate(uint texture, int pname, float *args);
	private static glGetTextureParameterfvDelegate s_glGetTextureParameterfv;
	public static void glGetTextureParameterfv(uint texture, int pname, float *args) => s_glGetTextureParameterfv(texture, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTextureParameterivDelegate(uint texture, int pname, int *args);
	private static glGetTextureParameterivDelegate s_glGetTextureParameteriv;
	public static void glGetTextureParameteriv(uint texture, int pname, int *args) => s_glGetTextureParameteriv(texture, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTextureSubImageDelegate(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, int bufSize, void *pixels);
	private static glGetTextureSubImageDelegate s_glGetTextureSubImage;
	public static void glGetTextureSubImage(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, int bufSize, void *pixels) => s_glGetTextureSubImage(texture, level, xoffset, yoffset, zoffset, width, height, depth, format, type, bufSize, pixels);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTransformFeedbackVaryingDelegate(uint program, uint index, int bufSize, int *length, int *size, int *type, char *name);
	private static glGetTransformFeedbackVaryingDelegate s_glGetTransformFeedbackVarying;
	public static void glGetTransformFeedbackVarying(uint program, uint index, int bufSize, int *length, int *size, int *type, char *name) => s_glGetTransformFeedbackVarying(program, index, bufSize, length, size, type, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTransformFeedbacki64_vDelegate(uint xfb, int pname, uint index, long *param);
	private static glGetTransformFeedbacki64_vDelegate s_glGetTransformFeedbacki64_v;
	public static void glGetTransformFeedbacki64_v(uint xfb, int pname, uint index, long *param) => s_glGetTransformFeedbacki64_v(xfb, pname, index, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTransformFeedbacki_vDelegate(uint xfb, int pname, uint index, int *param);
	private static glGetTransformFeedbacki_vDelegate s_glGetTransformFeedbacki_v;
	public static void glGetTransformFeedbacki_v(uint xfb, int pname, uint index, int *param) => s_glGetTransformFeedbacki_v(xfb, pname, index, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetTransformFeedbackivDelegate(uint xfb, int pname, int *param);
	private static glGetTransformFeedbackivDelegate s_glGetTransformFeedbackiv;
	public static void glGetTransformFeedbackiv(uint xfb, int pname, int *param) => s_glGetTransformFeedbackiv(xfb, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate uint glGetUniformBlockIndexDelegate(uint program, char *uniformBlockName);
	private static glGetUniformBlockIndexDelegate s_glGetUniformBlockIndex;
	public static uint glGetUniformBlockIndex(uint program, char *uniformBlockName) => s_glGetUniformBlockIndex(program, uniformBlockName);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetUniformIndicesDelegate(uint program, int uniformCount, char **uniformNames, uint *uniformIndices);
	private static glGetUniformIndicesDelegate s_glGetUniformIndices;
	public static void glGetUniformIndices(uint program, int uniformCount, char **uniformNames, uint *uniformIndices) => s_glGetUniformIndices(program, uniformCount, uniformNames, uniformIndices);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int glGetUniformLocationDelegate(uint program, char *name);
	private static glGetUniformLocationDelegate s_glGetUniformLocation;
	public static int glGetUniformLocation(uint program, char *name) => s_glGetUniformLocation(program, name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetUniformSubroutineuivDelegate(int shadertype, int location, uint *args);
	private static glGetUniformSubroutineuivDelegate s_glGetUniformSubroutineuiv;
	public static void glGetUniformSubroutineuiv(int shadertype, int location, uint *args) => s_glGetUniformSubroutineuiv(shadertype, location, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetUniformdvDelegate(uint program, int location, double *args);
	private static glGetUniformdvDelegate s_glGetUniformdv;
	public static void glGetUniformdv(uint program, int location, double *args) => s_glGetUniformdv(program, location, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetUniformfvDelegate(uint program, int location, float *args);
	private static glGetUniformfvDelegate s_glGetUniformfv;
	public static void glGetUniformfv(uint program, int location, float *args) => s_glGetUniformfv(program, location, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetUniformivDelegate(uint program, int location, int *args);
	private static glGetUniformivDelegate s_glGetUniformiv;
	public static void glGetUniformiv(uint program, int location, int *args) => s_glGetUniformiv(program, location, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetUniformuivDelegate(uint program, int location, uint *args);
	private static glGetUniformuivDelegate s_glGetUniformuiv;
	public static void glGetUniformuiv(uint program, int location, uint *args) => s_glGetUniformuiv(program, location, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetVertexArrayIndexed64ivDelegate(uint vaobj, uint index, int pname, long *param);
	private static glGetVertexArrayIndexed64ivDelegate s_glGetVertexArrayIndexed64iv;
	public static void glGetVertexArrayIndexed64iv(uint vaobj, uint index, int pname, long *param) => s_glGetVertexArrayIndexed64iv(vaobj, index, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetVertexArrayIndexedivDelegate(uint vaobj, uint index, int pname, int *param);
	private static glGetVertexArrayIndexedivDelegate s_glGetVertexArrayIndexediv;
	public static void glGetVertexArrayIndexediv(uint vaobj, uint index, int pname, int *param) => s_glGetVertexArrayIndexediv(vaobj, index, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetVertexArrayivDelegate(uint vaobj, int pname, int *param);
	private static glGetVertexArrayivDelegate s_glGetVertexArrayiv;
	public static void glGetVertexArrayiv(uint vaobj, int pname, int *param) => s_glGetVertexArrayiv(vaobj, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetVertexAttribIivDelegate(uint index, int pname, int *args);
	private static glGetVertexAttribIivDelegate s_glGetVertexAttribIiv;
	public static void glGetVertexAttribIiv(uint index, int pname, int *args) => s_glGetVertexAttribIiv(index, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetVertexAttribIuivDelegate(uint index, int pname, uint *args);
	private static glGetVertexAttribIuivDelegate s_glGetVertexAttribIuiv;
	public static void glGetVertexAttribIuiv(uint index, int pname, uint *args) => s_glGetVertexAttribIuiv(index, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetVertexAttribLdvDelegate(uint index, int pname, double *args);
	private static glGetVertexAttribLdvDelegate s_glGetVertexAttribLdv;
	public static void glGetVertexAttribLdv(uint index, int pname, double *args) => s_glGetVertexAttribLdv(index, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetVertexAttribPointervDelegate(uint index, int pname, void **pointer);
	private static glGetVertexAttribPointervDelegate s_glGetVertexAttribPointerv;
	public static void glGetVertexAttribPointerv(uint index, int pname, void **pointer) => s_glGetVertexAttribPointerv(index, pname, pointer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetVertexAttribdvDelegate(uint index, int pname, double *args);
	private static glGetVertexAttribdvDelegate s_glGetVertexAttribdv;
	public static void glGetVertexAttribdv(uint index, int pname, double *args) => s_glGetVertexAttribdv(index, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetVertexAttribfvDelegate(uint index, int pname, float *args);
	private static glGetVertexAttribfvDelegate s_glGetVertexAttribfv;
	public static void glGetVertexAttribfv(uint index, int pname, float *args) => s_glGetVertexAttribfv(index, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetVertexAttribivDelegate(uint index, int pname, int *args);
	private static glGetVertexAttribivDelegate s_glGetVertexAttribiv;
	public static void glGetVertexAttribiv(uint index, int pname, int *args) => s_glGetVertexAttribiv(index, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnColorTableDelegate(int target, int format, int type, int bufSize, void *table);
	private static glGetnColorTableDelegate s_glGetnColorTable;
	public static void glGetnColorTable(int target, int format, int type, int bufSize, void *table) => s_glGetnColorTable(target, format, type, bufSize, table);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnCompressedTexImageDelegate(int target, int lod, int bufSize, void *pixels);
	private static glGetnCompressedTexImageDelegate s_glGetnCompressedTexImage;
	public static void glGetnCompressedTexImage(int target, int lod, int bufSize, void *pixels) => s_glGetnCompressedTexImage(target, lod, bufSize, pixels);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnConvolutionFilterDelegate(int target, int format, int type, int bufSize, void *image);
	private static glGetnConvolutionFilterDelegate s_glGetnConvolutionFilter;
	public static void glGetnConvolutionFilter(int target, int format, int type, int bufSize, void *image) => s_glGetnConvolutionFilter(target, format, type, bufSize, image);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnHistogramDelegate(int target, bool reset, int format, int type, int bufSize, void *values);
	private static glGetnHistogramDelegate s_glGetnHistogram;
	public static void glGetnHistogram(int target, bool reset, int format, int type, int bufSize, void *values) => s_glGetnHistogram(target, reset, format, type, bufSize, values);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnMapdvDelegate(int target, int query, int bufSize, double *v);
	private static glGetnMapdvDelegate s_glGetnMapdv;
	public static void glGetnMapdv(int target, int query, int bufSize, double *v) => s_glGetnMapdv(target, query, bufSize, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnMapfvDelegate(int target, int query, int bufSize, float *v);
	private static glGetnMapfvDelegate s_glGetnMapfv;
	public static void glGetnMapfv(int target, int query, int bufSize, float *v) => s_glGetnMapfv(target, query, bufSize, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnMapivDelegate(int target, int query, int bufSize, int *v);
	private static glGetnMapivDelegate s_glGetnMapiv;
	public static void glGetnMapiv(int target, int query, int bufSize, int *v) => s_glGetnMapiv(target, query, bufSize, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnMinmaxDelegate(int target, bool reset, int format, int type, int bufSize, void *values);
	private static glGetnMinmaxDelegate s_glGetnMinmax;
	public static void glGetnMinmax(int target, bool reset, int format, int type, int bufSize, void *values) => s_glGetnMinmax(target, reset, format, type, bufSize, values);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnPixelMapfvDelegate(int map, int bufSize, float *values);
	private static glGetnPixelMapfvDelegate s_glGetnPixelMapfv;
	public static void glGetnPixelMapfv(int map, int bufSize, float *values) => s_glGetnPixelMapfv(map, bufSize, values);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnPixelMapuivDelegate(int map, int bufSize, uint *values);
	private static glGetnPixelMapuivDelegate s_glGetnPixelMapuiv;
	public static void glGetnPixelMapuiv(int map, int bufSize, uint *values) => s_glGetnPixelMapuiv(map, bufSize, values);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnPixelMapusvDelegate(int map, int bufSize, ushort *values);
	private static glGetnPixelMapusvDelegate s_glGetnPixelMapusv;
	public static void glGetnPixelMapusv(int map, int bufSize, ushort *values) => s_glGetnPixelMapusv(map, bufSize, values);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnPolygonStippleDelegate(int bufSize, byte *pattern);
	private static glGetnPolygonStippleDelegate s_glGetnPolygonStipple;
	public static void glGetnPolygonStipple(int bufSize, byte *pattern) => s_glGetnPolygonStipple(bufSize, pattern);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnSeparableFilterDelegate(int target, int format, int type, int rowBufSize, void *row, int columnBufSize, void *column, void *span);
	private static glGetnSeparableFilterDelegate s_glGetnSeparableFilter;
	public static void glGetnSeparableFilter(int target, int format, int type, int rowBufSize, void *row, int columnBufSize, void *column, void *span) => s_glGetnSeparableFilter(target, format, type, rowBufSize, row, columnBufSize, column, span);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnTexImageDelegate(int target, int level, int format, int type, int bufSize, void *pixels);
	private static glGetnTexImageDelegate s_glGetnTexImage;
	public static void glGetnTexImage(int target, int level, int format, int type, int bufSize, void *pixels) => s_glGetnTexImage(target, level, format, type, bufSize, pixels);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnUniformdvDelegate(uint program, int location, int bufSize, double *args);
	private static glGetnUniformdvDelegate s_glGetnUniformdv;
	public static void glGetnUniformdv(uint program, int location, int bufSize, double *args) => s_glGetnUniformdv(program, location, bufSize, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnUniformfvDelegate(uint program, int location, int bufSize, float *args);
	private static glGetnUniformfvDelegate s_glGetnUniformfv;
	public static void glGetnUniformfv(uint program, int location, int bufSize, float *args) => s_glGetnUniformfv(program, location, bufSize, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnUniformivDelegate(uint program, int location, int bufSize, int *args);
	private static glGetnUniformivDelegate s_glGetnUniformiv;
	public static void glGetnUniformiv(uint program, int location, int bufSize, int *args) => s_glGetnUniformiv(program, location, bufSize, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glGetnUniformuivDelegate(uint program, int location, int bufSize, uint *args);
	private static glGetnUniformuivDelegate s_glGetnUniformuiv;
	public static void glGetnUniformuiv(uint program, int location, int bufSize, uint *args) => s_glGetnUniformuiv(program, location, bufSize, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glHintDelegate(int target, int mode);
	private static glHintDelegate s_glHint;
	public static void glHint(int target, int mode) => s_glHint(target, mode);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glInvalidateBufferDataDelegate(uint buffer);
	private static glInvalidateBufferDataDelegate s_glInvalidateBufferData;
	public static void glInvalidateBufferData(uint buffer) => s_glInvalidateBufferData(buffer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glInvalidateBufferSubDataDelegate(uint buffer, IntPtr offset, IntPtr length);
	private static glInvalidateBufferSubDataDelegate s_glInvalidateBufferSubData;
	public static void glInvalidateBufferSubData(uint buffer, IntPtr offset, IntPtr length) => s_glInvalidateBufferSubData(buffer, offset, length);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glInvalidateFramebufferDelegate(int target, int numAttachments, int *attachments);
	private static glInvalidateFramebufferDelegate s_glInvalidateFramebuffer;
	public static void glInvalidateFramebuffer(int target, int numAttachments, int *attachments) => s_glInvalidateFramebuffer(target, numAttachments, attachments);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glInvalidateNamedFramebufferDataDelegate(uint framebuffer, int numAttachments, int *attachments);
	private static glInvalidateNamedFramebufferDataDelegate s_glInvalidateNamedFramebufferData;
	public static void glInvalidateNamedFramebufferData(uint framebuffer, int numAttachments, int *attachments) => s_glInvalidateNamedFramebufferData(framebuffer, numAttachments, attachments);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glInvalidateNamedFramebufferSubDataDelegate(uint framebuffer, int numAttachments, int *attachments, int x, int y, int width, int height);
	private static glInvalidateNamedFramebufferSubDataDelegate s_glInvalidateNamedFramebufferSubData;
	public static void glInvalidateNamedFramebufferSubData(uint framebuffer, int numAttachments, int *attachments, int x, int y, int width, int height) => s_glInvalidateNamedFramebufferSubData(framebuffer, numAttachments, attachments, x, y, width, height);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glInvalidateSubFramebufferDelegate(int target, int numAttachments, int *attachments, int x, int y, int width, int height);
	private static glInvalidateSubFramebufferDelegate s_glInvalidateSubFramebuffer;
	public static void glInvalidateSubFramebuffer(int target, int numAttachments, int *attachments, int x, int y, int width, int height) => s_glInvalidateSubFramebuffer(target, numAttachments, attachments, x, y, width, height);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glInvalidateTexImageDelegate(uint texture, int level);
	private static glInvalidateTexImageDelegate s_glInvalidateTexImage;
	public static void glInvalidateTexImage(uint texture, int level) => s_glInvalidateTexImage(texture, level);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glInvalidateTexSubImageDelegate(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth);
	private static glInvalidateTexSubImageDelegate s_glInvalidateTexSubImage;
	public static void glInvalidateTexSubImage(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth) => s_glInvalidateTexSubImage(texture, level, xoffset, yoffset, zoffset, width, height, depth);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool glIsBufferDelegate(uint buffer);
	private static glIsBufferDelegate s_glIsBuffer;
	public static bool glIsBuffer(uint buffer) => s_glIsBuffer(buffer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool glIsEnabledDelegate(int cap);
	private static glIsEnabledDelegate s_glIsEnabled;
	public static bool glIsEnabled(int cap) => s_glIsEnabled(cap);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool glIsEnablediDelegate(int target, uint index);
	private static glIsEnablediDelegate s_glIsEnabledi;
	public static bool glIsEnabledi(int target, uint index) => s_glIsEnabledi(target, index);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool glIsFramebufferDelegate(uint framebuffer);
	private static glIsFramebufferDelegate s_glIsFramebuffer;
	public static bool glIsFramebuffer(uint framebuffer) => s_glIsFramebuffer(framebuffer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool glIsProgramDelegate(uint program);
	private static glIsProgramDelegate s_glIsProgram;
	public static bool glIsProgram(uint program) => s_glIsProgram(program);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool glIsProgramPipelineDelegate(uint pipeline);
	private static glIsProgramPipelineDelegate s_glIsProgramPipeline;
	public static bool glIsProgramPipeline(uint pipeline) => s_glIsProgramPipeline(pipeline);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool glIsQueryDelegate(uint id);
	private static glIsQueryDelegate s_glIsQuery;
	public static bool glIsQuery(uint id) => s_glIsQuery(id);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool glIsRenderbufferDelegate(uint renderbuffer);
	private static glIsRenderbufferDelegate s_glIsRenderbuffer;
	public static bool glIsRenderbuffer(uint renderbuffer) => s_glIsRenderbuffer(renderbuffer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool glIsSamplerDelegate(uint sampler);
	private static glIsSamplerDelegate s_glIsSampler;
	public static bool glIsSampler(uint sampler) => s_glIsSampler(sampler);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool glIsShaderDelegate(uint shader);
	private static glIsShaderDelegate s_glIsShader;
	public static bool glIsShader(uint shader) => s_glIsShader(shader);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool glIsSyncDelegate(IntPtr sync);
	private static glIsSyncDelegate s_glIsSync;
	public static bool glIsSync(IntPtr sync) => s_glIsSync(sync);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool glIsTextureDelegate(uint texture);
	private static glIsTextureDelegate s_glIsTexture;
	public static bool glIsTexture(uint texture) => s_glIsTexture(texture);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool glIsTransformFeedbackDelegate(uint id);
	private static glIsTransformFeedbackDelegate s_glIsTransformFeedback;
	public static bool glIsTransformFeedback(uint id) => s_glIsTransformFeedback(id);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool glIsVertexArrayDelegate(uint array);
	private static glIsVertexArrayDelegate s_glIsVertexArray;
	public static bool glIsVertexArray(uint array) => s_glIsVertexArray(array);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glLineWidthDelegate(float width);
	private static glLineWidthDelegate s_glLineWidth;
	public static void glLineWidth(float width) => s_glLineWidth(width);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glLinkProgramDelegate(uint program);
	private static glLinkProgramDelegate s_glLinkProgram;
	public static void glLinkProgram(uint program) => s_glLinkProgram(program);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glLogicOpDelegate(int opcode);
	private static glLogicOpDelegate s_glLogicOp;
	public static void glLogicOp(int opcode) => s_glLogicOp(opcode);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void *glMapBufferDelegate(int target, int access);
	private static glMapBufferDelegate s_glMapBuffer;
	public static void *glMapBuffer(int target, int access) => s_glMapBuffer(target, access);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void *glMapBufferRangeDelegate(int target, IntPtr offset, IntPtr length, int access);
	private static glMapBufferRangeDelegate s_glMapBufferRange;
	public static void *glMapBufferRange(int target, IntPtr offset, IntPtr length, int access) => s_glMapBufferRange(target, offset, length, access);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void *glMapNamedBufferDelegate(uint buffer, int access);
	private static glMapNamedBufferDelegate s_glMapNamedBuffer;
	public static void *glMapNamedBuffer(uint buffer, int access) => s_glMapNamedBuffer(buffer, access);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void *glMapNamedBufferRangeDelegate(uint buffer, IntPtr offset, IntPtr length, int access);
	private static glMapNamedBufferRangeDelegate s_glMapNamedBufferRange;
	public static void *glMapNamedBufferRange(uint buffer, IntPtr offset, IntPtr length, int access) => s_glMapNamedBufferRange(buffer, offset, length, access);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMemoryBarrierDelegate(int barriers);
	private static glMemoryBarrierDelegate s_glMemoryBarrier;
	public static void glMemoryBarrier(int barriers) => s_glMemoryBarrier(barriers);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMemoryBarrierByRegionDelegate(int barriers);
	private static glMemoryBarrierByRegionDelegate s_glMemoryBarrierByRegion;
	public static void glMemoryBarrierByRegion(int barriers) => s_glMemoryBarrierByRegion(barriers);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMinSampleShadingDelegate(float value);
	private static glMinSampleShadingDelegate s_glMinSampleShading;
	public static void glMinSampleShading(float value) => s_glMinSampleShading(value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMultiDrawArraysDelegate(int mode, int *first, int *count, int drawcount);
	private static glMultiDrawArraysDelegate s_glMultiDrawArrays;
	public static void glMultiDrawArrays(int mode, int *first, int *count, int drawcount) => s_glMultiDrawArrays(mode, first, count, drawcount);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMultiDrawArraysIndirectDelegate(int mode, void *indirect, int drawcount, int stride);
	private static glMultiDrawArraysIndirectDelegate s_glMultiDrawArraysIndirect;
	public static void glMultiDrawArraysIndirect(int mode, void *indirect, int drawcount, int stride) => s_glMultiDrawArraysIndirect(mode, indirect, drawcount, stride);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMultiDrawArraysIndirectCountDelegate(int mode, void *indirect, IntPtr drawcount, int maxdrawcount, int stride);
	private static glMultiDrawArraysIndirectCountDelegate s_glMultiDrawArraysIndirectCount;
	public static void glMultiDrawArraysIndirectCount(int mode, void *indirect, IntPtr drawcount, int maxdrawcount, int stride) => s_glMultiDrawArraysIndirectCount(mode, indirect, drawcount, maxdrawcount, stride);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMultiDrawElementsDelegate(int mode, int *count, int type, void **indices, int drawcount);
	private static glMultiDrawElementsDelegate s_glMultiDrawElements;
	public static void glMultiDrawElements(int mode, int *count, int type, void **indices, int drawcount) => s_glMultiDrawElements(mode, count, type, indices, drawcount);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMultiDrawElementsBaseVertexDelegate(int mode, int *count, int type, void **indices, int drawcount, int *basevertex);
	private static glMultiDrawElementsBaseVertexDelegate s_glMultiDrawElementsBaseVertex;
	public static void glMultiDrawElementsBaseVertex(int mode, int *count, int type, void **indices, int drawcount, int *basevertex) => s_glMultiDrawElementsBaseVertex(mode, count, type, indices, drawcount, basevertex);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMultiDrawElementsIndirectDelegate(int mode, int type, void *indirect, int drawcount, int stride);
	private static glMultiDrawElementsIndirectDelegate s_glMultiDrawElementsIndirect;
	public static void glMultiDrawElementsIndirect(int mode, int type, void *indirect, int drawcount, int stride) => s_glMultiDrawElementsIndirect(mode, type, indirect, drawcount, stride);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMultiDrawElementsIndirectCountDelegate(int mode, int type, void *indirect, IntPtr drawcount, int maxdrawcount, int stride);
	private static glMultiDrawElementsIndirectCountDelegate s_glMultiDrawElementsIndirectCount;
	public static void glMultiDrawElementsIndirectCount(int mode, int type, void *indirect, IntPtr drawcount, int maxdrawcount, int stride) => s_glMultiDrawElementsIndirectCount(mode, type, indirect, drawcount, maxdrawcount, stride);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMultiTexCoordP1uiDelegate(int texture, int type, uint coords);
	private static glMultiTexCoordP1uiDelegate s_glMultiTexCoordP1ui;
	public static void glMultiTexCoordP1ui(int texture, int type, uint coords) => s_glMultiTexCoordP1ui(texture, type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMultiTexCoordP1uivDelegate(int texture, int type, uint *coords);
	private static glMultiTexCoordP1uivDelegate s_glMultiTexCoordP1uiv;
	public static void glMultiTexCoordP1uiv(int texture, int type, uint *coords) => s_glMultiTexCoordP1uiv(texture, type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMultiTexCoordP2uiDelegate(int texture, int type, uint coords);
	private static glMultiTexCoordP2uiDelegate s_glMultiTexCoordP2ui;
	public static void glMultiTexCoordP2ui(int texture, int type, uint coords) => s_glMultiTexCoordP2ui(texture, type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMultiTexCoordP2uivDelegate(int texture, int type, uint *coords);
	private static glMultiTexCoordP2uivDelegate s_glMultiTexCoordP2uiv;
	public static void glMultiTexCoordP2uiv(int texture, int type, uint *coords) => s_glMultiTexCoordP2uiv(texture, type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMultiTexCoordP3uiDelegate(int texture, int type, uint coords);
	private static glMultiTexCoordP3uiDelegate s_glMultiTexCoordP3ui;
	public static void glMultiTexCoordP3ui(int texture, int type, uint coords) => s_glMultiTexCoordP3ui(texture, type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMultiTexCoordP3uivDelegate(int texture, int type, uint *coords);
	private static glMultiTexCoordP3uivDelegate s_glMultiTexCoordP3uiv;
	public static void glMultiTexCoordP3uiv(int texture, int type, uint *coords) => s_glMultiTexCoordP3uiv(texture, type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMultiTexCoordP4uiDelegate(int texture, int type, uint coords);
	private static glMultiTexCoordP4uiDelegate s_glMultiTexCoordP4ui;
	public static void glMultiTexCoordP4ui(int texture, int type, uint coords) => s_glMultiTexCoordP4ui(texture, type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glMultiTexCoordP4uivDelegate(int texture, int type, uint *coords);
	private static glMultiTexCoordP4uivDelegate s_glMultiTexCoordP4uiv;
	public static void glMultiTexCoordP4uiv(int texture, int type, uint *coords) => s_glMultiTexCoordP4uiv(texture, type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glNamedBufferDataDelegate(uint buffer, IntPtr size, void *data, int usage);
	private static glNamedBufferDataDelegate s_glNamedBufferData;
	public static void glNamedBufferData(uint buffer, IntPtr size, void *data, int usage) => s_glNamedBufferData(buffer, size, data, usage);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glNamedBufferStorageDelegate(uint buffer, IntPtr size, void *data, int flags);
	private static glNamedBufferStorageDelegate s_glNamedBufferStorage;
	public static void glNamedBufferStorage(uint buffer, IntPtr size, void *data, int flags) => s_glNamedBufferStorage(buffer, size, data, flags);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glNamedBufferSubDataDelegate(uint buffer, IntPtr offset, IntPtr size, void *data);
	private static glNamedBufferSubDataDelegate s_glNamedBufferSubData;
	public static void glNamedBufferSubData(uint buffer, IntPtr offset, IntPtr size, void *data) => s_glNamedBufferSubData(buffer, offset, size, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glNamedFramebufferDrawBufferDelegate(uint framebuffer, int buf);
	private static glNamedFramebufferDrawBufferDelegate s_glNamedFramebufferDrawBuffer;
	public static void glNamedFramebufferDrawBuffer(uint framebuffer, int buf) => s_glNamedFramebufferDrawBuffer(framebuffer, buf);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glNamedFramebufferDrawBuffersDelegate(uint framebuffer, int n, int *bufs);
	private static glNamedFramebufferDrawBuffersDelegate s_glNamedFramebufferDrawBuffers;
	public static void glNamedFramebufferDrawBuffers(uint framebuffer, int n, int *bufs) => s_glNamedFramebufferDrawBuffers(framebuffer, n, bufs);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glNamedFramebufferParameteriDelegate(uint framebuffer, int pname, int param);
	private static glNamedFramebufferParameteriDelegate s_glNamedFramebufferParameteri;
	public static void glNamedFramebufferParameteri(uint framebuffer, int pname, int param) => s_glNamedFramebufferParameteri(framebuffer, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glNamedFramebufferReadBufferDelegate(uint framebuffer, int src);
	private static glNamedFramebufferReadBufferDelegate s_glNamedFramebufferReadBuffer;
	public static void glNamedFramebufferReadBuffer(uint framebuffer, int src) => s_glNamedFramebufferReadBuffer(framebuffer, src);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glNamedFramebufferRenderbufferDelegate(uint framebuffer, int attachment, int renderbuffertarget, uint renderbuffer);
	private static glNamedFramebufferRenderbufferDelegate s_glNamedFramebufferRenderbuffer;
	public static void glNamedFramebufferRenderbuffer(uint framebuffer, int attachment, int renderbuffertarget, uint renderbuffer) => s_glNamedFramebufferRenderbuffer(framebuffer, attachment, renderbuffertarget, renderbuffer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glNamedFramebufferTextureDelegate(uint framebuffer, int attachment, uint texture, int level);
	private static glNamedFramebufferTextureDelegate s_glNamedFramebufferTexture;
	public static void glNamedFramebufferTexture(uint framebuffer, int attachment, uint texture, int level) => s_glNamedFramebufferTexture(framebuffer, attachment, texture, level);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glNamedFramebufferTextureLayerDelegate(uint framebuffer, int attachment, uint texture, int level, int layer);
	private static glNamedFramebufferTextureLayerDelegate s_glNamedFramebufferTextureLayer;
	public static void glNamedFramebufferTextureLayer(uint framebuffer, int attachment, uint texture, int level, int layer) => s_glNamedFramebufferTextureLayer(framebuffer, attachment, texture, level, layer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glNamedRenderbufferStorageDelegate(uint renderbuffer, int internalformat, int width, int height);
	private static glNamedRenderbufferStorageDelegate s_glNamedRenderbufferStorage;
	public static void glNamedRenderbufferStorage(uint renderbuffer, int internalformat, int width, int height) => s_glNamedRenderbufferStorage(renderbuffer, internalformat, width, height);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glNamedRenderbufferStorageMultisampleDelegate(uint renderbuffer, int samples, int internalformat, int width, int height);
	private static glNamedRenderbufferStorageMultisampleDelegate s_glNamedRenderbufferStorageMultisample;
	public static void glNamedRenderbufferStorageMultisample(uint renderbuffer, int samples, int internalformat, int width, int height) => s_glNamedRenderbufferStorageMultisample(renderbuffer, samples, internalformat, width, height);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glNormalP3uiDelegate(int type, uint coords);
	private static glNormalP3uiDelegate s_glNormalP3ui;
	public static void glNormalP3ui(int type, uint coords) => s_glNormalP3ui(type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glNormalP3uivDelegate(int type, uint *coords);
	private static glNormalP3uivDelegate s_glNormalP3uiv;
	public static void glNormalP3uiv(int type, uint *coords) => s_glNormalP3uiv(type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glObjectLabelDelegate(int identifier, uint name, int length, char *label);
	private static glObjectLabelDelegate s_glObjectLabel;
	public static void glObjectLabel(int identifier, uint name, int length, char *label) => s_glObjectLabel(identifier, name, length, label);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glObjectPtrLabelDelegate(void *ptr, int length, char *label);
	private static glObjectPtrLabelDelegate s_glObjectPtrLabel;
	public static void glObjectPtrLabel(void *ptr, int length, char *label) => s_glObjectPtrLabel(ptr, length, label);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glPatchParameterfvDelegate(int pname, float *values);
	private static glPatchParameterfvDelegate s_glPatchParameterfv;
	public static void glPatchParameterfv(int pname, float *values) => s_glPatchParameterfv(pname, values);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glPatchParameteriDelegate(int pname, int value);
	private static glPatchParameteriDelegate s_glPatchParameteri;
	public static void glPatchParameteri(int pname, int value) => s_glPatchParameteri(pname, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glPauseTransformFeedbackDelegate();
	private static glPauseTransformFeedbackDelegate s_glPauseTransformFeedback;
	public static void glPauseTransformFeedback() => s_glPauseTransformFeedback();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glPixelStorefDelegate(int pname, float param);
	private static glPixelStorefDelegate s_glPixelStoref;
	public static void glPixelStoref(int pname, float param) => s_glPixelStoref(pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glPixelStoreiDelegate(int pname, int param);
	private static glPixelStoreiDelegate s_glPixelStorei;
	public static void glPixelStorei(int pname, int param) => s_glPixelStorei(pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glPointParameterfDelegate(int pname, float param);
	private static glPointParameterfDelegate s_glPointParameterf;
	public static void glPointParameterf(int pname, float param) => s_glPointParameterf(pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glPointParameterfvDelegate(int pname, float *args);
	private static glPointParameterfvDelegate s_glPointParameterfv;
	public static void glPointParameterfv(int pname, float *args) => s_glPointParameterfv(pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glPointParameteriDelegate(int pname, int param);
	private static glPointParameteriDelegate s_glPointParameteri;
	public static void glPointParameteri(int pname, int param) => s_glPointParameteri(pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glPointParameterivDelegate(int pname, int *args);
	private static glPointParameterivDelegate s_glPointParameteriv;
	public static void glPointParameteriv(int pname, int *args) => s_glPointParameteriv(pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glPointSizeDelegate(float size);
	private static glPointSizeDelegate s_glPointSize;
	public static void glPointSize(float size) => s_glPointSize(size);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glPolygonModeDelegate(int face, int mode);
	private static glPolygonModeDelegate s_glPolygonMode;
	public static void glPolygonMode(int face, int mode) => s_glPolygonMode(face, mode);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glPolygonOffsetDelegate(float factor, float units);
	private static glPolygonOffsetDelegate s_glPolygonOffset;
	public static void glPolygonOffset(float factor, float units) => s_glPolygonOffset(factor, units);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glPolygonOffsetClampDelegate(float factor, float units, float clamp);
	private static glPolygonOffsetClampDelegate s_glPolygonOffsetClamp;
	public static void glPolygonOffsetClamp(float factor, float units, float clamp) => s_glPolygonOffsetClamp(factor, units, clamp);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glPopDebugGroupDelegate();
	private static glPopDebugGroupDelegate s_glPopDebugGroup;
	public static void glPopDebugGroup() => s_glPopDebugGroup();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glPrimitiveRestartIndexDelegate(uint index);
	private static glPrimitiveRestartIndexDelegate s_glPrimitiveRestartIndex;
	public static void glPrimitiveRestartIndex(uint index) => s_glPrimitiveRestartIndex(index);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramBinaryDelegate(uint program, int binaryFormat, void *binary, int length);
	private static glProgramBinaryDelegate s_glProgramBinary;
	public static void glProgramBinary(uint program, int binaryFormat, void *binary, int length) => s_glProgramBinary(program, binaryFormat, binary, length);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramParameteriDelegate(uint program, int pname, int value);
	private static glProgramParameteriDelegate s_glProgramParameteri;
	public static void glProgramParameteri(uint program, int pname, int value) => s_glProgramParameteri(program, pname, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform1dDelegate(uint program, int location, double v0);
	private static glProgramUniform1dDelegate s_glProgramUniform1d;
	public static void glProgramUniform1d(uint program, int location, double v0) => s_glProgramUniform1d(program, location, v0);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform1dvDelegate(uint program, int location, int count, double *value);
	private static glProgramUniform1dvDelegate s_glProgramUniform1dv;
	public static void glProgramUniform1dv(uint program, int location, int count, double *value) => s_glProgramUniform1dv(program, location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform1fDelegate(uint program, int location, float v0);
	private static glProgramUniform1fDelegate s_glProgramUniform1f;
	public static void glProgramUniform1f(uint program, int location, float v0) => s_glProgramUniform1f(program, location, v0);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform1fvDelegate(uint program, int location, int count, float *value);
	private static glProgramUniform1fvDelegate s_glProgramUniform1fv;
	public static void glProgramUniform1fv(uint program, int location, int count, float *value) => s_glProgramUniform1fv(program, location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform1iDelegate(uint program, int location, int v0);
	private static glProgramUniform1iDelegate s_glProgramUniform1i;
	public static void glProgramUniform1i(uint program, int location, int v0) => s_glProgramUniform1i(program, location, v0);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform1ivDelegate(uint program, int location, int count, int *value);
	private static glProgramUniform1ivDelegate s_glProgramUniform1iv;
	public static void glProgramUniform1iv(uint program, int location, int count, int *value) => s_glProgramUniform1iv(program, location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform1uiDelegate(uint program, int location, uint v0);
	private static glProgramUniform1uiDelegate s_glProgramUniform1ui;
	public static void glProgramUniform1ui(uint program, int location, uint v0) => s_glProgramUniform1ui(program, location, v0);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform1uivDelegate(uint program, int location, int count, uint *value);
	private static glProgramUniform1uivDelegate s_glProgramUniform1uiv;
	public static void glProgramUniform1uiv(uint program, int location, int count, uint *value) => s_glProgramUniform1uiv(program, location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform2dDelegate(uint program, int location, double v0, double v1);
	private static glProgramUniform2dDelegate s_glProgramUniform2d;
	public static void glProgramUniform2d(uint program, int location, double v0, double v1) => s_glProgramUniform2d(program, location, v0, v1);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform2dvDelegate(uint program, int location, int count, double *value);
	private static glProgramUniform2dvDelegate s_glProgramUniform2dv;
	public static void glProgramUniform2dv(uint program, int location, int count, double *value) => s_glProgramUniform2dv(program, location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform2fDelegate(uint program, int location, float v0, float v1);
	private static glProgramUniform2fDelegate s_glProgramUniform2f;
	public static void glProgramUniform2f(uint program, int location, float v0, float v1) => s_glProgramUniform2f(program, location, v0, v1);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform2fvDelegate(uint program, int location, int count, float *value);
	private static glProgramUniform2fvDelegate s_glProgramUniform2fv;
	public static void glProgramUniform2fv(uint program, int location, int count, float *value) => s_glProgramUniform2fv(program, location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform2iDelegate(uint program, int location, int v0, int v1);
	private static glProgramUniform2iDelegate s_glProgramUniform2i;
	public static void glProgramUniform2i(uint program, int location, int v0, int v1) => s_glProgramUniform2i(program, location, v0, v1);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform2ivDelegate(uint program, int location, int count, int *value);
	private static glProgramUniform2ivDelegate s_glProgramUniform2iv;
	public static void glProgramUniform2iv(uint program, int location, int count, int *value) => s_glProgramUniform2iv(program, location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform2uiDelegate(uint program, int location, uint v0, uint v1);
	private static glProgramUniform2uiDelegate s_glProgramUniform2ui;
	public static void glProgramUniform2ui(uint program, int location, uint v0, uint v1) => s_glProgramUniform2ui(program, location, v0, v1);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform2uivDelegate(uint program, int location, int count, uint *value);
	private static glProgramUniform2uivDelegate s_glProgramUniform2uiv;
	public static void glProgramUniform2uiv(uint program, int location, int count, uint *value) => s_glProgramUniform2uiv(program, location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform3dDelegate(uint program, int location, double v0, double v1, double v2);
	private static glProgramUniform3dDelegate s_glProgramUniform3d;
	public static void glProgramUniform3d(uint program, int location, double v0, double v1, double v2) => s_glProgramUniform3d(program, location, v0, v1, v2);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform3dvDelegate(uint program, int location, int count, double *value);
	private static glProgramUniform3dvDelegate s_glProgramUniform3dv;
	public static void glProgramUniform3dv(uint program, int location, int count, double *value) => s_glProgramUniform3dv(program, location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform3fDelegate(uint program, int location, float v0, float v1, float v2);
	private static glProgramUniform3fDelegate s_glProgramUniform3f;
	public static void glProgramUniform3f(uint program, int location, float v0, float v1, float v2) => s_glProgramUniform3f(program, location, v0, v1, v2);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform3fvDelegate(uint program, int location, int count, float *value);
	private static glProgramUniform3fvDelegate s_glProgramUniform3fv;
	public static void glProgramUniform3fv(uint program, int location, int count, float *value) => s_glProgramUniform3fv(program, location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform3iDelegate(uint program, int location, int v0, int v1, int v2);
	private static glProgramUniform3iDelegate s_glProgramUniform3i;
	public static void glProgramUniform3i(uint program, int location, int v0, int v1, int v2) => s_glProgramUniform3i(program, location, v0, v1, v2);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform3ivDelegate(uint program, int location, int count, int *value);
	private static glProgramUniform3ivDelegate s_glProgramUniform3iv;
	public static void glProgramUniform3iv(uint program, int location, int count, int *value) => s_glProgramUniform3iv(program, location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform3uiDelegate(uint program, int location, uint v0, uint v1, uint v2);
	private static glProgramUniform3uiDelegate s_glProgramUniform3ui;
	public static void glProgramUniform3ui(uint program, int location, uint v0, uint v1, uint v2) => s_glProgramUniform3ui(program, location, v0, v1, v2);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform3uivDelegate(uint program, int location, int count, uint *value);
	private static glProgramUniform3uivDelegate s_glProgramUniform3uiv;
	public static void glProgramUniform3uiv(uint program, int location, int count, uint *value) => s_glProgramUniform3uiv(program, location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform4dDelegate(uint program, int location, double v0, double v1, double v2, double v3);
	private static glProgramUniform4dDelegate s_glProgramUniform4d;
	public static void glProgramUniform4d(uint program, int location, double v0, double v1, double v2, double v3) => s_glProgramUniform4d(program, location, v0, v1, v2, v3);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform4dvDelegate(uint program, int location, int count, double *value);
	private static glProgramUniform4dvDelegate s_glProgramUniform4dv;
	public static void glProgramUniform4dv(uint program, int location, int count, double *value) => s_glProgramUniform4dv(program, location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform4fDelegate(uint program, int location, float v0, float v1, float v2, float v3);
	private static glProgramUniform4fDelegate s_glProgramUniform4f;
	public static void glProgramUniform4f(uint program, int location, float v0, float v1, float v2, float v3) => s_glProgramUniform4f(program, location, v0, v1, v2, v3);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform4fvDelegate(uint program, int location, int count, float *value);
	private static glProgramUniform4fvDelegate s_glProgramUniform4fv;
	public static void glProgramUniform4fv(uint program, int location, int count, float *value) => s_glProgramUniform4fv(program, location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform4iDelegate(uint program, int location, int v0, int v1, int v2, int v3);
	private static glProgramUniform4iDelegate s_glProgramUniform4i;
	public static void glProgramUniform4i(uint program, int location, int v0, int v1, int v2, int v3) => s_glProgramUniform4i(program, location, v0, v1, v2, v3);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform4ivDelegate(uint program, int location, int count, int *value);
	private static glProgramUniform4ivDelegate s_glProgramUniform4iv;
	public static void glProgramUniform4iv(uint program, int location, int count, int *value) => s_glProgramUniform4iv(program, location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform4uiDelegate(uint program, int location, uint v0, uint v1, uint v2, uint v3);
	private static glProgramUniform4uiDelegate s_glProgramUniform4ui;
	public static void glProgramUniform4ui(uint program, int location, uint v0, uint v1, uint v2, uint v3) => s_glProgramUniform4ui(program, location, v0, v1, v2, v3);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniform4uivDelegate(uint program, int location, int count, uint *value);
	private static glProgramUniform4uivDelegate s_glProgramUniform4uiv;
	public static void glProgramUniform4uiv(uint program, int location, int count, uint *value) => s_glProgramUniform4uiv(program, location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix2dvDelegate(uint program, int location, int count, bool transpose, double *value);
	private static glProgramUniformMatrix2dvDelegate s_glProgramUniformMatrix2dv;
	public static void glProgramUniformMatrix2dv(uint program, int location, int count, bool transpose, double *value) => s_glProgramUniformMatrix2dv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix2fvDelegate(uint program, int location, int count, bool transpose, float *value);
	private static glProgramUniformMatrix2fvDelegate s_glProgramUniformMatrix2fv;
	public static void glProgramUniformMatrix2fv(uint program, int location, int count, bool transpose, float *value) => s_glProgramUniformMatrix2fv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix2x3dvDelegate(uint program, int location, int count, bool transpose, double *value);
	private static glProgramUniformMatrix2x3dvDelegate s_glProgramUniformMatrix2x3dv;
	public static void glProgramUniformMatrix2x3dv(uint program, int location, int count, bool transpose, double *value) => s_glProgramUniformMatrix2x3dv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix2x3fvDelegate(uint program, int location, int count, bool transpose, float *value);
	private static glProgramUniformMatrix2x3fvDelegate s_glProgramUniformMatrix2x3fv;
	public static void glProgramUniformMatrix2x3fv(uint program, int location, int count, bool transpose, float *value) => s_glProgramUniformMatrix2x3fv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix2x4dvDelegate(uint program, int location, int count, bool transpose, double *value);
	private static glProgramUniformMatrix2x4dvDelegate s_glProgramUniformMatrix2x4dv;
	public static void glProgramUniformMatrix2x4dv(uint program, int location, int count, bool transpose, double *value) => s_glProgramUniformMatrix2x4dv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix2x4fvDelegate(uint program, int location, int count, bool transpose, float *value);
	private static glProgramUniformMatrix2x4fvDelegate s_glProgramUniformMatrix2x4fv;
	public static void glProgramUniformMatrix2x4fv(uint program, int location, int count, bool transpose, float *value) => s_glProgramUniformMatrix2x4fv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix3dvDelegate(uint program, int location, int count, bool transpose, double *value);
	private static glProgramUniformMatrix3dvDelegate s_glProgramUniformMatrix3dv;
	public static void glProgramUniformMatrix3dv(uint program, int location, int count, bool transpose, double *value) => s_glProgramUniformMatrix3dv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix3fvDelegate(uint program, int location, int count, bool transpose, float *value);
	private static glProgramUniformMatrix3fvDelegate s_glProgramUniformMatrix3fv;
	public static void glProgramUniformMatrix3fv(uint program, int location, int count, bool transpose, float *value) => s_glProgramUniformMatrix3fv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix3x2dvDelegate(uint program, int location, int count, bool transpose, double *value);
	private static glProgramUniformMatrix3x2dvDelegate s_glProgramUniformMatrix3x2dv;
	public static void glProgramUniformMatrix3x2dv(uint program, int location, int count, bool transpose, double *value) => s_glProgramUniformMatrix3x2dv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix3x2fvDelegate(uint program, int location, int count, bool transpose, float *value);
	private static glProgramUniformMatrix3x2fvDelegate s_glProgramUniformMatrix3x2fv;
	public static void glProgramUniformMatrix3x2fv(uint program, int location, int count, bool transpose, float *value) => s_glProgramUniformMatrix3x2fv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix3x4dvDelegate(uint program, int location, int count, bool transpose, double *value);
	private static glProgramUniformMatrix3x4dvDelegate s_glProgramUniformMatrix3x4dv;
	public static void glProgramUniformMatrix3x4dv(uint program, int location, int count, bool transpose, double *value) => s_glProgramUniformMatrix3x4dv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix3x4fvDelegate(uint program, int location, int count, bool transpose, float *value);
	private static glProgramUniformMatrix3x4fvDelegate s_glProgramUniformMatrix3x4fv;
	public static void glProgramUniformMatrix3x4fv(uint program, int location, int count, bool transpose, float *value) => s_glProgramUniformMatrix3x4fv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix4dvDelegate(uint program, int location, int count, bool transpose, double *value);
	private static glProgramUniformMatrix4dvDelegate s_glProgramUniformMatrix4dv;
	public static void glProgramUniformMatrix4dv(uint program, int location, int count, bool transpose, double *value) => s_glProgramUniformMatrix4dv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix4fvDelegate(uint program, int location, int count, bool transpose, float *value);
	private static glProgramUniformMatrix4fvDelegate s_glProgramUniformMatrix4fv;
	public static void glProgramUniformMatrix4fv(uint program, int location, int count, bool transpose, float *value) => s_glProgramUniformMatrix4fv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix4x2dvDelegate(uint program, int location, int count, bool transpose, double *value);
	private static glProgramUniformMatrix4x2dvDelegate s_glProgramUniformMatrix4x2dv;
	public static void glProgramUniformMatrix4x2dv(uint program, int location, int count, bool transpose, double *value) => s_glProgramUniformMatrix4x2dv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix4x2fvDelegate(uint program, int location, int count, bool transpose, float *value);
	private static glProgramUniformMatrix4x2fvDelegate s_glProgramUniformMatrix4x2fv;
	public static void glProgramUniformMatrix4x2fv(uint program, int location, int count, bool transpose, float *value) => s_glProgramUniformMatrix4x2fv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix4x3dvDelegate(uint program, int location, int count, bool transpose, double *value);
	private static glProgramUniformMatrix4x3dvDelegate s_glProgramUniformMatrix4x3dv;
	public static void glProgramUniformMatrix4x3dv(uint program, int location, int count, bool transpose, double *value) => s_glProgramUniformMatrix4x3dv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProgramUniformMatrix4x3fvDelegate(uint program, int location, int count, bool transpose, float *value);
	private static glProgramUniformMatrix4x3fvDelegate s_glProgramUniformMatrix4x3fv;
	public static void glProgramUniformMatrix4x3fv(uint program, int location, int count, bool transpose, float *value) => s_glProgramUniformMatrix4x3fv(program, location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glProvokingVertexDelegate(int mode);
	private static glProvokingVertexDelegate s_glProvokingVertex;
	public static void glProvokingVertex(int mode) => s_glProvokingVertex(mode);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glPushDebugGroupDelegate(int source, uint id, int length, char *message);
	private static glPushDebugGroupDelegate s_glPushDebugGroup;
	public static void glPushDebugGroup(int source, uint id, int length, char *message) => s_glPushDebugGroup(source, id, length, message);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glQueryCounterDelegate(uint id, int target);
	private static glQueryCounterDelegate s_glQueryCounter;
	public static void glQueryCounter(uint id, int target) => s_glQueryCounter(id, target);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glReadBufferDelegate(int src);
	private static glReadBufferDelegate s_glReadBuffer;
	public static void glReadBuffer(int src) => s_glReadBuffer(src);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glReadPixelsDelegate(int x, int y, int width, int height, int format, int type, void *pixels);
	private static glReadPixelsDelegate s_glReadPixels;
	public static void glReadPixels(int x, int y, int width, int height, int format, int type, void *pixels) => s_glReadPixels(x, y, width, height, format, type, pixels);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glReadnPixelsDelegate(int x, int y, int width, int height, int format, int type, int bufSize, void *data);
	private static glReadnPixelsDelegate s_glReadnPixels;
	public static void glReadnPixels(int x, int y, int width, int height, int format, int type, int bufSize, void *data) => s_glReadnPixels(x, y, width, height, format, type, bufSize, data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glReleaseShaderCompilerDelegate();
	private static glReleaseShaderCompilerDelegate s_glReleaseShaderCompiler;
	public static void glReleaseShaderCompiler() => s_glReleaseShaderCompiler();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glRenderbufferStorageDelegate(int target, int internalformat, int width, int height);
	private static glRenderbufferStorageDelegate s_glRenderbufferStorage;
	public static void glRenderbufferStorage(int target, int internalformat, int width, int height) => s_glRenderbufferStorage(target, internalformat, width, height);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glRenderbufferStorageMultisampleDelegate(int target, int samples, int internalformat, int width, int height);
	private static glRenderbufferStorageMultisampleDelegate s_glRenderbufferStorageMultisample;
	public static void glRenderbufferStorageMultisample(int target, int samples, int internalformat, int width, int height) => s_glRenderbufferStorageMultisample(target, samples, internalformat, width, height);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glResumeTransformFeedbackDelegate();
	private static glResumeTransformFeedbackDelegate s_glResumeTransformFeedback;
	public static void glResumeTransformFeedback() => s_glResumeTransformFeedback();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glSampleCoverageDelegate(float value, bool invert);
	private static glSampleCoverageDelegate s_glSampleCoverage;
	public static void glSampleCoverage(float value, bool invert) => s_glSampleCoverage(value, invert);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glSampleMaskiDelegate(uint maskNumber, int mask);
	private static glSampleMaskiDelegate s_glSampleMaski;
	public static void glSampleMaski(uint maskNumber, int mask) => s_glSampleMaski(maskNumber, mask);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glSamplerParameterIivDelegate(uint sampler, int pname, int *param);
	private static glSamplerParameterIivDelegate s_glSamplerParameterIiv;
	public static void glSamplerParameterIiv(uint sampler, int pname, int *param) => s_glSamplerParameterIiv(sampler, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glSamplerParameterIuivDelegate(uint sampler, int pname, uint *param);
	private static glSamplerParameterIuivDelegate s_glSamplerParameterIuiv;
	public static void glSamplerParameterIuiv(uint sampler, int pname, uint *param) => s_glSamplerParameterIuiv(sampler, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glSamplerParameterfDelegate(uint sampler, int pname, float param);
	private static glSamplerParameterfDelegate s_glSamplerParameterf;
	public static void glSamplerParameterf(uint sampler, int pname, float param) => s_glSamplerParameterf(sampler, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glSamplerParameterfvDelegate(uint sampler, int pname, float *param);
	private static glSamplerParameterfvDelegate s_glSamplerParameterfv;
	public static void glSamplerParameterfv(uint sampler, int pname, float *param) => s_glSamplerParameterfv(sampler, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glSamplerParameteriDelegate(uint sampler, int pname, int param);
	private static glSamplerParameteriDelegate s_glSamplerParameteri;
	public static void glSamplerParameteri(uint sampler, int pname, int param) => s_glSamplerParameteri(sampler, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glSamplerParameterivDelegate(uint sampler, int pname, int *param);
	private static glSamplerParameterivDelegate s_glSamplerParameteriv;
	public static void glSamplerParameteriv(uint sampler, int pname, int *param) => s_glSamplerParameteriv(sampler, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glScissorDelegate(int x, int y, int width, int height);
	private static glScissorDelegate s_glScissor;
	public static void glScissor(int x, int y, int width, int height) => s_glScissor(x, y, width, height);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glScissorArrayvDelegate(uint first, int count, int *v);
	private static glScissorArrayvDelegate s_glScissorArrayv;
	public static void glScissorArrayv(uint first, int count, int *v) => s_glScissorArrayv(first, count, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glScissorIndexedDelegate(uint index, int left, int bottom, int width, int height);
	private static glScissorIndexedDelegate s_glScissorIndexed;
	public static void glScissorIndexed(uint index, int left, int bottom, int width, int height) => s_glScissorIndexed(index, left, bottom, width, height);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glScissorIndexedvDelegate(uint index, int *v);
	private static glScissorIndexedvDelegate s_glScissorIndexedv;
	public static void glScissorIndexedv(uint index, int *v) => s_glScissorIndexedv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glSecondaryColorP3uiDelegate(int type, uint color);
	private static glSecondaryColorP3uiDelegate s_glSecondaryColorP3ui;
	public static void glSecondaryColorP3ui(int type, uint color) => s_glSecondaryColorP3ui(type, color);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glSecondaryColorP3uivDelegate(int type, uint *color);
	private static glSecondaryColorP3uivDelegate s_glSecondaryColorP3uiv;
	public static void glSecondaryColorP3uiv(int type, uint *color) => s_glSecondaryColorP3uiv(type, color);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glShaderBinaryDelegate(int count, uint *shaders, int binaryFormat, void *binary, int length);
	private static glShaderBinaryDelegate s_glShaderBinary;
	public static void glShaderBinary(int count, uint *shaders, int binaryFormat, void *binary, int length) => s_glShaderBinary(count, shaders, binaryFormat, binary, length);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glShaderSourceDelegate(uint shader, int count, char **str, int *length);
	private static glShaderSourceDelegate s_glShaderSource;
	public static void glShaderSource(uint shader, int count, char **str, int *length) => s_glShaderSource(shader, count, str, length);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glShaderStorageBlockBindingDelegate(uint program, uint storageBlockIndex, uint storageBlockBinding);
	private static glShaderStorageBlockBindingDelegate s_glShaderStorageBlockBinding;
	public static void glShaderStorageBlockBinding(uint program, uint storageBlockIndex, uint storageBlockBinding) => s_glShaderStorageBlockBinding(program, storageBlockIndex, storageBlockBinding);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glSpecializeShaderDelegate(uint shader, char *pEntryPoint, uint numSpecializationConstants, uint *pConstantIndex, uint *pConstantValue);
	private static glSpecializeShaderDelegate s_glSpecializeShader;
	public static void glSpecializeShader(uint shader, char *pEntryPoint, uint numSpecializationConstants, uint *pConstantIndex, uint *pConstantValue) => s_glSpecializeShader(shader, pEntryPoint, numSpecializationConstants, pConstantIndex, pConstantValue);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glStencilFuncDelegate(int func, int reference, uint mask);
	private static glStencilFuncDelegate s_glStencilFunc;
	public static void glStencilFunc(int func, int reference, uint mask) => s_glStencilFunc(func, reference, mask);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glStencilFuncSeparateDelegate(int face, int func, int reference, uint mask);
	private static glStencilFuncSeparateDelegate s_glStencilFuncSeparate;
	public static void glStencilFuncSeparate(int face, int func, int reference, uint mask) => s_glStencilFuncSeparate(face, func, reference, mask);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glStencilMaskDelegate(uint mask);
	private static glStencilMaskDelegate s_glStencilMask;
	public static void glStencilMask(uint mask) => s_glStencilMask(mask);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glStencilMaskSeparateDelegate(int face, uint mask);
	private static glStencilMaskSeparateDelegate s_glStencilMaskSeparate;
	public static void glStencilMaskSeparate(int face, uint mask) => s_glStencilMaskSeparate(face, mask);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glStencilOpDelegate(int fail, int zfail, int zpass);
	private static glStencilOpDelegate s_glStencilOp;
	public static void glStencilOp(int fail, int zfail, int zpass) => s_glStencilOp(fail, zfail, zpass);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glStencilOpSeparateDelegate(int face, int sfail, int dpfail, int dppass);
	private static glStencilOpSeparateDelegate s_glStencilOpSeparate;
	public static void glStencilOpSeparate(int face, int sfail, int dpfail, int dppass) => s_glStencilOpSeparate(face, sfail, dpfail, dppass);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexBufferDelegate(int target, int internalformat, uint buffer);
	private static glTexBufferDelegate s_glTexBuffer;
	public static void glTexBuffer(int target, int internalformat, uint buffer) => s_glTexBuffer(target, internalformat, buffer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexBufferRangeDelegate(int target, int internalformat, uint buffer, IntPtr offset, IntPtr size);
	private static glTexBufferRangeDelegate s_glTexBufferRange;
	public static void glTexBufferRange(int target, int internalformat, uint buffer, IntPtr offset, IntPtr size) => s_glTexBufferRange(target, internalformat, buffer, offset, size);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexCoordP1uiDelegate(int type, uint coords);
	private static glTexCoordP1uiDelegate s_glTexCoordP1ui;
	public static void glTexCoordP1ui(int type, uint coords) => s_glTexCoordP1ui(type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexCoordP1uivDelegate(int type, uint *coords);
	private static glTexCoordP1uivDelegate s_glTexCoordP1uiv;
	public static void glTexCoordP1uiv(int type, uint *coords) => s_glTexCoordP1uiv(type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexCoordP2uiDelegate(int type, uint coords);
	private static glTexCoordP2uiDelegate s_glTexCoordP2ui;
	public static void glTexCoordP2ui(int type, uint coords) => s_glTexCoordP2ui(type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexCoordP2uivDelegate(int type, uint *coords);
	private static glTexCoordP2uivDelegate s_glTexCoordP2uiv;
	public static void glTexCoordP2uiv(int type, uint *coords) => s_glTexCoordP2uiv(type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexCoordP3uiDelegate(int type, uint coords);
	private static glTexCoordP3uiDelegate s_glTexCoordP3ui;
	public static void glTexCoordP3ui(int type, uint coords) => s_glTexCoordP3ui(type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexCoordP3uivDelegate(int type, uint *coords);
	private static glTexCoordP3uivDelegate s_glTexCoordP3uiv;
	public static void glTexCoordP3uiv(int type, uint *coords) => s_glTexCoordP3uiv(type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexCoordP4uiDelegate(int type, uint coords);
	private static glTexCoordP4uiDelegate s_glTexCoordP4ui;
	public static void glTexCoordP4ui(int type, uint coords) => s_glTexCoordP4ui(type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexCoordP4uivDelegate(int type, uint *coords);
	private static glTexCoordP4uivDelegate s_glTexCoordP4uiv;
	public static void glTexCoordP4uiv(int type, uint *coords) => s_glTexCoordP4uiv(type, coords);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexImage1DDelegate(int target, int level, int internalformat, int width, int border, int format, int type, void *pixels);
	private static glTexImage1DDelegate s_glTexImage1D;
	public static void glTexImage1D(int target, int level, int internalformat, int width, int border, int format, int type, void *pixels) => s_glTexImage1D(target, level, internalformat, width, border, format, type, pixels);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexImage2DDelegate(int target, int level, int internalformat, int width, int height, int border, int format, int type, void *pixels);
	private static glTexImage2DDelegate s_glTexImage2D;
	public static void glTexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, void *pixels) => s_glTexImage2D(target, level, internalformat, width, height, border, format, type, pixels);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexImage2DMultisampleDelegate(int target, int samples, int internalformat, int width, int height, bool fixedsamplelocations);
	private static glTexImage2DMultisampleDelegate s_glTexImage2DMultisample;
	public static void glTexImage2DMultisample(int target, int samples, int internalformat, int width, int height, bool fixedsamplelocations) => s_glTexImage2DMultisample(target, samples, internalformat, width, height, fixedsamplelocations);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexImage3DDelegate(int target, int level, int internalformat, int width, int height, int depth, int border, int format, int type, void *pixels);
	private static glTexImage3DDelegate s_glTexImage3D;
	public static void glTexImage3D(int target, int level, int internalformat, int width, int height, int depth, int border, int format, int type, void *pixels) => s_glTexImage3D(target, level, internalformat, width, height, depth, border, format, type, pixels);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexImage3DMultisampleDelegate(int target, int samples, int internalformat, int width, int height, int depth, bool fixedsamplelocations);
	private static glTexImage3DMultisampleDelegate s_glTexImage3DMultisample;
	public static void glTexImage3DMultisample(int target, int samples, int internalformat, int width, int height, int depth, bool fixedsamplelocations) => s_glTexImage3DMultisample(target, samples, internalformat, width, height, depth, fixedsamplelocations);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexParameterIivDelegate(int target, int pname, int *args);
	private static glTexParameterIivDelegate s_glTexParameterIiv;
	public static void glTexParameterIiv(int target, int pname, int *args) => s_glTexParameterIiv(target, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexParameterIuivDelegate(int target, int pname, uint *args);
	private static glTexParameterIuivDelegate s_glTexParameterIuiv;
	public static void glTexParameterIuiv(int target, int pname, uint *args) => s_glTexParameterIuiv(target, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexParameterfDelegate(int target, int pname, float param);
	private static glTexParameterfDelegate s_glTexParameterf;
	public static void glTexParameterf(int target, int pname, float param) => s_glTexParameterf(target, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexParameterfvDelegate(int target, int pname, float *args);
	private static glTexParameterfvDelegate s_glTexParameterfv;
	public static void glTexParameterfv(int target, int pname, float *args) => s_glTexParameterfv(target, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexParameteriDelegate(int target, int pname, int param);
	private static glTexParameteriDelegate s_glTexParameteri;
	public static void glTexParameteri(int target, int pname, int param) => s_glTexParameteri(target, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexParameterivDelegate(int target, int pname, int *args);
	private static glTexParameterivDelegate s_glTexParameteriv;
	public static void glTexParameteriv(int target, int pname, int *args) => s_glTexParameteriv(target, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexStorage1DDelegate(int target, int levels, int internalformat, int width);
	private static glTexStorage1DDelegate s_glTexStorage1D;
	public static void glTexStorage1D(int target, int levels, int internalformat, int width) => s_glTexStorage1D(target, levels, internalformat, width);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexStorage2DDelegate(int target, int levels, int internalformat, int width, int height);
	private static glTexStorage2DDelegate s_glTexStorage2D;
	public static void glTexStorage2D(int target, int levels, int internalformat, int width, int height) => s_glTexStorage2D(target, levels, internalformat, width, height);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexStorage2DMultisampleDelegate(int target, int samples, int internalformat, int width, int height, bool fixedsamplelocations);
	private static glTexStorage2DMultisampleDelegate s_glTexStorage2DMultisample;
	public static void glTexStorage2DMultisample(int target, int samples, int internalformat, int width, int height, bool fixedsamplelocations) => s_glTexStorage2DMultisample(target, samples, internalformat, width, height, fixedsamplelocations);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexStorage3DDelegate(int target, int levels, int internalformat, int width, int height, int depth);
	private static glTexStorage3DDelegate s_glTexStorage3D;
	public static void glTexStorage3D(int target, int levels, int internalformat, int width, int height, int depth) => s_glTexStorage3D(target, levels, internalformat, width, height, depth);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexStorage3DMultisampleDelegate(int target, int samples, int internalformat, int width, int height, int depth, bool fixedsamplelocations);
	private static glTexStorage3DMultisampleDelegate s_glTexStorage3DMultisample;
	public static void glTexStorage3DMultisample(int target, int samples, int internalformat, int width, int height, int depth, bool fixedsamplelocations) => s_glTexStorage3DMultisample(target, samples, internalformat, width, height, depth, fixedsamplelocations);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexSubImage1DDelegate(int target, int level, int xoffset, int width, int format, int type, void *pixels);
	private static glTexSubImage1DDelegate s_glTexSubImage1D;
	public static void glTexSubImage1D(int target, int level, int xoffset, int width, int format, int type, void *pixels) => s_glTexSubImage1D(target, level, xoffset, width, format, type, pixels);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexSubImage2DDelegate(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, void *pixels);
	private static glTexSubImage2DDelegate s_glTexSubImage2D;
	public static void glTexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, void *pixels) => s_glTexSubImage2D(target, level, xoffset, yoffset, width, height, format, type, pixels);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTexSubImage3DDelegate(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, void *pixels);
	private static glTexSubImage3DDelegate s_glTexSubImage3D;
	public static void glTexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, void *pixels) => s_glTexSubImage3D(target, level, xoffset, yoffset, zoffset, width, height, depth, format, type, pixels);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureBarrierDelegate();
	private static glTextureBarrierDelegate s_glTextureBarrier;
	public static void glTextureBarrier() => s_glTextureBarrier();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureBufferDelegate(uint texture, int internalformat, uint buffer);
	private static glTextureBufferDelegate s_glTextureBuffer;
	public static void glTextureBuffer(uint texture, int internalformat, uint buffer) => s_glTextureBuffer(texture, internalformat, buffer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureBufferRangeDelegate(uint texture, int internalformat, uint buffer, IntPtr offset, IntPtr size);
	private static glTextureBufferRangeDelegate s_glTextureBufferRange;
	public static void glTextureBufferRange(uint texture, int internalformat, uint buffer, IntPtr offset, IntPtr size) => s_glTextureBufferRange(texture, internalformat, buffer, offset, size);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureParameterIivDelegate(uint texture, int pname, int *args);
	private static glTextureParameterIivDelegate s_glTextureParameterIiv;
	public static void glTextureParameterIiv(uint texture, int pname, int *args) => s_glTextureParameterIiv(texture, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureParameterIuivDelegate(uint texture, int pname, uint *args);
	private static glTextureParameterIuivDelegate s_glTextureParameterIuiv;
	public static void glTextureParameterIuiv(uint texture, int pname, uint *args) => s_glTextureParameterIuiv(texture, pname, args);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureParameterfDelegate(uint texture, int pname, float param);
	private static glTextureParameterfDelegate s_glTextureParameterf;
	public static void glTextureParameterf(uint texture, int pname, float param) => s_glTextureParameterf(texture, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureParameterfvDelegate(uint texture, int pname, float *param);
	private static glTextureParameterfvDelegate s_glTextureParameterfv;
	public static void glTextureParameterfv(uint texture, int pname, float *param) => s_glTextureParameterfv(texture, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureParameteriDelegate(uint texture, int pname, int param);
	private static glTextureParameteriDelegate s_glTextureParameteri;
	public static void glTextureParameteri(uint texture, int pname, int param) => s_glTextureParameteri(texture, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureParameterivDelegate(uint texture, int pname, int *param);
	private static glTextureParameterivDelegate s_glTextureParameteriv;
	public static void glTextureParameteriv(uint texture, int pname, int *param) => s_glTextureParameteriv(texture, pname, param);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureStorage1DDelegate(uint texture, int levels, int internalformat, int width);
	private static glTextureStorage1DDelegate s_glTextureStorage1D;
	public static void glTextureStorage1D(uint texture, int levels, int internalformat, int width) => s_glTextureStorage1D(texture, levels, internalformat, width);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureStorage2DDelegate(uint texture, int levels, int internalformat, int width, int height);
	private static glTextureStorage2DDelegate s_glTextureStorage2D;
	public static void glTextureStorage2D(uint texture, int levels, int internalformat, int width, int height) => s_glTextureStorage2D(texture, levels, internalformat, width, height);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureStorage2DMultisampleDelegate(uint texture, int samples, int internalformat, int width, int height, bool fixedsamplelocations);
	private static glTextureStorage2DMultisampleDelegate s_glTextureStorage2DMultisample;
	public static void glTextureStorage2DMultisample(uint texture, int samples, int internalformat, int width, int height, bool fixedsamplelocations) => s_glTextureStorage2DMultisample(texture, samples, internalformat, width, height, fixedsamplelocations);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureStorage3DDelegate(uint texture, int levels, int internalformat, int width, int height, int depth);
	private static glTextureStorage3DDelegate s_glTextureStorage3D;
	public static void glTextureStorage3D(uint texture, int levels, int internalformat, int width, int height, int depth) => s_glTextureStorage3D(texture, levels, internalformat, width, height, depth);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureStorage3DMultisampleDelegate(uint texture, int samples, int internalformat, int width, int height, int depth, bool fixedsamplelocations);
	private static glTextureStorage3DMultisampleDelegate s_glTextureStorage3DMultisample;
	public static void glTextureStorage3DMultisample(uint texture, int samples, int internalformat, int width, int height, int depth, bool fixedsamplelocations) => s_glTextureStorage3DMultisample(texture, samples, internalformat, width, height, depth, fixedsamplelocations);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureSubImage1DDelegate(uint texture, int level, int xoffset, int width, int format, int type, void *pixels);
	private static glTextureSubImage1DDelegate s_glTextureSubImage1D;
	public static void glTextureSubImage1D(uint texture, int level, int xoffset, int width, int format, int type, void *pixels) => s_glTextureSubImage1D(texture, level, xoffset, width, format, type, pixels);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureSubImage2DDelegate(uint texture, int level, int xoffset, int yoffset, int width, int height, int format, int type, void *pixels);
	private static glTextureSubImage2DDelegate s_glTextureSubImage2D;
	public static void glTextureSubImage2D(uint texture, int level, int xoffset, int yoffset, int width, int height, int format, int type, void *pixels) => s_glTextureSubImage2D(texture, level, xoffset, yoffset, width, height, format, type, pixels);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureSubImage3DDelegate(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, void *pixels);
	private static glTextureSubImage3DDelegate s_glTextureSubImage3D;
	public static void glTextureSubImage3D(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, void *pixels) => s_glTextureSubImage3D(texture, level, xoffset, yoffset, zoffset, width, height, depth, format, type, pixels);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTextureViewDelegate(uint texture, int target, uint origtexture, int internalformat, uint minlevel, uint numlevels, uint minlayer, uint numlayers);
	private static glTextureViewDelegate s_glTextureView;
	public static void glTextureView(uint texture, int target, uint origtexture, int internalformat, uint minlevel, uint numlevels, uint minlayer, uint numlayers) => s_glTextureView(texture, target, origtexture, internalformat, minlevel, numlevels, minlayer, numlayers);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTransformFeedbackBufferBaseDelegate(uint xfb, uint index, uint buffer);
	private static glTransformFeedbackBufferBaseDelegate s_glTransformFeedbackBufferBase;
	public static void glTransformFeedbackBufferBase(uint xfb, uint index, uint buffer) => s_glTransformFeedbackBufferBase(xfb, index, buffer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTransformFeedbackBufferRangeDelegate(uint xfb, uint index, uint buffer, IntPtr offset, IntPtr size);
	private static glTransformFeedbackBufferRangeDelegate s_glTransformFeedbackBufferRange;
	public static void glTransformFeedbackBufferRange(uint xfb, uint index, uint buffer, IntPtr offset, IntPtr size) => s_glTransformFeedbackBufferRange(xfb, index, buffer, offset, size);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glTransformFeedbackVaryingsDelegate(uint program, int count, char **varyings, int bufferMode);
	private static glTransformFeedbackVaryingsDelegate s_glTransformFeedbackVaryings;
	public static void glTransformFeedbackVaryings(uint program, int count, char **varyings, int bufferMode) => s_glTransformFeedbackVaryings(program, count, varyings, bufferMode);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform1dDelegate(int location, double x);
	private static glUniform1dDelegate s_glUniform1d;
	public static void glUniform1d(int location, double x) => s_glUniform1d(location, x);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform1dvDelegate(int location, int count, double *value);
	private static glUniform1dvDelegate s_glUniform1dv;
	public static void glUniform1dv(int location, int count, double *value) => s_glUniform1dv(location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform1fDelegate(int location, float v0);
	private static glUniform1fDelegate s_glUniform1f;
	public static void glUniform1f(int location, float v0) => s_glUniform1f(location, v0);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform1fvDelegate(int location, int count, float *value);
	private static glUniform1fvDelegate s_glUniform1fv;
	public static void glUniform1fv(int location, int count, float *value) => s_glUniform1fv(location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform1iDelegate(int location, int v0);
	private static glUniform1iDelegate s_glUniform1i;
	public static void glUniform1i(int location, int v0) => s_glUniform1i(location, v0);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform1ivDelegate(int location, int count, int *value);
	private static glUniform1ivDelegate s_glUniform1iv;
	public static void glUniform1iv(int location, int count, int *value) => s_glUniform1iv(location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform1uiDelegate(int location, uint v0);
	private static glUniform1uiDelegate s_glUniform1ui;
	public static void glUniform1ui(int location, uint v0) => s_glUniform1ui(location, v0);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform1uivDelegate(int location, int count, uint *value);
	private static glUniform1uivDelegate s_glUniform1uiv;
	public static void glUniform1uiv(int location, int count, uint *value) => s_glUniform1uiv(location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform2dDelegate(int location, double x, double y);
	private static glUniform2dDelegate s_glUniform2d;
	public static void glUniform2d(int location, double x, double y) => s_glUniform2d(location, x, y);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform2dvDelegate(int location, int count, double *value);
	private static glUniform2dvDelegate s_glUniform2dv;
	public static void glUniform2dv(int location, int count, double *value) => s_glUniform2dv(location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform2fDelegate(int location, float v0, float v1);
	private static glUniform2fDelegate s_glUniform2f;
	public static void glUniform2f(int location, float v0, float v1) => s_glUniform2f(location, v0, v1);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform2fvDelegate(int location, int count, float *value);
	private static glUniform2fvDelegate s_glUniform2fv;
	public static void glUniform2fv(int location, int count, float *value) => s_glUniform2fv(location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform2iDelegate(int location, int v0, int v1);
	private static glUniform2iDelegate s_glUniform2i;
	public static void glUniform2i(int location, int v0, int v1) => s_glUniform2i(location, v0, v1);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform2ivDelegate(int location, int count, int *value);
	private static glUniform2ivDelegate s_glUniform2iv;
	public static void glUniform2iv(int location, int count, int *value) => s_glUniform2iv(location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform2uiDelegate(int location, uint v0, uint v1);
	private static glUniform2uiDelegate s_glUniform2ui;
	public static void glUniform2ui(int location, uint v0, uint v1) => s_glUniform2ui(location, v0, v1);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform2uivDelegate(int location, int count, uint *value);
	private static glUniform2uivDelegate s_glUniform2uiv;
	public static void glUniform2uiv(int location, int count, uint *value) => s_glUniform2uiv(location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform3dDelegate(int location, double x, double y, double z);
	private static glUniform3dDelegate s_glUniform3d;
	public static void glUniform3d(int location, double x, double y, double z) => s_glUniform3d(location, x, y, z);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform3dvDelegate(int location, int count, double *value);
	private static glUniform3dvDelegate s_glUniform3dv;
	public static void glUniform3dv(int location, int count, double *value) => s_glUniform3dv(location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform3fDelegate(int location, float v0, float v1, float v2);
	private static glUniform3fDelegate s_glUniform3f;
	public static void glUniform3f(int location, float v0, float v1, float v2) => s_glUniform3f(location, v0, v1, v2);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform3fvDelegate(int location, int count, float *value);
	private static glUniform3fvDelegate s_glUniform3fv;
	public static void glUniform3fv(int location, int count, float *value) => s_glUniform3fv(location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform3iDelegate(int location, int v0, int v1, int v2);
	private static glUniform3iDelegate s_glUniform3i;
	public static void glUniform3i(int location, int v0, int v1, int v2) => s_glUniform3i(location, v0, v1, v2);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform3ivDelegate(int location, int count, int *value);
	private static glUniform3ivDelegate s_glUniform3iv;
	public static void glUniform3iv(int location, int count, int *value) => s_glUniform3iv(location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform3uiDelegate(int location, uint v0, uint v1, uint v2);
	private static glUniform3uiDelegate s_glUniform3ui;
	public static void glUniform3ui(int location, uint v0, uint v1, uint v2) => s_glUniform3ui(location, v0, v1, v2);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform3uivDelegate(int location, int count, uint *value);
	private static glUniform3uivDelegate s_glUniform3uiv;
	public static void glUniform3uiv(int location, int count, uint *value) => s_glUniform3uiv(location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform4dDelegate(int location, double x, double y, double z, double w);
	private static glUniform4dDelegate s_glUniform4d;
	public static void glUniform4d(int location, double x, double y, double z, double w) => s_glUniform4d(location, x, y, z, w);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform4dvDelegate(int location, int count, double *value);
	private static glUniform4dvDelegate s_glUniform4dv;
	public static void glUniform4dv(int location, int count, double *value) => s_glUniform4dv(location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform4fDelegate(int location, float v0, float v1, float v2, float v3);
	private static glUniform4fDelegate s_glUniform4f;
	public static void glUniform4f(int location, float v0, float v1, float v2, float v3) => s_glUniform4f(location, v0, v1, v2, v3);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform4fvDelegate(int location, int count, float *value);
	private static glUniform4fvDelegate s_glUniform4fv;
	public static void glUniform4fv(int location, int count, float *value) => s_glUniform4fv(location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform4iDelegate(int location, int v0, int v1, int v2, int v3);
	private static glUniform4iDelegate s_glUniform4i;
	public static void glUniform4i(int location, int v0, int v1, int v2, int v3) => s_glUniform4i(location, v0, v1, v2, v3);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform4ivDelegate(int location, int count, int *value);
	private static glUniform4ivDelegate s_glUniform4iv;
	public static void glUniform4iv(int location, int count, int *value) => s_glUniform4iv(location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform4uiDelegate(int location, uint v0, uint v1, uint v2, uint v3);
	private static glUniform4uiDelegate s_glUniform4ui;
	public static void glUniform4ui(int location, uint v0, uint v1, uint v2, uint v3) => s_glUniform4ui(location, v0, v1, v2, v3);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniform4uivDelegate(int location, int count, uint *value);
	private static glUniform4uivDelegate s_glUniform4uiv;
	public static void glUniform4uiv(int location, int count, uint *value) => s_glUniform4uiv(location, count, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformBlockBindingDelegate(uint program, uint uniformBlockIndex, uint uniformBlockBinding);
	private static glUniformBlockBindingDelegate s_glUniformBlockBinding;
	public static void glUniformBlockBinding(uint program, uint uniformBlockIndex, uint uniformBlockBinding) => s_glUniformBlockBinding(program, uniformBlockIndex, uniformBlockBinding);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix2dvDelegate(int location, int count, bool transpose, double *value);
	private static glUniformMatrix2dvDelegate s_glUniformMatrix2dv;
	public static void glUniformMatrix2dv(int location, int count, bool transpose, double *value) => s_glUniformMatrix2dv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix2fvDelegate(int location, int count, bool transpose, float *value);
	private static glUniformMatrix2fvDelegate s_glUniformMatrix2fv;
	public static void glUniformMatrix2fv(int location, int count, bool transpose, float *value) => s_glUniformMatrix2fv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix2x3dvDelegate(int location, int count, bool transpose, double *value);
	private static glUniformMatrix2x3dvDelegate s_glUniformMatrix2x3dv;
	public static void glUniformMatrix2x3dv(int location, int count, bool transpose, double *value) => s_glUniformMatrix2x3dv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix2x3fvDelegate(int location, int count, bool transpose, float *value);
	private static glUniformMatrix2x3fvDelegate s_glUniformMatrix2x3fv;
	public static void glUniformMatrix2x3fv(int location, int count, bool transpose, float *value) => s_glUniformMatrix2x3fv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix2x4dvDelegate(int location, int count, bool transpose, double *value);
	private static glUniformMatrix2x4dvDelegate s_glUniformMatrix2x4dv;
	public static void glUniformMatrix2x4dv(int location, int count, bool transpose, double *value) => s_glUniformMatrix2x4dv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix2x4fvDelegate(int location, int count, bool transpose, float *value);
	private static glUniformMatrix2x4fvDelegate s_glUniformMatrix2x4fv;
	public static void glUniformMatrix2x4fv(int location, int count, bool transpose, float *value) => s_glUniformMatrix2x4fv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix3dvDelegate(int location, int count, bool transpose, double *value);
	private static glUniformMatrix3dvDelegate s_glUniformMatrix3dv;
	public static void glUniformMatrix3dv(int location, int count, bool transpose, double *value) => s_glUniformMatrix3dv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix3fvDelegate(int location, int count, bool transpose, float *value);
	private static glUniformMatrix3fvDelegate s_glUniformMatrix3fv;
	public static void glUniformMatrix3fv(int location, int count, bool transpose, float *value) => s_glUniformMatrix3fv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix3x2dvDelegate(int location, int count, bool transpose, double *value);
	private static glUniformMatrix3x2dvDelegate s_glUniformMatrix3x2dv;
	public static void glUniformMatrix3x2dv(int location, int count, bool transpose, double *value) => s_glUniformMatrix3x2dv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix3x2fvDelegate(int location, int count, bool transpose, float *value);
	private static glUniformMatrix3x2fvDelegate s_glUniformMatrix3x2fv;
	public static void glUniformMatrix3x2fv(int location, int count, bool transpose, float *value) => s_glUniformMatrix3x2fv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix3x4dvDelegate(int location, int count, bool transpose, double *value);
	private static glUniformMatrix3x4dvDelegate s_glUniformMatrix3x4dv;
	public static void glUniformMatrix3x4dv(int location, int count, bool transpose, double *value) => s_glUniformMatrix3x4dv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix3x4fvDelegate(int location, int count, bool transpose, float *value);
	private static glUniformMatrix3x4fvDelegate s_glUniformMatrix3x4fv;
	public static void glUniformMatrix3x4fv(int location, int count, bool transpose, float *value) => s_glUniformMatrix3x4fv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix4dvDelegate(int location, int count, bool transpose, double *value);
	private static glUniformMatrix4dvDelegate s_glUniformMatrix4dv;
	public static void glUniformMatrix4dv(int location, int count, bool transpose, double *value) => s_glUniformMatrix4dv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix4fvDelegate(int location, int count, bool transpose, float *value);
	private static glUniformMatrix4fvDelegate s_glUniformMatrix4fv;
	public static void glUniformMatrix4fv(int location, int count, bool transpose, float *value) => s_glUniformMatrix4fv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix4x2dvDelegate(int location, int count, bool transpose, double *value);
	private static glUniformMatrix4x2dvDelegate s_glUniformMatrix4x2dv;
	public static void glUniformMatrix4x2dv(int location, int count, bool transpose, double *value) => s_glUniformMatrix4x2dv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix4x2fvDelegate(int location, int count, bool transpose, float *value);
	private static glUniformMatrix4x2fvDelegate s_glUniformMatrix4x2fv;
	public static void glUniformMatrix4x2fv(int location, int count, bool transpose, float *value) => s_glUniformMatrix4x2fv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix4x3dvDelegate(int location, int count, bool transpose, double *value);
	private static glUniformMatrix4x3dvDelegate s_glUniformMatrix4x3dv;
	public static void glUniformMatrix4x3dv(int location, int count, bool transpose, double *value) => s_glUniformMatrix4x3dv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformMatrix4x3fvDelegate(int location, int count, bool transpose, float *value);
	private static glUniformMatrix4x3fvDelegate s_glUniformMatrix4x3fv;
	public static void glUniformMatrix4x3fv(int location, int count, bool transpose, float *value) => s_glUniformMatrix4x3fv(location, count, transpose, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUniformSubroutinesuivDelegate(int shadertype, int count, uint *indices);
	private static glUniformSubroutinesuivDelegate s_glUniformSubroutinesuiv;
	public static void glUniformSubroutinesuiv(int shadertype, int count, uint *indices) => s_glUniformSubroutinesuiv(shadertype, count, indices);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool glUnmapBufferDelegate(int target);
	private static glUnmapBufferDelegate s_glUnmapBuffer;
	public static bool glUnmapBuffer(int target) => s_glUnmapBuffer(target);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool glUnmapNamedBufferDelegate(uint buffer);
	private static glUnmapNamedBufferDelegate s_glUnmapNamedBuffer;
	public static bool glUnmapNamedBuffer(uint buffer) => s_glUnmapNamedBuffer(buffer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUseProgramDelegate(uint program);
	private static glUseProgramDelegate s_glUseProgram;
	public static void glUseProgram(uint program) => s_glUseProgram(program);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glUseProgramStagesDelegate(uint pipeline, int stages, uint program);
	private static glUseProgramStagesDelegate s_glUseProgramStages;
	public static void glUseProgramStages(uint pipeline, int stages, uint program) => s_glUseProgramStages(pipeline, stages, program);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glValidateProgramDelegate(uint program);
	private static glValidateProgramDelegate s_glValidateProgram;
	public static void glValidateProgram(uint program) => s_glValidateProgram(program);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glValidateProgramPipelineDelegate(uint pipeline);
	private static glValidateProgramPipelineDelegate s_glValidateProgramPipeline;
	public static void glValidateProgramPipeline(uint pipeline) => s_glValidateProgramPipeline(pipeline);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexArrayAttribBindingDelegate(uint vaobj, uint attribindex, uint bindingindex);
	private static glVertexArrayAttribBindingDelegate s_glVertexArrayAttribBinding;
	public static void glVertexArrayAttribBinding(uint vaobj, uint attribindex, uint bindingindex) => s_glVertexArrayAttribBinding(vaobj, attribindex, bindingindex);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexArrayAttribFormatDelegate(uint vaobj, uint attribindex, int size, int type, bool normalized, uint relativeoffset);
	private static glVertexArrayAttribFormatDelegate s_glVertexArrayAttribFormat;
	public static void glVertexArrayAttribFormat(uint vaobj, uint attribindex, int size, int type, bool normalized, uint relativeoffset) => s_glVertexArrayAttribFormat(vaobj, attribindex, size, type, normalized, relativeoffset);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexArrayAttribIFormatDelegate(uint vaobj, uint attribindex, int size, int type, uint relativeoffset);
	private static glVertexArrayAttribIFormatDelegate s_glVertexArrayAttribIFormat;
	public static void glVertexArrayAttribIFormat(uint vaobj, uint attribindex, int size, int type, uint relativeoffset) => s_glVertexArrayAttribIFormat(vaobj, attribindex, size, type, relativeoffset);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexArrayAttribLFormatDelegate(uint vaobj, uint attribindex, int size, int type, uint relativeoffset);
	private static glVertexArrayAttribLFormatDelegate s_glVertexArrayAttribLFormat;
	public static void glVertexArrayAttribLFormat(uint vaobj, uint attribindex, int size, int type, uint relativeoffset) => s_glVertexArrayAttribLFormat(vaobj, attribindex, size, type, relativeoffset);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexArrayBindingDivisorDelegate(uint vaobj, uint bindingindex, uint divisor);
	private static glVertexArrayBindingDivisorDelegate s_glVertexArrayBindingDivisor;
	public static void glVertexArrayBindingDivisor(uint vaobj, uint bindingindex, uint divisor) => s_glVertexArrayBindingDivisor(vaobj, bindingindex, divisor);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexArrayElementBufferDelegate(uint vaobj, uint buffer);
	private static glVertexArrayElementBufferDelegate s_glVertexArrayElementBuffer;
	public static void glVertexArrayElementBuffer(uint vaobj, uint buffer) => s_glVertexArrayElementBuffer(vaobj, buffer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexArrayVertexBufferDelegate(uint vaobj, uint bindingindex, uint buffer, IntPtr offset, int stride);
	private static glVertexArrayVertexBufferDelegate s_glVertexArrayVertexBuffer;
	public static void glVertexArrayVertexBuffer(uint vaobj, uint bindingindex, uint buffer, IntPtr offset, int stride) => s_glVertexArrayVertexBuffer(vaobj, bindingindex, buffer, offset, stride);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexArrayVertexBuffersDelegate(uint vaobj, uint first, int count, uint *buffers, IntPtr *offsets, int *strides);
	private static glVertexArrayVertexBuffersDelegate s_glVertexArrayVertexBuffers;
	public static void glVertexArrayVertexBuffers(uint vaobj, uint first, int count, uint *buffers, IntPtr *offsets, int *strides) => s_glVertexArrayVertexBuffers(vaobj, first, count, buffers, offsets, strides);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib1dDelegate(uint index, double x);
	private static glVertexAttrib1dDelegate s_glVertexAttrib1d;
	public static void glVertexAttrib1d(uint index, double x) => s_glVertexAttrib1d(index, x);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib1dvDelegate(uint index, double *v);
	private static glVertexAttrib1dvDelegate s_glVertexAttrib1dv;
	public static void glVertexAttrib1dv(uint index, double *v) => s_glVertexAttrib1dv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib1fDelegate(uint index, float x);
	private static glVertexAttrib1fDelegate s_glVertexAttrib1f;
	public static void glVertexAttrib1f(uint index, float x) => s_glVertexAttrib1f(index, x);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib1fvDelegate(uint index, float *v);
	private static glVertexAttrib1fvDelegate s_glVertexAttrib1fv;
	public static void glVertexAttrib1fv(uint index, float *v) => s_glVertexAttrib1fv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib1sDelegate(uint index, short x);
	private static glVertexAttrib1sDelegate s_glVertexAttrib1s;
	public static void glVertexAttrib1s(uint index, short x) => s_glVertexAttrib1s(index, x);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib1svDelegate(uint index, short *v);
	private static glVertexAttrib1svDelegate s_glVertexAttrib1sv;
	public static void glVertexAttrib1sv(uint index, short *v) => s_glVertexAttrib1sv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib2dDelegate(uint index, double x, double y);
	private static glVertexAttrib2dDelegate s_glVertexAttrib2d;
	public static void glVertexAttrib2d(uint index, double x, double y) => s_glVertexAttrib2d(index, x, y);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib2dvDelegate(uint index, double *v);
	private static glVertexAttrib2dvDelegate s_glVertexAttrib2dv;
	public static void glVertexAttrib2dv(uint index, double *v) => s_glVertexAttrib2dv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib2fDelegate(uint index, float x, float y);
	private static glVertexAttrib2fDelegate s_glVertexAttrib2f;
	public static void glVertexAttrib2f(uint index, float x, float y) => s_glVertexAttrib2f(index, x, y);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib2fvDelegate(uint index, float *v);
	private static glVertexAttrib2fvDelegate s_glVertexAttrib2fv;
	public static void glVertexAttrib2fv(uint index, float *v) => s_glVertexAttrib2fv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib2sDelegate(uint index, short x, short y);
	private static glVertexAttrib2sDelegate s_glVertexAttrib2s;
	public static void glVertexAttrib2s(uint index, short x, short y) => s_glVertexAttrib2s(index, x, y);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib2svDelegate(uint index, short *v);
	private static glVertexAttrib2svDelegate s_glVertexAttrib2sv;
	public static void glVertexAttrib2sv(uint index, short *v) => s_glVertexAttrib2sv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib3dDelegate(uint index, double x, double y, double z);
	private static glVertexAttrib3dDelegate s_glVertexAttrib3d;
	public static void glVertexAttrib3d(uint index, double x, double y, double z) => s_glVertexAttrib3d(index, x, y, z);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib3dvDelegate(uint index, double *v);
	private static glVertexAttrib3dvDelegate s_glVertexAttrib3dv;
	public static void glVertexAttrib3dv(uint index, double *v) => s_glVertexAttrib3dv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib3fDelegate(uint index, float x, float y, float z);
	private static glVertexAttrib3fDelegate s_glVertexAttrib3f;
	public static void glVertexAttrib3f(uint index, float x, float y, float z) => s_glVertexAttrib3f(index, x, y, z);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib3fvDelegate(uint index, float *v);
	private static glVertexAttrib3fvDelegate s_glVertexAttrib3fv;
	public static void glVertexAttrib3fv(uint index, float *v) => s_glVertexAttrib3fv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib3sDelegate(uint index, short x, short y, short z);
	private static glVertexAttrib3sDelegate s_glVertexAttrib3s;
	public static void glVertexAttrib3s(uint index, short x, short y, short z) => s_glVertexAttrib3s(index, x, y, z);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib3svDelegate(uint index, short *v);
	private static glVertexAttrib3svDelegate s_glVertexAttrib3sv;
	public static void glVertexAttrib3sv(uint index, short *v) => s_glVertexAttrib3sv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4NbvDelegate(uint index, sbyte *v);
	private static glVertexAttrib4NbvDelegate s_glVertexAttrib4Nbv;
	public static void glVertexAttrib4Nbv(uint index, sbyte *v) => s_glVertexAttrib4Nbv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4NivDelegate(uint index, int *v);
	private static glVertexAttrib4NivDelegate s_glVertexAttrib4Niv;
	public static void glVertexAttrib4Niv(uint index, int *v) => s_glVertexAttrib4Niv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4NsvDelegate(uint index, short *v);
	private static glVertexAttrib4NsvDelegate s_glVertexAttrib4Nsv;
	public static void glVertexAttrib4Nsv(uint index, short *v) => s_glVertexAttrib4Nsv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4NubDelegate(uint index, byte x, byte y, byte z, byte w);
	private static glVertexAttrib4NubDelegate s_glVertexAttrib4Nub;
	public static void glVertexAttrib4Nub(uint index, byte x, byte y, byte z, byte w) => s_glVertexAttrib4Nub(index, x, y, z, w);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4NubvDelegate(uint index, byte *v);
	private static glVertexAttrib4NubvDelegate s_glVertexAttrib4Nubv;
	public static void glVertexAttrib4Nubv(uint index, byte *v) => s_glVertexAttrib4Nubv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4NuivDelegate(uint index, uint *v);
	private static glVertexAttrib4NuivDelegate s_glVertexAttrib4Nuiv;
	public static void glVertexAttrib4Nuiv(uint index, uint *v) => s_glVertexAttrib4Nuiv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4NusvDelegate(uint index, ushort *v);
	private static glVertexAttrib4NusvDelegate s_glVertexAttrib4Nusv;
	public static void glVertexAttrib4Nusv(uint index, ushort *v) => s_glVertexAttrib4Nusv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4bvDelegate(uint index, sbyte *v);
	private static glVertexAttrib4bvDelegate s_glVertexAttrib4bv;
	public static void glVertexAttrib4bv(uint index, sbyte *v) => s_glVertexAttrib4bv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4dDelegate(uint index, double x, double y, double z, double w);
	private static glVertexAttrib4dDelegate s_glVertexAttrib4d;
	public static void glVertexAttrib4d(uint index, double x, double y, double z, double w) => s_glVertexAttrib4d(index, x, y, z, w);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4dvDelegate(uint index, double *v);
	private static glVertexAttrib4dvDelegate s_glVertexAttrib4dv;
	public static void glVertexAttrib4dv(uint index, double *v) => s_glVertexAttrib4dv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4fDelegate(uint index, float x, float y, float z, float w);
	private static glVertexAttrib4fDelegate s_glVertexAttrib4f;
	public static void glVertexAttrib4f(uint index, float x, float y, float z, float w) => s_glVertexAttrib4f(index, x, y, z, w);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4fvDelegate(uint index, float *v);
	private static glVertexAttrib4fvDelegate s_glVertexAttrib4fv;
	public static void glVertexAttrib4fv(uint index, float *v) => s_glVertexAttrib4fv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4ivDelegate(uint index, int *v);
	private static glVertexAttrib4ivDelegate s_glVertexAttrib4iv;
	public static void glVertexAttrib4iv(uint index, int *v) => s_glVertexAttrib4iv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4sDelegate(uint index, short x, short y, short z, short w);
	private static glVertexAttrib4sDelegate s_glVertexAttrib4s;
	public static void glVertexAttrib4s(uint index, short x, short y, short z, short w) => s_glVertexAttrib4s(index, x, y, z, w);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4svDelegate(uint index, short *v);
	private static glVertexAttrib4svDelegate s_glVertexAttrib4sv;
	public static void glVertexAttrib4sv(uint index, short *v) => s_glVertexAttrib4sv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4ubvDelegate(uint index, byte *v);
	private static glVertexAttrib4ubvDelegate s_glVertexAttrib4ubv;
	public static void glVertexAttrib4ubv(uint index, byte *v) => s_glVertexAttrib4ubv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4uivDelegate(uint index, uint *v);
	private static glVertexAttrib4uivDelegate s_glVertexAttrib4uiv;
	public static void glVertexAttrib4uiv(uint index, uint *v) => s_glVertexAttrib4uiv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttrib4usvDelegate(uint index, ushort *v);
	private static glVertexAttrib4usvDelegate s_glVertexAttrib4usv;
	public static void glVertexAttrib4usv(uint index, ushort *v) => s_glVertexAttrib4usv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribBindingDelegate(uint attribindex, uint bindingindex);
	private static glVertexAttribBindingDelegate s_glVertexAttribBinding;
	public static void glVertexAttribBinding(uint attribindex, uint bindingindex) => s_glVertexAttribBinding(attribindex, bindingindex);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribDivisorDelegate(uint index, uint divisor);
	private static glVertexAttribDivisorDelegate s_glVertexAttribDivisor;
	public static void glVertexAttribDivisor(uint index, uint divisor) => s_glVertexAttribDivisor(index, divisor);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribFormatDelegate(uint attribindex, int size, int type, bool normalized, uint relativeoffset);
	private static glVertexAttribFormatDelegate s_glVertexAttribFormat;
	public static void glVertexAttribFormat(uint attribindex, int size, int type, bool normalized, uint relativeoffset) => s_glVertexAttribFormat(attribindex, size, type, normalized, relativeoffset);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI1iDelegate(uint index, int x);
	private static glVertexAttribI1iDelegate s_glVertexAttribI1i;
	public static void glVertexAttribI1i(uint index, int x) => s_glVertexAttribI1i(index, x);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI1ivDelegate(uint index, int *v);
	private static glVertexAttribI1ivDelegate s_glVertexAttribI1iv;
	public static void glVertexAttribI1iv(uint index, int *v) => s_glVertexAttribI1iv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI1uiDelegate(uint index, uint x);
	private static glVertexAttribI1uiDelegate s_glVertexAttribI1ui;
	public static void glVertexAttribI1ui(uint index, uint x) => s_glVertexAttribI1ui(index, x);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI1uivDelegate(uint index, uint *v);
	private static glVertexAttribI1uivDelegate s_glVertexAttribI1uiv;
	public static void glVertexAttribI1uiv(uint index, uint *v) => s_glVertexAttribI1uiv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI2iDelegate(uint index, int x, int y);
	private static glVertexAttribI2iDelegate s_glVertexAttribI2i;
	public static void glVertexAttribI2i(uint index, int x, int y) => s_glVertexAttribI2i(index, x, y);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI2ivDelegate(uint index, int *v);
	private static glVertexAttribI2ivDelegate s_glVertexAttribI2iv;
	public static void glVertexAttribI2iv(uint index, int *v) => s_glVertexAttribI2iv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI2uiDelegate(uint index, uint x, uint y);
	private static glVertexAttribI2uiDelegate s_glVertexAttribI2ui;
	public static void glVertexAttribI2ui(uint index, uint x, uint y) => s_glVertexAttribI2ui(index, x, y);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI2uivDelegate(uint index, uint *v);
	private static glVertexAttribI2uivDelegate s_glVertexAttribI2uiv;
	public static void glVertexAttribI2uiv(uint index, uint *v) => s_glVertexAttribI2uiv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI3iDelegate(uint index, int x, int y, int z);
	private static glVertexAttribI3iDelegate s_glVertexAttribI3i;
	public static void glVertexAttribI3i(uint index, int x, int y, int z) => s_glVertexAttribI3i(index, x, y, z);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI3ivDelegate(uint index, int *v);
	private static glVertexAttribI3ivDelegate s_glVertexAttribI3iv;
	public static void glVertexAttribI3iv(uint index, int *v) => s_glVertexAttribI3iv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI3uiDelegate(uint index, uint x, uint y, uint z);
	private static glVertexAttribI3uiDelegate s_glVertexAttribI3ui;
	public static void glVertexAttribI3ui(uint index, uint x, uint y, uint z) => s_glVertexAttribI3ui(index, x, y, z);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI3uivDelegate(uint index, uint *v);
	private static glVertexAttribI3uivDelegate s_glVertexAttribI3uiv;
	public static void glVertexAttribI3uiv(uint index, uint *v) => s_glVertexAttribI3uiv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI4bvDelegate(uint index, sbyte *v);
	private static glVertexAttribI4bvDelegate s_glVertexAttribI4bv;
	public static void glVertexAttribI4bv(uint index, sbyte *v) => s_glVertexAttribI4bv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI4iDelegate(uint index, int x, int y, int z, int w);
	private static glVertexAttribI4iDelegate s_glVertexAttribI4i;
	public static void glVertexAttribI4i(uint index, int x, int y, int z, int w) => s_glVertexAttribI4i(index, x, y, z, w);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI4ivDelegate(uint index, int *v);
	private static glVertexAttribI4ivDelegate s_glVertexAttribI4iv;
	public static void glVertexAttribI4iv(uint index, int *v) => s_glVertexAttribI4iv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI4svDelegate(uint index, short *v);
	private static glVertexAttribI4svDelegate s_glVertexAttribI4sv;
	public static void glVertexAttribI4sv(uint index, short *v) => s_glVertexAttribI4sv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI4ubvDelegate(uint index, byte *v);
	private static glVertexAttribI4ubvDelegate s_glVertexAttribI4ubv;
	public static void glVertexAttribI4ubv(uint index, byte *v) => s_glVertexAttribI4ubv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI4uiDelegate(uint index, uint x, uint y, uint z, uint w);
	private static glVertexAttribI4uiDelegate s_glVertexAttribI4ui;
	public static void glVertexAttribI4ui(uint index, uint x, uint y, uint z, uint w) => s_glVertexAttribI4ui(index, x, y, z, w);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI4uivDelegate(uint index, uint *v);
	private static glVertexAttribI4uivDelegate s_glVertexAttribI4uiv;
	public static void glVertexAttribI4uiv(uint index, uint *v) => s_glVertexAttribI4uiv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribI4usvDelegate(uint index, ushort *v);
	private static glVertexAttribI4usvDelegate s_glVertexAttribI4usv;
	public static void glVertexAttribI4usv(uint index, ushort *v) => s_glVertexAttribI4usv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribIFormatDelegate(uint attribindex, int size, int type, uint relativeoffset);
	private static glVertexAttribIFormatDelegate s_glVertexAttribIFormat;
	public static void glVertexAttribIFormat(uint attribindex, int size, int type, uint relativeoffset) => s_glVertexAttribIFormat(attribindex, size, type, relativeoffset);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribIPointerDelegate(uint index, int size, int type, int stride, void *pointer);
	private static glVertexAttribIPointerDelegate s_glVertexAttribIPointer;
	public static void glVertexAttribIPointer(uint index, int size, int type, int stride, void *pointer) => s_glVertexAttribIPointer(index, size, type, stride, pointer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribL1dDelegate(uint index, double x);
	private static glVertexAttribL1dDelegate s_glVertexAttribL1d;
	public static void glVertexAttribL1d(uint index, double x) => s_glVertexAttribL1d(index, x);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribL1dvDelegate(uint index, double *v);
	private static glVertexAttribL1dvDelegate s_glVertexAttribL1dv;
	public static void glVertexAttribL1dv(uint index, double *v) => s_glVertexAttribL1dv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribL2dDelegate(uint index, double x, double y);
	private static glVertexAttribL2dDelegate s_glVertexAttribL2d;
	public static void glVertexAttribL2d(uint index, double x, double y) => s_glVertexAttribL2d(index, x, y);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribL2dvDelegate(uint index, double *v);
	private static glVertexAttribL2dvDelegate s_glVertexAttribL2dv;
	public static void glVertexAttribL2dv(uint index, double *v) => s_glVertexAttribL2dv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribL3dDelegate(uint index, double x, double y, double z);
	private static glVertexAttribL3dDelegate s_glVertexAttribL3d;
	public static void glVertexAttribL3d(uint index, double x, double y, double z) => s_glVertexAttribL3d(index, x, y, z);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribL3dvDelegate(uint index, double *v);
	private static glVertexAttribL3dvDelegate s_glVertexAttribL3dv;
	public static void glVertexAttribL3dv(uint index, double *v) => s_glVertexAttribL3dv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribL4dDelegate(uint index, double x, double y, double z, double w);
	private static glVertexAttribL4dDelegate s_glVertexAttribL4d;
	public static void glVertexAttribL4d(uint index, double x, double y, double z, double w) => s_glVertexAttribL4d(index, x, y, z, w);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribL4dvDelegate(uint index, double *v);
	private static glVertexAttribL4dvDelegate s_glVertexAttribL4dv;
	public static void glVertexAttribL4dv(uint index, double *v) => s_glVertexAttribL4dv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribLFormatDelegate(uint attribindex, int size, int type, uint relativeoffset);
	private static glVertexAttribLFormatDelegate s_glVertexAttribLFormat;
	public static void glVertexAttribLFormat(uint attribindex, int size, int type, uint relativeoffset) => s_glVertexAttribLFormat(attribindex, size, type, relativeoffset);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribLPointerDelegate(uint index, int size, int type, int stride, void *pointer);
	private static glVertexAttribLPointerDelegate s_glVertexAttribLPointer;
	public static void glVertexAttribLPointer(uint index, int size, int type, int stride, void *pointer) => s_glVertexAttribLPointer(index, size, type, stride, pointer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribP1uiDelegate(uint index, int type, bool normalized, uint value);
	private static glVertexAttribP1uiDelegate s_glVertexAttribP1ui;
	public static void glVertexAttribP1ui(uint index, int type, bool normalized, uint value) => s_glVertexAttribP1ui(index, type, normalized, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribP1uivDelegate(uint index, int type, bool normalized, uint *value);
	private static glVertexAttribP1uivDelegate s_glVertexAttribP1uiv;
	public static void glVertexAttribP1uiv(uint index, int type, bool normalized, uint *value) => s_glVertexAttribP1uiv(index, type, normalized, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribP2uiDelegate(uint index, int type, bool normalized, uint value);
	private static glVertexAttribP2uiDelegate s_glVertexAttribP2ui;
	public static void glVertexAttribP2ui(uint index, int type, bool normalized, uint value) => s_glVertexAttribP2ui(index, type, normalized, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribP2uivDelegate(uint index, int type, bool normalized, uint *value);
	private static glVertexAttribP2uivDelegate s_glVertexAttribP2uiv;
	public static void glVertexAttribP2uiv(uint index, int type, bool normalized, uint *value) => s_glVertexAttribP2uiv(index, type, normalized, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribP3uiDelegate(uint index, int type, bool normalized, uint value);
	private static glVertexAttribP3uiDelegate s_glVertexAttribP3ui;
	public static void glVertexAttribP3ui(uint index, int type, bool normalized, uint value) => s_glVertexAttribP3ui(index, type, normalized, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribP3uivDelegate(uint index, int type, bool normalized, uint *value);
	private static glVertexAttribP3uivDelegate s_glVertexAttribP3uiv;
	public static void glVertexAttribP3uiv(uint index, int type, bool normalized, uint *value) => s_glVertexAttribP3uiv(index, type, normalized, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribP4uiDelegate(uint index, int type, bool normalized, uint value);
	private static glVertexAttribP4uiDelegate s_glVertexAttribP4ui;
	public static void glVertexAttribP4ui(uint index, int type, bool normalized, uint value) => s_glVertexAttribP4ui(index, type, normalized, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribP4uivDelegate(uint index, int type, bool normalized, uint *value);
	private static glVertexAttribP4uivDelegate s_glVertexAttribP4uiv;
	public static void glVertexAttribP4uiv(uint index, int type, bool normalized, uint *value) => s_glVertexAttribP4uiv(index, type, normalized, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexAttribPointerDelegate(uint index, int size, int type, bool normalized, int stride, void *pointer);
	private static glVertexAttribPointerDelegate s_glVertexAttribPointer;
	public static void glVertexAttribPointer(uint index, int size, int type, bool normalized, int stride, void *pointer) => s_glVertexAttribPointer(index, size, type, normalized, stride, pointer);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexBindingDivisorDelegate(uint bindingindex, uint divisor);
	private static glVertexBindingDivisorDelegate s_glVertexBindingDivisor;
	public static void glVertexBindingDivisor(uint bindingindex, uint divisor) => s_glVertexBindingDivisor(bindingindex, divisor);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexP2uiDelegate(int type, uint value);
	private static glVertexP2uiDelegate s_glVertexP2ui;
	public static void glVertexP2ui(int type, uint value) => s_glVertexP2ui(type, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexP2uivDelegate(int type, uint *value);
	private static glVertexP2uivDelegate s_glVertexP2uiv;
	public static void glVertexP2uiv(int type, uint *value) => s_glVertexP2uiv(type, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexP3uiDelegate(int type, uint value);
	private static glVertexP3uiDelegate s_glVertexP3ui;
	public static void glVertexP3ui(int type, uint value) => s_glVertexP3ui(type, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexP3uivDelegate(int type, uint *value);
	private static glVertexP3uivDelegate s_glVertexP3uiv;
	public static void glVertexP3uiv(int type, uint *value) => s_glVertexP3uiv(type, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexP4uiDelegate(int type, uint value);
	private static glVertexP4uiDelegate s_glVertexP4ui;
	public static void glVertexP4ui(int type, uint value) => s_glVertexP4ui(type, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glVertexP4uivDelegate(int type, uint *value);
	private static glVertexP4uivDelegate s_glVertexP4uiv;
	public static void glVertexP4uiv(int type, uint *value) => s_glVertexP4uiv(type, value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glViewportDelegate(int x, int y, int width, int height);
	private static glViewportDelegate s_glViewport;
	public static void glViewport(int x, int y, int width, int height) => s_glViewport(x, y, width, height);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glViewportArrayvDelegate(uint first, int count, float *v);
	private static glViewportArrayvDelegate s_glViewportArrayv;
	public static void glViewportArrayv(uint first, int count, float *v) => s_glViewportArrayv(first, count, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glViewportIndexedfDelegate(uint index, float x, float y, float w, float h);
	private static glViewportIndexedfDelegate s_glViewportIndexedf;
	public static void glViewportIndexedf(uint index, float x, float y, float w, float h) => s_glViewportIndexedf(index, x, y, w, h);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glViewportIndexedfvDelegate(uint index, float *v);
	private static glViewportIndexedfvDelegate s_glViewportIndexedfv;
	public static void glViewportIndexedfv(uint index, float *v) => s_glViewportIndexedfv(index, v);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void glWaitSyncDelegate(IntPtr sync, int flags, ulong timeout);
	private static glWaitSyncDelegate s_glWaitSync;
	public static void glWaitSync(IntPtr sync, int flags, ulong timeout) => s_glWaitSync(sync, flags, timeout);

	public static void Import(GetProcAddressDelegate getProcAddress)
	{
		s_glActiveShaderProgram = Marshal.GetDelegateForFunctionPointer<glActiveShaderProgramDelegate>(getProcAddress("glActiveShaderProgram"));
		s_glActiveTexture = Marshal.GetDelegateForFunctionPointer<glActiveTextureDelegate>(getProcAddress("glActiveTexture"));
		s_glAttachShader = Marshal.GetDelegateForFunctionPointer<glAttachShaderDelegate>(getProcAddress("glAttachShader"));
		s_glBeginConditionalRender = Marshal.GetDelegateForFunctionPointer<glBeginConditionalRenderDelegate>(getProcAddress("glBeginConditionalRender"));
		s_glBeginQuery = Marshal.GetDelegateForFunctionPointer<glBeginQueryDelegate>(getProcAddress("glBeginQuery"));
		s_glBeginQueryIndexed = Marshal.GetDelegateForFunctionPointer<glBeginQueryIndexedDelegate>(getProcAddress("glBeginQueryIndexed"));
		s_glBeginTransformFeedback = Marshal.GetDelegateForFunctionPointer<glBeginTransformFeedbackDelegate>(getProcAddress("glBeginTransformFeedback"));
		s_glBindAttribLocation = Marshal.GetDelegateForFunctionPointer<glBindAttribLocationDelegate>(getProcAddress("glBindAttribLocation"));
		s_glBindBuffer = Marshal.GetDelegateForFunctionPointer<glBindBufferDelegate>(getProcAddress("glBindBuffer"));
		s_glBindBufferBase = Marshal.GetDelegateForFunctionPointer<glBindBufferBaseDelegate>(getProcAddress("glBindBufferBase"));
		s_glBindBufferRange = Marshal.GetDelegateForFunctionPointer<glBindBufferRangeDelegate>(getProcAddress("glBindBufferRange"));
		s_glBindBuffersBase = Marshal.GetDelegateForFunctionPointer<glBindBuffersBaseDelegate>(getProcAddress("glBindBuffersBase"));
		s_glBindBuffersRange = Marshal.GetDelegateForFunctionPointer<glBindBuffersRangeDelegate>(getProcAddress("glBindBuffersRange"));
		s_glBindFragDataLocation = Marshal.GetDelegateForFunctionPointer<glBindFragDataLocationDelegate>(getProcAddress("glBindFragDataLocation"));
		s_glBindFragDataLocationIndexed = Marshal.GetDelegateForFunctionPointer<glBindFragDataLocationIndexedDelegate>(getProcAddress("glBindFragDataLocationIndexed"));
		s_glBindFramebuffer = Marshal.GetDelegateForFunctionPointer<glBindFramebufferDelegate>(getProcAddress("glBindFramebuffer"));
		s_glBindImageTexture = Marshal.GetDelegateForFunctionPointer<glBindImageTextureDelegate>(getProcAddress("glBindImageTexture"));
		s_glBindImageTextures = Marshal.GetDelegateForFunctionPointer<glBindImageTexturesDelegate>(getProcAddress("glBindImageTextures"));
		s_glBindProgramPipeline = Marshal.GetDelegateForFunctionPointer<glBindProgramPipelineDelegate>(getProcAddress("glBindProgramPipeline"));
		s_glBindRenderbuffer = Marshal.GetDelegateForFunctionPointer<glBindRenderbufferDelegate>(getProcAddress("glBindRenderbuffer"));
		s_glBindSampler = Marshal.GetDelegateForFunctionPointer<glBindSamplerDelegate>(getProcAddress("glBindSampler"));
		s_glBindSamplers = Marshal.GetDelegateForFunctionPointer<glBindSamplersDelegate>(getProcAddress("glBindSamplers"));
		s_glBindTexture = Marshal.GetDelegateForFunctionPointer<glBindTextureDelegate>(getProcAddress("glBindTexture"));
		s_glBindTextureUnit = Marshal.GetDelegateForFunctionPointer<glBindTextureUnitDelegate>(getProcAddress("glBindTextureUnit"));
		s_glBindTextures = Marshal.GetDelegateForFunctionPointer<glBindTexturesDelegate>(getProcAddress("glBindTextures"));
		s_glBindTransformFeedback = Marshal.GetDelegateForFunctionPointer<glBindTransformFeedbackDelegate>(getProcAddress("glBindTransformFeedback"));
		s_glBindVertexArray = Marshal.GetDelegateForFunctionPointer<glBindVertexArrayDelegate>(getProcAddress("glBindVertexArray"));
		s_glBindVertexBuffer = Marshal.GetDelegateForFunctionPointer<glBindVertexBufferDelegate>(getProcAddress("glBindVertexBuffer"));
		s_glBindVertexBuffers = Marshal.GetDelegateForFunctionPointer<glBindVertexBuffersDelegate>(getProcAddress("glBindVertexBuffers"));
		s_glBlendColor = Marshal.GetDelegateForFunctionPointer<glBlendColorDelegate>(getProcAddress("glBlendColor"));
		s_glBlendEquation = Marshal.GetDelegateForFunctionPointer<glBlendEquationDelegate>(getProcAddress("glBlendEquation"));
		s_glBlendEquationSeparate = Marshal.GetDelegateForFunctionPointer<glBlendEquationSeparateDelegate>(getProcAddress("glBlendEquationSeparate"));
		s_glBlendEquationSeparatei = Marshal.GetDelegateForFunctionPointer<glBlendEquationSeparateiDelegate>(getProcAddress("glBlendEquationSeparatei"));
		s_glBlendEquationi = Marshal.GetDelegateForFunctionPointer<glBlendEquationiDelegate>(getProcAddress("glBlendEquationi"));
		s_glBlendFunc = Marshal.GetDelegateForFunctionPointer<glBlendFuncDelegate>(getProcAddress("glBlendFunc"));
		s_glBlendFuncSeparate = Marshal.GetDelegateForFunctionPointer<glBlendFuncSeparateDelegate>(getProcAddress("glBlendFuncSeparate"));
		s_glBlendFuncSeparatei = Marshal.GetDelegateForFunctionPointer<glBlendFuncSeparateiDelegate>(getProcAddress("glBlendFuncSeparatei"));
		s_glBlendFunci = Marshal.GetDelegateForFunctionPointer<glBlendFunciDelegate>(getProcAddress("glBlendFunci"));
		s_glBlitFramebuffer = Marshal.GetDelegateForFunctionPointer<glBlitFramebufferDelegate>(getProcAddress("glBlitFramebuffer"));
		s_glBlitNamedFramebuffer = Marshal.GetDelegateForFunctionPointer<glBlitNamedFramebufferDelegate>(getProcAddress("glBlitNamedFramebuffer"));
		s_glBufferData = Marshal.GetDelegateForFunctionPointer<glBufferDataDelegate>(getProcAddress("glBufferData"));
		s_glBufferStorage = Marshal.GetDelegateForFunctionPointer<glBufferStorageDelegate>(getProcAddress("glBufferStorage"));
		s_glBufferSubData = Marshal.GetDelegateForFunctionPointer<glBufferSubDataDelegate>(getProcAddress("glBufferSubData"));
		s_glCheckFramebufferStatus = Marshal.GetDelegateForFunctionPointer<glCheckFramebufferStatusDelegate>(getProcAddress("glCheckFramebufferStatus"));
		s_glCheckNamedFramebufferStatus = Marshal.GetDelegateForFunctionPointer<glCheckNamedFramebufferStatusDelegate>(getProcAddress("glCheckNamedFramebufferStatus"));
		s_glClampColor = Marshal.GetDelegateForFunctionPointer<glClampColorDelegate>(getProcAddress("glClampColor"));
		s_glClear = Marshal.GetDelegateForFunctionPointer<glClearDelegate>(getProcAddress("glClear"));
		s_glClearBufferData = Marshal.GetDelegateForFunctionPointer<glClearBufferDataDelegate>(getProcAddress("glClearBufferData"));
		s_glClearBufferSubData = Marshal.GetDelegateForFunctionPointer<glClearBufferSubDataDelegate>(getProcAddress("glClearBufferSubData"));
		s_glClearBufferfi = Marshal.GetDelegateForFunctionPointer<glClearBufferfiDelegate>(getProcAddress("glClearBufferfi"));
		s_glClearBufferfv = Marshal.GetDelegateForFunctionPointer<glClearBufferfvDelegate>(getProcAddress("glClearBufferfv"));
		s_glClearBufferiv = Marshal.GetDelegateForFunctionPointer<glClearBufferivDelegate>(getProcAddress("glClearBufferiv"));
		s_glClearBufferuiv = Marshal.GetDelegateForFunctionPointer<glClearBufferuivDelegate>(getProcAddress("glClearBufferuiv"));
		s_glClearColor = Marshal.GetDelegateForFunctionPointer<glClearColorDelegate>(getProcAddress("glClearColor"));
		s_glClearDepth = Marshal.GetDelegateForFunctionPointer<glClearDepthDelegate>(getProcAddress("glClearDepth"));
		s_glClearDepthf = Marshal.GetDelegateForFunctionPointer<glClearDepthfDelegate>(getProcAddress("glClearDepthf"));
		s_glClearNamedBufferData = Marshal.GetDelegateForFunctionPointer<glClearNamedBufferDataDelegate>(getProcAddress("glClearNamedBufferData"));
		s_glClearNamedBufferSubData = Marshal.GetDelegateForFunctionPointer<glClearNamedBufferSubDataDelegate>(getProcAddress("glClearNamedBufferSubData"));
		s_glClearNamedFramebufferfi = Marshal.GetDelegateForFunctionPointer<glClearNamedFramebufferfiDelegate>(getProcAddress("glClearNamedFramebufferfi"));
		s_glClearNamedFramebufferfv = Marshal.GetDelegateForFunctionPointer<glClearNamedFramebufferfvDelegate>(getProcAddress("glClearNamedFramebufferfv"));
		s_glClearNamedFramebufferiv = Marshal.GetDelegateForFunctionPointer<glClearNamedFramebufferivDelegate>(getProcAddress("glClearNamedFramebufferiv"));
		s_glClearNamedFramebufferuiv = Marshal.GetDelegateForFunctionPointer<glClearNamedFramebufferuivDelegate>(getProcAddress("glClearNamedFramebufferuiv"));
		s_glClearStencil = Marshal.GetDelegateForFunctionPointer<glClearStencilDelegate>(getProcAddress("glClearStencil"));
		s_glClearTexImage = Marshal.GetDelegateForFunctionPointer<glClearTexImageDelegate>(getProcAddress("glClearTexImage"));
		s_glClearTexSubImage = Marshal.GetDelegateForFunctionPointer<glClearTexSubImageDelegate>(getProcAddress("glClearTexSubImage"));
		s_glClientWaitSync = Marshal.GetDelegateForFunctionPointer<glClientWaitSyncDelegate>(getProcAddress("glClientWaitSync"));
		s_glClipControl = Marshal.GetDelegateForFunctionPointer<glClipControlDelegate>(getProcAddress("glClipControl"));
		s_glColorMask = Marshal.GetDelegateForFunctionPointer<glColorMaskDelegate>(getProcAddress("glColorMask"));
		s_glColorMaski = Marshal.GetDelegateForFunctionPointer<glColorMaskiDelegate>(getProcAddress("glColorMaski"));
		s_glColorP3ui = Marshal.GetDelegateForFunctionPointer<glColorP3uiDelegate>(getProcAddress("glColorP3ui"));
		s_glColorP3uiv = Marshal.GetDelegateForFunctionPointer<glColorP3uivDelegate>(getProcAddress("glColorP3uiv"));
		s_glColorP4ui = Marshal.GetDelegateForFunctionPointer<glColorP4uiDelegate>(getProcAddress("glColorP4ui"));
		s_glColorP4uiv = Marshal.GetDelegateForFunctionPointer<glColorP4uivDelegate>(getProcAddress("glColorP4uiv"));
		s_glCompileShader = Marshal.GetDelegateForFunctionPointer<glCompileShaderDelegate>(getProcAddress("glCompileShader"));
		s_glCompressedTexImage1D = Marshal.GetDelegateForFunctionPointer<glCompressedTexImage1DDelegate>(getProcAddress("glCompressedTexImage1D"));
		s_glCompressedTexImage2D = Marshal.GetDelegateForFunctionPointer<glCompressedTexImage2DDelegate>(getProcAddress("glCompressedTexImage2D"));
		s_glCompressedTexImage3D = Marshal.GetDelegateForFunctionPointer<glCompressedTexImage3DDelegate>(getProcAddress("glCompressedTexImage3D"));
		s_glCompressedTexSubImage1D = Marshal.GetDelegateForFunctionPointer<glCompressedTexSubImage1DDelegate>(getProcAddress("glCompressedTexSubImage1D"));
		s_glCompressedTexSubImage2D = Marshal.GetDelegateForFunctionPointer<glCompressedTexSubImage2DDelegate>(getProcAddress("glCompressedTexSubImage2D"));
		s_glCompressedTexSubImage3D = Marshal.GetDelegateForFunctionPointer<glCompressedTexSubImage3DDelegate>(getProcAddress("glCompressedTexSubImage3D"));
		s_glCompressedTextureSubImage1D = Marshal.GetDelegateForFunctionPointer<glCompressedTextureSubImage1DDelegate>(getProcAddress("glCompressedTextureSubImage1D"));
		s_glCompressedTextureSubImage2D = Marshal.GetDelegateForFunctionPointer<glCompressedTextureSubImage2DDelegate>(getProcAddress("glCompressedTextureSubImage2D"));
		s_glCompressedTextureSubImage3D = Marshal.GetDelegateForFunctionPointer<glCompressedTextureSubImage3DDelegate>(getProcAddress("glCompressedTextureSubImage3D"));
		s_glCopyBufferSubData = Marshal.GetDelegateForFunctionPointer<glCopyBufferSubDataDelegate>(getProcAddress("glCopyBufferSubData"));
		s_glCopyImageSubData = Marshal.GetDelegateForFunctionPointer<glCopyImageSubDataDelegate>(getProcAddress("glCopyImageSubData"));
		s_glCopyNamedBufferSubData = Marshal.GetDelegateForFunctionPointer<glCopyNamedBufferSubDataDelegate>(getProcAddress("glCopyNamedBufferSubData"));
		s_glCopyTexImage1D = Marshal.GetDelegateForFunctionPointer<glCopyTexImage1DDelegate>(getProcAddress("glCopyTexImage1D"));
		s_glCopyTexImage2D = Marshal.GetDelegateForFunctionPointer<glCopyTexImage2DDelegate>(getProcAddress("glCopyTexImage2D"));
		s_glCopyTexSubImage1D = Marshal.GetDelegateForFunctionPointer<glCopyTexSubImage1DDelegate>(getProcAddress("glCopyTexSubImage1D"));
		s_glCopyTexSubImage2D = Marshal.GetDelegateForFunctionPointer<glCopyTexSubImage2DDelegate>(getProcAddress("glCopyTexSubImage2D"));
		s_glCopyTexSubImage3D = Marshal.GetDelegateForFunctionPointer<glCopyTexSubImage3DDelegate>(getProcAddress("glCopyTexSubImage3D"));
		s_glCopyTextureSubImage1D = Marshal.GetDelegateForFunctionPointer<glCopyTextureSubImage1DDelegate>(getProcAddress("glCopyTextureSubImage1D"));
		s_glCopyTextureSubImage2D = Marshal.GetDelegateForFunctionPointer<glCopyTextureSubImage2DDelegate>(getProcAddress("glCopyTextureSubImage2D"));
		s_glCopyTextureSubImage3D = Marshal.GetDelegateForFunctionPointer<glCopyTextureSubImage3DDelegate>(getProcAddress("glCopyTextureSubImage3D"));
		s_glCreateBuffers = Marshal.GetDelegateForFunctionPointer<glCreateBuffersDelegate>(getProcAddress("glCreateBuffers"));
		s_glCreateFramebuffers = Marshal.GetDelegateForFunctionPointer<glCreateFramebuffersDelegate>(getProcAddress("glCreateFramebuffers"));
		s_glCreateProgram = Marshal.GetDelegateForFunctionPointer<glCreateProgramDelegate>(getProcAddress("glCreateProgram"));
		s_glCreateProgramPipelines = Marshal.GetDelegateForFunctionPointer<glCreateProgramPipelinesDelegate>(getProcAddress("glCreateProgramPipelines"));
		s_glCreateQueries = Marshal.GetDelegateForFunctionPointer<glCreateQueriesDelegate>(getProcAddress("glCreateQueries"));
		s_glCreateRenderbuffers = Marshal.GetDelegateForFunctionPointer<glCreateRenderbuffersDelegate>(getProcAddress("glCreateRenderbuffers"));
		s_glCreateSamplers = Marshal.GetDelegateForFunctionPointer<glCreateSamplersDelegate>(getProcAddress("glCreateSamplers"));
		s_glCreateShader = Marshal.GetDelegateForFunctionPointer<glCreateShaderDelegate>(getProcAddress("glCreateShader"));
		s_glCreateShaderProgramv = Marshal.GetDelegateForFunctionPointer<glCreateShaderProgramvDelegate>(getProcAddress("glCreateShaderProgramv"));
		s_glCreateTextures = Marshal.GetDelegateForFunctionPointer<glCreateTexturesDelegate>(getProcAddress("glCreateTextures"));
		s_glCreateTransformFeedbacks = Marshal.GetDelegateForFunctionPointer<glCreateTransformFeedbacksDelegate>(getProcAddress("glCreateTransformFeedbacks"));
		s_glCreateVertexArrays = Marshal.GetDelegateForFunctionPointer<glCreateVertexArraysDelegate>(getProcAddress("glCreateVertexArrays"));
		s_glCullFace = Marshal.GetDelegateForFunctionPointer<glCullFaceDelegate>(getProcAddress("glCullFace"));
		s_glDebugMessageCallback = Marshal.GetDelegateForFunctionPointer<glDebugMessageCallbackDelegate>(getProcAddress("glDebugMessageCallback"));
		s_glDebugMessageControl = Marshal.GetDelegateForFunctionPointer<glDebugMessageControlDelegate>(getProcAddress("glDebugMessageControl"));
		s_glDebugMessageInsert = Marshal.GetDelegateForFunctionPointer<glDebugMessageInsertDelegate>(getProcAddress("glDebugMessageInsert"));
		s_glDeleteBuffers = Marshal.GetDelegateForFunctionPointer<glDeleteBuffersDelegate>(getProcAddress("glDeleteBuffers"));
		s_glDeleteFramebuffers = Marshal.GetDelegateForFunctionPointer<glDeleteFramebuffersDelegate>(getProcAddress("glDeleteFramebuffers"));
		s_glDeleteProgram = Marshal.GetDelegateForFunctionPointer<glDeleteProgramDelegate>(getProcAddress("glDeleteProgram"));
		s_glDeleteProgramPipelines = Marshal.GetDelegateForFunctionPointer<glDeleteProgramPipelinesDelegate>(getProcAddress("glDeleteProgramPipelines"));
		s_glDeleteQueries = Marshal.GetDelegateForFunctionPointer<glDeleteQueriesDelegate>(getProcAddress("glDeleteQueries"));
		s_glDeleteRenderbuffers = Marshal.GetDelegateForFunctionPointer<glDeleteRenderbuffersDelegate>(getProcAddress("glDeleteRenderbuffers"));
		s_glDeleteSamplers = Marshal.GetDelegateForFunctionPointer<glDeleteSamplersDelegate>(getProcAddress("glDeleteSamplers"));
		s_glDeleteShader = Marshal.GetDelegateForFunctionPointer<glDeleteShaderDelegate>(getProcAddress("glDeleteShader"));
		s_glDeleteSync = Marshal.GetDelegateForFunctionPointer<glDeleteSyncDelegate>(getProcAddress("glDeleteSync"));
		s_glDeleteTextures = Marshal.GetDelegateForFunctionPointer<glDeleteTexturesDelegate>(getProcAddress("glDeleteTextures"));
		s_glDeleteTransformFeedbacks = Marshal.GetDelegateForFunctionPointer<glDeleteTransformFeedbacksDelegate>(getProcAddress("glDeleteTransformFeedbacks"));
		s_glDeleteVertexArrays = Marshal.GetDelegateForFunctionPointer<glDeleteVertexArraysDelegate>(getProcAddress("glDeleteVertexArrays"));
		s_glDepthFunc = Marshal.GetDelegateForFunctionPointer<glDepthFuncDelegate>(getProcAddress("glDepthFunc"));
		s_glDepthMask = Marshal.GetDelegateForFunctionPointer<glDepthMaskDelegate>(getProcAddress("glDepthMask"));
		s_glDepthRange = Marshal.GetDelegateForFunctionPointer<glDepthRangeDelegate>(getProcAddress("glDepthRange"));
		s_glDepthRangeArrayv = Marshal.GetDelegateForFunctionPointer<glDepthRangeArrayvDelegate>(getProcAddress("glDepthRangeArrayv"));
		s_glDepthRangeIndexed = Marshal.GetDelegateForFunctionPointer<glDepthRangeIndexedDelegate>(getProcAddress("glDepthRangeIndexed"));
		s_glDepthRangef = Marshal.GetDelegateForFunctionPointer<glDepthRangefDelegate>(getProcAddress("glDepthRangef"));
		s_glDetachShader = Marshal.GetDelegateForFunctionPointer<glDetachShaderDelegate>(getProcAddress("glDetachShader"));
		s_glDisable = Marshal.GetDelegateForFunctionPointer<glDisableDelegate>(getProcAddress("glDisable"));
		s_glDisableVertexArrayAttrib = Marshal.GetDelegateForFunctionPointer<glDisableVertexArrayAttribDelegate>(getProcAddress("glDisableVertexArrayAttrib"));
		s_glDisableVertexAttribArray = Marshal.GetDelegateForFunctionPointer<glDisableVertexAttribArrayDelegate>(getProcAddress("glDisableVertexAttribArray"));
		s_glDisablei = Marshal.GetDelegateForFunctionPointer<glDisableiDelegate>(getProcAddress("glDisablei"));
		s_glDispatchCompute = Marshal.GetDelegateForFunctionPointer<glDispatchComputeDelegate>(getProcAddress("glDispatchCompute"));
		s_glDispatchComputeIndirect = Marshal.GetDelegateForFunctionPointer<glDispatchComputeIndirectDelegate>(getProcAddress("glDispatchComputeIndirect"));
		s_glDrawArrays = Marshal.GetDelegateForFunctionPointer<glDrawArraysDelegate>(getProcAddress("glDrawArrays"));
		s_glDrawArraysIndirect = Marshal.GetDelegateForFunctionPointer<glDrawArraysIndirectDelegate>(getProcAddress("glDrawArraysIndirect"));
		s_glDrawArraysInstanced = Marshal.GetDelegateForFunctionPointer<glDrawArraysInstancedDelegate>(getProcAddress("glDrawArraysInstanced"));
		s_glDrawArraysInstancedBaseInstance = Marshal.GetDelegateForFunctionPointer<glDrawArraysInstancedBaseInstanceDelegate>(getProcAddress("glDrawArraysInstancedBaseInstance"));
		s_glDrawBuffer = Marshal.GetDelegateForFunctionPointer<glDrawBufferDelegate>(getProcAddress("glDrawBuffer"));
		s_glDrawBuffers = Marshal.GetDelegateForFunctionPointer<glDrawBuffersDelegate>(getProcAddress("glDrawBuffers"));
		s_glDrawElements = Marshal.GetDelegateForFunctionPointer<glDrawElementsDelegate>(getProcAddress("glDrawElements"));
		s_glDrawElementsBaseVertex = Marshal.GetDelegateForFunctionPointer<glDrawElementsBaseVertexDelegate>(getProcAddress("glDrawElementsBaseVertex"));
		s_glDrawElementsIndirect = Marshal.GetDelegateForFunctionPointer<glDrawElementsIndirectDelegate>(getProcAddress("glDrawElementsIndirect"));
		s_glDrawElementsInstanced = Marshal.GetDelegateForFunctionPointer<glDrawElementsInstancedDelegate>(getProcAddress("glDrawElementsInstanced"));
		s_glDrawElementsInstancedBaseInstance = Marshal.GetDelegateForFunctionPointer<glDrawElementsInstancedBaseInstanceDelegate>(getProcAddress("glDrawElementsInstancedBaseInstance"));
		s_glDrawElementsInstancedBaseVertex = Marshal.GetDelegateForFunctionPointer<glDrawElementsInstancedBaseVertexDelegate>(getProcAddress("glDrawElementsInstancedBaseVertex"));
		s_glDrawElementsInstancedBaseVertexBaseInstance = Marshal.GetDelegateForFunctionPointer<glDrawElementsInstancedBaseVertexBaseInstanceDelegate>(getProcAddress("glDrawElementsInstancedBaseVertexBaseInstance"));
		s_glDrawRangeElements = Marshal.GetDelegateForFunctionPointer<glDrawRangeElementsDelegate>(getProcAddress("glDrawRangeElements"));
		s_glDrawRangeElementsBaseVertex = Marshal.GetDelegateForFunctionPointer<glDrawRangeElementsBaseVertexDelegate>(getProcAddress("glDrawRangeElementsBaseVertex"));
		s_glDrawTransformFeedback = Marshal.GetDelegateForFunctionPointer<glDrawTransformFeedbackDelegate>(getProcAddress("glDrawTransformFeedback"));
		s_glDrawTransformFeedbackInstanced = Marshal.GetDelegateForFunctionPointer<glDrawTransformFeedbackInstancedDelegate>(getProcAddress("glDrawTransformFeedbackInstanced"));
		s_glDrawTransformFeedbackStream = Marshal.GetDelegateForFunctionPointer<glDrawTransformFeedbackStreamDelegate>(getProcAddress("glDrawTransformFeedbackStream"));
		s_glDrawTransformFeedbackStreamInstanced = Marshal.GetDelegateForFunctionPointer<glDrawTransformFeedbackStreamInstancedDelegate>(getProcAddress("glDrawTransformFeedbackStreamInstanced"));
		s_glEnable = Marshal.GetDelegateForFunctionPointer<glEnableDelegate>(getProcAddress("glEnable"));
		s_glEnableVertexArrayAttrib = Marshal.GetDelegateForFunctionPointer<glEnableVertexArrayAttribDelegate>(getProcAddress("glEnableVertexArrayAttrib"));
		s_glEnableVertexAttribArray = Marshal.GetDelegateForFunctionPointer<glEnableVertexAttribArrayDelegate>(getProcAddress("glEnableVertexAttribArray"));
		s_glEnablei = Marshal.GetDelegateForFunctionPointer<glEnableiDelegate>(getProcAddress("glEnablei"));
		s_glEndConditionalRender = Marshal.GetDelegateForFunctionPointer<glEndConditionalRenderDelegate>(getProcAddress("glEndConditionalRender"));
		s_glEndQuery = Marshal.GetDelegateForFunctionPointer<glEndQueryDelegate>(getProcAddress("glEndQuery"));
		s_glEndQueryIndexed = Marshal.GetDelegateForFunctionPointer<glEndQueryIndexedDelegate>(getProcAddress("glEndQueryIndexed"));
		s_glEndTransformFeedback = Marshal.GetDelegateForFunctionPointer<glEndTransformFeedbackDelegate>(getProcAddress("glEndTransformFeedback"));
		s_glFenceSync = Marshal.GetDelegateForFunctionPointer<glFenceSyncDelegate>(getProcAddress("glFenceSync"));
		s_glFinish = Marshal.GetDelegateForFunctionPointer<glFinishDelegate>(getProcAddress("glFinish"));
		s_glFlush = Marshal.GetDelegateForFunctionPointer<glFlushDelegate>(getProcAddress("glFlush"));
		s_glFlushMappedBufferRange = Marshal.GetDelegateForFunctionPointer<glFlushMappedBufferRangeDelegate>(getProcAddress("glFlushMappedBufferRange"));
		s_glFlushMappedNamedBufferRange = Marshal.GetDelegateForFunctionPointer<glFlushMappedNamedBufferRangeDelegate>(getProcAddress("glFlushMappedNamedBufferRange"));
		s_glFramebufferParameteri = Marshal.GetDelegateForFunctionPointer<glFramebufferParameteriDelegate>(getProcAddress("glFramebufferParameteri"));
		s_glFramebufferRenderbuffer = Marshal.GetDelegateForFunctionPointer<glFramebufferRenderbufferDelegate>(getProcAddress("glFramebufferRenderbuffer"));
		s_glFramebufferTexture = Marshal.GetDelegateForFunctionPointer<glFramebufferTextureDelegate>(getProcAddress("glFramebufferTexture"));
		s_glFramebufferTexture1D = Marshal.GetDelegateForFunctionPointer<glFramebufferTexture1DDelegate>(getProcAddress("glFramebufferTexture1D"));
		s_glFramebufferTexture2D = Marshal.GetDelegateForFunctionPointer<glFramebufferTexture2DDelegate>(getProcAddress("glFramebufferTexture2D"));
		s_glFramebufferTexture3D = Marshal.GetDelegateForFunctionPointer<glFramebufferTexture3DDelegate>(getProcAddress("glFramebufferTexture3D"));
		s_glFramebufferTextureLayer = Marshal.GetDelegateForFunctionPointer<glFramebufferTextureLayerDelegate>(getProcAddress("glFramebufferTextureLayer"));
		s_glFrontFace = Marshal.GetDelegateForFunctionPointer<glFrontFaceDelegate>(getProcAddress("glFrontFace"));
		s_glGenBuffers = Marshal.GetDelegateForFunctionPointer<glGenBuffersDelegate>(getProcAddress("glGenBuffers"));
		s_glGenFramebuffers = Marshal.GetDelegateForFunctionPointer<glGenFramebuffersDelegate>(getProcAddress("glGenFramebuffers"));
		s_glGenProgramPipelines = Marshal.GetDelegateForFunctionPointer<glGenProgramPipelinesDelegate>(getProcAddress("glGenProgramPipelines"));
		s_glGenQueries = Marshal.GetDelegateForFunctionPointer<glGenQueriesDelegate>(getProcAddress("glGenQueries"));
		s_glGenRenderbuffers = Marshal.GetDelegateForFunctionPointer<glGenRenderbuffersDelegate>(getProcAddress("glGenRenderbuffers"));
		s_glGenSamplers = Marshal.GetDelegateForFunctionPointer<glGenSamplersDelegate>(getProcAddress("glGenSamplers"));
		s_glGenTextures = Marshal.GetDelegateForFunctionPointer<glGenTexturesDelegate>(getProcAddress("glGenTextures"));
		s_glGenTransformFeedbacks = Marshal.GetDelegateForFunctionPointer<glGenTransformFeedbacksDelegate>(getProcAddress("glGenTransformFeedbacks"));
		s_glGenVertexArrays = Marshal.GetDelegateForFunctionPointer<glGenVertexArraysDelegate>(getProcAddress("glGenVertexArrays"));
		s_glGenerateMipmap = Marshal.GetDelegateForFunctionPointer<glGenerateMipmapDelegate>(getProcAddress("glGenerateMipmap"));
		s_glGenerateTextureMipmap = Marshal.GetDelegateForFunctionPointer<glGenerateTextureMipmapDelegate>(getProcAddress("glGenerateTextureMipmap"));
		s_glGetActiveAtomicCounterBufferiv = Marshal.GetDelegateForFunctionPointer<glGetActiveAtomicCounterBufferivDelegate>(getProcAddress("glGetActiveAtomicCounterBufferiv"));
		s_glGetActiveAttrib = Marshal.GetDelegateForFunctionPointer<glGetActiveAttribDelegate>(getProcAddress("glGetActiveAttrib"));
		s_glGetActiveSubroutineName = Marshal.GetDelegateForFunctionPointer<glGetActiveSubroutineNameDelegate>(getProcAddress("glGetActiveSubroutineName"));
		s_glGetActiveSubroutineUniformName = Marshal.GetDelegateForFunctionPointer<glGetActiveSubroutineUniformNameDelegate>(getProcAddress("glGetActiveSubroutineUniformName"));
		s_glGetActiveSubroutineUniformiv = Marshal.GetDelegateForFunctionPointer<glGetActiveSubroutineUniformivDelegate>(getProcAddress("glGetActiveSubroutineUniformiv"));
		s_glGetActiveUniform = Marshal.GetDelegateForFunctionPointer<glGetActiveUniformDelegate>(getProcAddress("glGetActiveUniform"));
		s_glGetActiveUniformBlockName = Marshal.GetDelegateForFunctionPointer<glGetActiveUniformBlockNameDelegate>(getProcAddress("glGetActiveUniformBlockName"));
		s_glGetActiveUniformBlockiv = Marshal.GetDelegateForFunctionPointer<glGetActiveUniformBlockivDelegate>(getProcAddress("glGetActiveUniformBlockiv"));
		s_glGetActiveUniformName = Marshal.GetDelegateForFunctionPointer<glGetActiveUniformNameDelegate>(getProcAddress("glGetActiveUniformName"));
		s_glGetActiveUniformsiv = Marshal.GetDelegateForFunctionPointer<glGetActiveUniformsivDelegate>(getProcAddress("glGetActiveUniformsiv"));
		s_glGetAttachedShaders = Marshal.GetDelegateForFunctionPointer<glGetAttachedShadersDelegate>(getProcAddress("glGetAttachedShaders"));
		s_glGetAttribLocation = Marshal.GetDelegateForFunctionPointer<glGetAttribLocationDelegate>(getProcAddress("glGetAttribLocation"));
		s_glGetBooleani_v = Marshal.GetDelegateForFunctionPointer<glGetBooleani_vDelegate>(getProcAddress("glGetBooleani_v"));
		s_glGetBooleanv = Marshal.GetDelegateForFunctionPointer<glGetBooleanvDelegate>(getProcAddress("glGetBooleanv"));
		s_glGetBufferParameteri64v = Marshal.GetDelegateForFunctionPointer<glGetBufferParameteri64vDelegate>(getProcAddress("glGetBufferParameteri64v"));
		s_glGetBufferParameteriv = Marshal.GetDelegateForFunctionPointer<glGetBufferParameterivDelegate>(getProcAddress("glGetBufferParameteriv"));
		s_glGetBufferPointerv = Marshal.GetDelegateForFunctionPointer<glGetBufferPointervDelegate>(getProcAddress("glGetBufferPointerv"));
		s_glGetBufferSubData = Marshal.GetDelegateForFunctionPointer<glGetBufferSubDataDelegate>(getProcAddress("glGetBufferSubData"));
		s_glGetCompressedTexImage = Marshal.GetDelegateForFunctionPointer<glGetCompressedTexImageDelegate>(getProcAddress("glGetCompressedTexImage"));
		s_glGetCompressedTextureImage = Marshal.GetDelegateForFunctionPointer<glGetCompressedTextureImageDelegate>(getProcAddress("glGetCompressedTextureImage"));
		s_glGetCompressedTextureSubImage = Marshal.GetDelegateForFunctionPointer<glGetCompressedTextureSubImageDelegate>(getProcAddress("glGetCompressedTextureSubImage"));
		s_glGetDebugMessageLog = Marshal.GetDelegateForFunctionPointer<glGetDebugMessageLogDelegate>(getProcAddress("glGetDebugMessageLog"));
		s_glGetDoublei_v = Marshal.GetDelegateForFunctionPointer<glGetDoublei_vDelegate>(getProcAddress("glGetDoublei_v"));
		s_glGetDoublev = Marshal.GetDelegateForFunctionPointer<glGetDoublevDelegate>(getProcAddress("glGetDoublev"));
		s_glGetError = Marshal.GetDelegateForFunctionPointer<glGetErrorDelegate>(getProcAddress("glGetError"));
		s_glGetFloati_v = Marshal.GetDelegateForFunctionPointer<glGetFloati_vDelegate>(getProcAddress("glGetFloati_v"));
		s_glGetFloatv = Marshal.GetDelegateForFunctionPointer<glGetFloatvDelegate>(getProcAddress("glGetFloatv"));
		s_glGetFragDataIndex = Marshal.GetDelegateForFunctionPointer<glGetFragDataIndexDelegate>(getProcAddress("glGetFragDataIndex"));
		s_glGetFragDataLocation = Marshal.GetDelegateForFunctionPointer<glGetFragDataLocationDelegate>(getProcAddress("glGetFragDataLocation"));
		s_glGetFramebufferAttachmentParameteriv = Marshal.GetDelegateForFunctionPointer<glGetFramebufferAttachmentParameterivDelegate>(getProcAddress("glGetFramebufferAttachmentParameteriv"));
		s_glGetFramebufferParameteriv = Marshal.GetDelegateForFunctionPointer<glGetFramebufferParameterivDelegate>(getProcAddress("glGetFramebufferParameteriv"));
		s_glGetGraphicsResetStatus = Marshal.GetDelegateForFunctionPointer<glGetGraphicsResetStatusDelegate>(getProcAddress("glGetGraphicsResetStatus"));
		s_glGetInteger64i_v = Marshal.GetDelegateForFunctionPointer<glGetInteger64i_vDelegate>(getProcAddress("glGetInteger64i_v"));
		s_glGetInteger64v = Marshal.GetDelegateForFunctionPointer<glGetInteger64vDelegate>(getProcAddress("glGetInteger64v"));
		s_glGetIntegeri_v = Marshal.GetDelegateForFunctionPointer<glGetIntegeri_vDelegate>(getProcAddress("glGetIntegeri_v"));
		s_glGetIntegerv = Marshal.GetDelegateForFunctionPointer<glGetIntegervDelegate>(getProcAddress("glGetIntegerv"));
		s_glGetInternalformati64v = Marshal.GetDelegateForFunctionPointer<glGetInternalformati64vDelegate>(getProcAddress("glGetInternalformati64v"));
		s_glGetInternalformativ = Marshal.GetDelegateForFunctionPointer<glGetInternalformativDelegate>(getProcAddress("glGetInternalformativ"));
		s_glGetMultisamplefv = Marshal.GetDelegateForFunctionPointer<glGetMultisamplefvDelegate>(getProcAddress("glGetMultisamplefv"));
		s_glGetNamedBufferParameteri64v = Marshal.GetDelegateForFunctionPointer<glGetNamedBufferParameteri64vDelegate>(getProcAddress("glGetNamedBufferParameteri64v"));
		s_glGetNamedBufferParameteriv = Marshal.GetDelegateForFunctionPointer<glGetNamedBufferParameterivDelegate>(getProcAddress("glGetNamedBufferParameteriv"));
		s_glGetNamedBufferPointerv = Marshal.GetDelegateForFunctionPointer<glGetNamedBufferPointervDelegate>(getProcAddress("glGetNamedBufferPointerv"));
		s_glGetNamedBufferSubData = Marshal.GetDelegateForFunctionPointer<glGetNamedBufferSubDataDelegate>(getProcAddress("glGetNamedBufferSubData"));
		s_glGetNamedFramebufferAttachmentParameteriv = Marshal.GetDelegateForFunctionPointer<glGetNamedFramebufferAttachmentParameterivDelegate>(getProcAddress("glGetNamedFramebufferAttachmentParameteriv"));
		s_glGetNamedFramebufferParameteriv = Marshal.GetDelegateForFunctionPointer<glGetNamedFramebufferParameterivDelegate>(getProcAddress("glGetNamedFramebufferParameteriv"));
		s_glGetNamedRenderbufferParameteriv = Marshal.GetDelegateForFunctionPointer<glGetNamedRenderbufferParameterivDelegate>(getProcAddress("glGetNamedRenderbufferParameteriv"));
		s_glGetObjectLabel = Marshal.GetDelegateForFunctionPointer<glGetObjectLabelDelegate>(getProcAddress("glGetObjectLabel"));
		s_glGetObjectPtrLabel = Marshal.GetDelegateForFunctionPointer<glGetObjectPtrLabelDelegate>(getProcAddress("glGetObjectPtrLabel"));
		s_glGetProgramBinary = Marshal.GetDelegateForFunctionPointer<glGetProgramBinaryDelegate>(getProcAddress("glGetProgramBinary"));
		s_glGetProgramInfoLog = Marshal.GetDelegateForFunctionPointer<glGetProgramInfoLogDelegate>(getProcAddress("glGetProgramInfoLog"));
		s_glGetProgramInterfaceiv = Marshal.GetDelegateForFunctionPointer<glGetProgramInterfaceivDelegate>(getProcAddress("glGetProgramInterfaceiv"));
		s_glGetProgramPipelineInfoLog = Marshal.GetDelegateForFunctionPointer<glGetProgramPipelineInfoLogDelegate>(getProcAddress("glGetProgramPipelineInfoLog"));
		s_glGetProgramPipelineiv = Marshal.GetDelegateForFunctionPointer<glGetProgramPipelineivDelegate>(getProcAddress("glGetProgramPipelineiv"));
		s_glGetProgramResourceIndex = Marshal.GetDelegateForFunctionPointer<glGetProgramResourceIndexDelegate>(getProcAddress("glGetProgramResourceIndex"));
		s_glGetProgramResourceLocation = Marshal.GetDelegateForFunctionPointer<glGetProgramResourceLocationDelegate>(getProcAddress("glGetProgramResourceLocation"));
		s_glGetProgramResourceLocationIndex = Marshal.GetDelegateForFunctionPointer<glGetProgramResourceLocationIndexDelegate>(getProcAddress("glGetProgramResourceLocationIndex"));
		s_glGetProgramResourceName = Marshal.GetDelegateForFunctionPointer<glGetProgramResourceNameDelegate>(getProcAddress("glGetProgramResourceName"));
		s_glGetProgramResourceiv = Marshal.GetDelegateForFunctionPointer<glGetProgramResourceivDelegate>(getProcAddress("glGetProgramResourceiv"));
		s_glGetProgramStageiv = Marshal.GetDelegateForFunctionPointer<glGetProgramStageivDelegate>(getProcAddress("glGetProgramStageiv"));
		s_glGetProgramiv = Marshal.GetDelegateForFunctionPointer<glGetProgramivDelegate>(getProcAddress("glGetProgramiv"));
		s_glGetQueryBufferObjecti64v = Marshal.GetDelegateForFunctionPointer<glGetQueryBufferObjecti64vDelegate>(getProcAddress("glGetQueryBufferObjecti64v"));
		s_glGetQueryBufferObjectiv = Marshal.GetDelegateForFunctionPointer<glGetQueryBufferObjectivDelegate>(getProcAddress("glGetQueryBufferObjectiv"));
		s_glGetQueryBufferObjectui64v = Marshal.GetDelegateForFunctionPointer<glGetQueryBufferObjectui64vDelegate>(getProcAddress("glGetQueryBufferObjectui64v"));
		s_glGetQueryBufferObjectuiv = Marshal.GetDelegateForFunctionPointer<glGetQueryBufferObjectuivDelegate>(getProcAddress("glGetQueryBufferObjectuiv"));
		s_glGetQueryIndexediv = Marshal.GetDelegateForFunctionPointer<glGetQueryIndexedivDelegate>(getProcAddress("glGetQueryIndexediv"));
		s_glGetQueryObjecti64v = Marshal.GetDelegateForFunctionPointer<glGetQueryObjecti64vDelegate>(getProcAddress("glGetQueryObjecti64v"));
		s_glGetQueryObjectiv = Marshal.GetDelegateForFunctionPointer<glGetQueryObjectivDelegate>(getProcAddress("glGetQueryObjectiv"));
		s_glGetQueryObjectui64v = Marshal.GetDelegateForFunctionPointer<glGetQueryObjectui64vDelegate>(getProcAddress("glGetQueryObjectui64v"));
		s_glGetQueryObjectuiv = Marshal.GetDelegateForFunctionPointer<glGetQueryObjectuivDelegate>(getProcAddress("glGetQueryObjectuiv"));
		s_glGetQueryiv = Marshal.GetDelegateForFunctionPointer<glGetQueryivDelegate>(getProcAddress("glGetQueryiv"));
		s_glGetRenderbufferParameteriv = Marshal.GetDelegateForFunctionPointer<glGetRenderbufferParameterivDelegate>(getProcAddress("glGetRenderbufferParameteriv"));
		s_glGetSamplerParameterIiv = Marshal.GetDelegateForFunctionPointer<glGetSamplerParameterIivDelegate>(getProcAddress("glGetSamplerParameterIiv"));
		s_glGetSamplerParameterIuiv = Marshal.GetDelegateForFunctionPointer<glGetSamplerParameterIuivDelegate>(getProcAddress("glGetSamplerParameterIuiv"));
		s_glGetSamplerParameterfv = Marshal.GetDelegateForFunctionPointer<glGetSamplerParameterfvDelegate>(getProcAddress("glGetSamplerParameterfv"));
		s_glGetSamplerParameteriv = Marshal.GetDelegateForFunctionPointer<glGetSamplerParameterivDelegate>(getProcAddress("glGetSamplerParameteriv"));
		s_glGetShaderInfoLog = Marshal.GetDelegateForFunctionPointer<glGetShaderInfoLogDelegate>(getProcAddress("glGetShaderInfoLog"));
		s_glGetShaderPrecisionFormat = Marshal.GetDelegateForFunctionPointer<glGetShaderPrecisionFormatDelegate>(getProcAddress("glGetShaderPrecisionFormat"));
		s_glGetShaderSource = Marshal.GetDelegateForFunctionPointer<glGetShaderSourceDelegate>(getProcAddress("glGetShaderSource"));
		s_glGetShaderiv = Marshal.GetDelegateForFunctionPointer<glGetShaderivDelegate>(getProcAddress("glGetShaderiv"));
		s_glGetString = Marshal.GetDelegateForFunctionPointer<glGetStringDelegate>(getProcAddress("glGetString"));
		s_glGetStringi = Marshal.GetDelegateForFunctionPointer<glGetStringiDelegate>(getProcAddress("glGetStringi"));
		s_glGetSubroutineIndex = Marshal.GetDelegateForFunctionPointer<glGetSubroutineIndexDelegate>(getProcAddress("glGetSubroutineIndex"));
		s_glGetSubroutineUniformLocation = Marshal.GetDelegateForFunctionPointer<glGetSubroutineUniformLocationDelegate>(getProcAddress("glGetSubroutineUniformLocation"));
		s_glGetSynciv = Marshal.GetDelegateForFunctionPointer<glGetSyncivDelegate>(getProcAddress("glGetSynciv"));
		s_glGetTexImage = Marshal.GetDelegateForFunctionPointer<glGetTexImageDelegate>(getProcAddress("glGetTexImage"));
		s_glGetTexLevelParameterfv = Marshal.GetDelegateForFunctionPointer<glGetTexLevelParameterfvDelegate>(getProcAddress("glGetTexLevelParameterfv"));
		s_glGetTexLevelParameteriv = Marshal.GetDelegateForFunctionPointer<glGetTexLevelParameterivDelegate>(getProcAddress("glGetTexLevelParameteriv"));
		s_glGetTexParameterIiv = Marshal.GetDelegateForFunctionPointer<glGetTexParameterIivDelegate>(getProcAddress("glGetTexParameterIiv"));
		s_glGetTexParameterIuiv = Marshal.GetDelegateForFunctionPointer<glGetTexParameterIuivDelegate>(getProcAddress("glGetTexParameterIuiv"));
		s_glGetTexParameterfv = Marshal.GetDelegateForFunctionPointer<glGetTexParameterfvDelegate>(getProcAddress("glGetTexParameterfv"));
		s_glGetTexParameteriv = Marshal.GetDelegateForFunctionPointer<glGetTexParameterivDelegate>(getProcAddress("glGetTexParameteriv"));
		s_glGetTextureImage = Marshal.GetDelegateForFunctionPointer<glGetTextureImageDelegate>(getProcAddress("glGetTextureImage"));
		s_glGetTextureLevelParameterfv = Marshal.GetDelegateForFunctionPointer<glGetTextureLevelParameterfvDelegate>(getProcAddress("glGetTextureLevelParameterfv"));
		s_glGetTextureLevelParameteriv = Marshal.GetDelegateForFunctionPointer<glGetTextureLevelParameterivDelegate>(getProcAddress("glGetTextureLevelParameteriv"));
		s_glGetTextureParameterIiv = Marshal.GetDelegateForFunctionPointer<glGetTextureParameterIivDelegate>(getProcAddress("glGetTextureParameterIiv"));
		s_glGetTextureParameterIuiv = Marshal.GetDelegateForFunctionPointer<glGetTextureParameterIuivDelegate>(getProcAddress("glGetTextureParameterIuiv"));
		s_glGetTextureParameterfv = Marshal.GetDelegateForFunctionPointer<glGetTextureParameterfvDelegate>(getProcAddress("glGetTextureParameterfv"));
		s_glGetTextureParameteriv = Marshal.GetDelegateForFunctionPointer<glGetTextureParameterivDelegate>(getProcAddress("glGetTextureParameteriv"));
		s_glGetTextureSubImage = Marshal.GetDelegateForFunctionPointer<glGetTextureSubImageDelegate>(getProcAddress("glGetTextureSubImage"));
		s_glGetTransformFeedbackVarying = Marshal.GetDelegateForFunctionPointer<glGetTransformFeedbackVaryingDelegate>(getProcAddress("glGetTransformFeedbackVarying"));
		s_glGetTransformFeedbacki64_v = Marshal.GetDelegateForFunctionPointer<glGetTransformFeedbacki64_vDelegate>(getProcAddress("glGetTransformFeedbacki64_v"));
		s_glGetTransformFeedbacki_v = Marshal.GetDelegateForFunctionPointer<glGetTransformFeedbacki_vDelegate>(getProcAddress("glGetTransformFeedbacki_v"));
		s_glGetTransformFeedbackiv = Marshal.GetDelegateForFunctionPointer<glGetTransformFeedbackivDelegate>(getProcAddress("glGetTransformFeedbackiv"));
		s_glGetUniformBlockIndex = Marshal.GetDelegateForFunctionPointer<glGetUniformBlockIndexDelegate>(getProcAddress("glGetUniformBlockIndex"));
		s_glGetUniformIndices = Marshal.GetDelegateForFunctionPointer<glGetUniformIndicesDelegate>(getProcAddress("glGetUniformIndices"));
		s_glGetUniformLocation = Marshal.GetDelegateForFunctionPointer<glGetUniformLocationDelegate>(getProcAddress("glGetUniformLocation"));
		s_glGetUniformSubroutineuiv = Marshal.GetDelegateForFunctionPointer<glGetUniformSubroutineuivDelegate>(getProcAddress("glGetUniformSubroutineuiv"));
		s_glGetUniformdv = Marshal.GetDelegateForFunctionPointer<glGetUniformdvDelegate>(getProcAddress("glGetUniformdv"));
		s_glGetUniformfv = Marshal.GetDelegateForFunctionPointer<glGetUniformfvDelegate>(getProcAddress("glGetUniformfv"));
		s_glGetUniformiv = Marshal.GetDelegateForFunctionPointer<glGetUniformivDelegate>(getProcAddress("glGetUniformiv"));
		s_glGetUniformuiv = Marshal.GetDelegateForFunctionPointer<glGetUniformuivDelegate>(getProcAddress("glGetUniformuiv"));
		s_glGetVertexArrayIndexed64iv = Marshal.GetDelegateForFunctionPointer<glGetVertexArrayIndexed64ivDelegate>(getProcAddress("glGetVertexArrayIndexed64iv"));
		s_glGetVertexArrayIndexediv = Marshal.GetDelegateForFunctionPointer<glGetVertexArrayIndexedivDelegate>(getProcAddress("glGetVertexArrayIndexediv"));
		s_glGetVertexArrayiv = Marshal.GetDelegateForFunctionPointer<glGetVertexArrayivDelegate>(getProcAddress("glGetVertexArrayiv"));
		s_glGetVertexAttribIiv = Marshal.GetDelegateForFunctionPointer<glGetVertexAttribIivDelegate>(getProcAddress("glGetVertexAttribIiv"));
		s_glGetVertexAttribIuiv = Marshal.GetDelegateForFunctionPointer<glGetVertexAttribIuivDelegate>(getProcAddress("glGetVertexAttribIuiv"));
		s_glGetVertexAttribLdv = Marshal.GetDelegateForFunctionPointer<glGetVertexAttribLdvDelegate>(getProcAddress("glGetVertexAttribLdv"));
		s_glGetVertexAttribPointerv = Marshal.GetDelegateForFunctionPointer<glGetVertexAttribPointervDelegate>(getProcAddress("glGetVertexAttribPointerv"));
		s_glGetVertexAttribdv = Marshal.GetDelegateForFunctionPointer<glGetVertexAttribdvDelegate>(getProcAddress("glGetVertexAttribdv"));
		s_glGetVertexAttribfv = Marshal.GetDelegateForFunctionPointer<glGetVertexAttribfvDelegate>(getProcAddress("glGetVertexAttribfv"));
		s_glGetVertexAttribiv = Marshal.GetDelegateForFunctionPointer<glGetVertexAttribivDelegate>(getProcAddress("glGetVertexAttribiv"));
		s_glGetnColorTable = Marshal.GetDelegateForFunctionPointer<glGetnColorTableDelegate>(getProcAddress("glGetnColorTable"));
		s_glGetnCompressedTexImage = Marshal.GetDelegateForFunctionPointer<glGetnCompressedTexImageDelegate>(getProcAddress("glGetnCompressedTexImage"));
		s_glGetnConvolutionFilter = Marshal.GetDelegateForFunctionPointer<glGetnConvolutionFilterDelegate>(getProcAddress("glGetnConvolutionFilter"));
		s_glGetnHistogram = Marshal.GetDelegateForFunctionPointer<glGetnHistogramDelegate>(getProcAddress("glGetnHistogram"));
		s_glGetnMapdv = Marshal.GetDelegateForFunctionPointer<glGetnMapdvDelegate>(getProcAddress("glGetnMapdv"));
		s_glGetnMapfv = Marshal.GetDelegateForFunctionPointer<glGetnMapfvDelegate>(getProcAddress("glGetnMapfv"));
		s_glGetnMapiv = Marshal.GetDelegateForFunctionPointer<glGetnMapivDelegate>(getProcAddress("glGetnMapiv"));
		s_glGetnMinmax = Marshal.GetDelegateForFunctionPointer<glGetnMinmaxDelegate>(getProcAddress("glGetnMinmax"));
		s_glGetnPixelMapfv = Marshal.GetDelegateForFunctionPointer<glGetnPixelMapfvDelegate>(getProcAddress("glGetnPixelMapfv"));
		s_glGetnPixelMapuiv = Marshal.GetDelegateForFunctionPointer<glGetnPixelMapuivDelegate>(getProcAddress("glGetnPixelMapuiv"));
		s_glGetnPixelMapusv = Marshal.GetDelegateForFunctionPointer<glGetnPixelMapusvDelegate>(getProcAddress("glGetnPixelMapusv"));
		s_glGetnPolygonStipple = Marshal.GetDelegateForFunctionPointer<glGetnPolygonStippleDelegate>(getProcAddress("glGetnPolygonStipple"));
		s_glGetnSeparableFilter = Marshal.GetDelegateForFunctionPointer<glGetnSeparableFilterDelegate>(getProcAddress("glGetnSeparableFilter"));
		s_glGetnTexImage = Marshal.GetDelegateForFunctionPointer<glGetnTexImageDelegate>(getProcAddress("glGetnTexImage"));
		s_glGetnUniformdv = Marshal.GetDelegateForFunctionPointer<glGetnUniformdvDelegate>(getProcAddress("glGetnUniformdv"));
		s_glGetnUniformfv = Marshal.GetDelegateForFunctionPointer<glGetnUniformfvDelegate>(getProcAddress("glGetnUniformfv"));
		s_glGetnUniformiv = Marshal.GetDelegateForFunctionPointer<glGetnUniformivDelegate>(getProcAddress("glGetnUniformiv"));
		s_glGetnUniformuiv = Marshal.GetDelegateForFunctionPointer<glGetnUniformuivDelegate>(getProcAddress("glGetnUniformuiv"));
		s_glHint = Marshal.GetDelegateForFunctionPointer<glHintDelegate>(getProcAddress("glHint"));
		s_glInvalidateBufferData = Marshal.GetDelegateForFunctionPointer<glInvalidateBufferDataDelegate>(getProcAddress("glInvalidateBufferData"));
		s_glInvalidateBufferSubData = Marshal.GetDelegateForFunctionPointer<glInvalidateBufferSubDataDelegate>(getProcAddress("glInvalidateBufferSubData"));
		s_glInvalidateFramebuffer = Marshal.GetDelegateForFunctionPointer<glInvalidateFramebufferDelegate>(getProcAddress("glInvalidateFramebuffer"));
		s_glInvalidateNamedFramebufferData = Marshal.GetDelegateForFunctionPointer<glInvalidateNamedFramebufferDataDelegate>(getProcAddress("glInvalidateNamedFramebufferData"));
		s_glInvalidateNamedFramebufferSubData = Marshal.GetDelegateForFunctionPointer<glInvalidateNamedFramebufferSubDataDelegate>(getProcAddress("glInvalidateNamedFramebufferSubData"));
		s_glInvalidateSubFramebuffer = Marshal.GetDelegateForFunctionPointer<glInvalidateSubFramebufferDelegate>(getProcAddress("glInvalidateSubFramebuffer"));
		s_glInvalidateTexImage = Marshal.GetDelegateForFunctionPointer<glInvalidateTexImageDelegate>(getProcAddress("glInvalidateTexImage"));
		s_glInvalidateTexSubImage = Marshal.GetDelegateForFunctionPointer<glInvalidateTexSubImageDelegate>(getProcAddress("glInvalidateTexSubImage"));
		s_glIsBuffer = Marshal.GetDelegateForFunctionPointer<glIsBufferDelegate>(getProcAddress("glIsBuffer"));
		s_glIsEnabled = Marshal.GetDelegateForFunctionPointer<glIsEnabledDelegate>(getProcAddress("glIsEnabled"));
		s_glIsEnabledi = Marshal.GetDelegateForFunctionPointer<glIsEnablediDelegate>(getProcAddress("glIsEnabledi"));
		s_glIsFramebuffer = Marshal.GetDelegateForFunctionPointer<glIsFramebufferDelegate>(getProcAddress("glIsFramebuffer"));
		s_glIsProgram = Marshal.GetDelegateForFunctionPointer<glIsProgramDelegate>(getProcAddress("glIsProgram"));
		s_glIsProgramPipeline = Marshal.GetDelegateForFunctionPointer<glIsProgramPipelineDelegate>(getProcAddress("glIsProgramPipeline"));
		s_glIsQuery = Marshal.GetDelegateForFunctionPointer<glIsQueryDelegate>(getProcAddress("glIsQuery"));
		s_glIsRenderbuffer = Marshal.GetDelegateForFunctionPointer<glIsRenderbufferDelegate>(getProcAddress("glIsRenderbuffer"));
		s_glIsSampler = Marshal.GetDelegateForFunctionPointer<glIsSamplerDelegate>(getProcAddress("glIsSampler"));
		s_glIsShader = Marshal.GetDelegateForFunctionPointer<glIsShaderDelegate>(getProcAddress("glIsShader"));
		s_glIsSync = Marshal.GetDelegateForFunctionPointer<glIsSyncDelegate>(getProcAddress("glIsSync"));
		s_glIsTexture = Marshal.GetDelegateForFunctionPointer<glIsTextureDelegate>(getProcAddress("glIsTexture"));
		s_glIsTransformFeedback = Marshal.GetDelegateForFunctionPointer<glIsTransformFeedbackDelegate>(getProcAddress("glIsTransformFeedback"));
		s_glIsVertexArray = Marshal.GetDelegateForFunctionPointer<glIsVertexArrayDelegate>(getProcAddress("glIsVertexArray"));
		s_glLineWidth = Marshal.GetDelegateForFunctionPointer<glLineWidthDelegate>(getProcAddress("glLineWidth"));
		s_glLinkProgram = Marshal.GetDelegateForFunctionPointer<glLinkProgramDelegate>(getProcAddress("glLinkProgram"));
		s_glLogicOp = Marshal.GetDelegateForFunctionPointer<glLogicOpDelegate>(getProcAddress("glLogicOp"));
		s_glMapBuffer = Marshal.GetDelegateForFunctionPointer<glMapBufferDelegate>(getProcAddress("glMapBuffer"));
		s_glMapBufferRange = Marshal.GetDelegateForFunctionPointer<glMapBufferRangeDelegate>(getProcAddress("glMapBufferRange"));
		s_glMapNamedBuffer = Marshal.GetDelegateForFunctionPointer<glMapNamedBufferDelegate>(getProcAddress("glMapNamedBuffer"));
		s_glMapNamedBufferRange = Marshal.GetDelegateForFunctionPointer<glMapNamedBufferRangeDelegate>(getProcAddress("glMapNamedBufferRange"));
		s_glMemoryBarrier = Marshal.GetDelegateForFunctionPointer<glMemoryBarrierDelegate>(getProcAddress("glMemoryBarrier"));
		s_glMemoryBarrierByRegion = Marshal.GetDelegateForFunctionPointer<glMemoryBarrierByRegionDelegate>(getProcAddress("glMemoryBarrierByRegion"));
		s_glMinSampleShading = Marshal.GetDelegateForFunctionPointer<glMinSampleShadingDelegate>(getProcAddress("glMinSampleShading"));
		s_glMultiDrawArrays = Marshal.GetDelegateForFunctionPointer<glMultiDrawArraysDelegate>(getProcAddress("glMultiDrawArrays"));
		s_glMultiDrawArraysIndirect = Marshal.GetDelegateForFunctionPointer<glMultiDrawArraysIndirectDelegate>(getProcAddress("glMultiDrawArraysIndirect"));
		s_glMultiDrawArraysIndirectCount = Marshal.GetDelegateForFunctionPointer<glMultiDrawArraysIndirectCountDelegate>(getProcAddress("glMultiDrawArraysIndirectCount"));
		s_glMultiDrawElements = Marshal.GetDelegateForFunctionPointer<glMultiDrawElementsDelegate>(getProcAddress("glMultiDrawElements"));
		s_glMultiDrawElementsBaseVertex = Marshal.GetDelegateForFunctionPointer<glMultiDrawElementsBaseVertexDelegate>(getProcAddress("glMultiDrawElementsBaseVertex"));
		s_glMultiDrawElementsIndirect = Marshal.GetDelegateForFunctionPointer<glMultiDrawElementsIndirectDelegate>(getProcAddress("glMultiDrawElementsIndirect"));
		s_glMultiDrawElementsIndirectCount = Marshal.GetDelegateForFunctionPointer<glMultiDrawElementsIndirectCountDelegate>(getProcAddress("glMultiDrawElementsIndirectCount"));
		s_glMultiTexCoordP1ui = Marshal.GetDelegateForFunctionPointer<glMultiTexCoordP1uiDelegate>(getProcAddress("glMultiTexCoordP1ui"));
		s_glMultiTexCoordP1uiv = Marshal.GetDelegateForFunctionPointer<glMultiTexCoordP1uivDelegate>(getProcAddress("glMultiTexCoordP1uiv"));
		s_glMultiTexCoordP2ui = Marshal.GetDelegateForFunctionPointer<glMultiTexCoordP2uiDelegate>(getProcAddress("glMultiTexCoordP2ui"));
		s_glMultiTexCoordP2uiv = Marshal.GetDelegateForFunctionPointer<glMultiTexCoordP2uivDelegate>(getProcAddress("glMultiTexCoordP2uiv"));
		s_glMultiTexCoordP3ui = Marshal.GetDelegateForFunctionPointer<glMultiTexCoordP3uiDelegate>(getProcAddress("glMultiTexCoordP3ui"));
		s_glMultiTexCoordP3uiv = Marshal.GetDelegateForFunctionPointer<glMultiTexCoordP3uivDelegate>(getProcAddress("glMultiTexCoordP3uiv"));
		s_glMultiTexCoordP4ui = Marshal.GetDelegateForFunctionPointer<glMultiTexCoordP4uiDelegate>(getProcAddress("glMultiTexCoordP4ui"));
		s_glMultiTexCoordP4uiv = Marshal.GetDelegateForFunctionPointer<glMultiTexCoordP4uivDelegate>(getProcAddress("glMultiTexCoordP4uiv"));
		s_glNamedBufferData = Marshal.GetDelegateForFunctionPointer<glNamedBufferDataDelegate>(getProcAddress("glNamedBufferData"));
		s_glNamedBufferStorage = Marshal.GetDelegateForFunctionPointer<glNamedBufferStorageDelegate>(getProcAddress("glNamedBufferStorage"));
		s_glNamedBufferSubData = Marshal.GetDelegateForFunctionPointer<glNamedBufferSubDataDelegate>(getProcAddress("glNamedBufferSubData"));
		s_glNamedFramebufferDrawBuffer = Marshal.GetDelegateForFunctionPointer<glNamedFramebufferDrawBufferDelegate>(getProcAddress("glNamedFramebufferDrawBuffer"));
		s_glNamedFramebufferDrawBuffers = Marshal.GetDelegateForFunctionPointer<glNamedFramebufferDrawBuffersDelegate>(getProcAddress("glNamedFramebufferDrawBuffers"));
		s_glNamedFramebufferParameteri = Marshal.GetDelegateForFunctionPointer<glNamedFramebufferParameteriDelegate>(getProcAddress("glNamedFramebufferParameteri"));
		s_glNamedFramebufferReadBuffer = Marshal.GetDelegateForFunctionPointer<glNamedFramebufferReadBufferDelegate>(getProcAddress("glNamedFramebufferReadBuffer"));
		s_glNamedFramebufferRenderbuffer = Marshal.GetDelegateForFunctionPointer<glNamedFramebufferRenderbufferDelegate>(getProcAddress("glNamedFramebufferRenderbuffer"));
		s_glNamedFramebufferTexture = Marshal.GetDelegateForFunctionPointer<glNamedFramebufferTextureDelegate>(getProcAddress("glNamedFramebufferTexture"));
		s_glNamedFramebufferTextureLayer = Marshal.GetDelegateForFunctionPointer<glNamedFramebufferTextureLayerDelegate>(getProcAddress("glNamedFramebufferTextureLayer"));
		s_glNamedRenderbufferStorage = Marshal.GetDelegateForFunctionPointer<glNamedRenderbufferStorageDelegate>(getProcAddress("glNamedRenderbufferStorage"));
		s_glNamedRenderbufferStorageMultisample = Marshal.GetDelegateForFunctionPointer<glNamedRenderbufferStorageMultisampleDelegate>(getProcAddress("glNamedRenderbufferStorageMultisample"));
		s_glNormalP3ui = Marshal.GetDelegateForFunctionPointer<glNormalP3uiDelegate>(getProcAddress("glNormalP3ui"));
		s_glNormalP3uiv = Marshal.GetDelegateForFunctionPointer<glNormalP3uivDelegate>(getProcAddress("glNormalP3uiv"));
		s_glObjectLabel = Marshal.GetDelegateForFunctionPointer<glObjectLabelDelegate>(getProcAddress("glObjectLabel"));
		s_glObjectPtrLabel = Marshal.GetDelegateForFunctionPointer<glObjectPtrLabelDelegate>(getProcAddress("glObjectPtrLabel"));
		s_glPatchParameterfv = Marshal.GetDelegateForFunctionPointer<glPatchParameterfvDelegate>(getProcAddress("glPatchParameterfv"));
		s_glPatchParameteri = Marshal.GetDelegateForFunctionPointer<glPatchParameteriDelegate>(getProcAddress("glPatchParameteri"));
		s_glPauseTransformFeedback = Marshal.GetDelegateForFunctionPointer<glPauseTransformFeedbackDelegate>(getProcAddress("glPauseTransformFeedback"));
		s_glPixelStoref = Marshal.GetDelegateForFunctionPointer<glPixelStorefDelegate>(getProcAddress("glPixelStoref"));
		s_glPixelStorei = Marshal.GetDelegateForFunctionPointer<glPixelStoreiDelegate>(getProcAddress("glPixelStorei"));
		s_glPointParameterf = Marshal.GetDelegateForFunctionPointer<glPointParameterfDelegate>(getProcAddress("glPointParameterf"));
		s_glPointParameterfv = Marshal.GetDelegateForFunctionPointer<glPointParameterfvDelegate>(getProcAddress("glPointParameterfv"));
		s_glPointParameteri = Marshal.GetDelegateForFunctionPointer<glPointParameteriDelegate>(getProcAddress("glPointParameteri"));
		s_glPointParameteriv = Marshal.GetDelegateForFunctionPointer<glPointParameterivDelegate>(getProcAddress("glPointParameteriv"));
		s_glPointSize = Marshal.GetDelegateForFunctionPointer<glPointSizeDelegate>(getProcAddress("glPointSize"));
		s_glPolygonMode = Marshal.GetDelegateForFunctionPointer<glPolygonModeDelegate>(getProcAddress("glPolygonMode"));
		s_glPolygonOffset = Marshal.GetDelegateForFunctionPointer<glPolygonOffsetDelegate>(getProcAddress("glPolygonOffset"));
		s_glPolygonOffsetClamp = Marshal.GetDelegateForFunctionPointer<glPolygonOffsetClampDelegate>(getProcAddress("glPolygonOffsetClamp"));
		s_glPopDebugGroup = Marshal.GetDelegateForFunctionPointer<glPopDebugGroupDelegate>(getProcAddress("glPopDebugGroup"));
		s_glPrimitiveRestartIndex = Marshal.GetDelegateForFunctionPointer<glPrimitiveRestartIndexDelegate>(getProcAddress("glPrimitiveRestartIndex"));
		s_glProgramBinary = Marshal.GetDelegateForFunctionPointer<glProgramBinaryDelegate>(getProcAddress("glProgramBinary"));
		s_glProgramParameteri = Marshal.GetDelegateForFunctionPointer<glProgramParameteriDelegate>(getProcAddress("glProgramParameteri"));
		s_glProgramUniform1d = Marshal.GetDelegateForFunctionPointer<glProgramUniform1dDelegate>(getProcAddress("glProgramUniform1d"));
		s_glProgramUniform1dv = Marshal.GetDelegateForFunctionPointer<glProgramUniform1dvDelegate>(getProcAddress("glProgramUniform1dv"));
		s_glProgramUniform1f = Marshal.GetDelegateForFunctionPointer<glProgramUniform1fDelegate>(getProcAddress("glProgramUniform1f"));
		s_glProgramUniform1fv = Marshal.GetDelegateForFunctionPointer<glProgramUniform1fvDelegate>(getProcAddress("glProgramUniform1fv"));
		s_glProgramUniform1i = Marshal.GetDelegateForFunctionPointer<glProgramUniform1iDelegate>(getProcAddress("glProgramUniform1i"));
		s_glProgramUniform1iv = Marshal.GetDelegateForFunctionPointer<glProgramUniform1ivDelegate>(getProcAddress("glProgramUniform1iv"));
		s_glProgramUniform1ui = Marshal.GetDelegateForFunctionPointer<glProgramUniform1uiDelegate>(getProcAddress("glProgramUniform1ui"));
		s_glProgramUniform1uiv = Marshal.GetDelegateForFunctionPointer<glProgramUniform1uivDelegate>(getProcAddress("glProgramUniform1uiv"));
		s_glProgramUniform2d = Marshal.GetDelegateForFunctionPointer<glProgramUniform2dDelegate>(getProcAddress("glProgramUniform2d"));
		s_glProgramUniform2dv = Marshal.GetDelegateForFunctionPointer<glProgramUniform2dvDelegate>(getProcAddress("glProgramUniform2dv"));
		s_glProgramUniform2f = Marshal.GetDelegateForFunctionPointer<glProgramUniform2fDelegate>(getProcAddress("glProgramUniform2f"));
		s_glProgramUniform2fv = Marshal.GetDelegateForFunctionPointer<glProgramUniform2fvDelegate>(getProcAddress("glProgramUniform2fv"));
		s_glProgramUniform2i = Marshal.GetDelegateForFunctionPointer<glProgramUniform2iDelegate>(getProcAddress("glProgramUniform2i"));
		s_glProgramUniform2iv = Marshal.GetDelegateForFunctionPointer<glProgramUniform2ivDelegate>(getProcAddress("glProgramUniform2iv"));
		s_glProgramUniform2ui = Marshal.GetDelegateForFunctionPointer<glProgramUniform2uiDelegate>(getProcAddress("glProgramUniform2ui"));
		s_glProgramUniform2uiv = Marshal.GetDelegateForFunctionPointer<glProgramUniform2uivDelegate>(getProcAddress("glProgramUniform2uiv"));
		s_glProgramUniform3d = Marshal.GetDelegateForFunctionPointer<glProgramUniform3dDelegate>(getProcAddress("glProgramUniform3d"));
		s_glProgramUniform3dv = Marshal.GetDelegateForFunctionPointer<glProgramUniform3dvDelegate>(getProcAddress("glProgramUniform3dv"));
		s_glProgramUniform3f = Marshal.GetDelegateForFunctionPointer<glProgramUniform3fDelegate>(getProcAddress("glProgramUniform3f"));
		s_glProgramUniform3fv = Marshal.GetDelegateForFunctionPointer<glProgramUniform3fvDelegate>(getProcAddress("glProgramUniform3fv"));
		s_glProgramUniform3i = Marshal.GetDelegateForFunctionPointer<glProgramUniform3iDelegate>(getProcAddress("glProgramUniform3i"));
		s_glProgramUniform3iv = Marshal.GetDelegateForFunctionPointer<glProgramUniform3ivDelegate>(getProcAddress("glProgramUniform3iv"));
		s_glProgramUniform3ui = Marshal.GetDelegateForFunctionPointer<glProgramUniform3uiDelegate>(getProcAddress("glProgramUniform3ui"));
		s_glProgramUniform3uiv = Marshal.GetDelegateForFunctionPointer<glProgramUniform3uivDelegate>(getProcAddress("glProgramUniform3uiv"));
		s_glProgramUniform4d = Marshal.GetDelegateForFunctionPointer<glProgramUniform4dDelegate>(getProcAddress("glProgramUniform4d"));
		s_glProgramUniform4dv = Marshal.GetDelegateForFunctionPointer<glProgramUniform4dvDelegate>(getProcAddress("glProgramUniform4dv"));
		s_glProgramUniform4f = Marshal.GetDelegateForFunctionPointer<glProgramUniform4fDelegate>(getProcAddress("glProgramUniform4f"));
		s_glProgramUniform4fv = Marshal.GetDelegateForFunctionPointer<glProgramUniform4fvDelegate>(getProcAddress("glProgramUniform4fv"));
		s_glProgramUniform4i = Marshal.GetDelegateForFunctionPointer<glProgramUniform4iDelegate>(getProcAddress("glProgramUniform4i"));
		s_glProgramUniform4iv = Marshal.GetDelegateForFunctionPointer<glProgramUniform4ivDelegate>(getProcAddress("glProgramUniform4iv"));
		s_glProgramUniform4ui = Marshal.GetDelegateForFunctionPointer<glProgramUniform4uiDelegate>(getProcAddress("glProgramUniform4ui"));
		s_glProgramUniform4uiv = Marshal.GetDelegateForFunctionPointer<glProgramUniform4uivDelegate>(getProcAddress("glProgramUniform4uiv"));
		s_glProgramUniformMatrix2dv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix2dvDelegate>(getProcAddress("glProgramUniformMatrix2dv"));
		s_glProgramUniformMatrix2fv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix2fvDelegate>(getProcAddress("glProgramUniformMatrix2fv"));
		s_glProgramUniformMatrix2x3dv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix2x3dvDelegate>(getProcAddress("glProgramUniformMatrix2x3dv"));
		s_glProgramUniformMatrix2x3fv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix2x3fvDelegate>(getProcAddress("glProgramUniformMatrix2x3fv"));
		s_glProgramUniformMatrix2x4dv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix2x4dvDelegate>(getProcAddress("glProgramUniformMatrix2x4dv"));
		s_glProgramUniformMatrix2x4fv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix2x4fvDelegate>(getProcAddress("glProgramUniformMatrix2x4fv"));
		s_glProgramUniformMatrix3dv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix3dvDelegate>(getProcAddress("glProgramUniformMatrix3dv"));
		s_glProgramUniformMatrix3fv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix3fvDelegate>(getProcAddress("glProgramUniformMatrix3fv"));
		s_glProgramUniformMatrix3x2dv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix3x2dvDelegate>(getProcAddress("glProgramUniformMatrix3x2dv"));
		s_glProgramUniformMatrix3x2fv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix3x2fvDelegate>(getProcAddress("glProgramUniformMatrix3x2fv"));
		s_glProgramUniformMatrix3x4dv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix3x4dvDelegate>(getProcAddress("glProgramUniformMatrix3x4dv"));
		s_glProgramUniformMatrix3x4fv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix3x4fvDelegate>(getProcAddress("glProgramUniformMatrix3x4fv"));
		s_glProgramUniformMatrix4dv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix4dvDelegate>(getProcAddress("glProgramUniformMatrix4dv"));
		s_glProgramUniformMatrix4fv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix4fvDelegate>(getProcAddress("glProgramUniformMatrix4fv"));
		s_glProgramUniformMatrix4x2dv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix4x2dvDelegate>(getProcAddress("glProgramUniformMatrix4x2dv"));
		s_glProgramUniformMatrix4x2fv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix4x2fvDelegate>(getProcAddress("glProgramUniformMatrix4x2fv"));
		s_glProgramUniformMatrix4x3dv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix4x3dvDelegate>(getProcAddress("glProgramUniformMatrix4x3dv"));
		s_glProgramUniformMatrix4x3fv = Marshal.GetDelegateForFunctionPointer<glProgramUniformMatrix4x3fvDelegate>(getProcAddress("glProgramUniformMatrix4x3fv"));
		s_glProvokingVertex = Marshal.GetDelegateForFunctionPointer<glProvokingVertexDelegate>(getProcAddress("glProvokingVertex"));
		s_glPushDebugGroup = Marshal.GetDelegateForFunctionPointer<glPushDebugGroupDelegate>(getProcAddress("glPushDebugGroup"));
		s_glQueryCounter = Marshal.GetDelegateForFunctionPointer<glQueryCounterDelegate>(getProcAddress("glQueryCounter"));
		s_glReadBuffer = Marshal.GetDelegateForFunctionPointer<glReadBufferDelegate>(getProcAddress("glReadBuffer"));
		s_glReadPixels = Marshal.GetDelegateForFunctionPointer<glReadPixelsDelegate>(getProcAddress("glReadPixels"));
		s_glReadnPixels = Marshal.GetDelegateForFunctionPointer<glReadnPixelsDelegate>(getProcAddress("glReadnPixels"));
		s_glReleaseShaderCompiler = Marshal.GetDelegateForFunctionPointer<glReleaseShaderCompilerDelegate>(getProcAddress("glReleaseShaderCompiler"));
		s_glRenderbufferStorage = Marshal.GetDelegateForFunctionPointer<glRenderbufferStorageDelegate>(getProcAddress("glRenderbufferStorage"));
		s_glRenderbufferStorageMultisample = Marshal.GetDelegateForFunctionPointer<glRenderbufferStorageMultisampleDelegate>(getProcAddress("glRenderbufferStorageMultisample"));
		s_glResumeTransformFeedback = Marshal.GetDelegateForFunctionPointer<glResumeTransformFeedbackDelegate>(getProcAddress("glResumeTransformFeedback"));
		s_glSampleCoverage = Marshal.GetDelegateForFunctionPointer<glSampleCoverageDelegate>(getProcAddress("glSampleCoverage"));
		s_glSampleMaski = Marshal.GetDelegateForFunctionPointer<glSampleMaskiDelegate>(getProcAddress("glSampleMaski"));
		s_glSamplerParameterIiv = Marshal.GetDelegateForFunctionPointer<glSamplerParameterIivDelegate>(getProcAddress("glSamplerParameterIiv"));
		s_glSamplerParameterIuiv = Marshal.GetDelegateForFunctionPointer<glSamplerParameterIuivDelegate>(getProcAddress("glSamplerParameterIuiv"));
		s_glSamplerParameterf = Marshal.GetDelegateForFunctionPointer<glSamplerParameterfDelegate>(getProcAddress("glSamplerParameterf"));
		s_glSamplerParameterfv = Marshal.GetDelegateForFunctionPointer<glSamplerParameterfvDelegate>(getProcAddress("glSamplerParameterfv"));
		s_glSamplerParameteri = Marshal.GetDelegateForFunctionPointer<glSamplerParameteriDelegate>(getProcAddress("glSamplerParameteri"));
		s_glSamplerParameteriv = Marshal.GetDelegateForFunctionPointer<glSamplerParameterivDelegate>(getProcAddress("glSamplerParameteriv"));
		s_glScissor = Marshal.GetDelegateForFunctionPointer<glScissorDelegate>(getProcAddress("glScissor"));
		s_glScissorArrayv = Marshal.GetDelegateForFunctionPointer<glScissorArrayvDelegate>(getProcAddress("glScissorArrayv"));
		s_glScissorIndexed = Marshal.GetDelegateForFunctionPointer<glScissorIndexedDelegate>(getProcAddress("glScissorIndexed"));
		s_glScissorIndexedv = Marshal.GetDelegateForFunctionPointer<glScissorIndexedvDelegate>(getProcAddress("glScissorIndexedv"));
		s_glSecondaryColorP3ui = Marshal.GetDelegateForFunctionPointer<glSecondaryColorP3uiDelegate>(getProcAddress("glSecondaryColorP3ui"));
		s_glSecondaryColorP3uiv = Marshal.GetDelegateForFunctionPointer<glSecondaryColorP3uivDelegate>(getProcAddress("glSecondaryColorP3uiv"));
		s_glShaderBinary = Marshal.GetDelegateForFunctionPointer<glShaderBinaryDelegate>(getProcAddress("glShaderBinary"));
		s_glShaderSource = Marshal.GetDelegateForFunctionPointer<glShaderSourceDelegate>(getProcAddress("glShaderSource"));
		s_glShaderStorageBlockBinding = Marshal.GetDelegateForFunctionPointer<glShaderStorageBlockBindingDelegate>(getProcAddress("glShaderStorageBlockBinding"));
		s_glSpecializeShader = Marshal.GetDelegateForFunctionPointer<glSpecializeShaderDelegate>(getProcAddress("glSpecializeShader"));
		s_glStencilFunc = Marshal.GetDelegateForFunctionPointer<glStencilFuncDelegate>(getProcAddress("glStencilFunc"));
		s_glStencilFuncSeparate = Marshal.GetDelegateForFunctionPointer<glStencilFuncSeparateDelegate>(getProcAddress("glStencilFuncSeparate"));
		s_glStencilMask = Marshal.GetDelegateForFunctionPointer<glStencilMaskDelegate>(getProcAddress("glStencilMask"));
		s_glStencilMaskSeparate = Marshal.GetDelegateForFunctionPointer<glStencilMaskSeparateDelegate>(getProcAddress("glStencilMaskSeparate"));
		s_glStencilOp = Marshal.GetDelegateForFunctionPointer<glStencilOpDelegate>(getProcAddress("glStencilOp"));
		s_glStencilOpSeparate = Marshal.GetDelegateForFunctionPointer<glStencilOpSeparateDelegate>(getProcAddress("glStencilOpSeparate"));
		s_glTexBuffer = Marshal.GetDelegateForFunctionPointer<glTexBufferDelegate>(getProcAddress("glTexBuffer"));
		s_glTexBufferRange = Marshal.GetDelegateForFunctionPointer<glTexBufferRangeDelegate>(getProcAddress("glTexBufferRange"));
		s_glTexCoordP1ui = Marshal.GetDelegateForFunctionPointer<glTexCoordP1uiDelegate>(getProcAddress("glTexCoordP1ui"));
		s_glTexCoordP1uiv = Marshal.GetDelegateForFunctionPointer<glTexCoordP1uivDelegate>(getProcAddress("glTexCoordP1uiv"));
		s_glTexCoordP2ui = Marshal.GetDelegateForFunctionPointer<glTexCoordP2uiDelegate>(getProcAddress("glTexCoordP2ui"));
		s_glTexCoordP2uiv = Marshal.GetDelegateForFunctionPointer<glTexCoordP2uivDelegate>(getProcAddress("glTexCoordP2uiv"));
		s_glTexCoordP3ui = Marshal.GetDelegateForFunctionPointer<glTexCoordP3uiDelegate>(getProcAddress("glTexCoordP3ui"));
		s_glTexCoordP3uiv = Marshal.GetDelegateForFunctionPointer<glTexCoordP3uivDelegate>(getProcAddress("glTexCoordP3uiv"));
		s_glTexCoordP4ui = Marshal.GetDelegateForFunctionPointer<glTexCoordP4uiDelegate>(getProcAddress("glTexCoordP4ui"));
		s_glTexCoordP4uiv = Marshal.GetDelegateForFunctionPointer<glTexCoordP4uivDelegate>(getProcAddress("glTexCoordP4uiv"));
		s_glTexImage1D = Marshal.GetDelegateForFunctionPointer<glTexImage1DDelegate>(getProcAddress("glTexImage1D"));
		s_glTexImage2D = Marshal.GetDelegateForFunctionPointer<glTexImage2DDelegate>(getProcAddress("glTexImage2D"));
		s_glTexImage2DMultisample = Marshal.GetDelegateForFunctionPointer<glTexImage2DMultisampleDelegate>(getProcAddress("glTexImage2DMultisample"));
		s_glTexImage3D = Marshal.GetDelegateForFunctionPointer<glTexImage3DDelegate>(getProcAddress("glTexImage3D"));
		s_glTexImage3DMultisample = Marshal.GetDelegateForFunctionPointer<glTexImage3DMultisampleDelegate>(getProcAddress("glTexImage3DMultisample"));
		s_glTexParameterIiv = Marshal.GetDelegateForFunctionPointer<glTexParameterIivDelegate>(getProcAddress("glTexParameterIiv"));
		s_glTexParameterIuiv = Marshal.GetDelegateForFunctionPointer<glTexParameterIuivDelegate>(getProcAddress("glTexParameterIuiv"));
		s_glTexParameterf = Marshal.GetDelegateForFunctionPointer<glTexParameterfDelegate>(getProcAddress("glTexParameterf"));
		s_glTexParameterfv = Marshal.GetDelegateForFunctionPointer<glTexParameterfvDelegate>(getProcAddress("glTexParameterfv"));
		s_glTexParameteri = Marshal.GetDelegateForFunctionPointer<glTexParameteriDelegate>(getProcAddress("glTexParameteri"));
		s_glTexParameteriv = Marshal.GetDelegateForFunctionPointer<glTexParameterivDelegate>(getProcAddress("glTexParameteriv"));
		s_glTexStorage1D = Marshal.GetDelegateForFunctionPointer<glTexStorage1DDelegate>(getProcAddress("glTexStorage1D"));
		s_glTexStorage2D = Marshal.GetDelegateForFunctionPointer<glTexStorage2DDelegate>(getProcAddress("glTexStorage2D"));
		s_glTexStorage2DMultisample = Marshal.GetDelegateForFunctionPointer<glTexStorage2DMultisampleDelegate>(getProcAddress("glTexStorage2DMultisample"));
		s_glTexStorage3D = Marshal.GetDelegateForFunctionPointer<glTexStorage3DDelegate>(getProcAddress("glTexStorage3D"));
		s_glTexStorage3DMultisample = Marshal.GetDelegateForFunctionPointer<glTexStorage3DMultisampleDelegate>(getProcAddress("glTexStorage3DMultisample"));
		s_glTexSubImage1D = Marshal.GetDelegateForFunctionPointer<glTexSubImage1DDelegate>(getProcAddress("glTexSubImage1D"));
		s_glTexSubImage2D = Marshal.GetDelegateForFunctionPointer<glTexSubImage2DDelegate>(getProcAddress("glTexSubImage2D"));
		s_glTexSubImage3D = Marshal.GetDelegateForFunctionPointer<glTexSubImage3DDelegate>(getProcAddress("glTexSubImage3D"));
		s_glTextureBarrier = Marshal.GetDelegateForFunctionPointer<glTextureBarrierDelegate>(getProcAddress("glTextureBarrier"));
		s_glTextureBuffer = Marshal.GetDelegateForFunctionPointer<glTextureBufferDelegate>(getProcAddress("glTextureBuffer"));
		s_glTextureBufferRange = Marshal.GetDelegateForFunctionPointer<glTextureBufferRangeDelegate>(getProcAddress("glTextureBufferRange"));
		s_glTextureParameterIiv = Marshal.GetDelegateForFunctionPointer<glTextureParameterIivDelegate>(getProcAddress("glTextureParameterIiv"));
		s_glTextureParameterIuiv = Marshal.GetDelegateForFunctionPointer<glTextureParameterIuivDelegate>(getProcAddress("glTextureParameterIuiv"));
		s_glTextureParameterf = Marshal.GetDelegateForFunctionPointer<glTextureParameterfDelegate>(getProcAddress("glTextureParameterf"));
		s_glTextureParameterfv = Marshal.GetDelegateForFunctionPointer<glTextureParameterfvDelegate>(getProcAddress("glTextureParameterfv"));
		s_glTextureParameteri = Marshal.GetDelegateForFunctionPointer<glTextureParameteriDelegate>(getProcAddress("glTextureParameteri"));
		s_glTextureParameteriv = Marshal.GetDelegateForFunctionPointer<glTextureParameterivDelegate>(getProcAddress("glTextureParameteriv"));
		s_glTextureStorage1D = Marshal.GetDelegateForFunctionPointer<glTextureStorage1DDelegate>(getProcAddress("glTextureStorage1D"));
		s_glTextureStorage2D = Marshal.GetDelegateForFunctionPointer<glTextureStorage2DDelegate>(getProcAddress("glTextureStorage2D"));
		s_glTextureStorage2DMultisample = Marshal.GetDelegateForFunctionPointer<glTextureStorage2DMultisampleDelegate>(getProcAddress("glTextureStorage2DMultisample"));
		s_glTextureStorage3D = Marshal.GetDelegateForFunctionPointer<glTextureStorage3DDelegate>(getProcAddress("glTextureStorage3D"));
		s_glTextureStorage3DMultisample = Marshal.GetDelegateForFunctionPointer<glTextureStorage3DMultisampleDelegate>(getProcAddress("glTextureStorage3DMultisample"));
		s_glTextureSubImage1D = Marshal.GetDelegateForFunctionPointer<glTextureSubImage1DDelegate>(getProcAddress("glTextureSubImage1D"));
		s_glTextureSubImage2D = Marshal.GetDelegateForFunctionPointer<glTextureSubImage2DDelegate>(getProcAddress("glTextureSubImage2D"));
		s_glTextureSubImage3D = Marshal.GetDelegateForFunctionPointer<glTextureSubImage3DDelegate>(getProcAddress("glTextureSubImage3D"));
		s_glTextureView = Marshal.GetDelegateForFunctionPointer<glTextureViewDelegate>(getProcAddress("glTextureView"));
		s_glTransformFeedbackBufferBase = Marshal.GetDelegateForFunctionPointer<glTransformFeedbackBufferBaseDelegate>(getProcAddress("glTransformFeedbackBufferBase"));
		s_glTransformFeedbackBufferRange = Marshal.GetDelegateForFunctionPointer<glTransformFeedbackBufferRangeDelegate>(getProcAddress("glTransformFeedbackBufferRange"));
		s_glTransformFeedbackVaryings = Marshal.GetDelegateForFunctionPointer<glTransformFeedbackVaryingsDelegate>(getProcAddress("glTransformFeedbackVaryings"));
		s_glUniform1d = Marshal.GetDelegateForFunctionPointer<glUniform1dDelegate>(getProcAddress("glUniform1d"));
		s_glUniform1dv = Marshal.GetDelegateForFunctionPointer<glUniform1dvDelegate>(getProcAddress("glUniform1dv"));
		s_glUniform1f = Marshal.GetDelegateForFunctionPointer<glUniform1fDelegate>(getProcAddress("glUniform1f"));
		s_glUniform1fv = Marshal.GetDelegateForFunctionPointer<glUniform1fvDelegate>(getProcAddress("glUniform1fv"));
		s_glUniform1i = Marshal.GetDelegateForFunctionPointer<glUniform1iDelegate>(getProcAddress("glUniform1i"));
		s_glUniform1iv = Marshal.GetDelegateForFunctionPointer<glUniform1ivDelegate>(getProcAddress("glUniform1iv"));
		s_glUniform1ui = Marshal.GetDelegateForFunctionPointer<glUniform1uiDelegate>(getProcAddress("glUniform1ui"));
		s_glUniform1uiv = Marshal.GetDelegateForFunctionPointer<glUniform1uivDelegate>(getProcAddress("glUniform1uiv"));
		s_glUniform2d = Marshal.GetDelegateForFunctionPointer<glUniform2dDelegate>(getProcAddress("glUniform2d"));
		s_glUniform2dv = Marshal.GetDelegateForFunctionPointer<glUniform2dvDelegate>(getProcAddress("glUniform2dv"));
		s_glUniform2f = Marshal.GetDelegateForFunctionPointer<glUniform2fDelegate>(getProcAddress("glUniform2f"));
		s_glUniform2fv = Marshal.GetDelegateForFunctionPointer<glUniform2fvDelegate>(getProcAddress("glUniform2fv"));
		s_glUniform2i = Marshal.GetDelegateForFunctionPointer<glUniform2iDelegate>(getProcAddress("glUniform2i"));
		s_glUniform2iv = Marshal.GetDelegateForFunctionPointer<glUniform2ivDelegate>(getProcAddress("glUniform2iv"));
		s_glUniform2ui = Marshal.GetDelegateForFunctionPointer<glUniform2uiDelegate>(getProcAddress("glUniform2ui"));
		s_glUniform2uiv = Marshal.GetDelegateForFunctionPointer<glUniform2uivDelegate>(getProcAddress("glUniform2uiv"));
		s_glUniform3d = Marshal.GetDelegateForFunctionPointer<glUniform3dDelegate>(getProcAddress("glUniform3d"));
		s_glUniform3dv = Marshal.GetDelegateForFunctionPointer<glUniform3dvDelegate>(getProcAddress("glUniform3dv"));
		s_glUniform3f = Marshal.GetDelegateForFunctionPointer<glUniform3fDelegate>(getProcAddress("glUniform3f"));
		s_glUniform3fv = Marshal.GetDelegateForFunctionPointer<glUniform3fvDelegate>(getProcAddress("glUniform3fv"));
		s_glUniform3i = Marshal.GetDelegateForFunctionPointer<glUniform3iDelegate>(getProcAddress("glUniform3i"));
		s_glUniform3iv = Marshal.GetDelegateForFunctionPointer<glUniform3ivDelegate>(getProcAddress("glUniform3iv"));
		s_glUniform3ui = Marshal.GetDelegateForFunctionPointer<glUniform3uiDelegate>(getProcAddress("glUniform3ui"));
		s_glUniform3uiv = Marshal.GetDelegateForFunctionPointer<glUniform3uivDelegate>(getProcAddress("glUniform3uiv"));
		s_glUniform4d = Marshal.GetDelegateForFunctionPointer<glUniform4dDelegate>(getProcAddress("glUniform4d"));
		s_glUniform4dv = Marshal.GetDelegateForFunctionPointer<glUniform4dvDelegate>(getProcAddress("glUniform4dv"));
		s_glUniform4f = Marshal.GetDelegateForFunctionPointer<glUniform4fDelegate>(getProcAddress("glUniform4f"));
		s_glUniform4fv = Marshal.GetDelegateForFunctionPointer<glUniform4fvDelegate>(getProcAddress("glUniform4fv"));
		s_glUniform4i = Marshal.GetDelegateForFunctionPointer<glUniform4iDelegate>(getProcAddress("glUniform4i"));
		s_glUniform4iv = Marshal.GetDelegateForFunctionPointer<glUniform4ivDelegate>(getProcAddress("glUniform4iv"));
		s_glUniform4ui = Marshal.GetDelegateForFunctionPointer<glUniform4uiDelegate>(getProcAddress("glUniform4ui"));
		s_glUniform4uiv = Marshal.GetDelegateForFunctionPointer<glUniform4uivDelegate>(getProcAddress("glUniform4uiv"));
		s_glUniformBlockBinding = Marshal.GetDelegateForFunctionPointer<glUniformBlockBindingDelegate>(getProcAddress("glUniformBlockBinding"));
		s_glUniformMatrix2dv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix2dvDelegate>(getProcAddress("glUniformMatrix2dv"));
		s_glUniformMatrix2fv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix2fvDelegate>(getProcAddress("glUniformMatrix2fv"));
		s_glUniformMatrix2x3dv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix2x3dvDelegate>(getProcAddress("glUniformMatrix2x3dv"));
		s_glUniformMatrix2x3fv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix2x3fvDelegate>(getProcAddress("glUniformMatrix2x3fv"));
		s_glUniformMatrix2x4dv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix2x4dvDelegate>(getProcAddress("glUniformMatrix2x4dv"));
		s_glUniformMatrix2x4fv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix2x4fvDelegate>(getProcAddress("glUniformMatrix2x4fv"));
		s_glUniformMatrix3dv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix3dvDelegate>(getProcAddress("glUniformMatrix3dv"));
		s_glUniformMatrix3fv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix3fvDelegate>(getProcAddress("glUniformMatrix3fv"));
		s_glUniformMatrix3x2dv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix3x2dvDelegate>(getProcAddress("glUniformMatrix3x2dv"));
		s_glUniformMatrix3x2fv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix3x2fvDelegate>(getProcAddress("glUniformMatrix3x2fv"));
		s_glUniformMatrix3x4dv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix3x4dvDelegate>(getProcAddress("glUniformMatrix3x4dv"));
		s_glUniformMatrix3x4fv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix3x4fvDelegate>(getProcAddress("glUniformMatrix3x4fv"));
		s_glUniformMatrix4dv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix4dvDelegate>(getProcAddress("glUniformMatrix4dv"));
		s_glUniformMatrix4fv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix4fvDelegate>(getProcAddress("glUniformMatrix4fv"));
		s_glUniformMatrix4x2dv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix4x2dvDelegate>(getProcAddress("glUniformMatrix4x2dv"));
		s_glUniformMatrix4x2fv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix4x2fvDelegate>(getProcAddress("glUniformMatrix4x2fv"));
		s_glUniformMatrix4x3dv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix4x3dvDelegate>(getProcAddress("glUniformMatrix4x3dv"));
		s_glUniformMatrix4x3fv = Marshal.GetDelegateForFunctionPointer<glUniformMatrix4x3fvDelegate>(getProcAddress("glUniformMatrix4x3fv"));
		s_glUniformSubroutinesuiv = Marshal.GetDelegateForFunctionPointer<glUniformSubroutinesuivDelegate>(getProcAddress("glUniformSubroutinesuiv"));
		s_glUnmapBuffer = Marshal.GetDelegateForFunctionPointer<glUnmapBufferDelegate>(getProcAddress("glUnmapBuffer"));
		s_glUnmapNamedBuffer = Marshal.GetDelegateForFunctionPointer<glUnmapNamedBufferDelegate>(getProcAddress("glUnmapNamedBuffer"));
		s_glUseProgram = Marshal.GetDelegateForFunctionPointer<glUseProgramDelegate>(getProcAddress("glUseProgram"));
		s_glUseProgramStages = Marshal.GetDelegateForFunctionPointer<glUseProgramStagesDelegate>(getProcAddress("glUseProgramStages"));
		s_glValidateProgram = Marshal.GetDelegateForFunctionPointer<glValidateProgramDelegate>(getProcAddress("glValidateProgram"));
		s_glValidateProgramPipeline = Marshal.GetDelegateForFunctionPointer<glValidateProgramPipelineDelegate>(getProcAddress("glValidateProgramPipeline"));
		s_glVertexArrayAttribBinding = Marshal.GetDelegateForFunctionPointer<glVertexArrayAttribBindingDelegate>(getProcAddress("glVertexArrayAttribBinding"));
		s_glVertexArrayAttribFormat = Marshal.GetDelegateForFunctionPointer<glVertexArrayAttribFormatDelegate>(getProcAddress("glVertexArrayAttribFormat"));
		s_glVertexArrayAttribIFormat = Marshal.GetDelegateForFunctionPointer<glVertexArrayAttribIFormatDelegate>(getProcAddress("glVertexArrayAttribIFormat"));
		s_glVertexArrayAttribLFormat = Marshal.GetDelegateForFunctionPointer<glVertexArrayAttribLFormatDelegate>(getProcAddress("glVertexArrayAttribLFormat"));
		s_glVertexArrayBindingDivisor = Marshal.GetDelegateForFunctionPointer<glVertexArrayBindingDivisorDelegate>(getProcAddress("glVertexArrayBindingDivisor"));
		s_glVertexArrayElementBuffer = Marshal.GetDelegateForFunctionPointer<glVertexArrayElementBufferDelegate>(getProcAddress("glVertexArrayElementBuffer"));
		s_glVertexArrayVertexBuffer = Marshal.GetDelegateForFunctionPointer<glVertexArrayVertexBufferDelegate>(getProcAddress("glVertexArrayVertexBuffer"));
		s_glVertexArrayVertexBuffers = Marshal.GetDelegateForFunctionPointer<glVertexArrayVertexBuffersDelegate>(getProcAddress("glVertexArrayVertexBuffers"));
		s_glVertexAttrib1d = Marshal.GetDelegateForFunctionPointer<glVertexAttrib1dDelegate>(getProcAddress("glVertexAttrib1d"));
		s_glVertexAttrib1dv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib1dvDelegate>(getProcAddress("glVertexAttrib1dv"));
		s_glVertexAttrib1f = Marshal.GetDelegateForFunctionPointer<glVertexAttrib1fDelegate>(getProcAddress("glVertexAttrib1f"));
		s_glVertexAttrib1fv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib1fvDelegate>(getProcAddress("glVertexAttrib1fv"));
		s_glVertexAttrib1s = Marshal.GetDelegateForFunctionPointer<glVertexAttrib1sDelegate>(getProcAddress("glVertexAttrib1s"));
		s_glVertexAttrib1sv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib1svDelegate>(getProcAddress("glVertexAttrib1sv"));
		s_glVertexAttrib2d = Marshal.GetDelegateForFunctionPointer<glVertexAttrib2dDelegate>(getProcAddress("glVertexAttrib2d"));
		s_glVertexAttrib2dv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib2dvDelegate>(getProcAddress("glVertexAttrib2dv"));
		s_glVertexAttrib2f = Marshal.GetDelegateForFunctionPointer<glVertexAttrib2fDelegate>(getProcAddress("glVertexAttrib2f"));
		s_glVertexAttrib2fv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib2fvDelegate>(getProcAddress("glVertexAttrib2fv"));
		s_glVertexAttrib2s = Marshal.GetDelegateForFunctionPointer<glVertexAttrib2sDelegate>(getProcAddress("glVertexAttrib2s"));
		s_glVertexAttrib2sv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib2svDelegate>(getProcAddress("glVertexAttrib2sv"));
		s_glVertexAttrib3d = Marshal.GetDelegateForFunctionPointer<glVertexAttrib3dDelegate>(getProcAddress("glVertexAttrib3d"));
		s_glVertexAttrib3dv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib3dvDelegate>(getProcAddress("glVertexAttrib3dv"));
		s_glVertexAttrib3f = Marshal.GetDelegateForFunctionPointer<glVertexAttrib3fDelegate>(getProcAddress("glVertexAttrib3f"));
		s_glVertexAttrib3fv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib3fvDelegate>(getProcAddress("glVertexAttrib3fv"));
		s_glVertexAttrib3s = Marshal.GetDelegateForFunctionPointer<glVertexAttrib3sDelegate>(getProcAddress("glVertexAttrib3s"));
		s_glVertexAttrib3sv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib3svDelegate>(getProcAddress("glVertexAttrib3sv"));
		s_glVertexAttrib4Nbv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4NbvDelegate>(getProcAddress("glVertexAttrib4Nbv"));
		s_glVertexAttrib4Niv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4NivDelegate>(getProcAddress("glVertexAttrib4Niv"));
		s_glVertexAttrib4Nsv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4NsvDelegate>(getProcAddress("glVertexAttrib4Nsv"));
		s_glVertexAttrib4Nub = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4NubDelegate>(getProcAddress("glVertexAttrib4Nub"));
		s_glVertexAttrib4Nubv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4NubvDelegate>(getProcAddress("glVertexAttrib4Nubv"));
		s_glVertexAttrib4Nuiv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4NuivDelegate>(getProcAddress("glVertexAttrib4Nuiv"));
		s_glVertexAttrib4Nusv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4NusvDelegate>(getProcAddress("glVertexAttrib4Nusv"));
		s_glVertexAttrib4bv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4bvDelegate>(getProcAddress("glVertexAttrib4bv"));
		s_glVertexAttrib4d = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4dDelegate>(getProcAddress("glVertexAttrib4d"));
		s_glVertexAttrib4dv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4dvDelegate>(getProcAddress("glVertexAttrib4dv"));
		s_glVertexAttrib4f = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4fDelegate>(getProcAddress("glVertexAttrib4f"));
		s_glVertexAttrib4fv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4fvDelegate>(getProcAddress("glVertexAttrib4fv"));
		s_glVertexAttrib4iv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4ivDelegate>(getProcAddress("glVertexAttrib4iv"));
		s_glVertexAttrib4s = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4sDelegate>(getProcAddress("glVertexAttrib4s"));
		s_glVertexAttrib4sv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4svDelegate>(getProcAddress("glVertexAttrib4sv"));
		s_glVertexAttrib4ubv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4ubvDelegate>(getProcAddress("glVertexAttrib4ubv"));
		s_glVertexAttrib4uiv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4uivDelegate>(getProcAddress("glVertexAttrib4uiv"));
		s_glVertexAttrib4usv = Marshal.GetDelegateForFunctionPointer<glVertexAttrib4usvDelegate>(getProcAddress("glVertexAttrib4usv"));
		s_glVertexAttribBinding = Marshal.GetDelegateForFunctionPointer<glVertexAttribBindingDelegate>(getProcAddress("glVertexAttribBinding"));
		s_glVertexAttribDivisor = Marshal.GetDelegateForFunctionPointer<glVertexAttribDivisorDelegate>(getProcAddress("glVertexAttribDivisor"));
		s_glVertexAttribFormat = Marshal.GetDelegateForFunctionPointer<glVertexAttribFormatDelegate>(getProcAddress("glVertexAttribFormat"));
		s_glVertexAttribI1i = Marshal.GetDelegateForFunctionPointer<glVertexAttribI1iDelegate>(getProcAddress("glVertexAttribI1i"));
		s_glVertexAttribI1iv = Marshal.GetDelegateForFunctionPointer<glVertexAttribI1ivDelegate>(getProcAddress("glVertexAttribI1iv"));
		s_glVertexAttribI1ui = Marshal.GetDelegateForFunctionPointer<glVertexAttribI1uiDelegate>(getProcAddress("glVertexAttribI1ui"));
		s_glVertexAttribI1uiv = Marshal.GetDelegateForFunctionPointer<glVertexAttribI1uivDelegate>(getProcAddress("glVertexAttribI1uiv"));
		s_glVertexAttribI2i = Marshal.GetDelegateForFunctionPointer<glVertexAttribI2iDelegate>(getProcAddress("glVertexAttribI2i"));
		s_glVertexAttribI2iv = Marshal.GetDelegateForFunctionPointer<glVertexAttribI2ivDelegate>(getProcAddress("glVertexAttribI2iv"));
		s_glVertexAttribI2ui = Marshal.GetDelegateForFunctionPointer<glVertexAttribI2uiDelegate>(getProcAddress("glVertexAttribI2ui"));
		s_glVertexAttribI2uiv = Marshal.GetDelegateForFunctionPointer<glVertexAttribI2uivDelegate>(getProcAddress("glVertexAttribI2uiv"));
		s_glVertexAttribI3i = Marshal.GetDelegateForFunctionPointer<glVertexAttribI3iDelegate>(getProcAddress("glVertexAttribI3i"));
		s_glVertexAttribI3iv = Marshal.GetDelegateForFunctionPointer<glVertexAttribI3ivDelegate>(getProcAddress("glVertexAttribI3iv"));
		s_glVertexAttribI3ui = Marshal.GetDelegateForFunctionPointer<glVertexAttribI3uiDelegate>(getProcAddress("glVertexAttribI3ui"));
		s_glVertexAttribI3uiv = Marshal.GetDelegateForFunctionPointer<glVertexAttribI3uivDelegate>(getProcAddress("glVertexAttribI3uiv"));
		s_glVertexAttribI4bv = Marshal.GetDelegateForFunctionPointer<glVertexAttribI4bvDelegate>(getProcAddress("glVertexAttribI4bv"));
		s_glVertexAttribI4i = Marshal.GetDelegateForFunctionPointer<glVertexAttribI4iDelegate>(getProcAddress("glVertexAttribI4i"));
		s_glVertexAttribI4iv = Marshal.GetDelegateForFunctionPointer<glVertexAttribI4ivDelegate>(getProcAddress("glVertexAttribI4iv"));
		s_glVertexAttribI4sv = Marshal.GetDelegateForFunctionPointer<glVertexAttribI4svDelegate>(getProcAddress("glVertexAttribI4sv"));
		s_glVertexAttribI4ubv = Marshal.GetDelegateForFunctionPointer<glVertexAttribI4ubvDelegate>(getProcAddress("glVertexAttribI4ubv"));
		s_glVertexAttribI4ui = Marshal.GetDelegateForFunctionPointer<glVertexAttribI4uiDelegate>(getProcAddress("glVertexAttribI4ui"));
		s_glVertexAttribI4uiv = Marshal.GetDelegateForFunctionPointer<glVertexAttribI4uivDelegate>(getProcAddress("glVertexAttribI4uiv"));
		s_glVertexAttribI4usv = Marshal.GetDelegateForFunctionPointer<glVertexAttribI4usvDelegate>(getProcAddress("glVertexAttribI4usv"));
		s_glVertexAttribIFormat = Marshal.GetDelegateForFunctionPointer<glVertexAttribIFormatDelegate>(getProcAddress("glVertexAttribIFormat"));
		s_glVertexAttribIPointer = Marshal.GetDelegateForFunctionPointer<glVertexAttribIPointerDelegate>(getProcAddress("glVertexAttribIPointer"));
		s_glVertexAttribL1d = Marshal.GetDelegateForFunctionPointer<glVertexAttribL1dDelegate>(getProcAddress("glVertexAttribL1d"));
		s_glVertexAttribL1dv = Marshal.GetDelegateForFunctionPointer<glVertexAttribL1dvDelegate>(getProcAddress("glVertexAttribL1dv"));
		s_glVertexAttribL2d = Marshal.GetDelegateForFunctionPointer<glVertexAttribL2dDelegate>(getProcAddress("glVertexAttribL2d"));
		s_glVertexAttribL2dv = Marshal.GetDelegateForFunctionPointer<glVertexAttribL2dvDelegate>(getProcAddress("glVertexAttribL2dv"));
		s_glVertexAttribL3d = Marshal.GetDelegateForFunctionPointer<glVertexAttribL3dDelegate>(getProcAddress("glVertexAttribL3d"));
		s_glVertexAttribL3dv = Marshal.GetDelegateForFunctionPointer<glVertexAttribL3dvDelegate>(getProcAddress("glVertexAttribL3dv"));
		s_glVertexAttribL4d = Marshal.GetDelegateForFunctionPointer<glVertexAttribL4dDelegate>(getProcAddress("glVertexAttribL4d"));
		s_glVertexAttribL4dv = Marshal.GetDelegateForFunctionPointer<glVertexAttribL4dvDelegate>(getProcAddress("glVertexAttribL4dv"));
		s_glVertexAttribLFormat = Marshal.GetDelegateForFunctionPointer<glVertexAttribLFormatDelegate>(getProcAddress("glVertexAttribLFormat"));
		s_glVertexAttribLPointer = Marshal.GetDelegateForFunctionPointer<glVertexAttribLPointerDelegate>(getProcAddress("glVertexAttribLPointer"));
		s_glVertexAttribP1ui = Marshal.GetDelegateForFunctionPointer<glVertexAttribP1uiDelegate>(getProcAddress("glVertexAttribP1ui"));
		s_glVertexAttribP1uiv = Marshal.GetDelegateForFunctionPointer<glVertexAttribP1uivDelegate>(getProcAddress("glVertexAttribP1uiv"));
		s_glVertexAttribP2ui = Marshal.GetDelegateForFunctionPointer<glVertexAttribP2uiDelegate>(getProcAddress("glVertexAttribP2ui"));
		s_glVertexAttribP2uiv = Marshal.GetDelegateForFunctionPointer<glVertexAttribP2uivDelegate>(getProcAddress("glVertexAttribP2uiv"));
		s_glVertexAttribP3ui = Marshal.GetDelegateForFunctionPointer<glVertexAttribP3uiDelegate>(getProcAddress("glVertexAttribP3ui"));
		s_glVertexAttribP3uiv = Marshal.GetDelegateForFunctionPointer<glVertexAttribP3uivDelegate>(getProcAddress("glVertexAttribP3uiv"));
		s_glVertexAttribP4ui = Marshal.GetDelegateForFunctionPointer<glVertexAttribP4uiDelegate>(getProcAddress("glVertexAttribP4ui"));
		s_glVertexAttribP4uiv = Marshal.GetDelegateForFunctionPointer<glVertexAttribP4uivDelegate>(getProcAddress("glVertexAttribP4uiv"));
		s_glVertexAttribPointer = Marshal.GetDelegateForFunctionPointer<glVertexAttribPointerDelegate>(getProcAddress("glVertexAttribPointer"));
		s_glVertexBindingDivisor = Marshal.GetDelegateForFunctionPointer<glVertexBindingDivisorDelegate>(getProcAddress("glVertexBindingDivisor"));
		s_glVertexP2ui = Marshal.GetDelegateForFunctionPointer<glVertexP2uiDelegate>(getProcAddress("glVertexP2ui"));
		s_glVertexP2uiv = Marshal.GetDelegateForFunctionPointer<glVertexP2uivDelegate>(getProcAddress("glVertexP2uiv"));
		s_glVertexP3ui = Marshal.GetDelegateForFunctionPointer<glVertexP3uiDelegate>(getProcAddress("glVertexP3ui"));
		s_glVertexP3uiv = Marshal.GetDelegateForFunctionPointer<glVertexP3uivDelegate>(getProcAddress("glVertexP3uiv"));
		s_glVertexP4ui = Marshal.GetDelegateForFunctionPointer<glVertexP4uiDelegate>(getProcAddress("glVertexP4ui"));
		s_glVertexP4uiv = Marshal.GetDelegateForFunctionPointer<glVertexP4uivDelegate>(getProcAddress("glVertexP4uiv"));
		s_glViewport = Marshal.GetDelegateForFunctionPointer<glViewportDelegate>(getProcAddress("glViewport"));
		s_glViewportArrayv = Marshal.GetDelegateForFunctionPointer<glViewportArrayvDelegate>(getProcAddress("glViewportArrayv"));
		s_glViewportIndexedf = Marshal.GetDelegateForFunctionPointer<glViewportIndexedfDelegate>(getProcAddress("glViewportIndexedf"));
		s_glViewportIndexedfv = Marshal.GetDelegateForFunctionPointer<glViewportIndexedfvDelegate>(getProcAddress("glViewportIndexedfv"));
		s_glWaitSync = Marshal.GetDelegateForFunctionPointer<glWaitSyncDelegate>(getProcAddress("glWaitSync"));
	}
}
