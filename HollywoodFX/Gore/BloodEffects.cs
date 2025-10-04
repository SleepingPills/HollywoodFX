using EFT.Ballistics;
using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Gore;

public class BloodEffects
{
    private readonly EffectBundle _mists;
    private readonly EffectSystem[] _puffs;
    private readonly EffectSystem _sprays;
    private readonly EffectSystem _squibs;

    // Attached to rigidbodies
    private readonly RigidbodyEffects _squirts;
    private readonly RigidbodyEffects _bleeds;

    // Attached to rigidbodies and played as a final splash in Ragdoll.AmplyImpulse
    private readonly RigidbodyEffects _finishers;
    private readonly RigidbodyEffects _bleedouts;

    public BloodEffects(
        Effects eftEffects, GameObject prefabMain, GameObject prefabSquirts, GameObject prefabBleeds, GameObject prefabBleedouts,
        GameObject prefabFinishers
    )
    {
        var effectMap = EffectBundle.LoadPrefab(eftEffects, prefabMain, false);

        _mists = effectMap["Mist_Blood"];
        _mists.ScaleDensity(Plugin.BloodMistEmission.Value);

        Plugin.Log.LogInfo("Building blood puffs");
        _puffs =
        [
            new EffectSystem(directional:
                [
                    new DirectionalEffect(effectMap["Puff_Blood"], camDir: CamDir.Angled)
                ],
                useOffsetNormals: true
            ),
            new EffectSystem(directional:
                [
                    new DirectionalEffect(effectMap["Puff_Blood_Front"]),
                ]
            )
        ];

        Plugin.Log.LogInfo("Building blood sprays");
        var bloodSprays = effectMap["Spray_Blood"];
        bloodSprays.ScaleDensity(Plugin.BloodSprayEmission.Value);
        _sprays = new EffectSystem(directional: [new DirectionalEffect(bloodSprays)]);

        Plugin.Log.LogInfo("Building blood squibs");
        _squibs = new EffectSystem(directional: [new DirectionalEffect(effectMap["Squib_Blood_Front"], isChanceScaledByKinetics: true)]);

        _squirts = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _squirts.Setup(eftEffects, prefabSquirts, 10, 2f, Plugin.BloodSquirtEmission.Value);

        _bleeds = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _bleeds.Setup(eftEffects, prefabBleeds, 30, 10f, Plugin.BloodBleedEmission.Value);

        _bleedouts = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _bleedouts.Setup(eftEffects, prefabBleedouts, 10, 3.5f, Plugin.BloodFinisherEmission.Value);

        _finishers = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _finishers.Setup(eftEffects, prefabFinishers, 10, 2f, Plugin.BloodFinisherEmission.Value);
    }

    public void Emit(ImpactKinetics kinetics, Rigidbody rigidbody)
    {
        var penetrated = ImpactStatic.PlayerHitInfo != null && ImpactStatic.PlayerHitInfo.Penetrated;
        var armorHit = kinetics.Material != MaterialType.Body;

        float armorChanceScale;

        if (armorHit)
            armorChanceScale = penetrated ? 0.25f : 0.15f;
        else
            armorChanceScale = 0.5f;

        var bullet = kinetics.Bullet;

        // Don't scale the sprays as they look wonky when too big
        var spraySizeScale = Plugin.BloodSpraySize.Value;
        var mistSizeScale = Mathf.Max(bullet.SizeScale, 1f) * Plugin.BloodMistSize.Value;
        var squibSizeScale = bullet.SizeScale * Plugin.BloodSquibSize.Value;

        var mistChance = 0.5f * armorChanceScale * bullet.ChanceScale * Plugin.BloodMistChanceScale.Value;
        if (Random.Range(0f, 1f) < mistChance)
        {
            // Emit a mist or a puff at a 25/75 chance
            if (Random.Range(0f, 1f) < 0.25f)
                _mists.EmitDirect(kinetics.Position, kinetics.Normal, mistSizeScale);
            else
            {
                for (var i = 0; i < _puffs.Length; i++)
                {
                    _puffs[i].Emit(kinetics, mistSizeScale);            
                }
            }
        }

        var miscChance = armorChanceScale * bullet.ChanceScale * Plugin.BloodMiscChanceScale.Value;
        _sprays.Emit(kinetics, spraySizeScale, miscChance);
        _squibs.Emit(kinetics, squibSizeScale, miscChance);

        if (!penetrated) return;

        // Flip the normal and add a bit of randomization
        var flippedNormal = -(0.7f * kinetics.Normal + 0.3f * Random.onUnitSphere);
        flippedNormal.Normalize();

        // Push the position along the flipped normal slightly
        var position = kinetics.Position + 0.1f * flippedNormal;

        var squirtChance = 0.75f * armorChanceScale * bullet.ChanceScale * Plugin.BloodSquirtChanceScale.Value;

        if (Random.Range(0f, 1f) < squirtChance)
        {
            // 50% chance that the squirt goes out the front, otherwise it goes out the exit wound
            if (Random.Range(0f, 1f) < 0.5f)
                _squirts.Emit(rigidbody, kinetics.Position, kinetics.Normal, bullet.SizeScale * Plugin.BloodSquirtSize.Value);
            else
                _squirts.Emit(rigidbody, position, flippedNormal, bullet.SizeScale * Plugin.BloodSquirtSize.Value);
        }
        else if (Random.Range(0f, 1f) < squirtChance)
        {
            // Emit bleeding if a squirt was not generated
            _bleeds.Emit(rigidbody, kinetics.Position, kinetics.Normal, bullet.SizeScale * Plugin.BloodBleedSize.Value);
        }
    }

    public void EmitBleedout(Rigidbody rigidbody, Vector3 position, Vector3 normal, float sizeScale)
    {
        _bleedouts.Emit(rigidbody, position, normal, sizeScale * Plugin.BloodFinisherSize.Value);
    }

    public void EmitFinisher(Rigidbody rigidbody, Vector3 position, Vector3 normal, float sizeScale)
    {
        _finishers.Emit(rigidbody, position, normal, sizeScale * Plugin.BloodFinisherSize.Value);
    }
}