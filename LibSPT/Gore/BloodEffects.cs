using System.Collections.Generic;
using Comfort.Common;
using EFT.Ballistics;
using EFT.UI;
using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Gore;

public class BloodEffects
{
    /*
     * - Apply the core set of mists, sprays and splashes.
     * - If armor was hit, cut the chances in half if there was penetration, or by 75% if there wasn't
     * - Apply squirt
     *  - If penetrated, apply a 50% chance to make the squirt go opposite the normal (ie out the exit wound)
     * - If the impact is not angled, apply a secondary effect that is tangential to the surface hit (ie normal to the normal vector)
     */

    private readonly EffectSystem _mists;
    // private readonly EffectSystem _mistsSide;
    private readonly EffectSystem _sprays;
    private readonly EffectSystem _squibs;
    // private readonly EffectSystem _squibsSide;

    // Attached to rigidbodies
    private readonly RigidbodyEffects _squirts;

    // Attached to rigidbodies and played as a final splash in Ragdoll.AmplyImpulse
    private readonly RigidbodyEffects _finishers;

    public BloodEffects(Effects eftEffects, GameObject prefabMain, GameObject prefabSquirts, GameObject prefabFinishers)
    {
        var effectMap = EffectBundle.LoadPrefab(eftEffects, prefabMain, false);

        var puffSide = new DirectionalEffect(effectMap["Puff_Blood"], camDir: CamDir.Angled);

        Plugin.Log.LogInfo("Building blood mists");
        _mists = new EffectSystem(directional: [new DirectionalEffect(effectMap["Puff_Blood_Front"]), puffSide]);
        // _mistsSide = new(directional: [puffSide]);

        Plugin.Log.LogInfo("Building blood sprays");
        _sprays = new EffectSystem(directional: [new DirectionalEffect(effectMap["Spray_Blood"])]);

        Plugin.Log.LogInfo("Building blood squibs");
        _squibs = new EffectSystem(directional: [new DirectionalEffect(effectMap["Squib_Blood_Front"], isChanceScaledByKinetics: true)]);
        // _squibsSide = new(directional: []);

        Plugin.Log.LogInfo("Building blood squirts");
        _squirts = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _squirts.Setup(eftEffects, prefabSquirts, 10);
        
        Plugin.Log.LogInfo("Building finishers");
        _finishers = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _finishers.Setup(eftEffects, prefabFinishers, 10);
    }

    public void Emit(ImpactKinetics kinetics, Rigidbody rigidbody)
    {
        var penetrated = ImpactStatic.PlayerHitInfo != null && ImpactStatic.PlayerHitInfo.Penetrated;
        var armorHit = kinetics.Material != MaterialType.Body;

        float armorChanceScale;

        if (armorHit)
            armorChanceScale = penetrated ? 0.5f : 0.25f;
        else
            armorChanceScale = 1f;

        var mistSizeScale = kinetics.SizeScale * Plugin.BloodMistSize.Value;
        var spraySizeScale = kinetics.SizeScale * Plugin.BloodSpraySize.Value;
        var squibSizeScale = kinetics.SizeScale * Plugin.BloodSquibSize.Value;

        var squibChanceScale = 0.5f * armorChanceScale;

        _mists.Emit(kinetics, mistSizeScale, armorChanceScale);
        _sprays.Emit(kinetics, spraySizeScale, armorChanceScale);
        _squibs.Emit(kinetics, squibSizeScale, squibChanceScale);

        if (!penetrated) return;

        // Flip the normal and add a bit of randomization
        var flippedNormal = -(0.7f * kinetics.Normal + 0.3f * Random.onUnitSphere);
        flippedNormal.Normalize();

        // Push the position along the flipped normal slightly
        var position = kinetics.Position + 0.1f * flippedNormal;

        var squirtChance = 0.5 * armorChanceScale * kinetics.ChanceScale;

        if (Random.Range(0f, 1f) < squirtChance)
        {
            // 25% chance that the squirt goes out the front, otherwise it goes out the exit wound
            if (Random.Range(0f, 1f) < 0.5f)
                _squirts.Emit(rigidbody, kinetics.Position, kinetics.Normal, kinetics.SizeScale * Plugin.BloodSquirtSize.Value);
            else
                _squirts.Emit(rigidbody, position, flippedNormal, kinetics.SizeScale * Plugin.BloodSquirtSize.Value);
        }
        
        // Other stuff gets emitted only if the flipped normal is angled (otherwise there's no point) 
        var camDir = Orientation.GetCamDir(flippedNormal);

        if (!camDir.HasFlag(CamDir.Angled)) return;

        var worldDir = Orientation.GetWorldDir(flippedNormal);
        _mists.Emit(kinetics, camDir, worldDir, position, flippedNormal, mistSizeScale, armorChanceScale);
        _squibs.Emit(kinetics, camDir, worldDir, position, flippedNormal, squibSizeScale, squibChanceScale);
    }

    public void EmitFinisher(ImpactKinetics kinetics, Rigidbody rigidbody, Vector3 position, Vector3 normal)
    {
        _finishers.Emit(rigidbody, position, normal, kinetics.SizeScale * Plugin.BloodFinisherSize.Value);
    }
}
