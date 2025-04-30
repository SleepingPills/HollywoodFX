using System.Collections.Generic;
using Comfort.Common;
using EFT.UI;
using HollywoodFX.Lighting;
using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX;

internal class BattleAmbienceEmissionTime
{
    public float Smoke;
    public float Dust;
}

internal class BattleAmbience
{
    private readonly ParticleSystem[] _cloudSmoke;
    private readonly ParticleSystem[] _suspendedDust;
    private readonly Dictionary<int, BattleAmbienceEmissionTime> _emissionTimes = new();

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
        // ReSharper disable once MergeSequentialChecks
        if (kinetics.Bullet.Info == null || kinetics.Bullet.Info.Player == null) return;
        
        var emission = Singleton<EmissionController>.Instance;

        var emissionChance = 0.4 * (kinetics.Bullet.Energy / 2500f);

        var playerId = kinetics.Bullet.Info.Player.iPlayer.Id;
        
        if (!_emissionTimes.TryGetValue(playerId, out var emissionTime))
        {
            _emissionTimes[playerId] = emissionTime = new BattleAmbienceEmissionTime
            {
                Smoke = 0f,
                Dust = 0f
            };
        }

        var dustEmissionDeltaTime = Time.unscaledTime - emissionTime.Dust;
        
        if (Random.Range(0f, 1f) < emissionChance && dustEmissionDeltaTime > 0.25f)
        {
            var smokeEffect = _cloudSmoke[Random.Range(0, _cloudSmoke.Length)];
            emission.Emit(smokeEffect, kinetics.Position, kinetics.Normal);
            emissionTime.Dust = Time.unscaledTime;
        }

        var smokeEmissionRoll = Random.Range(0f, 1f) < emissionChance;
        var smokeEmissionDeltaTime = Time.unscaledTime - emissionTime.Smoke;

        if (!(smokeEmissionRoll && smokeEmissionDeltaTime > 0.25f)) return;
        
        var dustEffect = _suspendedDust[Random.Range(0, _suspendedDust.Length)];
        emission.Emit(dustEffect, kinetics.Position, kinetics.Normal);
        emissionTime.Smoke = Time.unscaledTime;
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