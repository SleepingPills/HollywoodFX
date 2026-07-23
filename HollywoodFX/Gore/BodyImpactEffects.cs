using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT.Ballistics;
using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Gore;

public class BodyImpactEffects
{
    private readonly EffectSystem[] _puffs;
    private readonly EffectSystem _sprays;

    // Attached to rigidbodies
    private readonly RigidbodyEffects _squirts;

    // Attached to rigidbodies and played as a final splash in Ragdoll.AmplyImpulse
    private readonly RigidbodyEffects _finishers;
    private readonly RigidbodyEffects _bleedouts;

    private readonly List<EffectSystem> _dustyImpact;
    private readonly EffectSystem _bodyArmorImpact;
    private readonly EffectSystem _helmetImpact;

    private float _timestampLocal;
    private float _timestampOther;

    public BodyImpactEffects(
        Effects eftEffects, Dictionary<string, EffectBundle> impactEffects,
        GameObject prefabMain, GameObject prefabSquirts, GameObject prefabBleedouts, GameObject prefabFinishers
    )
    {
        var effectMap = EffectBundle.LoadPrefab(eftEffects, prefabMain, false);
        
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

        _bleedouts = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _bleedouts.Setup(eftEffects, prefabBleedouts, 10, 10f, Plugin.BloodBleedoutEmission.Value);

        _finishers = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _finishers.Setup(eftEffects, prefabFinishers, 15, 3.5f, Plugin.BloodFinisherEmission.Value);

        // Note: duplicated from ImpactEffects
        var puffFront = EffectBundle.Merge(impactEffects["Puff_Front"], impactEffects["Puff_Front_Dusty"]);
        var puffGeneric = impactEffects["Puff"];

        var sprayDust = impactEffects["Spray_Dust"];
        var spraySparksLight = EffectBundle.Merge(
            impactEffects["Spray_Sparks_Light"], impactEffects["Spray_Dust"], impactEffects["Spray_Dust"],
            impactEffects["Spray_Dust"], impactEffects["Spray_Dust"]
        );

        _dustyImpact =
        [
            new EffectSystem(directional: [], generic: puffGeneric, forceGeneric: 1f, useOffsetNormals: true),
            new EffectSystem(directional: [new DirectionalEffect(puffFront)])
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

        // Separate timestamp tracking for the local player and others.
        // The objective is to ensure that bots (or coop partners) don't hog the pacing. 
        ref var timestamp = ref bullet.Info.Player.iPlayer.IsYourPlayer ? ref _timestampLocal : ref _timestampOther;

        // Apply pacing to the heavier effects
        if (Time.unscaledTime >= timestamp)
        {
            // First roll to decide whether we emit anything at all
            if (Random.Range(0f, 1f) < chanceBase)
            {
                // Bump the timer
                timestamp = Time.unscaledTime + 0.5f;
                
                // Second roll to decide whether we emit a squirt or a spray
                if (Random.Range(0f, 1f) < 0.5f && rigidbody.gameObject.layer != LayerMaskClass.DeadbodyLayer)
                {
                    var normal = kinetics.Normal - bullet.Info.Direction;
                    normal.Normalize();
                
                    var (camDir, _) = Orientation.GetCamDir(normal);
                    var normalOffset = Orientation.GetNormOffset(normal, camDir);
                
                    _squirts.Emit(rigidbody, kinetics.Position, normalOffset, sizeScaleKinetics * Plugin.BloodSquirtSize.Value);
                    
                    // Bail out completely if we emitted a squirt
                    return;
                }

                _sprays.Emit(kinetics, sizeScaleKinetics * Plugin.BloodSpraySize.Value);
            }
        }

        if (Random.Range(0f, 1f) < chanceBase)
        {
            for (var i = 0; i < _puffs.Length; i++)
            {
                _puffs[i].Emit(kinetics, sizeScaleKinetics);
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