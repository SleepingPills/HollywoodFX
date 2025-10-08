using System.Runtime.CompilerServices;
using EFT.UI;
using UnityEngine;

namespace HollywoodFX.Particles;

internal class DirectionalEffect(
    EffectBundle effect,
    float chance = 1f,
    bool isChanceScaledByKinetics = false,
    CamDir camDir = CamDir.None,
    WorldDir worldDir = WorldDir.None,
    float pacing = 0f
)
{
    public readonly EffectBundle Effect = effect;
    public readonly WorldDir WorldDir = worldDir;
    public readonly CamDir CamDir = camDir;
    public readonly float Chance = chance;
    public readonly bool IsChanceScaledByKinetics = isChanceScaledByKinetics;
    public readonly float Pacing = pacing;
    public float Timestamp;
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

            if (impact.Pacing > 0.01f)
            {
                if (Time.unscaledTime < impact.Timestamp)
                {
                    continue;
                }
                
                impact.Timestamp = Time.unscaledTime + impact.Pacing;
            }
            
            var impactChance = chanceScale * (impact.IsChanceScaledByKinetics ? impact.Chance * bullet.ChanceScale : impact.Chance);
            if (!(Random.Range(0f, 1f) < impactChance)) continue;

            impact.Effect.EmitDirect(position, normal, sizeScaleFull);
            hasEmitted = true;
        }

        if (hasEmitted || genericImpact == null) return;

        genericImpact.EmitDirect(position, normal, sizeScaleFull);
    }
}