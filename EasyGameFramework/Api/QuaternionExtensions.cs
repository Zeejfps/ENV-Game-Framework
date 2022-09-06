using System.Numerics;

namespace EasyGameFramework.Api;

public static class QuaternionExtensions
{
    public static void ExtractYawPitchRoll(this Quaternion r, out float yaw, out float pitch, out float roll) {
        yaw = MathF.Atan2(2.0f * (r.Y * r.W + r.X * r.Z), 1.0f - 2.0f * (r.X * r.X + r.Y * r.Y));
        pitch = MathF.Asin(2.0f * (r.X * r.W - r.Y * r.Z));
        roll = MathF.Atan2(2.0f * (r.X * r.Y + r.Z * r.W), 1.0f - 2.0f * (r.X * r.X + r.Z * r.Z));
    }
}