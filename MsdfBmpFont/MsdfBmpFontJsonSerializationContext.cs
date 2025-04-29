using System.Text.Json.Serialization;

namespace MsdfBmpFont;

[JsonSerializable(typeof(MsdfFontFile))]
internal partial class MsdfBmpFontJsonSerializationContext : JsonSerializerContext {}