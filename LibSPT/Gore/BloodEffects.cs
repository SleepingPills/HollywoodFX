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
    private readonly EffectSystem _mistsSide;
    private readonly EffectSystem _sprays;
    private readonly EffectSystem _squibs;
    private readonly EffectSystem _squibsSide;

    // Attached to rigidbodies
    private readonly EffectBundle _squirts;

    // Attached to rigidbodies and played as a final splash in Ragdoll.AmplyImpulse
    private readonly EffectBundle _finishers;

    public BloodEffects(Effects eftEffects, GameObject prefab)
    {
        var effectMap = EffectBundle.LoadPrefab(eftEffects, prefab);

        var puffSide = new DirectionalEffect(effectMap["Puff_Blood"], camDir: CamDir.Angled);

        Plugin.Log.LogInfo($"Building blood mists");
        _mists = new(directional: [new DirectionalEffect(effectMap["Puff_Blood_Front"]), puffSide]);
        _mistsSide = new(directional: [puffSide]);

        Plugin.Log.LogInfo($"Building blood sprays");
        _sprays = new(directional: [new DirectionalEffect(effectMap["Spray_Blood"])]);

        Plugin.Log.LogInfo($"Building blood squibs");
        _squibs = new(directional: [new DirectionalEffect(effectMap["Squib_Blood_Front"], isChanceScaledByKinetics: true)]);
        _squibsSide = new(directional: []);

        Plugin.Log.LogInfo($"Building blood squirts");
        _squirts = effectMap["Squirt_Blood"];

        foreach (var squirt in _squirts.ParticleSystems)
        {
            squirt.gameObject.AddComponent<BloodSquirtCollisionHandler>();
        }
    }

    public void Emit(ImpactKinetics kinetics)
    {
        var penetrated = ImpactStatic.PlayerHitInfo != null && ImpactStatic.PlayerHitInfo.Penetrated;
        var armorHit = kinetics.Material != MaterialType.Body;

        float chanceScale;

        if (armorHit)
            chanceScale = penetrated ? 0.5f : 0.25f;
        else
            chanceScale = 1f;

        var mistSizeScale = kinetics.SizeScale * Plugin.BloodMistSize.Value;
        var spraySizeScale = kinetics.SizeScale * Plugin.BloodSpraySize.Value;
        var squibSizeScale = kinetics.SizeScale * Plugin.BloodSquibSize.Value;

        var squibChanceScale = 0.5f * chanceScale;

        _mists.Emit(kinetics, mistSizeScale, chanceScale);
        _sprays.Emit(kinetics, spraySizeScale, chanceScale);
        _squibs.Emit(kinetics, squibSizeScale, squibChanceScale);

        var squirtChance = 0.5 * chanceScale * kinetics.ChanceScale;

        if (Random.Range(0f, 1f) < squirtChance)
        {
            _squirts.EmitRandom(kinetics.Position, kinetics.Normal, kinetics.SizeScale * Plugin.BloodSquirtSize.Value);
        }

        if (!penetrated) return;

        // Flip the normal and add a bit of randomization
        var flippedNormal = -(0.7f * kinetics.Normal + 0.3f * Random.onUnitSphere);
        flippedNormal.Normalize();

        // Push the position along the flipped normal slightly
        var position = kinetics.Position + 0.1f * flippedNormal;

        // Emit a squirt purely based on chance
        if (Random.Range(0f, 1f) < squirtChance)
        {
            // TODO: Check why this is always triggering?
            ConsoleScreen.Log($"Emitting penetrating squirt {squirtChance}");
            _squirts.EmitRandom(position, flippedNormal, kinetics.SizeScale * Plugin.BloodSquirtSize.Value);
        }

        // Other stuff gets emitted only if the flipped normal is angled (otherwise there's no point) 
        var camDir = Orientation.GetCamDir(flippedNormal);

        if (!camDir.HasFlag(CamDir.Angled)) return;

        ConsoleScreen.Log($"Emitting penetrating mists/squibs");

        var worldDir = Orientation.GetWorldDir(flippedNormal);
        _mists.Emit(kinetics, camDir, worldDir, position, flippedNormal, mistSizeScale, chanceScale);
        _squibs.Emit(kinetics, camDir, worldDir, position, flippedNormal, squibSizeScale, squibChanceScale);
    }
}

public class BloodSquirtCollisionHandler : MonoBehaviour
{
    private Effects _effects;
    private ParticleSystem _particleSystem;
    private List<ParticleCollisionEvent> _collisionEvents;

    public void Start()
    {
        _effects = Singleton<Effects>.Instance;
        _particleSystem = GetComponent<ParticleSystem>();
        _collisionEvents = [];
        Plugin.Log.LogInfo("Starting collision handler");
    }

    public void OnParticleCollision(GameObject other)
    {
        if (other == null)
            return;

        var numEvents = _particleSystem.GetCollisionEvents(other, _collisionEvents);

        for (var i = 0; i < numEvents; i++)
        {
            var hitPos = _collisionEvents[i].intersection;
            var hitNormal = _collisionEvents[i].normal;

            _effects.EmitBleeding(hitPos, hitNormal);
        }
    }
}