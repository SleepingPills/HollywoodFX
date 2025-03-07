using UnityEngine;

namespace HollywoodFX;

public static class ImpactStatic
{
    public static readonly ImpactKinetics Kinetics = new();
    public static ShotInfoClass PlayerHitInfo = null;
    public static Transform LocalPlayerTransform = null;
}