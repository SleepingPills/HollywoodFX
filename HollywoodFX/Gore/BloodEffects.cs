using System.Runtime.CompilerServices;
using EFT.Ballistics;
using EFT.UI;
using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;
using MemoryExtensions = System.MemoryExtensions;

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
        _bleeds.Setup(eftEffects, prefabBleeds, 20, 10f, Plugin.BloodBleedEmission.Value);

        _bleedouts = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _bleedouts.Setup(eftEffects, prefabBleedouts, 20, 10f, Plugin.BloodBleedoutEmission.Value);

        _finishers = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _finishers.Setup(eftEffects, prefabFinishers, 10, 3.5f, Plugin.BloodFinisherEmission.Value);
    }

    public void Emit(ImpactKinetics kinetics, Rigidbody rigidbody)
    {
        var bullet = kinetics.Bullet;
        
        if (!bullet.Penetrated)
        {
            return;
        }
        
        var vitalScale = 0.5f;

        if (rigidbody.name.Length >= 11)
        {
            var nameSubset = MemoryExtensions.AsSpan(rigidbody.name, 10);

            if (MemoryExtensions.StartsWith(nameSubset, "Spine")
                || MemoryExtensions.StartsWith(nameSubset, "Pelvis")
                || MemoryExtensions.StartsWith(nameSubset, "Head")
                || MemoryExtensions.StartsWith(nameSubset, "Neck"))
            {
                vitalScale = 1f;
            }
        }
        
        var armorScale = kinetics.Material != MaterialType.Body ? 0.5f : 1f;
        var sizeScale = Mathf.Min(vitalScale * bullet.SizeScale, 1f);

        var chance = vitalScale * armorScale * bullet.ChanceScale;

        // Roll once whether we emit blood at all
        if (!(Random.Range(0f, 1f) < chance)) return;
        
        var mistSize = sizeScale * Plugin.BloodMistSize.Value;
            
        // Emit a mist or a puff at a 25/75 chance
        if (Random.Range(0f, 1f) < 0.25f)
            _mists.EmitDirect(kinetics.Position, kinetics.Normal, mistSize);
        else
        {
            for (var i = 0; i < _puffs.Length; i++)
            {
                _puffs[i].Emit(kinetics, mistSize);            
            }
        }
            
        _sprays.Emit(kinetics, sizeScale * Plugin.BloodSpraySize.Value);

        if (Random.Range(0f, 1f) < 0.5f * chance)
        {
            _squibs.Emit(kinetics, sizeScale * Plugin.BloodSquibSize.Value, chance);
            
            // 50% chance that the squirt goes out the front, otherwise it goes out the exit wound
            if (Random.Range(0f, 1f) < 0.5f)
                _squirts.Emit(rigidbody, kinetics.Position, kinetics.Normal, sizeScale * Plugin.BloodSquirtSize.Value);
            else
            {
                // Flip the normal and add a bit of randomization
                var flippedNormal = -(0.7f * kinetics.Normal + 0.3f * Random.onUnitSphere);
                flippedNormal.Normalize();

                // Push the position along the flipped normal slightly
                var position = kinetics.Position + 0.1f * flippedNormal;
                
                _squirts.Emit(rigidbody, position, flippedNormal, sizeScale * Plugin.BloodSquirtSize.Value);
            }
        }
        
        if (Random.Range(0f, 1f) < 0.25f * chance)
        {
            _bleeds.Emit(rigidbody, kinetics.Position, kinetics.Normal, sizeScale * Plugin.BloodBleedSize.Value);
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