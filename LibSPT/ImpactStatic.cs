using UnityEngine;

namespace HollywoodFX;

public static class ImpactStatic
{
    public static ImpactKinetics Kinetics = new();
    public static ShotInfoClass PlayerHitInfo = null;
    public static Transform LocalPlayerTransform = null;
}