using JetBrains.Annotations;

namespace HollywoodFX;

public static class ImpactStatic
{
    public static readonly ImpactKinetics Kinetics = new();
    [CanBeNull] public static EftBulletClass BulletInfo = null;
    [CanBeNull] public static ShotInfoClass PlayerHitInfo = null;
}