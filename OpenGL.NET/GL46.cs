using System.Security;
// ReSharper disable InconsistentNaming

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
	public const long GL_ALL_BARRIER_BITS = 0xFFFFFFFF;
	public const int GL_SYNC_FLUSH_COMMANDS_BIT = 0x00000001;
	public const int GL_VERTEX_SHADER_BIT = 0x00000001;
	public const int GL_FRAGMENT_SHADER_BIT = 0x00000002;
	public const int GL_GEOMETRY_SHADER_BIT = 0x00000004;
	public const int GL_TESS_CONTROL_SHADER_BIT = 0x00000008;
	public const int GL_TESS_EVALUATION_SHADER_BIT = 0x00000010;
	public const int GL_COMPUTE_SHADER_BIT = 0x00000020;
	public const long GL_ALL_SHADER_BITS = 0xFFFFFFFF;
	public const int GL_FALSE = 0;
	public const int GL_NO_ERROR = 0;
	public const int GL_ZERO = 0;
	public const int GL_NONE = 0;
	public const int GL_TRUE = 1;
	public const int GL_ONE = 1;
	public const long GL_INVALID_INDEX = 0xFFFFFFFF;
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
	public const int GL_COMPRESSED_RGBA_BPTC_UNORM_ARB = 0x8E8C;

	private static void* s_glActiveShaderProgram;
	public static void glActiveShaderProgram(uint pipeline, uint program) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glActiveShaderProgram)(pipeline, program);

	private static void* s_glActiveTexture;
	public static void glActiveTexture(int texture) => ((delegate* unmanaged[Cdecl]<int, void>)s_glActiveTexture)(texture);

	private static void* s_glAttachShader;
	public static void glAttachShader(uint program, uint shader) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glAttachShader)(program, shader);

	private static void* s_glBeginConditionalRender;
	public static void glBeginConditionalRender(uint id, int mode) => ((delegate* unmanaged[Cdecl]<uint, int, void>)s_glBeginConditionalRender)(id, mode);

	private static void* s_glBeginQuery;
	public static void glBeginQuery(int target, uint id) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glBeginQuery)(target, id);

	private static void* s_glBeginQueryIndexed;
	public static void glBeginQueryIndexed(int target, uint index, uint id) => ((delegate* unmanaged[Cdecl]<int, uint, uint, void>)s_glBeginQueryIndexed)(target, index, id);

	private static void* s_glBeginTransformFeedback;
	public static void glBeginTransformFeedback(int primitiveMode) => ((delegate* unmanaged[Cdecl]<int, void>)s_glBeginTransformFeedback)(primitiveMode);

	private static void* s_glBindAttribLocation;
	public static void glBindAttribLocation(uint program, uint index, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, byte*, void>)s_glBindAttribLocation)(program, index, name);

	private static void* s_glBindBuffer;
	public static void glBindBuffer(int target, uint buffer) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glBindBuffer)(target, buffer);

	private static void* s_glBindBufferBase;
	public static void glBindBufferBase(int target, uint index, uint buffer) => ((delegate* unmanaged[Cdecl]<int, uint, uint, void>)s_glBindBufferBase)(target, index, buffer);

	private static void* s_glBindBufferRange;
	public static void glBindBufferRange(int target, uint index, uint buffer, IntPtr offset, IntPtr size) => ((delegate* unmanaged[Cdecl]<int, uint, uint, IntPtr, IntPtr, void>)s_glBindBufferRange)(target, index, buffer, offset, size);

	private static void* s_glBindBuffersBase;
	public static void glBindBuffersBase(int target, uint first, int count, uint* buffers) => ((delegate* unmanaged[Cdecl]<int, uint, int, uint*, void>)s_glBindBuffersBase)(target, first, count, buffers);

	private static void* s_glBindBuffersRange;
	public static void glBindBuffersRange(int target, uint first, int count, uint* buffers, IntPtr* offsets, IntPtr* sizes) => ((delegate* unmanaged[Cdecl]<int, uint, int, uint*, IntPtr*, IntPtr*, void>)s_glBindBuffersRange)(target, first, count, buffers, offsets, sizes);

	private static void* s_glBindFragDataLocation;
	public static void glBindFragDataLocation(uint program, uint color, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, byte*, void>)s_glBindFragDataLocation)(program, color, name);

	private static void* s_glBindFragDataLocationIndexed;
	public static void glBindFragDataLocationIndexed(uint program, uint colorNumber, uint index, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, byte*, void>)s_glBindFragDataLocationIndexed)(program, colorNumber, index, name);

	private static void* s_glBindFramebuffer;
	public static void glBindFramebuffer(int target, uint framebuffer) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glBindFramebuffer)(target, framebuffer);

	private static void* s_glBindImageTexture;
	public static void glBindImageTexture(uint unit, uint texture, int level, bool layered, int layer, int access, int format) => ((delegate* unmanaged[Cdecl]<uint, uint, int, bool, int, int, int, void>)s_glBindImageTexture)(unit, texture, level, layered, layer, access, format);

	private static void* s_glBindImageTextures;
	public static void glBindImageTextures(uint first, int count, uint* textures) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glBindImageTextures)(first, count, textures);

	private static void* s_glBindProgramPipeline;
	public static void glBindProgramPipeline(uint pipeline) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glBindProgramPipeline)(pipeline);

	private static void* s_glBindRenderbuffer;
	public static void glBindRenderbuffer(int target, uint renderbuffer) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glBindRenderbuffer)(target, renderbuffer);

	private static void* s_glBindSampler;
	public static void glBindSampler(uint unit, uint sampler) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glBindSampler)(unit, sampler);

	private static void* s_glBindSamplers;
	public static void glBindSamplers(uint first, int count, uint* samplers) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glBindSamplers)(first, count, samplers);

	private static void* s_glBindTexture;
	public static void glBindTexture(int target, uint texture) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glBindTexture)(target, texture);

	private static void* s_glBindTextureUnit;
	public static void glBindTextureUnit(uint unit, uint texture) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glBindTextureUnit)(unit, texture);

	private static void* s_glBindTextures;
	public static void glBindTextures(uint first, int count, uint* textures) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glBindTextures)(first, count, textures);

	private static void* s_glBindTransformFeedback;
	public static void glBindTransformFeedback(int target, uint id) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glBindTransformFeedback)(target, id);

	private static void* s_glBindVertexArray;
	public static void glBindVertexArray(uint array) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glBindVertexArray)(array);

	private static void* s_glBindVertexBuffer;
	public static void glBindVertexBuffer(uint bindingindex, uint buffer, IntPtr offset, int stride) => ((delegate* unmanaged[Cdecl]<uint, uint, IntPtr, int, void>)s_glBindVertexBuffer)(bindingindex, buffer, offset, stride);

	private static void* s_glBindVertexBuffers;
	public static void glBindVertexBuffers(uint first, int count, uint* buffers, IntPtr* offsets, int* strides) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, IntPtr*, int*, void>)s_glBindVertexBuffers)(first, count, buffers, offsets, strides);

	private static void* s_glBlendColor;
	public static void glBlendColor(float red, float green, float blue, float alpha) => ((delegate* unmanaged[Cdecl]<float, float, float, float, void>)s_glBlendColor)(red, green, blue, alpha);

	private static void* s_glBlendEquation;
	public static void glBlendEquation(int mode) => ((delegate* unmanaged[Cdecl]<int, void>)s_glBlendEquation)(mode);

	private static void* s_glBlendEquationSeparate;
	public static void glBlendEquationSeparate(int modeRGB, int modeAlpha) => ((delegate* unmanaged[Cdecl]<int, int, void>)s_glBlendEquationSeparate)(modeRGB, modeAlpha);

	private static void* s_glBlendEquationSeparatei;
	public static void glBlendEquationSeparatei(uint buf, int modeRGB, int modeAlpha) => ((delegate* unmanaged[Cdecl]<uint, int, int, void>)s_glBlendEquationSeparatei)(buf, modeRGB, modeAlpha);

	private static void* s_glBlendEquationi;
	public static void glBlendEquationi(uint buf, int mode) => ((delegate* unmanaged[Cdecl]<uint, int, void>)s_glBlendEquationi)(buf, mode);

	private static void* s_glBlendFunc;
	public static void glBlendFunc(int sfactor, int dfactor) => ((delegate* unmanaged[Cdecl]<int, int, void>)s_glBlendFunc)(sfactor, dfactor);

	private static void* s_glBlendFuncSeparate;
	public static void glBlendFuncSeparate(int sfactorRGB, int dfactorRGB, int sfactorAlpha, int dfactorAlpha) => ((delegate* unmanaged[Cdecl]<int, int, int, int, void>)s_glBlendFuncSeparate)(sfactorRGB, dfactorRGB, sfactorAlpha, dfactorAlpha);

	private static void* s_glBlendFuncSeparatei;
	public static void glBlendFuncSeparatei(uint buf, int srcRGB, int dstRGB, int srcAlpha, int dstAlpha) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, void>)s_glBlendFuncSeparatei)(buf, srcRGB, dstRGB, srcAlpha, dstAlpha);

	private static void* s_glBlendFunci;
	public static void glBlendFunci(uint buf, int src, int dst) => ((delegate* unmanaged[Cdecl]<uint, int, int, void>)s_glBlendFunci)(buf, src, dst);

	private static void* s_glBlitFramebuffer;
	public static void glBlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, int mask, int filter) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, int, int, int, void>)s_glBlitFramebuffer)(srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter);

	private static void* s_glBlitNamedFramebuffer;
	public static void glBlitNamedFramebuffer(uint readFramebuffer, uint drawFramebuffer, int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, int mask, int filter) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int, int, int, int, int, int, int, int, int, void>)s_glBlitNamedFramebuffer)(readFramebuffer, drawFramebuffer, srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter);

	private static void* s_glBufferData;
	public static void glBufferData(int target, IntPtr size, void* data, int usage) => ((delegate* unmanaged[Cdecl]<int, IntPtr, void*, int, void>)s_glBufferData)(target, size, data, usage);

	private static void* s_glBufferStorage;
	public static void glBufferStorage(int target, IntPtr size, void* data, int flags) => ((delegate* unmanaged[Cdecl]<int, IntPtr, void*, int, void>)s_glBufferStorage)(target, size, data, flags);

	private static void* s_glBufferSubData;
	public static void glBufferSubData(int target, IntPtr offset, IntPtr size, void* data) => ((delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, void*, void>)s_glBufferSubData)(target, offset, size, data);

	private static void* s_glCheckFramebufferStatus;
	public static int glCheckFramebufferStatus(int target) => ((delegate* unmanaged[Cdecl]<int, int>)s_glCheckFramebufferStatus)(target);

	private static void* s_glCheckNamedFramebufferStatus;
	public static int glCheckNamedFramebufferStatus(uint framebuffer, int target) => ((delegate* unmanaged[Cdecl]<uint, int, int>)s_glCheckNamedFramebufferStatus)(framebuffer, target);

	private static void* s_glClampColor;
	public static void glClampColor(int target, int clamp) => ((delegate* unmanaged[Cdecl]<int, int, void>)s_glClampColor)(target, clamp);

	private static void* s_glClear;
	public static void glClear(int mask) => ((delegate* unmanaged[Cdecl]<int, void>)s_glClear)(mask);

	private static void* s_glClearBufferData;
	public static void glClearBufferData(int target, int internalformat, int format, int type, void* data) => ((delegate* unmanaged[Cdecl]<int, int, int, int, void*, void>)s_glClearBufferData)(target, internalformat, format, type, data);

	private static void* s_glClearBufferSubData;
	public static void glClearBufferSubData(int target, int internalformat, IntPtr offset, IntPtr size, int format, int type, void* data) => ((delegate* unmanaged[Cdecl]<int, int, IntPtr, IntPtr, int, int, void*, void>)s_glClearBufferSubData)(target, internalformat, offset, size, format, type, data);

	private static void* s_glClearBufferfi;
	public static void glClearBufferfi(int buffer, int drawbuffer, float depth, int stencil) => ((delegate* unmanaged[Cdecl]<int, int, float, int, void>)s_glClearBufferfi)(buffer, drawbuffer, depth, stencil);

	private static void* s_glClearBufferfv;
	public static void glClearBufferfv(int buffer, int drawbuffer, float* value) => ((delegate* unmanaged[Cdecl]<int, int, float*, void>)s_glClearBufferfv)(buffer, drawbuffer, value);

	private static void* s_glClearBufferiv;
	public static void glClearBufferiv(int buffer, int drawbuffer, int* value) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glClearBufferiv)(buffer, drawbuffer, value);

	private static void* s_glClearBufferuiv;
	public static void glClearBufferuiv(int buffer, int drawbuffer, uint* value) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glClearBufferuiv)(buffer, drawbuffer, value);

	private static void* s_glClearColor;
	public static void glClearColor(float red, float green, float blue, float alpha) => ((delegate* unmanaged[Cdecl]<float, float, float, float, void>)s_glClearColor)(red, green, blue, alpha);

	private static void* s_glClearDepth;
	public static void glClearDepth(double depth) => ((delegate* unmanaged[Cdecl]<double, void>)s_glClearDepth)(depth);

	private static void* s_glClearDepthf;
	public static void glClearDepthf(float d) => ((delegate* unmanaged[Cdecl]<float, void>)s_glClearDepthf)(d);

	private static void* s_glClearNamedBufferData;
	public static void glClearNamedBufferData(uint buffer, int internalformat, int format, int type, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, void*, void>)s_glClearNamedBufferData)(buffer, internalformat, format, type, data);

	private static void* s_glClearNamedBufferSubData;
	public static void glClearNamedBufferSubData(uint buffer, int internalformat, IntPtr offset, IntPtr size, int format, int type, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, IntPtr, IntPtr, int, int, void*, void>)s_glClearNamedBufferSubData)(buffer, internalformat, offset, size, format, type, data);

	private static void* s_glClearNamedFramebufferfi;
	public static void glClearNamedFramebufferfi(uint framebuffer, int buffer, int drawbuffer, float depth, int stencil) => ((delegate* unmanaged[Cdecl]<uint, int, int, float, int, void>)s_glClearNamedFramebufferfi)(framebuffer, buffer, drawbuffer, depth, stencil);

	private static void* s_glClearNamedFramebufferfv;
	public static void glClearNamedFramebufferfv(uint framebuffer, int buffer, int drawbuffer, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, float*, void>)s_glClearNamedFramebufferfv)(framebuffer, buffer, drawbuffer, value);

	private static void* s_glClearNamedFramebufferiv;
	public static void glClearNamedFramebufferiv(uint framebuffer, int buffer, int drawbuffer, int* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, int*, void>)s_glClearNamedFramebufferiv)(framebuffer, buffer, drawbuffer, value);

	private static void* s_glClearNamedFramebufferuiv;
	public static void glClearNamedFramebufferuiv(uint framebuffer, int buffer, int drawbuffer, uint* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, uint*, void>)s_glClearNamedFramebufferuiv)(framebuffer, buffer, drawbuffer, value);

	private static void* s_glClearStencil;
	public static void glClearStencil(int s) => ((delegate* unmanaged[Cdecl]<int, void>)s_glClearStencil)(s);

	private static void* s_glClearTexImage;
	public static void glClearTexImage(uint texture, int level, int format, int type, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, void*, void>)s_glClearTexImage)(texture, level, format, type, data);

	private static void* s_glClearTexSubImage;
	public static void glClearTexSubImage(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, int, int, void*, void>)s_glClearTexSubImage)(texture, level, xoffset, yoffset, zoffset, width, height, depth, format, type, data);

	private static void* s_glClientWaitSync;
	public static int glClientWaitSync(IntPtr sync, int flags, ulong timeout) => ((delegate* unmanaged[Cdecl]<IntPtr, int, ulong, int>)s_glClientWaitSync)(sync, flags, timeout);

	private static void* s_glClipControl;
	public static void glClipControl(int origin, int depth) => ((delegate* unmanaged[Cdecl]<int, int, void>)s_glClipControl)(origin, depth);

	private static void* s_glColorMask;
	public static void glColorMask(bool red, bool green, bool blue, bool alpha) => ((delegate* unmanaged[Cdecl]<bool, bool, bool, bool, void>)s_glColorMask)(red, green, blue, alpha);

	private static void* s_glColorMaski;
	public static void glColorMaski(uint index, bool r, bool g, bool b, bool a) => ((delegate* unmanaged[Cdecl]<uint, bool, bool, bool, bool, void>)s_glColorMaski)(index, r, g, b, a);

	private static void* s_glColorP3ui;
	public static void glColorP3ui(int type, uint color) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glColorP3ui)(type, color);

	private static void* s_glColorP3uiv;
	public static void glColorP3uiv(int type, uint* color) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glColorP3uiv)(type, color);

	private static void* s_glColorP4ui;
	public static void glColorP4ui(int type, uint color) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glColorP4ui)(type, color);

	private static void* s_glColorP4uiv;
	public static void glColorP4uiv(int type, uint* color) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glColorP4uiv)(type, color);

	private static void* s_glCompileShader;
	public static void glCompileShader(uint shader) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glCompileShader)(shader);

	private static void* s_glCompressedTexImage1D;
	public static void glCompressedTexImage1D(int target, int level, int internalformat, int width, int border, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, void*, void>)s_glCompressedTexImage1D)(target, level, internalformat, width, border, imageSize, data);

	private static void* s_glCompressedTexImage2D;
	public static void glCompressedTexImage2D(int target, int level, int internalformat, int width, int height, int border, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, void*, void>)s_glCompressedTexImage2D)(target, level, internalformat, width, height, border, imageSize, data);

	private static void* s_glCompressedTexImage3D;
	public static void glCompressedTexImage3D(int target, int level, int internalformat, int width, int height, int depth, int border, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, int, void*, void>)s_glCompressedTexImage3D)(target, level, internalformat, width, height, depth, border, imageSize, data);

	private static void* s_glCompressedTexSubImage1D;
	public static void glCompressedTexSubImage1D(int target, int level, int xoffset, int width, int format, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, void*, void>)s_glCompressedTexSubImage1D)(target, level, xoffset, width, format, imageSize, data);

	private static void* s_glCompressedTexSubImage2D;
	public static void glCompressedTexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, int, void*, void>)s_glCompressedTexSubImage2D)(target, level, xoffset, yoffset, width, height, format, imageSize, data);

	private static void* s_glCompressedTexSubImage3D;
	public static void glCompressedTexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, int, int, int, void*, void>)s_glCompressedTexSubImage3D)(target, level, xoffset, yoffset, zoffset, width, height, depth, format, imageSize, data);

	private static void* s_glCompressedTextureSubImage1D;
	public static void glCompressedTextureSubImage1D(uint texture, int level, int xoffset, int width, int format, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, void*, void>)s_glCompressedTextureSubImage1D)(texture, level, xoffset, width, format, imageSize, data);

	private static void* s_glCompressedTextureSubImage2D;
	public static void glCompressedTextureSubImage2D(uint texture, int level, int xoffset, int yoffset, int width, int height, int format, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, void*, void>)s_glCompressedTextureSubImage2D)(texture, level, xoffset, yoffset, width, height, format, imageSize, data);

	private static void* s_glCompressedTextureSubImage3D;
	public static void glCompressedTextureSubImage3D(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int imageSize, void* data) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, int, int, void*, void>)s_glCompressedTextureSubImage3D)(texture, level, xoffset, yoffset, zoffset, width, height, depth, format, imageSize, data);

	private static void* s_glCopyBufferSubData;
	public static void glCopyBufferSubData(int readTarget, int writeTarget, IntPtr readOffset, IntPtr writeOffset, IntPtr size) => ((delegate* unmanaged[Cdecl]<int, int, IntPtr, IntPtr, IntPtr, void>)s_glCopyBufferSubData)(readTarget, writeTarget, readOffset, writeOffset, size);

	private static void* s_glCopyImageSubData;
	public static void glCopyImageSubData(uint srcName, int srcTarget, int srcLevel, int srcX, int srcY, int srcZ, uint dstName, int dstTarget, int dstLevel, int dstX, int dstY, int dstZ, int srcWidth, int srcHeight, int srcDepth) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, uint, int, int, int, int, int, int, int, int, void>)s_glCopyImageSubData)(srcName, srcTarget, srcLevel, srcX, srcY, srcZ, dstName, dstTarget, dstLevel, dstX, dstY, dstZ, srcWidth, srcHeight, srcDepth);

	private static void* s_glCopyNamedBufferSubData;
	public static void glCopyNamedBufferSubData(uint readBuffer, uint writeBuffer, IntPtr readOffset, IntPtr writeOffset, IntPtr size) => ((delegate* unmanaged[Cdecl]<uint, uint, IntPtr, IntPtr, IntPtr, void>)s_glCopyNamedBufferSubData)(readBuffer, writeBuffer, readOffset, writeOffset, size);

	private static void* s_glCopyTexImage1D;
	public static void glCopyTexImage1D(int target, int level, int internalformat, int x, int y, int width, int border) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, void>)s_glCopyTexImage1D)(target, level, internalformat, x, y, width, border);

	private static void* s_glCopyTexImage2D;
	public static void glCopyTexImage2D(int target, int level, int internalformat, int x, int y, int width, int height, int border) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, int, void>)s_glCopyTexImage2D)(target, level, internalformat, x, y, width, height, border);

	private static void* s_glCopyTexSubImage1D;
	public static void glCopyTexSubImage1D(int target, int level, int xoffset, int x, int y, int width) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, void>)s_glCopyTexSubImage1D)(target, level, xoffset, x, y, width);

	private static void* s_glCopyTexSubImage2D;
	public static void glCopyTexSubImage2D(int target, int level, int xoffset, int yoffset, int x, int y, int width, int height) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, int, void>)s_glCopyTexSubImage2D)(target, level, xoffset, yoffset, x, y, width, height);

	private static void* s_glCopyTexSubImage3D;
	public static void glCopyTexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int x, int y, int width, int height) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, int, int, void>)s_glCopyTexSubImage3D)(target, level, xoffset, yoffset, zoffset, x, y, width, height);

	private static void* s_glCopyTextureSubImage1D;
	public static void glCopyTextureSubImage1D(uint texture, int level, int xoffset, int x, int y, int width) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, void>)s_glCopyTextureSubImage1D)(texture, level, xoffset, x, y, width);

	private static void* s_glCopyTextureSubImage2D;
	public static void glCopyTextureSubImage2D(uint texture, int level, int xoffset, int yoffset, int x, int y, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, void>)s_glCopyTextureSubImage2D)(texture, level, xoffset, yoffset, x, y, width, height);

	private static void* s_glCopyTextureSubImage3D;
	public static void glCopyTextureSubImage3D(uint texture, int level, int xoffset, int yoffset, int zoffset, int x, int y, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, int, void>)s_glCopyTextureSubImage3D)(texture, level, xoffset, yoffset, zoffset, x, y, width, height);

	private static void* s_glCreateBuffers;
	public static void glCreateBuffers(int n, uint* buffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glCreateBuffers)(n, buffers);

	private static void* s_glCreateFramebuffers;
	public static void glCreateFramebuffers(int n, uint* framebuffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glCreateFramebuffers)(n, framebuffers);

	private static void* s_glCreateProgram;
	public static uint glCreateProgram() => ((delegate* unmanaged[Cdecl]<uint>)s_glCreateProgram)();

	private static void* s_glCreateProgramPipelines;
	public static void glCreateProgramPipelines(int n, uint* pipelines) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glCreateProgramPipelines)(n, pipelines);

	private static void* s_glCreateQueries;
	public static void glCreateQueries(int target, int n, uint* ids) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glCreateQueries)(target, n, ids);

	private static void* s_glCreateRenderbuffers;
	public static void glCreateRenderbuffers(int n, uint* renderbuffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glCreateRenderbuffers)(n, renderbuffers);

	private static void* s_glCreateSamplers;
	public static void glCreateSamplers(int n, uint* samplers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glCreateSamplers)(n, samplers);

	private static void* s_glCreateShader;
	public static uint glCreateShader(int type) => ((delegate* unmanaged[Cdecl]<int, uint>)s_glCreateShader)(type);

	private static void* s_glCreateShaderProgramv;
	public static uint glCreateShaderProgramv(int type, int count, byte* strs) => ((delegate* unmanaged[Cdecl]<int, int, byte*, uint>)s_glCreateShaderProgramv)(type, count, strs);

	private static void* s_glCreateTextures;
	public static void glCreateTextures(int target, int n, uint* textures) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glCreateTextures)(target, n, textures);

	private static void* s_glCreateTransformFeedbacks;
	public static void glCreateTransformFeedbacks(int n, uint* ids) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glCreateTransformFeedbacks)(n, ids);

	private static void* s_glCreateVertexArrays;
	public static void glCreateVertexArrays(int n, uint* arrays) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glCreateVertexArrays)(n, arrays);

	private static void* s_glCullFace;
	public static void glCullFace(int mode) => ((delegate* unmanaged[Cdecl]<int, void>)s_glCullFace)(mode);

	private static void* s_glDebugMessageCallback;
	public static void glDebugMessageCallback(IntPtr callback, void* userParam) => ((delegate* unmanaged[Cdecl]<IntPtr, void*, void>)s_glDebugMessageCallback)(callback, userParam);

	private static void* s_glDebugMessageControl;
	public static void glDebugMessageControl(int source, int type, int severity, int count, uint* ids, bool enabled) => ((delegate* unmanaged[Cdecl]<int, int, int, int, uint*, bool, void>)s_glDebugMessageControl)(source, type, severity, count, ids, enabled);

	private static void* s_glDebugMessageInsert;
	public static void glDebugMessageInsert(int source, int type, uint id, int severity, int length, byte* buf) => ((delegate* unmanaged[Cdecl]<int, int, uint, int, int, byte*, void>)s_glDebugMessageInsert)(source, type, id, severity, length, buf);

	private static void* s_glDeleteBuffers;
	public static void glDeleteBuffers(int n, uint* buffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteBuffers)(n, buffers);

	private static void* s_glDeleteFramebuffers;
	public static void glDeleteFramebuffers(int n, uint* framebuffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteFramebuffers)(n, framebuffers);

	private static void* s_glDeleteProgram;
	public static void glDeleteProgram(uint program) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glDeleteProgram)(program);

	private static void* s_glDeleteProgramPipelines;
	public static void glDeleteProgramPipelines(int n, uint* pipelines) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteProgramPipelines)(n, pipelines);

	private static void* s_glDeleteQueries;
	public static void glDeleteQueries(int n, uint* ids) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteQueries)(n, ids);

	private static void* s_glDeleteRenderbuffers;
	public static void glDeleteRenderbuffers(int n, uint* renderbuffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteRenderbuffers)(n, renderbuffers);

	private static void* s_glDeleteSamplers;
	public static void glDeleteSamplers(int count, uint* samplers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteSamplers)(count, samplers);

	private static void* s_glDeleteShader;
	public static void glDeleteShader(uint shader) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glDeleteShader)(shader);

	private static void* s_glDeleteSync;
	public static void glDeleteSync(IntPtr sync) => ((delegate* unmanaged[Cdecl]<IntPtr, void>)s_glDeleteSync)(sync);

	private static void* s_glDeleteTextures;
	public static void glDeleteTextures(int n, uint* textures) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteTextures)(n, textures);

	private static void* s_glDeleteTransformFeedbacks;
	public static void glDeleteTransformFeedbacks(int n, uint* ids) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteTransformFeedbacks)(n, ids);

	private static void* s_glDeleteVertexArrays;
	public static void glDeleteVertexArrays(int n, uint* arrays) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glDeleteVertexArrays)(n, arrays);

	private static void* s_glDepthFunc;
	public static void glDepthFunc(int func) => ((delegate* unmanaged[Cdecl]<int, void>)s_glDepthFunc)(func);

	private static void* s_glDepthMask;
	public static void glDepthMask(bool flag) => ((delegate* unmanaged[Cdecl]<bool, void>)s_glDepthMask)(flag);

	private static void* s_glDepthRange;
	public static void glDepthRange(double n, double f) => ((delegate* unmanaged[Cdecl]<double, double, void>)s_glDepthRange)(n, f);

	private static void* s_glDepthRangeArrayv;
	public static void glDepthRangeArrayv(uint first, int count, double* v) => ((delegate* unmanaged[Cdecl]<uint, int, double*, void>)s_glDepthRangeArrayv)(first, count, v);

	private static void* s_glDepthRangeIndexed;
	public static void glDepthRangeIndexed(uint index, double n, double f) => ((delegate* unmanaged[Cdecl]<uint, double, double, void>)s_glDepthRangeIndexed)(index, n, f);

	private static void* s_glDepthRangef;
	public static void glDepthRangef(float n, float f) => ((delegate* unmanaged[Cdecl]<float, float, void>)s_glDepthRangef)(n, f);

	private static void* s_glDetachShader;
	public static void glDetachShader(uint program, uint shader) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glDetachShader)(program, shader);

	private static void* s_glDisable;
	public static void glDisable(int cap) => ((delegate* unmanaged[Cdecl]<int, void>)s_glDisable)(cap);

	private static void* s_glDisableVertexArrayAttrib;
	public static void glDisableVertexArrayAttrib(uint vaobj, uint index) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glDisableVertexArrayAttrib)(vaobj, index);

	private static void* s_glDisableVertexAttribArray;
	public static void glDisableVertexAttribArray(uint index) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glDisableVertexAttribArray)(index);

	private static void* s_glDisablei;
	public static void glDisablei(int target, uint index) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glDisablei)(target, index);

	private static void* s_glDispatchCompute;
	public static void glDispatchCompute(uint num_groups_x, uint num_groups_y, uint num_groups_z) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glDispatchCompute)(num_groups_x, num_groups_y, num_groups_z);

	private static void* s_glDispatchComputeIndirect;
	public static void glDispatchComputeIndirect(IntPtr indirect) => ((delegate* unmanaged[Cdecl]<IntPtr, void>)s_glDispatchComputeIndirect)(indirect);

	private static void* s_glDrawArrays;
	public static void glDrawArrays(int mode, int first, int count) => ((delegate* unmanaged[Cdecl]<int, int, int, void>)s_glDrawArrays)(mode, first, count);

	private static void* s_glDrawArraysIndirect;
	public static void glDrawArraysIndirect(int mode, void* indirect) => ((delegate* unmanaged[Cdecl]<int, void*, void>)s_glDrawArraysIndirect)(mode, indirect);

	private static void* s_glDrawArraysInstanced;
	public static void glDrawArraysInstanced(int mode, int first, int count, int instancecount) => ((delegate* unmanaged[Cdecl]<int, int, int, int, void>)s_glDrawArraysInstanced)(mode, first, count, instancecount);

	private static void* s_glDrawArraysInstancedBaseInstance;
	public static void glDrawArraysInstancedBaseInstance(int mode, int first, int count, int instancecount, uint baseinstance) => ((delegate* unmanaged[Cdecl]<int, int, int, int, uint, void>)s_glDrawArraysInstancedBaseInstance)(mode, first, count, instancecount, baseinstance);

	private static void* s_glDrawBuffer;
	public static void glDrawBuffer(int buf) => ((delegate* unmanaged[Cdecl]<int, void>)s_glDrawBuffer)(buf);

	private static void* s_glDrawBuffers;
	public static void glDrawBuffers(int n, int* bufs) => ((delegate* unmanaged[Cdecl]<int, int*, void>)s_glDrawBuffers)(n, bufs);

	private static void* s_glDrawElements;
	public static void glDrawElements(int mode, int count, int type, void* indices) => ((delegate* unmanaged[Cdecl]<int, int, int, void*, void>)s_glDrawElements)(mode, count, type, indices);

	private static void* s_glDrawElementsBaseVertex;
	public static void glDrawElementsBaseVertex(int mode, int count, int type, void* indices, int basevertex) => ((delegate* unmanaged[Cdecl]<int, int, int, void*, int, void>)s_glDrawElementsBaseVertex)(mode, count, type, indices, basevertex);

	private static void* s_glDrawElementsIndirect;
	public static void glDrawElementsIndirect(int mode, int type, void* indirect) => ((delegate* unmanaged[Cdecl]<int, int, void*, void>)s_glDrawElementsIndirect)(mode, type, indirect);

	private static void* s_glDrawElementsInstanced;
	public static void glDrawElementsInstanced(int mode, int count, int type, void* indices, int instancecount) => ((delegate* unmanaged[Cdecl]<int, int, int, void*, int, void>)s_glDrawElementsInstanced)(mode, count, type, indices, instancecount);

	private static void* s_glDrawElementsInstancedBaseInstance;
	public static void glDrawElementsInstancedBaseInstance(int mode, int count, int type, void* indices, int instancecount, uint baseinstance) => ((delegate* unmanaged[Cdecl]<int, int, int, void*, int, uint, void>)s_glDrawElementsInstancedBaseInstance)(mode, count, type, indices, instancecount, baseinstance);

	private static void* s_glDrawElementsInstancedBaseVertex;
	public static void glDrawElementsInstancedBaseVertex(int mode, int count, int type, void* indices, int instancecount, int basevertex) => ((delegate* unmanaged[Cdecl]<int, int, int, void*, int, int, void>)s_glDrawElementsInstancedBaseVertex)(mode, count, type, indices, instancecount, basevertex);

	private static void* s_glDrawElementsInstancedBaseVertexBaseInstance;
	public static void glDrawElementsInstancedBaseVertexBaseInstance(int mode, int count, int type, void* indices, int instancecount, int basevertex, uint baseinstance) => ((delegate* unmanaged[Cdecl]<int, int, int, void*, int, int, uint, void>)s_glDrawElementsInstancedBaseVertexBaseInstance)(mode, count, type, indices, instancecount, basevertex, baseinstance);

	private static void* s_glDrawRangeElements;
	public static void glDrawRangeElements(int mode, uint start, uint end, int count, int type, void* indices) => ((delegate* unmanaged[Cdecl]<int, uint, uint, int, int, void*, void>)s_glDrawRangeElements)(mode, start, end, count, type, indices);

	private static void* s_glDrawRangeElementsBaseVertex;
	public static void glDrawRangeElementsBaseVertex(int mode, uint start, uint end, int count, int type, void* indices, int basevertex) => ((delegate* unmanaged[Cdecl]<int, uint, uint, int, int, void*, int, void>)s_glDrawRangeElementsBaseVertex)(mode, start, end, count, type, indices, basevertex);

	private static void* s_glDrawTransformFeedback;
	public static void glDrawTransformFeedback(int mode, uint id) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glDrawTransformFeedback)(mode, id);

	private static void* s_glDrawTransformFeedbackInstanced;
	public static void glDrawTransformFeedbackInstanced(int mode, uint id, int instancecount) => ((delegate* unmanaged[Cdecl]<int, uint, int, void>)s_glDrawTransformFeedbackInstanced)(mode, id, instancecount);

	private static void* s_glDrawTransformFeedbackStream;
	public static void glDrawTransformFeedbackStream(int mode, uint id, uint stream) => ((delegate* unmanaged[Cdecl]<int, uint, uint, void>)s_glDrawTransformFeedbackStream)(mode, id, stream);

	private static void* s_glDrawTransformFeedbackStreamInstanced;
	public static void glDrawTransformFeedbackStreamInstanced(int mode, uint id, uint stream, int instancecount) => ((delegate* unmanaged[Cdecl]<int, uint, uint, int, void>)s_glDrawTransformFeedbackStreamInstanced)(mode, id, stream, instancecount);

	private static void* s_glEnable;
	public static void glEnable(int cap) => ((delegate* unmanaged[Cdecl]<int, void>)s_glEnable)(cap);

	private static void* s_glEnableVertexArrayAttrib;
	public static void glEnableVertexArrayAttrib(uint vaobj, uint index) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glEnableVertexArrayAttrib)(vaobj, index);

	private static void* s_glEnableVertexAttribArray;
	public static void glEnableVertexAttribArray(uint index) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glEnableVertexAttribArray)(index);

	private static void* s_glEnablei;
	public static void glEnablei(int target, uint index) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glEnablei)(target, index);

	private static void* s_glEndConditionalRender;
	public static void glEndConditionalRender() => ((delegate* unmanaged[Cdecl]<void>)s_glEndConditionalRender)();

	private static void* s_glEndQuery;
	public static void glEndQuery(int target) => ((delegate* unmanaged[Cdecl]<int, void>)s_glEndQuery)(target);

	private static void* s_glEndQueryIndexed;
	public static void glEndQueryIndexed(int target, uint index) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glEndQueryIndexed)(target, index);

	private static void* s_glEndTransformFeedback;
	public static void glEndTransformFeedback() => ((delegate* unmanaged[Cdecl]<void>)s_glEndTransformFeedback)();

	private static void* s_glFenceSync;
	public static IntPtr glFenceSync(int condition, int flags) => ((delegate* unmanaged[Cdecl]<int, int, IntPtr>)s_glFenceSync)(condition, flags);

	private static void* s_glFinish;
	public static void glFinish() => ((delegate* unmanaged[Cdecl]<void>)s_glFinish)();

	private static void* s_glFlush;
	public static void glFlush() => ((delegate* unmanaged[Cdecl]<void>)s_glFlush)();

	private static void* s_glFlushMappedBufferRange;
	public static void glFlushMappedBufferRange(int target, IntPtr offset, IntPtr length) => ((delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, void>)s_glFlushMappedBufferRange)(target, offset, length);

	private static void* s_glFlushMappedNamedBufferRange;
	public static void glFlushMappedNamedBufferRange(uint buffer, IntPtr offset, IntPtr length) => ((delegate* unmanaged[Cdecl]<uint, IntPtr, IntPtr, void>)s_glFlushMappedNamedBufferRange)(buffer, offset, length);

	private static void* s_glFramebufferParameteri;
	public static void glFramebufferParameteri(int target, int pname, int param) => ((delegate* unmanaged[Cdecl]<int, int, int, void>)s_glFramebufferParameteri)(target, pname, param);

	private static void* s_glFramebufferRenderbuffer;
	public static void glFramebufferRenderbuffer(int target, int attachment, int renderbuffertarget, uint renderbuffer) => ((delegate* unmanaged[Cdecl]<int, int, int, uint, void>)s_glFramebufferRenderbuffer)(target, attachment, renderbuffertarget, renderbuffer);

	private static void* s_glFramebufferTexture;
	public static void glFramebufferTexture(int target, int attachment, uint texture, int level) => ((delegate* unmanaged[Cdecl]<int, int, uint, int, void>)s_glFramebufferTexture)(target, attachment, texture, level);

	private static void* s_glFramebufferTexture1D;
	public static void glFramebufferTexture1D(int target, int attachment, int textarget, uint texture, int level) => ((delegate* unmanaged[Cdecl]<int, int, int, uint, int, void>)s_glFramebufferTexture1D)(target, attachment, textarget, texture, level);

	private static void* s_glFramebufferTexture2D;
	public static void glFramebufferTexture2D(int target, int attachment, int textarget, uint texture, int level) => ((delegate* unmanaged[Cdecl]<int, int, int, uint, int, void>)s_glFramebufferTexture2D)(target, attachment, textarget, texture, level);

	private static void* s_glFramebufferTexture3D;
	public static void glFramebufferTexture3D(int target, int attachment, int textarget, uint texture, int level, int zoffset) => ((delegate* unmanaged[Cdecl]<int, int, int, uint, int, int, void>)s_glFramebufferTexture3D)(target, attachment, textarget, texture, level, zoffset);

	private static void* s_glFramebufferTextureLayer;
	public static void glFramebufferTextureLayer(int target, int attachment, uint texture, int level, int layer) => ((delegate* unmanaged[Cdecl]<int, int, uint, int, int, void>)s_glFramebufferTextureLayer)(target, attachment, texture, level, layer);

	private static void* s_glFrontFace;
	public static void glFrontFace(int mode) => ((delegate* unmanaged[Cdecl]<int, void>)s_glFrontFace)(mode);

	private static void* s_glGenBuffers;
	public static void glGenBuffers(int n, uint* buffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenBuffers)(n, buffers);

	private static void* s_glGenFramebuffers;
	public static void glGenFramebuffers(int n, uint* framebuffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenFramebuffers)(n, framebuffers);

	private static void* s_glGenProgramPipelines;
	public static void glGenProgramPipelines(int n, uint* pipelines) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenProgramPipelines)(n, pipelines);

	private static void* s_glGenQueries;
	public static void glGenQueries(int n, uint* ids) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenQueries)(n, ids);

	private static void* s_glGenRenderbuffers;
	public static void glGenRenderbuffers(int n, uint* renderbuffers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenRenderbuffers)(n, renderbuffers);

	private static void* s_glGenSamplers;
	public static void glGenSamplers(int count, uint* samplers) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenSamplers)(count, samplers);

	private static void* s_glGenTextures;
	public static void glGenTextures(int n, uint* textures) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenTextures)(n, textures);

	private static void* s_glGenTransformFeedbacks;
	public static void glGenTransformFeedbacks(int n, uint* ids) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenTransformFeedbacks)(n, ids);

	private static void* s_glGenVertexArrays;
	public static void glGenVertexArrays(int n, uint* arrays) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glGenVertexArrays)(n, arrays);

	private static void* s_glGenerateMipmap;
	public static void glGenerateMipmap(int target) => ((delegate* unmanaged[Cdecl]<int, void>)s_glGenerateMipmap)(target);

	private static void* s_glGenerateTextureMipmap;
	public static void glGenerateTextureMipmap(uint texture) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glGenerateTextureMipmap)(texture);

	private static void* s_glGetActiveAtomicCounterBufferiv;
	public static void glGetActiveAtomicCounterBufferiv(uint program, uint bufferIndex, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int*, void>)s_glGetActiveAtomicCounterBufferiv)(program, bufferIndex, pname, args);

	private static void* s_glGetActiveAttrib;
	public static void glGetActiveAttrib(uint program, uint index, int bufSize, int* length, int* size, int* type, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int*, int*, int*, byte*, void>)s_glGetActiveAttrib)(program, index, bufSize, length, size, type, name);

	private static void* s_glGetActiveSubroutineName;
	public static void glGetActiveSubroutineName(uint program, int shadertype, uint index, int bufSize, int* length, byte* name) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int*, byte*, void>)s_glGetActiveSubroutineName)(program, shadertype, index, bufSize, length, name);

	private static void* s_glGetActiveSubroutineUniformName;
	public static void glGetActiveSubroutineUniformName(uint program, int shadertype, uint index, int bufSize, int* length, byte* name) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int*, byte*, void>)s_glGetActiveSubroutineUniformName)(program, shadertype, index, bufSize, length, name);

	private static void* s_glGetActiveSubroutineUniformiv;
	public static void glGetActiveSubroutineUniformiv(uint program, int shadertype, uint index, int pname, int* values) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int*, void>)s_glGetActiveSubroutineUniformiv)(program, shadertype, index, pname, values);

	private static void* s_glGetActiveUniform;
	public static void glGetActiveUniform(uint program, uint index, int bufSize, int* length, int* size, int* type, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int*, int*, int*, byte*, void>)s_glGetActiveUniform)(program, index, bufSize, length, size, type, name);

	private static void* s_glGetActiveUniformBlockName;
	public static void glGetActiveUniformBlockName(uint program, uint uniformBlockIndex, int bufSize, int* length, byte* uniformBlockName) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int*, byte*, void>)s_glGetActiveUniformBlockName)(program, uniformBlockIndex, bufSize, length, uniformBlockName);

	private static void* s_glGetActiveUniformBlockiv;
	public static void glGetActiveUniformBlockiv(uint program, uint uniformBlockIndex, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int*, void>)s_glGetActiveUniformBlockiv)(program, uniformBlockIndex, pname, args);

	private static void* s_glGetActiveUniformName;
	public static void glGetActiveUniformName(uint program, uint uniformIndex, int bufSize, int* length, byte* uniformName) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int*, byte*, void>)s_glGetActiveUniformName)(program, uniformIndex, bufSize, length, uniformName);

	private static void* s_glGetActiveUniformsiv;
	public static void glGetActiveUniformsiv(uint program, int uniformCount, uint* uniformIndices, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, int, int*, void>)s_glGetActiveUniformsiv)(program, uniformCount, uniformIndices, pname, args);

	private static void* s_glGetAttachedShaders;
	public static void glGetAttachedShaders(uint program, int maxCount, int* count, uint* shaders) => ((delegate* unmanaged[Cdecl]<uint, int, int*, uint*, void>)s_glGetAttachedShaders)(program, maxCount, count, shaders);

	private static void* s_glGetAttribLocation;
	public static int glGetAttribLocation(uint program, byte* name) => ((delegate* unmanaged[Cdecl]<uint, byte*, int>)s_glGetAttribLocation)(program, name);

	private static void* s_glGetBooleani_v;
	public static void glGetBooleani_v(int target, uint index, bool* data) => ((delegate* unmanaged[Cdecl]<int, uint, bool*, void>)s_glGetBooleani_v)(target, index, data);

	private static void* s_glGetBooleanv;
	public static void glGetBooleanv(int pname, bool* data) => ((delegate* unmanaged[Cdecl]<int, bool*, void>)s_glGetBooleanv)(pname, data);

	private static void* s_glGetBufferParameteri64v;
	public static void glGetBufferParameteri64v(int target, int pname, long* args) => ((delegate* unmanaged[Cdecl]<int, int, long*, void>)s_glGetBufferParameteri64v)(target, pname, args);

	private static void* s_glGetBufferParameteriv;
	public static void glGetBufferParameteriv(int target, int pname, int* args) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glGetBufferParameteriv)(target, pname, args);

	private static void* s_glGetBufferPointerv;
	public static void glGetBufferPointerv(int target, int pname, void** args) => ((delegate* unmanaged[Cdecl]<int, int, void**, void>)s_glGetBufferPointerv)(target, pname, args);

	private static void* s_glGetBufferSubData;
	public static void glGetBufferSubData(int target, IntPtr offset, IntPtr size, void* data) => ((delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, void*, void>)s_glGetBufferSubData)(target, offset, size, data);

	private static void* s_glGetCompressedTexImage;
	public static void glGetCompressedTexImage(int target, int level, void* img) => ((delegate* unmanaged[Cdecl]<int, int, void*, void>)s_glGetCompressedTexImage)(target, level, img);

	private static void* s_glGetCompressedTextureImage;
	public static void glGetCompressedTextureImage(uint texture, int level, int bufSize, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, void*, void>)s_glGetCompressedTextureImage)(texture, level, bufSize, pixels);

	private static void* s_glGetCompressedTextureSubImage;
	public static void glGetCompressedTextureSubImage(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int bufSize, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, int, void*, void>)s_glGetCompressedTextureSubImage)(texture, level, xoffset, yoffset, zoffset, width, height, depth, bufSize, pixels);

	private static void* s_glGetDebugMessageLog;
	public static uint glGetDebugMessageLog(uint count, int bufSize, int* sources, int* types, uint* ids, int* severities, int* lengths, byte* messageLog) => ((delegate* unmanaged[Cdecl]<uint, int, int*, int*, uint*, int*, int*, byte*, uint>)s_glGetDebugMessageLog)(count, bufSize, sources, types, ids, severities, lengths, messageLog);

	private static void* s_glGetDoublei_v;
	public static void glGetDoublei_v(int target, uint index, double* data) => ((delegate* unmanaged[Cdecl]<int, uint, double*, void>)s_glGetDoublei_v)(target, index, data);

	private static void* s_glGetDoublev;
	public static void glGetDoublev(int pname, double* data) => ((delegate* unmanaged[Cdecl]<int, double*, void>)s_glGetDoublev)(pname, data);

	private static void* s_glGetError;
	public static int glGetError() => ((delegate* unmanaged[Cdecl]<int>)s_glGetError)();

	private static void* s_glGetFloati_v;
	public static void glGetFloati_v(int target, uint index, float* data) => ((delegate* unmanaged[Cdecl]<int, uint, float*, void>)s_glGetFloati_v)(target, index, data);

	private static void* s_glGetFloatv;
	public static void glGetFloatv(int pname, float* data) => ((delegate* unmanaged[Cdecl]<int, float*, void>)s_glGetFloatv)(pname, data);

	private static void* s_glGetFragDataIndex;
	public static int glGetFragDataIndex(uint program, byte* name) => ((delegate* unmanaged[Cdecl]<uint, byte*, int>)s_glGetFragDataIndex)(program, name);

	private static void* s_glGetFragDataLocation;
	public static int glGetFragDataLocation(uint program, byte* name) => ((delegate* unmanaged[Cdecl]<uint, byte*, int>)s_glGetFragDataLocation)(program, name);

	private static void* s_glGetFramebufferAttachmentParameteriv;
	public static void glGetFramebufferAttachmentParameteriv(int target, int attachment, int pname, int* args) => ((delegate* unmanaged[Cdecl]<int, int, int, int*, void>)s_glGetFramebufferAttachmentParameteriv)(target, attachment, pname, args);

	private static void* s_glGetFramebufferParameteriv;
	public static void glGetFramebufferParameteriv(int target, int pname, int* args) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glGetFramebufferParameteriv)(target, pname, args);

	private static void* s_glGetGraphicsResetStatus;
	public static int glGetGraphicsResetStatus() => ((delegate* unmanaged[Cdecl]<int>)s_glGetGraphicsResetStatus)();

	private static void* s_glGetInteger64i_v;
	public static void glGetInteger64i_v(int target, uint index, long* data) => ((delegate* unmanaged[Cdecl]<int, uint, long*, void>)s_glGetInteger64i_v)(target, index, data);

	private static void* s_glGetInteger64v;
	public static void glGetInteger64v(int pname, long* data) => ((delegate* unmanaged[Cdecl]<int, long*, void>)s_glGetInteger64v)(pname, data);

	private static void* s_glGetIntegeri_v;
	public static void glGetIntegeri_v(int target, uint index, int* data) => ((delegate* unmanaged[Cdecl]<int, uint, int*, void>)s_glGetIntegeri_v)(target, index, data);

	private static void* s_glGetIntegerv;
	public static void glGetIntegerv(int pname, int* data) => ((delegate* unmanaged[Cdecl]<int, int*, void>)s_glGetIntegerv)(pname, data);

	private static void* s_glGetInternalformati64v;
	public static void glGetInternalformati64v(int target, int internalformat, int pname, int count, long* args) => ((delegate* unmanaged[Cdecl]<int, int, int, int, long*, void>)s_glGetInternalformati64v)(target, internalformat, pname, count, args);

	private static void* s_glGetInternalformativ;
	public static void glGetInternalformativ(int target, int internalformat, int pname, int count, int* args) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int*, void>)s_glGetInternalformativ)(target, internalformat, pname, count, args);

	private static void* s_glGetMultisamplefv;
	public static void glGetMultisamplefv(int pname, uint index, float* val) => ((delegate* unmanaged[Cdecl]<int, uint, float*, void>)s_glGetMultisamplefv)(pname, index, val);

	private static void* s_glGetNamedBufferParameteri64v;
	public static void glGetNamedBufferParameteri64v(uint buffer, int pname, long* args) => ((delegate* unmanaged[Cdecl]<uint, int, long*, void>)s_glGetNamedBufferParameteri64v)(buffer, pname, args);

	private static void* s_glGetNamedBufferParameteriv;
	public static void glGetNamedBufferParameteriv(uint buffer, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glGetNamedBufferParameteriv)(buffer, pname, args);

	private static void* s_glGetNamedBufferPointerv;
	public static void glGetNamedBufferPointerv(uint buffer, int pname, void** args) => ((delegate* unmanaged[Cdecl]<uint, int, void**, void>)s_glGetNamedBufferPointerv)(buffer, pname, args);

	private static void* s_glGetNamedBufferSubData;
	public static void glGetNamedBufferSubData(uint buffer, IntPtr offset, IntPtr size, void* data) => ((delegate* unmanaged[Cdecl]<uint, IntPtr, IntPtr, void*, void>)s_glGetNamedBufferSubData)(buffer, offset, size, data);

	private static void* s_glGetNamedFramebufferAttachmentParameteriv;
	public static void glGetNamedFramebufferAttachmentParameteriv(uint framebuffer, int attachment, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int, int*, void>)s_glGetNamedFramebufferAttachmentParameteriv)(framebuffer, attachment, pname, args);

	private static void* s_glGetNamedFramebufferParameteriv;
	public static void glGetNamedFramebufferParameteriv(uint framebuffer, int pname, int* param) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glGetNamedFramebufferParameteriv)(framebuffer, pname, param);

	private static void* s_glGetNamedRenderbufferParameteriv;
	public static void glGetNamedRenderbufferParameteriv(uint renderbuffer, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glGetNamedRenderbufferParameteriv)(renderbuffer, pname, args);

	private static void* s_glGetObjectLabel;
	public static void glGetObjectLabel(int identifier, uint name, int bufSize, int* length, byte* label) => ((delegate* unmanaged[Cdecl]<int, uint, int, int*, byte*, void>)s_glGetObjectLabel)(identifier, name, bufSize, length, label);

	private static void* s_glGetObjectPtrLabel;
	public static void glGetObjectPtrLabel(void* ptr, int bufSize, int* length, byte* label) => ((delegate* unmanaged[Cdecl]<void*, int, int*, byte*, void>)s_glGetObjectPtrLabel)(ptr, bufSize, length, label);

	private static void* s_glGetProgramBinary;
	public static void glGetProgramBinary(uint program, int bufSize, int* length, int* binaryFormat, void* binary) => ((delegate* unmanaged[Cdecl]<uint, int, int*, int*, void*, void>)s_glGetProgramBinary)(program, bufSize, length, binaryFormat, binary);

	private static void* s_glGetProgramInfoLog;
	public static void glGetProgramInfoLog(uint program, int bufSize, int* length, byte* infoLog) => ((delegate* unmanaged[Cdecl]<uint, int, int*, byte*, void>)s_glGetProgramInfoLog)(program, bufSize, length, infoLog);

	private static void* s_glGetProgramInterfaceiv;
	public static void glGetProgramInterfaceiv(uint program, int programInterface, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int, int*, void>)s_glGetProgramInterfaceiv)(program, programInterface, pname, args);

	private static void* s_glGetProgramPipelineInfoLog;
	public static void glGetProgramPipelineInfoLog(uint pipeline, int bufSize, int* length, byte* infoLog) => ((delegate* unmanaged[Cdecl]<uint, int, int*, byte*, void>)s_glGetProgramPipelineInfoLog)(pipeline, bufSize, length, infoLog);

	private static void* s_glGetProgramPipelineiv;
	public static void glGetProgramPipelineiv(uint pipeline, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glGetProgramPipelineiv)(pipeline, pname, args);

	private static void* s_glGetProgramResourceIndex;
	public static uint glGetProgramResourceIndex(uint program, int programInterface, byte* name) => ((delegate* unmanaged[Cdecl]<uint, int, byte*, uint>)s_glGetProgramResourceIndex)(program, programInterface, name);

	private static void* s_glGetProgramResourceLocation;
	public static int glGetProgramResourceLocation(uint program, int programInterface, byte* name) => ((delegate* unmanaged[Cdecl]<uint, int, byte*, int>)s_glGetProgramResourceLocation)(program, programInterface, name);

	private static void* s_glGetProgramResourceLocationIndex;
	public static int glGetProgramResourceLocationIndex(uint program, int programInterface, byte* name) => ((delegate* unmanaged[Cdecl]<uint, int, byte*, int>)s_glGetProgramResourceLocationIndex)(program, programInterface, name);

	private static void* s_glGetProgramResourceName;
	public static void glGetProgramResourceName(uint program, int programInterface, uint index, int bufSize, int* length, byte* name) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int*, byte*, void>)s_glGetProgramResourceName)(program, programInterface, index, bufSize, length, name);

	private static void* s_glGetProgramResourceiv;
	public static void glGetProgramResourceiv(uint program, int programInterface, uint index, int propCount, int* props, int count, int* length, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int*, int, int*, int*, void>)s_glGetProgramResourceiv)(program, programInterface, index, propCount, props, count, length, args);

	private static void* s_glGetProgramStageiv;
	public static void glGetProgramStageiv(uint program, int shadertype, int pname, int* values) => ((delegate* unmanaged[Cdecl]<uint, int, int, int*, void>)s_glGetProgramStageiv)(program, shadertype, pname, values);

	private static void* s_glGetProgramiv;
	public static void glGetProgramiv(uint program, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glGetProgramiv)(program, pname, args);

	private static void* s_glGetQueryBufferObjecti64v;
	public static void glGetQueryBufferObjecti64v(uint id, uint buffer, int pname, IntPtr offset) => ((delegate* unmanaged[Cdecl]<uint, uint, int, IntPtr, void>)s_glGetQueryBufferObjecti64v)(id, buffer, pname, offset);

	private static void* s_glGetQueryBufferObjectiv;
	public static void glGetQueryBufferObjectiv(uint id, uint buffer, int pname, IntPtr offset) => ((delegate* unmanaged[Cdecl]<uint, uint, int, IntPtr, void>)s_glGetQueryBufferObjectiv)(id, buffer, pname, offset);

	private static void* s_glGetQueryBufferObjectui64v;
	public static void glGetQueryBufferObjectui64v(uint id, uint buffer, int pname, IntPtr offset) => ((delegate* unmanaged[Cdecl]<uint, uint, int, IntPtr, void>)s_glGetQueryBufferObjectui64v)(id, buffer, pname, offset);

	private static void* s_glGetQueryBufferObjectuiv;
	public static void glGetQueryBufferObjectuiv(uint id, uint buffer, int pname, IntPtr offset) => ((delegate* unmanaged[Cdecl]<uint, uint, int, IntPtr, void>)s_glGetQueryBufferObjectuiv)(id, buffer, pname, offset);

	private static void* s_glGetQueryIndexediv;
	public static void glGetQueryIndexediv(int target, uint index, int pname, int* args) => ((delegate* unmanaged[Cdecl]<int, uint, int, int*, void>)s_glGetQueryIndexediv)(target, index, pname, args);

	private static void* s_glGetQueryObjecti64v;
	public static void glGetQueryObjecti64v(uint id, int pname, long* args) => ((delegate* unmanaged[Cdecl]<uint, int, long*, void>)s_glGetQueryObjecti64v)(id, pname, args);

	private static void* s_glGetQueryObjectiv;
	public static void glGetQueryObjectiv(uint id, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glGetQueryObjectiv)(id, pname, args);

	private static void* s_glGetQueryObjectui64v;
	public static void glGetQueryObjectui64v(uint id, int pname, ulong* args) => ((delegate* unmanaged[Cdecl]<uint, int, ulong*, void>)s_glGetQueryObjectui64v)(id, pname, args);

	private static void* s_glGetQueryObjectuiv;
	public static void glGetQueryObjectuiv(uint id, int pname, uint* args) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glGetQueryObjectuiv)(id, pname, args);

	private static void* s_glGetQueryiv;
	public static void glGetQueryiv(int target, int pname, int* args) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glGetQueryiv)(target, pname, args);

	private static void* s_glGetRenderbufferParameteriv;
	public static void glGetRenderbufferParameteriv(int target, int pname, int* args) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glGetRenderbufferParameteriv)(target, pname, args);

	private static void* s_glGetSamplerParameterIiv;
	public static void glGetSamplerParameterIiv(uint sampler, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glGetSamplerParameterIiv)(sampler, pname, args);

	private static void* s_glGetSamplerParameterIuiv;
	public static void glGetSamplerParameterIuiv(uint sampler, int pname, uint* args) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glGetSamplerParameterIuiv)(sampler, pname, args);

	private static void* s_glGetSamplerParameterfv;
	public static void glGetSamplerParameterfv(uint sampler, int pname, float* args) => ((delegate* unmanaged[Cdecl]<uint, int, float*, void>)s_glGetSamplerParameterfv)(sampler, pname, args);

	private static void* s_glGetSamplerParameteriv;
	public static void glGetSamplerParameteriv(uint sampler, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glGetSamplerParameteriv)(sampler, pname, args);

	private static void* s_glGetShaderInfoLog;
	public static void glGetShaderInfoLog(uint shader, int bufSize, int* length, byte* infoLog) => ((delegate* unmanaged[Cdecl]<uint, int, int*, byte*, void>)s_glGetShaderInfoLog)(shader, bufSize, length, infoLog);

	private static void* s_glGetShaderPrecisionFormat;
	public static void glGetShaderPrecisionFormat(int shadertype, int precisiontype, int* range, int* precision) => ((delegate* unmanaged[Cdecl]<int, int, int*, int*, void>)s_glGetShaderPrecisionFormat)(shadertype, precisiontype, range, precision);

	private static void* s_glGetShaderSource;
	public static void glGetShaderSource(uint shader, int bufSize, int* length, byte* source) => ((delegate* unmanaged[Cdecl]<uint, int, int*, byte*, void>)s_glGetShaderSource)(shader, bufSize, length, source);

	private static void* s_glGetShaderiv;
	public static void glGetShaderiv(uint shader, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glGetShaderiv)(shader, pname, args);

	private static void* s_glGetString;
	public static  byte *glGetString(int name) => ((delegate* unmanaged[Cdecl]<int, byte*>)s_glGetString)(name);

	private static void* s_glGetStringi;
	public static  byte *glGetStringi(int name, uint index) => ((delegate* unmanaged[Cdecl]<int, uint, byte*>)s_glGetStringi)(name, index);

	private static void* s_glGetSubroutineIndex;
	public static uint glGetSubroutineIndex(uint program, int shadertype, byte* name) => ((delegate* unmanaged[Cdecl]<uint, int, byte*, uint>)s_glGetSubroutineIndex)(program, shadertype, name);

	private static void* s_glGetSubroutineUniformLocation;
	public static int glGetSubroutineUniformLocation(uint program, int shadertype, byte* name) => ((delegate* unmanaged[Cdecl]<uint, int, byte*, int>)s_glGetSubroutineUniformLocation)(program, shadertype, name);

	private static void* s_glGetSynciv;
	public static void glGetSynciv(IntPtr sync, int pname, int count, int* length, int* values) => ((delegate* unmanaged[Cdecl]<IntPtr, int, int, int*, int*, void>)s_glGetSynciv)(sync, pname, count, length, values);

	private static void* s_glGetTexImage;
	public static void glGetTexImage(int target, int level, int format, int type, void* pixels) => ((delegate* unmanaged[Cdecl]<int, int, int, int, void*, void>)s_glGetTexImage)(target, level, format, type, pixels);

	private static void* s_glGetTexLevelParameterfv;
	public static void glGetTexLevelParameterfv(int target, int level, int pname, float* args) => ((delegate* unmanaged[Cdecl]<int, int, int, float*, void>)s_glGetTexLevelParameterfv)(target, level, pname, args);

	private static void* s_glGetTexLevelParameteriv;
	public static void glGetTexLevelParameteriv(int target, int level, int pname, int* args) => ((delegate* unmanaged[Cdecl]<int, int, int, int*, void>)s_glGetTexLevelParameteriv)(target, level, pname, args);

	private static void* s_glGetTexParameterIiv;
	public static void glGetTexParameterIiv(int target, int pname, int* args) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glGetTexParameterIiv)(target, pname, args);

	private static void* s_glGetTexParameterIuiv;
	public static void glGetTexParameterIuiv(int target, int pname, uint* args) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glGetTexParameterIuiv)(target, pname, args);

	private static void* s_glGetTexParameterfv;
	public static void glGetTexParameterfv(int target, int pname, float* args) => ((delegate* unmanaged[Cdecl]<int, int, float*, void>)s_glGetTexParameterfv)(target, pname, args);

	private static void* s_glGetTexParameteriv;
	public static void glGetTexParameteriv(int target, int pname, int* args) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glGetTexParameteriv)(target, pname, args);

	private static void* s_glGetTextureImage;
	public static void glGetTextureImage(uint texture, int level, int format, int type, int bufSize, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, void*, void>)s_glGetTextureImage)(texture, level, format, type, bufSize, pixels);

	private static void* s_glGetTextureLevelParameterfv;
	public static void glGetTextureLevelParameterfv(uint texture, int level, int pname, float* args) => ((delegate* unmanaged[Cdecl]<uint, int, int, float*, void>)s_glGetTextureLevelParameterfv)(texture, level, pname, args);

	private static void* s_glGetTextureLevelParameteriv;
	public static void glGetTextureLevelParameteriv(uint texture, int level, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int, int*, void>)s_glGetTextureLevelParameteriv)(texture, level, pname, args);

	private static void* s_glGetTextureParameterIiv;
	public static void glGetTextureParameterIiv(uint texture, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glGetTextureParameterIiv)(texture, pname, args);

	private static void* s_glGetTextureParameterIuiv;
	public static void glGetTextureParameterIuiv(uint texture, int pname, uint* args) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glGetTextureParameterIuiv)(texture, pname, args);

	private static void* s_glGetTextureParameterfv;
	public static void glGetTextureParameterfv(uint texture, int pname, float* args) => ((delegate* unmanaged[Cdecl]<uint, int, float*, void>)s_glGetTextureParameterfv)(texture, pname, args);

	private static void* s_glGetTextureParameteriv;
	public static void glGetTextureParameteriv(uint texture, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glGetTextureParameteriv)(texture, pname, args);

	private static void* s_glGetTextureSubImage;
	public static void glGetTextureSubImage(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, int bufSize, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, int, int, int, void*, void>)s_glGetTextureSubImage)(texture, level, xoffset, yoffset, zoffset, width, height, depth, format, type, bufSize, pixels);

	private static void* s_glGetTransformFeedbackVarying;
	public static void glGetTransformFeedbackVarying(uint program, uint index, int bufSize, int* length, int* size, int* type, byte* name) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int*, int*, int*, byte*, void>)s_glGetTransformFeedbackVarying)(program, index, bufSize, length, size, type, name);

	private static void* s_glGetTransformFeedbacki64_v;
	public static void glGetTransformFeedbacki64_v(uint xfb, int pname, uint index, long* param) => ((delegate* unmanaged[Cdecl]<uint, int, uint, long*, void>)s_glGetTransformFeedbacki64_v)(xfb, pname, index, param);

	private static void* s_glGetTransformFeedbacki_v;
	public static void glGetTransformFeedbacki_v(uint xfb, int pname, uint index, int* param) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int*, void>)s_glGetTransformFeedbacki_v)(xfb, pname, index, param);

	private static void* s_glGetTransformFeedbackiv;
	public static void glGetTransformFeedbackiv(uint xfb, int pname, int* param) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glGetTransformFeedbackiv)(xfb, pname, param);

	private static void* s_glGetUniformBlockIndex;
	public static uint glGetUniformBlockIndex(uint program, byte* uniformBlockName) => ((delegate* unmanaged[Cdecl]<uint, byte*, uint>)s_glGetUniformBlockIndex)(program, uniformBlockName);

	private static void* s_glGetUniformIndices;
	public static void glGetUniformIndices(uint program, int uniformCount, byte* uniformNames, uint* uniformIndices) => ((delegate* unmanaged[Cdecl]<uint, int, byte*, uint*, void>)s_glGetUniformIndices)(program, uniformCount, uniformNames, uniformIndices);

	private static void* s_glGetUniformLocation;
	public static int glGetUniformLocation(uint program, byte* name) => ((delegate* unmanaged[Cdecl]<uint, byte*, int>)s_glGetUniformLocation)(program, name);

	private static void* s_glGetUniformSubroutineuiv;
	public static void glGetUniformSubroutineuiv(int shadertype, int location, uint* args) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glGetUniformSubroutineuiv)(shadertype, location, args);

	private static void* s_glGetUniformdv;
	public static void glGetUniformdv(uint program, int location, double* args) => ((delegate* unmanaged[Cdecl]<uint, int, double*, void>)s_glGetUniformdv)(program, location, args);

	private static void* s_glGetUniformfv;
	public static void glGetUniformfv(uint program, int location, float* args) => ((delegate* unmanaged[Cdecl]<uint, int, float*, void>)s_glGetUniformfv)(program, location, args);

	private static void* s_glGetUniformiv;
	public static void glGetUniformiv(uint program, int location, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glGetUniformiv)(program, location, args);

	private static void* s_glGetUniformuiv;
	public static void glGetUniformuiv(uint program, int location, uint* args) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glGetUniformuiv)(program, location, args);

	private static void* s_glGetVertexArrayIndexed64iv;
	public static void glGetVertexArrayIndexed64iv(uint vaobj, uint index, int pname, long* param) => ((delegate* unmanaged[Cdecl]<uint, uint, int, long*, void>)s_glGetVertexArrayIndexed64iv)(vaobj, index, pname, param);

	private static void* s_glGetVertexArrayIndexediv;
	public static void glGetVertexArrayIndexediv(uint vaobj, uint index, int pname, int* param) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int*, void>)s_glGetVertexArrayIndexediv)(vaobj, index, pname, param);

	private static void* s_glGetVertexArrayiv;
	public static void glGetVertexArrayiv(uint vaobj, int pname, int* param) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glGetVertexArrayiv)(vaobj, pname, param);

	private static void* s_glGetVertexAttribIiv;
	public static void glGetVertexAttribIiv(uint index, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glGetVertexAttribIiv)(index, pname, args);

	private static void* s_glGetVertexAttribIuiv;
	public static void glGetVertexAttribIuiv(uint index, int pname, uint* args) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glGetVertexAttribIuiv)(index, pname, args);

	private static void* s_glGetVertexAttribLdv;
	public static void glGetVertexAttribLdv(uint index, int pname, double* args) => ((delegate* unmanaged[Cdecl]<uint, int, double*, void>)s_glGetVertexAttribLdv)(index, pname, args);

	private static void* s_glGetVertexAttribPointerv;
	public static void glGetVertexAttribPointerv(uint index, int pname, void** pointer) => ((delegate* unmanaged[Cdecl]<uint, int, void**, void>)s_glGetVertexAttribPointerv)(index, pname, pointer);

	private static void* s_glGetVertexAttribdv;
	public static void glGetVertexAttribdv(uint index, int pname, double* args) => ((delegate* unmanaged[Cdecl]<uint, int, double*, void>)s_glGetVertexAttribdv)(index, pname, args);

	private static void* s_glGetVertexAttribfv;
	public static void glGetVertexAttribfv(uint index, int pname, float* args) => ((delegate* unmanaged[Cdecl]<uint, int, float*, void>)s_glGetVertexAttribfv)(index, pname, args);

	private static void* s_glGetVertexAttribiv;
	public static void glGetVertexAttribiv(uint index, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glGetVertexAttribiv)(index, pname, args);

	private static void* s_glGetnColorTable;
	public static void glGetnColorTable(int target, int format, int type, int bufSize, void* table) => ((delegate* unmanaged[Cdecl]<int, int, int, int, void*, void>)s_glGetnColorTable)(target, format, type, bufSize, table);

	private static void* s_glGetnCompressedTexImage;
	public static void glGetnCompressedTexImage(int target, int lod, int bufSize, void* pixels) => ((delegate* unmanaged[Cdecl]<int, int, int, void*, void>)s_glGetnCompressedTexImage)(target, lod, bufSize, pixels);

	private static void* s_glGetnConvolutionFilter;
	public static void glGetnConvolutionFilter(int target, int format, int type, int bufSize, void* image) => ((delegate* unmanaged[Cdecl]<int, int, int, int, void*, void>)s_glGetnConvolutionFilter)(target, format, type, bufSize, image);

	private static void* s_glGetnHistogram;
	public static void glGetnHistogram(int target, bool reset, int format, int type, int bufSize, void* values) => ((delegate* unmanaged[Cdecl]<int, bool, int, int, int, void*, void>)s_glGetnHistogram)(target, reset, format, type, bufSize, values);

	private static void* s_glGetnMapdv;
	public static void glGetnMapdv(int target, int query, int bufSize, double* v) => ((delegate* unmanaged[Cdecl]<int, int, int, double*, void>)s_glGetnMapdv)(target, query, bufSize, v);

	private static void* s_glGetnMapfv;
	public static void glGetnMapfv(int target, int query, int bufSize, float* v) => ((delegate* unmanaged[Cdecl]<int, int, int, float*, void>)s_glGetnMapfv)(target, query, bufSize, v);

	private static void* s_glGetnMapiv;
	public static void glGetnMapiv(int target, int query, int bufSize, int* v) => ((delegate* unmanaged[Cdecl]<int, int, int, int*, void>)s_glGetnMapiv)(target, query, bufSize, v);

	private static void* s_glGetnMinmax;
	public static void glGetnMinmax(int target, bool reset, int format, int type, int bufSize, void* values) => ((delegate* unmanaged[Cdecl]<int, bool, int, int, int, void*, void>)s_glGetnMinmax)(target, reset, format, type, bufSize, values);

	private static void* s_glGetnPixelMapfv;
	public static void glGetnPixelMapfv(int map, int bufSize, float* values) => ((delegate* unmanaged[Cdecl]<int, int, float*, void>)s_glGetnPixelMapfv)(map, bufSize, values);

	private static void* s_glGetnPixelMapuiv;
	public static void glGetnPixelMapuiv(int map, int bufSize, uint* values) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glGetnPixelMapuiv)(map, bufSize, values);

	private static void* s_glGetnPixelMapusv;
	public static void glGetnPixelMapusv(int map, int bufSize, ushort* values) => ((delegate* unmanaged[Cdecl]<int, int, ushort*, void>)s_glGetnPixelMapusv)(map, bufSize, values);

	private static void* s_glGetnPolygonStipple;
	public static void glGetnPolygonStipple(int bufSize, byte* pattern) => ((delegate* unmanaged[Cdecl]<int, byte*, void>)s_glGetnPolygonStipple)(bufSize, pattern);

	private static void* s_glGetnSeparableFilter;
	public static void glGetnSeparableFilter(int target, int format, int type, int rowBufSize, void* row, int columnBufSize, void* column, void* span) => ((delegate* unmanaged[Cdecl]<int, int, int, int, void*, int, void*, void*, void>)s_glGetnSeparableFilter)(target, format, type, rowBufSize, row, columnBufSize, column, span);

	private static void* s_glGetnTexImage;
	public static void glGetnTexImage(int target, int level, int format, int type, int bufSize, void* pixels) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, void*, void>)s_glGetnTexImage)(target, level, format, type, bufSize, pixels);

	private static void* s_glGetnUniformdv;
	public static void glGetnUniformdv(uint program, int location, int bufSize, double* args) => ((delegate* unmanaged[Cdecl]<uint, int, int, double*, void>)s_glGetnUniformdv)(program, location, bufSize, args);

	private static void* s_glGetnUniformfv;
	public static void glGetnUniformfv(uint program, int location, int bufSize, float* args) => ((delegate* unmanaged[Cdecl]<uint, int, int, float*, void>)s_glGetnUniformfv)(program, location, bufSize, args);

	private static void* s_glGetnUniformiv;
	public static void glGetnUniformiv(uint program, int location, int bufSize, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int, int*, void>)s_glGetnUniformiv)(program, location, bufSize, args);

	private static void* s_glGetnUniformuiv;
	public static void glGetnUniformuiv(uint program, int location, int bufSize, uint* args) => ((delegate* unmanaged[Cdecl]<uint, int, int, uint*, void>)s_glGetnUniformuiv)(program, location, bufSize, args);

	private static void* s_glHint;
	public static void glHint(int target, int mode) => ((delegate* unmanaged[Cdecl]<int, int, void>)s_glHint)(target, mode);

	private static void* s_glInvalidateBufferData;
	public static void glInvalidateBufferData(uint buffer) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glInvalidateBufferData)(buffer);

	private static void* s_glInvalidateBufferSubData;
	public static void glInvalidateBufferSubData(uint buffer, IntPtr offset, IntPtr length) => ((delegate* unmanaged[Cdecl]<uint, IntPtr, IntPtr, void>)s_glInvalidateBufferSubData)(buffer, offset, length);

	private static void* s_glInvalidateFramebuffer;
	public static void glInvalidateFramebuffer(int target, int numAttachments, int* attachments) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glInvalidateFramebuffer)(target, numAttachments, attachments);

	private static void* s_glInvalidateNamedFramebufferData;
	public static void glInvalidateNamedFramebufferData(uint framebuffer, int numAttachments, int* attachments) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glInvalidateNamedFramebufferData)(framebuffer, numAttachments, attachments);

	private static void* s_glInvalidateNamedFramebufferSubData;
	public static void glInvalidateNamedFramebufferSubData(uint framebuffer, int numAttachments, int* attachments, int x, int y, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, int*, int, int, int, int, void>)s_glInvalidateNamedFramebufferSubData)(framebuffer, numAttachments, attachments, x, y, width, height);

	private static void* s_glInvalidateSubFramebuffer;
	public static void glInvalidateSubFramebuffer(int target, int numAttachments, int* attachments, int x, int y, int width, int height) => ((delegate* unmanaged[Cdecl]<int, int, int*, int, int, int, int, void>)s_glInvalidateSubFramebuffer)(target, numAttachments, attachments, x, y, width, height);

	private static void* s_glInvalidateTexImage;
	public static void glInvalidateTexImage(uint texture, int level) => ((delegate* unmanaged[Cdecl]<uint, int, void>)s_glInvalidateTexImage)(texture, level);

	private static void* s_glInvalidateTexSubImage;
	public static void glInvalidateTexSubImage(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, void>)s_glInvalidateTexSubImage)(texture, level, xoffset, yoffset, zoffset, width, height, depth);

	private static void* s_glIsBuffer;
	public static bool glIsBuffer(uint buffer) => ((delegate* unmanaged[Cdecl]<uint, bool>)s_glIsBuffer)(buffer);

	private static void* s_glIsEnabled;
	public static bool glIsEnabled(int cap) => ((delegate* unmanaged[Cdecl]<int, bool>)s_glIsEnabled)(cap);

	private static void* s_glIsEnabledi;
	public static bool glIsEnabledi(int target, uint index) => ((delegate* unmanaged[Cdecl]<int, uint, bool>)s_glIsEnabledi)(target, index);

	private static void* s_glIsFramebuffer;
	public static bool glIsFramebuffer(uint framebuffer) => ((delegate* unmanaged[Cdecl]<uint, bool>)s_glIsFramebuffer)(framebuffer);

	private static void* s_glIsProgram;
	public static bool glIsProgram(uint program) => ((delegate* unmanaged[Cdecl]<uint, bool>)s_glIsProgram)(program);

	private static void* s_glIsProgramPipeline;
	public static bool glIsProgramPipeline(uint pipeline) => ((delegate* unmanaged[Cdecl]<uint, bool>)s_glIsProgramPipeline)(pipeline);

	private static void* s_glIsQuery;
	public static bool glIsQuery(uint id) => ((delegate* unmanaged[Cdecl]<uint, bool>)s_glIsQuery)(id);

	private static void* s_glIsRenderbuffer;
	public static bool glIsRenderbuffer(uint renderbuffer) => ((delegate* unmanaged[Cdecl]<uint, bool>)s_glIsRenderbuffer)(renderbuffer);

	private static void* s_glIsSampler;
	public static bool glIsSampler(uint sampler) => ((delegate* unmanaged[Cdecl]<uint, bool>)s_glIsSampler)(sampler);

	private static void* s_glIsShader;
	public static bool glIsShader(uint shader) => ((delegate* unmanaged[Cdecl]<uint, bool>)s_glIsShader)(shader);

	private static void* s_glIsSync;
	public static bool glIsSync(IntPtr sync) => ((delegate* unmanaged[Cdecl]<IntPtr, bool>)s_glIsSync)(sync);

	private static void* s_glIsTexture;
	public static bool glIsTexture(uint texture) => ((delegate* unmanaged[Cdecl]<uint, bool>)s_glIsTexture)(texture);

	private static void* s_glIsTransformFeedback;
	public static bool glIsTransformFeedback(uint id) => ((delegate* unmanaged[Cdecl]<uint, bool>)s_glIsTransformFeedback)(id);

	private static void* s_glIsVertexArray;
	public static bool glIsVertexArray(uint array) => ((delegate* unmanaged[Cdecl]<uint, bool>)s_glIsVertexArray)(array);

	private static void* s_glLineWidth;
	public static void glLineWidth(float width) => ((delegate* unmanaged[Cdecl]<float, void>)s_glLineWidth)(width);

	private static void* s_glLinkProgram;
	public static void glLinkProgram(uint program) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glLinkProgram)(program);

	private static void* s_glLogicOp;
	public static void glLogicOp(int opcode) => ((delegate* unmanaged[Cdecl]<int, void>)s_glLogicOp)(opcode);

	private static void* s_glMapBuffer;
	public static void *glMapBuffer(int target, int access) => ((delegate* unmanaged[Cdecl]<int, int, void *>)s_glMapBuffer)(target, access);

	private static void* s_glMapBufferRange;
	public static void *glMapBufferRange(int target, IntPtr offset, IntPtr length, int access) => ((delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, int, void *>)s_glMapBufferRange)(target, offset, length, access);

	private static void* s_glMapNamedBuffer;
	public static void *glMapNamedBuffer(uint buffer, int access) => ((delegate* unmanaged[Cdecl]<uint, int, void *>)s_glMapNamedBuffer)(buffer, access);

	private static void* s_glMapNamedBufferRange;
	public static void *glMapNamedBufferRange(uint buffer, IntPtr offset, IntPtr length, int access) => ((delegate* unmanaged[Cdecl]<uint, IntPtr, IntPtr, int, void *>)s_glMapNamedBufferRange)(buffer, offset, length, access);

	private static void* s_glMemoryBarrier;
	public static void glMemoryBarrier(int barriers) => ((delegate* unmanaged[Cdecl]<int, void>)s_glMemoryBarrier)(barriers);

	private static void* s_glMemoryBarrierByRegion;
	public static void glMemoryBarrierByRegion(int barriers) => ((delegate* unmanaged[Cdecl]<int, void>)s_glMemoryBarrierByRegion)(barriers);

	private static void* s_glMinSampleShading;
	public static void glMinSampleShading(float value) => ((delegate* unmanaged[Cdecl]<float, void>)s_glMinSampleShading)(value);

	private static void* s_glMultiDrawArrays;
	public static void glMultiDrawArrays(int mode, int* first, int* count, int drawcount) => ((delegate* unmanaged[Cdecl]<int, int*, int*, int, void>)s_glMultiDrawArrays)(mode, first, count, drawcount);

	private static void* s_glMultiDrawArraysIndirect;
	public static void glMultiDrawArraysIndirect(int mode, void* indirect, int drawcount, int stride) => ((delegate* unmanaged[Cdecl]<int, void*, int, int, void>)s_glMultiDrawArraysIndirect)(mode, indirect, drawcount, stride);

	private static void* s_glMultiDrawArraysIndirectCount;
	public static void glMultiDrawArraysIndirectCount(int mode, void* indirect, IntPtr drawcount, int maxdrawcount, int stride) => ((delegate* unmanaged[Cdecl]<int, void*, IntPtr, int, int, void>)s_glMultiDrawArraysIndirectCount)(mode, indirect, drawcount, maxdrawcount, stride);

	private static void* s_glMultiDrawElements;
	public static void glMultiDrawElements(int mode, int* count, int type, void** indices, int drawcount) => ((delegate* unmanaged[Cdecl]<int, int*, int, void**, int, void>)s_glMultiDrawElements)(mode, count, type, indices, drawcount);

	private static void* s_glMultiDrawElementsBaseVertex;
	public static void glMultiDrawElementsBaseVertex(int mode, int* count, int type, void** indices, int drawcount, int* basevertex) => ((delegate* unmanaged[Cdecl]<int, int*, int, void**, int, int*, void>)s_glMultiDrawElementsBaseVertex)(mode, count, type, indices, drawcount, basevertex);

	private static void* s_glMultiDrawElementsIndirect;
	public static void glMultiDrawElementsIndirect(int mode, int type, void* indirect, int drawcount, int stride) => ((delegate* unmanaged[Cdecl]<int, int, void*, int, int, void>)s_glMultiDrawElementsIndirect)(mode, type, indirect, drawcount, stride);

	private static void* s_glMultiDrawElementsIndirectCount;
	public static void glMultiDrawElementsIndirectCount(int mode, int type, void* indirect, IntPtr drawcount, int maxdrawcount, int stride) => ((delegate* unmanaged[Cdecl]<int, int, void*, IntPtr, int, int, void>)s_glMultiDrawElementsIndirectCount)(mode, type, indirect, drawcount, maxdrawcount, stride);

	private static void* s_glMultiTexCoordP1ui;
	public static void glMultiTexCoordP1ui(int texture, int type, uint coords) => ((delegate* unmanaged[Cdecl]<int, int, uint, void>)s_glMultiTexCoordP1ui)(texture, type, coords);

	private static void* s_glMultiTexCoordP1uiv;
	public static void glMultiTexCoordP1uiv(int texture, int type, uint* coords) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glMultiTexCoordP1uiv)(texture, type, coords);

	private static void* s_glMultiTexCoordP2ui;
	public static void glMultiTexCoordP2ui(int texture, int type, uint coords) => ((delegate* unmanaged[Cdecl]<int, int, uint, void>)s_glMultiTexCoordP2ui)(texture, type, coords);

	private static void* s_glMultiTexCoordP2uiv;
	public static void glMultiTexCoordP2uiv(int texture, int type, uint* coords) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glMultiTexCoordP2uiv)(texture, type, coords);

	private static void* s_glMultiTexCoordP3ui;
	public static void glMultiTexCoordP3ui(int texture, int type, uint coords) => ((delegate* unmanaged[Cdecl]<int, int, uint, void>)s_glMultiTexCoordP3ui)(texture, type, coords);

	private static void* s_glMultiTexCoordP3uiv;
	public static void glMultiTexCoordP3uiv(int texture, int type, uint* coords) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glMultiTexCoordP3uiv)(texture, type, coords);

	private static void* s_glMultiTexCoordP4ui;
	public static void glMultiTexCoordP4ui(int texture, int type, uint coords) => ((delegate* unmanaged[Cdecl]<int, int, uint, void>)s_glMultiTexCoordP4ui)(texture, type, coords);

	private static void* s_glMultiTexCoordP4uiv;
	public static void glMultiTexCoordP4uiv(int texture, int type, uint* coords) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glMultiTexCoordP4uiv)(texture, type, coords);

	private static void* s_glNamedBufferData;
	public static void glNamedBufferData(uint buffer, IntPtr size, void* data, int usage) => ((delegate* unmanaged[Cdecl]<uint, IntPtr, void*, int, void>)s_glNamedBufferData)(buffer, size, data, usage);

	private static void* s_glNamedBufferStorage;
	public static void glNamedBufferStorage(uint buffer, IntPtr size, void* data, int flags) => ((delegate* unmanaged[Cdecl]<uint, IntPtr, void*, int, void>)s_glNamedBufferStorage)(buffer, size, data, flags);

	private static void* s_glNamedBufferSubData;
	public static void glNamedBufferSubData(uint buffer, IntPtr offset, IntPtr size, void* data) => ((delegate* unmanaged[Cdecl]<uint, IntPtr, IntPtr, void*, void>)s_glNamedBufferSubData)(buffer, offset, size, data);

	private static void* s_glNamedFramebufferDrawBuffer;
	public static void glNamedFramebufferDrawBuffer(uint framebuffer, int buf) => ((delegate* unmanaged[Cdecl]<uint, int, void>)s_glNamedFramebufferDrawBuffer)(framebuffer, buf);

	private static void* s_glNamedFramebufferDrawBuffers;
	public static void glNamedFramebufferDrawBuffers(uint framebuffer, int n, int* bufs) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glNamedFramebufferDrawBuffers)(framebuffer, n, bufs);

	private static void* s_glNamedFramebufferParameteri;
	public static void glNamedFramebufferParameteri(uint framebuffer, int pname, int param) => ((delegate* unmanaged[Cdecl]<uint, int, int, void>)s_glNamedFramebufferParameteri)(framebuffer, pname, param);

	private static void* s_glNamedFramebufferReadBuffer;
	public static void glNamedFramebufferReadBuffer(uint framebuffer, int src) => ((delegate* unmanaged[Cdecl]<uint, int, void>)s_glNamedFramebufferReadBuffer)(framebuffer, src);

	private static void* s_glNamedFramebufferRenderbuffer;
	public static void glNamedFramebufferRenderbuffer(uint framebuffer, int attachment, int renderbuffertarget, uint renderbuffer) => ((delegate* unmanaged[Cdecl]<uint, int, int, uint, void>)s_glNamedFramebufferRenderbuffer)(framebuffer, attachment, renderbuffertarget, renderbuffer);

	private static void* s_glNamedFramebufferTexture;
	public static void glNamedFramebufferTexture(uint framebuffer, int attachment, uint texture, int level) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, void>)s_glNamedFramebufferTexture)(framebuffer, attachment, texture, level);

	private static void* s_glNamedFramebufferTextureLayer;
	public static void glNamedFramebufferTextureLayer(uint framebuffer, int attachment, uint texture, int level, int layer) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, int, void>)s_glNamedFramebufferTextureLayer)(framebuffer, attachment, texture, level, layer);

	private static void* s_glNamedRenderbufferStorage;
	public static void glNamedRenderbufferStorage(uint renderbuffer, int internalformat, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, void>)s_glNamedRenderbufferStorage)(renderbuffer, internalformat, width, height);

	private static void* s_glNamedRenderbufferStorageMultisample;
	public static void glNamedRenderbufferStorageMultisample(uint renderbuffer, int samples, int internalformat, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, void>)s_glNamedRenderbufferStorageMultisample)(renderbuffer, samples, internalformat, width, height);

	private static void* s_glNormalP3ui;
	public static void glNormalP3ui(int type, uint coords) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glNormalP3ui)(type, coords);

	private static void* s_glNormalP3uiv;
	public static void glNormalP3uiv(int type, uint* coords) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glNormalP3uiv)(type, coords);

	private static void* s_glObjectLabel;
	public static void glObjectLabel(int identifier, uint name, int length, byte* label) => ((delegate* unmanaged[Cdecl]<int, uint, int, byte*, void>)s_glObjectLabel)(identifier, name, length, label);

	private static void* s_glObjectPtrLabel;
	public static void glObjectPtrLabel(void* ptr, int length, byte* label) => ((delegate* unmanaged[Cdecl]<void*, int, byte*, void>)s_glObjectPtrLabel)(ptr, length, label);

	private static void* s_glPatchParameterfv;
	public static void glPatchParameterfv(int pname, float* values) => ((delegate* unmanaged[Cdecl]<int, float*, void>)s_glPatchParameterfv)(pname, values);

	private static void* s_glPatchParameteri;
	public static void glPatchParameteri(int pname, int value) => ((delegate* unmanaged[Cdecl]<int, int, void>)s_glPatchParameteri)(pname, value);

	private static void* s_glPauseTransformFeedback;
	public static void glPauseTransformFeedback() => ((delegate* unmanaged[Cdecl]<void>)s_glPauseTransformFeedback)();

	private static void* s_glPixelStoref;
	public static void glPixelStoref(int pname, float param) => ((delegate* unmanaged[Cdecl]<int, float, void>)s_glPixelStoref)(pname, param);

	private static void* s_glPixelStorei;
	public static void glPixelStorei(int pname, int param) => ((delegate* unmanaged[Cdecl]<int, int, void>)s_glPixelStorei)(pname, param);

	private static void* s_glPointParameterf;
	public static void glPointParameterf(int pname, float param) => ((delegate* unmanaged[Cdecl]<int, float, void>)s_glPointParameterf)(pname, param);

	private static void* s_glPointParameterfv;
	public static void glPointParameterfv(int pname, float* args) => ((delegate* unmanaged[Cdecl]<int, float*, void>)s_glPointParameterfv)(pname, args);

	private static void* s_glPointParameteri;
	public static void glPointParameteri(int pname, int param) => ((delegate* unmanaged[Cdecl]<int, int, void>)s_glPointParameteri)(pname, param);

	private static void* s_glPointParameteriv;
	public static void glPointParameteriv(int pname, int* args) => ((delegate* unmanaged[Cdecl]<int, int*, void>)s_glPointParameteriv)(pname, args);

	private static void* s_glPointSize;
	public static void glPointSize(float size) => ((delegate* unmanaged[Cdecl]<float, void>)s_glPointSize)(size);

	private static void* s_glPolygonMode;
	public static void glPolygonMode(int face, int mode) => ((delegate* unmanaged[Cdecl]<int, int, void>)s_glPolygonMode)(face, mode);

	private static void* s_glPolygonOffset;
	public static void glPolygonOffset(float factor, float units) => ((delegate* unmanaged[Cdecl]<float, float, void>)s_glPolygonOffset)(factor, units);

	private static void* s_glPolygonOffsetClamp;
	public static void glPolygonOffsetClamp(float factor, float units, float clamp) => ((delegate* unmanaged[Cdecl]<float, float, float, void>)s_glPolygonOffsetClamp)(factor, units, clamp);

	private static void* s_glPopDebugGroup;
	public static void glPopDebugGroup() => ((delegate* unmanaged[Cdecl]<void>)s_glPopDebugGroup)();

	private static void* s_glPrimitiveRestartIndex;
	public static void glPrimitiveRestartIndex(uint index) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glPrimitiveRestartIndex)(index);

	private static void* s_glProgramBinary;
	public static void glProgramBinary(uint program, int binaryFormat, void* binary, int length) => ((delegate* unmanaged[Cdecl]<uint, int, void*, int, void>)s_glProgramBinary)(program, binaryFormat, binary, length);

	private static void* s_glProgramParameteri;
	public static void glProgramParameteri(uint program, int pname, int value) => ((delegate* unmanaged[Cdecl]<uint, int, int, void>)s_glProgramParameteri)(program, pname, value);

	private static void* s_glProgramUniform1d;
	public static void glProgramUniform1d(uint program, int location, double v0) => ((delegate* unmanaged[Cdecl]<uint, int, double, void>)s_glProgramUniform1d)(program, location, v0);

	private static void* s_glProgramUniform1dv;
	public static void glProgramUniform1dv(uint program, int location, int count, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, double*, void>)s_glProgramUniform1dv)(program, location, count, value);

	private static void* s_glProgramUniform1f;
	public static void glProgramUniform1f(uint program, int location, float v0) => ((delegate* unmanaged[Cdecl]<uint, int, float, void>)s_glProgramUniform1f)(program, location, v0);

	private static void* s_glProgramUniform1fv;
	public static void glProgramUniform1fv(uint program, int location, int count, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, float*, void>)s_glProgramUniform1fv)(program, location, count, value);

	private static void* s_glProgramUniform1i;
	public static void glProgramUniform1i(uint program, int location, int v0) => ((delegate* unmanaged[Cdecl]<uint, int, int, void>)s_glProgramUniform1i)(program, location, v0);

	private static void* s_glProgramUniform1iv;
	public static void glProgramUniform1iv(uint program, int location, int count, int* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, int*, void>)s_glProgramUniform1iv)(program, location, count, value);

	private static void* s_glProgramUniform1ui;
	public static void glProgramUniform1ui(uint program, int location, uint v0) => ((delegate* unmanaged[Cdecl]<uint, int, uint, void>)s_glProgramUniform1ui)(program, location, v0);

	private static void* s_glProgramUniform1uiv;
	public static void glProgramUniform1uiv(uint program, int location, int count, uint* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, uint*, void>)s_glProgramUniform1uiv)(program, location, count, value);

	private static void* s_glProgramUniform2d;
	public static void glProgramUniform2d(uint program, int location, double v0, double v1) => ((delegate* unmanaged[Cdecl]<uint, int, double, double, void>)s_glProgramUniform2d)(program, location, v0, v1);

	private static void* s_glProgramUniform2dv;
	public static void glProgramUniform2dv(uint program, int location, int count, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, double*, void>)s_glProgramUniform2dv)(program, location, count, value);

	private static void* s_glProgramUniform2f;
	public static void glProgramUniform2f(uint program, int location, float v0, float v1) => ((delegate* unmanaged[Cdecl]<uint, int, float, float, void>)s_glProgramUniform2f)(program, location, v0, v1);

	private static void* s_glProgramUniform2fv;
	public static void glProgramUniform2fv(uint program, int location, int count, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, float*, void>)s_glProgramUniform2fv)(program, location, count, value);

	private static void* s_glProgramUniform2i;
	public static void glProgramUniform2i(uint program, int location, int v0, int v1) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, void>)s_glProgramUniform2i)(program, location, v0, v1);

	private static void* s_glProgramUniform2iv;
	public static void glProgramUniform2iv(uint program, int location, int count, int* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, int*, void>)s_glProgramUniform2iv)(program, location, count, value);

	private static void* s_glProgramUniform2ui;
	public static void glProgramUniform2ui(uint program, int location, uint v0, uint v1) => ((delegate* unmanaged[Cdecl]<uint, int, uint, uint, void>)s_glProgramUniform2ui)(program, location, v0, v1);

	private static void* s_glProgramUniform2uiv;
	public static void glProgramUniform2uiv(uint program, int location, int count, uint* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, uint*, void>)s_glProgramUniform2uiv)(program, location, count, value);

	private static void* s_glProgramUniform3d;
	public static void glProgramUniform3d(uint program, int location, double v0, double v1, double v2) => ((delegate* unmanaged[Cdecl]<uint, int, double, double, double, void>)s_glProgramUniform3d)(program, location, v0, v1, v2);

	private static void* s_glProgramUniform3dv;
	public static void glProgramUniform3dv(uint program, int location, int count, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, double*, void>)s_glProgramUniform3dv)(program, location, count, value);

	private static void* s_glProgramUniform3f;
	public static void glProgramUniform3f(uint program, int location, float v0, float v1, float v2) => ((delegate* unmanaged[Cdecl]<uint, int, float, float, float, void>)s_glProgramUniform3f)(program, location, v0, v1, v2);

	private static void* s_glProgramUniform3fv;
	public static void glProgramUniform3fv(uint program, int location, int count, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, float*, void>)s_glProgramUniform3fv)(program, location, count, value);

	private static void* s_glProgramUniform3i;
	public static void glProgramUniform3i(uint program, int location, int v0, int v1, int v2) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, void>)s_glProgramUniform3i)(program, location, v0, v1, v2);

	private static void* s_glProgramUniform3iv;
	public static void glProgramUniform3iv(uint program, int location, int count, int* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, int*, void>)s_glProgramUniform3iv)(program, location, count, value);

	private static void* s_glProgramUniform3ui;
	public static void glProgramUniform3ui(uint program, int location, uint v0, uint v1, uint v2) => ((delegate* unmanaged[Cdecl]<uint, int, uint, uint, uint, void>)s_glProgramUniform3ui)(program, location, v0, v1, v2);

	private static void* s_glProgramUniform3uiv;
	public static void glProgramUniform3uiv(uint program, int location, int count, uint* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, uint*, void>)s_glProgramUniform3uiv)(program, location, count, value);

	private static void* s_glProgramUniform4d;
	public static void glProgramUniform4d(uint program, int location, double v0, double v1, double v2, double v3) => ((delegate* unmanaged[Cdecl]<uint, int, double, double, double, double, void>)s_glProgramUniform4d)(program, location, v0, v1, v2, v3);

	private static void* s_glProgramUniform4dv;
	public static void glProgramUniform4dv(uint program, int location, int count, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, double*, void>)s_glProgramUniform4dv)(program, location, count, value);

	private static void* s_glProgramUniform4f;
	public static void glProgramUniform4f(uint program, int location, float v0, float v1, float v2, float v3) => ((delegate* unmanaged[Cdecl]<uint, int, float, float, float, float, void>)s_glProgramUniform4f)(program, location, v0, v1, v2, v3);

	private static void* s_glProgramUniform4fv;
	public static void glProgramUniform4fv(uint program, int location, int count, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, float*, void>)s_glProgramUniform4fv)(program, location, count, value);

	private static void* s_glProgramUniform4i;
	public static void glProgramUniform4i(uint program, int location, int v0, int v1, int v2, int v3) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, void>)s_glProgramUniform4i)(program, location, v0, v1, v2, v3);

	private static void* s_glProgramUniform4iv;
	public static void glProgramUniform4iv(uint program, int location, int count, int* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, int*, void>)s_glProgramUniform4iv)(program, location, count, value);

	private static void* s_glProgramUniform4ui;
	public static void glProgramUniform4ui(uint program, int location, uint v0, uint v1, uint v2, uint v3) => ((delegate* unmanaged[Cdecl]<uint, int, uint, uint, uint, uint, void>)s_glProgramUniform4ui)(program, location, v0, v1, v2, v3);

	private static void* s_glProgramUniform4uiv;
	public static void glProgramUniform4uiv(uint program, int location, int count, uint* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, uint*, void>)s_glProgramUniform4uiv)(program, location, count, value);

	private static void* s_glProgramUniformMatrix2dv;
	public static void glProgramUniformMatrix2dv(uint program, int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, double*, void>)s_glProgramUniformMatrix2dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix2fv;
	public static void glProgramUniformMatrix2fv(uint program, int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, float*, void>)s_glProgramUniformMatrix2fv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix2x3dv;
	public static void glProgramUniformMatrix2x3dv(uint program, int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, double*, void>)s_glProgramUniformMatrix2x3dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix2x3fv;
	public static void glProgramUniformMatrix2x3fv(uint program, int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, float*, void>)s_glProgramUniformMatrix2x3fv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix2x4dv;
	public static void glProgramUniformMatrix2x4dv(uint program, int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, double*, void>)s_glProgramUniformMatrix2x4dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix2x4fv;
	public static void glProgramUniformMatrix2x4fv(uint program, int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, float*, void>)s_glProgramUniformMatrix2x4fv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix3dv;
	public static void glProgramUniformMatrix3dv(uint program, int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, double*, void>)s_glProgramUniformMatrix3dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix3fv;
	public static void glProgramUniformMatrix3fv(uint program, int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, float*, void>)s_glProgramUniformMatrix3fv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix3x2dv;
	public static void glProgramUniformMatrix3x2dv(uint program, int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, double*, void>)s_glProgramUniformMatrix3x2dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix3x2fv;
	public static void glProgramUniformMatrix3x2fv(uint program, int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, float*, void>)s_glProgramUniformMatrix3x2fv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix3x4dv;
	public static void glProgramUniformMatrix3x4dv(uint program, int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, double*, void>)s_glProgramUniformMatrix3x4dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix3x4fv;
	public static void glProgramUniformMatrix3x4fv(uint program, int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, float*, void>)s_glProgramUniformMatrix3x4fv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix4dv;
	public static void glProgramUniformMatrix4dv(uint program, int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, double*, void>)s_glProgramUniformMatrix4dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix4fv;
	public static void glProgramUniformMatrix4fv(uint program, int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, float*, void>)s_glProgramUniformMatrix4fv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix4x2dv;
	public static void glProgramUniformMatrix4x2dv(uint program, int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, double*, void>)s_glProgramUniformMatrix4x2dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix4x2fv;
	public static void glProgramUniformMatrix4x2fv(uint program, int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, float*, void>)s_glProgramUniformMatrix4x2fv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix4x3dv;
	public static void glProgramUniformMatrix4x3dv(uint program, int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, double*, void>)s_glProgramUniformMatrix4x3dv)(program, location, count, transpose, value);

	private static void* s_glProgramUniformMatrix4x3fv;
	public static void glProgramUniformMatrix4x3fv(uint program, int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, float*, void>)s_glProgramUniformMatrix4x3fv)(program, location, count, transpose, value);

	private static void* s_glProvokingVertex;
	public static void glProvokingVertex(int mode) => ((delegate* unmanaged[Cdecl]<int, void>)s_glProvokingVertex)(mode);

	private static void* s_glPushDebugGroup;
	public static void glPushDebugGroup(int source, uint id, int length, byte* message) => ((delegate* unmanaged[Cdecl]<int, uint, int, byte*, void>)s_glPushDebugGroup)(source, id, length, message);

	private static void* s_glQueryCounter;
	public static void glQueryCounter(uint id, int target) => ((delegate* unmanaged[Cdecl]<uint, int, void>)s_glQueryCounter)(id, target);

	private static void* s_glReadBuffer;
	public static void glReadBuffer(int src) => ((delegate* unmanaged[Cdecl]<int, void>)s_glReadBuffer)(src);

	private static void* s_glReadPixels;
	public static void glReadPixels(int x, int y, int width, int height, int format, int type, void* pixels) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, void*, void>)s_glReadPixels)(x, y, width, height, format, type, pixels);

	private static void* s_glReadnPixels;
	public static void glReadnPixels(int x, int y, int width, int height, int format, int type, int bufSize, void* data) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, void*, void>)s_glReadnPixels)(x, y, width, height, format, type, bufSize, data);

	private static void* s_glReleaseShaderCompiler;
	public static void glReleaseShaderCompiler() => ((delegate* unmanaged[Cdecl]<void>)s_glReleaseShaderCompiler)();

	private static void* s_glRenderbufferStorage;
	public static void glRenderbufferStorage(int target, int internalformat, int width, int height) => ((delegate* unmanaged[Cdecl]<int, int, int, int, void>)s_glRenderbufferStorage)(target, internalformat, width, height);

	private static void* s_glRenderbufferStorageMultisample;
	public static void glRenderbufferStorageMultisample(int target, int samples, int internalformat, int width, int height) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, void>)s_glRenderbufferStorageMultisample)(target, samples, internalformat, width, height);

	private static void* s_glResumeTransformFeedback;
	public static void glResumeTransformFeedback() => ((delegate* unmanaged[Cdecl]<void>)s_glResumeTransformFeedback)();

	private static void* s_glSampleCoverage;
	public static void glSampleCoverage(float value, bool invert) => ((delegate* unmanaged[Cdecl]<float, bool, void>)s_glSampleCoverage)(value, invert);

	private static void* s_glSampleMaski;
	public static void glSampleMaski(uint maskNumber, int mask) => ((delegate* unmanaged[Cdecl]<uint, int, void>)s_glSampleMaski)(maskNumber, mask);

	private static void* s_glSamplerParameterIiv;
	public static void glSamplerParameterIiv(uint sampler, int pname, int* param) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glSamplerParameterIiv)(sampler, pname, param);

	private static void* s_glSamplerParameterIuiv;
	public static void glSamplerParameterIuiv(uint sampler, int pname, uint* param) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glSamplerParameterIuiv)(sampler, pname, param);

	private static void* s_glSamplerParameterf;
	public static void glSamplerParameterf(uint sampler, int pname, float param) => ((delegate* unmanaged[Cdecl]<uint, int, float, void>)s_glSamplerParameterf)(sampler, pname, param);

	private static void* s_glSamplerParameterfv;
	public static void glSamplerParameterfv(uint sampler, int pname, float* param) => ((delegate* unmanaged[Cdecl]<uint, int, float*, void>)s_glSamplerParameterfv)(sampler, pname, param);

	private static void* s_glSamplerParameteri;
	public static void glSamplerParameteri(uint sampler, int pname, int param) => ((delegate* unmanaged[Cdecl]<uint, int, int, void>)s_glSamplerParameteri)(sampler, pname, param);

	private static void* s_glSamplerParameteriv;
	public static void glSamplerParameteriv(uint sampler, int pname, int* param) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glSamplerParameteriv)(sampler, pname, param);

	private static void* s_glScissor;
	public static void glScissor(int x, int y, int width, int height) => ((delegate* unmanaged[Cdecl]<int, int, int, int, void>)s_glScissor)(x, y, width, height);

	private static void* s_glScissorArrayv;
	public static void glScissorArrayv(uint first, int count, int* v) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glScissorArrayv)(first, count, v);

	private static void* s_glScissorIndexed;
	public static void glScissorIndexed(uint index, int left, int bottom, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, void>)s_glScissorIndexed)(index, left, bottom, width, height);

	private static void* s_glScissorIndexedv;
	public static void glScissorIndexedv(uint index, int* v) => ((delegate* unmanaged[Cdecl]<uint, int*, void>)s_glScissorIndexedv)(index, v);

	private static void* s_glSecondaryColorP3ui;
	public static void glSecondaryColorP3ui(int type, uint color) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glSecondaryColorP3ui)(type, color);

	private static void* s_glSecondaryColorP3uiv;
	public static void glSecondaryColorP3uiv(int type, uint* color) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glSecondaryColorP3uiv)(type, color);

	private static void* s_glShaderBinary;
	public static void glShaderBinary(int count, uint* shaders, int binaryFormat, void* binary, int length) => ((delegate* unmanaged[Cdecl]<int, uint*, int, void*, int, void>)s_glShaderBinary)(count, shaders, binaryFormat, binary, length);

	private static void* s_glShaderSource;
	public static void glShaderSource(uint shader, int count, byte* str, int* length) => ((delegate* unmanaged[Cdecl]<uint, int, byte*, int*, void>)s_glShaderSource)(shader, count, str, length);

	private static void* s_glShaderStorageBlockBinding;
	public static void glShaderStorageBlockBinding(uint program, uint storageBlockIndex, uint storageBlockBinding) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glShaderStorageBlockBinding)(program, storageBlockIndex, storageBlockBinding);

	private static void* s_glSpecializeShader;
	public static void glSpecializeShader(uint shader, byte* pEntryPoint, uint numSpecializationConstants, uint* pConstantIndex, uint* pConstantValue) => ((delegate* unmanaged[Cdecl]<uint, byte*, uint, uint*, uint*, void>)s_glSpecializeShader)(shader, pEntryPoint, numSpecializationConstants, pConstantIndex, pConstantValue);

	private static void* s_glStencilFunc;
	public static void glStencilFunc(int func, int reference, uint mask) => ((delegate* unmanaged[Cdecl]<int, int, uint, void>)s_glStencilFunc)(func, reference, mask);

	private static void* s_glStencilFuncSeparate;
	public static void glStencilFuncSeparate(int face, int func, int reference, uint mask) => ((delegate* unmanaged[Cdecl]<int, int, int, uint, void>)s_glStencilFuncSeparate)(face, func, reference, mask);

	private static void* s_glStencilMask;
	public static void glStencilMask(uint mask) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glStencilMask)(mask);

	private static void* s_glStencilMaskSeparate;
	public static void glStencilMaskSeparate(int face, uint mask) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glStencilMaskSeparate)(face, mask);

	private static void* s_glStencilOp;
	public static void glStencilOp(int fail, int zfail, int zpass) => ((delegate* unmanaged[Cdecl]<int, int, int, void>)s_glStencilOp)(fail, zfail, zpass);

	private static void* s_glStencilOpSeparate;
	public static void glStencilOpSeparate(int face, int sfail, int dpfail, int dppass) => ((delegate* unmanaged[Cdecl]<int, int, int, int, void>)s_glStencilOpSeparate)(face, sfail, dpfail, dppass);

	private static void* s_glTexBuffer;
	public static void glTexBuffer(int target, int internalformat, uint buffer) => ((delegate* unmanaged[Cdecl]<int, int, uint, void>)s_glTexBuffer)(target, internalformat, buffer);

	private static void* s_glTexBufferRange;
	public static void glTexBufferRange(int target, int internalformat, uint buffer, IntPtr offset, IntPtr size) => ((delegate* unmanaged[Cdecl]<int, int, uint, IntPtr, IntPtr, void>)s_glTexBufferRange)(target, internalformat, buffer, offset, size);

	private static void* s_glTexCoordP1ui;
	public static void glTexCoordP1ui(int type, uint coords) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glTexCoordP1ui)(type, coords);

	private static void* s_glTexCoordP1uiv;
	public static void glTexCoordP1uiv(int type, uint* coords) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glTexCoordP1uiv)(type, coords);

	private static void* s_glTexCoordP2ui;
	public static void glTexCoordP2ui(int type, uint coords) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glTexCoordP2ui)(type, coords);

	private static void* s_glTexCoordP2uiv;
	public static void glTexCoordP2uiv(int type, uint* coords) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glTexCoordP2uiv)(type, coords);

	private static void* s_glTexCoordP3ui;
	public static void glTexCoordP3ui(int type, uint coords) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glTexCoordP3ui)(type, coords);

	private static void* s_glTexCoordP3uiv;
	public static void glTexCoordP3uiv(int type, uint* coords) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glTexCoordP3uiv)(type, coords);

	private static void* s_glTexCoordP4ui;
	public static void glTexCoordP4ui(int type, uint coords) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glTexCoordP4ui)(type, coords);

	private static void* s_glTexCoordP4uiv;
	public static void glTexCoordP4uiv(int type, uint* coords) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glTexCoordP4uiv)(type, coords);

	private static void* s_glTexImage1D;
	public static void glTexImage1D(int target, int level, int internalformat, int width, int border, int format, int type, void* pixels) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, void*, void>)s_glTexImage1D)(target, level, internalformat, width, border, format, type, pixels);

	private static void* s_glTexImage2D;
	public static void glTexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, void* pixels) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, int, void*, void>)s_glTexImage2D)(target, level, internalformat, width, height, border, format, type, pixels);

	private static void* s_glTexImage2DMultisample;
	public static void glTexImage2DMultisample(int target, int samples, int internalformat, int width, int height, bool fixedsamplelocations) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, bool, void>)s_glTexImage2DMultisample)(target, samples, internalformat, width, height, fixedsamplelocations);

	private static void* s_glTexImage3D;
	public static void glTexImage3D(int target, int level, int internalformat, int width, int height, int depth, int border, int format, int type, void* pixels) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, int, int, void*, void>)s_glTexImage3D)(target, level, internalformat, width, height, depth, border, format, type, pixels);

	private static void* s_glTexImage3DMultisample;
	public static void glTexImage3DMultisample(int target, int samples, int internalformat, int width, int height, int depth, bool fixedsamplelocations) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, bool, void>)s_glTexImage3DMultisample)(target, samples, internalformat, width, height, depth, fixedsamplelocations);

	private static void* s_glTexParameterIiv;
	public static void glTexParameterIiv(int target, int pname, int* args) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glTexParameterIiv)(target, pname, args);

	private static void* s_glTexParameterIuiv;
	public static void glTexParameterIuiv(int target, int pname, uint* args) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glTexParameterIuiv)(target, pname, args);

	private static void* s_glTexParameterf;
	public static void glTexParameterf(int target, int pname, float param) => ((delegate* unmanaged[Cdecl]<int, int, float, void>)s_glTexParameterf)(target, pname, param);

	private static void* s_glTexParameterfv;
	public static void glTexParameterfv(int target, int pname, float* args) => ((delegate* unmanaged[Cdecl]<int, int, float*, void>)s_glTexParameterfv)(target, pname, args);

	private static void* s_glTexParameteri;
	public static void glTexParameteri(int target, int pname, int param) => ((delegate* unmanaged[Cdecl]<int, int, int, void>)s_glTexParameteri)(target, pname, param);

	private static void* s_glTexParameteriv;
	public static void glTexParameteriv(int target, int pname, int* args) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glTexParameteriv)(target, pname, args);

	private static void* s_glTexStorage1D;
	public static void glTexStorage1D(int target, int levels, int internalformat, int width) => ((delegate* unmanaged[Cdecl]<int, int, int, int, void>)s_glTexStorage1D)(target, levels, internalformat, width);

	private static void* s_glTexStorage2D;
	public static void glTexStorage2D(int target, int levels, int internalformat, int width, int height) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, void>)s_glTexStorage2D)(target, levels, internalformat, width, height);

	private static void* s_glTexStorage2DMultisample;
	public static void glTexStorage2DMultisample(int target, int samples, int internalformat, int width, int height, bool fixedsamplelocations) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, bool, void>)s_glTexStorage2DMultisample)(target, samples, internalformat, width, height, fixedsamplelocations);

	private static void* s_glTexStorage3D;
	public static void glTexStorage3D(int target, int levels, int internalformat, int width, int height, int depth) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, void>)s_glTexStorage3D)(target, levels, internalformat, width, height, depth);

	private static void* s_glTexStorage3DMultisample;
	public static void glTexStorage3DMultisample(int target, int samples, int internalformat, int width, int height, int depth, bool fixedsamplelocations) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, bool, void>)s_glTexStorage3DMultisample)(target, samples, internalformat, width, height, depth, fixedsamplelocations);

	private static void* s_glTexSubImage1D;
	public static void glTexSubImage1D(int target, int level, int xoffset, int width, int format, int type, void* pixels) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, void*, void>)s_glTexSubImage1D)(target, level, xoffset, width, format, type, pixels);

	private static void* s_glTexSubImage2D;
	public static void glTexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, void* pixels) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, int, void*, void>)s_glTexSubImage2D)(target, level, xoffset, yoffset, width, height, format, type, pixels);

	private static void* s_glTexSubImage3D;
	public static void glTexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, void* pixels) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, int, int, int, void*, void>)s_glTexSubImage3D)(target, level, xoffset, yoffset, zoffset, width, height, depth, format, type, pixels);

	private static void* s_glTextureBarrier;
	public static void glTextureBarrier() => ((delegate* unmanaged[Cdecl]<void>)s_glTextureBarrier)();

	private static void* s_glTextureBuffer;
	public static void glTextureBuffer(uint texture, int internalformat, uint buffer) => ((delegate* unmanaged[Cdecl]<uint, int, uint, void>)s_glTextureBuffer)(texture, internalformat, buffer);

	private static void* s_glTextureBufferRange;
	public static void glTextureBufferRange(uint texture, int internalformat, uint buffer, IntPtr offset, IntPtr size) => ((delegate* unmanaged[Cdecl]<uint, int, uint, IntPtr, IntPtr, void>)s_glTextureBufferRange)(texture, internalformat, buffer, offset, size);

	private static void* s_glTextureParameterIiv;
	public static void glTextureParameterIiv(uint texture, int pname, int* args) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glTextureParameterIiv)(texture, pname, args);

	private static void* s_glTextureParameterIuiv;
	public static void glTextureParameterIuiv(uint texture, int pname, uint* args) => ((delegate* unmanaged[Cdecl]<uint, int, uint*, void>)s_glTextureParameterIuiv)(texture, pname, args);

	private static void* s_glTextureParameterf;
	public static void glTextureParameterf(uint texture, int pname, float param) => ((delegate* unmanaged[Cdecl]<uint, int, float, void>)s_glTextureParameterf)(texture, pname, param);

	private static void* s_glTextureParameterfv;
	public static void glTextureParameterfv(uint texture, int pname, float* param) => ((delegate* unmanaged[Cdecl]<uint, int, float*, void>)s_glTextureParameterfv)(texture, pname, param);

	private static void* s_glTextureParameteri;
	public static void glTextureParameteri(uint texture, int pname, int param) => ((delegate* unmanaged[Cdecl]<uint, int, int, void>)s_glTextureParameteri)(texture, pname, param);

	private static void* s_glTextureParameteriv;
	public static void glTextureParameteriv(uint texture, int pname, int* param) => ((delegate* unmanaged[Cdecl]<uint, int, int*, void>)s_glTextureParameteriv)(texture, pname, param);

	private static void* s_glTextureStorage1D;
	public static void glTextureStorage1D(uint texture, int levels, int internalformat, int width) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, void>)s_glTextureStorage1D)(texture, levels, internalformat, width);

	private static void* s_glTextureStorage2D;
	public static void glTextureStorage2D(uint texture, int levels, int internalformat, int width, int height) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, void>)s_glTextureStorage2D)(texture, levels, internalformat, width, height);

	private static void* s_glTextureStorage2DMultisample;
	public static void glTextureStorage2DMultisample(uint texture, int samples, int internalformat, int width, int height, bool fixedsamplelocations) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, bool, void>)s_glTextureStorage2DMultisample)(texture, samples, internalformat, width, height, fixedsamplelocations);

	private static void* s_glTextureStorage3D;
	public static void glTextureStorage3D(uint texture, int levels, int internalformat, int width, int height, int depth) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, void>)s_glTextureStorage3D)(texture, levels, internalformat, width, height, depth);

	private static void* s_glTextureStorage3DMultisample;
	public static void glTextureStorage3DMultisample(uint texture, int samples, int internalformat, int width, int height, int depth, bool fixedsamplelocations) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, bool, void>)s_glTextureStorage3DMultisample)(texture, samples, internalformat, width, height, depth, fixedsamplelocations);

	private static void* s_glTextureSubImage1D;
	public static void glTextureSubImage1D(uint texture, int level, int xoffset, int width, int format, int type, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, void*, void>)s_glTextureSubImage1D)(texture, level, xoffset, width, format, type, pixels);

	private static void* s_glTextureSubImage2D;
	public static void glTextureSubImage2D(uint texture, int level, int xoffset, int yoffset, int width, int height, int format, int type, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, void*, void>)s_glTextureSubImage2D)(texture, level, xoffset, yoffset, width, height, format, type, pixels);

	private static void* s_glTextureSubImage3D;
	public static void glTextureSubImage3D(uint texture, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, void* pixels) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, int, int, int, int, int, void*, void>)s_glTextureSubImage3D)(texture, level, xoffset, yoffset, zoffset, width, height, depth, format, type, pixels);

	private static void* s_glTextureView;
	public static void glTextureView(uint texture, int target, uint origtexture, int internalformat, uint minlevel, uint numlevels, uint minlayer, uint numlayers) => ((delegate* unmanaged[Cdecl]<uint, int, uint, int, uint, uint, uint, uint, void>)s_glTextureView)(texture, target, origtexture, internalformat, minlevel, numlevels, minlayer, numlayers);

	private static void* s_glTransformFeedbackBufferBase;
	public static void glTransformFeedbackBufferBase(uint xfb, uint index, uint buffer) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glTransformFeedbackBufferBase)(xfb, index, buffer);

	private static void* s_glTransformFeedbackBufferRange;
	public static void glTransformFeedbackBufferRange(uint xfb, uint index, uint buffer, IntPtr offset, IntPtr size) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, IntPtr, IntPtr, void>)s_glTransformFeedbackBufferRange)(xfb, index, buffer, offset, size);

	private static void* s_glTransformFeedbackVaryings;
	public static void glTransformFeedbackVaryings(uint program, int count, byte* varyings, int bufferMode) => ((delegate* unmanaged[Cdecl]<uint, int, byte*, int, void>)s_glTransformFeedbackVaryings)(program, count, varyings, bufferMode);

	private static void* s_glUniform1d;
	public static void glUniform1d(int location, double x) => ((delegate* unmanaged[Cdecl]<int, double, void>)s_glUniform1d)(location, x);

	private static void* s_glUniform1dv;
	public static void glUniform1dv(int location, int count, double* value) => ((delegate* unmanaged[Cdecl]<int, int, double*, void>)s_glUniform1dv)(location, count, value);

	private static void* s_glUniform1f;
	public static void glUniform1f(int location, float v0) => ((delegate* unmanaged[Cdecl]<int, float, void>)s_glUniform1f)(location, v0);

	private static void* s_glUniform1fv;
	public static void glUniform1fv(int location, int count, float* value) => ((delegate* unmanaged[Cdecl]<int, int, float*, void>)s_glUniform1fv)(location, count, value);

	private static void* s_glUniform1i;
	public static void glUniform1i(int location, int v0) => ((delegate* unmanaged[Cdecl]<int, int, void>)s_glUniform1i)(location, v0);

	private static void* s_glUniform1iv;
	public static void glUniform1iv(int location, int count, int* value) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glUniform1iv)(location, count, value);

	private static void* s_glUniform1ui;
	public static void glUniform1ui(int location, uint v0) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glUniform1ui)(location, v0);

	private static void* s_glUniform1uiv;
	public static void glUniform1uiv(int location, int count, uint* value) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glUniform1uiv)(location, count, value);

	private static void* s_glUniform2d;
	public static void glUniform2d(int location, double x, double y) => ((delegate* unmanaged[Cdecl]<int, double, double, void>)s_glUniform2d)(location, x, y);

	private static void* s_glUniform2dv;
	public static void glUniform2dv(int location, int count, double* value) => ((delegate* unmanaged[Cdecl]<int, int, double*, void>)s_glUniform2dv)(location, count, value);

	private static void* s_glUniform2f;
	public static void glUniform2f(int location, float v0, float v1) => ((delegate* unmanaged[Cdecl]<int, float, float, void>)s_glUniform2f)(location, v0, v1);

	private static void* s_glUniform2fv;
	public static void glUniform2fv(int location, int count, float* value) => ((delegate* unmanaged[Cdecl]<int, int, float*, void>)s_glUniform2fv)(location, count, value);

	private static void* s_glUniform2i;
	public static void glUniform2i(int location, int v0, int v1) => ((delegate* unmanaged[Cdecl]<int, int, int, void>)s_glUniform2i)(location, v0, v1);

	private static void* s_glUniform2iv;
	public static void glUniform2iv(int location, int count, int* value) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glUniform2iv)(location, count, value);

	private static void* s_glUniform2ui;
	public static void glUniform2ui(int location, uint v0, uint v1) => ((delegate* unmanaged[Cdecl]<int, uint, uint, void>)s_glUniform2ui)(location, v0, v1);

	private static void* s_glUniform2uiv;
	public static void glUniform2uiv(int location, int count, uint* value) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glUniform2uiv)(location, count, value);

	private static void* s_glUniform3d;
	public static void glUniform3d(int location, double x, double y, double z) => ((delegate* unmanaged[Cdecl]<int, double, double, double, void>)s_glUniform3d)(location, x, y, z);

	private static void* s_glUniform3dv;
	public static void glUniform3dv(int location, int count, double* value) => ((delegate* unmanaged[Cdecl]<int, int, double*, void>)s_glUniform3dv)(location, count, value);

	private static void* s_glUniform3f;
	public static void glUniform3f(int location, float v0, float v1, float v2) => ((delegate* unmanaged[Cdecl]<int, float, float, float, void>)s_glUniform3f)(location, v0, v1, v2);

	private static void* s_glUniform3fv;
	public static void glUniform3fv(int location, int count, float* value) => ((delegate* unmanaged[Cdecl]<int, int, float*, void>)s_glUniform3fv)(location, count, value);

	private static void* s_glUniform3i;
	public static void glUniform3i(int location, int v0, int v1, int v2) => ((delegate* unmanaged[Cdecl]<int, int, int, int, void>)s_glUniform3i)(location, v0, v1, v2);

	private static void* s_glUniform3iv;
	public static void glUniform3iv(int location, int count, int* value) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glUniform3iv)(location, count, value);

	private static void* s_glUniform3ui;
	public static void glUniform3ui(int location, uint v0, uint v1, uint v2) => ((delegate* unmanaged[Cdecl]<int, uint, uint, uint, void>)s_glUniform3ui)(location, v0, v1, v2);

	private static void* s_glUniform3uiv;
	public static void glUniform3uiv(int location, int count, uint* value) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glUniform3uiv)(location, count, value);

	private static void* s_glUniform4d;
	public static void glUniform4d(int location, double x, double y, double z, double w) => ((delegate* unmanaged[Cdecl]<int, double, double, double, double, void>)s_glUniform4d)(location, x, y, z, w);

	private static void* s_glUniform4dv;
	public static void glUniform4dv(int location, int count, double* value) => ((delegate* unmanaged[Cdecl]<int, int, double*, void>)s_glUniform4dv)(location, count, value);

	private static void* s_glUniform4f;
	public static void glUniform4f(int location, float v0, float v1, float v2, float v3) => ((delegate* unmanaged[Cdecl]<int, float, float, float, float, void>)s_glUniform4f)(location, v0, v1, v2, v3);

	private static void* s_glUniform4fv;
	public static void glUniform4fv(int location, int count, float* value) => ((delegate* unmanaged[Cdecl]<int, int, float*, void>)s_glUniform4fv)(location, count, value);

	private static void* s_glUniform4i;
	public static void glUniform4i(int location, int v0, int v1, int v2, int v3) => ((delegate* unmanaged[Cdecl]<int, int, int, int, int, void>)s_glUniform4i)(location, v0, v1, v2, v3);

	private static void* s_glUniform4iv;
	public static void glUniform4iv(int location, int count, int* value) => ((delegate* unmanaged[Cdecl]<int, int, int*, void>)s_glUniform4iv)(location, count, value);

	private static void* s_glUniform4ui;
	public static void glUniform4ui(int location, uint v0, uint v1, uint v2, uint v3) => ((delegate* unmanaged[Cdecl]<int, uint, uint, uint, uint, void>)s_glUniform4ui)(location, v0, v1, v2, v3);

	private static void* s_glUniform4uiv;
	public static void glUniform4uiv(int location, int count, uint* value) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glUniform4uiv)(location, count, value);

	private static void* s_glUniformBlockBinding;
	public static void glUniformBlockBinding(uint program, uint uniformBlockIndex, uint uniformBlockBinding) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glUniformBlockBinding)(program, uniformBlockIndex, uniformBlockBinding);

	private static void* s_glUniformMatrix2dv;
	public static void glUniformMatrix2dv(int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, double*, void>)s_glUniformMatrix2dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix2fv;
	public static void glUniformMatrix2fv(int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, float*, void>)s_glUniformMatrix2fv)(location, count, transpose, value);

	private static void* s_glUniformMatrix2x3dv;
	public static void glUniformMatrix2x3dv(int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, double*, void>)s_glUniformMatrix2x3dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix2x3fv;
	public static void glUniformMatrix2x3fv(int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, float*, void>)s_glUniformMatrix2x3fv)(location, count, transpose, value);

	private static void* s_glUniformMatrix2x4dv;
	public static void glUniformMatrix2x4dv(int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, double*, void>)s_glUniformMatrix2x4dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix2x4fv;
	public static void glUniformMatrix2x4fv(int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, float*, void>)s_glUniformMatrix2x4fv)(location, count, transpose, value);

	private static void* s_glUniformMatrix3dv;
	public static void glUniformMatrix3dv(int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, double*, void>)s_glUniformMatrix3dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix3fv;
	public static void glUniformMatrix3fv(int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, float*, void>)s_glUniformMatrix3fv)(location, count, transpose, value);

	private static void* s_glUniformMatrix3x2dv;
	public static void glUniformMatrix3x2dv(int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, double*, void>)s_glUniformMatrix3x2dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix3x2fv;
	public static void glUniformMatrix3x2fv(int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, float*, void>)s_glUniformMatrix3x2fv)(location, count, transpose, value);

	private static void* s_glUniformMatrix3x4dv;
	public static void glUniformMatrix3x4dv(int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, double*, void>)s_glUniformMatrix3x4dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix3x4fv;
	public static void glUniformMatrix3x4fv(int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, float*, void>)s_glUniformMatrix3x4fv)(location, count, transpose, value);

	private static void* s_glUniformMatrix4dv;
	public static void glUniformMatrix4dv(int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, double*, void>)s_glUniformMatrix4dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix4fv;
	public static void glUniformMatrix4fv(int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, float*, void>)s_glUniformMatrix4fv)(location, count, transpose, value);

	private static void* s_glUniformMatrix4x2dv;
	public static void glUniformMatrix4x2dv(int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, double*, void>)s_glUniformMatrix4x2dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix4x2fv;
	public static void glUniformMatrix4x2fv(int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, float*, void>)s_glUniformMatrix4x2fv)(location, count, transpose, value);

	private static void* s_glUniformMatrix4x3dv;
	public static void glUniformMatrix4x3dv(int location, int count, bool transpose, double* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, double*, void>)s_glUniformMatrix4x3dv)(location, count, transpose, value);

	private static void* s_glUniformMatrix4x3fv;
	public static void glUniformMatrix4x3fv(int location, int count, bool transpose, float* value) => ((delegate* unmanaged[Cdecl]<int, int, bool, float*, void>)s_glUniformMatrix4x3fv)(location, count, transpose, value);

	private static void* s_glUniformSubroutinesuiv;
	public static void glUniformSubroutinesuiv(int shadertype, int count, uint* indices) => ((delegate* unmanaged[Cdecl]<int, int, uint*, void>)s_glUniformSubroutinesuiv)(shadertype, count, indices);

	private static void* s_glUnmapBuffer;
	public static bool glUnmapBuffer(int target) => ((delegate* unmanaged[Cdecl]<int, bool>)s_glUnmapBuffer)(target);

	private static void* s_glUnmapNamedBuffer;
	public static bool glUnmapNamedBuffer(uint buffer) => ((delegate* unmanaged[Cdecl]<uint, bool>)s_glUnmapNamedBuffer)(buffer);

	private static void* s_glUseProgram;
	public static void glUseProgram(uint program) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glUseProgram)(program);

	private static void* s_glUseProgramStages;
	public static void glUseProgramStages(uint pipeline, int stages, uint program) => ((delegate* unmanaged[Cdecl]<uint, int, uint, void>)s_glUseProgramStages)(pipeline, stages, program);

	private static void* s_glValidateProgram;
	public static void glValidateProgram(uint program) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glValidateProgram)(program);

	private static void* s_glValidateProgramPipeline;
	public static void glValidateProgramPipeline(uint pipeline) => ((delegate* unmanaged[Cdecl]<uint, void>)s_glValidateProgramPipeline)(pipeline);

	private static void* s_glVertexArrayAttribBinding;
	public static void glVertexArrayAttribBinding(uint vaobj, uint attribindex, uint bindingindex) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glVertexArrayAttribBinding)(vaobj, attribindex, bindingindex);

	private static void* s_glVertexArrayAttribFormat;
	public static void glVertexArrayAttribFormat(uint vaobj, uint attribindex, int size, int type, bool normalized, uint relativeoffset) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int, bool, uint, void>)s_glVertexArrayAttribFormat)(vaobj, attribindex, size, type, normalized, relativeoffset);

	private static void* s_glVertexArrayAttribIFormat;
	public static void glVertexArrayAttribIFormat(uint vaobj, uint attribindex, int size, int type, uint relativeoffset) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int, uint, void>)s_glVertexArrayAttribIFormat)(vaobj, attribindex, size, type, relativeoffset);

	private static void* s_glVertexArrayAttribLFormat;
	public static void glVertexArrayAttribLFormat(uint vaobj, uint attribindex, int size, int type, uint relativeoffset) => ((delegate* unmanaged[Cdecl]<uint, uint, int, int, uint, void>)s_glVertexArrayAttribLFormat)(vaobj, attribindex, size, type, relativeoffset);

	private static void* s_glVertexArrayBindingDivisor;
	public static void glVertexArrayBindingDivisor(uint vaobj, uint bindingindex, uint divisor) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glVertexArrayBindingDivisor)(vaobj, bindingindex, divisor);

	private static void* s_glVertexArrayElementBuffer;
	public static void glVertexArrayElementBuffer(uint vaobj, uint buffer) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glVertexArrayElementBuffer)(vaobj, buffer);

	private static void* s_glVertexArrayVertexBuffer;
	public static void glVertexArrayVertexBuffer(uint vaobj, uint bindingindex, uint buffer, IntPtr offset, int stride) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, IntPtr, int, void>)s_glVertexArrayVertexBuffer)(vaobj, bindingindex, buffer, offset, stride);

	private static void* s_glVertexArrayVertexBuffers;
	public static void glVertexArrayVertexBuffers(uint vaobj, uint first, int count, uint* buffers, IntPtr* offsets, int* strides) => ((delegate* unmanaged[Cdecl]<uint, uint, int, uint*, IntPtr*, int*, void>)s_glVertexArrayVertexBuffers)(vaobj, first, count, buffers, offsets, strides);

	private static void* s_glVertexAttrib1d;
	public static void glVertexAttrib1d(uint index, double x) => ((delegate* unmanaged[Cdecl]<uint, double, void>)s_glVertexAttrib1d)(index, x);

	private static void* s_glVertexAttrib1dv;
	public static void glVertexAttrib1dv(uint index, double* v) => ((delegate* unmanaged[Cdecl]<uint, double*, void>)s_glVertexAttrib1dv)(index, v);

	private static void* s_glVertexAttrib1f;
	public static void glVertexAttrib1f(uint index, float x) => ((delegate* unmanaged[Cdecl]<uint, float, void>)s_glVertexAttrib1f)(index, x);

	private static void* s_glVertexAttrib1fv;
	public static void glVertexAttrib1fv(uint index, float* v) => ((delegate* unmanaged[Cdecl]<uint, float*, void>)s_glVertexAttrib1fv)(index, v);

	private static void* s_glVertexAttrib1s;
	public static void glVertexAttrib1s(uint index, short x) => ((delegate* unmanaged[Cdecl]<uint, short, void>)s_glVertexAttrib1s)(index, x);

	private static void* s_glVertexAttrib1sv;
	public static void glVertexAttrib1sv(uint index, short* v) => ((delegate* unmanaged[Cdecl]<uint, short*, void>)s_glVertexAttrib1sv)(index, v);

	private static void* s_glVertexAttrib2d;
	public static void glVertexAttrib2d(uint index, double x, double y) => ((delegate* unmanaged[Cdecl]<uint, double, double, void>)s_glVertexAttrib2d)(index, x, y);

	private static void* s_glVertexAttrib2dv;
	public static void glVertexAttrib2dv(uint index, double* v) => ((delegate* unmanaged[Cdecl]<uint, double*, void>)s_glVertexAttrib2dv)(index, v);

	private static void* s_glVertexAttrib2f;
	public static void glVertexAttrib2f(uint index, float x, float y) => ((delegate* unmanaged[Cdecl]<uint, float, float, void>)s_glVertexAttrib2f)(index, x, y);

	private static void* s_glVertexAttrib2fv;
	public static void glVertexAttrib2fv(uint index, float* v) => ((delegate* unmanaged[Cdecl]<uint, float*, void>)s_glVertexAttrib2fv)(index, v);

	private static void* s_glVertexAttrib2s;
	public static void glVertexAttrib2s(uint index, short x, short y) => ((delegate* unmanaged[Cdecl]<uint, short, short, void>)s_glVertexAttrib2s)(index, x, y);

	private static void* s_glVertexAttrib2sv;
	public static void glVertexAttrib2sv(uint index, short* v) => ((delegate* unmanaged[Cdecl]<uint, short*, void>)s_glVertexAttrib2sv)(index, v);

	private static void* s_glVertexAttrib3d;
	public static void glVertexAttrib3d(uint index, double x, double y, double z) => ((delegate* unmanaged[Cdecl]<uint, double, double, double, void>)s_glVertexAttrib3d)(index, x, y, z);

	private static void* s_glVertexAttrib3dv;
	public static void glVertexAttrib3dv(uint index, double* v) => ((delegate* unmanaged[Cdecl]<uint, double*, void>)s_glVertexAttrib3dv)(index, v);

	private static void* s_glVertexAttrib3f;
	public static void glVertexAttrib3f(uint index, float x, float y, float z) => ((delegate* unmanaged[Cdecl]<uint, float, float, float, void>)s_glVertexAttrib3f)(index, x, y, z);

	private static void* s_glVertexAttrib3fv;
	public static void glVertexAttrib3fv(uint index, float* v) => ((delegate* unmanaged[Cdecl]<uint, float*, void>)s_glVertexAttrib3fv)(index, v);

	private static void* s_glVertexAttrib3s;
	public static void glVertexAttrib3s(uint index, short x, short y, short z) => ((delegate* unmanaged[Cdecl]<uint, short, short, short, void>)s_glVertexAttrib3s)(index, x, y, z);

	private static void* s_glVertexAttrib3sv;
	public static void glVertexAttrib3sv(uint index, short* v) => ((delegate* unmanaged[Cdecl]<uint, short*, void>)s_glVertexAttrib3sv)(index, v);

	private static void* s_glVertexAttrib4Nbv;
	public static void glVertexAttrib4Nbv(uint index, sbyte* v) => ((delegate* unmanaged[Cdecl]<uint, sbyte*, void>)s_glVertexAttrib4Nbv)(index, v);

	private static void* s_glVertexAttrib4Niv;
	public static void glVertexAttrib4Niv(uint index, int* v) => ((delegate* unmanaged[Cdecl]<uint, int*, void>)s_glVertexAttrib4Niv)(index, v);

	private static void* s_glVertexAttrib4Nsv;
	public static void glVertexAttrib4Nsv(uint index, short* v) => ((delegate* unmanaged[Cdecl]<uint, short*, void>)s_glVertexAttrib4Nsv)(index, v);

	private static void* s_glVertexAttrib4Nub;
	public static void glVertexAttrib4Nub(uint index, byte x, byte y, byte z, byte w) => ((delegate* unmanaged[Cdecl]<uint, byte, byte, byte, byte, void>)s_glVertexAttrib4Nub)(index, x, y, z, w);

	private static void* s_glVertexAttrib4Nubv;
	public static void glVertexAttrib4Nubv(uint index, byte* v) => ((delegate* unmanaged[Cdecl]<uint, byte*, void>)s_glVertexAttrib4Nubv)(index, v);

	private static void* s_glVertexAttrib4Nuiv;
	public static void glVertexAttrib4Nuiv(uint index, uint* v) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glVertexAttrib4Nuiv)(index, v);

	private static void* s_glVertexAttrib4Nusv;
	public static void glVertexAttrib4Nusv(uint index, ushort* v) => ((delegate* unmanaged[Cdecl]<uint, ushort*, void>)s_glVertexAttrib4Nusv)(index, v);

	private static void* s_glVertexAttrib4bv;
	public static void glVertexAttrib4bv(uint index, sbyte* v) => ((delegate* unmanaged[Cdecl]<uint, sbyte*, void>)s_glVertexAttrib4bv)(index, v);

	private static void* s_glVertexAttrib4d;
	public static void glVertexAttrib4d(uint index, double x, double y, double z, double w) => ((delegate* unmanaged[Cdecl]<uint, double, double, double, double, void>)s_glVertexAttrib4d)(index, x, y, z, w);

	private static void* s_glVertexAttrib4dv;
	public static void glVertexAttrib4dv(uint index, double* v) => ((delegate* unmanaged[Cdecl]<uint, double*, void>)s_glVertexAttrib4dv)(index, v);

	private static void* s_glVertexAttrib4f;
	public static void glVertexAttrib4f(uint index, float x, float y, float z, float w) => ((delegate* unmanaged[Cdecl]<uint, float, float, float, float, void>)s_glVertexAttrib4f)(index, x, y, z, w);

	private static void* s_glVertexAttrib4fv;
	public static void glVertexAttrib4fv(uint index, float* v) => ((delegate* unmanaged[Cdecl]<uint, float*, void>)s_glVertexAttrib4fv)(index, v);

	private static void* s_glVertexAttrib4iv;
	public static void glVertexAttrib4iv(uint index, int* v) => ((delegate* unmanaged[Cdecl]<uint, int*, void>)s_glVertexAttrib4iv)(index, v);

	private static void* s_glVertexAttrib4s;
	public static void glVertexAttrib4s(uint index, short x, short y, short z, short w) => ((delegate* unmanaged[Cdecl]<uint, short, short, short, short, void>)s_glVertexAttrib4s)(index, x, y, z, w);

	private static void* s_glVertexAttrib4sv;
	public static void glVertexAttrib4sv(uint index, short* v) => ((delegate* unmanaged[Cdecl]<uint, short*, void>)s_glVertexAttrib4sv)(index, v);

	private static void* s_glVertexAttrib4ubv;
	public static void glVertexAttrib4ubv(uint index, byte* v) => ((delegate* unmanaged[Cdecl]<uint, byte*, void>)s_glVertexAttrib4ubv)(index, v);

	private static void* s_glVertexAttrib4uiv;
	public static void glVertexAttrib4uiv(uint index, uint* v) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glVertexAttrib4uiv)(index, v);

	private static void* s_glVertexAttrib4usv;
	public static void glVertexAttrib4usv(uint index, ushort* v) => ((delegate* unmanaged[Cdecl]<uint, ushort*, void>)s_glVertexAttrib4usv)(index, v);

	private static void* s_glVertexAttribBinding;
	public static void glVertexAttribBinding(uint attribindex, uint bindingindex) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glVertexAttribBinding)(attribindex, bindingindex);

	private static void* s_glVertexAttribDivisor;
	public static void glVertexAttribDivisor(uint index, uint divisor) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glVertexAttribDivisor)(index, divisor);

	private static void* s_glVertexAttribFormat;
	public static void glVertexAttribFormat(uint attribindex, int size, int type, bool normalized, uint relativeoffset) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, uint, void>)s_glVertexAttribFormat)(attribindex, size, type, normalized, relativeoffset);

	private static void* s_glVertexAttribI1i;
	public static void glVertexAttribI1i(uint index, int x) => ((delegate* unmanaged[Cdecl]<uint, int, void>)s_glVertexAttribI1i)(index, x);

	private static void* s_glVertexAttribI1iv;
	public static void glVertexAttribI1iv(uint index, int* v) => ((delegate* unmanaged[Cdecl]<uint, int*, void>)s_glVertexAttribI1iv)(index, v);

	private static void* s_glVertexAttribI1ui;
	public static void glVertexAttribI1ui(uint index, uint x) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glVertexAttribI1ui)(index, x);

	private static void* s_glVertexAttribI1uiv;
	public static void glVertexAttribI1uiv(uint index, uint* v) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glVertexAttribI1uiv)(index, v);

	private static void* s_glVertexAttribI2i;
	public static void glVertexAttribI2i(uint index, int x, int y) => ((delegate* unmanaged[Cdecl]<uint, int, int, void>)s_glVertexAttribI2i)(index, x, y);

	private static void* s_glVertexAttribI2iv;
	public static void glVertexAttribI2iv(uint index, int* v) => ((delegate* unmanaged[Cdecl]<uint, int*, void>)s_glVertexAttribI2iv)(index, v);

	private static void* s_glVertexAttribI2ui;
	public static void glVertexAttribI2ui(uint index, uint x, uint y) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, void>)s_glVertexAttribI2ui)(index, x, y);

	private static void* s_glVertexAttribI2uiv;
	public static void glVertexAttribI2uiv(uint index, uint* v) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glVertexAttribI2uiv)(index, v);

	private static void* s_glVertexAttribI3i;
	public static void glVertexAttribI3i(uint index, int x, int y, int z) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, void>)s_glVertexAttribI3i)(index, x, y, z);

	private static void* s_glVertexAttribI3iv;
	public static void glVertexAttribI3iv(uint index, int* v) => ((delegate* unmanaged[Cdecl]<uint, int*, void>)s_glVertexAttribI3iv)(index, v);

	private static void* s_glVertexAttribI3ui;
	public static void glVertexAttribI3ui(uint index, uint x, uint y, uint z) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, uint, void>)s_glVertexAttribI3ui)(index, x, y, z);

	private static void* s_glVertexAttribI3uiv;
	public static void glVertexAttribI3uiv(uint index, uint* v) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glVertexAttribI3uiv)(index, v);

	private static void* s_glVertexAttribI4bv;
	public static void glVertexAttribI4bv(uint index, sbyte* v) => ((delegate* unmanaged[Cdecl]<uint, sbyte*, void>)s_glVertexAttribI4bv)(index, v);

	private static void* s_glVertexAttribI4i;
	public static void glVertexAttribI4i(uint index, int x, int y, int z, int w) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, int, void>)s_glVertexAttribI4i)(index, x, y, z, w);

	private static void* s_glVertexAttribI4iv;
	public static void glVertexAttribI4iv(uint index, int* v) => ((delegate* unmanaged[Cdecl]<uint, int*, void>)s_glVertexAttribI4iv)(index, v);

	private static void* s_glVertexAttribI4sv;
	public static void glVertexAttribI4sv(uint index, short* v) => ((delegate* unmanaged[Cdecl]<uint, short*, void>)s_glVertexAttribI4sv)(index, v);

	private static void* s_glVertexAttribI4ubv;
	public static void glVertexAttribI4ubv(uint index, byte* v) => ((delegate* unmanaged[Cdecl]<uint, byte*, void>)s_glVertexAttribI4ubv)(index, v);

	private static void* s_glVertexAttribI4ui;
	public static void glVertexAttribI4ui(uint index, uint x, uint y, uint z, uint w) => ((delegate* unmanaged[Cdecl]<uint, uint, uint, uint, uint, void>)s_glVertexAttribI4ui)(index, x, y, z, w);

	private static void* s_glVertexAttribI4uiv;
	public static void glVertexAttribI4uiv(uint index, uint* v) => ((delegate* unmanaged[Cdecl]<uint, uint*, void>)s_glVertexAttribI4uiv)(index, v);

	private static void* s_glVertexAttribI4usv;
	public static void glVertexAttribI4usv(uint index, ushort* v) => ((delegate* unmanaged[Cdecl]<uint, ushort*, void>)s_glVertexAttribI4usv)(index, v);

	private static void* s_glVertexAttribIFormat;
	public static void glVertexAttribIFormat(uint attribindex, int size, int type, uint relativeoffset) => ((delegate* unmanaged[Cdecl]<uint, int, int, uint, void>)s_glVertexAttribIFormat)(attribindex, size, type, relativeoffset);

	private static void* s_glVertexAttribIPointer;
	public static void glVertexAttribIPointer(uint index, int size, int type, int stride, void* pointer) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, void*, void>)s_glVertexAttribIPointer)(index, size, type, stride, pointer);

	private static void* s_glVertexAttribL1d;
	public static void glVertexAttribL1d(uint index, double x) => ((delegate* unmanaged[Cdecl]<uint, double, void>)s_glVertexAttribL1d)(index, x);

	private static void* s_glVertexAttribL1dv;
	public static void glVertexAttribL1dv(uint index, double* v) => ((delegate* unmanaged[Cdecl]<uint, double*, void>)s_glVertexAttribL1dv)(index, v);

	private static void* s_glVertexAttribL2d;
	public static void glVertexAttribL2d(uint index, double x, double y) => ((delegate* unmanaged[Cdecl]<uint, double, double, void>)s_glVertexAttribL2d)(index, x, y);

	private static void* s_glVertexAttribL2dv;
	public static void glVertexAttribL2dv(uint index, double* v) => ((delegate* unmanaged[Cdecl]<uint, double*, void>)s_glVertexAttribL2dv)(index, v);

	private static void* s_glVertexAttribL3d;
	public static void glVertexAttribL3d(uint index, double x, double y, double z) => ((delegate* unmanaged[Cdecl]<uint, double, double, double, void>)s_glVertexAttribL3d)(index, x, y, z);

	private static void* s_glVertexAttribL3dv;
	public static void glVertexAttribL3dv(uint index, double* v) => ((delegate* unmanaged[Cdecl]<uint, double*, void>)s_glVertexAttribL3dv)(index, v);

	private static void* s_glVertexAttribL4d;
	public static void glVertexAttribL4d(uint index, double x, double y, double z, double w) => ((delegate* unmanaged[Cdecl]<uint, double, double, double, double, void>)s_glVertexAttribL4d)(index, x, y, z, w);

	private static void* s_glVertexAttribL4dv;
	public static void glVertexAttribL4dv(uint index, double* v) => ((delegate* unmanaged[Cdecl]<uint, double*, void>)s_glVertexAttribL4dv)(index, v);

	private static void* s_glVertexAttribLFormat;
	public static void glVertexAttribLFormat(uint attribindex, int size, int type, uint relativeoffset) => ((delegate* unmanaged[Cdecl]<uint, int, int, uint, void>)s_glVertexAttribLFormat)(attribindex, size, type, relativeoffset);

	private static void* s_glVertexAttribLPointer;
	public static void glVertexAttribLPointer(uint index, int size, int type, int stride, void* pointer) => ((delegate* unmanaged[Cdecl]<uint, int, int, int, void*, void>)s_glVertexAttribLPointer)(index, size, type, stride, pointer);

	private static void* s_glVertexAttribP1ui;
	public static void glVertexAttribP1ui(uint index, int type, bool normalized, uint value) => ((delegate* unmanaged[Cdecl]<uint, int, bool, uint, void>)s_glVertexAttribP1ui)(index, type, normalized, value);

	private static void* s_glVertexAttribP1uiv;
	public static void glVertexAttribP1uiv(uint index, int type, bool normalized, uint* value) => ((delegate* unmanaged[Cdecl]<uint, int, bool, uint*, void>)s_glVertexAttribP1uiv)(index, type, normalized, value);

	private static void* s_glVertexAttribP2ui;
	public static void glVertexAttribP2ui(uint index, int type, bool normalized, uint value) => ((delegate* unmanaged[Cdecl]<uint, int, bool, uint, void>)s_glVertexAttribP2ui)(index, type, normalized, value);

	private static void* s_glVertexAttribP2uiv;
	public static void glVertexAttribP2uiv(uint index, int type, bool normalized, uint* value) => ((delegate* unmanaged[Cdecl]<uint, int, bool, uint*, void>)s_glVertexAttribP2uiv)(index, type, normalized, value);

	private static void* s_glVertexAttribP3ui;
	public static void glVertexAttribP3ui(uint index, int type, bool normalized, uint value) => ((delegate* unmanaged[Cdecl]<uint, int, bool, uint, void>)s_glVertexAttribP3ui)(index, type, normalized, value);

	private static void* s_glVertexAttribP3uiv;
	public static void glVertexAttribP3uiv(uint index, int type, bool normalized, uint* value) => ((delegate* unmanaged[Cdecl]<uint, int, bool, uint*, void>)s_glVertexAttribP3uiv)(index, type, normalized, value);

	private static void* s_glVertexAttribP4ui;
	public static void glVertexAttribP4ui(uint index, int type, bool normalized, uint value) => ((delegate* unmanaged[Cdecl]<uint, int, bool, uint, void>)s_glVertexAttribP4ui)(index, type, normalized, value);

	private static void* s_glVertexAttribP4uiv;
	public static void glVertexAttribP4uiv(uint index, int type, bool normalized, uint* value) => ((delegate* unmanaged[Cdecl]<uint, int, bool, uint*, void>)s_glVertexAttribP4uiv)(index, type, normalized, value);

	private static void* s_glVertexAttribPointer;
	public static void glVertexAttribPointer(uint index, int size, int type, bool normalized, int stride, void* pointer) => ((delegate* unmanaged[Cdecl]<uint, int, int, bool, int, void*, void>)s_glVertexAttribPointer)(index, size, type, normalized, stride, pointer);

	private static void* s_glVertexBindingDivisor;
	public static void glVertexBindingDivisor(uint bindingindex, uint divisor) => ((delegate* unmanaged[Cdecl]<uint, uint, void>)s_glVertexBindingDivisor)(bindingindex, divisor);

	private static void* s_glVertexP2ui;
	public static void glVertexP2ui(int type, uint value) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glVertexP2ui)(type, value);

	private static void* s_glVertexP2uiv;
	public static void glVertexP2uiv(int type, uint* value) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glVertexP2uiv)(type, value);

	private static void* s_glVertexP3ui;
	public static void glVertexP3ui(int type, uint value) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glVertexP3ui)(type, value);

	private static void* s_glVertexP3uiv;
	public static void glVertexP3uiv(int type, uint* value) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glVertexP3uiv)(type, value);

	private static void* s_glVertexP4ui;
	public static void glVertexP4ui(int type, uint value) => ((delegate* unmanaged[Cdecl]<int, uint, void>)s_glVertexP4ui)(type, value);

	private static void* s_glVertexP4uiv;
	public static void glVertexP4uiv(int type, uint* value) => ((delegate* unmanaged[Cdecl]<int, uint*, void>)s_glVertexP4uiv)(type, value);

	private static void* s_glViewport;
	public static void glViewport(int x, int y, int width, int height) => ((delegate* unmanaged[Cdecl]<int, int, int, int, void>)s_glViewport)(x, y, width, height);

	private static void* s_glViewportArrayv;
	public static void glViewportArrayv(uint first, int count, float* v) => ((delegate* unmanaged[Cdecl]<uint, int, float*, void>)s_glViewportArrayv)(first, count, v);

	private static void* s_glViewportIndexedf;
	public static void glViewportIndexedf(uint index, float x, float y, float w, float h) => ((delegate* unmanaged[Cdecl]<uint, float, float, float, float, void>)s_glViewportIndexedf)(index, x, y, w, h);

	private static void* s_glViewportIndexedfv;
	public static void glViewportIndexedfv(uint index, float* v) => ((delegate* unmanaged[Cdecl]<uint, float*, void>)s_glViewportIndexedfv)(index, v);

	private static void* s_glWaitSync;
	public static void glWaitSync(IntPtr sync, int flags, ulong timeout) => ((delegate* unmanaged[Cdecl]<IntPtr, int, ulong, void>)s_glWaitSync)(sync, flags, timeout);

	public static void Import(GetProcAddressDelegate getProcAddress)
	{
		s_glActiveShaderProgram = (void*)getProcAddress("glActiveShaderProgram");
		s_glActiveTexture = (void*)getProcAddress("glActiveTexture");
		s_glAttachShader = (void*)getProcAddress("glAttachShader");
		s_glBeginConditionalRender = (void*)getProcAddress("glBeginConditionalRender");
		s_glBeginQuery = (void*)getProcAddress("glBeginQuery");
		s_glBeginQueryIndexed = (void*)getProcAddress("glBeginQueryIndexed");
		s_glBeginTransformFeedback = (void*)getProcAddress("glBeginTransformFeedback");
		s_glBindAttribLocation = (void*)getProcAddress("glBindAttribLocation");
		s_glBindBuffer = (void*)getProcAddress("glBindBuffer");
		s_glBindBufferBase = (void*)getProcAddress("glBindBufferBase");
		s_glBindBufferRange = (void*)getProcAddress("glBindBufferRange");
		s_glBindBuffersBase = (void*)getProcAddress("glBindBuffersBase");
		s_glBindBuffersRange = (void*)getProcAddress("glBindBuffersRange");
		s_glBindFragDataLocation = (void*)getProcAddress("glBindFragDataLocation");
		s_glBindFragDataLocationIndexed = (void*)getProcAddress("glBindFragDataLocationIndexed");
		s_glBindFramebuffer = (void*)getProcAddress("glBindFramebuffer");
		s_glBindImageTexture = (void*)getProcAddress("glBindImageTexture");
		s_glBindImageTextures = (void*)getProcAddress("glBindImageTextures");
		s_glBindProgramPipeline = (void*)getProcAddress("glBindProgramPipeline");
		s_glBindRenderbuffer = (void*)getProcAddress("glBindRenderbuffer");
		s_glBindSampler = (void*)getProcAddress("glBindSampler");
		s_glBindSamplers = (void*)getProcAddress("glBindSamplers");
		s_glBindTexture = (void*)getProcAddress("glBindTexture");
		s_glBindTextureUnit = (void*)getProcAddress("glBindTextureUnit");
		s_glBindTextures = (void*)getProcAddress("glBindTextures");
		s_glBindTransformFeedback = (void*)getProcAddress("glBindTransformFeedback");
		s_glBindVertexArray = (void*)getProcAddress("glBindVertexArray");
		s_glBindVertexBuffer = (void*)getProcAddress("glBindVertexBuffer");
		s_glBindVertexBuffers = (void*)getProcAddress("glBindVertexBuffers");
		s_glBlendColor = (void*)getProcAddress("glBlendColor");
		s_glBlendEquation = (void*)getProcAddress("glBlendEquation");
		s_glBlendEquationSeparate = (void*)getProcAddress("glBlendEquationSeparate");
		s_glBlendEquationSeparatei = (void*)getProcAddress("glBlendEquationSeparatei");
		s_glBlendEquationi = (void*)getProcAddress("glBlendEquationi");
		s_glBlendFunc = (void*)getProcAddress("glBlendFunc");
		s_glBlendFuncSeparate = (void*)getProcAddress("glBlendFuncSeparate");
		s_glBlendFuncSeparatei = (void*)getProcAddress("glBlendFuncSeparatei");
		s_glBlendFunci = (void*)getProcAddress("glBlendFunci");
		s_glBlitFramebuffer = (void*)getProcAddress("glBlitFramebuffer");
		s_glBlitNamedFramebuffer = (void*)getProcAddress("glBlitNamedFramebuffer");
		s_glBufferData = (void*)getProcAddress("glBufferData");
		s_glBufferStorage = (void*)getProcAddress("glBufferStorage");
		s_glBufferSubData = (void*)getProcAddress("glBufferSubData");
		s_glCheckFramebufferStatus = (void*)getProcAddress("glCheckFramebufferStatus");
		s_glCheckNamedFramebufferStatus = (void*)getProcAddress("glCheckNamedFramebufferStatus");
		s_glClampColor = (void*)getProcAddress("glClampColor");
		s_glClear = (void*)getProcAddress("glClear");
		s_glClearBufferData = (void*)getProcAddress("glClearBufferData");
		s_glClearBufferSubData = (void*)getProcAddress("glClearBufferSubData");
		s_glClearBufferfi = (void*)getProcAddress("glClearBufferfi");
		s_glClearBufferfv = (void*)getProcAddress("glClearBufferfv");
		s_glClearBufferiv = (void*)getProcAddress("glClearBufferiv");
		s_glClearBufferuiv = (void*)getProcAddress("glClearBufferuiv");
		s_glClearColor = (void*)getProcAddress("glClearColor");
		s_glClearDepth = (void*)getProcAddress("glClearDepth");
		s_glClearDepthf = (void*)getProcAddress("glClearDepthf");
		s_glClearNamedBufferData = (void*)getProcAddress("glClearNamedBufferData");
		s_glClearNamedBufferSubData = (void*)getProcAddress("glClearNamedBufferSubData");
		s_glClearNamedFramebufferfi = (void*)getProcAddress("glClearNamedFramebufferfi");
		s_glClearNamedFramebufferfv = (void*)getProcAddress("glClearNamedFramebufferfv");
		s_glClearNamedFramebufferiv = (void*)getProcAddress("glClearNamedFramebufferiv");
		s_glClearNamedFramebufferuiv = (void*)getProcAddress("glClearNamedFramebufferuiv");
		s_glClearStencil = (void*)getProcAddress("glClearStencil");
		s_glClearTexImage = (void*)getProcAddress("glClearTexImage");
		s_glClearTexSubImage = (void*)getProcAddress("glClearTexSubImage");
		s_glClientWaitSync = (void*)getProcAddress("glClientWaitSync");
		s_glClipControl = (void*)getProcAddress("glClipControl");
		s_glColorMask = (void*)getProcAddress("glColorMask");
		s_glColorMaski = (void*)getProcAddress("glColorMaski");
		s_glColorP3ui = (void*)getProcAddress("glColorP3ui");
		s_glColorP3uiv = (void*)getProcAddress("glColorP3uiv");
		s_glColorP4ui = (void*)getProcAddress("glColorP4ui");
		s_glColorP4uiv = (void*)getProcAddress("glColorP4uiv");
		s_glCompileShader = (void*)getProcAddress("glCompileShader");
		s_glCompressedTexImage1D = (void*)getProcAddress("glCompressedTexImage1D");
		s_glCompressedTexImage2D = (void*)getProcAddress("glCompressedTexImage2D");
		s_glCompressedTexImage3D = (void*)getProcAddress("glCompressedTexImage3D");
		s_glCompressedTexSubImage1D = (void*)getProcAddress("glCompressedTexSubImage1D");
		s_glCompressedTexSubImage2D = (void*)getProcAddress("glCompressedTexSubImage2D");
		s_glCompressedTexSubImage3D = (void*)getProcAddress("glCompressedTexSubImage3D");
		s_glCompressedTextureSubImage1D = (void*)getProcAddress("glCompressedTextureSubImage1D");
		s_glCompressedTextureSubImage2D = (void*)getProcAddress("glCompressedTextureSubImage2D");
		s_glCompressedTextureSubImage3D = (void*)getProcAddress("glCompressedTextureSubImage3D");
		s_glCopyBufferSubData = (void*)getProcAddress("glCopyBufferSubData");
		s_glCopyImageSubData = (void*)getProcAddress("glCopyImageSubData");
		s_glCopyNamedBufferSubData = (void*)getProcAddress("glCopyNamedBufferSubData");
		s_glCopyTexImage1D = (void*)getProcAddress("glCopyTexImage1D");
		s_glCopyTexImage2D = (void*)getProcAddress("glCopyTexImage2D");
		s_glCopyTexSubImage1D = (void*)getProcAddress("glCopyTexSubImage1D");
		s_glCopyTexSubImage2D = (void*)getProcAddress("glCopyTexSubImage2D");
		s_glCopyTexSubImage3D = (void*)getProcAddress("glCopyTexSubImage3D");
		s_glCopyTextureSubImage1D = (void*)getProcAddress("glCopyTextureSubImage1D");
		s_glCopyTextureSubImage2D = (void*)getProcAddress("glCopyTextureSubImage2D");
		s_glCopyTextureSubImage3D = (void*)getProcAddress("glCopyTextureSubImage3D");
		s_glCreateBuffers = (void*)getProcAddress("glCreateBuffers");
		s_glCreateFramebuffers = (void*)getProcAddress("glCreateFramebuffers");
		s_glCreateProgram = (void*)getProcAddress("glCreateProgram");
		s_glCreateProgramPipelines = (void*)getProcAddress("glCreateProgramPipelines");
		s_glCreateQueries = (void*)getProcAddress("glCreateQueries");
		s_glCreateRenderbuffers = (void*)getProcAddress("glCreateRenderbuffers");
		s_glCreateSamplers = (void*)getProcAddress("glCreateSamplers");
		s_glCreateShader = (void*)getProcAddress("glCreateShader");
		s_glCreateShaderProgramv = (void*)getProcAddress("glCreateShaderProgramv");
		s_glCreateTextures = (void*)getProcAddress("glCreateTextures");
		s_glCreateTransformFeedbacks = (void*)getProcAddress("glCreateTransformFeedbacks");
		s_glCreateVertexArrays = (void*)getProcAddress("glCreateVertexArrays");
		s_glCullFace = (void*)getProcAddress("glCullFace");
		s_glDebugMessageCallback = (void*)getProcAddress("glDebugMessageCallback");
		s_glDebugMessageControl = (void*)getProcAddress("glDebugMessageControl");
		s_glDebugMessageInsert = (void*)getProcAddress("glDebugMessageInsert");
		s_glDeleteBuffers = (void*)getProcAddress("glDeleteBuffers");
		s_glDeleteFramebuffers = (void*)getProcAddress("glDeleteFramebuffers");
		s_glDeleteProgram = (void*)getProcAddress("glDeleteProgram");
		s_glDeleteProgramPipelines = (void*)getProcAddress("glDeleteProgramPipelines");
		s_glDeleteQueries = (void*)getProcAddress("glDeleteQueries");
		s_glDeleteRenderbuffers = (void*)getProcAddress("glDeleteRenderbuffers");
		s_glDeleteSamplers = (void*)getProcAddress("glDeleteSamplers");
		s_glDeleteShader = (void*)getProcAddress("glDeleteShader");
		s_glDeleteSync = (void*)getProcAddress("glDeleteSync");
		s_glDeleteTextures = (void*)getProcAddress("glDeleteTextures");
		s_glDeleteTransformFeedbacks = (void*)getProcAddress("glDeleteTransformFeedbacks");
		s_glDeleteVertexArrays = (void*)getProcAddress("glDeleteVertexArrays");
		s_glDepthFunc = (void*)getProcAddress("glDepthFunc");
		s_glDepthMask = (void*)getProcAddress("glDepthMask");
		s_glDepthRange = (void*)getProcAddress("glDepthRange");
		s_glDepthRangeArrayv = (void*)getProcAddress("glDepthRangeArrayv");
		s_glDepthRangeIndexed = (void*)getProcAddress("glDepthRangeIndexed");
		s_glDepthRangef = (void*)getProcAddress("glDepthRangef");
		s_glDetachShader = (void*)getProcAddress("glDetachShader");
		s_glDisable = (void*)getProcAddress("glDisable");
		s_glDisableVertexArrayAttrib = (void*)getProcAddress("glDisableVertexArrayAttrib");
		s_glDisableVertexAttribArray = (void*)getProcAddress("glDisableVertexAttribArray");
		s_glDisablei = (void*)getProcAddress("glDisablei");
		s_glDispatchCompute = (void*)getProcAddress("glDispatchCompute");
		s_glDispatchComputeIndirect = (void*)getProcAddress("glDispatchComputeIndirect");
		s_glDrawArrays = (void*)getProcAddress("glDrawArrays");
		s_glDrawArraysIndirect = (void*)getProcAddress("glDrawArraysIndirect");
		s_glDrawArraysInstanced = (void*)getProcAddress("glDrawArraysInstanced");
		s_glDrawArraysInstancedBaseInstance = (void*)getProcAddress("glDrawArraysInstancedBaseInstance");
		s_glDrawBuffer = (void*)getProcAddress("glDrawBuffer");
		s_glDrawBuffers = (void*)getProcAddress("glDrawBuffers");
		s_glDrawElements = (void*)getProcAddress("glDrawElements");
		s_glDrawElementsBaseVertex = (void*)getProcAddress("glDrawElementsBaseVertex");
		s_glDrawElementsIndirect = (void*)getProcAddress("glDrawElementsIndirect");
		s_glDrawElementsInstanced = (void*)getProcAddress("glDrawElementsInstanced");
		s_glDrawElementsInstancedBaseInstance = (void*)getProcAddress("glDrawElementsInstancedBaseInstance");
		s_glDrawElementsInstancedBaseVertex = (void*)getProcAddress("glDrawElementsInstancedBaseVertex");
		s_glDrawElementsInstancedBaseVertexBaseInstance = (void*)getProcAddress("glDrawElementsInstancedBaseVertexBaseInstance");
		s_glDrawRangeElements = (void*)getProcAddress("glDrawRangeElements");
		s_glDrawRangeElementsBaseVertex = (void*)getProcAddress("glDrawRangeElementsBaseVertex");
		s_glDrawTransformFeedback = (void*)getProcAddress("glDrawTransformFeedback");
		s_glDrawTransformFeedbackInstanced = (void*)getProcAddress("glDrawTransformFeedbackInstanced");
		s_glDrawTransformFeedbackStream = (void*)getProcAddress("glDrawTransformFeedbackStream");
		s_glDrawTransformFeedbackStreamInstanced = (void*)getProcAddress("glDrawTransformFeedbackStreamInstanced");
		s_glEnable = (void*)getProcAddress("glEnable");
		s_glEnableVertexArrayAttrib = (void*)getProcAddress("glEnableVertexArrayAttrib");
		s_glEnableVertexAttribArray = (void*)getProcAddress("glEnableVertexAttribArray");
		s_glEnablei = (void*)getProcAddress("glEnablei");
		s_glEndConditionalRender = (void*)getProcAddress("glEndConditionalRender");
		s_glEndQuery = (void*)getProcAddress("glEndQuery");
		s_glEndQueryIndexed = (void*)getProcAddress("glEndQueryIndexed");
		s_glEndTransformFeedback = (void*)getProcAddress("glEndTransformFeedback");
		s_glFenceSync = (void*)getProcAddress("glFenceSync");
		s_glFinish = (void*)getProcAddress("glFinish");
		s_glFlush = (void*)getProcAddress("glFlush");
		s_glFlushMappedBufferRange = (void*)getProcAddress("glFlushMappedBufferRange");
		s_glFlushMappedNamedBufferRange = (void*)getProcAddress("glFlushMappedNamedBufferRange");
		s_glFramebufferParameteri = (void*)getProcAddress("glFramebufferParameteri");
		s_glFramebufferRenderbuffer = (void*)getProcAddress("glFramebufferRenderbuffer");
		s_glFramebufferTexture = (void*)getProcAddress("glFramebufferTexture");
		s_glFramebufferTexture1D = (void*)getProcAddress("glFramebufferTexture1D");
		s_glFramebufferTexture2D = (void*)getProcAddress("glFramebufferTexture2D");
		s_glFramebufferTexture3D = (void*)getProcAddress("glFramebufferTexture3D");
		s_glFramebufferTextureLayer = (void*)getProcAddress("glFramebufferTextureLayer");
		s_glFrontFace = (void*)getProcAddress("glFrontFace");
		s_glGenBuffers = (void*)getProcAddress("glGenBuffers");
		s_glGenFramebuffers = (void*)getProcAddress("glGenFramebuffers");
		s_glGenProgramPipelines = (void*)getProcAddress("glGenProgramPipelines");
		s_glGenQueries = (void*)getProcAddress("glGenQueries");
		s_glGenRenderbuffers = (void*)getProcAddress("glGenRenderbuffers");
		s_glGenSamplers = (void*)getProcAddress("glGenSamplers");
		s_glGenTextures = (void*)getProcAddress("glGenTextures");
		s_glGenTransformFeedbacks = (void*)getProcAddress("glGenTransformFeedbacks");
		s_glGenVertexArrays = (void*)getProcAddress("glGenVertexArrays");
		s_glGenerateMipmap = (void*)getProcAddress("glGenerateMipmap");
		s_glGenerateTextureMipmap = (void*)getProcAddress("glGenerateTextureMipmap");
		s_glGetActiveAtomicCounterBufferiv = (void*)getProcAddress("glGetActiveAtomicCounterBufferiv");
		s_glGetActiveAttrib = (void*)getProcAddress("glGetActiveAttrib");
		s_glGetActiveSubroutineName = (void*)getProcAddress("glGetActiveSubroutineName");
		s_glGetActiveSubroutineUniformName = (void*)getProcAddress("glGetActiveSubroutineUniformName");
		s_glGetActiveSubroutineUniformiv = (void*)getProcAddress("glGetActiveSubroutineUniformiv");
		s_glGetActiveUniform = (void*)getProcAddress("glGetActiveUniform");
		s_glGetActiveUniformBlockName = (void*)getProcAddress("glGetActiveUniformBlockName");
		s_glGetActiveUniformBlockiv = (void*)getProcAddress("glGetActiveUniformBlockiv");
		s_glGetActiveUniformName = (void*)getProcAddress("glGetActiveUniformName");
		s_glGetActiveUniformsiv = (void*)getProcAddress("glGetActiveUniformsiv");
		s_glGetAttachedShaders = (void*)getProcAddress("glGetAttachedShaders");
		s_glGetAttribLocation = (void*)getProcAddress("glGetAttribLocation");
		s_glGetBooleani_v = (void*)getProcAddress("glGetBooleani_v");
		s_glGetBooleanv = (void*)getProcAddress("glGetBooleanv");
		s_glGetBufferParameteri64v = (void*)getProcAddress("glGetBufferParameteri64v");
		s_glGetBufferParameteriv = (void*)getProcAddress("glGetBufferParameteriv");
		s_glGetBufferPointerv = (void*)getProcAddress("glGetBufferPointerv");
		s_glGetBufferSubData = (void*)getProcAddress("glGetBufferSubData");
		s_glGetCompressedTexImage = (void*)getProcAddress("glGetCompressedTexImage");
		s_glGetCompressedTextureImage = (void*)getProcAddress("glGetCompressedTextureImage");
		s_glGetCompressedTextureSubImage = (void*)getProcAddress("glGetCompressedTextureSubImage");
		s_glGetDebugMessageLog = (void*)getProcAddress("glGetDebugMessageLog");
		s_glGetDoublei_v = (void*)getProcAddress("glGetDoublei_v");
		s_glGetDoublev = (void*)getProcAddress("glGetDoublev");
		s_glGetError = (void*)getProcAddress("glGetError");
		s_glGetFloati_v = (void*)getProcAddress("glGetFloati_v");
		s_glGetFloatv = (void*)getProcAddress("glGetFloatv");
		s_glGetFragDataIndex = (void*)getProcAddress("glGetFragDataIndex");
		s_glGetFragDataLocation = (void*)getProcAddress("glGetFragDataLocation");
		s_glGetFramebufferAttachmentParameteriv = (void*)getProcAddress("glGetFramebufferAttachmentParameteriv");
		s_glGetFramebufferParameteriv = (void*)getProcAddress("glGetFramebufferParameteriv");
		s_glGetGraphicsResetStatus = (void*)getProcAddress("glGetGraphicsResetStatus");
		s_glGetInteger64i_v = (void*)getProcAddress("glGetInteger64i_v");
		s_glGetInteger64v = (void*)getProcAddress("glGetInteger64v");
		s_glGetIntegeri_v = (void*)getProcAddress("glGetIntegeri_v");
		s_glGetIntegerv = (void*)getProcAddress("glGetIntegerv");
		s_glGetInternalformati64v = (void*)getProcAddress("glGetInternalformati64v");
		s_glGetInternalformativ = (void*)getProcAddress("glGetInternalformativ");
		s_glGetMultisamplefv = (void*)getProcAddress("glGetMultisamplefv");
		s_glGetNamedBufferParameteri64v = (void*)getProcAddress("glGetNamedBufferParameteri64v");
		s_glGetNamedBufferParameteriv = (void*)getProcAddress("glGetNamedBufferParameteriv");
		s_glGetNamedBufferPointerv = (void*)getProcAddress("glGetNamedBufferPointerv");
		s_glGetNamedBufferSubData = (void*)getProcAddress("glGetNamedBufferSubData");
		s_glGetNamedFramebufferAttachmentParameteriv = (void*)getProcAddress("glGetNamedFramebufferAttachmentParameteriv");
		s_glGetNamedFramebufferParameteriv = (void*)getProcAddress("glGetNamedFramebufferParameteriv");
		s_glGetNamedRenderbufferParameteriv = (void*)getProcAddress("glGetNamedRenderbufferParameteriv");
		s_glGetObjectLabel = (void*)getProcAddress("glGetObjectLabel");
		s_glGetObjectPtrLabel = (void*)getProcAddress("glGetObjectPtrLabel");
		s_glGetProgramBinary = (void*)getProcAddress("glGetProgramBinary");
		s_glGetProgramInfoLog = (void*)getProcAddress("glGetProgramInfoLog");
		s_glGetProgramInterfaceiv = (void*)getProcAddress("glGetProgramInterfaceiv");
		s_glGetProgramPipelineInfoLog = (void*)getProcAddress("glGetProgramPipelineInfoLog");
		s_glGetProgramPipelineiv = (void*)getProcAddress("glGetProgramPipelineiv");
		s_glGetProgramResourceIndex = (void*)getProcAddress("glGetProgramResourceIndex");
		s_glGetProgramResourceLocation = (void*)getProcAddress("glGetProgramResourceLocation");
		s_glGetProgramResourceLocationIndex = (void*)getProcAddress("glGetProgramResourceLocationIndex");
		s_glGetProgramResourceName = (void*)getProcAddress("glGetProgramResourceName");
		s_glGetProgramResourceiv = (void*)getProcAddress("glGetProgramResourceiv");
		s_glGetProgramStageiv = (void*)getProcAddress("glGetProgramStageiv");
		s_glGetProgramiv = (void*)getProcAddress("glGetProgramiv");
		s_glGetQueryBufferObjecti64v = (void*)getProcAddress("glGetQueryBufferObjecti64v");
		s_glGetQueryBufferObjectiv = (void*)getProcAddress("glGetQueryBufferObjectiv");
		s_glGetQueryBufferObjectui64v = (void*)getProcAddress("glGetQueryBufferObjectui64v");
		s_glGetQueryBufferObjectuiv = (void*)getProcAddress("glGetQueryBufferObjectuiv");
		s_glGetQueryIndexediv = (void*)getProcAddress("glGetQueryIndexediv");
		s_glGetQueryObjecti64v = (void*)getProcAddress("glGetQueryObjecti64v");
		s_glGetQueryObjectiv = (void*)getProcAddress("glGetQueryObjectiv");
		s_glGetQueryObjectui64v = (void*)getProcAddress("glGetQueryObjectui64v");
		s_glGetQueryObjectuiv = (void*)getProcAddress("glGetQueryObjectuiv");
		s_glGetQueryiv = (void*)getProcAddress("glGetQueryiv");
		s_glGetRenderbufferParameteriv = (void*)getProcAddress("glGetRenderbufferParameteriv");
		s_glGetSamplerParameterIiv = (void*)getProcAddress("glGetSamplerParameterIiv");
		s_glGetSamplerParameterIuiv = (void*)getProcAddress("glGetSamplerParameterIuiv");
		s_glGetSamplerParameterfv = (void*)getProcAddress("glGetSamplerParameterfv");
		s_glGetSamplerParameteriv = (void*)getProcAddress("glGetSamplerParameteriv");
		s_glGetShaderInfoLog = (void*)getProcAddress("glGetShaderInfoLog");
		s_glGetShaderPrecisionFormat = (void*)getProcAddress("glGetShaderPrecisionFormat");
		s_glGetShaderSource = (void*)getProcAddress("glGetShaderSource");
		s_glGetShaderiv = (void*)getProcAddress("glGetShaderiv");
		s_glGetString = (void*)getProcAddress("glGetString");
		s_glGetStringi = (void*)getProcAddress("glGetStringi");
		s_glGetSubroutineIndex = (void*)getProcAddress("glGetSubroutineIndex");
		s_glGetSubroutineUniformLocation = (void*)getProcAddress("glGetSubroutineUniformLocation");
		s_glGetSynciv = (void*)getProcAddress("glGetSynciv");
		s_glGetTexImage = (void*)getProcAddress("glGetTexImage");
		s_glGetTexLevelParameterfv = (void*)getProcAddress("glGetTexLevelParameterfv");
		s_glGetTexLevelParameteriv = (void*)getProcAddress("glGetTexLevelParameteriv");
		s_glGetTexParameterIiv = (void*)getProcAddress("glGetTexParameterIiv");
		s_glGetTexParameterIuiv = (void*)getProcAddress("glGetTexParameterIuiv");
		s_glGetTexParameterfv = (void*)getProcAddress("glGetTexParameterfv");
		s_glGetTexParameteriv = (void*)getProcAddress("glGetTexParameteriv");
		s_glGetTextureImage = (void*)getProcAddress("glGetTextureImage");
		s_glGetTextureLevelParameterfv = (void*)getProcAddress("glGetTextureLevelParameterfv");
		s_glGetTextureLevelParameteriv = (void*)getProcAddress("glGetTextureLevelParameteriv");
		s_glGetTextureParameterIiv = (void*)getProcAddress("glGetTextureParameterIiv");
		s_glGetTextureParameterIuiv = (void*)getProcAddress("glGetTextureParameterIuiv");
		s_glGetTextureParameterfv = (void*)getProcAddress("glGetTextureParameterfv");
		s_glGetTextureParameteriv = (void*)getProcAddress("glGetTextureParameteriv");
		s_glGetTextureSubImage = (void*)getProcAddress("glGetTextureSubImage");
		s_glGetTransformFeedbackVarying = (void*)getProcAddress("glGetTransformFeedbackVarying");
		s_glGetTransformFeedbacki64_v = (void*)getProcAddress("glGetTransformFeedbacki64_v");
		s_glGetTransformFeedbacki_v = (void*)getProcAddress("glGetTransformFeedbacki_v");
		s_glGetTransformFeedbackiv = (void*)getProcAddress("glGetTransformFeedbackiv");
		s_glGetUniformBlockIndex = (void*)getProcAddress("glGetUniformBlockIndex");
		s_glGetUniformIndices = (void*)getProcAddress("glGetUniformIndices");
		s_glGetUniformLocation = (void*)getProcAddress("glGetUniformLocation");
		s_glGetUniformSubroutineuiv = (void*)getProcAddress("glGetUniformSubroutineuiv");
		s_glGetUniformdv = (void*)getProcAddress("glGetUniformdv");
		s_glGetUniformfv = (void*)getProcAddress("glGetUniformfv");
		s_glGetUniformiv = (void*)getProcAddress("glGetUniformiv");
		s_glGetUniformuiv = (void*)getProcAddress("glGetUniformuiv");
		s_glGetVertexArrayIndexed64iv = (void*)getProcAddress("glGetVertexArrayIndexed64iv");
		s_glGetVertexArrayIndexediv = (void*)getProcAddress("glGetVertexArrayIndexediv");
		s_glGetVertexArrayiv = (void*)getProcAddress("glGetVertexArrayiv");
		s_glGetVertexAttribIiv = (void*)getProcAddress("glGetVertexAttribIiv");
		s_glGetVertexAttribIuiv = (void*)getProcAddress("glGetVertexAttribIuiv");
		s_glGetVertexAttribLdv = (void*)getProcAddress("glGetVertexAttribLdv");
		s_glGetVertexAttribPointerv = (void*)getProcAddress("glGetVertexAttribPointerv");
		s_glGetVertexAttribdv = (void*)getProcAddress("glGetVertexAttribdv");
		s_glGetVertexAttribfv = (void*)getProcAddress("glGetVertexAttribfv");
		s_glGetVertexAttribiv = (void*)getProcAddress("glGetVertexAttribiv");
		s_glGetnColorTable = (void*)getProcAddress("glGetnColorTable");
		s_glGetnCompressedTexImage = (void*)getProcAddress("glGetnCompressedTexImage");
		s_glGetnConvolutionFilter = (void*)getProcAddress("glGetnConvolutionFilter");
		s_glGetnHistogram = (void*)getProcAddress("glGetnHistogram");
		s_glGetnMapdv = (void*)getProcAddress("glGetnMapdv");
		s_glGetnMapfv = (void*)getProcAddress("glGetnMapfv");
		s_glGetnMapiv = (void*)getProcAddress("glGetnMapiv");
		s_glGetnMinmax = (void*)getProcAddress("glGetnMinmax");
		s_glGetnPixelMapfv = (void*)getProcAddress("glGetnPixelMapfv");
		s_glGetnPixelMapuiv = (void*)getProcAddress("glGetnPixelMapuiv");
		s_glGetnPixelMapusv = (void*)getProcAddress("glGetnPixelMapusv");
		s_glGetnPolygonStipple = (void*)getProcAddress("glGetnPolygonStipple");
		s_glGetnSeparableFilter = (void*)getProcAddress("glGetnSeparableFilter");
		s_glGetnTexImage = (void*)getProcAddress("glGetnTexImage");
		s_glGetnUniformdv = (void*)getProcAddress("glGetnUniformdv");
		s_glGetnUniformfv = (void*)getProcAddress("glGetnUniformfv");
		s_glGetnUniformiv = (void*)getProcAddress("glGetnUniformiv");
		s_glGetnUniformuiv = (void*)getProcAddress("glGetnUniformuiv");
		s_glHint = (void*)getProcAddress("glHint");
		s_glInvalidateBufferData = (void*)getProcAddress("glInvalidateBufferData");
		s_glInvalidateBufferSubData = (void*)getProcAddress("glInvalidateBufferSubData");
		s_glInvalidateFramebuffer = (void*)getProcAddress("glInvalidateFramebuffer");
		s_glInvalidateNamedFramebufferData = (void*)getProcAddress("glInvalidateNamedFramebufferData");
		s_glInvalidateNamedFramebufferSubData = (void*)getProcAddress("glInvalidateNamedFramebufferSubData");
		s_glInvalidateSubFramebuffer = (void*)getProcAddress("glInvalidateSubFramebuffer");
		s_glInvalidateTexImage = (void*)getProcAddress("glInvalidateTexImage");
		s_glInvalidateTexSubImage = (void*)getProcAddress("glInvalidateTexSubImage");
		s_glIsBuffer = (void*)getProcAddress("glIsBuffer");
		s_glIsEnabled = (void*)getProcAddress("glIsEnabled");
		s_glIsEnabledi = (void*)getProcAddress("glIsEnabledi");
		s_glIsFramebuffer = (void*)getProcAddress("glIsFramebuffer");
		s_glIsProgram = (void*)getProcAddress("glIsProgram");
		s_glIsProgramPipeline = (void*)getProcAddress("glIsProgramPipeline");
		s_glIsQuery = (void*)getProcAddress("glIsQuery");
		s_glIsRenderbuffer = (void*)getProcAddress("glIsRenderbuffer");
		s_glIsSampler = (void*)getProcAddress("glIsSampler");
		s_glIsShader = (void*)getProcAddress("glIsShader");
		s_glIsSync = (void*)getProcAddress("glIsSync");
		s_glIsTexture = (void*)getProcAddress("glIsTexture");
		s_glIsTransformFeedback = (void*)getProcAddress("glIsTransformFeedback");
		s_glIsVertexArray = (void*)getProcAddress("glIsVertexArray");
		s_glLineWidth = (void*)getProcAddress("glLineWidth");
		s_glLinkProgram = (void*)getProcAddress("glLinkProgram");
		s_glLogicOp = (void*)getProcAddress("glLogicOp");
		s_glMapBuffer = (void*)getProcAddress("glMapBuffer");
		s_glMapBufferRange = (void*)getProcAddress("glMapBufferRange");
		s_glMapNamedBuffer = (void*)getProcAddress("glMapNamedBuffer");
		s_glMapNamedBufferRange = (void*)getProcAddress("glMapNamedBufferRange");
		s_glMemoryBarrier = (void*)getProcAddress("glMemoryBarrier");
		s_glMemoryBarrierByRegion = (void*)getProcAddress("glMemoryBarrierByRegion");
		s_glMinSampleShading = (void*)getProcAddress("glMinSampleShading");
		s_glMultiDrawArrays = (void*)getProcAddress("glMultiDrawArrays");
		s_glMultiDrawArraysIndirect = (void*)getProcAddress("glMultiDrawArraysIndirect");
		s_glMultiDrawArraysIndirectCount = (void*)getProcAddress("glMultiDrawArraysIndirectCount");
		s_glMultiDrawElements = (void*)getProcAddress("glMultiDrawElements");
		s_glMultiDrawElementsBaseVertex = (void*)getProcAddress("glMultiDrawElementsBaseVertex");
		s_glMultiDrawElementsIndirect = (void*)getProcAddress("glMultiDrawElementsIndirect");
		s_glMultiDrawElementsIndirectCount = (void*)getProcAddress("glMultiDrawElementsIndirectCount");
		s_glMultiTexCoordP1ui = (void*)getProcAddress("glMultiTexCoordP1ui");
		s_glMultiTexCoordP1uiv = (void*)getProcAddress("glMultiTexCoordP1uiv");
		s_glMultiTexCoordP2ui = (void*)getProcAddress("glMultiTexCoordP2ui");
		s_glMultiTexCoordP2uiv = (void*)getProcAddress("glMultiTexCoordP2uiv");
		s_glMultiTexCoordP3ui = (void*)getProcAddress("glMultiTexCoordP3ui");
		s_glMultiTexCoordP3uiv = (void*)getProcAddress("glMultiTexCoordP3uiv");
		s_glMultiTexCoordP4ui = (void*)getProcAddress("glMultiTexCoordP4ui");
		s_glMultiTexCoordP4uiv = (void*)getProcAddress("glMultiTexCoordP4uiv");
		s_glNamedBufferData = (void*)getProcAddress("glNamedBufferData");
		s_glNamedBufferStorage = (void*)getProcAddress("glNamedBufferStorage");
		s_glNamedBufferSubData = (void*)getProcAddress("glNamedBufferSubData");
		s_glNamedFramebufferDrawBuffer = (void*)getProcAddress("glNamedFramebufferDrawBuffer");
		s_glNamedFramebufferDrawBuffers = (void*)getProcAddress("glNamedFramebufferDrawBuffers");
		s_glNamedFramebufferParameteri = (void*)getProcAddress("glNamedFramebufferParameteri");
		s_glNamedFramebufferReadBuffer = (void*)getProcAddress("glNamedFramebufferReadBuffer");
		s_glNamedFramebufferRenderbuffer = (void*)getProcAddress("glNamedFramebufferRenderbuffer");
		s_glNamedFramebufferTexture = (void*)getProcAddress("glNamedFramebufferTexture");
		s_glNamedFramebufferTextureLayer = (void*)getProcAddress("glNamedFramebufferTextureLayer");
		s_glNamedRenderbufferStorage = (void*)getProcAddress("glNamedRenderbufferStorage");
		s_glNamedRenderbufferStorageMultisample = (void*)getProcAddress("glNamedRenderbufferStorageMultisample");
		s_glNormalP3ui = (void*)getProcAddress("glNormalP3ui");
		s_glNormalP3uiv = (void*)getProcAddress("glNormalP3uiv");
		s_glObjectLabel = (void*)getProcAddress("glObjectLabel");
		s_glObjectPtrLabel = (void*)getProcAddress("glObjectPtrLabel");
		s_glPatchParameterfv = (void*)getProcAddress("glPatchParameterfv");
		s_glPatchParameteri = (void*)getProcAddress("glPatchParameteri");
		s_glPauseTransformFeedback = (void*)getProcAddress("glPauseTransformFeedback");
		s_glPixelStoref = (void*)getProcAddress("glPixelStoref");
		s_glPixelStorei = (void*)getProcAddress("glPixelStorei");
		s_glPointParameterf = (void*)getProcAddress("glPointParameterf");
		s_glPointParameterfv = (void*)getProcAddress("glPointParameterfv");
		s_glPointParameteri = (void*)getProcAddress("glPointParameteri");
		s_glPointParameteriv = (void*)getProcAddress("glPointParameteriv");
		s_glPointSize = (void*)getProcAddress("glPointSize");
		s_glPolygonMode = (void*)getProcAddress("glPolygonMode");
		s_glPolygonOffset = (void*)getProcAddress("glPolygonOffset");
		s_glPolygonOffsetClamp = (void*)getProcAddress("glPolygonOffsetClamp");
		s_glPopDebugGroup = (void*)getProcAddress("glPopDebugGroup");
		s_glPrimitiveRestartIndex = (void*)getProcAddress("glPrimitiveRestartIndex");
		s_glProgramBinary = (void*)getProcAddress("glProgramBinary");
		s_glProgramParameteri = (void*)getProcAddress("glProgramParameteri");
		s_glProgramUniform1d = (void*)getProcAddress("glProgramUniform1d");
		s_glProgramUniform1dv = (void*)getProcAddress("glProgramUniform1dv");
		s_glProgramUniform1f = (void*)getProcAddress("glProgramUniform1f");
		s_glProgramUniform1fv = (void*)getProcAddress("glProgramUniform1fv");
		s_glProgramUniform1i = (void*)getProcAddress("glProgramUniform1i");
		s_glProgramUniform1iv = (void*)getProcAddress("glProgramUniform1iv");
		s_glProgramUniform1ui = (void*)getProcAddress("glProgramUniform1ui");
		s_glProgramUniform1uiv = (void*)getProcAddress("glProgramUniform1uiv");
		s_glProgramUniform2d = (void*)getProcAddress("glProgramUniform2d");
		s_glProgramUniform2dv = (void*)getProcAddress("glProgramUniform2dv");
		s_glProgramUniform2f = (void*)getProcAddress("glProgramUniform2f");
		s_glProgramUniform2fv = (void*)getProcAddress("glProgramUniform2fv");
		s_glProgramUniform2i = (void*)getProcAddress("glProgramUniform2i");
		s_glProgramUniform2iv = (void*)getProcAddress("glProgramUniform2iv");
		s_glProgramUniform2ui = (void*)getProcAddress("glProgramUniform2ui");
		s_glProgramUniform2uiv = (void*)getProcAddress("glProgramUniform2uiv");
		s_glProgramUniform3d = (void*)getProcAddress("glProgramUniform3d");
		s_glProgramUniform3dv = (void*)getProcAddress("glProgramUniform3dv");
		s_glProgramUniform3f = (void*)getProcAddress("glProgramUniform3f");
		s_glProgramUniform3fv = (void*)getProcAddress("glProgramUniform3fv");
		s_glProgramUniform3i = (void*)getProcAddress("glProgramUniform3i");
		s_glProgramUniform3iv = (void*)getProcAddress("glProgramUniform3iv");
		s_glProgramUniform3ui = (void*)getProcAddress("glProgramUniform3ui");
		s_glProgramUniform3uiv = (void*)getProcAddress("glProgramUniform3uiv");
		s_glProgramUniform4d = (void*)getProcAddress("glProgramUniform4d");
		s_glProgramUniform4dv = (void*)getProcAddress("glProgramUniform4dv");
		s_glProgramUniform4f = (void*)getProcAddress("glProgramUniform4f");
		s_glProgramUniform4fv = (void*)getProcAddress("glProgramUniform4fv");
		s_glProgramUniform4i = (void*)getProcAddress("glProgramUniform4i");
		s_glProgramUniform4iv = (void*)getProcAddress("glProgramUniform4iv");
		s_glProgramUniform4ui = (void*)getProcAddress("glProgramUniform4ui");
		s_glProgramUniform4uiv = (void*)getProcAddress("glProgramUniform4uiv");
		s_glProgramUniformMatrix2dv = (void*)getProcAddress("glProgramUniformMatrix2dv");
		s_glProgramUniformMatrix2fv = (void*)getProcAddress("glProgramUniformMatrix2fv");
		s_glProgramUniformMatrix2x3dv = (void*)getProcAddress("glProgramUniformMatrix2x3dv");
		s_glProgramUniformMatrix2x3fv = (void*)getProcAddress("glProgramUniformMatrix2x3fv");
		s_glProgramUniformMatrix2x4dv = (void*)getProcAddress("glProgramUniformMatrix2x4dv");
		s_glProgramUniformMatrix2x4fv = (void*)getProcAddress("glProgramUniformMatrix2x4fv");
		s_glProgramUniformMatrix3dv = (void*)getProcAddress("glProgramUniformMatrix3dv");
		s_glProgramUniformMatrix3fv = (void*)getProcAddress("glProgramUniformMatrix3fv");
		s_glProgramUniformMatrix3x2dv = (void*)getProcAddress("glProgramUniformMatrix3x2dv");
		s_glProgramUniformMatrix3x2fv = (void*)getProcAddress("glProgramUniformMatrix3x2fv");
		s_glProgramUniformMatrix3x4dv = (void*)getProcAddress("glProgramUniformMatrix3x4dv");
		s_glProgramUniformMatrix3x4fv = (void*)getProcAddress("glProgramUniformMatrix3x4fv");
		s_glProgramUniformMatrix4dv = (void*)getProcAddress("glProgramUniformMatrix4dv");
		s_glProgramUniformMatrix4fv = (void*)getProcAddress("glProgramUniformMatrix4fv");
		s_glProgramUniformMatrix4x2dv = (void*)getProcAddress("glProgramUniformMatrix4x2dv");
		s_glProgramUniformMatrix4x2fv = (void*)getProcAddress("glProgramUniformMatrix4x2fv");
		s_glProgramUniformMatrix4x3dv = (void*)getProcAddress("glProgramUniformMatrix4x3dv");
		s_glProgramUniformMatrix4x3fv = (void*)getProcAddress("glProgramUniformMatrix4x3fv");
		s_glProvokingVertex = (void*)getProcAddress("glProvokingVertex");
		s_glPushDebugGroup = (void*)getProcAddress("glPushDebugGroup");
		s_glQueryCounter = (void*)getProcAddress("glQueryCounter");
		s_glReadBuffer = (void*)getProcAddress("glReadBuffer");
		s_glReadPixels = (void*)getProcAddress("glReadPixels");
		s_glReadnPixels = (void*)getProcAddress("glReadnPixels");
		s_glReleaseShaderCompiler = (void*)getProcAddress("glReleaseShaderCompiler");
		s_glRenderbufferStorage = (void*)getProcAddress("glRenderbufferStorage");
		s_glRenderbufferStorageMultisample = (void*)getProcAddress("glRenderbufferStorageMultisample");
		s_glResumeTransformFeedback = (void*)getProcAddress("glResumeTransformFeedback");
		s_glSampleCoverage = (void*)getProcAddress("glSampleCoverage");
		s_glSampleMaski = (void*)getProcAddress("glSampleMaski");
		s_glSamplerParameterIiv = (void*)getProcAddress("glSamplerParameterIiv");
		s_glSamplerParameterIuiv = (void*)getProcAddress("glSamplerParameterIuiv");
		s_glSamplerParameterf = (void*)getProcAddress("glSamplerParameterf");
		s_glSamplerParameterfv = (void*)getProcAddress("glSamplerParameterfv");
		s_glSamplerParameteri = (void*)getProcAddress("glSamplerParameteri");
		s_glSamplerParameteriv = (void*)getProcAddress("glSamplerParameteriv");
		s_glScissor = (void*)getProcAddress("glScissor");
		s_glScissorArrayv = (void*)getProcAddress("glScissorArrayv");
		s_glScissorIndexed = (void*)getProcAddress("glScissorIndexed");
		s_glScissorIndexedv = (void*)getProcAddress("glScissorIndexedv");
		s_glSecondaryColorP3ui = (void*)getProcAddress("glSecondaryColorP3ui");
		s_glSecondaryColorP3uiv = (void*)getProcAddress("glSecondaryColorP3uiv");
		s_glShaderBinary = (void*)getProcAddress("glShaderBinary");
		s_glShaderSource = (void*)getProcAddress("glShaderSource");
		s_glShaderStorageBlockBinding = (void*)getProcAddress("glShaderStorageBlockBinding");
		s_glSpecializeShader = (void*)getProcAddress("glSpecializeShader");
		s_glStencilFunc = (void*)getProcAddress("glStencilFunc");
		s_glStencilFuncSeparate = (void*)getProcAddress("glStencilFuncSeparate");
		s_glStencilMask = (void*)getProcAddress("glStencilMask");
		s_glStencilMaskSeparate = (void*)getProcAddress("glStencilMaskSeparate");
		s_glStencilOp = (void*)getProcAddress("glStencilOp");
		s_glStencilOpSeparate = (void*)getProcAddress("glStencilOpSeparate");
		s_glTexBuffer = (void*)getProcAddress("glTexBuffer");
		s_glTexBufferRange = (void*)getProcAddress("glTexBufferRange");
		s_glTexCoordP1ui = (void*)getProcAddress("glTexCoordP1ui");
		s_glTexCoordP1uiv = (void*)getProcAddress("glTexCoordP1uiv");
		s_glTexCoordP2ui = (void*)getProcAddress("glTexCoordP2ui");
		s_glTexCoordP2uiv = (void*)getProcAddress("glTexCoordP2uiv");
		s_glTexCoordP3ui = (void*)getProcAddress("glTexCoordP3ui");
		s_glTexCoordP3uiv = (void*)getProcAddress("glTexCoordP3uiv");
		s_glTexCoordP4ui = (void*)getProcAddress("glTexCoordP4ui");
		s_glTexCoordP4uiv = (void*)getProcAddress("glTexCoordP4uiv");
		s_glTexImage1D = (void*)getProcAddress("glTexImage1D");
		s_glTexImage2D = (void*)getProcAddress("glTexImage2D");
		s_glTexImage2DMultisample = (void*)getProcAddress("glTexImage2DMultisample");
		s_glTexImage3D = (void*)getProcAddress("glTexImage3D");
		s_glTexImage3DMultisample = (void*)getProcAddress("glTexImage3DMultisample");
		s_glTexParameterIiv = (void*)getProcAddress("glTexParameterIiv");
		s_glTexParameterIuiv = (void*)getProcAddress("glTexParameterIuiv");
		s_glTexParameterf = (void*)getProcAddress("glTexParameterf");
		s_glTexParameterfv = (void*)getProcAddress("glTexParameterfv");
		s_glTexParameteri = (void*)getProcAddress("glTexParameteri");
		s_glTexParameteriv = (void*)getProcAddress("glTexParameteriv");
		s_glTexStorage1D = (void*)getProcAddress("glTexStorage1D");
		s_glTexStorage2D = (void*)getProcAddress("glTexStorage2D");
		s_glTexStorage2DMultisample = (void*)getProcAddress("glTexStorage2DMultisample");
		s_glTexStorage3D = (void*)getProcAddress("glTexStorage3D");
		s_glTexStorage3DMultisample = (void*)getProcAddress("glTexStorage3DMultisample");
		s_glTexSubImage1D = (void*)getProcAddress("glTexSubImage1D");
		s_glTexSubImage2D = (void*)getProcAddress("glTexSubImage2D");
		s_glTexSubImage3D = (void*)getProcAddress("glTexSubImage3D");
		s_glTextureBarrier = (void*)getProcAddress("glTextureBarrier");
		s_glTextureBuffer = (void*)getProcAddress("glTextureBuffer");
		s_glTextureBufferRange = (void*)getProcAddress("glTextureBufferRange");
		s_glTextureParameterIiv = (void*)getProcAddress("glTextureParameterIiv");
		s_glTextureParameterIuiv = (void*)getProcAddress("glTextureParameterIuiv");
		s_glTextureParameterf = (void*)getProcAddress("glTextureParameterf");
		s_glTextureParameterfv = (void*)getProcAddress("glTextureParameterfv");
		s_glTextureParameteri = (void*)getProcAddress("glTextureParameteri");
		s_glTextureParameteriv = (void*)getProcAddress("glTextureParameteriv");
		s_glTextureStorage1D = (void*)getProcAddress("glTextureStorage1D");
		s_glTextureStorage2D = (void*)getProcAddress("glTextureStorage2D");
		s_glTextureStorage2DMultisample = (void*)getProcAddress("glTextureStorage2DMultisample");
		s_glTextureStorage3D = (void*)getProcAddress("glTextureStorage3D");
		s_glTextureStorage3DMultisample = (void*)getProcAddress("glTextureStorage3DMultisample");
		s_glTextureSubImage1D = (void*)getProcAddress("glTextureSubImage1D");
		s_glTextureSubImage2D = (void*)getProcAddress("glTextureSubImage2D");
		s_glTextureSubImage3D = (void*)getProcAddress("glTextureSubImage3D");
		s_glTextureView = (void*)getProcAddress("glTextureView");
		s_glTransformFeedbackBufferBase = (void*)getProcAddress("glTransformFeedbackBufferBase");
		s_glTransformFeedbackBufferRange = (void*)getProcAddress("glTransformFeedbackBufferRange");
		s_glTransformFeedbackVaryings = (void*)getProcAddress("glTransformFeedbackVaryings");
		s_glUniform1d = (void*)getProcAddress("glUniform1d");
		s_glUniform1dv = (void*)getProcAddress("glUniform1dv");
		s_glUniform1f = (void*)getProcAddress("glUniform1f");
		s_glUniform1fv = (void*)getProcAddress("glUniform1fv");
		s_glUniform1i = (void*)getProcAddress("glUniform1i");
		s_glUniform1iv = (void*)getProcAddress("glUniform1iv");
		s_glUniform1ui = (void*)getProcAddress("glUniform1ui");
		s_glUniform1uiv = (void*)getProcAddress("glUniform1uiv");
		s_glUniform2d = (void*)getProcAddress("glUniform2d");
		s_glUniform2dv = (void*)getProcAddress("glUniform2dv");
		s_glUniform2f = (void*)getProcAddress("glUniform2f");
		s_glUniform2fv = (void*)getProcAddress("glUniform2fv");
		s_glUniform2i = (void*)getProcAddress("glUniform2i");
		s_glUniform2iv = (void*)getProcAddress("glUniform2iv");
		s_glUniform2ui = (void*)getProcAddress("glUniform2ui");
		s_glUniform2uiv = (void*)getProcAddress("glUniform2uiv");
		s_glUniform3d = (void*)getProcAddress("glUniform3d");
		s_glUniform3dv = (void*)getProcAddress("glUniform3dv");
		s_glUniform3f = (void*)getProcAddress("glUniform3f");
		s_glUniform3fv = (void*)getProcAddress("glUniform3fv");
		s_glUniform3i = (void*)getProcAddress("glUniform3i");
		s_glUniform3iv = (void*)getProcAddress("glUniform3iv");
		s_glUniform3ui = (void*)getProcAddress("glUniform3ui");
		s_glUniform3uiv = (void*)getProcAddress("glUniform3uiv");
		s_glUniform4d = (void*)getProcAddress("glUniform4d");
		s_glUniform4dv = (void*)getProcAddress("glUniform4dv");
		s_glUniform4f = (void*)getProcAddress("glUniform4f");
		s_glUniform4fv = (void*)getProcAddress("glUniform4fv");
		s_glUniform4i = (void*)getProcAddress("glUniform4i");
		s_glUniform4iv = (void*)getProcAddress("glUniform4iv");
		s_glUniform4ui = (void*)getProcAddress("glUniform4ui");
		s_glUniform4uiv = (void*)getProcAddress("glUniform4uiv");
		s_glUniformBlockBinding = (void*)getProcAddress("glUniformBlockBinding");
		s_glUniformMatrix2dv = (void*)getProcAddress("glUniformMatrix2dv");
		s_glUniformMatrix2fv = (void*)getProcAddress("glUniformMatrix2fv");
		s_glUniformMatrix2x3dv = (void*)getProcAddress("glUniformMatrix2x3dv");
		s_glUniformMatrix2x3fv = (void*)getProcAddress("glUniformMatrix2x3fv");
		s_glUniformMatrix2x4dv = (void*)getProcAddress("glUniformMatrix2x4dv");
		s_glUniformMatrix2x4fv = (void*)getProcAddress("glUniformMatrix2x4fv");
		s_glUniformMatrix3dv = (void*)getProcAddress("glUniformMatrix3dv");
		s_glUniformMatrix3fv = (void*)getProcAddress("glUniformMatrix3fv");
		s_glUniformMatrix3x2dv = (void*)getProcAddress("glUniformMatrix3x2dv");
		s_glUniformMatrix3x2fv = (void*)getProcAddress("glUniformMatrix3x2fv");
		s_glUniformMatrix3x4dv = (void*)getProcAddress("glUniformMatrix3x4dv");
		s_glUniformMatrix3x4fv = (void*)getProcAddress("glUniformMatrix3x4fv");
		s_glUniformMatrix4dv = (void*)getProcAddress("glUniformMatrix4dv");
		s_glUniformMatrix4fv = (void*)getProcAddress("glUniformMatrix4fv");
		s_glUniformMatrix4x2dv = (void*)getProcAddress("glUniformMatrix4x2dv");
		s_glUniformMatrix4x2fv = (void*)getProcAddress("glUniformMatrix4x2fv");
		s_glUniformMatrix4x3dv = (void*)getProcAddress("glUniformMatrix4x3dv");
		s_glUniformMatrix4x3fv = (void*)getProcAddress("glUniformMatrix4x3fv");
		s_glUniformSubroutinesuiv = (void*)getProcAddress("glUniformSubroutinesuiv");
		s_glUnmapBuffer = (void*)getProcAddress("glUnmapBuffer");
		s_glUnmapNamedBuffer = (void*)getProcAddress("glUnmapNamedBuffer");
		s_glUseProgram = (void*)getProcAddress("glUseProgram");
		s_glUseProgramStages = (void*)getProcAddress("glUseProgramStages");
		s_glValidateProgram = (void*)getProcAddress("glValidateProgram");
		s_glValidateProgramPipeline = (void*)getProcAddress("glValidateProgramPipeline");
		s_glVertexArrayAttribBinding = (void*)getProcAddress("glVertexArrayAttribBinding");
		s_glVertexArrayAttribFormat = (void*)getProcAddress("glVertexArrayAttribFormat");
		s_glVertexArrayAttribIFormat = (void*)getProcAddress("glVertexArrayAttribIFormat");
		s_glVertexArrayAttribLFormat = (void*)getProcAddress("glVertexArrayAttribLFormat");
		s_glVertexArrayBindingDivisor = (void*)getProcAddress("glVertexArrayBindingDivisor");
		s_glVertexArrayElementBuffer = (void*)getProcAddress("glVertexArrayElementBuffer");
		s_glVertexArrayVertexBuffer = (void*)getProcAddress("glVertexArrayVertexBuffer");
		s_glVertexArrayVertexBuffers = (void*)getProcAddress("glVertexArrayVertexBuffers");
		s_glVertexAttrib1d = (void*)getProcAddress("glVertexAttrib1d");
		s_glVertexAttrib1dv = (void*)getProcAddress("glVertexAttrib1dv");
		s_glVertexAttrib1f = (void*)getProcAddress("glVertexAttrib1f");
		s_glVertexAttrib1fv = (void*)getProcAddress("glVertexAttrib1fv");
		s_glVertexAttrib1s = (void*)getProcAddress("glVertexAttrib1s");
		s_glVertexAttrib1sv = (void*)getProcAddress("glVertexAttrib1sv");
		s_glVertexAttrib2d = (void*)getProcAddress("glVertexAttrib2d");
		s_glVertexAttrib2dv = (void*)getProcAddress("glVertexAttrib2dv");
		s_glVertexAttrib2f = (void*)getProcAddress("glVertexAttrib2f");
		s_glVertexAttrib2fv = (void*)getProcAddress("glVertexAttrib2fv");
		s_glVertexAttrib2s = (void*)getProcAddress("glVertexAttrib2s");
		s_glVertexAttrib2sv = (void*)getProcAddress("glVertexAttrib2sv");
		s_glVertexAttrib3d = (void*)getProcAddress("glVertexAttrib3d");
		s_glVertexAttrib3dv = (void*)getProcAddress("glVertexAttrib3dv");
		s_glVertexAttrib3f = (void*)getProcAddress("glVertexAttrib3f");
		s_glVertexAttrib3fv = (void*)getProcAddress("glVertexAttrib3fv");
		s_glVertexAttrib3s = (void*)getProcAddress("glVertexAttrib3s");
		s_glVertexAttrib3sv = (void*)getProcAddress("glVertexAttrib3sv");
		s_glVertexAttrib4Nbv = (void*)getProcAddress("glVertexAttrib4Nbv");
		s_glVertexAttrib4Niv = (void*)getProcAddress("glVertexAttrib4Niv");
		s_glVertexAttrib4Nsv = (void*)getProcAddress("glVertexAttrib4Nsv");
		s_glVertexAttrib4Nub = (void*)getProcAddress("glVertexAttrib4Nub");
		s_glVertexAttrib4Nubv = (void*)getProcAddress("glVertexAttrib4Nubv");
		s_glVertexAttrib4Nuiv = (void*)getProcAddress("glVertexAttrib4Nuiv");
		s_glVertexAttrib4Nusv = (void*)getProcAddress("glVertexAttrib4Nusv");
		s_glVertexAttrib4bv = (void*)getProcAddress("glVertexAttrib4bv");
		s_glVertexAttrib4d = (void*)getProcAddress("glVertexAttrib4d");
		s_glVertexAttrib4dv = (void*)getProcAddress("glVertexAttrib4dv");
		s_glVertexAttrib4f = (void*)getProcAddress("glVertexAttrib4f");
		s_glVertexAttrib4fv = (void*)getProcAddress("glVertexAttrib4fv");
		s_glVertexAttrib4iv = (void*)getProcAddress("glVertexAttrib4iv");
		s_glVertexAttrib4s = (void*)getProcAddress("glVertexAttrib4s");
		s_glVertexAttrib4sv = (void*)getProcAddress("glVertexAttrib4sv");
		s_glVertexAttrib4ubv = (void*)getProcAddress("glVertexAttrib4ubv");
		s_glVertexAttrib4uiv = (void*)getProcAddress("glVertexAttrib4uiv");
		s_glVertexAttrib4usv = (void*)getProcAddress("glVertexAttrib4usv");
		s_glVertexAttribBinding = (void*)getProcAddress("glVertexAttribBinding");
		s_glVertexAttribDivisor = (void*)getProcAddress("glVertexAttribDivisor");
		s_glVertexAttribFormat = (void*)getProcAddress("glVertexAttribFormat");
		s_glVertexAttribI1i = (void*)getProcAddress("glVertexAttribI1i");
		s_glVertexAttribI1iv = (void*)getProcAddress("glVertexAttribI1iv");
		s_glVertexAttribI1ui = (void*)getProcAddress("glVertexAttribI1ui");
		s_glVertexAttribI1uiv = (void*)getProcAddress("glVertexAttribI1uiv");
		s_glVertexAttribI2i = (void*)getProcAddress("glVertexAttribI2i");
		s_glVertexAttribI2iv = (void*)getProcAddress("glVertexAttribI2iv");
		s_glVertexAttribI2ui = (void*)getProcAddress("glVertexAttribI2ui");
		s_glVertexAttribI2uiv = (void*)getProcAddress("glVertexAttribI2uiv");
		s_glVertexAttribI3i = (void*)getProcAddress("glVertexAttribI3i");
		s_glVertexAttribI3iv = (void*)getProcAddress("glVertexAttribI3iv");
		s_glVertexAttribI3ui = (void*)getProcAddress("glVertexAttribI3ui");
		s_glVertexAttribI3uiv = (void*)getProcAddress("glVertexAttribI3uiv");
		s_glVertexAttribI4bv = (void*)getProcAddress("glVertexAttribI4bv");
		s_glVertexAttribI4i = (void*)getProcAddress("glVertexAttribI4i");
		s_glVertexAttribI4iv = (void*)getProcAddress("glVertexAttribI4iv");
		s_glVertexAttribI4sv = (void*)getProcAddress("glVertexAttribI4sv");
		s_glVertexAttribI4ubv = (void*)getProcAddress("glVertexAttribI4ubv");
		s_glVertexAttribI4ui = (void*)getProcAddress("glVertexAttribI4ui");
		s_glVertexAttribI4uiv = (void*)getProcAddress("glVertexAttribI4uiv");
		s_glVertexAttribI4usv = (void*)getProcAddress("glVertexAttribI4usv");
		s_glVertexAttribIFormat = (void*)getProcAddress("glVertexAttribIFormat");
		s_glVertexAttribIPointer = (void*)getProcAddress("glVertexAttribIPointer");
		s_glVertexAttribL1d = (void*)getProcAddress("glVertexAttribL1d");
		s_glVertexAttribL1dv = (void*)getProcAddress("glVertexAttribL1dv");
		s_glVertexAttribL2d = (void*)getProcAddress("glVertexAttribL2d");
		s_glVertexAttribL2dv = (void*)getProcAddress("glVertexAttribL2dv");
		s_glVertexAttribL3d = (void*)getProcAddress("glVertexAttribL3d");
		s_glVertexAttribL3dv = (void*)getProcAddress("glVertexAttribL3dv");
		s_glVertexAttribL4d = (void*)getProcAddress("glVertexAttribL4d");
		s_glVertexAttribL4dv = (void*)getProcAddress("glVertexAttribL4dv");
		s_glVertexAttribLFormat = (void*)getProcAddress("glVertexAttribLFormat");
		s_glVertexAttribLPointer = (void*)getProcAddress("glVertexAttribLPointer");
		s_glVertexAttribP1ui = (void*)getProcAddress("glVertexAttribP1ui");
		s_glVertexAttribP1uiv = (void*)getProcAddress("glVertexAttribP1uiv");
		s_glVertexAttribP2ui = (void*)getProcAddress("glVertexAttribP2ui");
		s_glVertexAttribP2uiv = (void*)getProcAddress("glVertexAttribP2uiv");
		s_glVertexAttribP3ui = (void*)getProcAddress("glVertexAttribP3ui");
		s_glVertexAttribP3uiv = (void*)getProcAddress("glVertexAttribP3uiv");
		s_glVertexAttribP4ui = (void*)getProcAddress("glVertexAttribP4ui");
		s_glVertexAttribP4uiv = (void*)getProcAddress("glVertexAttribP4uiv");
		s_glVertexAttribPointer = (void*)getProcAddress("glVertexAttribPointer");
		s_glVertexBindingDivisor = (void*)getProcAddress("glVertexBindingDivisor");
		s_glVertexP2ui = (void*)getProcAddress("glVertexP2ui");
		s_glVertexP2uiv = (void*)getProcAddress("glVertexP2uiv");
		s_glVertexP3ui = (void*)getProcAddress("glVertexP3ui");
		s_glVertexP3uiv = (void*)getProcAddress("glVertexP3uiv");
		s_glVertexP4ui = (void*)getProcAddress("glVertexP4ui");
		s_glVertexP4uiv = (void*)getProcAddress("glVertexP4uiv");
		s_glViewport = (void*)getProcAddress("glViewport");
		s_glViewportArrayv = (void*)getProcAddress("glViewportArrayv");
		s_glViewportIndexedf = (void*)getProcAddress("glViewportIndexedf");
		s_glViewportIndexedfv = (void*)getProcAddress("glViewportIndexedfv");
		s_glWaitSync = (void*)getProcAddress("glWaitSync");
	}
}
