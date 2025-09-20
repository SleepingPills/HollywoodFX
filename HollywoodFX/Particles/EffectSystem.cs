using System.Runtime.CompilerServices;
using UnityEngine;

namespace HollywoodFX.Particles;

internal readonly struct DirectionalEffect(
    EffectBundle effect,
    float chance = 1f,
    bool isChanceScaledByKinetics = false,
    CamDir camDir = CamDir.None,
    WorldDir worldDir = WorldDir.None
)
{
    public readonly EffectBundle Effect = effect;
    public readonly WorldDir WorldDir = worldDir;
    public readonly CamDir CamDir = camDir;
    public readonly float Chance = chance;
    public readonly bool IsChanceScaledByKinetics = isChanceScaledByKinetics;
}

internal class EffectSystem(
    DirectionalEffect[] directional,
    EffectBundle generic = null,
    float forceGeneric = 0f,
    bool useOffsetNormals = false
)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emit(ImpactKinetics kinetics, float sizeScale = 1f, float chanceScale = 1f)
    {
        var normal = useOffsetNormals ? kinetics.RandNormalOffset : kinetics.RandNormal;
        Emit(kinetics, kinetics.CamDir, kinetics.WorldDir, kinetics.Position, normal, sizeScale, chanceScale);
    }

    private void Emit(ImpactKinetics kinetics, CamDir camDir, WorldDir worldDir, Vector3 position, Vector3 normal, float sizeScale, float chanceScale)
    {
        var bullet = kinetics.Bullet;
        var sizeScaleFull = bullet.SizeScale * sizeScale;

        EffectBundle genericImpact = null;

        if (generic != null && camDir.IsSet(CamDir.Angled))
        {
            genericImpact = generic;

            if (Random.Range(0f, 1f) < forceGeneric)
            {
                genericImpact.EmitDirect(position, normal, sizeScaleFull);
                return;
            }
        }

        var hasEmitted = false;

        for (var i = 0; i < directional.Length; i++)
        {
            var impact = directional[i];

            if (!camDir.IsSet(impact.CamDir) || !worldDir.IsSet(impact.WorldDir)) continue;

            var impactChance = chanceScale * (impact.IsChanceScaledByKinetics ? impact.Chance * bullet.ChanceScale : impact.Chance);
            if (!(Random.Range(0f, 1f) < impactChance)) continue;

            impact.Effect.EmitDirect(position, normal, sizeScaleFull);
            hasEmitted = true;
        }

        if (hasEmitted || genericImpact == null) return;

        genericImpact.EmitDirect(position, normal, sizeScaleFull);
    }
}