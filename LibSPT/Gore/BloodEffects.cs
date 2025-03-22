using EFT.Ballistics;
using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Gore;

public class BloodEffects
{
    private readonly EffectSystem _mists;
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

        var puffSide = new DirectionalEffect(effectMap["Puff_Blood"], camDir: CamDir.Angled);

        Plugin.Log.LogInfo("Building blood mists");
        _mists = new EffectSystem(directional: [new DirectionalEffect(effectMap["Puff_Blood_Front"]), puffSide]);

        Plugin.Log.LogInfo("Building blood sprays");
        _sprays = new EffectSystem(directional: [new DirectionalEffect(effectMap["Spray_Blood"])]);

        Plugin.Log.LogInfo("Building blood squibs");
        _squibs = new EffectSystem(directional: [new DirectionalEffect(effectMap["Squib_Blood_Front"], isChanceScaledByKinetics: true)]);

        Plugin.Log.LogInfo("Building blood squirts");
        _squirts = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _squirts.Setup(eftEffects, prefabSquirts, 10, 2f);
        
        Plugin.Log.LogInfo("Building bleeds");
        _bleeds = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _bleeds.Setup(eftEffects, prefabBleeds, 30, 10f);

        Plugin.Log.LogInfo("Building bleedouts");
        _bleedouts = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _bleedouts.Setup(eftEffects, prefabBleedouts, 10, 3.5f);
        
        Plugin.Log.LogInfo("Building finishers");
        _finishers = eftEffects.gameObject.AddComponent<RigidbodyEffects>();
        _finishers.Setup(eftEffects, prefabFinishers, 10, 2f);
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

        var bullet = kinetics.Bullet;

        var mistSizeScale = bullet.SizeScale * Plugin.BloodMistSize.Value;
        var spraySizeScale = bullet.SizeScale * Plugin.BloodSpraySize.Value;
        var squibSizeScale = bullet.SizeScale * Plugin.BloodSquibSize.Value;

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

        var squirtChance = 0.5f * armorChanceScale * bullet.ChanceScale;

        if (Random.Range(0f, 1f) < squirtChance)
        {
            // 50% chance that the squirt goes out the front, otherwise it goes out the exit wound
            if (Random.Range(0f, 1f) < 0.5f)
                _squirts.Emit(rigidbody, kinetics.Position, kinetics.Normal, bullet.SizeScale * Plugin.BloodSquirtSize.Value);
            else
                _squirts.Emit(rigidbody, position, flippedNormal, bullet.SizeScale * Plugin.BloodSquirtSize.Value);
        }
        else
        {
            // Emit bleeding if a squirt was not generated
            _bleeds.Emit(rigidbody, kinetics.Position, kinetics.Normal, bullet.SizeScale * Plugin.BloodSquirtSize.Value);
        }

        // Other stuff gets emitted only if the flipped normal is angled (otherwise there's no point) 
        var camDir = Orientation.GetCamDir(flippedNormal);

        if (!camDir.IsSet(CamDir.Angled)) return;

        var worldDir = Orientation.GetWorldDir(flippedNormal);
        _mists.Emit(kinetics, camDir, worldDir, position, flippedNormal, mistSizeScale, armorChanceScale);
        _squibs.Emit(kinetics, camDir, worldDir, position, flippedNormal, squibSizeScale, squibChanceScale);
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