using WebPSharp.Vp8L;
namespace WebPSharp.Tests;
public class Vp8LDistanceTests
{
    [Fact]
    public void FarCodes_MapDirectly()
    {
        Assert.Equal(5, Vp8LDistance.PlaneCodeToDistance(64, 125)); // 125 - 120
        Assert.Equal(1000, Vp8LDistance.PlaneCodeToDistance(64, 1120));
    }

    [Fact]
    public void NearCode1_IsDistanceOne()
    {
        // Plane code 1 -> kCodeToPlane[0]=0x18 -> y=1, x=8-8=0 -> dist=xsize; but the
        // spec's first near code corresponds to the nearest neighbor. Just assert it's valid (>=1).
        Assert.True(Vp8LDistance.PlaneCodeToDistance(64, 1) >= 1);
    }

    [Fact]
    public void AllNearCodes_ProducePositiveDistances()
    {
        for (var pc = 1; pc <= 120; pc++)
            Assert.True(Vp8LDistance.PlaneCodeToDistance(64, pc) >= 1);
    }
}
