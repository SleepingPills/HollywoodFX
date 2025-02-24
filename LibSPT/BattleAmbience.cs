using System.Collections.Generic;
using Comfort.Common;
using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX;

internal class BattleAmbience
{
    private readonly ParticleSystem[] _cloudSmoke;
    private readonly ParticleSystem[] _suspendedDust;

    public BattleAmbience(Effects eftEffects, GameObject prefab)
    {
        Plugin.Log.LogInfo("Building Battle Ambience Effects");

        var rootInstance = Object.Instantiate(prefab);

        var effectMap = new Dictionary<string, ParticleSystem>();
        foreach (var child in rootInstance.transform.GetChildren())
        {
            if (!child.gameObject.TryGetComponent<ParticleSystem>(out var particleSystem)) continue;

            child.parent = eftEffects.transform;
            ScaleEffect(particleSystem, Plugin.AmbientParticleLifetime.Value, Plugin.AmbientParticleLimit.Value, Plugin.AmbientEffectDensity.Value);
            Singleton<LitMaterialRegistry>.Instance.Register(particleSystem, false);
            effectMap.Add(child.name, particleSystem);
            Plugin.Log.LogInfo($"Added battle ambience effect {particleSystem.name} {particleSystem.transform} {particleSystem.transform.parent}");
        }

        _cloudSmoke = [effectMap["Smoke_1"]];
        _suspendedDust = [effectMap["Dust_1"], effectMap["Glitter_1"]];
    }

    public void Emit(ImpactKinetics kinetics)
    {
        var emission = Singleton<EmissionController>.Instance;

        var emissionChance = 0.4 * (kinetics.Energy / 2500f);

        if (Random.Range(0f, 1f) < emissionChance)
        {
            var smokeEffect = _cloudSmoke[Random.Range(0, _cloudSmoke.Length)];
            emission.Emit(smokeEffect, kinetics.Position, kinetics.Normal);
        }

        if (!(Random.Range(0f, 1f) < emissionChance)) return;

        var dustEffect = _suspendedDust[Random.Range(0, _suspendedDust.Length)];
        emission.Emit(dustEffect, kinetics.Position, kinetics.Normal);
    }

    private static void ScaleEffect(ParticleSystem particleSystem, float lifetimeScaling, float limitScaling, float emissionScaling)
    {
        var main = particleSystem.main;

        if (!Mathf.Approximately(limitScaling, 1))
        {
            main.maxParticles = (int)(main.maxParticles * limitScaling);
        }

        if (!Mathf.Approximately(lifetimeScaling, 1))
        {
            var lifetime = main.startLifetime;
            lifetime.constant *= lifetimeScaling;
            lifetime.constantMin *= lifetimeScaling;
            lifetime.constantMax *= lifetimeScaling;
            lifetime.curveMultiplier = lifetimeScaling;
        }

        if (Mathf.Approximately(emissionScaling, 1)) return;

        var emission = particleSystem.emission;

        for (var i = 0; i < emission.burstCount; i++)
        {
            var burst = emission.GetBurst(i);
            burst.minCount = CalcBurstCount(burst.minCount, emissionScaling);
            burst.maxCount = CalcBurstCount(burst.maxCount, emissionScaling);
            emission.SetBurst(i, burst);
        }
    }

    private static short CalcBurstCount(short count, float scaling)
    {
        // Don't try to scale single particle emissions
        if (count < 2)
        {
            return count;
        }

        // Clip the lower value to 1
        return (short)Mathf.Max(count * scaling, 1f);
    }
}