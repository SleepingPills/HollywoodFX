using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT.Ballistics;
using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;
using MemoryExtensions = System.MemoryExtensions;

namespace HollywoodFX.Gore;

public class BodyImpactEffects
{
    private readonly EffectBundle _mists;
    private readonly EffectSystem[] _puffs;
    private readonly EffectSystem _sprays;

    // Attached to rigidbodies
    private readonly RigidbodyEffects _squirts;
    private readonly RigidbodyEffects _bleeds;

    // Attached to rigidbodies and played as a final splash in Ragdoll.AmplyImpulse
    private readonly RigidbodyEffects _finishers;
    private readonly RigidbodyEffects _bleedouts;

    private readonly List<EffectSystem> _dustyImpact;
    private readonly EffectSystem _bodyArmorImpact;
    private readonly EffectSystem _helmetImpact;
    
    private float _timestamp;

    public BodyImpactEffects(
        Effects eftEffects, Dictionary<string, EffectBundle> impactEffects,
        GameObject prefabMain, GameObject prefabSquirts, GameObject prefabBleeds, GameObject prefabBleedouts, GameObject prefabFinishers
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
                    new DirectionalEffect(effectMap["Puff_Blood"])
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

        _squirts = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _squirts.Setup(eftEffects, prefabSquirts, 10, 2f, Plugin.BloodSquirtEmission.Value);

        _bleeds = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _bleeds.Setup(eftEffects, prefabBleeds, 10, 5f, Plugin.BloodBleedEmission.Value);

        _bleedouts = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _bleedouts.Setup(eftEffects, prefabBleedouts, 10, 10f, Plugin.BloodBleedoutEmission.Value);

        _finishers = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _finishers.Setup(eftEffects, prefabFinishers, 15, 3.5f, Plugin.BloodFinisherEmission.Value);

        // Note: duplicated from ImpactEffects
        var puffFront = EffectBundle.Merge(impactEffects["Puff_Front"], impactEffects["Puff_Dusty_Front"]);
        var puffGeneric = impactEffects["Puff"];

        var sprayDust = impactEffects["Spray_Dust"];
        var spraySparksLight = EffectBundle.Merge(
            impactEffects["Spray_Sparks_Light"], impactEffects["Spray_Dust"], impactEffects["Spray_Dust"],
            impactEffects["Spray_Dust"], impactEffects["Spray_Dust"]
        );

        _dustyImpact =
        [
            new(directional: [], generic: puffGeneric, forceGeneric: 1f, useOffsetNormals: true),
            new(directional: [new DirectionalEffect(puffFront)])
        ];

        _bodyArmorImpact = new EffectSystem(
            directional:
            [
                new DirectionalEffect(sprayDust, chance: 0.5f, isChanceScaledByKinetics: true, pacing: 0.05f),
                new DirectionalEffect(
                    EffectBundle.Merge(impactEffects["Debris_Armor_Metal"], impactEffects["Debris_Armor_Fabric"]),
                    chance: 0.5f, isChanceScaledByKinetics: true, pacing: 0.1f
                )
            ]
        );

        _helmetImpact = new EffectSystem(
            directional:
            [
                new DirectionalEffect(spraySparksLight, chance: 0.4f, isChanceScaledByKinetics: true, pacing: 0.05f),
                new DirectionalEffect(impactEffects["Debris_Armor_Metal"], chance: 0.5f, isChanceScaledByKinetics: true, pacing: 0.1f)
            ]
        );
    }

    public void Emit(ImpactKinetics kinetics, Rigidbody rigidbody)
    {
        var bullet = kinetics.Bullet;

        // Emit some material specific effects under all conditions
        switch (kinetics.Material)
        {
            case MaterialType.BodyArmor:
                _bodyArmorImpact.Emit(kinetics, Plugin.EffectSize.Value);
                break;
            case MaterialType.Helmet or MaterialType.GlassVisor or MaterialType.HelmetRicochet:
                _helmetImpact.Emit(kinetics, Plugin.EffectSize.Value);
                break;
        }

        if (!bullet.Penetrated)
        {
            // If the bullet didn't penetrate, emit dust
            for (var i = 0; i < _dustyImpact.Count; i++)
            {
                var effect = _dustyImpact[i];
                effect.Emit(kinetics, Plugin.EffectSize.Value);
            }
            return;
        }

        var sizeScaleKinetics = Mathf.Min(bullet.SizeScale, 1f);
        var chanceScaleArmor = kinetics.Material != MaterialType.Body ? 0.5f : 1f;
        var chanceBase = chanceScaleArmor * Mathf.Min(bullet.ChanceScale, 1f);

        var puffSize = sizeScaleKinetics * Plugin.BloodMistSize.Value;

        // Apply pacing to the heavier effects
        if (Time.unscaledTime >= _timestamp)
        {
            // Squirts
            if (Random.Range(0f, 1f) < 0.5f * chanceBase)
            {
                if (rigidbody.name.Length >= 11 && rigidbody.gameObject.layer != LayerMaskClass.DeadbodyLayer)
                {
                    var nameSubset = MemoryExtensions.AsSpan(rigidbody.name, 10);

                    if (MemoryExtensions.StartsWith(nameSubset, "Spine")
                        || MemoryExtensions.StartsWith(nameSubset, "Pelvis")
                        || MemoryExtensions.StartsWith(nameSubset, "Head")
                        || MemoryExtensions.StartsWith(nameSubset, "Neck"))
                    {
                        _squirts.Emit(rigidbody, kinetics.Position, kinetics.RandNormalOffset, sizeScaleKinetics * Plugin.BloodSquirtSize.Value);
                        _mists.EmitDirect(kinetics.Position, kinetics.Normal, puffSize);
                        _timestamp = Time.unscaledTime + 0.1f;

                        // Bail out completely if we emitted a squirt
                        return;
                    }
                }
            }
            
            var bumpTime = false;
            
            // Bleeding
            if (Random.Range(0f, 1f) < 0.2f * chanceBase)
            {
                _bleeds.Emit(rigidbody, kinetics.Position, kinetics.Normal, sizeScaleKinetics * Plugin.BloodBleedSize.Value);
                bumpTime = true;
            }
            
            // Only run this if the squirt hasn't triggered, to avoid piling too many heavy effects at once
            if (Random.Range(0f, 1f) < chanceBase)
            {
                _sprays.Emit(kinetics, sizeScaleKinetics * Plugin.BloodSpraySize.Value);
                bumpTime = true;
            }

            if (bumpTime)
            {
                _timestamp = Time.unscaledTime + 0.1f;
            }
        }

        if (Random.Range(0f, 1f) < chanceBase)
        {
            for (var i = 0; i < _puffs.Length; i++)
            {
                _puffs[i].Emit(kinetics, puffSize);
            }
        }
        else
        {
            for (var i = 0; i < _dustyImpact.Count; i++)
            {
                var effect = _dustyImpact[i];
                effect.Emit(kinetics, Plugin.EffectSize.Value);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EmitBleedout(Rigidbody rigidbody, Vector3 position, Vector3 normal, float sizeScale)
    {
        _bleedouts.Emit(rigidbody, position, normal, sizeScale * Plugin.BloodBleedoutSize.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EmitFinisher(Rigidbody rigidbody, Vector3 position, Vector3 normal, float sizeScale)
    {
        _finishers.Emit(rigidbody, position, normal, sizeScale * Plugin.BloodFinisherSize.Value);
    }
}